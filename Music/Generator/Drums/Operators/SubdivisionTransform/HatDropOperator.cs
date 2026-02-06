// AI: purpose=SubdivisionTransform operator switching hats from 16ths to 8ths to decrease rhythmic density.
// AI: invariants=Apply in verse/bridge/intro; produces full-bar 8th hat candidates (beat.00 and beat.50).
// AI: deps=DrummerContext, DrumCandidate, Groove; uses Bar.BeatsPerBar for positions; deterministic via seed.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.SubdivisionTransform
{
    // AI: purpose=Emit full-bar 8th hi-hat pattern (two 8th positions per beat) to reduce hat density.
    // AI: note=Downbeats are accented; skip positions beyond bar end; scoring boosted near transitions.
    public sealed class HatDropOperator : DrumOperatorBase
    {
        private const int VelocityMin = 70;
        private const int VelocityMax = 95;
        private const int AccentVelocityMin = 85;
        private const int AccentVelocityMax = 105;
        private const double BaseScore = 0.6;

        /// <inheritdoc/>
        public override string OperatorId => "DrumHatDrop";

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        /// <summary>
        /// Only applies in appropriate sections (verse, bridge, intro).
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.ClosedHat;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Suitable for verse, bridge, intro (not chorus)
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Chorus)
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

            // Generate full bar of 8th notes
            int beatsPerBar = drummerContext.Bar.BeatsPerBar;
            
            for (int beatInt = 1; beatInt <= beatsPerBar; beatInt++)
            {
                // Two 8th positions per beat: .00, .50
                decimal[] positions = [0.0m, 0.5m];
                
                foreach (decimal offset in positions)
                {
                    decimal beat = beatInt + offset;
                    
                    // Skip if beyond bar
                    if (beat > beatsPerBar + 1)
                        continue;

                    bool isDownbeat = offset == 0.0m;
                    
                    OnsetStrength strength = isDownbeat ? OnsetStrength.Strong : OnsetStrength.Offbeat;

                    // Accents on downbeats
                    int velMin = isDownbeat ? AccentVelocityMin : VelocityMin;
                    int velMax = isDownbeat ? AccentVelocityMax : VelocityMax;
                    
                    int velocityHint = GenerateVelocityHint(
                        velMin, velMax,
                        drummerContext.Bar.BarNumber, beat,
                        drummerContext.Seed);

                    // Score increases at section transitions (verse entry, bridge)
                    double score = ComputeScore(drummerContext, isDownbeat);

                    yield return CreateCandidate(
                        role: GrooveRoles.ClosedHat,
                        barNumber: drummerContext.Bar.BarNumber,
                        beat: beat,
                        strength: strength,
                        score: score,
                        velocityHint: velocityHint);
                }
            }
        }

        private double ComputeScore(DrummerContext context, bool isDownbeat)
        {
            double score = BaseScore;

            // Boost at section transitions (verse/bridge entry often drops density)
            if (context.Bar.BarsUntilSectionEnd <= 2)
                score *= 1.1;

            if ( context.Bar.IsAtSectionBoundary)
                score *= 1.15;

            // Downbeats score higher (more important to select)
            if (isDownbeat)
                score *= 1.1;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
