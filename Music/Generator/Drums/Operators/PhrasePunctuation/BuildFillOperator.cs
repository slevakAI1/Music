// AI: purpose=PhrasePunctuation operator generating ascending tom fill for tension building.
// AI: invariants=Apply only when Bar.IsFillWindow true and at least one tom/snare active; produces 6-12 hits.
// AI: deps=DrumOperatorBase, Bar, OperatorCandidate; uses Tom1/Tom2/FloorTom mapping for ascending pitch.
using Music.Generator.Core;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Drums.Planning;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PhrasePunctuation
{
    // AI: purpose=Create ascending tom fill in fill window; maps positions to available toms low->high.
    // AI: note=Fill occupies last two beats by default; velocity crescendos; deterministic selection from (seed,bar).
    public sealed class BuildFillOperator : DrumOperatorBase
    {
        private const int VelocityStartMin = 60;
        private const int VelocityStartMax = 80;
        private const int VelocityEndMin = 95;
        private const int VelocityEndMax = 120;
        private const double BaseScore = 0.7;
        private const string FillTag = "BuildFill_Ascending";

        /// <inheritdoc/>
        public override string OperatorId => FillOperatorIds.BuildFill;

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        /// <inheritdoc/>
        public override IEnumerable<OperatorCandidate> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            int beatsPerBar = bar.BeatsPerBar;

            // Build fill occupies last 2 beats
            decimal fillStartBeat = Math.Max(1.0m, beatsPerBar - 1);

            // Compute hit count based on energy (6-12 hits)
            int hitCount = ComputeHitCount(0.5); // default energy

            // Get available toms in ascending order (low to high)
            var availableToms = GetAvailableTomsAscending(new HashSet<string> { GrooveRoles.Tom1, GrooveRoles.Tom2, GrooveRoles.FloorTom });

            // Generate ascending pattern
            var positions = GenerateFillPositions(fillStartBeat, beatsPerBar, hitCount, seed, bar.BarNumber);

            int positionIndex = 0;
            int positionCount = positions.Count;

            foreach (decimal beat in positions)
            {
                // Map position to tom (ascending: start low, end high)
                string role = MapPositionToTom(positionIndex, positionCount, availableToms);

                // Velocity crescendo (start soft, end loud)
                double velocityProgress = (double)positionIndex / Math.Max(1, positionCount - 1);
                int velMin = (int)(VelocityStartMin + (VelocityEndMin - VelocityStartMin) * velocityProgress);
                int velMax = (int)(VelocityStartMax + (VelocityEndMax - VelocityStartMax) * velocityProgress);

                int velocityHint = GenerateVelocityHint(
                    velMin, velMax,
                    bar.BarNumber, beat,
                    seed);

                FillRole fillRole = positionIndex == 0 ? FillRole.FillStart :
                                    positionIndex == positionCount - 1 ? FillRole.FillEnd :
                                    FillRole.FillBody;

                double score = ComputeScore(bar);

                yield return CreateCandidate(
                    role: role,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    strength: OnsetStrength.Strong,
                    score: score,
                    velocityHint: velocityHint,
                    fillRole: fillRole);

                positionIndex++;
            }
        }

        private static int ComputeHitCount(double energyLevel)
        {
            // 6 hits at energy 0.5, 12 hits at energy 1.0
            return 6 + (int)((energyLevel - 0.5) / 0.5 * 6);
        }

        private static List<string> GetAvailableTomsAscending(IReadOnlySet<string> activeRoles)
        {
            var toms = new List<string>();

            // Order: FloorTom (lowest) -> Tom2 (mid) -> Tom1 (highest)
            if (activeRoles.Contains(GrooveRoles.FloorTom))
                toms.Add(GrooveRoles.FloorTom);
            if (activeRoles.Contains(GrooveRoles.Tom2))
                toms.Add(GrooveRoles.Tom2);
            if (activeRoles.Contains(GrooveRoles.Tom1))
                toms.Add(GrooveRoles.Tom1);

            // Fallback to snare if no toms available
            if (toms.Count == 0 && activeRoles.Contains(GrooveRoles.Snare))
                toms.Add(GrooveRoles.Snare);

            return toms;
        }

        private static string MapPositionToTom(int positionIndex, int positionCount, List<string> availableToms)
        {
            if (availableToms.Count == 0)
                return GrooveRoles.Snare;

            if (availableToms.Count == 1)
                return availableToms[0];

            // Map position to tom index (ascending: low early, high late)
            double progress = (double)positionIndex / Math.Max(1, positionCount - 1);
            int tomIndex = (int)(progress * (availableToms.Count - 1) + 0.5);
            tomIndex = Math.Clamp(tomIndex, 0, availableToms.Count - 1);

            return availableToms[tomIndex];
        }

        private static List<decimal> GenerateFillPositions(
            decimal fillStartBeat, int beatsPerBar, int hitCount, int seed, int barNumber)
        {
            // Generate 16th grid positions for last 2 beats
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

            // Filter to within bar
            allPositions = allPositions.Where(b => b >= 1.0m && b <= beatsPerBar + 0.75m).ToList();

            // Select positions deterministically
            var selected = new List<decimal>();
            var available = new List<decimal>(allPositions);

            // Ensure first position
            if (available.Count > 0)
            {
                selected.Add(available[0]);
                available.RemoveAt(0);
            }

            // Fill remaining
            int remaining = Math.Max(0, hitCount - selected.Count);
            for (int i = 0; i < remaining && available.Count > 0; i++)
            {
                int hash = HashCode.Combine(seed, barNumber, "BuildFill", i);
                int idx = Math.Abs(hash) % available.Count;
                selected.Add(available[idx]);
                available.RemoveAt(idx);
            }

            return selected.OrderBy(b => b).ToList();
        }

        private static double ComputeScore(Bar bar)
        {
            double score = BaseScore;

            // Higher at section end
            if (bar.BarsUntilSectionEnd <= 1)
                score *= 1.2;

            // Energy scaling
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
