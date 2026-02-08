// AI: purpose=Produce backbeat articulation variants (flam, rimshot, sidestick) to vary snare tone.
// AI: invariants=Uses Bar.BackbeatBeats; applies only when Snare role active; deterministic selection by (bar,seed).
// AI: deps=DrummerContext, DrumArticulation, DrumCandidate; integrates with Groove section types for selection.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Drums.Performance;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PatternSubstitution
{
    // AI: purpose=Generate alternative snare articulations for backbeat hits to change section character.
    // AI: note=Selection is section-aware (verse/chorus/bridge/etc.); lower base score to encourage sparing use.
    public sealed class BackbeatVariantOperator : DrumOperatorBase
    {
        private const int SideStickVelocityMin = 60;
        private const int SideStickVelocityMax = 80;
        private const int RimshotVelocityMin = 95;
        private const int RimshotVelocityMax = 115;
        private const int FlamVelocityMin = 85;
        private const int FlamVelocityMax = 105;
        private const double BaseScore = 0.5; // Lower than MicroAddition for sparing use

        public override string OperatorId => "DrumBackbeatVariant";

        public override OperatorFamily OperatorFamily => OperatorFamily.PatternSubstitution;

        // Requires snare role active; articulation changes intended for audible section-level effect.
        protected override string? RequiredRole => GrooveRoles.Snare;

        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need backbeat positions defined
            if (context.Bar.BackbeatBeats.Count == 0)
                return false;

            return true;
        }

        // Generate articulation-variant candidates for each backbeat; velocity/timing reflect articulation.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            // Determine articulation variant for this bar based on section and deterministic hash
            DrumArticulation articulation = SelectArticulation(drummerContext);

            // Skip if no articulation change (use standard backbeat)
            if (articulation == DrumArticulation.None)
                yield break;

            // Generate candidates for each backbeat position
            foreach (int backbeat in drummerContext.Bar.BackbeatBeats)
            {
                // Skip if backbeat beyond bar
                if (backbeat > drummerContext.Bar.BeatsPerBar)
                    continue;

                decimal beat = backbeat;

                // Get velocity range for articulation
                (int velMin, int velMax) = GetVelocityRange(articulation);

                int velocityHint = GenerateVelocityHint(
                    velMin, velMax,
                    drummerContext.Bar.BarNumber, beat,
                    drummerContext.Seed);

                // Optional timing offset for flam (grace note effect simulated via timing)
                int? timingHint = articulation == DrumArticulation.Flam ? -10 : null;

                double score = ComputeScore(drummerContext);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: beat,
                    strength: OnsetStrength.Backbeat,
                    score: score,
                    velocityHint: velocityHint,
                    timingHint: timingHint,
                    articulationHint: articulation);
            }
        }

        // Select articulation deterministically from section type and (bar,seed) entropy.
        // Prefer SideStick in Verse, Rimshot in Chorus, Flam/SideStick in Bridge when appropriate.
        private static DrumArticulation SelectArticulation(DrummerContext context)
        {
            // Deterministic selection based on section type and bar number
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            int hash = HashCode.Combine(context.Bar.BarNumber, sectionType, context.Seed, "BackbeatVariant");

            return sectionType switch
            {
                // Verse: prefer sidestick for lighter feel
                MusicConstants.eSectionType.Verse => DrumArticulation.SideStick,

                // Chorus: prefer rimshot for power
                MusicConstants.eSectionType.Chorus => DrumArticulation.Rimshot,

                // Bridge: prefer flam for texture or sidestick
                MusicConstants.eSectionType.Bridge => DrumArticulation.Flam,

                // Solo: rimshot for cutting through
                MusicConstants.eSectionType.Solo => DrumArticulation.Rimshot,

                // Intro/Outro: sidestick for subtlety
                MusicConstants.eSectionType.Intro or MusicConstants.eSectionType.Outro =>
                    DrumArticulation.SideStick,

                // Default: use deterministic hash to vary
                _ => (Math.Abs(hash) % 3) switch
                {
                    0 => DrumArticulation.Rimshot,
                    1 => DrumArticulation.SideStick,
                    _ => DrumArticulation.None
                }
            };
        }

        private static (int min, int max) GetVelocityRange(DrumArticulation articulation)
        {
            return articulation switch
            {
                DrumArticulation.SideStick => (SideStickVelocityMin, SideStickVelocityMax),
                DrumArticulation.Rimshot => (RimshotVelocityMin, RimshotVelocityMax),
                DrumArticulation.Flam => (FlamVelocityMin, FlamVelocityMax),
                _ => (80, 100)
            };
        }

        private double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Boost at section boundaries (articulation change marks new section)
            if (context.Bar.IsAtSectionBoundary)
                score += 0.15;

            // Slight boost at section start (first few bars)
            if (context.Bar.BarsUntilSectionEnd >= 6)
                score += 0.05;

            // Energy scaling
            score *= 0.7 + 0.5 /* default energy factor */;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
