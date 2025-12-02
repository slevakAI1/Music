namespace Music.Writer
{
    /// <summary>
    /// Represents the type of MIDI event (channel, meta, or system exclusive).
    /// </summary>

    public enum MidiEventType
    {
        // ----------------------------
        // Meta events (SMF track-level)
        // ----------------------------

        // Text (0x01).
        // What it is / how it works:
        // Generic text attached to a timestamp. Data is arbitrary text bytes (encoding depends on your app).
        // Affects playback?
        // No. It’s for display/notes.
        // Example use:
        // At bar 17 you embed “Solo starts here” so the DAW shows a cue during playback.
        Text,

        // CopyrightNotice (0x02).
        // What it is / how it works:
        // Text intended specifically for copyright information.
        // Affects playback?
        // No.
        // Example use:
        // At time 0: “© 2025 Your Name. All rights reserved.” so metadata survives file sharing.
        CopyrightNotice,

        // SequenceTrackName (0x03).
        // What it is / how it works:
        // Track name (or “sequence name” depending on context). Used by DAWs for labeling tracks.
        // Affects playback?
        // No directly. It’s UI/organization.
        // Example use:
        // Track 2 gets name “Strings - Legato” so a DAW shows meaningful track labels.
        SequenceTrackName,

        // InstrumentName (0x04).
        // What it is / how it works:
        // A human-readable instrument label. Unlike ProgramChange, this does not select a sound—just a name.
        // Affects playback?
        // Usually no. Some tools map names to sounds, but that’s tool-specific.
        // Example use:
        // You label a track “Fender Rhodes” even if the actual sound is determined by ProgramChange + Bank Select.
        InstrumentName,

        // Lyric (0x05).
        // What it is / how it works:
        // Timed lyric text, often used for karaoke-style lyric display.
        // Affects playback?
        // Doesn’t change audio directly; affects lyric display in karaoke/lyric-aware players.
        // Example use:
        // At each sung syllable timestamp you emit “Hel-”, “lo”, “world” so a karaoke player can display them in sync.
        Lyric,

        // Marker (0x06).
        // What it is / how it works:
        // A labeled marker at a time position (like DAW markers).
        // Affects playback?
        // No. It’s navigation/annotation.
        // Example use:
        // At bar 9: “Chorus” so the user can jump to it in an editor.
        Marker,

        // ProgramName (0x08).
        // What it is / how it works:
        // Human-readable program/patch name. This is not the ProgramChange message.
        // Affects playback?
        // No by itself. It’s descriptive.
        // Example use:
        // You send ProgramChange 41 (violin), and also add ProgramName="Solo Violin KS" so a DAW displays the intended patch name.
        ProgramName,

        // EndOfTrack (0x2F).
        // What it is / how it works:
        // Marks the end of the track data (length always 0).
        // Affects playback?
        // Yes in the sense that it defines the track’s end; players use it to know when the track is done.
        // Example use:
        // A track with a final chord has EndOfTrack at the chord’s note-off time so the track doesn’t “run forever”.
        EndOfTrack,

        // SetTempo (0x51).
        // What it is / how it works:
        // 3 bytes: microseconds per quarter note (MPQN). This is how SMF defines tempo changes; BPM is derived from it.
        // Affects playback?
        // Yes, strongly. It changes how ticks/delta-times convert into real time.
        // Example use:
        // Start at 120 BPM, then at bar 33 slow to 90 BPM by inserting a SetTempo event at that timestamp.
        SetTempo,

        // TimeSignature (0x58).
        // What it is / how it works:
        // 4 bytes: numerator; log2(denominator); MIDI clocks per metronome click; notated 32nd notes per quarter note.
        // Affects playback?
        // Not notes directly, but can affect metronome behavior, bar/beat mapping, grids, looping, and measure counting in sequencers.
        // Example use:
        // Switch from 4/4 to 7/8 at bar 9 so DAW barlines and grid stay correct.
        TimeSignature,

        // KeySignature (0x59).
        // What it is / how it works:
        // 2 bytes: sf (-7..+7 flats/sharps) and mi (0 major, 1 minor). Primarily for notation/display.
        // Affects playback?
        // No for sound; MIDI notes are absolute pitches.
        // Example use:
        // A MIDI intended for sheet generation sets KeySignature=D major (+2 sharps) so notation uses correct accidentals.
        KeySignature,

        // SequencerSpecific (0x7F).
        // What it is / how it works:
        // Arbitrary bytes for a specific sequencer/app to store private data.
        // Affects playback?
        // Not by standard MIDI rules; may affect playback only in the specific sequencer that interprets it.
        // Example use:
        // Your app stores custom humanization settings in SequencerSpecific and reads them back on import.
        SequencerSpecific,

        // ----------------------------
        // Channel voice messages
        // ----------------------------

        // NoteOff (0x8n).
        // What it is / how it works:
        // Ends a note on a channel for a specific key number (0–127). Includes release velocity (often ignored).
        // Affects playback?
        // Yes. Stops the note (unless sustain pedal is down or the synth tail continues).
        // Example use:
        // Note C4 (60) starts at tick 0; at tick 480 emit NoteOff to make it last one quarter note.
        NoteOff,

        // NoteOn (0x9n).
        // What it is / how it works:
        // Starts a note on a channel for a specific key (0–127) with velocity (1–127). Velocity 0 is commonly treated as NoteOff.
        // Affects playback?
        // Yes. Triggers the note; velocity usually maps to loudness/brightness.
        // Example use:
        // Trigger drum hits by sending NoteOn on the drum channel with appropriate note numbers and velocities on each beat.
        NoteOn,

        // PolyKeyPressure (0xAn).
        // What it is / how it works:
        // Per-key (per-note) aftertouch/pressure for a specific note number.
        // Affects playback?
        // Yes if the synth maps it (often to vibrato depth, filter, volume). Many devices ignore it.
        // Example use:
        // Press harder on the top note of a chord and send PolyKeyPressure for that note only to add vibrato to just that note.
        PolyKeyPressure,

        // ControlChange (0xBn).
        // What it is / how it works:
        // Controller number (0–127) plus value (0–127). Used for sustain, modulation, volume, pan, expression, bank select, etc.
        // Affects playback?
        // Yes, very often. It’s a primary way to shape and control performance and routing.
        // Example use:
        // Sustain pedal: CC64 value=127 (down), later CC64 value=0 (up) so held notes continue while the pedal is down.
        ControlChange,

        // ProgramChange (0xCn).
        // What it is / how it works:
        // Selects an instrument/program number (0–127) for that channel; often paired with Bank Select (CC0/CC32) for more banks.
        // Affects playback?
        // Yes. Changes the patch the channel uses going forward until changed again.
        // Example use:
        // Switch from strings to choir at the bridge by inserting a ProgramChange at the section start.
        ProgramChange,

        // ChannelPressure (0xDn).
        // What it is / how it works:
        // Channel-wide aftertouch/pressure value (0–127) affecting all notes on the channel.
        // Affects playback?
        // Yes if the synth maps it (often vibrato/filter/volume).
        // Example use:
        // While holding a lead note, increase ChannelPressure to deepen vibrato without sending a new NoteOn.
        ChannelPressure,

        // PitchBend (0xEn).
        // What it is / how it works:
        // 14-bit pitch bend value (two data bytes). Center means no bend; bend range depends on synth settings.
        // Affects playback?
        // Yes. Bends pitch for notes on that channel (channel-wide).
        // Example use:
        // Simulate a guitar bend by ramping PitchBend up over 200ms while the note is sounding, then returning to center.
        PitchBend,


        // ----------------------------
        // System Exclusive (SMF + Live)
        // ----------------------------

        // SysEx.
        // What it is / how it works:
        // Manufacturer/device-specific message carrying arbitrary bytes; used to set parameters, modes, resets, custom patches, etc.
        // Affects playback?
        // Can, massively—if the target device understands it; otherwise it’s ignored.
        // Example use:
        // At tick 0 send a SysEx reset/mode configuration so the target synth/module is in a known state for consistent playback.
        SysEx,


        // ----------------------------
        // System Common (Live MIDI I/O)
        // ----------------------------

        // TimeCodeQuarterFrame (0xF1).
        // What it is / how it works:
        // Carries a portion of MIDI Time Code (MTC); multiple quarter-frame messages assemble into full timecode.
        // Affects playback?
        // Indirectly. Used for sync; does not change notes itself.
        // Example use:
        // A DAW locks to incoming MTC from video hardware; quarter frame messages keep MIDI aligned to video timecode.
        TimeCodeQuarterFrame,

        // SongPositionPointer (0xF2).
        // What it is / how it works:
        // 14-bit song position used by synced devices to locate within a song/pattern timeline (hardware sequencing contexts).
        // Affects playback?
        // Yes for synced devices. It tells slaves where to start/resume.
        // Example use:
        // Hitting “locate to bar 33” on the master sends SongPositionPointer so a slave drum machine starts at the right position.
        SongPositionPointer,

        // SongSelect (0xF3).
        // What it is / how it works:
        // Selects a song number (0–127) on devices that store multiple songs/pattern sets.
        // Affects playback?
        // Yes on supporting devices (changes which song/pattern will play).
        // Example use:
        // A controller selects “Song 12” on a hardware groovebox before sending Start.
        SongSelect,

        // TuneRequest (0xF6).
        // What it is / how it works:
        // Requests certain instruments/modules to run their tuning routine (rare today).
        // Affects playback?
        // Potentially yes, but not musically; it’s device maintenance/behavior.
        // Example use:
        // A vintage module receives TuneRequest before recording to ensure oscillators are calibrated.
        TuneRequest,


        // ----------------------------
        // System Real-Time (Live MIDI I/O)
        // ----------------------------

        // TimingClock (0xF8).
        // What it is / how it works:
        // Clock pulse used for tempo sync; sent repeatedly while running so slaves can sync arps/sequencers/LFOs.
        // Affects playback?
        // Yes in sync setups. Defines tempo timing for slaves, but is not a “BPM meta” message.
        // Example use:
        // A DAW sends TimingClock; a hardware delay pedal syncs tempo subdivisions to the incoming clock.
        TimingClock,

        // Start (0xFA).
        // What it is / how it works:
        // Tells slave devices to start playback from the beginning (often paired with SongPositionPointer for non-zero starts).
        // Affects playback?
        // Yes on synced devices (starts transport-driven playback).
        // Example use:
        // Press play on the DAW: it sends Start and the drum machine begins its pattern in time.
        Start,

        // Continue (0xFB).
        // What it is / how it works:
        // Resumes playback from the current position (typically after Stop).
        // Affects playback?
        // Yes on synced devices (resume).
        // Example use:
        // Stop mid-song, then hit play again and send Continue so the groovebox continues where it left off.
        Continue,

        // Stop (0xFC).
        // What it is / how it works:
        // Stops transport/clock-driven playback on slave devices.
        // Affects playback?
        // Yes (halts playback on synced devices).
        // Example use:
        // Hitting stop in the DAW sends Stop; arpeggiators and drum machines stop together.
        Stop,

        // ActiveSensing (0xFE).
        // What it is / how it works:
        // Keepalive message some devices send; if it stops arriving, receivers may silence notes to prevent hangs on disconnect.
        // Affects playback?
        // Indirectly. Connection safety feature; can prevent stuck notes when a connection dies.
        // Example use:
        // A keyboard sends ActiveSensing; if unplugged mid-note, the module stops notes instead of droning forever.
        ActiveSensing,

        // SystemReset.
        // What it is / how it works:
        // Resets a device’s MIDI system state (live MIDI concept; device-specific behavior).
        // Affects playback?
        // Yes (big hammer). Can wipe controller states, stop notes, reset modes—varies by device.
        // Example use:
        // A rig gets into a bad state; a reset message forces modules back to a known baseline (not something you’d casually put in a file).
        SystemReset,


        // ----------------------------
        // Safety / forward-compat
        // ----------------------------

        // Unknown.
        // What it is / how it works:
        // Placeholder for unrecognized or not-yet-supported event types when parsing/importing.
        // Affects playback?
        // No by itself; behavior depends on whether you drop it or preserve raw bytes for round-tripping.
        // Example use:
        // You load a file containing private sequencer data you don’t implement; store it as Unknown with payload so you can round-trip without losing data.
        Unknown
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
                Type = MidiEventType.PolyKeyPressure,
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
