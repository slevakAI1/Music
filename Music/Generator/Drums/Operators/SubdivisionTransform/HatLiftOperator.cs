// AI: purpose=SubdivisionTransform operator switching hi-hat from 8ths to 16ths to increase density.
// AI: invariants=Apply in suitable sections; produces full 16th grid candidates for the bar; deterministic by seed.
// AI: deps=Bar, DrumCandidate, Groove; avoid altering downbeat anchors; no runtime behavior changes.


using Music.Generator.Core;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.SubdivisionTransform
{
    // AI: purpose=Emit full-bar 16th hi-hat pattern (four 16th positions per beat) to raise hat density.
    // AI: note=Downbeats get accents; skip positions beyond bar end; score boosted near transitions; deterministic.
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
        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        /// <inheritdoc/>
        public override IEnumerable<DrumCandidate> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Generate full bar of 16th notes
            int beatsPerBar = bar.BeatsPerBar;

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
                        bar.BarNumber, beat,
                        seed);

                    // Score increases at section transitions
                    double score = ComputeScore(bar, isDownbeat);

                    yield return CreateCandidate(
                        role: GrooveRoles.ClosedHat,
                        barNumber: bar.BarNumber,
                        beat: beat,
                        strength: strength,
                        score: score,
                        velocityHint: velocityHint);
                }
            }
        }

        private double ComputeScore(Bar bar, bool isDownbeat)
        {
            double score = BaseScore;

            // Boost at section transitions
            if (bar.BarsUntilSectionEnd <= 2)
                score *= 1.15;

            if (bar.IsAtSectionBoundary)
                score *= 1.1;

            // Energy scaling            
            // Downbeats score higher (more important to select)
            if (isDownbeat)
                score *= 1.1;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
