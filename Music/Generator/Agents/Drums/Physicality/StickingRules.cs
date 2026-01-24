// AI: purpose=Implements sticking validation rules for drum candidates.
// AI: invariants=Defaults: MaxConsecutiveSameHand=4, MaxGhostsPerBar=4, MinGapBetweenFastHits=TPQN/4.
// AI: deps=LimbModel, LimbAssignment, DrumCandidate, MusicConstants.TicksPerQuarterNote.
namespace Music.Generator.Agents.Drums.Physicality
{
    public sealed class StickingRules
    {        public int MaxConsecutiveSameHand { get; init; } = 4;        public int MaxGhostsPerBar { get; init; } = 4;        public int MinGapBetweenFastHits { get; init; } = MusicConstants.TicksPerQuarterNote / 4; // default: 16th gap (120)
        private readonly LimbModel _limbModel;
        public StickingRules(LimbModel? limbModel = null)        {            _limbModel = limbModel ?? LimbModel.Default;        }
        public StickingValidation ValidatePattern(IReadOnlyList<DrumCandidate> candidates)        {            return ValidatePattern(candidates, _limbModel);        }
        public StickingValidation ValidatePattern(IReadOnlyList<DrumCandidate> candidates, LimbModel limbModel)        {            ArgumentNullException.ThrowIfNull(candidates);            ArgumentNullException.ThrowIfNull(limbModel);
            var validation = new StickingValidation();
            if (candidates.Count == 0)                return validation;
            // Count ghosts per bar (only OnsetStrength.Ghost)            var ghostsByBar = candidates                .Where(c => c.Strength == Music.Generator.Groove.OnsetStrength.Ghost)                .GroupBy(c => c.BarNumber)                .ToDictionary(g => g.Key, g => g.Select(c => c.CandidateId).ToList());
            foreach (var kv in ghostsByBar)            {                if (kv.Value.Count > MaxGhostsPerBar)                {                    validation.Violations.Add(new StickingViolation(                        "MaxGhostsPerBar",                        $"Bar {kv.Key} has {kv.Value.Count} ghost hits (max {MaxGhostsPerBar}).",                        kv.Value,                        kv.Key,                        0m,                        null));                }            }
            // Build limb assignments with absolute tick ordering.            // We cannot compute absolute ticks without BarTrack here; use bar,beat ordering and TimingHint as tie-breaker.            var assignments = new List<(DrumCandidate Candidate, Limb Limb, long SortKey)>();
            foreach (var c in candidates)            {                var limb = limbModel.GetRequiredLimb(c.Role);                if (!limb.HasValue)                    continue; // skip unknown roles per policy Answer 5
                // Compute a sort key: bar * large + beat*1000 + timingHint (nullable)                // Beat is decimal (1-based). Multiply fractional by 1000 to preserve order.                int beatWhole = (int)Math.Floor(c.Beat);                int beatFrac = (int)((c.Beat - beatWhole) * 1000);                int timingHint = c.TimingHint ?? 0;                long key = ((long)c.BarNumber << 32) ^ ((long)beatWhole << 16) ^ (long)beatFrac ^ timingHint;                assignments.Add((c, limb.Value, key));            }
            // Group assignments per limb and sort by key (temporal order)            var byLimb = assignments                .OrderBy(a => a.SortKey)                .GroupBy(a => a.Limb)                .ToDictionary(g => g.Key, g => g.Select(a => a.Candidate).ToList());
            foreach (var kv in byLimb)            {                var limb = kv.Key;                var list = kv.Value.OrderBy(c => c.BarNumber).ThenBy(c => c.Beat).ThenBy(c => c.TimingHint ?? 0).ToList();                // Check consecutive same-hand hits and min gap
                int consecutive = 0;
                long? lastTickApprox = null;
                string? lastCandidateId = null;
                foreach (var c in list)
                {
                    // Approximate absolute tick using bar + beat.
                    // Bar contributes (barNumber - 1) * 4 beats * TPQN (assuming 4/4).
                    // Beat contributes (beat - 1.0) * TPQN + TimingHint.
                    long barOffset = (long)(c.BarNumber - 1) * 4 * MusicConstants.TicksPerQuarterNote;
                    long beatOffset = (long)((c.Beat - 1.0m) * MusicConstants.TicksPerQuarterNote);
                    long approxTick = barOffset + beatOffset + (c.TimingHint ?? 0);

                    if (lastTickApprox.HasValue)
                    {
                        long gap = Math.Abs(approxTick - lastTickApprox.Value);
                        // Use <= to include hits at exactly the minimum gap as "fast" consecutive hits
                        if (gap <= MinGapBetweenFastHits)
                        {
                            consecutive++;
                        }
                        else
                        {
                            consecutive = 1; // reset chain
                        }
                    }
                    else
                    {
                        consecutive = 1;
                    }
                    lastTickApprox = approxTick;
                    lastCandidateId = c.CandidateId;
                    if (consecutive > MaxConsecutiveSameHand)
                    {
                        validation.Violations.Add(new StickingViolation(
                            "MaxConsecutiveSameHand",
                            $"Limb {limb} exceeds max consecutive hits ({MaxConsecutiveSameHand}) at candidate {c.CandidateId}.",
                            new List<string> { c.CandidateId },
                            c.BarNumber,
                            c.Beat,
                            limb));
                        // do not break; report all occurrences
                    }
                }
            }
            return validation;
        }
    }
}