// AI: purpose=Remove one weak-beat bass onset to create space; never remove beat 1.
// AI: invariants=Removal-only; skips bars with <=2 onsets; targets 2/4/offbeats.
// AI: deps=BassOperatorHelper beat strength; GroovePresetDefinition anchor beats.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.RhythmicPlacement
{
    public sealed class BassRestStrategicSpaceOperator : OperatorBase
    {
        public override string OperatorId => "BassRestStrategicSpace";

        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);
            return Array.Empty<OperatorCandidateAddition>();
        }

        public override IEnumerable<OperatorCandidateRemoval> GenerateRemovals(Bar bar)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count <= 2)
                yield break;

            var candidates = bassOnsets
                .Where(beat => beat != 1.0m && !BassOperatorHelper.IsStrongBeat(beat))
                .OrderBy(beat => beat)
                .ToList();

            if (candidates.Count == 0)
                yield break;

            int index = Math.Abs(HashCode.Combine(bar.BarNumber)) % candidates.Count;
            decimal targetBeat = candidates[index];

            yield return new OperatorCandidateRemoval
            {
                BarNumber = bar.BarNumber,
                Beat = targetBeat,
                Role = GrooveRoles.Bass,
                OperatorId = OperatorId,
                Reason = "Strategic rest removes a weak-beat onset"
            };
        }
    }
}
