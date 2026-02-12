// AI: purpose=Clamp bass anchor notes into playable range; only yields when pitch changes.
// AI: invariants=Requires groove anchors; clamp range [28,55]; avoid redundant candidates.
// AI: deps=BassOperatorHelper.ClampToRange; assumes anchor beats are valid targets.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.RegisterAndContour
{
    public sealed class BassRangeClampOperator : OperatorBase
    {
        private const int MinRange = 28;
        private const int MaxRange = 55;

        public override string OperatorId => "BassRangeClamp";

        public override OperatorFamily OperatorFamily => OperatorFamily.StyleIdiom;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                yield break;

            foreach (var beat in bassOnsets)
            {
                int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, beat, 2);
                if (!rootNote.HasValue)
                    continue;

                int clamped = BassOperatorHelper.ClampToRange(rootNote.Value, MinRange, MaxRange);
                if (clamped == rootNote.Value)
                    continue;

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: 1.0,
                    midiNote: clamped);
            }
        }
    }
}
