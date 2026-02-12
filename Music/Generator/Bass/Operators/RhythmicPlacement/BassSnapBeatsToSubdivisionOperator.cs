// AI: purpose=Cleanup operator to snap bass beats to a fixed grid after other ops.
// AI: invariants=Only nudges existing beats via candidate updates; never emits beat<1; clamps to bar.BeatsPerBar.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.RhythmicPlacement
{
    public sealed class BassSnapBeatsToSubdivisionOperator : OperatorBase
    {
        private const decimal DefaultGridBeats = 0.25m;

        public override string OperatorId => "BassSnapBeatsToSubdivision";

        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext is null)
                yield break;

            if (SongContext.BarTrack is null)
                yield break;

            var anchorBeats = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (anchorBeats.Count == 0)
                yield break;

            decimal lastBeat = bar.BeatsPerBar;

            foreach (decimal beat in anchorBeats)
            {
                decimal snapped = SnapBeat(beat, DefaultGridBeats);

                if (snapped < 1.0m)
                    snapped = 1.0m;
                if (snapped > lastBeat)
                    snapped = lastBeat;

                if (snapped == beat)
                    continue;

                long startTick = SongContext.BarTrack.ToTick(bar.BarNumber, beat);
                long snappedTick = SongContext.BarTrack.ToTick(bar.BarNumber, snapped);
                long deltaTicksLong = snappedTick - startTick;
                int deltaTicks = (int)Math.Clamp(deltaTicksLong, int.MinValue, int.MaxValue);

                if (deltaTicks == 0)
                    continue;

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: 1.0,
                    timingHint: deltaTicks);
            }
        }

        private static decimal SnapBeat(decimal beat, decimal gridBeats)
        {
            if (gridBeats <= 0)
                return beat;

            decimal snapped = Math.Round(beat / gridBeats, 0, MidpointRounding.AwayFromZero) * gridBeats;
            return snapped == 0.0m ? 1.0m : snapped;
        }
    }
}
