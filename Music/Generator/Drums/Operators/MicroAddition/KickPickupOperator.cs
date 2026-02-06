// AI: purpose=MicroAddition operator generating kick anticipation at bar end (beat +0.75).
// AI: invariants=VelocityHint in [60,80]; requires Kick role and BeatsPerBar>=4; deterministic via Seed.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.MicroAddition
{
    // AI: purpose=Create a kick pickup at bar end (e.g., 4.75 in 4/4) to lead into next downbeat.
    // AI: invariants=Avoid at section boundaries; reduced score at boundaries; deterministic velocity via seed.
    public sealed class KickPickupOperator : DrumOperatorBase
    {
        private const int VelocityMin = 60;
        private const int VelocityMax = 80;
        private const double BaseScore = 0.65;

        public override string OperatorId => "DrumKickPickup";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        // Requires kick role active in groove; energy gating handled elsewhere in policy/selector.
        protected override string? RequiredRole => GrooveRoles.Kick;

        // Gate: needs at least BeatsPerBar >= 4 for pickup at BeatsPerBar + 0.75.
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need at least 4 beats in bar for 4.75 pickup
            if (context.Bar.BeatsPerBar < 4)
                return false;

            return true;
        }

        // Generate a single kick pickup candidate at bar end minus quarter beat. Reduce score at section boundaries.
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
