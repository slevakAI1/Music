// AI: purpose=Anticipate beat-1 bass onset by 0.5 beats; removes original downbeat onset.
// AI: invariants=Skips bar 1; requires harmony+groove; uses beat 1 root as pitch.
// AI: deps=BassOperatorHelper root lookup; GroovePresetDefinition anchor beats.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.RhythmicPlacement
{
    public sealed class BassAnticipateDownbeatOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const decimal AnticipationBeat = 0.5m;

        public override string OperatorId => "BassAnticipateDownbeat";

        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            if (bar.BarNumber <= 1)
                yield break;

            if (bar.TicksPerBeat <= 0)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (!bassOnsets.Contains(1.0m))
                yield break;

            int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, 1.0m, BaseOctave);
            if (!rootNote.HasValue)
                yield break;

            int durationTicks = Math.Max(1, bar.TicksPerBeat / 2);

            yield return CreateCandidate(
                role: GrooveRoles.Bass,
                barNumber: bar.BarNumber,
                beat: AnticipationBeat,
                score: 1.0,
                midiNote: rootNote.Value,
                durationTicks: durationTicks);
        }

        public override IEnumerable<OperatorCandidateRemoval> GenerateRemovals(Bar bar)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            if (bar.BarNumber <= 1)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (!bassOnsets.Contains(1.0m))
                yield break;

            yield return new OperatorCandidateRemoval
            {
                BarNumber = bar.BarNumber,
                Beat = 1.0m,
                Role = GrooveRoles.Bass,
                OperatorId = OperatorId,
                Reason = "Anticipate downbeat shifts beat 1 earlier"
            };
        }
    }
}
