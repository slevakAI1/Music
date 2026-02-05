// AI: purpose=SubdivisionTransform operator switching hi-hat from 16ths to 8ths for density decrease.
// AI: invariants=Only applies in suitable sections (verse, bridge); generates full 8th pattern for bar.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.2; adjust section checks based on listening tests.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.SubdivisionTransform
{
    /// <summary>
    /// Switches hi-hat pattern from 16ths to 8ths for decreased rhythmic density.
    /// Generates full bar of 8th hi-hat candidates when energy is low.
    /// Story 3.2: Subdivision Transform Operators (Timekeeping Changes).
    /// </summary>
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
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.SubdivisionTransform;

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
        public override IEnumerable<DrumCandidate> GenerateCandidates(Common.AgentContext context)
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
