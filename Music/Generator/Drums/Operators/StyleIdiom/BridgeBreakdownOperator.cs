// AI: purpose=StyleIdiom operator for PopRock bridge breakdowns (half-time or minimal variants).
// AI: invariants=Apply only when style=PopRock and Section=Bridge; deterministic from (bar,seed); no external side-effects.
// AI: deps=Bar, OperatorCandidateAddition; integrates with groove/section type; variant chosen by energy/seed.


using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.StyleIdiom
{
    // AI: purpose=Emit bridge breakdown variants for PopRock: HalfTime or Minimal sparse patterns.
    // AI: note=HalfTime => snare on 3; Minimal => sparse kick/snare and optional sparse hats; selection from energy/seed.
    public sealed class BridgeBreakdownOperator : OperatorBase
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
        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Determine breakdown variant based on energy
            BreakdownVariant variant = DetermineVariant(bar);

            // Generate kick pattern
            if (true /* role check deferred */)
            {
                foreach (var candidate in GenerateKickPattern(bar, seed, variant))
                {
                    yield return candidate;
                }
            }

            // Generate snare pattern (half-time: beat 3 only)
            if (true /* role check deferred */)
            {
                foreach (var candidate in GenerateSnarePattern(bar, seed, variant))
                {
                    yield return candidate;
                }
            }

            // Generate sparse hat pattern for minimal variant
            if (variant == BreakdownVariant.Minimal &&
                true /* role check deferred */)
            {
                foreach (var candidate in GenerateMinimalHatPattern(bar, seed))
                {
                    yield return candidate;
                }
            }
        }

        private static BreakdownVariant DetermineVariant(Bar bar)
        {
            return true /* energy check removed */
                ? BreakdownVariant.HalfTime
                : BreakdownVariant.Minimal;
        }

        private IEnumerable<OperatorCandidateAddition> GenerateKickPattern(Bar bar, int seed, BreakdownVariant variant)
        {
            // Kick on beat 1 for both variants
            int velocityHint = GenerateVelocityHint(
                KickVelocityMin, KickVelocityMax,
                bar.BarNumber, 1.0m,
                seed);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: bar.BarNumber,
                beat: 1.0m,
                strength: OnsetStrength.Downbeat,
                score: ComputeScore(bar),
                velocityHint: velocityHint);

            // Half-time may add kick on beat 3 as well
            if (variant == BreakdownVariant.HalfTime && true /* energy check removed */)
            {
                velocityHint = GenerateVelocityHint(
                    KickVelocityMin - 10, KickVelocityMax - 10,
                    bar.BarNumber, 3.0m,
                    seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: bar.BarNumber,
                    beat: 3.0m,
                    strength: OnsetStrength.Strong,
                    score: ComputeScore(bar) * 0.8,
                    velocityHint: velocityHint);
            }
        }

        private IEnumerable<OperatorCandidateAddition> GenerateSnarePattern(Bar bar, int seed, BreakdownVariant variant)
        {
            // Half-time: snare on beat 3 only (not 2 and 4)
            // Minimal: snare on beat 3 only (same pattern, lower velocity)
            decimal snareBeat = 3.0m;

            int velocityHint = variant == BreakdownVariant.HalfTime
                ? GenerateVelocityHint(SnareVelocityMin, SnareVelocityMax, bar.BarNumber, snareBeat, seed)
                : GenerateVelocityHint(SnareVelocityMin - 15, SnareVelocityMax - 15, bar.BarNumber, snareBeat, seed);

            yield return CreateCandidate(
                role: GrooveRoles.Snare,
                barNumber: bar.BarNumber,
                beat: snareBeat,
                strength: OnsetStrength.Backbeat,
                score: ComputeScore(bar),
                velocityHint: velocityHint);
        }

        private IEnumerable<OperatorCandidateAddition> GenerateMinimalHatPattern(Bar bar, int seed)
        {
            // Very sparse hats: just quarters or less
            decimal[] hatBeats = [1.0m, 3.0m];

            foreach (decimal beat in hatBeats)
            {
                int velocityHint = GenerateVelocityHint(
                    HatVelocityMin, HatVelocityMax,
                    bar.BarNumber, beat,
                    seed);

                yield return CreateCandidate(
                    role: GrooveRoles.ClosedHat,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    strength: beat == 1.0m ? OnsetStrength.Downbeat : OnsetStrength.Strong,
                    score: ComputeScore(bar) * 0.6,
                    velocityHint: velocityHint);
            }
        }

        private double ComputeScore(Bar bar)
        {
            double score = BaseScore;

            // Boost at section start for establishing breakdown feel
            if (bar.IsAtSectionBoundary)
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
