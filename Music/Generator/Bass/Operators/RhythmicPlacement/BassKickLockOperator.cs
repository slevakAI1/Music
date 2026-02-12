// AI: purpose=Add bass onsets aligned to kick beats when bass anchor is missing.
// AI: invariants=Requires GroovePresetDefinition.AnchorLayer; cap additions to avoid >8 onsets.
// AI: deps=BassOperatorHelper root lookup; KickOnsets from AnchorLayer.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.RhythmicPlacement
{
    public sealed class BassKickLockOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const int MaxOnsetsPerBar = 8;

        public override string OperatorId => "BassKickLock";

        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            if (bar.TicksPerBeat <= 0)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            var groovePreset = SongContext.GroovePresetDefinition.GetActiveGroovePreset(bar.BarNumber);
            var kickOnsets = groovePreset?.AnchorLayer?.KickOnsets ?? new List<decimal>();
            if (kickOnsets.Count == 0)
                yield break;

            int currentCount = bassOnsets.Count;
            if (currentCount >= MaxOnsetsPerBar)
                yield break;

            int durationTicks = Math.Max(1, bar.TicksPerBeat / 2);
            int added = 0;

            foreach (var beat in kickOnsets.OrderBy(b => b))
            {
                if (bassOnsets.Contains(beat))
                    continue;

                if (currentCount + added >= MaxOnsetsPerBar)
                    yield break;

                int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, beat, BaseOctave);
                if (!rootNote.HasValue)
                    continue;

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: 1.0,
                    midiNote: rootNote.Value,
                    durationTicks: durationTicks);

                added++;
            }
        }
    }
}
