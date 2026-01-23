// AI: purpose=PhrasePunctuation operator generating ascending tom fill for tension building.
// AI: invariants=Only applies when IsFillWindow=true and toms are active; generates 6-12 ascending hits.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; uses Tom1, Tom2, FloorTom for ascending pattern.
// AI: change=Story 3.3; adjust pattern and velocity curve based on listening tests.

using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.PhrasePunctuation
{
    /// <summary>
    /// Generates an ascending tom fill pattern for tension building.
    /// Moves from floor tom (low) to high tom, creating upward energy.
    /// Story 3.3: Phrase Punctuation Operators (Boundaries &amp; Fills).
    /// </summary>
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
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.PhrasePunctuation;

        /// <summary>
        /// Requires moderate-high energy for build fills.
        /// </summary>
        protected override double MinEnergyThreshold => 0.5;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Only in fill window
            if (!context.IsFillWindow)
                return false;

            // Need at least one tom OR snare for build fill (snare is fallback)
            bool hasTomOrSnare = context.ActiveRoles.Contains(GrooveRoles.FloorTom) ||
                                 context.ActiveRoles.Contains(GrooveRoles.Tom1) ||
                                 context.ActiveRoles.Contains(GrooveRoles.Tom2) ||
                                 context.ActiveRoles.Contains(GrooveRoles.Snare);

            if (!hasTomOrSnare)
                return false;

            // Need at least 2 beats for meaningful build
            if (context.BeatsPerBar < 2)
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override IEnumerable<DrumCandidate> GenerateCandidates(Common.AgentContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            int beatsPerBar = drummerContext.BeatsPerBar;

            // Build fill occupies last 2 beats
            decimal fillStartBeat = Math.Max(1.0m, beatsPerBar - 1);

            // Compute hit count based on energy (6-12 hits)
            int hitCount = ComputeHitCount(drummerContext.EnergyLevel);

            // Get available toms in ascending order (low to high)
            var availableToms = GetAvailableTomsAscending(drummerContext.ActiveRoles);

            // Generate ascending pattern
            var positions = GenerateFillPositions(fillStartBeat, beatsPerBar, hitCount, drummerContext.Seed, drummerContext.BarNumber);

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
                    drummerContext.BarNumber, beat,
                    drummerContext.Seed);

                FillRole fillRole = positionIndex == 0 ? FillRole.FillStart :
                                    positionIndex == positionCount - 1 ? FillRole.FillEnd :
                                    FillRole.FillBody;

                double score = ComputeScore(drummerContext);

                yield return CreateCandidate(
                    role: role,
                    barNumber: drummerContext.BarNumber,
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

        private static double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Higher at section end
            if (context.BarsUntilSectionEnd <= 1)
                score *= 1.2;

            // Energy scaling
            score *= (0.5 + 0.5 * context.EnergyLevel);

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
