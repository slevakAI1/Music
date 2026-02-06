// AI: purpose=StyleIdiom operator for PopRock bridge breakdowns (half-time or minimal variants).
// AI: invariants=Apply only when style=PopRock and Section=Bridge; deterministic from (bar,seed); no external side-effects.
// AI: deps=DrummerContext, DrumCandidate; integrates with groove/section type; variant chosen by energy/seed.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.StyleIdiom
{
    // AI: purpose=Emit bridge breakdown variants for PopRock: HalfTime or Minimal sparse patterns.
    // AI: note=HalfTime => snare on 3; Minimal => sparse kick/snare and optional sparse hats; selection from energy/seed.
    public sealed class BridgeBreakdownOperator : DrumOperatorBase
    {
        private const string PopRockStyleId = "PopRock";
        private const int KickVelocityMin = 75;
        private const int KickVelocityMax = 95;
        private const int SnareVelocityMin = 85;
        private const int SnareVelocityMax = 105;
        private const int HatVelocityMin = 45;
        private const int HatVelocityMax = 65;
        private const double BaseScore = 0.7;

        /// <inheritdoc/>
        public override string OperatorId => "DrumBridgeBreakdown";

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.StyleIdiom;

        /// <summary>
        /// Works at any energy level (bridges can vary widely).
        /// </summary>

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Style gating: only PopRock
            if (!IsPopRockStyle(context))
                return false;

            // Section gating: only bridge
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType != MusicConstants.eSectionType.Bridge)
                return false;

            // Need at least 4 beats for half-time pattern
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

            // Determine breakdown variant based on energy
            BreakdownVariant variant = DetermineVariant(drummerContext);

            // Generate kick pattern
            if (true /* role check deferred */)
            {
                foreach (var candidate in GenerateKickPattern(drummerContext, variant))
                {
                    yield return candidate;
                }
            }

            // Generate snare pattern (half-time: beat 3 only)
            if (true /* role check deferred */)
            {
                foreach (var candidate in GenerateSnarePattern(drummerContext, variant))
                {
                    yield return candidate;
                }
            }

            // Generate sparse hat pattern for minimal variant
            if (variant == BreakdownVariant.Minimal &&
                true /* role check deferred */)
            {
                foreach (var candidate in GenerateMinimalHatPattern(drummerContext))
                {
                    yield return candidate;
                }
            }
        }

        private static BreakdownVariant DetermineVariant(DrummerContext context)
        {
            return true /* energy check removed */
                ? BreakdownVariant.HalfTime
                : BreakdownVariant.Minimal;
        }

        private IEnumerable<DrumCandidate> GenerateKickPattern(DrummerContext context, BreakdownVariant variant)
        {
            // Kick on beat 1 for both variants
            int velocityHint = GenerateVelocityHint(
                KickVelocityMin, KickVelocityMax,
                context.Bar.BarNumber, 1.0m,
                context.Seed);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: context.Bar.BarNumber,
                beat: 1.0m,
                strength: OnsetStrength.Downbeat,
                score: ComputeScore(context),
                velocityHint: velocityHint);

            // Half-time may add kick on beat 3 as well
            if (variant == BreakdownVariant.HalfTime && true /* energy check removed */)
            {
                velocityHint = GenerateVelocityHint(
                    KickVelocityMin - 10, KickVelocityMax - 10,
                    context.Bar.BarNumber, 3.0m,
                    context.Seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: context.Bar.BarNumber,
                    beat: 3.0m,
                    strength: OnsetStrength.Strong,
                    score: ComputeScore(context) * 0.8,
                    velocityHint: velocityHint);
            }
        }

        private IEnumerable<DrumCandidate> GenerateSnarePattern(DrummerContext context, BreakdownVariant variant)
        {
            // Half-time: snare on beat 3 only (not 2 and 4)
            // Minimal: snare on beat 3 only (same pattern, lower velocity)
            decimal snareBeat = 3.0m;

            int velocityHint = variant == BreakdownVariant.HalfTime
                ? GenerateVelocityHint(SnareVelocityMin, SnareVelocityMax, context.Bar.BarNumber, snareBeat, context.Seed)
                : GenerateVelocityHint(SnareVelocityMin - 15, SnareVelocityMax - 15, context.Bar.BarNumber, snareBeat, context.Seed);

            yield return CreateCandidate(
                role: GrooveRoles.Snare,
                barNumber: context.Bar.BarNumber,
                beat: snareBeat,
                strength: OnsetStrength.Backbeat,
                score: ComputeScore(context),
                velocityHint: velocityHint);
        }

        private IEnumerable<DrumCandidate> GenerateMinimalHatPattern(DrummerContext context)
        {
            // Very sparse hats: just quarters or less
            decimal[] hatBeats = [1.0m, 3.0m];

            foreach (decimal beat in hatBeats)
            {
                int velocityHint = GenerateVelocityHint(
                    HatVelocityMin, HatVelocityMax,
                    context.Bar.BarNumber, beat,
                    context.Seed);

                yield return CreateCandidate(
                    role: GrooveRoles.ClosedHat,
                    barNumber: context.Bar.BarNumber,
                    beat: beat,
                    strength: beat == 1.0m ? OnsetStrength.Downbeat : OnsetStrength.Strong,
                    score: ComputeScore(context) * 0.6,
                    velocityHint: velocityHint);
            }
        }

        private static bool IsPopRockStyle(DrummerContext context)
        {
            return context.RngStreamKey?.Contains(PopRockStyleId, StringComparison.OrdinalIgnoreCase) == true
                || context.RngStreamKey?.StartsWith("Drummer_", StringComparison.Ordinal) == true;
        }

        private double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Boost at section start for establishing breakdown feel
            if (context.Bar.IsAtSectionBoundary)
                score += 0.1;

            // Slight reduction at higher energy (less breakdown-y)
            return Math.Clamp(score, 0.3, 0.9);
        }

        private enum BreakdownVariant
        {
            HalfTime,
            Minimal
        }
    }
}
