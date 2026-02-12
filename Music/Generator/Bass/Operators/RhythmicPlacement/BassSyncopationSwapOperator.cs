// AI: purpose=Swap one strong-beat bass onset (2 or 4) to a nearby offbeat for syncopation.
// AI: invariants=Never targets beat 1; skips when no strong-beat anchor or offbeat occupied.
// AI: deps=BassOperatorHelper root lookup; GroovePresetDefinition anchor beats.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.RhythmicPlacement
{
    public sealed class BassSyncopationSwapOperator : OperatorBase
    {
        private const int BaseOctave = 2;

        public override string OperatorId => "BassSyncopationSwap";

        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            if (bar.TicksPerBeat <= 0)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                yield break;

            decimal? strongBeat = SelectStrongBeat(bassOnsets, bar.BarNumber, seed);
            if (!strongBeat.HasValue)
                yield break;

            decimal offbeat = strongBeat.Value == 2.0m ? 2.5m : 3.5m;
            if (bassOnsets.Contains(offbeat))
                yield break;

            int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, strongBeat.Value, BaseOctave);
            if (!rootNote.HasValue)
                yield break;

            int durationTicks = Math.Max(1, bar.TicksPerBeat / 2);

            yield return CreateCandidate(
                role: GrooveRoles.Bass,
                barNumber: bar.BarNumber,
                beat: offbeat,
                score: 1.0,
                midiNote: rootNote.Value,
                durationTicks: durationTicks);
        }

        public override IEnumerable<OperatorCandidateRemoval> GenerateRemovals(Bar bar)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                yield break;

            decimal? strongBeat = SelectStrongBeat(bassOnsets, bar.BarNumber, bar.BarNumber);
            if (!strongBeat.HasValue)
                yield break;

            yield return new OperatorCandidateRemoval
            {
                BarNumber = bar.BarNumber,
                Beat = strongBeat.Value,
                Role = GrooveRoles.Bass,
                OperatorId = OperatorId,
                Reason = "Syncopation swap shifts strong beat to offbeat"
            };
        }

        private static decimal? SelectStrongBeat(IReadOnlyList<decimal> bassOnsets, int barNumber, int seed)
        {
            ArgumentNullException.ThrowIfNull(bassOnsets);

            var strongBeats = new List<decimal>(2);
            if (bassOnsets.Contains(2.0m))
                strongBeats.Add(2.0m);
            if (bassOnsets.Contains(4.0m))
                strongBeats.Add(4.0m);

            if (strongBeats.Count == 0)
                return null;

            if (strongBeats.Count == 1)
                return strongBeats[0];

            int selector = Math.Abs(HashCode.Combine(barNumber, seed)) % strongBeats.Count;
            return strongBeats[selector];
        }
    }
}
