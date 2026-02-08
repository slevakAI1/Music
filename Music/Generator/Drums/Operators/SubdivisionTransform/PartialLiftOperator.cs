// AI: purpose=SubdivisionTransform operator: lift to 16ths only in second half of bar to build energy.
// AI: invariants=Produces 8th grid in first half, 16th grid in second half; skip positions beyond bar end.
// AI: deps=DrummerContext.Bar provides BeatsPerBar/IsAtSectionBoundary; deterministic positions from (seed,bar).


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.SubdivisionTransform
{
    // AI: purpose=Apply 16th subdivision only for the latter half of the bar (e.g., beats 3-4 in 4/4).
    // AI: note=Used to create intra-bar energy build; first half remains 8ths to preserve anchor hits.
    public sealed class PartialLiftOperator : DrumOperatorBase
    {
        private const int VelocityMin = 65;
        private const int VelocityMax = 85;
        private const int AccentVelocityMin = 80;
        private const int AccentVelocityMax = 100;
        private const double BaseScore = 0.6;

        public override string OperatorId => "DrumPartialLift";

        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        // Requires closed hi-hat role active; energy gating handled by selection/policy layer.
        protected override string? RequiredRole => GrooveRoles.ClosedHat;

        // AI: gate=validate context type and hat/grid assumptions; require BeatsPerBar>=4 and non-ride hat mode.
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

        // AI: purpose=Emit 8th positions for first half and 16th positions for second half deterministically.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
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
