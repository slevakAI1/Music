// AI: purpose=Fill bass anchor onsets with chord-root half notes from HarmonyTrack for current bar.
// AI: deps=SongContext.HarmonyTrack+GroovePresetDefinition; uses ChordVoicingHelper; duration=half note ticks.

using Music.Generator.Core;
using Music.Generator.Groove;
using Music;

namespace Music.Generator.Bass.Operators.MicroAddition
{
    // AI: purpose=Assign chord-root MIDI pitch to each bass anchor onset as half-note durations.
    // AI: invariants=Uses bass anchor beats from active groove preset; skips when harmony is missing.
    public sealed class BassChordRootHalfNoteOperator : OperatorBase
    {
        private const double BaseScore = 0.9;
        private const int BaseOctave = 2;
        private const string BassRoot = "root";
        private const int HalfNoteBeats = 2;

        public override string OperatorId => "BassChordRootHalfNote";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext?.HarmonyTrack == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var groovePreset = SongContext.GroovePresetDefinition.GetActiveGroovePreset(bar.BarNumber);
            var bassOnsets = groovePreset.AnchorLayer.GetOnsets(GrooveRoles.Bass);
            if (bassOnsets.Count == 0)
                yield break;

            foreach (var beat in bassOnsets)
            {
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

                int durationTicks = MusicConstants.TicksPerQuarterNote * HalfNoteBeats;

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: BaseScore,
                    midiNote: chordMidiNotes[0],
                    durationTicks: durationTicks);
            }
        }
    }
}
