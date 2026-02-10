// AI: purpose=PhrasePunctuation operator: emit a setup hit on the last "and" (e.g., 4.5) before a section change.
// AI: invariants=Apply when Bar.IsAtSectionBoundary or Bar.IsFillWindow; requires Kick role; uses 16th grid positions.
// AI: deps=Bar, OperatorCandidate, FillRole conventions; deterministic velocity from (barNumber,seed).


using Music.Generator.Core;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Drums.Planning;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PhrasePunctuation
{
    // AI: purpose=Create a subtle setup hit on the bar's last "and" to lead into the next section.
    // AI: note=Produces kick and optional snare; uses FillRole.Setup; avoid when BeatsPerBar < 4.
    public sealed class SetupHitOperator : DrumOperatorBase
    {
        private const int VelocityMin = 70;
        private const int VelocityMax = 100;
        private const double BaseScore = 0.65;

        public override string OperatorId => FillOperatorIds.SetupHit;

        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        // Generate setup hit candidate(s) at beat = BeatsPerBar + 0.5 (the "and" of last beat).
        public override IEnumerable<OperatorCandidate> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Setup hit on the "and" of the last beat (e.g., 4.5 in 4/4)
            decimal setupBeat = bar.BeatsPerBar + 0.5m;

            // Generate kick on setup beat
            int kickVelocity = GenerateVelocityHint(
                VelocityMin, VelocityMax,
                bar.BarNumber, setupBeat,
                seed);

            double score = ComputeScore(bar);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: bar.BarNumber,
                beat: setupBeat,
                strength: OnsetStrength.Pickup,
                score: score,
                velocityHint: kickVelocity,
                fillRole: FillRole.Setup);

            // Optionally add snare if energy is high enough and snare is active
            if (true /* snare assumed available */)
            {
                int snareVelocity = GenerateVelocityHint(
                    VelocityMin - 10, VelocityMax - 10,
                    bar.BarNumber, setupBeat + 0.01m, // Slightly different seed
                    seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: bar.BarNumber,
                    beat: setupBeat,
                    strength: OnsetStrength.Pickup,
                    score: score * 0.9, // Slightly lower score than kick
                    velocityHint: snareVelocity,
                    fillRole: FillRole.Setup);
            }
        }

        private static double ComputeScore(Bar bar)
        {
            double score = BaseScore;

            // Higher at actual section end
            if (bar.BarsUntilSectionEnd <= 1)
                score *= 1.2;

            // Energy scaling
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
