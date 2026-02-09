// AI: purpose=StyleIdiom operator generating consistent crash patterns for PopRock chorus sections.
// AI: invariants=Apply only when style=PopRock and Section=Chorus; Crash role required; crash usually on beat 1.
// AI: deps=Bar, DrumCandidate, DrumArticulation; deterministic selection from (barNumber,seed).


using Music.Generator.Core;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.StyleIdiom
{
    // AI: purpose=Emit crash cymbal for PopRock choruses; default every-bar, alternate every-other or phrase-start patterns.
    // AI: note=Pattern is a section signature (low repetition penalty); selection depends on energy/seed.
    public sealed class PopChorusCrashPatternOperator : DrumOperatorBase
    {
        private const string PopRockStyleId = "PopRock";
        private const int VelocityMin = 100;
        private const int VelocityMax = 120;
        private const double BaseScore = 0.8;

        public override string OperatorId => "DrumPopChorusCrashPattern";

        public override OperatorFamily OperatorFamily => OperatorFamily.StyleIdiom;

        /// <inheritdoc/>
        public override IEnumerable<DrumCandidate> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Determine crash pattern based on energy
            CrashPattern pattern = DetermineCrashPattern(bar);

            if (!ShouldCrashThisBar(bar, pattern))
                yield break;

            // Generate crash on beat 1
            int velocityHint = GenerateVelocityHint(
                VelocityMin, VelocityMax,
                bar.BarNumber, 1.0m,
                seed);

            double score = ComputeScore(bar);

            yield return CreateCandidate(
                role: GrooveRoles.Crash,
                barNumber: bar.BarNumber,
                beat: 1.0m,
                strength: OnsetStrength.Downbeat,
                score: score,
                velocityHint: velocityHint,
                articulationHint: DrumArticulation.Crash);
        }

        private static CrashPattern DetermineCrashPattern(Bar bar)
        {
            // Higher energy = more crashes
            return 0.5 switch /* default energy */
            {
                >= 0.7 => CrashPattern.EveryBar,
                >= 0.5 => CrashPattern.EveryOtherBar,
                _ => CrashPattern.PhraseStartOnly
            };
        }

        private static bool ShouldCrashThisBar(Bar bar, CrashPattern pattern)
        {
            // Calculate position within section (0-based)
            int barInSection = GetBarPositionInSection(bar);

            return pattern switch
            {
                CrashPattern.EveryBar => true,
                CrashPattern.EveryOtherBar => barInSection % 2 == 0,
                CrashPattern.PhraseStartOnly => barInSection == 0 || bar.IsAtSectionBoundary,
                _ => false
            };
        }

        private static int GetBarPositionInSection(Bar bar)
        {
            // Approximate position based on BarsUntilSectionEnd
            // This is a simplification; actual implementation may use more context
            int totalBarsInSection = bar.BarsUntilSectionEnd + 1;
            int barInSection = totalBarsInSection - bar.BarsUntilSectionEnd - 1;
            return Math.Max(0, barInSection);
        }

        private double ComputeScore(Bar bar)
        {
            double score = BaseScore;

            // Boost at section boundary (first bar of chorus)
            if (bar.IsAtSectionBoundary)
                score += 0.1;

            // Energy scaling
            return Math.Clamp(score, 0.0, 1.0);
        }

        private enum CrashPattern
        {
            EveryBar,
            EveryOtherBar,
            PhraseStartOnly
        }
    }
}
