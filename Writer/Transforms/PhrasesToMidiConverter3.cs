using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Music.Domain;
using Music.Tests;

namespace Music.Writer
{
    /// <summary>
    /// Converts set of phrases to a MIDI file (MidiSongDocument).
    /// </summary>
    public static class PhrasesToMidiConverter
    {
        public static MidiSongDocument Convert(
            List<List<MidiEvent> midiEvents,  // Project data types, not DryWetMidi types
            int tempo,
            int timeSignatureNumerator,
            int timeSignatureDenominator,
            short ticksPerQuarterNote = 480)
        {
            if (phrases == null)
                throw new ArgumentNullException(nameof(phrases));
            if (tempo <= 0)
                throw new ArgumentException("Tempo must be greater than 0", nameof(tempo));
            if (timeSignatureNumerator <= 0)
                throw new ArgumentException("Time signature numerator must be greater than 0", nameof(timeSignatureNumerator));
            if (timeSignatureDenominator <= 0)
                throw new ArgumentException("Time signature denominator must be greater than 0", nameof(timeSignatureDenominator));

            // Create MIDI file with specified time division
            var midiFile = new MidiFile
            {
                TimeDivision = new TicksPerQuarterNoteTimeDivision(ticksPerQuarterNote)
            };

            // Global tempo / time signature track (track 0)
            var tempoTrack = new TrackChunk();
            
            // Calculate microseconds per quarter note from BPM
            var microsecondsPerQuarterNote = 60_000_000 / tempo;
            tempoTrack.Events.Add(new SetTempoEvent(microsecondsPerQuarterNote) { DeltaTime = 0 });
            
            // Set time signature
            // The denominator in MIDI is expressed as a power of 2 (e.g., 2 = quarter note, 3 = eighth note)
            byte denominatorPower = (byte)Math.Log2(timeSignatureDenominator);
            tempoTrack.Events.Add(new TimeSignatureEvent(
                (byte)timeSignatureNumerator, 
                denominatorPower) 
            { 
                DeltaTime = 0 
            });
            
            //tempoTrack.Events.Add(CreateEndOfTrackEvent());
            midiFile.Chunks.Add(tempoTrack);

            // Group phrases by instrument
            var phrasesByInstrument = phrases
                .GroupBy(p => p.MidiProgramNumber)
                .ToDictionary(g => g.Key, g => g.ToList());

            int trackNumber = 1;
            int channelCursor = 0;

            foreach (var kvp in phrasesByInstrument)
            {
                byte programNumber = kvp.Key;
                var phrasesForInstrument = kvp.Value;

                // Determine if this is a drum set track
                bool isDrumSet = IsDrumSet(programNumber);
                
                int channel;
                if (isDrumSet)
                {
                    // Drum set always uses channel 10 (zero-based index 9)
                    channel = 9;
                }
                else
                {
                    // Skip channel 9 (drum channel) for melodic instruments
                    if (channelCursor == 9) channelCursor++;
                    channel = channelCursor;
                    channelCursor = (channelCursor + 1) % 16;
                }
                
                var trackChunk = CreateTrackFromPhrases(
                    phrasesForInstrument, 
                    programNumber, 
                    trackNumber, 
                    channel, 
                    isDrumSet,
                    ticksPerQuarterNote);
                midiFile.Chunks.Add(trackChunk);
                trackNumber++;
            }

            return new MidiSongDocument(midiFile);
        }

        private static TrackChunk CreateTrackFromPhrases(
            List<Phrase> phrases,
            byte programNumber,
            int trackNumber,
            int channel,
            bool isDrumSet,
            short ticksPerQuarterNote)
        {
            var trackChunk = new TrackChunk();

            var instrumentName = GetInstrumentName(programNumber);
            var trackName = $"{instrumentName} - Track {trackNumber}";
            trackChunk.Events.Add(new SequenceTrackNameEvent(trackName) { DeltaTime = 0 });

            // Only send program change for non-drum tracks
            // Channel 10 (drums) ignores program changes per GM spec
            if (!isDrumSet)
            {
                trackChunk.Events.Add(new SevenBitNumber((SevenBitNumber)programNumber)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = 0
                });
            }

            // Convert all phrases to timed events and merge them
            var allEvents = new List<(long absoluteTime, bool isNoteOn, byte noteNumber, byte velocity, long duration)>();

            foreach (var phrase in phrases)
            {
                foreach (var phraseNote in phrase.PhraseNotes ?? Enumerable.Empty<PhraseNote>())
                {
                    if (phraseNote.IsRest)
                    {
                        continue;
                    }

                    allEvents.Add((phraseNote.AbsolutePositionTicks, true, (byte)phraseNote.NoteNumber, (byte)phraseNote.NoteOnVelocity, phraseNote.Duration));
                }
            }

            // Sort events by absolute time, then by event type (NoteOn before NoteOff at same time)
            var sortedEvents = allEvents
                .OrderBy(e => e.absoluteTime)
                .ThenBy(e => e.isNoteOn ? 0 : 1)
                .ToList();

            // Convert to delta time and add to track
            long lastAbsoluteTime = 0;
            var pendingNoteOffs = new List<(long absoluteTime, byte noteNumber)>();

            foreach (var evt in sortedEvents)
            {
                // Process any pending note-offs that should happen before or at this time
                var noteOffsToProcess = pendingNoteOffs
                    .Where(n => n.absoluteTime <= evt.absoluteTime)
                    .OrderBy(n => n.absoluteTime)
                    .ToList();

                foreach (var noteOff in noteOffsToProcess)
                {
                    var deltaTime = noteOff.absoluteTime - lastAbsoluteTime;
                    trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)noteOff.noteNumber, (SevenBitNumber)0)
                    {
                        Channel = (FourBitNumber)channel,
                        DeltaTime = deltaTime
                    });
                    lastAbsoluteTime = noteOff.absoluteTime;
                    pendingNoteOffs.Remove(noteOff);
                }

                // Add the current note-on event
                var noteDelta = evt.absoluteTime - lastAbsoluteTime;
                trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)evt.noteNumber, (SevenBitNumber)evt.velocity)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = noteDelta
                });
                lastAbsoluteTime = evt.absoluteTime;

                // Schedule the note-off
                pendingNoteOffs.Add((evt.absoluteTime + evt.duration, evt.noteNumber));
            }

            // Process any remaining note-offs
            foreach (var noteOff in pendingNoteOffs.OrderBy(n => n.absoluteTime))
            {
                var deltaTime = noteOff.absoluteTime - lastAbsoluteTime;
                trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)noteOff.noteNumber, (SevenBitNumber)0)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = deltaTime
                });
                lastAbsoluteTime = noteOff.absoluteTime;
            }

            // Add end-of-track meta event
            // trackChunk.Events.Add(CreateEndOfTrackEvent());
            return trackChunk;
        }

        /// <summary>
        /// Determines if a program number represents a drum set.
        /// </summary>
        private static bool IsDrumSet(byte programNumber)
        {
            // Check if MidiProgramNumber is the sentinel value 255 (from MidiInstrument list)
            return programNumber == 255;
        }

        private static string GetInstrumentName(byte programNumber)
        {
            var instruments = MidiInstrument.GetGeneralMidiInstruments();
            var instrument = instruments.FirstOrDefault(i => i.ProgramNumber == programNumber);
            return instrument?.Name ?? "Acoustic Grand Piano";
        }
    }
}