using Music.Writer;

namespace Music.MyMidi
{
    /// <summary>
    /// High-level, human-readable MIDI event representation.
    /// Uses a dictionary-based parameter system for flexibility.
    /// Use factory methods for creating event types with proper validation.
    /// </summary>
    public class PartTrackEvent
    {
        /// <summary>
        /// Absolute time position in ticks from the start of the track.
        /// This is the source of truth for event timing.
        /// </summary>
        public long AbsoluteTimeTicks { get; init; }

        /// <summary>
        /// Delta time in ticks since the previous event.
        /// This is calculated separately and will be 0 when the event is created.
        /// </summary>
        public long DeltaTicks { get; init; }

        /// <summary>
        /// The type of MIDI event.
        /// </summary>
        public MidiEventType Type { get; init; }

        /// <summary>
        /// Dictionary of event parameters. Keys are parameter names following MIDI standards.
        /// Values can be int, string, or byte[] depending on the parameter type.
        /// </summary>
        public Dictionary<string, object> Parameters { get; init; } = new();

        // MIDI-related properties for simple note creation - 480 ticks / quarter note is standard
        public int NoteNumber { get; set; }
        public int AbsolutePositionTicks { get; set; }
        public int NoteDurationTicks { get; set; }
        public int NoteOnVelocity { get; set; } = 100;




        // Metadata fields - can be used for display purposes. Also used by musicxml.
        public char Step { get; set; }
        public int Alter { get; set; }
        public int Octave { get; set; }
        public int Duration { get; set; }
        public int Dots { get; set; }
        public int? TupletActualNotes { get; set; }
        public int? TupletNormalNotes { get; set; }

        /// <summary>
        /// Simple note constructor for backward compatibility with note-based code.
        /// </summary>
        public PartTrackEvent(
            int noteNumber,
            int absolutePositionTicks,
            int noteDurationTicks,
            int noteOnVelocity = 100)
        {
            NoteNumber = noteNumber;
            AbsolutePositionTicks = absolutePositionTicks;
            NoteDurationTicks = noteDurationTicks;
            NoteOnVelocity = noteOnVelocity;
            AbsoluteTimeTicks = absolutePositionTicks;

            // Calculate metadata fields from MIDI properties
            (Step, Alter, Octave) = MusicCalculations.CalculatePitch(noteNumber);
            (Duration, Dots, TupletActualNotes, TupletNormalNotes) = MusicCalculations.CalculateRhythm(noteDurationTicks);
        }

        /// <summary>
        /// Default constructor for factory methods.
        /// </summary>
        public PartTrackEvent()
        {
        }

        // ============================================================
        // Factory Methods - Meta Events
        // ============================================================

        /// <summary>
        /// Creates a sequence number meta event (0x00).
        /// </summary>
        public static PartTrackEvent CreateSequenceNumber(long absoluteTime, ushort sequenceNumber) =>
            new()
            {
                Type = MidiEventType.SequenceNumber,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["SequenceNumber"] = sequenceNumber }
            };

        /// <summary>
        /// Creates a text meta event (0x01).
        /// </summary>
        public static PartTrackEvent CreateText(long absoluteTime, string text) =>
            new()
            {
                Type = MidiEventType.Text,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = text }
            };

        /// <summary>
        /// Creates a copyright notice meta event (0x02).
        /// </summary>
        public static PartTrackEvent CreateCopyrightNotice(long absoluteTime, string text) =>
            new()
            {
                Type = MidiEventType.CopyrightNotice,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = text }
            };

        /// <summary>
        /// Creates a sequence/track name meta event (0x03).
        /// </summary>
        public static PartTrackEvent CreateSequenceTrackName(long absoluteTime, string name) =>
            new()
            {
                Type = MidiEventType.SequenceTrackName,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = name }
            };

        /// <summary>
        /// Creates an instrument name meta event (0x04).
        /// </summary>
        public static PartTrackEvent CreateInstrumentName(long absoluteTime, string name) =>
            new()
            {
                Type = MidiEventType.InstrumentName,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = name }
            };

        /// <summary>
        /// Creates a lyric meta event (0x05).
        /// </summary>
        public static PartTrackEvent CreateLyric(long absoluteTime, string lyric) =>
            new()
            {
                Type = MidiEventType.Lyric,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = lyric }
            };

        /// <summary>
        /// Creates a marker meta event (0x06).
        /// </summary>
        public static PartTrackEvent CreateMarker(long absoluteTime, string marker) =>
            new()
            {
                Type = MidiEventType.Marker,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = marker }
            };

        /// <summary>
        /// Creates a cue point meta event (0x07).
        /// </summary>
        public static PartTrackEvent CreateCuePoint(long absoluteTime, string cuePoint) =>
            new()
            {
                Type = MidiEventType.CuePoint,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = cuePoint }
            };

        /// <summary>
        /// Creates a program name meta event (0x08).
        /// </summary>
        public static PartTrackEvent CreateProgramName(long absoluteTime, string programName) =>
            new()
            {
                Type = MidiEventType.ProgramName,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = programName }
            };

        /// <summary>
        /// Creates a device name meta event (0x09).
        /// </summary>
        public static PartTrackEvent CreateDeviceName(long absoluteTime, string deviceName) =>
            new()
            {
                Type = MidiEventType.DeviceName,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = deviceName }
            };

        /// <summary>
        /// Creates a MIDI channel prefix meta event (0x20).
        /// </summary>
        public static PartTrackEvent CreateMidiChannelPrefix(long absoluteTime, byte channel) =>
            new()
            {
                Type = MidiEventType.MidiChannelPrefix,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Channel"] = channel }
            };

        /// <summary>
        /// Creates a MIDI port meta event (0x21).
        /// </summary>
        public static PartTrackEvent CreateMidiPort(long absoluteTime, byte port) =>
            new()
            {
                Type = MidiEventType.MidiPort,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Port"] = port }
            };

        /// <summary>
        /// Creates an end-of-track meta event (0x2F).
        /// </summary>
        public static PartTrackEvent CreateEndOfTrack(long absoluteTime = 0) =>
            new()
            {
                Type = MidiEventType.EndOfTrack,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        /// <summary>
        /// Creates a set tempo meta event (0x51).
        /// Can specify either BPM or microseconds per quarter note.
        /// </summary>
        public static PartTrackEvent CreateSetTempo(long absoluteTime, int? bpm = null, int? microsecondsPerQuarterNote = null)
        {
            var parameters = new Dictionary<string, object>();
            if (bpm.HasValue)
                parameters["BPM"] = bpm.Value;
            else if (microsecondsPerQuarterNote.HasValue)
                parameters["MicrosecondsPerQuarterNote"] = microsecondsPerQuarterNote.Value;

            return new()
            {
                Type = MidiEventType.SetTempo,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Creates a SMPTE offset meta event (0x54).
        /// Specifies an offset for SMPTE time code synchronization.
        /// </summary>
        public static PartTrackEvent CreateSmpteOffset(
            long absoluteTime,
            byte hours,
            byte minutes,
            byte seconds,
            byte frames,
            byte subFrames)
        {
            return new PartTrackEvent
            {
                AbsoluteTimeTicks = absoluteTime,
                Type = MidiEventType.SmpteOffset,
                Parameters = new Dictionary<string, object>
                {
                    { "Hours", hours },
                    { "Minutes", minutes },
                    { "Seconds", seconds },
                    { "Frames", frames },
                    { "SubFrames", subFrames }
                }
            };
        }

        /// <summary>
        /// Creates a SMPTE offset meta event (0x54) with format specification.
        /// </summary>
        public static PartTrackEvent CreateSmpteOffset(
            long absoluteTime,
            int format,
            byte hours,
            byte minutes,
            byte seconds,
            byte frames,
            byte subFrames)
        {
            return new PartTrackEvent
            {
                AbsoluteTimeTicks = absoluteTime,
                Type = MidiEventType.SmpteOffset,
                Parameters = new Dictionary<string, object>
                {
                    { "Format", format },
                    { "Hours", hours },
                    { "Minutes", minutes },
                    { "Seconds", seconds },
                    { "Frames", frames },
                    { "SubFrames", subFrames }
                }
            };
        }

        /// <summary>
        /// Creates a time signature meta event (0x58).
        /// </summary>
        public static PartTrackEvent CreateTimeSignature(
            long absoluteTime,
            int numerator,
            int denominator,
            int clocksPerMetronomeClick = 24,
            int thirtySecondNotesPerQuarter = 8) =>
            new()
            {
                Type = MidiEventType.TimeSignature,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
                {
                    ["Numerator"] = numerator,
                    ["Denominator"] = denominator,
                    ["ClocksPerMetronomeClick"] = clocksPerMetronomeClick,
                    ["ThirtySecondNotesPerQuarter"] = thirtySecondNotesPerQuarter
                }
            };

        /// <summary>
        /// Creates a key signature meta event (0x59).
        /// </summary>
        public static PartTrackEvent CreateKeySignature(long absoluteTime, int sharpsFlats, int mode) =>
            new()
            {
                Type = MidiEventType.KeySignature,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
                {
                    ["SharpsFlats"] = sharpsFlats,
                    ["Mode"] = mode
                }
            };

        /// <summary>
        /// Creates a sequencer-specific meta event (0x7F).
        /// </summary>
        public static PartTrackEvent CreateSequencerSpecific(long absoluteTime, byte[] data) =>
            new()
            {
                Type = MidiEventType.SequencerSpecific,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Data"] = data }
            };

        /// <summary>
        /// Creates an unknown meta event for forward compatibility.
        /// </summary>
        public static PartTrackEvent CreateUnknownMeta(long absoluteTime, byte statusByte, byte[] data) =>
            new()
            {
                Type = MidiEventType.UnknownMeta,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() 
                { 
                    ["StatusByte"] = statusByte,
                    ["Data"] = data 
                }
            };

        // ============================================================
        // Factory Methods - Channel Voice Messages
        // ============================================================

        /// <summary>
        /// Creates a note-off event (0x8n).
        /// </summary>
        public static PartTrackEvent CreateNoteOff(long absoluteTime, int channel, int noteNumber, int velocity = 0, string? note = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["Channel"] = channel,
                ["NoteNumber"] = noteNumber,
                ["Velocity"] = velocity
            };
            if (note != null)
                parameters["Note"] = note;

            return new()
            {
                Type = MidiEventType.NoteOff,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Creates a note-on event (0x9n).
        /// </summary>
        public static PartTrackEvent CreateNoteOn(long absoluteTime, int channel, int noteNumber, int velocity, string? note = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["Channel"] = channel,
                ["NoteNumber"] = noteNumber,
                ["Velocity"] = velocity
            };
            if (note != null)
                parameters["Note"] = note;

            return new()
            {
                Type = MidiEventType.NoteOn,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Creates a polyphonic key pressure (aftertouch) event (0xAn).
        /// </summary>
        public static PartTrackEvent CreatePolyKeyPressure(long absoluteTime, int channel, int noteNumber, int pressure, string? note = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["Channel"] = channel,
                ["NoteNumber"] = noteNumber,
                ["Pressure"] = pressure
            };
            if (note != null)
                parameters["Note"] = note;

            return new()
            {
                Type = MidiEventType.PolyKeyPressure,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Creates a control change event (0xBn).
        /// </summary>
        public static PartTrackEvent CreateControlChange(long absoluteTime, int channel, int controller, int value, string? controllerName = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["Channel"] = channel,
                ["Controller"] = controller,
                ["Value"] = value
            };
            if (controllerName != null)
                parameters["ControllerName"] = controllerName;

            return new()
            {
                Type = MidiEventType.ControlChange,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Creates a program change event (0xCn).
        /// </summary>
        public static PartTrackEvent CreateProgramChange(long absoluteTime, int channel, int program, string? programName = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["Channel"] = channel,
                ["Program"] = program
            };
            if (programName != null)
                parameters["ProgramName"] = programName;

            return new()
            {
                Type = MidiEventType.ProgramChange,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Creates a channel pressure (aftertouch) event (0xDn).
        /// </summary>
        public static PartTrackEvent CreateChannelPressure(long absoluteTime, int channel, int pressure) =>
            new()
            {
                Type = MidiEventType.ChannelPressure,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
                {
                    ["Channel"] = channel,
                    ["Pressure"] = pressure
                }
            };

        /// <summary>
        /// Creates a pitch bend event (0xEn).
        /// </summary>
        public static PartTrackEvent CreatePitchBend(long absoluteTime, int channel, int value) =>
            new()
            {
                Type = MidiEventType.PitchBend,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
                {
                    ["Channel"] = channel,
                    ["Value"] = value
                }
            };

        // ============================================================
        // Factory Methods - System Exclusive Events
        // ============================================================

        /// <summary>
        /// Creates a normal system exclusive event (0xF0).
        /// </summary>
        public static PartTrackEvent CreateNormalSysEx(long absoluteTime, byte[] data) =>
            new()
            {
                Type = MidiEventType.NormalSysEx,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Data"] = data }
            };

        /// <summary>
        /// Creates an escape system exclusive event (0xF7).
        /// </summary>
        public static PartTrackEvent CreateEscapeSysEx(long absoluteTime, byte[] data) =>
            new()
            {
                Type = MidiEventType.EscapeSysEx,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Data"] = data }
            };

        // ============================================================
        // Factory Methods - System Common Messages
        // ============================================================

        /// <summary>
        /// Creates an MTC quarter frame event (0xF1).
        /// </summary>
        public static PartTrackEvent CreateMtcQuarterFrame(long absoluteTime, byte messageType, byte values) =>
            new()
            {
                Type = MidiEventType.MtcQuarterFrame,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
                {
                    ["MessageType"] = messageType,
                    ["Values"] = values
                }
            };

        /// <summary>
        /// Creates a song position pointer event (0xF2).
        /// </summary>
        public static PartTrackEvent CreateSongPositionPointer(long absoluteTime, ushort position) =>
            new()
            {
                Type = MidiEventType.SongPositionPointer,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Position"] = position }
            };

        /// <summary>
        /// Creates a song select event (0xF3).
        /// </summary>
        public static PartTrackEvent CreateSongSelect(long absoluteTime, byte songNumber) =>
            new()
            {
                Type = MidiEventType.SongSelect,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["SongNumber"] = songNumber }
            };

        /// <summary>
        /// Creates a tune request event (0xF6).
        /// </summary>
        public static PartTrackEvent CreateTuneRequest(long absoluteTime) =>
            new()
            {
                Type = MidiEventType.TuneRequest,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        // ============================================================
        // Factory Methods - System Real-Time Messages
        // ============================================================

        /// <summary>
        /// Creates a timing clock event (0xF8).
        /// </summary>
        public static PartTrackEvent CreateTimingClock(long absoluteTime) =>
            new()
            {
                Type = MidiEventType.TimingClock,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        /// <summary>
        /// Creates a start event (0xFA).
        /// </summary>
        public static PartTrackEvent CreateStart(long absoluteTime) =>
            new()
            {
                Type = MidiEventType.Start,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        /// <summary>
        /// Creates a continue event (0xFB).
        /// </summary>
        public static PartTrackEvent CreateContinue(long absoluteTime) =>
            new()
            {
                Type = MidiEventType.Continue,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        /// <summary>
        /// Creates a stop event (0xFC).
        /// </summary>
        public static PartTrackEvent CreateStop(long absoluteTime) =>
            new()
            {
                Type = MidiEventType.Stop,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        /// <summary>
        /// Creates an active sensing event (0xFE).
        /// </summary>
        public static PartTrackEvent CreateActiveSensing(long absoluteTime) =>
            new()
            {
                Type = MidiEventType.ActiveSensing,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        /// <summary>
        /// Creates a system reset event (0xFF).
        /// </summary>
        public static PartTrackEvent CreateSystemReset(long absoluteTime) =>
            new()
            {
                Type = MidiEventType.SystemReset,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        // ============================================================
        // Factory Methods - Unknown/Forward Compatibility
        // ============================================================

        /// <summary>
        /// Creates an unknown event type for forward compatibility.
        /// </summary>
        public static PartTrackEvent CreateUnknown(long absoluteTime, byte[] rawData) =>
            new()
            {
                Type = MidiEventType.Unknown,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["RawData"] = rawData }
            };
    }
}
