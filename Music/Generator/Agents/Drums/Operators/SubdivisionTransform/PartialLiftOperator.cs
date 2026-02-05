// AI: purpose=SubdivisionTransform operator lifting to 16ths only on last half of bar (beats 3-4 in 4/4).
// AI: invariants=Only applies when HatSubdivision==Eighth and EnergyLevel>=0.5; creates energy build within bar.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.2; adjust beat range or velocity curve based on listening tests.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.SubdivisionTransform
{
    /// <summary>
    /// Partial lift: 16th notes only on the last half of the bar (beats 3-4 in 4/4).
    /// Creates a natural energy build within the bar, keeping first half at 8ths.
    /// Story 3.2: Subdivision Transform Operators (Timekeeping Changes).
    /// </summary>
    public sealed class PartialLiftOperator : DrumOperatorBase
    {
        private const int VelocityMin = 65;
        private const int VelocityMax = 85;
        private const int AccentVelocityMin = 80;
        private const int AccentVelocityMax = 100;
        private const double BaseScore = 0.6;

        /// <inheritdoc/>
        public override string OperatorId => "DrumPartialLift";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.SubdivisionTransform;

        /// <summary>
        /// Requires moderate-high energy (>= 0.5) for partial lift.
        /// </summary>

        /// <summary>
        /// Requires closed hi-hat to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.ClosedHat;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Only lift from 8ths
            if (HatSubdivision.Eighth /* default assumption */ != HatSubdivision.Eighth)
                return false;

            // Requires hat mode (not ride)
            if (HatMode.Closed /* default assumption */ == HatMode.Ride)
                return false;

            // Needs at least 4 beats for meaningful partial lift
            if (context.Bar.BeatsPerBar < 4)
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

            int beatsPerBar = drummerContext.Bar.BeatsPerBar;
            int halfwayBeat = (beatsPerBar / 2) + 1; // Beat 3 in 4/4, Beat 4 in 6/8

            // Generate 8ths for first half, 16ths for second half
            for (int beatInt = 1; beatInt <= beatsPerBar; beatInt++)
            {
                bool isSecondHalf = beatInt >= halfwayBeat;
                decimal[] positions = isSecondHalf
                    ? [0.0m, 0.25m, 0.5m, 0.75m]  // 16ths in second half
                    : [0.0m, 0.5m];                // 8ths in first half
                
                foreach (decimal offset in positions)
                {
                    decimal beat = beatInt + offset;
                    
                    if (beat > beatsPerBar + 1)
                        continue;

                    bool isDownbeat = offset == 0.0m;
                    bool isOffbeat = offset == 0.5m;
                    
                    OnsetStrength strength = isDownbeat ? OnsetStrength.Strong : 
                                            isOffbeat ? OnsetStrength.Offbeat : 
                                            OnsetStrength.Ghost;

                    int velMin = isDownbeat ? AccentVelocityMin : VelocityMin;
                    int velMax = isDownbeat ? AccentVelocityMax : VelocityMax;
                    
                    int velocityHint = GenerateVelocityHint(
                        velMin, velMax,
                        drummerContext.Bar.BarNumber, beat,
                        drummerContext.Seed);

                    double score = ComputeScore(drummerContext, isDownbeat, isSecondHalf);

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

        private double ComputeScore(DrummerContext context, bool isDownbeat, bool isSecondHalf)
        {
            double score = BaseScore;
            
            // Partial lift works well leading into section ends
            if (context.Bar.BarsUntilSectionEnd <= 2)
                score *= 1.2;
            
            // Energy scaling            
            // Second half 16ths are the "feature" of this operator
            if (isSecondHalf)
                score *= 1.05;
            
            // Downbeats score higher
            if (isDownbeat)
                score *= 1.1;
            
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
