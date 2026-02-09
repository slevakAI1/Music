// AI: purpose=NoteRemoval operator that removes kick on beat 1 for anticipation/tension.
// AI: invariants=Never removes from bar 1 of a section (preserves section entry downbeat).
// AI: deps=DrumOperatorBase, IDrumRemovalOperator, DrummerContext, RemovalCandidate, GrooveRoles.

using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.NoteRemoval
{
    // AI: purpose=Remove kick on beat 1 in interior bars to create "missing downbeat" anticipation.
    // AI: note=Common funk/pop technique; absence of expected kick creates forward momentum.
    public sealed class KickPullOperator : DrumOperatorBase, IDrumRemovalOperator
    {
        public override string OperatorId => "DrumKickPull";

        public override OperatorFamily OperatorFamily => OperatorFamily.NoteRemoval;

        // Removal operators do not add onsets.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
            => [];

        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Never pull kick on section start — downbeat anchors the section.
            if (context.Bar.BarWithinSection == 0)
                return false;

            // Avoid fill windows; fills manage their own kick placement.
            if (context.Bar.IsFillWindow)
                return false;

            return true;
        }

        // Remove kick on beat 1 of the current bar.
        public IEnumerable<RemovalCandidate> GenerateRemovals(DrummerContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!CanApply(context))
                yield break;

            yield return new RemovalCandidate
            {
                BarNumber = context.Bar.BarNumber,
                Beat = 1.0m,
                Role = GrooveRoles.Kick,
                OperatorId = OperatorId,
                Reason = "Pull kick on beat 1 for anticipation (missing downbeat)"
            };
        }
    }
}
