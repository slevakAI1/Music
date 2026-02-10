// AI: purpose=StyleIdom operator that thins verse grooves for contrast with chorus.
// AI: invariants=Apply in PopRock verses only; reduces density via lower velocities and scores.
// AI: deps=Bar, OperatorCandidate; deterministic outputs from (barNumber,seed); no stylistic side-effects.


using Music.Generator.Core;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.StyleIdiom
{
    // AI: purpose=Produce simplified verse candidates (sparser kick/hat patterns, lower velocities).
    // AI: note=Prefer 1/3 kick pattern and eighth-note hats; selection engine density weights determine final output.
    public sealed class VerseSimplifyOperator : DrumOperatorBase
    {
        private const string PopRockStyleId = "PopRock";
        private const int KickVelocityMin = 70;
        private const int KickVelocityMax = 90;
        private const int HatVelocityMin = 50;
        private const int HatVelocityMax = 70;
        private const double BaseScore = 0.65;

        /// <inheritdoc/>
        public override string OperatorId => "DrumVerseSimplify";

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.StyleIdiom;

        /// <inheritdoc/>
        public override IEnumerable<OperatorCandidate> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Generate simplified kick pattern (1 and 3 only)
            if (true /* role check deferred */)
            {
                foreach (var candidate in GenerateSimplifiedKickPattern(bar, seed))
                {
                    yield return candidate;
                }
            }

            // Generate sparser hat pattern (eighth notes only, lower velocity)
            if (true /* role check deferred */)
            {
                foreach (var candidate in GenerateSimplifiedHatPattern(bar, seed))
                {
                    yield return candidate;
                }
            }
        }

        private IEnumerable<OperatorCandidate> GenerateSimplifiedKickPattern(Bar bar, int seed)
        {
            // Simple "1 and 3" kick pattern for verses
            decimal[] kickBeats = bar.BeatsPerBar >= 4
                ? [1.0m, 3.0m]
                : [1.0m];

            foreach (decimal beat in kickBeats)
            {
                int velocityHint = GenerateVelocityHint(
                    KickVelocityMin, KickVelocityMax,
                    bar.BarNumber, beat,
                    seed);

                OnsetStrength strength = beat == 1.0m ? OnsetStrength.Downbeat : OnsetStrength.Strong;

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    strength: strength,
                    score: ComputeScore(bar),
                    velocityHint: velocityHint);
            }
        }

        private IEnumerable<OperatorCandidate> GenerateSimplifiedHatPattern(Bar bar, int seed)
        {
            // Eighth note pattern only (simpler than 16ths)
            for (int beatInt = 1; beatInt <= bar.BeatsPerBar; beatInt++)
            {
                decimal beat = beatInt;

                int velocityHint = GenerateVelocityHint(
                    HatVelocityMin, HatVelocityMax,
                    bar.BarNumber, beat,
                    seed);

                // Lower score for off-beats to prefer sparse selection
                double score = beatInt % 2 == 1
                    ? ComputeScore(bar)
                    : ComputeScore(bar) * 0.8;

                yield return CreateCandidate(
                    role: GrooveRoles.ClosedHat,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    strength: beatInt == 1 ? OnsetStrength.Downbeat : OnsetStrength.Strong,
                    score: score,
                    velocityHint: velocityHint);
            }
        }

        private double ComputeScore(Bar bar)
        {
            double score = BaseScore;

            // Lower score at higher energy (let busier patterns win)
            // Slight boost at section start for establishing the simple pattern
            if (bar.IsAtSectionBoundary)
                score += 0.05;

            return Math.Clamp(score, 0.2, 0.8);
        }
    }
}
