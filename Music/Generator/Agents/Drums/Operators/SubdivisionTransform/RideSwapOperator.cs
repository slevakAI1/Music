// AI: purpose=SubdivisionTransform operator switching from hi-hat to ride cymbal for timbral variation.
// AI: invariants=Only applies when CurrentHatMode!=Ride and Ride in ActiveRoles; generates full ride pattern for bar.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.2; adjust scoring for section types (bridges, choruses) based on listening tests.


using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.SubdivisionTransform
{
    /// <summary>
    /// Switches timekeeping from hi-hat to ride cymbal for timbral variation.
    /// Generates ride pattern matching current subdivision (8th or 16th).
    /// Story 3.2: Subdivision Transform Operators (Timekeeping Changes).
    /// </summary>
    public sealed class RideSwapOperator : DrumOperatorBase
    {
        private const int VelocityMin = 70;
        private const int VelocityMax = 90;
        private const int BellVelocityMin = 85;
        private const int BellVelocityMax = 105;
        private const double BaseScore = 0.55;

        /// <inheritdoc/>
        public override string OperatorId => "DrumRideSwap";

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        /// <summary>
        /// Requires moderate energy (>= 0.4) for ride to be musical.
        /// </summary>

        /// <summary>
        /// Requires ride cymbal to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Ride;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Only swap from hat to ride (not if already on ride)
            if (HatMode.Closed /* default assumption */ == HatMode.Ride)
                return false;

            // Need a defined subdivision to work with
            if (HatSubdivision.Eighth /* default assumption */ == HatSubdivision.None)
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

            // Generate ride pattern matching current subdivision
            int beatsPerBar = drummerContext.Bar.BeatsPerBar;
            bool is16th = HatSubdivision.Eighth /* default assumption */ == HatSubdivision.Sixteenth;
            
            for (int beatInt = 1; beatInt <= beatsPerBar; beatInt++)
            {
                decimal[] positions = is16th 
                    ? [0.0m, 0.25m, 0.5m, 0.75m] 
                    : [0.0m, 0.5m];
                
                foreach (decimal offset in positions)
                {
                    decimal beat = beatInt + offset;
                    
                    if (beat > beatsPerBar + 1)
                        continue;

                    bool isDownbeat = offset == 0.0m;
                    bool isOffbeat = offset == 0.5m;
                    
                    // Use bell on beat 1 for emphasis
                    bool useBell = beatInt == 1 && isDownbeat;
                    
                    OnsetStrength strength = isDownbeat ? OnsetStrength.Strong : 
                                            isOffbeat ? OnsetStrength.Offbeat : 
                                            OnsetStrength.Ghost;

                    int velMin = useBell ? BellVelocityMin : VelocityMin;
                    int velMax = useBell ? BellVelocityMax : VelocityMax;
                    
                    int velocityHint = GenerateVelocityHint(
                        velMin, velMax,
                        drummerContext.Bar.BarNumber, beat,
                        drummerContext.Seed);

                    double score = ComputeScore(drummerContext, isDownbeat, useBell);

                    // Use RideBell articulation for beat 1 downbeat
                    DrumArticulation? articulation = useBell ? DrumArticulation.RideBell : DrumArticulation.Ride;

                    yield return CreateCandidate(
                        role: GrooveRoles.Ride,
                        barNumber: drummerContext.Bar.BarNumber,
                        beat: beat,
                        strength: strength,
                        score: score,
                        velocityHint: velocityHint,
                        articulationHint: articulation);
                }
            }
        }

        private double ComputeScore(DrummerContext context, bool isDownbeat, bool useBell)
        {
            double score = BaseScore;
            
            // Ride swap is often used at chorus or bridge sections
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Chorus)
                score *= 1.2;
            else if (sectionType == MusicConstants.eSectionType.Bridge)
                score *= 1.15;
            
            // Boost at section boundaries
            if (context.Bar.IsAtSectionBoundary)
                score *= 1.1;
            
            // Energy scaling (ride works better at moderate-high energy)            
            // Bell hits and downbeats score higher
            if (useBell)
                score *= 1.15;
            else if (isDownbeat)
                score *= 1.1;
            
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
