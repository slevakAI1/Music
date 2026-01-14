// AI: purpose=stage3: build MidiSongDocument from PartTrack lists; convert absolute AbsoluteTimeTicks->delta times for DryWetMidi
// AI: invariants=input events must be pre-sorted by AbsoluteTimeTicks; TimeDivision uses MusicConstants.TicksPerQuarterNote
// AI: deps=Melanchall.DryWetMidi.Core; PartTrackEvent.Parameters keys expected: Channel, Program, NoteNumber, Velocity, BPM
// AI: errors=unsupported/unknown PartTrackEvent types are skipped (return null); do not convert skip->throw
// AI: perf=hotpath O(n) over events; preserve single-pass delta calculation and minimal allocations
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Music.MyMidi;
using PartTrackEventType = Music.Generator.PartTrackEventType;

namespace Music.Writer
{
    public static class ConvertPartTracksToMidiSongDocument_Step_3
    {
        // AI: Convert: null-check input; creates MidiFile.TimeDivision from MusicConstants; one TrackChunk per PartTrack
        public static MidiSongDocument Convert(List<Generator.PartTrack> partTracks)
        {
            if (partTracks == null)
                throw new ArgumentNullException(nameof(partTracks));

            // Create MIDI file with specified time division
            var midiFile = new MidiFile
            {
                TimeDivision = new TicksPerQuarterNoteTimeDivision(MusicConstants.TicksPerQuarterNote)
            };

            // Convert each PartTrack to a track
            foreach (var partTrack in partTracks)
            {
                var trackChunk = CreateTrackFromMidiEvents(partTrack.PartTrackNoteEvents);
                midiFile.Chunks.Add(trackChunk);
            }

            return new MidiSongDocument(midiFile);
        }

        // AI: CreateTrackFromMidiEvents: single-pass convert; lastAbsoluteTime starts 0; detect drum track if any Program==255
        // AI: Skip EndOfTrack from input; TrackChunk writer will add final EndOfTrack automatically
        private static TrackChunk CreateTrackFromMidiEvents(List<PartTrackEvent> midiEvents)
        {
            var trackChunk = new TrackChunk();
            long lastAbsoluteTime = 0;

            // Detect if this is a drum track by checking for program number 255
            bool isDrumTrack = midiEvents.Any(e => 
                e.Type == PartTrackEventType.ProgramChange && 
                GetIntParam(e, "Program") == 255);

            foreach (var midiEvent in midiEvents)
            {
                // Skip EndOfTrack events from input - TrackChunk will add it automatically
                if (midiEvent.Type == PartTrackEventType.EndOfTrack)
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

        // AI: ConvertToDryWetMidiEvent: exhaustive mapping; keep channel rules (drum->9) and conversions (pitchbend signed->unsigned)
        // AI: DO NOT change which event types are converted vs skipped; maintain parameter key usage consistency
        private static Melanchall.DryWetMidi.Core.MidiEvent? ConvertToDryWetMidiEvent(
            PartTrackEvent midiEvent,
            long deltaTime,
            bool isDrumTrack)
        {
            return midiEvent.Type switch
            {
                // Meta Events
                PartTrackEventType.Text => new TextEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.CopyrightNotice => new CopyrightNoticeEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.SequenceTrackName => new SequenceTrackNameEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.InstrumentName => new InstrumentNameEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.Lyric => new LyricEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.Marker => new MarkerEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.ProgramName => new ProgramNameEvent(GetStringParam(midiEvent, "Text"))
                {
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.EndOfTrack => 
                    // Skip - TrackChunk handles EndOfTrack automatically during write
                    null,
                
                PartTrackEventType.SetTempo => CreateSetTempoEvent(midiEvent, deltaTime),
                
                PartTrackEventType.TimeSignature => CreateTimeSignatureEvent(midiEvent, deltaTime),
                
                PartTrackEventType.KeySignature => new KeySignatureEvent(
                    (sbyte)GetIntParam(midiEvent, "SharpsFlats"),
                    (byte)GetIntParam(midiEvent, "Mode"))
                {
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.SequencerSpecific => new SequencerSpecificEvent(GetBytesParam(midiEvent, "Data"))
                {
                    DeltaTime = deltaTime
                },

                PartTrackEventType.SmpteOffset => CreateSmpteOffsetEvent(midiEvent, deltaTime),
                
                // Channel Voice Messages
                PartTrackEventType.NoteOff => new NoteOffEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "NoteNumber"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Velocity"))
                {
                    Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.NoteOn => new NoteOnEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "NoteNumber"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Velocity"))
                {
                    Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.PolyKeyPressure => new NoteAftertouchEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "NoteNumber"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Pressure"))
                {
                    Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.ControlChange => new ControlChangeEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "Controller"),
                    (SevenBitNumber)GetIntParam(midiEvent, "Value"))
                {
                    Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.ProgramChange => CreateProgramChangeEvent(midiEvent, deltaTime, isDrumTrack),
                
                PartTrackEventType.ChannelPressure => new ChannelAftertouchEvent(
                    (SevenBitNumber)GetIntParam(midiEvent, "Pressure"))
                {
                    Channel = isDrumTrack ? (FourBitNumber)9 : (FourBitNumber)GetIntParam(midiEvent, "Channel"),
                    DeltaTime = deltaTime
                },
                
                PartTrackEventType.PitchBend => CreatePitchBendEvent(midiEvent, deltaTime, isDrumTrack),
                
                PartTrackEventType.Unknown => null, // Skip unknown events
                
                _ => null
            };
        }

        // AI: CreateProgramChangeEvent: programNumber==255 is sentinel for drums; skip emitting ProgramChange for drums
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

        // AI: CreateSetTempoEvent: honors MicrosecondsPerQuarterNote if present; else uses BPM; default 120BPM if missing
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

        // AI: TimeSignature conversion: Parameters store actual denominator (4,8,16); DryWetMidi expects exponent (2,3,4)
        // AI: Keep fallback behavior: non-power-of-two denominators round to nearest exponent and clamp 0..8
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

        // AI: CreatePitchBendEvent: expects param Value signed (-8192..+8191); convert to unsigned 0..16383 by +8192
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