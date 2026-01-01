// AI: purpose=Flexible, human-friendly MIDI event representation for composition and MIDI export.
// AI: invariants=AbsoluteTimeTicks is authoritative timing; Parameters keys and types are a stable contract; Type must match Parameters.
// AI: deps=Used by MIDI exporter and generators; changing parameter key names/types breaks downstream serialization and consumers.
// AI: perf=Not hotpath; objects created during track build/export; avoid large binary blobs in Parameters unless intentional.

using Music.Generator;

namespace Music.MyMidi
{
    // AI: PartTrackEvent is a thin DTO; factories set Type and Parameters consistently. Keep constructors and factory behaviors stable.
    public class PartTrackEvent
    {
        // AI: AbsoluteTimeTicks: absolute tick position from track start; used to compute delta times later.
        public long AbsoluteTimeTicks { get; init; }

        // AI: DeltaTicks: computed later; initialized to 0 on creation; not authoritative for ordering.
        public long DeltaTicks { get; init; }

        // AI: Type: MIDI event classification; factories must set matching Parameters schema.
        public PartTrackEventType Type { get; init; }

        // AI: Parameters: string keys to object values (int,string,byte[]). Keys must be stable across exporters.
        public Dictionary<string, object> Parameters { get; init; } = new();

        // AI: Convenience fields for simple note events; factories prefer Parameters dictionary for full compatibility.
        public int NoteNumber { get; set; }
        public int NoteDurationTicks { get; set; }
        public int NoteOnVelocity { get; set; } = 100;

        // AI: Simple note constructor: creates a NoteOn-type event with minimal fields for backward compatibility.
        // AI: change=If altering behavior, update callers that construct notes directly instead of using factories.
        public PartTrackEvent(
            int noteNumber,
            int absoluteTimeTicks,
            int noteDurationTicks,
            int noteOnVelocity = 100)
        {
            NoteNumber = noteNumber;
            AbsoluteTimeTicks = absoluteTimeTicks;
            NoteDurationTicks = noteDurationTicks;
            NoteOnVelocity = noteOnVelocity;
            Type = PartTrackEventType.NoteOn; // Set proper type for simple notes
        }

        // AI: Default ctor retained for factory methods and deserialization.
        public PartTrackEvent()
        {
        }

        // ============================================================
        // Factory Methods - Meta Events
        // ============================================================
        // AI: Factory methods construct fully-formed events: set Type, AbsoluteTimeTicks, and Parameters.
        // AI: Keep parameter key names stable; exporters rely on these keys.

        public static PartTrackEvent CreateSequenceNumber(long absoluteTime, ushort sequenceNumber) =>
            new()
            {
                Type = PartTrackEventType.SequenceNumber,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["SequenceNumber"] = sequenceNumber }
            };

        public static PartTrackEvent CreateText(long absoluteTime, string text) =>
            new()
            {
                Type = PartTrackEventType.Text,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = text }
            };

        public static PartTrackEvent CreateCopyrightNotice(long absoluteTime, string text) =>
            new()
            {
                Type = PartTrackEventType.CopyrightNotice,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = text }
            };

        public static PartTrackEvent CreateSequenceTrackName(long absoluteTime, string name) =>
            new()
            {
                Type = PartTrackEventType.SequenceTrackName,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = name }
            };

        public static PartTrackEvent CreateInstrumentName(long absoluteTime, string name) =>
            new()
            {
                Type = PartTrackEventType.InstrumentName,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = name }
            };

        public static PartTrackEvent CreateLyric(long absoluteTime, string lyric) =>
            new()
            {
                Type = PartTrackEventType.Lyric,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = lyric }
            };

        public static PartTrackEvent CreateMarker(long absoluteTime, string marker) =>
            new()
            {
                Type = PartTrackEventType.Marker,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = marker }
            };

        public static PartTrackEvent CreateCuePoint(long absoluteTime, string cuePoint) =>
            new()
            {
                Type = PartTrackEventType.CuePoint,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = cuePoint }
            };

        public static PartTrackEvent CreateProgramName(long absoluteTime, string programName) =>
            new()
            {
                Type = PartTrackEventType.ProgramName,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = programName }
            };

        public static PartTrackEvent CreateDeviceName(long absoluteTime, string deviceName) =>
            new()
            {
                Type = PartTrackEventType.DeviceName,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = deviceName }
            };

        public static PartTrackEvent CreateMidiChannelPrefix(long absoluteTime, byte channel) =>
            new()
            {
                Type = PartTrackEventType.MidiChannelPrefix,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Channel"] = channel }
            };

        public static PartTrackEvent CreateMidiPort(long absoluteTime, byte port) =>
            new()
            {
                Type = PartTrackEventType.MidiPort,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Port"] = port }
            };

        public static PartTrackEvent CreateEndOfTrack(long absoluteTime = 0) =>
            new()
            {
                Type = PartTrackEventType.EndOfTrack,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        // AI: CreateSetTempo accepts either bpm or microsecondsPerQuarterNote; prefer bpm for readability.
        public static PartTrackEvent CreateSetTempo(long absoluteTime, int? bpm = null, int? microsecondsPerQuarterNote = null)
        {
            var parameters = new Dictionary<string, object>();
            if (bpm.HasValue)
                parameters["BPM"] = bpm.Value;
            else if (microsecondsPerQuarterNote.HasValue)
                parameters["MicrosecondsPerQuarterNote"] = microsecondsPerQuarterNote.Value;

            return new()
            {
                Type = PartTrackEventType.SetTempo,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

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
                Type = PartTrackEventType.SmpteOffset,
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
                Type = PartTrackEventType.SmpteOffset,
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

        public static PartTrackEvent CreateTimeSignature(
            long absoluteTime,
            int numerator,
            int denominator,
            int clocksPerMetronomeClick = 24,
            int thirtySecondNotesPerQuarter = 8) =>
            new()
            {
                Type = PartTrackEventType.TimeSignature,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
                {
                    ["Numerator"] = numerator,
                    ["Denominator"] = denominator,
                    ["ClocksPerMetronomeClick"] = clocksPerMetronomeClick,
                    ["ThirtySecondNotesPerQuarter"] = thirtySecondNotesPerQuarter
                }
            };

        public static PartTrackEvent CreateKeySignature(long absoluteTime, int sharpsFlats, int mode) =>
            new()
            {
                Type = PartTrackEventType.KeySignature,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
                {
                    ["SharpsFlats"] = sharpsFlats,
                    ["Mode"] = mode
                }
            };

        public static PartTrackEvent CreateSequencerSpecific(long absoluteTime, byte[] data) =>
            new()
            {
                Type = PartTrackEventType.SequencerSpecific,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Data"] = data }
            };

        public static PartTrackEvent CreateUnknownMeta(long absoluteTime, byte statusByte, byte[] data) =>
            new()
            {
                Type = PartTrackEventType.UnknownMeta,
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
                Type = PartTrackEventType.NoteOff,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

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
                Type = PartTrackEventType.NoteOn,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

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
                Type = PartTrackEventType.PolyKeyPressure,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

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
                Type = PartTrackEventType.ControlChange,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

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
                Type = PartTrackEventType.ProgramChange,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

        public static PartTrackEvent CreateChannelPressure(long absoluteTime, int channel, int pressure) =>
            new()
            {
                Type = PartTrackEventType.ChannelPressure,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
                {
                    ["Channel"] = channel,
                    ["Pressure"] = pressure
                }
            };

        public static PartTrackEvent CreatePitchBend(long absoluteTime, int channel, int value) =>
            new()
            {
                Type = PartTrackEventType.PitchBend,
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

        public static PartTrackEvent CreateNormalSysEx(long absoluteTime, byte[] data) =>
            new()
            {
                Type = PartTrackEventType.NormalSysEx,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Data"] = data }
            };

        public static PartTrackEvent CreateEscapeSysEx(long absoluteTime, byte[] data) =>
            new()
            {
                Type = PartTrackEventType.EscapeSysEx,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Data"] = data }
            };

        // ============================================================
        // Factory Methods - System Common Messages
        // ============================================================

        public static PartTrackEvent CreateMtcQuarterFrame(long absoluteTime, byte messageType, byte values) =>
            new()
            {
                Type = PartTrackEventType.MtcQuarterFrame,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
                {
                    ["MessageType"] = messageType,
                    ["Values"] = values
                }
            };

        public static PartTrackEvent CreateSongPositionPointer(long absoluteTime, ushort position) =>
            new()
            {
                Type = PartTrackEventType.SongPositionPointer,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Position"] = position }
            };

        public static PartTrackEvent CreateSongSelect(long absoluteTime, byte songNumber) =>
            new()
            {
                Type = PartTrackEventType.SongSelect,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["SongNumber"] = songNumber }
            };

        public static PartTrackEvent CreateTuneRequest(long absoluteTime) =>
            new()
            {
                Type = PartTrackEventType.TuneRequest,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        // ============================================================
        // Factory Methods - System Real-Time Messages
        // ============================================================

        public static PartTrackEvent CreateTimingClock(long absoluteTime) =>
            new()
            {
                Type = PartTrackEventType.TimingClock,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        public static PartTrackEvent CreateStart(long absoluteTime) =>
            new()
            {
                Type = PartTrackEventType.Start,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        public static PartTrackEvent CreateContinue(long absoluteTime) =>
            new()
            {
                Type = PartTrackEventType.Continue,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        public static PartTrackEvent CreateStop(long absoluteTime) =>
            new()
            {
                Type = PartTrackEventType.Stop,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        public static PartTrackEvent CreateActiveSensing(long absoluteTime) =>
            new()
            {
                Type = PartTrackEventType.ActiveSensing,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        public static PartTrackEvent CreateSystemReset(long absoluteTime) =>
            new()
            {
                Type = PartTrackEventType.SystemReset,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new()
            };

        // ============================================================
        // Factory Methods - Unknown/Forward Compatibility
        // ============================================================

        public static PartTrackEvent CreateUnknown(long absoluteTime, byte[] rawData) =>
            new()
            {
                Type = PartTrackEventType.Unknown,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["RawData"] = rawData }
            };
    }
}
