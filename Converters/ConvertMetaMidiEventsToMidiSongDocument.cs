using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Music.MyMidi;
using MidiEventType = Music.MyMidi.MidiEventType;

namespace Music.Writer
{
    /// <summary>
    /// Converts MetaMidiEvent lists to a MIDI file (MidiSongDocument).
    /// Each inner list represents one track in the MIDI file.
    /// Converts absolute time positions to delta times for the MIDI format.
    /// </summary>
    public static class ConvertMetaMidiEventsToMidiSongDocument
    {
        public static MidiSongDocument Convert(List<List<PartTrackEvent>> midiEventLists)
        {
            if (midiEventLists == null)
                throw new ArgumentNullException(nameof(midiEventLists));

            // Create MIDI file with specified time division
            var midiFile = new MidiFile
            {
                TimeDivision = new TicksPerQuarterNoteTimeDivision(MusicConstants.TicksPerQuarterNote)
            };

            // Convert each MetaMidiEvent list to a track
            foreach (var eventList in midiEventLists)
            {
                var trackChunk = CreateTrackFromMidiEvents(eventList);
                midiFile.Chunks.Add(trackChunk);
            }

            return new MidiSongDocument(midiFile);
        }

        private static TrackChunk CreateTrackFromMidiEvents(List<PartTrackEvent> midiEvents)
        {
            var trackChunk = new TrackChunk();
            long lastAbsoluteTime = 0;

            // Detect if this is a drum track by checking for program number 255
            bool isDrumTrack = midiEvents.Any(e => 
                e.Type == MidiEventType.ProgramChange && 
                GetIntParam(e, "Program") == 255);

            foreach (var midiEvent in midiEvents)
            {
                // Skip EndOfTrack events from input - TrackChunk will add it automatically
                if (midiEvent.Type == MidiEventType.EndOfTrack)
                    continue;

                // Calculate delta time from last event
                long deltaTime = midiEvent.AbsoluteTimeTicks - lastAbsoluteTime;
                
                // Convert high-level MetaMidiEvent to DryWetMidi event
                var dryWetMidiEvent = ConvertToDryWetMidiEvent(midiEvent, deltaTime, isDrumTrack);
                
                if (dryWetMidiEvent != null)
                {
                    trackChunk.Events.Add(dryWetMidiEvent);
                    lastAbsoluteTime = midiEvent.AbsoluteTimeTicks;
                }
            }

            return trackChunk;
        }

        private static Melanchall.DryWetMidi.Core.MidiEvent? ConvertToDryWetMidiEvent(
            PartTrackEvent midiEvent,
            long deltaTime,
            bool isDrumTrack)
        {
            return midiEvent.Type switch
            {
                // Meta Events
                MidiEventType.Text => new TextEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                MidiEventType.CopyrightNotice => new CopyrightNoticeEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                MidiEventType.SequenceTrackName => new SequenceTrackNameEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                MidiEventType.InstrumentName => new InstrumentNameEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                MidiEventType.Lyric => new LyricEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                MidiEventType.Marker => new MarkerEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                MidiEventType.ProgramName => new ProgramNameEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                MidiEventType.EndOfTrack => 
                    // Skip - TrackChunk handles EndOfTrack automatically during write
                    null,
                
                MidiEventType.SetTempo => CreateSetTempoEvent(midiEvent, deltaTime),
                
                MidiEventType.TimeSignature => CreateTimeSignatureEvent(midiEvent, deltaTime),
                
                MidiEventType.KeySignature => new KeySignatureEvent(
                    (sbyte)GetIntParam(midiEvent, "SharpsFlats"),
                    (byte)GetIntParam(midiEvent, "Mode"))
                {
                    DeltaTime = deltaTime
                },
                
                MidiEventType.SequencerSpecific => new SequencerSpecificEvent(GetBytesParam(midiEvent, "Data"))
                {
                    DeltaTime = deltaTime
                },

                MidiEventType.SmpteOffset => CreateSmpteOffsetEvent(midiEvent, deltaTime),
                
                // Channel Voice Messages
                MidiEventType.NoteOff => new NoteOffEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "NoteNumber"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Velocity"))
                {
                    Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                MidiEventType.NoteOn => new NoteOnEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "NoteNumber"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Velocity"))
                {
                    Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                MidiEventType.PolyKeyPressure => new NoteAftertouchEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "NoteNumber"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Pressure"))
                {
                    Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                MidiEventType.ControlChange => new ControlChangeEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "Controller"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Value"))
                {
                    Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                MidiEventType.ProgramChange => CreateProgramChangeEvent(midiEvent, deltaTime, isDrumTrack),
                
                MidiEventType.ChannelPressure => new ChannelAftertouchEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "Pressure"))
                {
                    Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                MidiEventType.PitchBend => CreatePitchBendEvent(midiEvent, deltaTime, isDrumTrack),
                
                MidiEventType.Unknown => null, // Skip unknown events
                
                _ => null
            };
        }

        private static ProgramChangeEvent? CreateProgramChangeEvent(PartTrackEvent midiEvent, long deltaTime, bool isDrumTrack)
        {
            int programNumber = GetIntParam(midiEvent, "Program");
            
            // Skip program change events for drum tracks (255 sentinel value)
            if (programNumber == 255)
                return null;
            
            return new ProgramChangeEvent((SevenBitNumber)programNumber)
            {
                Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                DeltaTime = deltaTime
            };
        }

        private static SetTempoEvent CreateSetTempoEvent(PartTrackEvent midiEvent, long deltaTime)
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
                // Default to 120 BPM if no tempo specified
                microsecondsPerQuarterNote = 500_000;
            }

            return new SetTempoEvent(microsecondsPerQuarterNote)
            {
                DeltaTime = deltaTime
            };
        } 

        // TO DO - LOW - look at this. Looks like overkill.
        private static TimeSignatureEvent CreateTimeSignatureEvent(PartTrackEvent midiEvent, long deltaTime)
        {
            var numerator = (byte)GetIntParam(midiEvent, "Numerator");

            // The Midi meta-event stored in our MetaMidiEvent.Parameters uses the *actual* denominator (e.g. 4, 8, 16).
            // DryWetMidi's TimeSignatureEvent constructor expects the denominator encoded as the exponent (i.e. 2 -> 4, 3 -> 8).
            // To avoid squaring twice when round-tripping, convert the actual denominator back to the exponent here.
            int denomValue = GetIntParam(midiEvent, "Denominator");
            byte denominatorExponent;

            if (denomValue <= 0)
            {
                // fallback to common default: 4 -> exponent 2
                denominatorExponent = 2;
            }
            else
            {
                // compute exponent such that (1 << exponent) == denomValue
                int exp = 0;
                while ((1 << exp) < denomValue && exp < 8) // limit exponent to reasonable range
                    exp++;

                if ((1 << exp) != denomValue)
                {
                    // If denomValue is not an exact power of two, fall back to nearest exponent
                    exp = (int)Math.Round(Math.Log(denomValue, 2));
                    if (exp < 0) exp = 0;
                    if (exp > 8) exp = 8;
                }

                denominatorExponent = (byte)exp;

            }

            var clocksPerMetronomeClick = midiEvent.Parameters.TryGetValue("ClocksPerMetronomeClick", out var cpm) 
                ? (byte)System.Convert.ToInt32(cpm) : (byte)24;
            var thirtySecondNotesPerQuarter = midiEvent.Parameters.TryGetValue("ThirtySecondNotesPerQuarter", out var tsnpq) 
                ? (byte)System.Convert.ToInt32(tsnpq) : (byte)8;

            return new TimeSignatureEvent(numerator, denominatorExponent, clocksPerMetronomeClick, thirtySecondNotesPerQuarter) 
            { 
                DeltaTime = deltaTime 
            };
        }






        private static PitchBendEvent CreatePitchBendEvent(PartTrackEvent midiEvent, long deltaTime, bool isDrumTrack)
        {
            int value = GetIntParam(midiEvent, "Value");
            
            // Convert from signed (-8192 to +8191) to unsigned (0 to 16383)
            ushort unsignedValue = (ushort)(value + 8192);
            
            return new PitchBendEvent(unsignedValue)
            {
                Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                DeltaTime = deltaTime
            };
        }

        private static SmpteOffsetEvent CreateSmpteOffsetEvent(PartTrackEvent midiEvent, long deltaTime)
        {
            int formatValue = GetIntParam(midiEvent, "Format");
            byte hours = (byte)GetIntParam(midiEvent, "Hours");
            byte minutes = (byte)GetIntParam(midiEvent, "Minutes");
            byte seconds = (byte)GetIntParam(midiEvent, "Seconds");
            byte frames = (byte)GetIntParam(midiEvent, "Frames");
            byte subFrames = (byte)GetIntParam(midiEvent, "SubFrames");

            SmpteFormat format = formatValue switch
            {
                0 => SmpteFormat.TwentyFour,
                1 => SmpteFormat.TwentyFive,
                2 => SmpteFormat.ThirtyDrop,
                3 => SmpteFormat.Thirty,
                _ => SmpteFormat.TwentyFour
            };
            
            return new SmpteOffsetEvent(format, hours, minutes, seconds, frames, subFrames)
            {
                DeltaTime = deltaTime
            };
        }

        // Helper methods to extract parameters safely
        private static string GetStringParam(PartTrackEvent midiEvent, string key)
        {
            if (midiEvent.Parameters.TryGetValue(key, out var value))
                return value.ToString() ?? string.Empty;
            return string.Empty;
        }

        private static int GetIntParam(PartTrackEvent midiEvent, string key)
        {
            if (midiEvent.Parameters.TryGetValue(key, out var value))
                return System.Convert.ToInt32(value);
            return 0;
        }

        private static byte[] GetBytesParam(PartTrackEvent midiEvent, string key)
        {
            if (midiEvent.Parameters.TryGetValue(key, out var value) && value is byte[] bytes)
                return bytes;
            return Array.Empty<byte>();
        }
    }
}