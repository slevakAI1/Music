// AI: purpose=Simplify bass bar to quarter-note roots; removes existing anchors then adds beats 1-4.
// AI: invariants=Requires harmony+groove; skips when beats per bar < 4; uses chord root at each beat.
// AI: deps=BassOperatorHelper root lookup; assumes 4/4 quarter grid.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.DensityAndSubdivision
{
    public sealed class BassReduceToQuarterNotesOperator : OperatorBase
    {
        private const int BaseOctave = 2;

        public override string OperatorId => "BassReduceToQuarterNotes";

        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            if (bar.BeatsPerBar < 4 || bar.TicksPerBeat <= 0)
                yield break;

            int durationTicks = Math.Max(1, bar.TicksPerBeat);

            for (int beat = 1; beat <= 4; beat++)
            {
                decimal beatValue = beat;
                int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, beatValue, BaseOctave);
                if (!rootNote.HasValue)
                    continue;

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: beatValue,
                    score: 1.0,
                    midiNote: rootNote.Value,
                    durationTicks: durationTicks);
            }
        }

        public override IEnumerable<OperatorCandidateRemoval> GenerateRemovals(Bar bar)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                yield break;

            foreach (var beat in bassOnsets)
            {
                yield return new OperatorCandidateRemoval
                {
                    BarNumber = bar.BarNumber,
                    Beat = beat,
                    Role = GrooveRoles.Bass,
                    OperatorId = OperatorId,
                    Reason = "Reduce to quarter notes replaces existing bass anchors"
                };
            }
        }
    }
}
