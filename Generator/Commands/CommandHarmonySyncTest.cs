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
            List<MidiInstrument> midiInstruments,
            ref int phraseNumber,
            Form owner)
        {
            // Extract harmony timeline from the fixed harmony row
            var harmonyRow = dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY];
            var harmonyTimeline = harmonyRow.Cells["colData"].Value as Music.Designer.HarmonyTimeline;
            
            if (harmonyTimeline == null || harmonyTimeline.Events.Count == 0)
            {
                MessageBox.Show(owner,
                    "No harmony events defined. Please add harmony events first.",
                    "Missing Harmony",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Extract time signature timeline to determine beats per measure
            var timeSignatureRow = dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE];
            var timeSignatureTimeline = timeSignatureRow.Cells["colData"].Value as Music.Designer.TimeSignatureTimeline;
            
            if (timeSignatureTimeline == null || timeSignatureTimeline.Events.Count == 0)
            {
                MessageBox.Show(owner,
                    "No time signature events defined. Please add at least one time signature event.",
                    "Missing Time Signature",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Create 4 phrases for the test
            var rockOrganPhrase = CreateRockOrganPhrase(harmonyTimeline, timeSignatureTimeline);
            var electricGuitarPhrase = CreateElectricGuitarPhrase(harmonyTimeline, timeSignatureTimeline);
            var electricBassPhrase = CreateElectricBassPhrase(harmonyTimeline, timeSignatureTimeline);
            var drumSetPhrase = CreateDrumSetPhrase(harmonyTimeline, timeSignatureTimeline);

            // Add phrases to the grid
            SongGridManager.AddPhraseToGrid(rockOrganPhrase, midiInstruments, dgSong, ref phraseNumber);
            SongGridManager.AddPhraseToGrid(electricGuitarPhrase, midiInstruments, dgSong, ref phraseNumber);
            SongGridManager.AddPhraseToGrid(electricBassPhrase, midiInstruments, dgSong, ref phraseNumber);
            SongGridManager.AddPhraseToGrid(drumSetPhrase, midiInstruments, dgSong, ref phraseNumber);

            MessageBox.Show(owner,
                "Successfully created 4 synchronized phrases based on harmony timeline.",
                "Harmony Sync Test",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static SongTrack CreateRockOrganPhrase(Music.Designer.HarmonyTimeline harmonyTimeline, Music.Designer.TimeSignatureTimeline timeSignatureTimeline)
        {
            var notes = new List<SongTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new SongTrack(notes) { MidiProgramNumber = 18 }; // Rock Organ

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int halfNoteDuration = ticksPerMeasure / 2; // Two half notes per measure

            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                var chordNotes = ConvertHarmonyEventToListOfPartNoteEvents.Convert(
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
                        notes.Add(new SongTrackNoteEvent(
                            noteNumber: chordNote.NoteNumber,
                            absolutePositionTicks: currentTick,
                            noteDurationTicks: halfNoteDuration,
                            noteOnVelocity: 80,
                            isRest: false));
                    }
                    currentTick += halfNoteDuration;
                }
            }

            return new SongTrack(notes) { MidiProgramNumber = 18 };
        }

        private static SongTrack CreateElectricGuitarPhrase(Music.Designer.HarmonyTimeline harmonyTimeline, Music.Designer.TimeSignatureTimeline timeSignatureTimeline)
        {
            var notes = new List<SongTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new SongTrack(notes) { MidiProgramNumber = 27 }; // Electric Guitar

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int eighthNoteDuration = ticksPerMeasure / 8;

            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                var chordNotes = ConvertHarmonyEventToListOfPartNoteEvents.Convert(
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
                    notes.Add(new SongTrackNoteEvent(
                        noteNumber: chordNote.NoteNumber,
                        absolutePositionTicks: currentTick,
                        noteDurationTicks: eighthNoteDuration,
                        noteOnVelocity: 90,
                        isRest: false));
                    currentTick += eighthNoteDuration;
                }
            }

            return new SongTrack(notes) { MidiProgramNumber = 27 };
        }

        private static SongTrack CreateElectricBassPhrase(Music.Designer.HarmonyTimeline harmonyTimeline, Music.Designer.TimeSignatureTimeline timeSignatureTimeline)
        {
            var notes = new List<SongTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new SongTrack(notes) { MidiProgramNumber = 33 }; // Electric Bass

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int quarterNoteDuration = ticksPerQuarterNote;

            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                var chordNotes = ConvertHarmonyEventToListOfPartNoteEvents.Convert(
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
                    notes.Add(new SongTrackNoteEvent(
                        noteNumber: rootNote.NoteNumber,
                        absolutePositionTicks: currentTick,
                        noteDurationTicks: quarterNoteDuration,
                        noteOnVelocity: 95,
                        isRest: false));
                    currentTick += quarterNoteDuration;
                }
            }

            return new SongTrack(notes) { MidiProgramNumber = 33 };
        }

        private static SongTrack CreateDrumSetPhrase(Music.Designer.HarmonyTimeline harmonyTimeline, Music.Designer.TimeSignatureTimeline timeSignatureTimeline)
        {
            var notes = new List<SongTrackNoteEvent>();
            int currentTick = 0;

            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new SongTrack(notes) { MidiProgramNumber = 255 }; // Drum Set

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

            const int bassDrum = 36;
            const int snareDrum = 38;

            int totalMeasures = harmonyTimeline.Events.Max(e => e.StartBar);

            for (int measure = 0; measure < totalMeasures; measure++)
            {
                int measureStartTick = measure * ticksPerMeasure;

                notes.Add(new SongTrackNoteEvent(
                    noteNumber: bassDrum,
                    absolutePositionTicks: measureStartTick,
                    noteDurationTicks: ticksPerQuarterNote,
                    noteOnVelocity: 100,
                    isRest: false));

                notes.Add(new SongTrackNoteEvent(
                    noteNumber: bassDrum,
                    absolutePositionTicks: measureStartTick + (2 * ticksPerQuarterNote),
                    noteDurationTicks: ticksPerQuarterNote,
                    noteOnVelocity: 100,
                    isRest: false));

                for (int beat = 0; beat < 4; beat++)
                {
                    notes.Add(new SongTrackNoteEvent(
                        noteNumber: snareDrum,
                        absolutePositionTicks: measureStartTick + (beat * ticksPerQuarterNote),
                        noteDurationTicks: ticksPerQuarterNote,
                        noteOnVelocity: 85,
                        isRest: false));
                }
            }

            return new SongTrack(notes) { MidiProgramNumber = 255 };
        }
    }
}