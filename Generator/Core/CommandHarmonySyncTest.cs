using Music.MyMidi;
using Music.Writer;

namespace Music.Generator
{
    /// <summary>
    /// Handler(s) for Harmony Sync test commands extracted from HandleRepeatNoteCommand.
    /// </summary>
    public static class CommandHarmonySyncTest
    {
        public static void HandleHarmonySyncTest(
            DataGridView dgSong,
            ref int trackNumber)
        {
            // Extract harmony timeline from the fixed harmony row
            var harmonyRow = dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY];
            var harmonyTimeline = harmonyRow.Cells["colData"].Value as Music.Designer.HarmonyTrack;
            
            if (!ValidateHarmonyTimeline(harmonyTimeline))
                return;

            // Extract time signature timeline to determine beats per measure
            var timeSignatureRow = dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE];
            var timeSignatureTimeline = timeSignatureRow.Cells["colData"].Value as Music.Designer.TimeSignatureTrack;
            
            if (!ValidateTimeSignatureTimeline(timeSignatureTimeline))
                return;

            // Create 4 tracks for the test
            var rockOrganTrack = CreateRockOrganTrack(harmonyTimeline, timeSignatureTimeline);
            var electricGuitarTrack = CreateElectricGuitarTrack(harmonyTimeline, timeSignatureTimeline);
            var electricBassTrack = CreateElectricBassTrack(harmonyTimeline, timeSignatureTimeline);
            var drumSetTrack = CreateDrumSetTrack(harmonyTimeline, timeSignatureTimeline);

            // Add tracks to the grid
            SongGridManager.AddNewTrack(rockOrganTrack, dgSong, ref trackNumber);
            SongGridManager.AddNewTrack(electricGuitarTrack, dgSong, ref trackNumber);
            SongGridManager.AddNewTrack(electricBassTrack, dgSong, ref trackNumber);
            SongGridManager.AddNewTrack(drumSetTrack, dgSong, ref trackNumber);

            ShowSuccessMessage();
        }

        private static PartTrack CreateRockOrganTrack(Music.Designer.HarmonyTrack harmonyTimeline, Music.Designer.TimeSignatureTrack timeSignatureTimeline)
        {
            var notes = new List<PartTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new PartTrack(notes) { MidiProgramNumber = 4 }; // Electric Piano 1

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int halfNoteDuration = ticksPerMeasure / 2; // Two half notes per measure

            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                var chordNotes = ConvertHarmonyEventToSongTrackNoteEvents.Convert(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    baseOctave: 4);

                if (chordNotes == null || chordNotes.Count == 0)
                    continue;

                int measureTick = (harmonyEvent.StartBar - 1) * ticksPerMeasure;
                int beatTick = (harmonyEvent.StartBeat - 1) * ticksPerQuarterNote;
                int absolutePosition = measureTick + beatTick;

                if (absolutePosition > currentTick)
                    currentTick = absolutePosition;

                for (int halfNote = 0; halfNote < 2; halfNote++)
                {
                    foreach (var chordNote in chordNotes)
                    {
                        notes.Add(new PartTrackNoteEvent(
                            noteNumber: chordNote.NoteNumber,
                            absolutePositionTicks: currentTick,
                            noteDurationTicks: halfNoteDuration,
                            noteOnVelocity: 80,
                            isRest: false));
                    }
                    currentTick += halfNoteDuration;
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = 18 };
        }

        private static PartTrack CreateElectricGuitarTrack(Music.Designer.HarmonyTrack harmonyTimeline, Music.Designer.TimeSignatureTrack timeSignatureTimeline)
        {
            var notes = new List<PartTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new PartTrack(notes) { MidiProgramNumber = 27 }; // Electric Guitar (clean)

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int eighthNoteDuration = ticksPerMeasure / 8;

            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                var chordNotes = ConvertHarmonyEventToSongTrackNoteEvents.Convert(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    baseOctave: 4);

                if (chordNotes == null || chordNotes.Count == 0)
                    continue;

                int measureTick = (harmonyEvent.StartBar - 1) * ticksPerMeasure;
                int beatTick = (harmonyEvent.StartBeat - 1) * ticksPerQuarterNote;
                int absolutePosition = measureTick + beatTick;

                if (absolutePosition > currentTick)
                    currentTick = absolutePosition;

                for (int eighthNote = 0; eighthNote < 8; eighthNote++)
                {
                    var chordNote = chordNotes[eighthNote % chordNotes.Count];
                    notes.Add(new PartTrackNoteEvent(
                        noteNumber: chordNote.NoteNumber,
                        absolutePositionTicks: currentTick,
                        noteDurationTicks: eighthNoteDuration,
                        noteOnVelocity: 90,
                        isRest: false));
                    currentTick += eighthNoteDuration;
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = 27 };
        }

        private static PartTrack CreateElectricBassTrack(Music.Designer.HarmonyTrack harmonyTimeline, Music.Designer.TimeSignatureTrack timeSignatureTimeline)
        {
            var notes = new List<PartTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new PartTrack(notes) { MidiProgramNumber = 33 }; // Electric Bass (finger)

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int quarterNoteDuration = ticksPerQuarterNote;

            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                var chordNotes = ConvertHarmonyEventToSongTrackNoteEvents.Convert(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    baseOctave: 2);

                if (chordNotes == null || chordNotes.Count == 0)
                    continue;

                int measureTick = (harmonyEvent.StartBar - 1) * ticksPerMeasure;
                int beatTick = (harmonyEvent.StartBeat - 1) * ticksPerQuarterNote;
                int absolutePosition = measureTick + beatTick;

                if (absolutePosition > currentTick)
                    currentTick = absolutePosition;

                var rootNote = chordNotes[0];
                for (int quarterNote = 0; quarterNote < 4; quarterNote++)
                {
                    notes.Add(new PartTrackNoteEvent(
                        noteNumber: rootNote.NoteNumber,
                        absolutePositionTicks: currentTick,
                        noteDurationTicks: quarterNoteDuration,
                        noteOnVelocity: 95,
                        isRest: false));
                    currentTick += quarterNoteDuration;
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = 33 };
        }

        private static PartTrack CreateDrumSetTrack(Music.Designer.HarmonyTrack harmonyTimeline, Music.Designer.TimeSignatureTrack timeSignatureTimeline)
        {
            var notes = new List<PartTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new PartTrack(notes) { MidiProgramNumber = 255 }; // Drum Set

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

            const int bassDrum = 36;
            const int snareDrum = 38;

            int totalMeasures = harmonyTimeline.Events.Max(e => e.StartBar);

            for (int measure = 0; measure < totalMeasures; measure++)
            {
                int measureStartTick = measure * ticksPerMeasure;

                notes.Add(new PartTrackNoteEvent(
                    noteNumber: bassDrum,
                    absolutePositionTicks: measureStartTick,
                    noteDurationTicks: ticksPerQuarterNote,
                    noteOnVelocity: 100,
                    isRest: false));

                notes.Add(new PartTrackNoteEvent(
                    noteNumber: bassDrum,
                    absolutePositionTicks: measureStartTick + (2 * ticksPerQuarterNote),
                    noteDurationTicks: ticksPerQuarterNote,
                    noteOnVelocity: 100,
                    isRest: false));

                for (int beat = 0; beat < 4; beat++)
                {
                    notes.Add(new PartTrackNoteEvent(
                        noteNumber: snareDrum,
                        absolutePositionTicks: measureStartTick + (beat * ticksPerQuarterNote),
                        noteDurationTicks: ticksPerQuarterNote,
                        noteOnVelocity: 85,
                        isRest: false));
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = 255 };
        }

        #region Message Box Handlers
        private static bool ValidateHarmonyTimeline(Music.Designer.HarmonyTrack harmonyTimeline)
        {
            if (harmonyTimeline == null || harmonyTimeline.Events.Count == 0)
            {
                MessageBoxHelper.Show(
                    "No harmony events defined. Please add harmony events first.",
                    "Missing Harmony",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private static bool ValidateTimeSignatureTimeline(Music.Designer.TimeSignatureTrack timeSignatureTimeline)
        {
            if (timeSignatureTimeline == null || timeSignatureTimeline.Events.Count == 0)
            {
                MessageBoxHelper.Show(
                    "No time signature events defined. Please add at least one time signature event.",
                    "Missing Time Signature",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private static void ShowSuccessMessage()
        {
            MessageBoxHelper.Show(
                "Successfully created 4 synchronized tracks based on harmony timeline.",
                "Harmony Sync Test",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        #endregion
    }
}