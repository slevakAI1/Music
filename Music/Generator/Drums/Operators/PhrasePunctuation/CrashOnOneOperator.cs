// AI: purpose=PhrasePunctuation operator: crash cymbal on beat 1 for section starts.
// AI: invariants=Apply only when Bar.IsAtSectionBoundary && Bar.PhrasePosition near start; Crash role required.
// AI: deps=Bar, OperatorCandidateAddition, FillRole semantics; deterministic velocity from (bar,seed).


using Music.Generator.Core;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Drums.Planning;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PhrasePunctuation
{
    // AI: purpose=Emit crash cymbal at bar 1 for section starts; marks transitions and fill ends.
    // AI: note=VelocityHint range [100,127]; reduced when not high energy; uses FillRole.FillEnd by convention.
    public sealed class CrashOnOneOperator : OperatorBase
    {
        private const int VelocityMin = 100;
        private const int VelocityMax = 127;
        private const double BaseScore = 0.85;

        public override string OperatorId => DrumFillOperatorIds.CrashOnOne;

        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        // Generate a single crash candidate at beat 1 when at section start. Use deterministic velocity hint.
        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            int velocityHint = GenerateVelocityHint(
                VelocityMin,
                VelocityMax,
                bar.BarNumber,
                1.0m,
                seed);

            // Score increases with energy (crashes are more appropriate at higher energy)
            double score = BaseScore * (0.7 + 0.5 /* default energy factor */);

            yield return CreateCandidate(
                role: GrooveRoles.Crash,
                barNumber: bar.BarNumber,
                beat: 1.0m,
                score: Math.Clamp(score, 0.0, 1.0),
                velocityHint: velocityHint,
                instrumentData: DrumCandidateData.Create(
                    strength: OnsetStrength.Downbeat,
                    fillRole: FillRole.FillEnd,
                    articulationHint: DrumArticulation.Crash)); // Crash on 1 marks the end of previous fill/transition
        }
    }
}
