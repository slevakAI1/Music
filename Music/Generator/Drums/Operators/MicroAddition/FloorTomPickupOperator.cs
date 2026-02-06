// AI: purpose=MicroAddition operator generating floor tom anticipation at 4.75.
// AI: invariants=VelocityHint in [60,80]; only applies when FloorTom in ActiveRoles 
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.

using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.MicroAddition
{
    // AI: purpose=MicroAddition operator: floor-tom pickup anticipation at bar end (beat +0.75).
    // AI: invariants=VelocityHint in [60,80]; only applies when GrooveRoles.FloorTom active; deterministic via Seed.
    public sealed class FloorTomPickupOperator : DrumOperatorBase
    {
        private const int VelocityMin = 60;
        private const int VelocityMax = 80;
        private const double BaseScore = 0.55;

        public override string OperatorId => "DrumFloorTomPickup";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        // Requires floor tom be an active role in groove preset; operator self-gates on BeatsPerBar >= 4.
        protected override string? RequiredRole => GrooveRoles.FloorTom;

        // Gate: needs at least 4 beats (pickup at beat BeatsPerBar + 0.75) and role present via RequiredRole.
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need at least 4 beats in bar for 4.75 pickup
            if (context.Bar.BeatsPerBar < 4)
                return false;

            return true;
        }

        // Generate a single pickup candidate at end-of-bar minus quarter (e.g., 4.75 in 4/4).
        // Velocity determined deterministically via GenerateVelocityHint; score reduced at section boundaries.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
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
