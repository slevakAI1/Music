// AI: purpose=Add a 16th-note burst on beat 4 alternating root/octave; clamps to bass range.
// AI: invariants=Requires harmony+groove; uses 4.0-4.75 beats; duration is 16th-note ticks.
// AI: deps=BassOperatorHelper root lookup and range clamp; assumes 4/4 16th grid.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.DensityAndSubdivision
{
    public sealed class BassBurstSixteenthsOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const int MinRange = 28;
        private const int MaxRange = 55;
        private static readonly decimal[] BurstBeats = [4.0m, 4.25m, 4.5m, 4.75m];

        public override string OperatorId => "BassBurstSixteenths";

        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            if (bar.TicksPerBeat <= 0)
                yield break;

            int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, 4.0m, BaseOctave);
            if (!rootNote.HasValue)
                yield break;

            int durationTicks = Math.Max(1, bar.TicksPerBeat / 4);

            for (int i = 0; i < BurstBeats.Length; i++)
            {
                int note = i % 2 == 0 ? rootNote.Value : rootNote.Value + 12;
                int clamped = BassOperatorHelper.ClampToRange(note, MinRange, MaxRange);

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: BurstBeats[i],
                    score: 1.0,
                    midiNote: clamped,
                    durationTicks: durationTicks);
            }
        }
    }
}
