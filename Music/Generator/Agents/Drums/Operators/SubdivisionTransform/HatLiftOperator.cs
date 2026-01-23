// AI: purpose=SubdivisionTransform operator switching hi-hat from 8ths to 16ths for energy increase.
// AI: invariants=Only applies when HatSubdivision==Eighth and EnergyLevel>=0.6; generates full 16th pattern for bar.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.2; adjust energy threshold or velocity curve based on listening tests.

using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.SubdivisionTransform
{
    /// <summary>
    /// Switches hi-hat pattern from 8ths to 16ths for increased rhythmic density.
    /// Generates full bar of 16th hi-hat candidates when energy is high.
    /// Story 3.2: Subdivision Transform Operators (Timekeeping Changes).
    /// </summary>
    public sealed class HatLiftOperator : DrumOperatorBase
    {
        private const int VelocityMin = 60;
        private const int VelocityMax = 85;
        private const int AccentVelocityMin = 80;
        private const int AccentVelocityMax = 100;
        private const double BaseScore = 0.65;

        /// <inheritdoc/>
        public override string OperatorId => "DrumHatLift";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.SubdivisionTransform;

        /// <summary>
        /// Requires high energy (>= 0.6) for 16th note density increase.
        /// </summary>
        protected override double MinEnergyThreshold => 0.6;

        /// <summary>
        /// Requires closed hi-hat to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.ClosedHat;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Suppress during fill windows - fills handle their own patterns
            if (context.IsFillWindow)
                return false;

            // Only lift from 8ths to 16ths
            if (context.HatSubdivision != HatSubdivision.Eighth)
                return false;

            // Requires hat mode (not ride)
            if (context.CurrentHatMode == HatMode.Ride)
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

            // Generate full bar of 16th notes
            int beatsPerBar = drummerContext.BeatsPerBar;
            
            for (int beatInt = 1; beatInt <= beatsPerBar; beatInt++)
            {
                // Four 16th positions per beat: .00, .25, .50, .75
                decimal[] positions = [0.0m, 0.25m, 0.5m, 0.75m];
                
                foreach (decimal offset in positions)
                {
                    decimal beat = beatInt + offset;
                    
                    // Skip if beyond bar
                    if (beat > beatsPerBar + 1)
                        continue;

                    bool isDownbeat = offset == 0.0m;
                    bool isOffbeat = offset == 0.5m;
                    
                    OnsetStrength strength = isDownbeat ? OnsetStrength.Strong : 
                                            isOffbeat ? OnsetStrength.Offbeat : 
                                            OnsetStrength.Ghost;

                    // Accents on downbeats, lighter on in-between
                    int velMin = isDownbeat ? AccentVelocityMin : VelocityMin;
                    int velMax = isDownbeat ? AccentVelocityMax : VelocityMax;
                    
                    int velocityHint = GenerateVelocityHint(
                        velMin, velMax,
                        drummerContext.BarNumber, beat,
                        drummerContext.Seed);

                    // Score increases at section transitions
                    double score = ComputeScore(drummerContext, isDownbeat);

                    yield return CreateCandidate(
                        role: GrooveRoles.ClosedHat,
                        barNumber: drummerContext.BarNumber,
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
            
            // Boost at section transitions
            if (context.BarsUntilSectionEnd <= 2)
                score *= 1.15;
            
            if (context.IsAtSectionBoundary)
                score *= 1.1;
            
            // Energy scaling
            score *= (0.7 + 0.3 * context.EnergyLevel);
            
            // Downbeats score higher (more important to select)
            if (isDownbeat)
                score *= 1.1;
            
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
