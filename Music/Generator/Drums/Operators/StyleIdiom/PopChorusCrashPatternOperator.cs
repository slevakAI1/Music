// AI: purpose=StyleIdiom operator generating consistent crash patterns for PopRock chorus sections.
// AI: invariants=Apply only when style=PopRock and Section=Chorus; Crash role required; crash usually on beat 1.
// AI: deps=DrummerContext, DrumCandidate, DrumArticulation; deterministic selection from (barNumber,seed).


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Drums.Performance;
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

        /// <summary>
        /// Crashes are most effective at moderate to high energy.
        /// </summary>

        /// <summary>
        /// Requires crash role.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Crash;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Style gating: only PopRock
            if (!IsPopRockStyle(context))
                return false;

            // Section gating: only chorus
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType != MusicConstants.eSectionType.Chorus)
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

            // Determine crash pattern based on energy
            CrashPattern pattern = DetermineCrashPattern(drummerContext);

            if (!ShouldCrashThisBar(drummerContext, pattern))
                yield break;

            // Generate crash on beat 1
            int velocityHint = GenerateVelocityHint(
                VelocityMin, VelocityMax,
                drummerContext.Bar.BarNumber, 1.0m,
                drummerContext.Seed);

            double score = ComputeScore(drummerContext);

            yield return CreateCandidate(
                role: GrooveRoles.Crash,
                barNumber: drummerContext.Bar.BarNumber,
                beat: 1.0m,
                strength: OnsetStrength.Downbeat,
                score: score,
                velocityHint: velocityHint,
                articulationHint: DrumArticulation.Crash);
        }

        private static bool IsPopRockStyle(DrummerContext context)
        {
            return context.RngStreamKey?.Contains(PopRockStyleId, StringComparison.OrdinalIgnoreCase) == true
                || context.RngStreamKey?.StartsWith("Drummer_", StringComparison.Ordinal) == true;
        }

        private static CrashPattern DetermineCrashPattern(DrummerContext context)
        {
            // Higher energy = more crashes
            return 0.5 switch /* default energy */
            {
                >= 0.7 => CrashPattern.EveryBar,
                >= 0.5 => CrashPattern.EveryOtherBar,
                _ => CrashPattern.PhraseStartOnly
            };
        }

        private static bool ShouldCrashThisBar(DrummerContext context, CrashPattern pattern)
        {
            // Calculate position within section (0-based)
            int barInSection = GetBarPositionInSection(context);

            return pattern switch
            {
                CrashPattern.EveryBar => true,
                CrashPattern.EveryOtherBar => barInSection % 2 == 0,
                CrashPattern.PhraseStartOnly => barInSection == 0 || context.Bar.IsAtSectionBoundary,
                _ => false
            };
        }

        private static int GetBarPositionInSection(DrummerContext context)
        {
            // Approximate position based on BarsUntilSectionEnd
            // This is a simplification; actual implementation may use more context
            int totalBarsInSection = context.Bar.BarsUntilSectionEnd + 1;
            int barInSection = totalBarsInSection - context.Bar.BarsUntilSectionEnd - 1;
            return Math.Max(0, barInSection);
        }

        private double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Boost at section boundary (first bar of chorus)
            if (context.Bar.IsAtSectionBoundary)
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
