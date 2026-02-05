// AI: purpose=MicroAddition operator generating kick anticipation at 4.75 leading into next bar.
// AI: invariants=VelocityHint in [60,80]; only applies when Kick in ActiveRoles 
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.1; adjust energy threshold based on listening tests.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.MicroAddition
{
    /// <summary>
    /// Generates a kick pickup at beat 4.75 leading into the next bar's downbeat.
    /// Creates forward motion and anticipation for phrase continuation.
    /// Story 3.1: Micro-Addition Operators (Ghost Notes &amp; Embellishments).
    /// </summary>
    public sealed class KickPickupOperator : DrumOperatorBase
    {
        private const int VelocityMin = 60;
        private const int VelocityMax = 80;
        private const double BaseScore = 0.65;

        /// <inheritdoc/>
        public override string OperatorId => "DrumKickPickup";

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        /// <summary>
        /// Requires moderate energy for pickup to be musical.
        /// </summary>

        /// <summary>
        /// Requires kick to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Kick;

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
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            // Pickup at 0.25 beats before the bar ends (e.g., 4.75 in 4/4)
            decimal pickupBeat = drummerContext.Bar.BeatsPerBar + 0.75m;

            int velocityHint = GenerateVelocityHint(
                VelocityMin,
                VelocityMax,
                drummerContext.Bar.BarNumber,
                pickupBeat,
                drummerContext.Seed);

            // Score increases with energy and when not at section boundary
            // (section boundary pickups are handled by SetupHitOperator)
            double score = BaseScore * (0.6 + 0.5 /* default energy factor */);
            if (context is DrummerContext dc && dc.Bar.IsAtSectionBoundary)
                score *= 0.5; // Reduce score at section boundary

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: drummerContext.Bar.BarNumber,
                beat: pickupBeat,
                strength: OnsetStrength.Pickup,
                score: Math.Clamp(score, 0.0, 1.0),
                velocityHint: velocityHint);
        }
    }
}
