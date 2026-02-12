// AI: purpose=Add a beat-4.5 pickup into the next bar's chord root; chromatic approach below.
// AI: invariants=Skip last bar; needs SectionTrack.TotalBars and HarmonyTrack for bar+1.
// AI: deps=BassOperatorHelper for root lookup; assumes 4/4 for beat 4.5 placement.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.RhythmicPlacement
{
    public sealed class BassPickupIntoNextBarOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const decimal PickupBeat = 4.5m;

        public override string OperatorId => "BassPickupIntoNextBar";

        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            int totalBars = SongContext.SectionTrack?.TotalBars ?? 0;
            if (totalBars <= 0 || bar.BarNumber >= totalBars)
                yield break;

            if (bar.TicksPerBeat <= 0)
                yield break;

            int nextBar = bar.BarNumber + 1;
            int? targetRoot = BassOperatorHelper.GetChordRootMidiNote(SongContext, nextBar, 1.0m, BaseOctave);
            if (!targetRoot.HasValue)
                yield break;

            int durationTicks = Math.Max(1, bar.TicksPerBeat / 2);
            int pickupNote = Math.Clamp(targetRoot.Value - 1, 0, 127);

            yield return CreateCandidate(
                role: GrooveRoles.Bass,
                barNumber: bar.BarNumber,
                beat: PickupBeat,
                score: 1.0,
                midiNote: pickupNote,
                durationTicks: durationTicks);
        }
    }
}
