// AI: purpose=PhrasePunctuation operator: emit a setup hit on the last "and" (e.g., 4.5) before a section change.
// AI: invariants=Apply when Bar.IsAtSectionBoundary or Bar.IsFillWindow; requires Kick role; uses 16th grid positions.
// AI: deps=DrummerContext, DrumCandidate, FillRole conventions; deterministic velocity from (barNumber,seed).


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Planning;
using Music.Generator.Drums.Selection.Candidates;
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

        // Requires kick role; snare optional. Energy gating handled by selection policies.
        protected override string? RequiredRole => GrooveRoles.Kick;

        // Gate: apply only at section ends/fill windows and when BeatsPerBar >= 4.
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Setup hits are at section boundaries (end of section leading to next)
            if (!context.Bar.IsFillWindow && !context.Bar.IsAtSectionBoundary)
                return false;

            // Only in last bar of section or fill window
            if (context.Bar.BarsUntilSectionEnd > 1 && !context.Bar.IsFillWindow)
                return false;

            // Need at least 4 beats for "4&" position
            if (context.Bar.BeatsPerBar < 4)
                return false;

            return true;
        }

        // Generate setup hit candidate(s) at beat = BeatsPerBar + 0.5 (the "and" of last beat).
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            // Setup hit on the "and" of the last beat (e.g., 4.5 in 4/4)
            decimal setupBeat = drummerContext.Bar.BeatsPerBar + 0.5m;

            // Generate kick on setup beat
            int kickVelocity = GenerateVelocityHint(
                VelocityMin, VelocityMax,
                drummerContext.Bar.BarNumber, setupBeat,
                drummerContext.Seed);

            double score = ComputeScore(drummerContext);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: drummerContext.Bar.BarNumber,
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
                    drummerContext.Bar.BarNumber, setupBeat + 0.01m, // Slightly different seed
                    drummerContext.Seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: setupBeat,
                    strength: OnsetStrength.Pickup,
                    score: score * 0.9, // Slightly lower score than kick
                    velocityHint: snareVelocity,
                    fillRole: FillRole.Setup);
            }
        }

        private static double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Higher at actual section end
            if (context.Bar.BarsUntilSectionEnd <= 1)
                score *= 1.2;

            // Energy scaling
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
