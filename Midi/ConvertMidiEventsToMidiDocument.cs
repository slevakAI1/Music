using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Music.Domain;
using Music.Tests;

namespace Music.Writer
{
    /// <summary>
    /// Converts MidiEvent lists to a MIDI file (MidiSongDocument).
    /// Each inner list represents one track in the MIDI file.
    /// Converts absolute time positions to delta times for the MIDI format.
    /// </summary>
    public static class ConvertMidiEventsToMidiDocument
    {
        public static MidiSongDocument Convert(
            List<List<MidiEvent>> midiEventLists,
            int tempo,
            int timeSignatureNumerator,
            int timeSignatureDenominator,
            short ticksPerQuarterNote = 480)
        {
            if (midiEventLists == null)
                throw new ArgumentNullException(nameof(midiEventLists));
            if (tempo <= 0)
                throw new ArgumentException("Tempo must be greater than 0", nameof(tempo));

            // Create MIDI file with specified time division
            var midiFile = new MidiFile
            {
                TimeDivision = new TicksPerQuarterNoteTimeDivision(ticksPerQuarterNote)
            };

            // Convert each MidiEvent list to a track
            foreach (var eventList in midiEventLists)
            {
                var trackChunk = CreateTrackFromMidiEvents(eventList, tempo);
                midiFile.Chunks.Add(trackChunk);
            }

            return new MidiSongDocument(midiFile);
        }

        private static TrackChunk CreateTrackFromMidiEvents(
            List<MidiEvent> midiEvents,
            int tempo)
        {
            var trackChunk = new TrackChunk();
            long lastAbsoluteTime = 0;

            foreach (var midiEvent in midiEvents)
            {
                // Skip EndOfTrack events from input - TrackChunk will add it automatically
                if (midiEvent.Type == Writer.MidiEventType.EndOfTrack)
                    continue;

                // Calculate delta time from last event
                long deltaTime = midiEvent.AbsoluteTimeTicks - lastAbsoluteTime;
                
                // Convert high-level MidiEvent to DryWetMidi event
                var dryWetMidiEvent = ConvertToDryWetMidiEvent(midiEvent, deltaTime, tempo);
                
                if (dryWetMidiEvent != null)
                {
                    trackChunk.Events.Add(dryWetMidiEvent);
                    lastAbsoluteTime = midiEvent.AbsoluteTimeTicks;
                }
            }

            // TrackChunk automatically adds EndOfTrack when the MIDI file is written
            // No need to manually add it

            return trackChunk;
        }

        private static Melanchall.DryWetMidi.Core.MidiEvent? ConvertToDryWetMidiEvent(
            MidiEvent midiEvent,
            long deltaTime,
            int tempo)
        {
            return midiEvent.Type switch
            {
                // Meta Events
                Writer.MidiEventType.Text => new TextEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.CopyrightNotice => new CopyrightNoticeEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.SequenceTrackName => new SequenceTrackNameEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.InstrumentName => new InstrumentNameEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.Lyric => new LyricEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.Marker => new MarkerEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.ProgramName => new ProgramNameEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.EndOfTrack => 
                    // Skip - TrackChunk handles EndOfTrack automatically during write
                    null,
                
                Writer.MidiEventType.SetTempo => CreateSetTempoEvent(midiEvent, deltaTime, tempo),
                
                Writer.MidiEventType.TimeSignature => new TimeSignatureEvent(
                    (byte)GetIntParam(midiEvent, "Numerator"),
                    (byte)Math.Log2(GetIntParam(midiEvent, "Denominator")))
                {
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.KeySignature => new KeySignatureEvent(
                    (sbyte)GetIntParam(midiEvent, "SharpsFlats"),
                    (byte)GetIntParam(midiEvent, "Mode"))
                {
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.SequencerSpecific => new SequencerSpecificEvent(GetBytesParam(midiEvent, "Data"))
                {
                    DeltaTime = deltaTime
                },
                
                // Channel Voice Messages
                Writer.MidiEventType.NoteOff => new NoteOffEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "NoteNumber"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Velocity"))
                {
                    Channel = (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.NoteOn => new NoteOnEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "NoteNumber"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Velocity"))
                {
                    Channel = (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.PolyKeyPressure => new NoteAftertouchEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "NoteNumber"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Pressure"))
                {
                    Channel = (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.ControlChange => new ControlChangeEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "Controller"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Value"))
                {
                    Channel = (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.ProgramChange => new ProgramChangeEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "Program"))
                {
                    Channel = (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.ChannelPressure => new ChannelAftertouchEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "Pressure"))
                {
                    Channel = (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                Writer.MidiEventType.PitchBend => CreatePitchBendEvent(midiEvent, deltaTime),
                
                Writer.MidiEventType.Unknown => null, // Skip unknown events
                
                _ => null
            };
        }

        private static SetTempoEvent CreateSetTempoEvent(MidiEvent midiEvent, long deltaTime, int defaultTempo)
        {
            int microsecondsPerQuarterNote;
            
            if (midiEvent.Parameters.TryGetValue("MicrosecondsPerQuarterNote", out var usParam))
            {
                microsecondsPerQuarterNote = System.Convert.ToInt32(usParam);
            }
            else if (midiEvent.Parameters.TryGetValue("BPM", out var bpmParam))
            {
                int bpm = System.Convert.ToInt32(bpmParam);
                microsecondsPerQuarterNote = 60_000_000 / bpm;
            }
            else
            {
                microsecondsPerQuarterNote = 60_000_000 / defaultTempo;
            }

            return new SetTempoEvent(microsecondsPerQuarterNote)
            {
                DeltaTime = deltaTime
            };
        }

        private static PitchBendEvent CreatePitchBendEvent(MidiEvent midiEvent, long deltaTime)
        {
            int value = GetIntParam(midiEvent, "Value");
            
            // Convert from signed (-8192 to +8191) to unsigned (0 to 16383)
            ushort unsignedValue = (ushort)(value + 8192);
            
            return new PitchBendEvent(unsignedValue)
            {
                Channel = (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                DeltaTime = deltaTime
            };
        }

        // Helper methods to extract parameters safely
        private static string GetStringParam(MidiEvent midiEvent, string key)
        {
            if (midiEvent.Parameters.TryGetValue(key, out var value))
                return value.ToString() ?? string.Empty;
            return string.Empty;
        }

        private static int GetIntParam(MidiEvent midiEvent, string key)
        {
            if (midiEvent.Parameters.TryGetValue(key, out var value))
                return System.Convert.ToInt32(value);
            return 0;
        }

        private static byte[] GetBytesParam(MidiEvent midiEvent, string key)
        {
            if (midiEvent.Parameters.TryGetValue(key, out var value) && value is byte[] bytes)
                return bytes;
            return Array.Empty<byte>();
        }
    }
}