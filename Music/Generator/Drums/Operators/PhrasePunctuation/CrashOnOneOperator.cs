// AI: purpose=PhrasePunctuation operator: crash cymbal on beat 1 for section starts.
// AI: invariants=Apply only when Bar.IsAtSectionBoundary && Bar.PhrasePosition near start; Crash role required.
// AI: deps=DrummerContext, DrumCandidate, FillRole semantics; deterministic velocity from (bar,seed).


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Performance;
using Music.Generator.Drums.Planning;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PhrasePunctuation
{
    // AI: purpose=Emit crash cymbal at bar 1 for section starts; marks transitions and fill ends.
    // AI: note=VelocityHint range [100,127]; reduced when not high energy; uses FillRole.FillEnd by convention.
    public sealed class CrashOnOneOperator : DrumOperatorBase
    {
        private const int VelocityMin = 100;
        private const int VelocityMax = 127;
        private const double BaseScore = 0.85;

        public override string OperatorId => FillOperatorIds.CrashOnOne;

        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        // Requires crash cymbal to be an active role in the groove preset.
        protected override string? RequiredRole => GrooveRoles.Crash;

        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Only at section boundaries (start of new section)
            if (!context.Bar.IsAtSectionBoundary)
                return false;

            // Crash on 1 is for section STARTS, not ends
            // Check if we're at the beginning of a section using PhrasePosition (0.0 = phrase start)
            // or by checking BarsUntilSectionEnd is high (indicating we're early in section)
            // A phrase position near 0.0 means we're at the start
            bool isAtSectionStart = context.Bar.PhrasePosition < 0.1;

            if (!isAtSectionStart)
                return false;

            return true;
        }

        // Generate a single crash candidate at beat 1 when at section start. Use deterministic velocity hint.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            int velocityHint = GenerateVelocityHint(
                VelocityMin,
                VelocityMax,
                drummerContext.Bar.BarNumber,
                1.0m,
                drummerContext.Seed);

            // Score increases with energy (crashes are more appropriate at higher energy)
            double score = BaseScore * (0.7 + 0.5 /* default energy factor */);

            yield return CreateCandidate(
                role: GrooveRoles.Crash,
                barNumber: drummerContext.Bar.BarNumber,
                beat: 1.0m,
                strength: OnsetStrength.Downbeat,
                score: Math.Clamp(score, 0.0, 1.0),
                velocityHint: velocityHint,
                articulationHint: DrumArticulation.Crash,
                fillRole: FillRole.FillEnd); // Crash on 1 marks the end of previous fill/transition
        }
    }
}
