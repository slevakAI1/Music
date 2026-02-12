// AI: purpose=Adds two 16th-note enclosure notes before a target bass onset.
// AI: invariants=Targets beat 1 when present else last anchor; skips if <0.5 beats of space.
// AI: deps=BassOperatorHelper.GetChordRootMidiNote for target pitch; uses chord root.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.HarmonicTargeting
{
    public sealed class BassEnclosureOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const decimal EnclosureSpanBeats = 0.5m;
        private const decimal SixteenthBeat = 0.25m;

        public override string OperatorId => "BassEnclosure";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null)
                yield break;

            if (bar.TicksPerBeat <= 0)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                yield break;

            decimal targetBeat = bassOnsets.Contains(1.0m) ? 1.0m : bassOnsets.Max();
            if (targetBeat - EnclosureSpanBeats < 1.0m)
                yield break;

            int? targetNote = BassOperatorHelper.GetChordRootMidiNote(
                SongContext,
                bar.BarNumber,
                targetBeat,
                BaseOctave);

            if (!targetNote.HasValue)
                yield break;

            int durationTicks = Math.Max(1, bar.TicksPerBeat / 4);
            decimal firstBeat = targetBeat - EnclosureSpanBeats;
            decimal secondBeat = targetBeat - SixteenthBeat;
            int aboveNote = Math.Clamp(targetNote.Value + 1, 0, 127);
            int belowNote = Math.Clamp(targetNote.Value - 1, 0, 127);

            yield return CreateCandidate(
                role: GrooveRoles.Bass,
                barNumber: bar.BarNumber,
                beat: firstBeat,
                score: 1.0,
                midiNote: aboveNote,
                durationTicks: durationTicks);

            yield return CreateCandidate(
                role: GrooveRoles.Bass,
                barNumber: bar.BarNumber,
                beat: secondBeat,
                score: 1.0,
                midiNote: belowNote,
                durationTicks: durationTicks);
        }
    }
}
