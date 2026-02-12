// AI: purpose=Shared bass-operator helpers for harmony lookup, beat math, and range clamps.
// AI: invariants=Null inputs throw; missing harmony/groove returns null/empty; beat math uses BarTrack 1-based beats.
// AI: deps=SongContext.HarmonyTrack, GroovePresetDefinition.AnchorLayer, ChordVoicingHelper.

using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators
{
    public static class BassOperatorHelper
    {
        private const string RootVoicing = "root";

        public static int? GetChordRootMidiNote(SongContext songContext, int barNumber, decimal beat, int baseOctave)
        {
            ArgumentNullException.ThrowIfNull(songContext);

            if (songContext.HarmonyTrack == null)
                return null;

            var harmonyEvent = songContext.HarmonyTrack.GetActiveHarmonyEvent(barNumber, beat);
            if (harmonyEvent == null)
                return null;

            var midiNotes = ChordVoicingHelper.GenerateChordMidiNotes(
                harmonyEvent.Key,
                harmonyEvent.Degree,
                harmonyEvent.Quality,
                RootVoicing,
                baseOctave);

            if (midiNotes.Count == 0)
                return null;

            return midiNotes[0];
        }

        public static List<int> GetChordToneMidiNotes(
            SongContext songContext,
            int barNumber,
            decimal beat,
            string voicing,
            int baseOctave)
        {
            ArgumentNullException.ThrowIfNull(songContext);
            if (string.IsNullOrWhiteSpace(voicing))
                throw new ArgumentException("Voicing cannot be null or empty.", nameof(voicing));

            if (songContext.HarmonyTrack == null)
                return new List<int>();

            var harmonyEvent = songContext.HarmonyTrack.GetActiveHarmonyEvent(barNumber, beat);
            if (harmonyEvent == null)
                return new List<int>();

            return ChordVoicingHelper.GenerateChordMidiNotes(
                harmonyEvent.Key,
                harmonyEvent.Degree,
                harmonyEvent.Quality,
                voicing,
                baseOctave);
        }

        public static decimal? GetNextOnsetBeat(Bar bar, decimal currentBeat, IReadOnlyList<decimal> anchorBeats)
        {
            ArgumentNullException.ThrowIfNull(bar);
            ArgumentNullException.ThrowIfNull(anchorBeats);

            decimal? nextBeat = null;
            foreach (var beat in anchorBeats)
            {
                if (beat <= currentBeat)
                    continue;

                if (!nextBeat.HasValue || beat < nextBeat.Value)
                    nextBeat = beat;
            }

            return nextBeat;
        }

        public static int DurationTicksToNextBeat(
            BarTrack barTrack,
            int barNumber,
            decimal currentBeat,
            decimal? nextBeat)
        {
            ArgumentNullException.ThrowIfNull(barTrack);

            long startTick = barTrack.ToTick(barNumber, currentBeat);
            long endTick = nextBeat.HasValue
                ? barTrack.ToTick(barNumber, nextBeat.Value)
                : barTrack.GetBarEndTick(barNumber);

            long delta = endTick - startTick;
            if (delta <= 0)
                return 0;

            return (int)Math.Min(int.MaxValue, delta);
        }

        public static int ClampToRange(int midiNote, int minNote, int maxNote)
        {
            if (minNote > maxNote)
                throw new ArgumentOutOfRangeException(nameof(minNote), "minNote must be <= maxNote.");

            int note = midiNote;
            while (note < minNote)
            {
                note += 12;
            }

            while (note > maxNote)
            {
                note -= 12;
            }

            return note;
        }

        public static bool IsStrongBeat(decimal beat) => beat == 1.0m || beat == 3.0m;

        public static IReadOnlyList<decimal> GetBassAnchorBeats(SongContext songContext, int barNumber)
        {
            ArgumentNullException.ThrowIfNull(songContext);

            if (songContext.GroovePresetDefinition?.AnchorLayer == null)
                return Array.Empty<decimal>();

            var groovePreset = songContext.GroovePresetDefinition.GetActiveGroovePreset(barNumber);
            if (groovePreset?.AnchorLayer == null)
                return Array.Empty<decimal>();

            return groovePreset.AnchorLayer.GetOnsets(GrooveRoles.Bass);
        }
    }
}
