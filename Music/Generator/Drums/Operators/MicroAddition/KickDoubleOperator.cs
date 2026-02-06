// AI: purpose=MicroAddition operator: add extra kick hits on offbeats (8th or 16th variants).
// AI: invariants=VelocityHint in [60,80]; respects Bar.BeatsPerBar; deterministic selection from (bar,seed).
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.MicroAddition
{
    // Add extra kick hits on offbeats to enhance rhythmic drive.
    // Uses 8th positions by default; may use 16th variants when grid/energy allow.
    public sealed class KickDoubleOperator : DrumOperatorBase
    {
        private const int VelocityMin = 60;
        private const int VelocityMax = 80;
        private const double BaseScore = 0.55;
        private const double HighEnergyThreshold = 0.6;

        public override string OperatorId => "DrumKickDouble";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        // Requires kick role active in groove preset; energy gating happens in selector/policy.
        protected override string? RequiredRole => GrooveRoles.Kick;

        // CanApply: rely on base checks; no additional gating here.
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            return true;
        }

        // Generate kick double candidates at selected offbeat positions deterministically.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            // Determine whether to use 16th grid positions
            bool use16thGrid = false; // simplified: assume 8th grid

            // Select positions based on grid
            var positions = use16thGrid
                ? Select16thPositions(drummerContext)
                : Select8thPositions(drummerContext);

            foreach (decimal beat in positions)
            {
                int velocityHint = GenerateVelocityHint(
                    VelocityMin,
                    VelocityMax,
                    drummerContext.Bar.BarNumber,
                    beat,
                    drummerContext.Seed);

                // Score based on energy and position naturalness
                double score = BaseScore * (0.6 + 0.5 /* default energy factor */);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: beat,
                    strength: OnsetStrength.Offbeat,
                    score: Math.Clamp(score, 0.0, 1.0),
                    velocityHint: velocityHint);
            }
        }

        // Return 8th-note offbeat positions to place kick doubles (1.5, 3.5 when available).
        private static IEnumerable<decimal> Select8thPositions(DrummerContext context)
        {
            if (context.Bar.BeatsPerBar >= 2)
                yield return 1.5m;

            if (context.Bar.BeatsPerBar >= 4)
                yield return 3.5m;
        }

        // Return 16th-note positions for kick doubles; deterministically choose early/late variant per bar/seed.
        private static IEnumerable<decimal> Select16thPositions(DrummerContext context)
        {
            // Deterministic selection based on bar number and seed
            int hash = HashCode.Combine(context.Bar.BarNumber, context.Seed, "KickDouble16th");
            bool useEarlyVariant = (hash & 1) == 0;

            if (context.Bar.BeatsPerBar >= 2)
            {
                yield return useEarlyVariant ? 1.25m : 1.75m;
            }

            if (context.Bar.BeatsPerBar >= 4)
            {
                yield return useEarlyVariant ? 3.25m : 3.75m;
            }
        }
    }
}
