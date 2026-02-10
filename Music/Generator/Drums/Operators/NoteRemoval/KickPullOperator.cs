// AI: purpose=NoteRemoval operator that removes kick on beat 1 for anticipation/tension.
// AI: invariants=Never removes from bar 1 of a section (preserves section entry downbeat).
// AI: deps=OperatorBase, Bar, RemovalCandidate, GrooveRoles.

using Music.Generator.Core;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.NoteRemoval
{
    // AI: purpose=Remove kick on beat 1 in interior bars to create "missing downbeat" anticipation.
    // AI: note=Common funk/pop technique; absence of expected kick creates forward momentum.
    public sealed class KickPullOperator : OperatorBase
    {
        public override string OperatorId => "DrumKickPull";

        public override OperatorFamily OperatorFamily => OperatorFamily.NoteRemoval;

        // Removal operators do not add onsets.
        public override IEnumerable<OperatorCandidate> GenerateCandidates(Bar bar, int seed)
            => [];

        // Remove kick on beat 1 of the current bar.
        public override IEnumerable<RemovalCandidate> GenerateRemovals(Bar bar)
        {
            ArgumentNullException.ThrowIfNull(bar);

            yield return new RemovalCandidate
            {
                BarNumber = bar.BarNumber,
                Beat = 1.0m,
                Role = GrooveRoles.Kick,
                OperatorId = OperatorId,
                Reason = "Pull kick on beat 1 for anticipation (missing downbeat)"
            };
        }
    }
}
