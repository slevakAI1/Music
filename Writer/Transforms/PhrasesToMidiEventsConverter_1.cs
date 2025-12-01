using Music.Domain;

namespace Music.Writer
{
    /// <summary>
    /// Converts Phrase objects to lists of MidiEvent objects with absolute time positioning.
    /// This is stage 1 processing - creates NoteOn, NoteOff, and SequenceTrackName events only.
    /// Channel assignment and other processing happens in later stages.
    /// </summary>
    public static class PhrasesToMidiEventsConverter_Phase_1
    {
        private const short DefaultTicksPerQuarterNote = 480;

        /// <summary>
        /// Converts a list of phrases to lists of MIDI events (one list per phrase).
        /// Each phrase is processed independently with its own event list.
        /// </summary>
        /// <param name="phrases">List of phrases to convert</param>
        /// <param name="tempo">Tempo in beats per minute (not used for timing calculations but kept for context)</param>
        /// <param name="timeSignatureNumerator">Time signature numerator (not used for timing but kept for context)</param>
        /// <param name="timeSignatureDenominator">Time signature denominator (not used for timing but kept for context)</param>
        /// <param name="ticksPerQuarterNote">MIDI time resolution (default 480 ticks per quarter note)</param>
        /// <returns>List of MidiEvent lists, one per input phrase</returns>
        public static List<List<MidiEvent>> Convert(
            List<Phrase> phrases,
            int tempo,
            int timeSignatureNumerator,
            int timeSignatureDenominator,
            short ticksPerQuarterNote = DefaultTicksPerQuarterNote)
        {
            if (phrases == null)
                throw new ArgumentNullException(nameof(phrases));
            if (tempo <= 0)
                throw new ArgumentException("Tempo must be greater than 0", nameof(tempo));
            if (timeSignatureNumerator <= 0)
                throw new ArgumentException("Time signature numerator must be greater than 0", nameof(timeSignatureNumerator));
            if (timeSignatureDenominator <= 0)
                throw new ArgumentException("Time signature denominator must be greater than 0", nameof(timeSignatureDenominator));

            var result = new List<List<MidiEvent>>();

            foreach (var phrase in phrases)
            {
                result.Add(ConvertSinglePhrase(phrase, ticksPerQuarterNote));
            }

            return result;
        }

        /// <summary>
        /// Converts a single phrase to a list of MIDI events with absolute time positioning.
        /// </summary>
        private static List<MidiEvent> ConvertSinglePhrase(Phrase phrase, short ticksPerQuarterNote)
        {
            var events = new List<MidiEvent>();
            long absoluteTime = 0;

            // Add track name event at the beginning (using instrument name)
            var trackName = string.IsNullOrWhiteSpace(phrase.MidiPartName) 
                ? "Unnamed Track" 
                : phrase.MidiPartName;
            events.Add(MidiEvent.TrackName(0, trackName));

            // Add program change event at the beginning to set the instrument
            // Channel is null and will be assigned in Phase 2
            events.Add(new MidiEvent
            {
                Type = MidiEventType.ProgramChange,
                AbsoluteTimeTicks = 0,
                Channel = null,
                ProgramNumber = phrase.MidiProgramNumber
            });

            // Process each note event in the phrase
            foreach (var noteEvent in phrase.NoteEvents ?? Enumerable.Empty<NoteEvent>())
            {
                if (noteEvent.IsRest)
                {
                    // Rests advance time but don't create MIDI events
                    var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
                    absoluteTime += duration;
                    continue;
                }

                // Check if this is a chord that needs to be expanded
                if (ShouldExpandChord(noteEvent))
                {
                    ProcessChord(events, noteEvent, ref absoluteTime, ticksPerQuarterNote);
                }
                else
                {
                    ProcessSingleNote(events, noteEvent, ref absoluteTime, ticksPerQuarterNote);
                }
            }

            return events;
        }

        /// <summary>
        /// Determines if a note event represents a chord that needs expansion.
        /// </summary>
        private static bool ShouldExpandChord(NoteEvent noteEvent)
        {
            return !string.IsNullOrWhiteSpace(noteEvent.ChordKey) &&
                   noteEvent.ChordDegree.HasValue &&
                   !string.IsNullOrWhiteSpace(noteEvent.ChordQuality) &&
                   !string.IsNullOrWhiteSpace(noteEvent.ChordBase);
        }

        /// <summary>
        /// Processes a chord note event by expanding it to individual notes.
        /// </summary>
        private static void ProcessChord(
            List<MidiEvent> events,
            NoteEvent noteEvent,
            ref long absoluteTime,
            short ticksPerQuarterNote)
        {
            // Use ChordConverter to generate individual chord notes
            var chordNotes = ChordConverter.Convert(
                noteEvent.ChordKey!,
                noteEvent.ChordDegree!.Value,
                noteEvent.ChordQuality!,
                noteEvent.ChordBase!,
                baseOctave: noteEvent.Octave,
                noteValue: noteEvent.Duration);

            // Apply dots and tuplet settings to all chord notes
            foreach (var cn in chordNotes)
            {
                cn.Dots = noteEvent.Dots;
                if (!string.IsNullOrWhiteSpace(noteEvent.TupletNumber))
                {
                    cn.TupletNumber = noteEvent.TupletNumber;
                    cn.TupletActualNotes = noteEvent.TupletActualNotes;
                    cn.TupletNormalNotes = noteEvent.TupletNormalNotes;
                }
            }

            // Calculate chord duration once
            var chordDuration = CalculateDuration(chordNotes[0], ticksPerQuarterNote);

            // Create NoteOn events for all chord notes at the same absolute time
            foreach (var cn in chordNotes)
            {
                var noteNumber = CalculateMidiNoteNumber(cn.Step, cn.Alter, cn.Octave);
                events.Add(new MidiEvent
                {
                    Type = MidiEventType.NoteOn,
                    AbsoluteTimeTicks = absoluteTime,
                    Channel = null, // Channel assignment happens in later stage
                    NoteNumber = noteNumber,
                    Velocity = 100 // Default velocity
                });
            }

            // Create NoteOff events for all chord notes at the same end time
            long noteOffTime = absoluteTime + chordDuration;
            foreach (var cn in chordNotes)
            {
                var noteNumber = CalculateMidiNoteNumber(cn.Step, cn.Alter, cn.Octave);
                events.Add(new MidiEvent
                {
                    Type = MidiEventType.NoteOff,
                    AbsoluteTimeTicks = noteOffTime,
                    Channel = null, // Channel assignment happens in later stage
                    NoteNumber = noteNumber,
                    ReleaseVelocity = 0
                });
            }

            // Advance absolute time by the chord duration
            absoluteTime += chordDuration;
        }

        /// <summary>
        /// Processes a single note event.
        /// </summary>
        private static void ProcessSingleNote(
            List<MidiEvent> events,
            NoteEvent noteEvent,
            ref long absoluteTime,
            short ticksPerQuarterNote)
        {
            var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
            var noteNumber = CalculateMidiNoteNumber(noteEvent.Step, noteEvent.Alter, noteEvent.Octave);

            // Create NoteOn event at current absolute time
            events.Add(new MidiEvent
            {
                Type = MidiEventType.NoteOn,
                AbsoluteTimeTicks = absoluteTime,
                Channel = null, // Channel assignment happens in later stage
                NoteNumber = noteNumber,
                Velocity = 100 // Default velocity
            });

            // Create NoteOff event at note end time
            long noteOffTime = absoluteTime + duration;
            events.Add(new MidiEvent
            {
                Type = MidiEventType.NoteOff,
                AbsoluteTimeTicks = noteOffTime,
                Channel = null, // Channel assignment happens in later stage
                NoteNumber = noteNumber,
                ReleaseVelocity = 0
            });

            // Advance absolute time by the note duration
            absoluteTime += duration;
        }

        /// <summary>
        /// Calculates MIDI note number from note properties.
        /// </summary>
        private static int CalculateMidiNoteNumber(char step, int alter, int octave)
        {
            var baseNote = char.ToUpper(step) switch
            {
                'C' => 0,
                'D' => 2,
                'E' => 4,
                'F' => 5,
                'G' => 7,
                'A' => 9,
                'B' => 11,
                _ => 0
            };
            return (octave + 1) * 12 + baseNote + alter;
        }

        /// <summary>
        /// Calculates duration in ticks for a note event.
        /// </summary>
        private static long CalculateDuration(NoteEvent noteEvent, short ticksPerQuarterNote)
        {
            // Base duration: quarter note = ticksPerQuarterNote
            // Formula: (ticksPerQuarterNote * 4) / noteValue
            // Example: whole note (1) = 480 * 4 / 1 = 1920 ticks
            //          quarter note (4) = 480 * 4 / 4 = 480 ticks
            //          eighth note (8) = 480 * 4 / 8 = 240 ticks
            var baseDuration = (ticksPerQuarterNote * 4.0) / noteEvent.Duration;

            // Apply dots (each dot adds half of the previous value)
            var dottedMultiplier = 1.0;
            var dotValue = 0.5;
            for (int i = 0; i < noteEvent.Dots; i++)
            {
                dottedMultiplier += dotValue;
                dotValue /= 2;
            }
            baseDuration *= dottedMultiplier;

            // Apply tuplet ratio
            if (noteEvent.TupletActualNotes > 0 && noteEvent.TupletNormalNotes > 0)
            {
                baseDuration *= (double)noteEvent.TupletNormalNotes / noteEvent.TupletActualNotes;
            }

            return (long)Math.Round(baseDuration);
        }
    }
}
