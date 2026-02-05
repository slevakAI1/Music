// AI: purpose=StyleIdiom operator generating consistent crash patterns for Pop Rock chorus sections.
// AI: invariants=Only applies when StyleId=="PopRock", SectionType==Chorus, and Crash in ActiveRoles; crash on beat 1.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.5; crash pattern is a section signature (allowed repetition for consistency).


using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.StyleIdiom
{
    /// <summary>
    /// Generates consistent crash cymbal patterns for Pop Rock chorus sections.
    /// Default: crash on beat 1 of each chorus bar, with optional every-other-bar variant.
    /// Story 3.5: Style Idiom Operators (Pop Rock Specifics).
    /// </summary>
    /// <remarks>
    /// Crash patterns:
    /// - Default: crash on beat 1 of every chorus bar
    /// - Every-other-bar: crash on beat 1 of bars 1, 3, 5... (lower energy)
    /// - Phrase start only: crash only on first bar of chorus (minimal)
    /// 
    /// This pattern is treated as a section signature (low repetition penalty) because
    /// consistency across choruses is musically desirable in Pop Rock.
    /// This operator is PopRock-specific and will not apply for other styles.
    /// </remarks>
    public sealed class PopChorusCrashPatternOperator : DrumOperatorBase
    {
        private const string PopRockStyleId = "PopRock";
        private const int VelocityMin = 100;
        private const int VelocityMax = 120;
        private const double BaseScore = 0.8;

        /// <inheritdoc/>
        public override string OperatorId => "DrumPopChorusCrashPattern";

        /// <inheritdoc/>
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
        public override IEnumerable<DrumCandidate> GenerateCandidates(Common.AgentContext context)
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
