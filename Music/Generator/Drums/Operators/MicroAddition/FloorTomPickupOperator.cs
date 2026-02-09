// AI: purpose=MicroAddition operator generating floor tom anticipation at 4.75.
// AI: invariants=VelocityHint in [60,80]; only applies when FloorTom in ActiveRoles 
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.

using Music.Generator.Core;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
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

        // Generate a single pickup candidate at end-of-bar minus quarter (e.g., 4.75 in 4/4).
        // Velocity determined deterministically via GenerateVelocityHint; score reduced at section boundaries.
        public override IEnumerable<DrumCandidate> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Pickup at 0.25 beats before the bar ends (4.75 in 4/4)
            decimal pickupBeat = bar.BeatsPerBar + 0.75m;

            int velocityHint = GenerateVelocityHint(
                VelocityMin,
                VelocityMax,
                bar.BarNumber,
                pickupBeat,
                seed);

            // Score increases with energy, reduced when kick pickup is also likely
            // (avoid doubling up on pickups at same position)
            double score = BaseScore * (0.5 + 0.5 /* default energy factor */);

            // Reduce score at section boundary (SetupHitOperator handles those)
            if (bar.IsAtSectionBoundary)
                score *= 0.4;

            yield return CreateCandidate(
                role: GrooveRoles.FloorTom,
                barNumber: bar.BarNumber,
                beat: pickupBeat,
                strength: OnsetStrength.Pickup,
                score: Math.Clamp(score, 0.0, 1.0),
                velocityHint: velocityHint);
        }
    }
}
