// AI: purpose=PhrasePunctuation operator generating crash cymbal on beat 1 at section starts.
// AI: invariants=Only applies when IsAtSectionBoundary=true and Crash in ActiveRoles; VelocityHint in [100,127].
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.3; adjust base score or velocity based on listening tests.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.PhrasePunctuation
{
    /// <summary>
    /// Generates a crash cymbal hit on beat 1 at section/phrase starts.
    /// Provides clear punctuation marking transitions between sections.
    /// Story 3.3: Phrase Punctuation Operators (Boundaries &amp; Fills).
    /// </summary>
    public sealed class CrashOnOneOperator : DrumOperatorBase
    {
        private const int VelocityMin = 100;
        private const int VelocityMax = 127;
        private const double BaseScore = 0.85;

        /// <inheritdoc/>
        public override string OperatorId => FillOperatorIds.CrashOnOne;

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.PhrasePunctuation;

        /// <summary>
        /// Requires crash cymbal to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Crash;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Only at section boundaries (start of new section)
            if (!context.IsAtSectionBoundary)
                return false;

            // Crash on 1 is for section STARTS, not ends
            // Check if we're at the beginning of a section using PhrasePosition (0.0 = phrase start)
            // or by checking BarsUntilSectionEnd is high (indicating we're early in section)
            // A phrase position near 0.0 means we're at the start
            bool isAtSectionStart = context.PhrasePosition < 0.1;

            if (!isAtSectionStart)
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

            int velocityHint = GenerateVelocityHint(
                VelocityMin,
                VelocityMax,
                drummerContext.BarNumber,
                1.0m,
                drummerContext.Seed);

            // Score increases with energy (crashes are more appropriate at higher energy)
            double score = BaseScore * (0.7 + 0.3 * drummerContext.EnergyLevel);

            yield return CreateCandidate(
                role: GrooveRoles.Crash,
                barNumber: drummerContext.BarNumber,
                beat: 1.0m,
                strength: OnsetStrength.Downbeat,
                score: Math.Clamp(score, 0.0, 1.0),
                velocityHint: velocityHint,
                articulationHint: DrumArticulation.Crash,
                fillRole: FillRole.FillEnd); // Crash on 1 marks the end of previous fill/transition
        }
    }
}
