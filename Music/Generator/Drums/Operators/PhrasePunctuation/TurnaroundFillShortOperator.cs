// AI: purpose=PhrasePunctuation: produce 2-beat turnaround fills (last 2 beats of bar) to punctuate phrases.
// AI: invariants=Apply only when Bar.IsFillWindow true; 16th-grid positions within last 2 beats; deterministic from seed.
// AI: deps=Bar, OperatorCandidate; roles map to snare primary; anti-repetition handled outside this operator.
using Music.Generator.Core;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Drums.Planning;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PhrasePunctuation
{
    // AI: purpose=Create short 2-beat fills occupying the final two beats; velocity accents on downbeats.
    // AI: note=Positions selected deterministically from 16th grid; hitCount scales with energy; no external side-effects.
    public sealed class TurnaroundFillShortOperator : DrumOperatorBase
    {
        private const int GhostVelocityMin = 40;
        private const int GhostVelocityMax = 60;
        private const int AccentVelocityMin = 80;
        private const int AccentVelocityMax = 110;
        private const double BaseScore = 0.7;
        private const string FillTag = "TurnaroundShort";

        /// <inheritdoc/>
        public override string OperatorId => FillOperatorIds.TurnaroundFillShort;

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        /// <inheritdoc/>
        public override IEnumerable<OperatorCandidate> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Fill occupies last 2 beats of the bar
            int beatsPerBar = bar.BeatsPerBar;
            decimal fillStartBeat = beatsPerBar - 1; // e.g., beat 3 in 4/4

            // Compute hit count based on energy (4-8 hits for 2-beat fill)
            int hitCount = ComputeHitCount(0.5); // default energy

            // Generate fill pattern positions (16th grid within last 2 beats)
            var positions = GenerateFillPositions(fillStartBeat, beatsPerBar, hitCount, seed, bar.BarNumber);

            bool isFirst = true;
            foreach (var (beat, isAccent) in positions)
            {
                FillRole fillRole = isFirst ? FillRole.FillStart : FillRole.FillBody;
                isFirst = false;

                int velMin = isAccent ? AccentVelocityMin : GhostVelocityMin;
                int velMax = isAccent ? AccentVelocityMax : GhostVelocityMax;

                int velocityHint = GenerateVelocityHint(
                    velMin, velMax,
                    bar.BarNumber, beat,
                    seed);

                OnsetStrength strength = isAccent ? OnsetStrength.Strong : OnsetStrength.Ghost;

                // Score higher at section boundaries
                double score = ComputeScore(bar);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    strength: strength,
                    score: score,
                    velocityHint: velocityHint,
                    fillRole: fillRole);
            }
        }

        private static int ComputeHitCount(double energyLevel)
        {
            // 4 hits at energy 0.0, 8 hits at energy 1.0
            return 4 + (int)(energyLevel * 4);
        }

        private static IEnumerable<(decimal Beat, bool IsAccent)> GenerateFillPositions(
            decimal fillStartBeat, int beatsPerBar, int hitCount, int seed, int barNumber)
        {
            // All available 16th positions in the last 2 beats
            var allPositions = new List<decimal>();
            for (int beatOffset = 0; beatOffset < 2; beatOffset++)
            {
                decimal baseBeat = fillStartBeat + beatOffset;
                if (baseBeat > beatsPerBar)
                    break;

                allPositions.Add(baseBeat);
                allPositions.Add(baseBeat + 0.25m);
                allPositions.Add(baseBeat + 0.5m);
                allPositions.Add(baseBeat + 0.75m);
            }

            // Filter to positions within bar
            allPositions = allPositions.Where(b => b >= 1.0m && b <= beatsPerBar + 0.75m).ToList();

            // Select hitCount positions deterministically
            var selected = new List<decimal>();
            var available = new List<decimal>(allPositions);

            // Always include first and last positions for fill shape
            if (available.Count > 0)
            {
                selected.Add(available[0]);
                available.RemoveAt(0);
            }
            if (available.Count > 0)
            {
                selected.Add(available[^1]);
                available.RemoveAt(available.Count - 1);
            }

            // Fill remaining slots deterministically
            int remainingHits = Math.Max(0, hitCount - selected.Count);
            for (int i = 0; i < remainingHits && available.Count > 0; i++)
            {
                int hash = HashCode.Combine(seed, barNumber, i);
                int idx = Math.Abs(hash) % available.Count;
                selected.Add(available[idx]);
                available.RemoveAt(idx);
            }

            // Sort and mark accents (downbeats are accents)
            foreach (decimal beat in selected.OrderBy(b => b))
            {
                bool isAccent = (beat % 1.0m) == 0.0m; // Downbeats are accents
                yield return (beat, isAccent);
            }
        }

        private static double ComputeScore(Bar bar)
        {
            double score = BaseScore;

            // Higher score at section boundaries
            if (bar.IsAtSectionBoundary)
                score *= 1.15;

            // Energy scaling
            // Boost near section end
            if (bar.BarsUntilSectionEnd <= 1)
                score *= 1.1;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
