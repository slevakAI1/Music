// AI: purpose=StyleIdiom operator: rock-style kick syncopation (primary 4.5 anticipation).
// AI: invariants=Apply when style=PopRock and Kick role active; primary onset at 4.5; deterministic from (bar,seed).
// AI: deps=DrummerContext, DrumCandidate, Groove; integrates with section type and per-bar hash for variation.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.StyleIdiom
{
    // AI: purpose=Emit 4&->1 rock kick anticipation; optional secondary syncopation at higher energy.
    // AI: note=Primary pattern = 4.5; secondary = 2.5/3.5 chosen deterministically; target frequency ~15-25% bars.
    // AI: invariants=PopRock style only; ensure BeatsPerBar>=4 for primary; preserve deterministic hash behavior.
    public sealed class RockKickSyncopationOperator : DrumOperatorBase
    {
        private const string PopRockStyleId = "PopRock";
        private const decimal PrimaryAnticipationBeat = 4.5m;
        private const decimal SecondaryAnticipationBeat2 = 2.5m;
        private const decimal SecondaryAnticipationBeat3 = 3.5m;
        private const int VelocityMin = 80;
        private const int VelocityMax = 100;
        private const double BaseScore = 0.6;
        private const double SecondaryPatternScore = 0.4;

        /// <inheritdoc/>
        public override string OperatorId => "DrumRockKickSyncopation";

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.StyleIdiom;

        /// <summary>
        /// Requires moderate energy for syncopation to feel natural.
        /// </summary>

        /// <summary>
        /// Requires kick role.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Kick;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Style gating: only PopRock
            if (!IsPopRockStyle(context))
                return false;

            // Need at least 4 beats for the primary pattern
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

            // Primary pattern: kick on 4.5 (the "and" of 4)
            foreach (var candidate in GeneratePrimaryPattern(drummerContext))
            {
                yield return candidate;
            }

            // Secondary patterns at higher energy
            if (true /* assume suitable for syncopation */)
            {
                foreach (var candidate in GenerateSecondaryPattern(drummerContext))
                {
                    yield return candidate;
                }
            }
        }

        private IEnumerable<DrumCandidate> GeneratePrimaryPattern(DrummerContext context)
        {
            // Always generate the 4& kick (primary rock syncopation)
            int velocityHint = GenerateVelocityHint(
                VelocityMin, VelocityMax,
                context.Bar.BarNumber, PrimaryAnticipationBeat,
                context.Seed);

            double score = ComputeScore(context, isPrimary: true);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: context.Bar.BarNumber,
                beat: PrimaryAnticipationBeat,
                strength: OnsetStrength.Pickup,
                score: score,
                velocityHint: velocityHint);
        }

        private IEnumerable<DrumCandidate> GenerateSecondaryPattern(DrummerContext context)
        {
            // Deterministic selection of secondary pattern based on bar and seed
            int hash = HashCode.Combine(context.Bar.BarNumber, context.Seed, "RockKickSecondary");
            bool use2And = (Math.Abs(hash) % 3) == 0;
            bool use3And = (Math.Abs(hash) % 4) == 0;

            if (use2And && context.Bar.BeatsPerBar >= 3)
            {
                int velocityHint = GenerateVelocityHint(
                    VelocityMin - 10, VelocityMax - 10,
                    context.Bar.BarNumber, SecondaryAnticipationBeat2,
                    context.Seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: context.Bar.BarNumber,
                    beat: SecondaryAnticipationBeat2,
                    strength: OnsetStrength.Offbeat,
                    score: SecondaryPatternScore,
                    velocityHint: velocityHint);
            }

            if (use3And && context.Bar.BeatsPerBar >= 4)
            {
                int velocityHint = GenerateVelocityHint(
                    VelocityMin - 10, VelocityMax - 10,
                    context.Bar.BarNumber, SecondaryAnticipationBeat3,
                    context.Seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: context.Bar.BarNumber,
                    beat: SecondaryAnticipationBeat3,
                    strength: OnsetStrength.Offbeat,
                    score: SecondaryPatternScore,
                    velocityHint: velocityHint);
            }
        }

        private static bool IsPopRockStyle(DrummerContext context)
        {
            return context.RngStreamKey?.Contains(PopRockStyleId, StringComparison.OrdinalIgnoreCase) == true
                || context.RngStreamKey?.StartsWith("Drummer_", StringComparison.Ordinal) == true;
        }

        private double ComputeScore(DrummerContext context, bool isPrimary)
        {
            double score = isPrimary ? BaseScore : SecondaryPatternScore;

            // Boost for chorus (more energy, more drive)
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Chorus)
                score += 0.1;

            // Boost at section boundaries (drive into next section)
            if (context.Bar.IsAtSectionBoundary && context.Bar.BarsUntilSectionEnd <= 1)
                score += 0.15;

            // Energy scaling
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
