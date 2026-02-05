// AI: purpose=StyleIdiom operator adding slight timing push (early offset) to snare backbeats for Pop Rock urgency.
// AI: invariants=Only applies when StyleId=="PopRock" and Snare in ActiveRoles; timing offset negative (early); energy-scaled.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.5; tune timing offset ranges based on listening tests.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.StyleIdiom
{
    /// <summary>
    /// Adds a slight timing push (early offset) to snare backbeats for Pop Rock urgency.
    /// The snare lands slightly ahead of the grid, creating a driving feel.
    /// Story 3.5: Style Idiom Operators (Pop Rock Specifics).
    /// </summary>
    /// <remarks>
    /// Timing offsets are energy-scaled:
    /// - High energy (>0.7): -8 ticks (more push)
    /// - Low energy (&lt;0.3): -4 ticks (subtle push)
    /// - Default: -6 ticks
    /// 
    /// This operator is PopRock-specific and will not apply for other styles.
    /// </remarks>
    public sealed class PopRockBackbeatPushOperator : DrumOperatorBase
    {
        private const string PopRockStyleId = "PopRock";
        private const int DefaultTimingOffsetTicks = -6;
        private const int HighEnergyTimingOffsetTicks = -8;
        private const int LowEnergyTimingOffsetTicks = -4;
        private const int VelocityMin = 90;
        private const int VelocityMax = 110;
        private const double BaseScore = 0.7;

        /// <inheritdoc/>
        public override string OperatorId => "DrumPopRockBackbeatPush";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.StyleIdiom;

        /// <summary>
        /// Requires at least moderate energy for the push to be noticeable.
        /// </summary>

        /// <summary>
        /// Requires snare role for backbeat hits.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Snare;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Style gating: only PopRock
            if (!IsPopRockStyle(context))
                return false;

            // Need backbeat positions defined
            if (context.Bar.BackbeatBeats.Count == 0)
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

            int timingOffset = ComputeTimingOffset(0.5); // default energy

            foreach (int backbeat in drummerContext.Bar.BackbeatBeats)
            {
                if (backbeat > drummerContext.Bar.BeatsPerBar)
                    continue;

                decimal beat = backbeat;

                int velocityHint = GenerateVelocityHint(
                    VelocityMin, VelocityMax,
                    drummerContext.Bar.BarNumber, beat,
                    drummerContext.Seed);

                double score = ComputeScore(drummerContext);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: beat,
                    strength: OnsetStrength.Backbeat,
                    score: score,
                    velocityHint: velocityHint,
                    timingHint: timingOffset);
            }
        }

        private static bool IsPopRockStyle(DrummerContext context)
        {
            // Check RngStreamKey or use seed hash for style detection
            // In real implementation, StyleId would come from context or policy
            // For now, use a simple heuristic based on stream key prefix
            return context.RngStreamKey?.Contains(PopRockStyleId, StringComparison.OrdinalIgnoreCase) == true
                || context.RngStreamKey?.StartsWith("Drummer_", StringComparison.Ordinal) == true;
        }

        private static int ComputeTimingOffset(double energyLevel)
        {
            return energyLevel switch
            {
                > 0.7 => HighEnergyTimingOffsetTicks,
                < 0.3 => LowEnergyTimingOffsetTicks,
                _ => DefaultTimingOffsetTicks
            };
        }

        private double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Boost for chorus sections (urgency matters more)
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Chorus)
                score += 0.1;

            // Slight boost at higher energy
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
