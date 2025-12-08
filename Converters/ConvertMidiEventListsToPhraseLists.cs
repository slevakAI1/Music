using Music.Domain;

namespace Music.Writer
{
    /// <summary>
    /// Helper to convert lists of MidiEvent objects to Phrase objects.
    /// </summary>
    internal static class ConvertMidiEventListsToPhraseLists
    {
        /// <summary>
        /// Converts lists of MidiEvent objects to Phrase objects.
        /// Splits tracks by program changes - each program change segment becomes a separate phrase.
        /// </summary>
        /// <param name="midiEventLists">Lists of MidiEvent objects, one per track</param>
        /// <param name="midiInstruments">Available MIDI instruments for name lookup</param>
        /// <param name="sourceTicksPerQuarterNote">The ticks per quarter note from the source MIDI file (default 480)</param>
        public static List<Phrase> ConvertMidiEventListsToPhraseList(
            List<List<MidiEvent>> midiEventLists,
            List<MidiInstrument> midiInstruments,
            short sourceTicksPerQuarterNote)
        {
            var phrases = new List<Phrase>();

            foreach (var midiEventList in midiEventLists)
            {
                // Split track by program changes
                var segmentedEvents = SplitByProgramChanges(midiEventList);

                foreach (var segment in segmentedEvents)
                {
                    var phraseNotes = new List<PhraseNote>();
                    var phrase = new Phrase(phraseNotes);

                    // Get instrument info from this segment's program change
                    var programChangeEvent = segment.Events
                        .FirstOrDefault(e => e.Type == MidiEventType.ProgramChange);

                    // Determine if this is a drum track (channel 10/9)
                    bool isDrumTrack = segment.Events.Any(e => 
                        e.Parameters.TryGetValue("Channel", out var ch) && 
                        Convert.ToInt32(ch) == 9);

                    if (isDrumTrack)
                    {
                        // Drums use channel 10 (index 9) and don't have program changes
                        phrase.MidiProgramNumber = 255; // Sentinel value for drums
                        phrase.MidiProgramName = "Drum Set";
                    }
                    else if (programChangeEvent != null &&
                             programChangeEvent.Parameters.TryGetValue("Program", out var programObj))
                    {
                        int programNumber = Convert.ToInt32(programObj);
                        phrase.MidiProgramNumber = programNumber;

                        var instrument = midiInstruments
                            .FirstOrDefault(i => i.ProgramNumber == programNumber);
                        phrase.MidiProgramName = instrument?.Name ?? $"Program {programNumber}";
                    }
                    else
                    {
                        // No program change found - use default
                        phrase.MidiProgramNumber = 0;
                        phrase.MidiProgramName = "Acoustic Grand Piano";
                    }

                    // Calculate tick scaling factor
                    double tickScale = (double)MusicConstants.TicksPerQuarterNote / sourceTicksPerQuarterNote;

                    // Process note events
                    var noteOnEvents = new Dictionary<int, MidiEvent>();

                    foreach (var midiEvent in segment.Events.OrderBy(e => e.AbsoluteTimeTicks))
                    {
                        if (midiEvent.Type == MidiEventType.NoteOn)
                        {
                            if (!midiEvent.Parameters.TryGetValue("NoteNumber", out var noteNumObj) ||
                                !midiEvent.Parameters.TryGetValue("Velocity", out var velocityObj))
                                continue;

                            int noteNumber = Convert.ToInt32(noteNumObj);
                            int velocity = Convert.ToInt32(velocityObj);

                            if (velocity == 0)
                            {
                                if (noteOnEvents.TryGetValue(noteNumber, out var noteOnEvent))
                                {
                                    CreatePhraseNoteFromPair(noteOnEvent, midiEvent, phraseNotes, tickScale);
                                    noteOnEvents.Remove(noteNumber);
                                }
                            }
                            else
                            {
                                noteOnEvents[noteNumber] = midiEvent;
                            }
                        }
                        else if (midiEvent.Type == MidiEventType.NoteOff)
                        {
                            if (!midiEvent.Parameters.TryGetValue("NoteNumber", out var noteNumObj))
                                continue;

                            int noteNumber = Convert.ToInt32(noteNumObj);

                            if (noteOnEvents.TryGetValue(noteNumber, out var noteOnEvent))
                            {
                                CreatePhraseNoteFromPair(noteOnEvent, midiEvent, phraseNotes, tickScale);
                                noteOnEvents.Remove(noteNumber);
                            }
                        }
                    }

                    // Only add phrase if it has notes
                    if (phraseNotes.Count > 0)
                    {
                        phrases.Add(phrase);
                    }
                }
            }

            return phrases;
        }

        /// <summary>
        /// Splits a list of MIDI events by program changes.
        /// Each segment contains events from one program change to the next.
        /// </summary>
        private static List<EventSegment> SplitByProgramChanges(List<MidiEvent> events)
        {
            var segments = new List<EventSegment>();
            var currentSegment = new EventSegment();

            foreach (var evt in events)
            {
                if (evt.Type == MidiEventType.ProgramChange && currentSegment.Events.Any())
                {
                    // Start new segment when we encounter a program change
                    // (but only if current segment has events)
                    segments.Add(currentSegment);
                    currentSegment = new EventSegment();
                }

                currentSegment.Events.Add(evt);
            }

            // Add final segment
            if (currentSegment.Events.Any())
            {
                segments.Add(currentSegment);
            }

            // If no program changes were found, return single segment with all events
            if (segments.Count == 0)
            {
                segments.Add(new EventSegment { Events = events });
            }

            return segments;
        }

        /// <summary>
        /// Helper class to group events by program change segments
        /// </summary>
        private class EventSegment
        {
            public List<MidiEvent> Events { get; set; } = new List<MidiEvent>();
        }

        /// <summary>
        /// Creates a PhraseNote from a NoteOn/NoteOff event pair.
        /// </summary>
        private static void CreatePhraseNoteFromPair(
            MidiEvent noteOnEvent,
            MidiEvent noteOffEvent,
            List<PhraseNote> phraseNotes,
            double tickScale)
        {
            if (!noteOnEvent.Parameters.TryGetValue("NoteNumber", out var noteNumObj) ||
                !noteOnEvent.Parameters.TryGetValue("Velocity", out var velocityObj))
                return;

            int noteNumber = Convert.ToInt32(noteNumObj);
            int velocity = Convert.ToInt32(velocityObj);
            
            int absolutePositionTicks = (int)Math.Round(noteOnEvent.AbsoluteTimeTicks * tickScale);
            int noteDurationTicks = (int)Math.Round((noteOffEvent.AbsoluteTimeTicks - noteOnEvent.AbsoluteTimeTicks) * tickScale);

            if (noteDurationTicks < 1)
                noteDurationTicks = 1;

            var phraseNote = new PhraseNote(
                noteNumber,
                absolutePositionTicks,
                noteDurationTicks,
                velocity,
                isRest: false);

            phraseNotes.Add(phraseNote);
        }
    }
}