using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Music.Domain;
using Music.Tests;

namespace Music.Writer
{
    /// <summary>
    /// Converts TimedNote lists to a MIDI file (MidiSongDocument).
    /// </summary>
    public static class PhrasesToMidiConverter
    {
        public static MidiSongDocument Convert(
            Dictionary<byte, List<TimedNote>> mergedByInstrument,
            int tempo,
            int timeSignatureNumerator,
            int timeSignatureDenominator,
            short ticksPerQuarterNote = 480)
        {
            if (mergedByInstrument == null)
                throw new ArgumentNullException(nameof(mergedByInstrument));
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
            
            tempoTrack.Events.Add(CreateEndOfTrackEvent());
            midiFile.Chunks.Add(tempoTrack);

            int trackNumber = 1;
            int channelCursor = 0;

            foreach (var kvp in mergedByInstrument)
            {
                byte programNumber = kvp.Key;
                var timedNotes = kvp.Value;

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
                
                var trackChunk = CreateTrackFromTimedNotes(timedNotes, programNumber, trackNumber, channel, isDrumSet);
                midiFile.Chunks.Add(trackChunk);
                trackNumber++;
            }

            return new MidiSongDocument(midiFile);
        }

        private static TrackChunk CreateTrackFromTimedNotes(
            List<TimedNote> timedNotes,
            byte programNumber,
            int trackNumber,
            int channel,
            bool isDrumSet)
        {
            var trackChunk = new TrackChunk();

            var instrumentName = GetInstrumentName(programNumber);
            var trackName = $"{instrumentName} - Track {trackNumber}";
            trackChunk.Events.Add(new SequenceTrackNameEvent(trackName) { DeltaTime = 0 });

            // Only send program change for non-drum tracks
            // Channel 10 (drums) ignores program changes per GM spec
            if (!isDrumSet)
            {
                trackChunk.Events.Add(new ProgramChangeEvent((SevenBitNumber)programNumber)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = 0
                });
            }

            foreach (var timedNote in timedNotes)
            {
                if (timedNote.IsRest)
                    continue;

                trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)timedNote.NoteNumber, (SevenBitNumber)timedNote.Velocity)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = timedNote.Delta
                });

                trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)timedNote.NoteNumber, (SevenBitNumber)0)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = timedNote.Duration
                });
            }

            // Add end-of-track meta event
            trackChunk.Events.Add(CreateEndOfTrackEvent());
            return trackChunk;
        }

        // Reflection helper to instantiate EndOfTrackEvent with internal constructor
        private static MidiEvent CreateEndOfTrackEvent() =>
            (MidiEvent)Activator.CreateInstance(typeof(EndOfTrackEvent), nonPublic: true)!;

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
            if (programNumber == 255)
                return "Drum Set";

            var instruments = MidiInstrument.GetGeneralMidiInstruments();
            var instrument = instruments.FirstOrDefault(i => i.ProgramNumber == programNumber);
            return instrument?.Name ?? "Acoustic Grand Piano";
        }
    }
}