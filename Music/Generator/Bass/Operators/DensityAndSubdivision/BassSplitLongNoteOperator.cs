// AI: purpose=Split a long bass onset into repeated 8th-note pulses; removes original onset.
// AI: invariants=Targets first onset with duration >= 2 quarter notes; requires BarTrack for tick math.
// AI: deps=BassOperatorHelper beat math; harmony root used for pitch consistency.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.DensityAndSubdivision
{
    public sealed class BassSplitLongNoteOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const int MinDurationTicks = MusicConstants.TicksPerQuarterNote * 2;
        private const decimal SliceBeats = 0.5m;

        public override string OperatorId => "BassSplitLongNote";

        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            if (bar.TicksPerBeat <= 0)
                yield break;

            var (targetBeat, durationTicks) = FindLongOnset(bar);
            if (!targetBeat.HasValue || durationTicks < MinDurationTicks)
                yield break;

            int sliceTicks = Math.Max(1, bar.TicksPerBeat / 2);
            int maxSlices = durationTicks / sliceTicks;
            if (maxSlices < 2)
                yield break;

            int maxSelectable = Math.Min(4, maxSlices);
            int sliceCount = 2 + (Math.Abs(HashCode.Combine(bar.BarNumber, targetBeat.Value, seed)) % (maxSelectable - 1));

            int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, targetBeat.Value, BaseOctave);
            if (!rootNote.HasValue)
                yield break;

            for (int i = 0; i < sliceCount; i++)
            {
                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: targetBeat.Value + (SliceBeats * i),
                    score: 1.0,
                    midiNote: rootNote.Value,
                    durationTicks: sliceTicks);
            }
        }

        public override IEnumerable<OperatorCandidateRemoval> GenerateRemovals(Bar bar)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var (targetBeat, durationTicks) = FindLongOnset(bar);
            if (!targetBeat.HasValue || durationTicks < MinDurationTicks)
                yield break;

            yield return new OperatorCandidateRemoval
            {
                BarNumber = bar.BarNumber,
                Beat = targetBeat.Value,
                Role = GrooveRoles.Bass,
                OperatorId = OperatorId,
                Reason = "Split long note into 8th-note pulses"
            };
        }

        private (decimal? Beat, int DurationTicks) FindLongOnset(Bar bar)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext?.BarTrack == null)
                return (null, 0);

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                return (null, 0);

            var orderedBeats = bassOnsets.OrderBy(b => b).ToList();

            foreach (decimal beat in orderedBeats)
            {
                decimal? nextBeat = BassOperatorHelper.GetNextOnsetBeat(bar, beat, orderedBeats);
                int durationTicks = BassOperatorHelper.DurationTicksToNextBeat(
                    SongContext.BarTrack,
                    bar.BarNumber,
                    beat,
                    nextBeat);

                if (durationTicks >= MinDurationTicks)
                    return (beat, durationTicks);
            }

            return (null, 0);
        }
    }
}
