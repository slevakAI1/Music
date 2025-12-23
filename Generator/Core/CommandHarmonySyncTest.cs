using Music.Writer;

namespace Music.Generator
{
    /// <summary>
    /// Handler(s) for Harmony Sync test commands extracted from HandleRepeatNoteCommand.
    /// </summary>
    public static class CommandHarmonySyncTest
    {
        public static void HandleHarmonySyncTest(DataGridView dgSong)
        {
            // Extract harmony track from the fixed harmony row
            var harmonyRow = dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY];
            var harmonyTrack = harmonyRow.Cells["colData"].Value as HarmonyTrack;
            
            if (!ValidateHarmonyTrack(harmonyTrack))
                return;

            // Extract time signature track to determine beats per measure
            var timeSignatureRow = dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE];
            var timeSignatureTrack = timeSignatureRow.Cells["colData"].Value as Designer.TimeSignatureTrack;
            
            if (!ValidateTimeSignatureTrack(timeSignatureTrack))
                return;

            // Create 4 tracks for the test
            var rockOrganTrack = CreateRockOrganTrack(harmonyTrack, timeSignatureTrack);
            var electricGuitarTrack = CreateElectricGuitarTrack(harmonyTrack, timeSignatureTrack);
            var electricBassTrack = CreateElectricBassTrack(harmonyTrack, timeSignatureTrack);
            var drumSetTrack = CreateDrumSetTrack(harmonyTrack, timeSignatureTrack);

            // Add tracks to the grid - no need to track trackNumber anymore
            SongGridManager.AddNewTrack(rockOrganTrack, dgSong);
            SongGridManager.AddNewTrack(electricGuitarTrack, dgSong);
            SongGridManager.AddNewTrack(electricBassTrack, dgSong);
            SongGridManager.AddNewTrack(drumSetTrack, dgSong);

            ShowSuccessMessage();
        }

        private static PartTrack CreateRockOrganTrack(HarmonyTrack harmonyTrack, Designer.TimeSignatureTrack timeSignatureTrack)
        {
            var notes = new List<PartTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTrack.Events.FirstOrDefault();
            if (timeSignature == null)
                return new PartTrack(notes) { MidiProgramNumber = 4 }; // Electric Piano 1

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int halfNoteDuration = ticksPerMeasure / 2; // Two half notes per measure

            foreach (var harmonyEvent in harmonyTrack.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
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

        private static PartTrack CreateElectricGuitarTrack(HarmonyTrack harmonyTimeTrack, Designer.TimeSignatureTrack timeSignatureTrack)
        {
            var notes = new List<PartTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTrack.Events.FirstOrDefault();
            if (timeSignature == null)
                return new PartTrack(notes) { MidiProgramNumber = 27 }; // Electric Guitar (clean)

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int eighthNoteDuration = ticksPerMeasure / 8;

            foreach (var harmonyEvent in harmonyTimeTrack.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
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

        private static PartTrack CreateElectricBassTrack(HarmonyTrack harmonyTrack, Designer.TimeSignatureTrack timeSignatureTrack)
        {
            var notes = new List<PartTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTrack.Events.FirstOrDefault();
            if (timeSignature == null)
                return new PartTrack(notes) { MidiProgramNumber = 33 }; // Electric Bass (finger)

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int quarterNoteDuration = ticksPerQuarterNote;

            foreach (var harmonyEvent in harmonyTrack.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
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

        private static PartTrack CreateDrumSetTrack(HarmonyTrack harmonyTrack, Designer.TimeSignatureTrack timeSignatureTrack)
        {
            var notes = new List<PartTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTrack.Events.FirstOrDefault();
            if (timeSignature == null)
                return new PartTrack(notes) { MidiProgramNumber = 255 }; // Drum Set

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

            const int bassDrum = 36;
            const int snareDrum = 38;

            int totalMeasures = harmonyTrack.Events.Max(e => e.StartBar);

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
        private static bool ValidateHarmonyTrack(HarmonyTrack harmonyTrack)
        {
            if (harmonyTrack == null || harmonyTrack.Events.Count == 0)
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

        private static bool ValidateTimeSignatureTrack(Music.Designer.TimeSignatureTrack timeSignatureTrack)
        {
            if (timeSignatureTrack == null || timeSignatureTrack.Events.Count == 0)
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
                "Successfully created 4 synchronized tracks based on harmony track.",
                "Harmony Sync Test",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        #endregion
    }
}