// AI: purpose=SubdivisionTransform operator: lift to 16ths only in second half of bar to build energy.
// AI: invariants=Produces 8th grid in first half, 16th grid in second half; skip positions beyond bar end.
// AI: deps=Bar provides BeatsPerBar/IsAtSectionBoundary; deterministic positions from (seed,bar).


using Music.Generator.Core;
using Music.Generator.Groove;
using Music.Generator.Drums.Operators.Candidates;

namespace Music.Generator.Drums.Operators.SubdivisionTransform
{
    // AI: purpose=Apply 16th subdivision only for the latter half of the bar (e.g., beats 3-4 in 4/4).
    // AI: note=Used to create intra-bar energy build; first half remains 8ths to preserve anchor hits.
    public sealed class PartialLiftOperator : OperatorBase
    {
        private const int VelocityMin = 65;
        private const int VelocityMax = 85;
        private const int AccentVelocityMin = 80;
        private const int AccentVelocityMax = 100;
        private const double BaseScore = 0.6;

        public override string OperatorId => "DrumPartialLift";

        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        // AI: purpose=Emit 8th positions for first half and 16th positions for second half deterministically.
        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            int beatsPerBar = bar.BeatsPerBar;
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
                        bar.BarNumber, beat,
                        seed);

                    double score = ComputeScore(bar, isDownbeat, isSecondHalf);

                    yield return CreateCandidate(
                        role: GrooveRoles.ClosedHat,
                        barNumber: bar.BarNumber,
                        beat: beat,
                        score: score,
                        velocityHint: velocityHint,
                        instrumentData: DrumCandidateData.Create(strength: strength));
                }
            }
        }

        private double ComputeScore(Bar bar, bool isDownbeat, bool isSecondHalf)
        {
            double score = BaseScore;

            // Partial lift works well leading into section ends
            if (bar.BarsUntilSectionEnd <= 2)
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
