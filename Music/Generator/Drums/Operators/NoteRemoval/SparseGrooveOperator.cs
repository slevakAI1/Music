// AI: purpose=NoteRemoval operator that removes weak offbeat onsets to open up the groove.
// AI: invariants=Only targets offbeat/ghost-strength onsets; backbeats and downbeats are never targeted.
// AI: deps=OperatorBase, Bar, OperatorCandidateRemoval, GrooveRoles.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.NoteRemoval
{
    // AI: purpose=Strip offbeat ghost/weak onsets across kick and snare to create an open, minimal groove.
    // AI: note=Human drummers simplify when the vocal or lead instrument is busy; "less is more" approach.
    public sealed class SparseGrooveOperator : OperatorBase
    {
        // Offbeat positions that are candidates for removal (16th-note "e" and "a" positions).
        private static readonly decimal[] OffbeatFractions = [0.25m, 0.75m];

        public override string OperatorId => "DrumSparseGroove";

        public override OperatorFamily OperatorFamily => OperatorFamily.NoteRemoval;

        // Removal operators do not add onsets.
        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
            => [];

        // Remove kick and snare onsets on weak 16th-note positions ("e" and "a" of each beat).
        public override IEnumerable<OperatorRemovalCandidate> GenerateRemovals(Bar bar)
        {
            ArgumentNullException.ThrowIfNull(bar);

            int barNumber = bar.BarNumber;
            int beatsPerBar = bar.BeatsPerBar;
            string[] targetRoles = [GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat];

            for (int beat = 1; beat <= beatsPerBar; beat++)
            {
                foreach (decimal fraction in OffbeatFractions)
                {
                    decimal position = beat + fraction;
                    foreach (string role in targetRoles)
                    {
                        yield return new OperatorRemovalCandidate
                        {
                            BarNumber = barNumber,
                            Beat = position,
                            Role = role,
                            OperatorId = OperatorId,
                            Reason = "Strip weak offbeat for sparser groove"
                        };
                    }
                }
            }
        }
    }
}
