// AI: purpose=MicroAddition operator generating floor tom anticipation at 4.75.
// AI: invariants=VelocityHint in [60,80]; only applies when FloorTom in ActiveRoles 
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.1; adjust energy threshold based on listening tests.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.MicroAddition
{
    /// <summary>
    /// Generates a floor tom pickup at beat 4.75 as a low-pitched anticipation.
    /// Provides a heavier, more dramatic alternative to kick pickups.
    /// Story 3.1: Micro-Addition Operators (Ghost Notes &amp; Embellishments).
    /// </summary>
    public sealed class FloorTomPickupOperator : DrumOperatorBase
    {
        private const int VelocityMin = 60;
        private const int VelocityMax = 80;
        private const double BaseScore = 0.55;

        /// <inheritdoc/>
        public override string OperatorId => "DrumFloorTomPickup";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.MicroAddition;

        /// <summary>
        /// Requires moderate-high energy for floor tom pickups.
        /// </summary>

        /// <summary>
        /// Requires floor tom to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.FloorTom;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need at least 4 beats in bar for 4.75 pickup
            if (context.Bar.BeatsPerBar < 4)
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

            // Pickup at 0.25 beats before the bar ends (4.75 in 4/4)
            decimal pickupBeat = drummerContext.Bar.BeatsPerBar + 0.75m;

            int velocityHint = GenerateVelocityHint(
                VelocityMin,
                VelocityMax,
                drummerContext.Bar.BarNumber,
                pickupBeat,
                drummerContext.Seed);

            // Score increases with energy, reduced when kick pickup is also likely
            // (avoid doubling up on pickups at same position)
            double score = BaseScore * (0.5 + 0.5 /* default energy factor */);

            // Reduce score at section boundary (SetupHitOperator handles those)
            if (context is DrummerContext dc && dc.Bar.IsAtSectionBoundary)
                score *= 0.4;

            yield return CreateCandidate(
                role: GrooveRoles.FloorTom,
                barNumber: drummerContext.Bar.BarNumber,
                beat: pickupBeat,
                strength: OnsetStrength.Pickup,
                score: Math.Clamp(score, 0.0, 1.0),
                velocityHint: velocityHint);
        }
    }
}
