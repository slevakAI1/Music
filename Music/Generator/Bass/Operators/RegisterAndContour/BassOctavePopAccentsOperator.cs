// AI: purpose=Add octave accents on strong-beat bass onsets; applies only on deterministic bars.
// AI: invariants=Skip if root+12 exceeds max range; targets beats 1/3 when present.
// AI: deps=BassOperatorHelper root lookup; deterministic bar filter to avoid every-bar accents.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.RegisterAndContour
{
    public sealed class BassOctavePopAccentsOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const int MaxRange = 55;

        public override string OperatorId => "BassOctavePopAccents";

        public override OperatorFamily OperatorFamily => OperatorFamily.StyleIdiom;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            if (!ShouldAccentBar(bar.BarNumber))
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                yield break;

            foreach (var beat in bassOnsets)
            {
                if (!BassOperatorHelper.IsStrongBeat(beat))
                    continue;

                int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, beat, BaseOctave);
                if (!rootNote.HasValue)
                    continue;

                int accentNote = rootNote.Value + 12;
                if (accentNote > MaxRange)
                    continue;

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: 1.0,
                    midiNote: accentNote);
            }
        }

        private static bool ShouldAccentBar(int barNumber) => barNumber % 2 == 0;
    }
}
