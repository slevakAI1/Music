using Music.MyMidi;
using static System.Windows.Forms.DataFormats;

namespace Music.Writer
{
    // Command execution logic for WriterForm
    public partial class WriterForm
    {
        // ========== COMMAND EXECUTION ==========

        /// <summary>
        /// Adds repeating notes to the phrases selected in the grid
        /// </summary>
        public void HandleRepeatNote(WriterFormData formData)
        {
            // Validate that phrases are selected before executing
            if (!ValidatePhrasesSelected())
                return;

            var (noteNumber, noteDurationTicks, repeatCount, isRest) =
                MusicCalculations.GetRepeatingNotesParameters(formData);

            var phrase = CreateRepeatingNotes.Execute(
                noteNumber: noteNumber,
                noteDurationTicks: noteDurationTicks,
                repeatCount: repeatCount,
                noteOnVelocity: 100,
                isRest: isRest);

            // Append the phrase notes to all selected rows
            AppendPhraseNotesToSelectedRows(phrase);
        }

        public void HandleHarmonySyncTest(WriterFormData formData)
        {
            // Extract harmony timeline from the fixed harmony row
            var harmonyRow = dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY];
            var harmonyTimeline = harmonyRow.Cells["colData"].Value as Music.Designer.HarmonyTimeline;
            
            if (harmonyTimeline == null || harmonyTimeline.Events.Count == 0)
            {
                MessageBox.Show(this,
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
                MessageBox.Show(this,
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
            SongGridManager.AddPhraseToGrid(rockOrganPhrase, _midiInstruments, dgSong, ref phraseNumber);
            SongGridManager.AddPhraseToGrid(electricGuitarPhrase, _midiInstruments, dgSong, ref phraseNumber);
            SongGridManager.AddPhraseToGrid(electricBassPhrase, _midiInstruments, dgSong, ref phraseNumber);
            SongGridManager.AddPhraseToGrid(drumSetPhrase, _midiInstruments, dgSong, ref phraseNumber);

            MessageBox.Show(this,
                "Successfully created 4 synchronized phrases based on harmony timeline.",
                "Harmony Sync Test",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>
        /// Creates a Rock Organ phrase with two half notes per measure based on the harmony timeline.
        /// </summary>
        private Phrase CreateRockOrganPhrase(Music.Designer.HarmonyTimeline harmonyTimeline, Music.Designer.TimeSignatureTimeline timeSignatureTimeline)
        {
            var notes = new List<PhraseNote>();
            int currentTick = 0;

            // Get the active time signature for calculating durations
            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new Phrase(notes) { MidiProgramNumber = 18 }; // Rock Organ

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int halfNoteDuration = ticksPerMeasure / 2; // Two half notes per measure

            // Process each harmony event
            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                // Convert the harmony event to chord notes
                var chordNotes = ConvertHarmonicEventToListOfPhraseNotes.Convert(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    baseOctave: 4);

                if (chordNotes == null || chordNotes.Count == 0)
                    continue;

                // Calculate the absolute position for this harmony event
                int measureTick = (harmonyEvent.StartBar - 1) * ticksPerMeasure;
                int beatTick = (harmonyEvent.StartBeat - 1) * ticksPerQuarterNote;
                int absolutePosition = measureTick + beatTick;

                // Advance currentTick if needed
                if (absolutePosition > currentTick)
                    currentTick = absolutePosition;

                // Add two half notes (chord voicing) for this measure
                for (int halfNote = 0; halfNote < 2; halfNote++)
                {
                    foreach (var chordNote in chordNotes)
                    {
                        notes.Add(new PhraseNote(
                            noteNumber: chordNote.NoteNumber,
                            absolutePositionTicks: currentTick,
                            noteDurationTicks: halfNoteDuration,
                            noteOnVelocity: 80,
                            isRest: false));
                    }
                    currentTick += halfNoteDuration;
                }
            }

            return new Phrase(notes) { MidiProgramNumber = 18 }; // Rock Organ (MIDI program 18)
        }

        /// <summary>
        /// Creates an Electric Guitar phrase with 8 eighth notes per measure based on the harmony timeline.
        /// </summary>
        private Phrase CreateElectricGuitarPhrase(Music.Designer.HarmonyTimeline harmonyTimeline, Music.Designer.TimeSignatureTimeline timeSignatureTimeline)
        {
            var notes = new List<PhraseNote>();
            int currentTick = 0;

            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new Phrase(notes) { MidiProgramNumber = 27 }; // Electric Guitar (clean)

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int eighthNoteDuration = ticksPerMeasure / 8; // 8 eighth notes per measure

            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                var chordNotes = ConvertHarmonicEventToListOfPhraseNotes.Convert(
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

                // Add 8 eighth notes (arpeggiating through chord tones)
                for (int eighthNote = 0; eighthNote < 8; eighthNote++)
                {
                    var chordNote = chordNotes[eighthNote % chordNotes.Count];
                    notes.Add(new PhraseNote(
                        noteNumber: chordNote.NoteNumber,
                        absolutePositionTicks: currentTick,
                        noteDurationTicks: eighthNoteDuration,
                        noteOnVelocity: 90,
                        isRest: false));
                    currentTick += eighthNoteDuration;
                }
            }

            return new Phrase(notes) { MidiProgramNumber = 27 }; // Electric Guitar (clean)
        }

        /// <summary>
        /// Creates an Electric Bass phrase with 4 quarter notes per measure based on the harmony timeline.
        /// </summary>
        private Phrase CreateElectricBassPhrase(Music.Designer.HarmonyTimeline harmonyTimeline, Music.Designer.TimeSignatureTimeline timeSignatureTimeline)
        {
            var notes = new List<PhraseNote>();
            int currentTick = 0;

            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new Phrase(notes) { MidiProgramNumber = 33 }; // Electric Bass (finger)

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;
            int quarterNoteDuration = ticksPerQuarterNote;

            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                var chordNotes = ConvertHarmonicEventToListOfPhraseNotes.Convert(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    baseOctave: 2); // Bass plays in lower octave

                if (chordNotes == null || chordNotes.Count == 0)
                    continue;

                int measureTick = (harmonyEvent.StartBar - 1) * ticksPerMeasure;
                int beatTick = (harmonyEvent.StartBeat - 1) * ticksPerQuarterNote;
                int absolutePosition = measureTick + beatTick;

                if (absolutePosition > currentTick)
                    currentTick = absolutePosition;

                // Add 4 quarter notes (root note of the chord)
                var rootNote = chordNotes[0]; // Use the root note for bass line
                for (int quarterNote = 0; quarterNote < 4; quarterNote++)
                {
                    notes.Add(new PhraseNote(
                        noteNumber: rootNote.NoteNumber,
                        absolutePositionTicks: currentTick,
                        noteDurationTicks: quarterNoteDuration,
                        noteOnVelocity: 95,
                        isRest: false));
                    currentTick += quarterNoteDuration;
                }
            }

            return new Phrase(notes) { MidiProgramNumber = 33 }; // Electric Bass (finger)
        }

        /// <summary>
        /// Creates a Drum Set phrase with bass drum on beats 1 and 3, and snare on every beat.
        /// </summary>
        private Phrase CreateDrumSetPhrase(Music.Designer.HarmonyTimeline harmonyTimeline, Music.Designer.TimeSignatureTimeline timeSignatureTimeline)
        {
            var notes = new List<PhraseNote>();
            int currentTick = 0;

            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return new Phrase(notes) { MidiProgramNumber = 255 }; // Drum Set (channel 10)

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

            // MIDI drum note numbers (General MIDI percussion map)
            const int bassDrum = 36;  // Bass Drum 1
            const int snareDrum = 38; // Snare Drum 1

            // Calculate total measures from harmony events
            int totalMeasures = harmonyTimeline.Events.Max(e => e.StartBar);

            // Generate drum pattern for each measure
            for (int measure = 0; measure < totalMeasures; measure++)
            {
                int measureStartTick = measure * ticksPerMeasure;

                // Add bass drum on beats 1 and 3
                notes.Add(new PhraseNote(
                    noteNumber: bassDrum,
                    absolutePositionTicks: measureStartTick,
                    noteDurationTicks: ticksPerQuarterNote,
                    noteOnVelocity: 100,
                    isRest: false));

                notes.Add(new PhraseNote(
                    noteNumber: bassDrum,
                    absolutePositionTicks: measureStartTick + (2 * ticksPerQuarterNote),
                    noteDurationTicks: ticksPerQuarterNote,
                    noteOnVelocity: 100,
                    isRest: false));

                // Add snare drum on every beat (1, 2, 3, 4)
                for (int beat = 0; beat < 4; beat++)
                {
                    notes.Add(new PhraseNote(
                        noteNumber: snareDrum,
                        absolutePositionTicks: measureStartTick + (beat * ticksPerQuarterNote),
                        noteDurationTicks: ticksPerQuarterNote,
                        noteOnVelocity: 85,
                        isRest: false));
                }
            }

            return new Phrase(notes) { MidiProgramNumber = 255 }; // Drum Set (255 indicates percussion/channel 10)
        }
    }
}