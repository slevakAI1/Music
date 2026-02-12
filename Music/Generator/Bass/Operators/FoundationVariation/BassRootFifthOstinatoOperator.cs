// AI: purpose=Alternate root and fifth for bass anchors within each bar; keeps rhythm, rewrites pitch only.
// AI: invariants=Requires SongContext harmony+groove; skips beats with missing harmony.
// AI: deps=ChordVoicingHelper; fifth is +7 semitones from root; clamp to range [28,52].

using Music.Generator.Bass.Operators;
using Music.Generator.Core;
using Music.Generator.Groove;
using Music;

namespace Music.Generator.Bass.Operators.FoundationVariation
{
    public sealed class BassRootFifthOstinatoOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const int FifthIntervalSemitones = 7;
        private const int MinNote = 28;
        private const int MaxNote = 52;

        public override string OperatorId => "BassRootFifthOstinato";

        public override OperatorFamily OperatorFamily => OperatorFamily.PatternSubstitution;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                yield break;

            var orderedBeats = bassOnsets.OrderBy(b => b).ToList();

            for (int i = 0; i < orderedBeats.Count; i++)
            {
                decimal beat = orderedBeats[i];
                int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, beat, BaseOctave);
                if (!rootNote.HasValue)
                    continue;

                int root = rootNote.Value;
                int targetNote = i % 2 == 0
                    ? root
                    : root + FifthIntervalSemitones;

                targetNote = BassOperatorHelper.ClampToRange(targetNote, MinNote, MaxNote);

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: 1.0,
                    midiNote: targetNote);
            }
        }

    }
}
