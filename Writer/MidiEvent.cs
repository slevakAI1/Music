namespace Music.Writer
{
    /// <summary>
    /// Represents the type of MIDI event (channel, meta, or system exclusive).
    /// </summary>
    public enum MidiEventType
    {
        // Meta events (track-level)
        SequenceTrackName,
        SetTempo,          // supply BPM or MicrosecondsPerQuarterNote
        TimeSignature,     // supply Numerator/Denominator (+ optional metronome fields)
        KeySignature,      // optional but common
        Text,
        Marker,
        CuePoint,
        Lyric,
        EndOfTrack,

        // Channel events (need Channel)
        NoteOn,
        NoteOff,
        ProgramChange,
        ControlChange,
        PitchBend,         // -8192..+8191
        ChannelPressure,   // 0..127
        PolyPressure,      // 0..127 (needs Note/NoteNumber)

        // System exclusive
        SysEx
    }

    /// <summary>
    /// High-level, human-readable MIDI event representation.
    /// Supports channel events, meta events, and system exclusive messages.
    /// Use factory methods for creating common event types with proper validation.
    /// </summary>
    public sealed record MidiEvent
    {
        // ============================================================
        // Core properties (always present)
        // ============================================================

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

        // ============================================================
        // Channel-specific (for channel events only)
        // ============================================================

        /// <summary>
        /// MIDI channel (0-15). Null for meta events.
        /// Channel 9 is typically reserved for drums.
        /// </summary>
        public int? Channel { get; init; }

        // ============================================================
        // Note-related properties (NoteOn/NoteOff/PolyPressure)
        // ============================================================

        /// <summary>
        /// Human-readable note name, e.g., "C4", "F#3", "Bb5".
        /// Generator can convert this to NoteNumber.
        /// </summary>
        public string? Note { get; init; }

        /// <summary>
        /// MIDI note number (0-127). Alternative to Note property.
        /// Generator can compute this from Note if present.
        /// </summary>
        public int? NoteNumber { get; init; }

        /// <summary>
        /// Note-on velocity (0-127). 0 is often treated as note-off.
        /// </summary>
        public int? Velocity { get; init; }

        /// <summary>
        /// Note-off release velocity (0-127). Often 0 in practice.
        /// </summary>
        public int? ReleaseVelocity { get; init; }

        // ============================================================
        // Program Change
        // ============================================================

        /// <summary>
        /// MIDI program number (0-127) for instrument selection.
        /// </summary>
        public int? ProgramNumber { get; init; }

        /// <summary>
        /// Human-readable program name, e.g., "Acoustic Grand Piano", "Violin".
        /// Purely for readability; generator resolves to ProgramNumber.
        /// </summary>
        public string? ProgramName { get; init; }

        // ============================================================
        // Control Change
        // ============================================================

        /// <summary>
        /// MIDI controller number (0-127).
        /// Common: 7=Volume, 10=Pan, 64=Sustain, 91=Reverb, 93=Chorus.
        /// </summary>
        public int? ControllerNumber { get; init; }

        /// <summary>
        /// Human-readable controller name, e.g., "Volume", "Pan", "Sustain".
        /// Generator can resolve to ControllerNumber.
        /// </summary>
        public string? ControllerName { get; init; }

        /// <summary>
        /// Controller value (0-127).
        /// For Sustain: 0-63=off, 64-127=on.
        /// </summary>
        public int? ControllerValue { get; init; }

        // ============================================================
        // Pitch Bend / Pressure
        // ============================================================

        /// <summary>
        /// Pitch bend value (-8192 to +8191). 0 = center (no bend).
        /// </summary>
        public int? PitchBendValue { get; init; }

        /// <summary>
        /// Channel or polyphonic aftertouch pressure (0-127).
        /// </summary>
        public int? Pressure { get; init; }

        // ============================================================
        // Tempo
        // ============================================================

        /// <summary>
        /// Tempo in beats per minute (BPM), e.g., 120.
        /// Alternative to MicrosecondsPerQuarterNote.
        /// </summary>
        public int? TempoBpm { get; init; }

        /// <summary>
        /// Tempo in microseconds per quarter note, e.g., 500000 for 120 BPM.
        /// Alternative to TempoBpm.
        /// </summary>
        public int? MicrosecondsPerQuarterNote { get; init; }

        // ============================================================
        // Time Signature
        // ============================================================

        /// <summary>
        /// Time signature numerator (top number), e.g., 4 for 4/4 time.
        /// </summary>
        public int? TimeSigNumerator { get; init; }

        /// <summary>
        /// Time signature denominator (bottom number), e.g., 4 for 4/4 time.
        /// </summary>
        public int? TimeSigDenominator { get; init; }

        /// <summary>
        /// MIDI clocks per metronome click. Often 24 if not specified.
        /// </summary>
        public int? ClocksPerMetronomeClick { get; init; }

        /// <summary>
        /// Number of 32nd notes per quarter note. Often 8 if not specified.
        /// </summary>
        public int? ThirtySecondNotesPerQuarter { get; init; }

        // ============================================================
        // Key Signature
        // ============================================================

        /// <summary>
        /// Number of sharps (positive) or flats (negative) in key signature (-7 to +7).
        /// 0 = C major / A minor.
        /// </summary>
        public int? KeySigSharpsFlats { get; init; }

        /// <summary>
        /// Key signature mode: "major" or "minor".
        /// </summary>
        public string? KeySigMode { get; init; }

        // ============================================================
        // Text/Meta Content
        // ============================================================

        /// <summary>
        /// Text content for meta events (TrackName, Text, Marker, Lyric, CuePoint, etc.).
        /// </summary>
        public string? Text { get; init; }

        // ============================================================
        // System Exclusive
        // ============================================================

        /// <summary>
        /// SysEx preset name, e.g., "GM System On", "Roland GS Reset".
        /// Generator translates known presets to byte sequences.
        /// </summary>
        public string? SysExPreset { get; init; }

        /// <summary>
        /// Human-readable SysEx payload for custom messages.
        /// Not raw bytes - application-specific format.
        /// </summary>
        public string? SysExPayload { get; init; }

        // ============================================================
        // Factory Methods (recommended usage)
        // ============================================================

        /// <summary>
        /// Creates a sequence/track name meta event.
        /// </summary>
        public static MidiEvent TrackName(long absoluteTime, string name) =>
            new() { Type = MidiEventType.SequenceTrackName, AbsoluteTimeTicks = absoluteTime, Text = name };

        /// <summary>
        /// Creates a tempo meta event using BPM (beats per minute).
        /// </summary>
        public static MidiEvent SetTempoBpm(long absoluteTime, int bpm) =>
            new() { Type = MidiEventType.SetTempo, AbsoluteTimeTicks = absoluteTime, TempoBpm = bpm };

        /// <summary>
        /// Creates a tempo meta event using microseconds per quarter note.
        /// </summary>
        public static MidiEvent SetTempoUsPerQn(long absoluteTime, int microsecondsPerQuarterNote) =>
            new() 
            { 
                Type = MidiEventType.SetTempo, 
                AbsoluteTimeTicks = absoluteTime, 
                MicrosecondsPerQuarterNote = microsecondsPerQuarterNote 
            };

        /// <summary>
        /// Creates a time signature meta event.
        /// </summary>
        public static MidiEvent TimeSignature(
            long absoluteTime, 
            int numerator, 
            int denominator,
            int? clocksPerMetronomeClick = null, 
            int? thirtySecondNotesPerQuarter = null) =>
            new()
            {
                Type = MidiEventType.TimeSignature,
                AbsoluteTimeTicks = absoluteTime,
                TimeSigNumerator = numerator,
                TimeSigDenominator = denominator,
                ClocksPerMetronomeClick = clocksPerMetronomeClick,
                ThirtySecondNotesPerQuarter = thirtySecondNotesPerQuarter
            };

        /// <summary>
        /// Creates a key signature meta event.
        /// </summary>
        public static MidiEvent KeySignature(long absoluteTime, int sharpsFlats, string mode) =>
            new()
            {
                Type = MidiEventType.KeySignature,
                AbsoluteTimeTicks = absoluteTime,
                KeySigSharpsFlats = sharpsFlats,
                KeySigMode = mode
            };

        /// <summary>
        /// Creates a text meta event.
        /// </summary>
        public static MidiEvent TextEvent(long absoluteTime, string text) =>
            new() { Type = MidiEventType.Text, AbsoluteTimeTicks = absoluteTime, Text = text };

        /// <summary>
        /// Creates a marker meta event.
        /// </summary>
        public static MidiEvent Marker(long absoluteTime, string text) =>
            new() { Type = MidiEventType.Marker, AbsoluteTimeTicks = absoluteTime, Text = text };

        /// <summary>
        /// Creates a cue point meta event.
        /// </summary>
        public static MidiEvent CuePoint(long absoluteTime, string text) =>
            new() { Type = MidiEventType.CuePoint, AbsoluteTimeTicks = absoluteTime, Text = text };

        /// <summary>
        /// Creates a lyric meta event.
        /// </summary>
        public static MidiEvent Lyric(long absoluteTime, string text) =>
            new() { Type = MidiEventType.Lyric, AbsoluteTimeTicks = absoluteTime, Text = text };

        /// <summary>
        /// Creates an end-of-track meta event.
        /// </summary>
        public static MidiEvent EndOfTrack(long absoluteTime = 0) =>
            new() { Type = MidiEventType.EndOfTrack, AbsoluteTimeTicks = absoluteTime };

        /// <summary>
        /// Creates a program change event.
        /// </summary>
        public static MidiEvent ProgramChange(
            long absoluteTime, 
            int channel, 
            int programNumber, 
            string? programName = null) =>
            new()
            {
                Type = MidiEventType.ProgramChange,
                AbsoluteTimeTicks = absoluteTime,
                Channel = channel,
                ProgramNumber = programNumber,
                ProgramName = programName
            };

        /// <summary>
        /// Creates a control change event.
        /// </summary>
        public static MidiEvent ControlChange(
            long absoluteTime, 
            int channel, 
            int controllerNumber, 
            int value, 
            string? controllerName = null) =>
            new()
            {
                Type = MidiEventType.ControlChange,
                AbsoluteTimeTicks = absoluteTime,
                Channel = channel,
                ControllerNumber = controllerNumber,
                ControllerValue = value,
                ControllerName = controllerName
            };

        /// <summary>
        /// Creates a note-on event using human-readable note name.
        /// </summary>
        public static MidiEvent NoteOn(long absoluteTime, int channel, string note, int velocity) =>
            new()
            {
                Type = MidiEventType.NoteOn,
                AbsoluteTimeTicks = absoluteTime,
                Channel = channel,
                Note = note,
                Velocity = velocity
            };

        /// <summary>
        /// Creates a note-on event using MIDI note number.
        /// </summary>
        public static MidiEvent NoteOnByNumber(long absoluteTime, int channel, int noteNumber, int velocity) =>
            new()
            {
                Type = MidiEventType.NoteOn,
                AbsoluteTimeTicks = absoluteTime,
                Channel = channel,
                NoteNumber = noteNumber,
                Velocity = velocity
            };

        /// <summary>
        /// Creates a note-off event using human-readable note name.
        /// </summary>
        public static MidiEvent NoteOff(long absoluteTime, int channel, string note, int releaseVelocity = 0) =>
            new()
            {
                Type = MidiEventType.NoteOff,
                AbsoluteTimeTicks = absoluteTime,
                Channel = channel,
                Note = note,
                ReleaseVelocity = releaseVelocity
            };

        /// <summary>
        /// Creates a note-off event using MIDI note number.
        /// </summary>
        public static MidiEvent NoteOffByNumber(long absoluteTime, int channel, int noteNumber, int releaseVelocity = 0) =>
            new()
            {
                Type = MidiEventType.NoteOff,
                AbsoluteTimeTicks = absoluteTime,
                Channel = channel,
                NoteNumber = noteNumber,
                ReleaseVelocity = releaseVelocity
            };

        /// <summary>
        /// Creates a pitch bend event.
        /// </summary>
        /// <param name="bendValue">Pitch bend value (-8192 to +8191, 0 = center)</param>
        public static MidiEvent PitchBend(long absoluteTime, int channel, int bendValue) =>
            new()
            {
                Type = MidiEventType.PitchBend,
                AbsoluteTimeTicks = absoluteTime,
                Channel = channel,
                PitchBendValue = bendValue
            };

        /// <summary>
        /// Creates a channel pressure (aftertouch) event.
        /// </summary>
        public static MidiEvent ChannelPressure(long absoluteTime, int channel, int pressure) =>
            new()
            {
                Type = MidiEventType.ChannelPressure,
                AbsoluteTimeTicks = absoluteTime,
                Channel = channel,
                Pressure = pressure
            };

        /// <summary>
        /// Creates a polyphonic pressure (aftertouch) event.
        /// </summary>
        public static MidiEvent PolyPressure(long absoluteTime, int channel, string note, int pressure) =>
            new()
            {
                Type = MidiEventType.PolyPressure,
                AbsoluteTimeTicks = absoluteTime,
                Channel = channel,
                Note = note,
                Pressure = pressure
            };

        /// <summary>
        /// Creates a system exclusive event.
        /// </summary>
        public static MidiEvent SysEx(long absoluteTime, string preset, string? payload = null) =>
            new()
            {
                Type = MidiEventType.SysEx,
                AbsoluteTimeTicks = absoluteTime,
                SysExPreset = preset,
                SysExPayload = payload
            };
    }
}
