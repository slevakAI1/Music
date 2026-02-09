// AI: purpose=NoteRemoval operator that removes weak offbeat onsets to open up the groove.
// AI: invariants=Only targets offbeat/ghost-strength onsets; backbeats and downbeats are never targeted.
// AI: deps=DrumOperatorBase, IDrumRemovalOperator, DrummerContext, RemovalCandidate, GrooveRoles.

using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.NoteRemoval
{
    // AI: purpose=Strip offbeat ghost/weak onsets across kick and snare to create an open, minimal groove.
    // AI: note=Human drummers simplify when the vocal or lead instrument is busy; "less is more" approach.
    public sealed class SparseGrooveOperator : DrumOperatorBase, IDrumRemovalOperator
    {
        // Offbeat positions that are candidates for removal (16th-note "e" and "a" positions).
        private static readonly decimal[] OffbeatFractions = [0.25m, 0.75m];

        public override string OperatorId => "DrumSparseGroove";

        public override OperatorFamily OperatorFamily => OperatorFamily.NoteRemoval;

        // Removal operators do not add onsets.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
            => [];

        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Avoid fill windows; fills need their density.
            if (context.Bar.IsFillWindow)
                return false;

            return true;
        }

        // Remove kick and snare onsets on weak 16th-note positions ("e" and "a" of each beat).
        public IEnumerable<RemovalCandidate> GenerateRemovals(DrummerContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!CanApply(context))
                yield break;

            int barNumber = context.Bar.BarNumber;
            int beatsPerBar = context.Bar.BeatsPerBar;
            string[] targetRoles = [GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat];

            for (int beat = 1; beat <= beatsPerBar; beat++)
            {
                foreach (decimal fraction in OffbeatFractions)
                {
                    decimal position = beat + fraction;
                    foreach (string role in targetRoles)
                    {
                        yield return new RemovalCandidate
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
