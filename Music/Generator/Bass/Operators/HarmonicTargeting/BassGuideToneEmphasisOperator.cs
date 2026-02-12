// AI: purpose=Emphasize guide tones on weak beats while keeping roots on strong beats.
// AI: invariants=Requires SongContext+GroovePresetDefinition; skips beats with missing harmony.
// AI: deps=BassOperatorHelper chord tone helpers; weak beat uses 3rd/7th, fallback=5th.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.HarmonicTargeting
{
    public sealed class BassGuideToneEmphasisOperator : OperatorBase
    {
        private const int BaseOctave = 2;

        public override string OperatorId => "BassGuideToneEmphasis";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                yield break;

            foreach (var beat in bassOnsets.OrderBy(b => b))
            {
                int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, beat, BaseOctave);
                if (!rootNote.HasValue)
                    continue;

                int targetNote = rootNote.Value;
                if (!BassOperatorHelper.IsStrongBeat(beat))
                {
                    var chordTones = BassOperatorHelper.GetChordToneMidiNotes(
                        SongContext,
                        bar.BarNumber,
                        beat,
                        "root",
                        BaseOctave);

                    targetNote = SelectGuideTone(chordTones, rootNote.Value, bar.BarNumber, beat, seed);
                }

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: 1.0,
                    midiNote: targetNote);
            }
        }

        private static int SelectGuideTone(
            IReadOnlyList<int> chordTones,
            int rootNote,
            int barNumber,
            decimal beat,
            int seed)
        {
            if (chordTones == null || chordTones.Count == 0)
                return rootNote;

            if (chordTones.Count >= 4)
            {
                int selector = Math.Abs(HashCode.Combine(barNumber, beat, seed)) % 2;
                return selector == 0 ? chordTones[1] : chordTones[3];
            }

            if (chordTones.Count >= 3)
                return chordTones[2];

            return chordTones[^1];
        }
    }
}
