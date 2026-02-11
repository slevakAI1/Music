// AI: purpose=Rotate chord tones across bass anchor beats; preserves rhythm and durations.
// AI: invariants=Requires HarmonyTrack+GroovePresetDefinition; skips beats with missing harmony/tones.
// AI: deps=ChordVoicingHelper.GenerateChordMidiNotes for tone ordering; no duration changes.

using Music.Generator.Core;
using Music.Generator.Groove;
using Music;

namespace Music.Generator.Bass.Operators.FoundationVariation
{
    public sealed class BassChordTonePulseOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const string BassRoot = "root";

        public override string OperatorId => "BassChordTonePulse";

        public override OperatorFamily OperatorFamily => OperatorFamily.PatternSubstitution;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext?.HarmonyTrack == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var groovePreset = SongContext.GroovePresetDefinition.GetActiveGroovePreset(bar.BarNumber);
            var bassOnsets = groovePreset.AnchorLayer.GetOnsets(GrooveRoles.Bass);
            if (bassOnsets.Count == 0)
                yield break;

            var orderedBeats = bassOnsets.OrderBy(b => b).ToList();

            for (int i = 0; i < orderedBeats.Count; i++)
            {
                decimal beat = orderedBeats[i];
                var harmonyEvent = SongContext.HarmonyTrack.GetActiveHarmonyEvent(bar.BarNumber, beat);
                if (harmonyEvent == null)
                    continue;

                var chordMidiNotes = ChordVoicingHelper.GenerateChordMidiNotes(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    BassRoot,
                    BaseOctave);

                if (chordMidiNotes.Count == 0)
                    continue;

                int noteIndex = i % chordMidiNotes.Count;
                int targetNote = chordMidiNotes[noteIndex];

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
