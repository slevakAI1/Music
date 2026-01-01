namespace Music.Generator
{
    /// <summary>
    /// Represents the type of MIDI event (channel, meta, or system exclusive).
    /// </summary>

    public enum PartTrackEventType
    {
        // ----------------------------
        // Meta events (SMF track-level)
        // ----------------------------

        /// <summary>
        /// Sequence number meta event (0x00).
        /// Specifies the sequence number in multi-sequence MIDI files.
        /// </summary>
        SequenceNumber,

        // Text (0x01).
        Text,

        // CopyrightNotice (0x02).
        CopyrightNotice,

        // SequenceTrackName (0x03).
        SequenceTrackName,

        // InstrumentName (0x04).
        InstrumentName,

        // Lyric (0x05).
        Lyric,

        // Marker (0x06).
        Marker,

        /// <summary>
        /// Cue point meta event (0x07).
        /// Similar to markers but typically used for external device cues.
        /// </summary>
        CuePoint,

        // ProgramName (0x08).
        ProgramName,

        /// <summary>
        /// Device name meta event (0x09).
        /// Specifies the device or port name for playback routing.
        /// </summary>
        DeviceName,

        /// <summary>
        /// MIDI channel prefix meta event (0x20).
        /// Associates subsequent meta events with a specific MIDI channel.
        /// </summary>
        MidiChannelPrefix,

        /// <summary>
        /// MIDI port meta event (0x21).
        /// Specifies which MIDI port/device the track should use.
        /// </summary>
        MidiPort,

        // EndOfTrack (0x2F).
        EndOfTrack,

        // SetTempo (0x51).
        SetTempo,

        /// <summary>
        /// SMPTE offset meta event (0x54).
        /// Specifies SMPTE time code offset for synchronization.
        /// </summary>
        SmpteOffset,

        // TimeSignature (0x58).
        TimeSignature,

        // KeySignature (0x59).
        KeySignature,

        // SequencerSpecific (0x7F).
        SequencerSpecific,

        /// <summary>
        /// Unknown meta event.
        /// Catch-all for unrecognized meta events, preserves raw data for round-tripping.
        /// </summary>
        UnknownMeta,

        // ----------------------------
        // Channel voice messages
        // ----------------------------

        // NoteOff (0x8n).
        NoteOff,

        // NoteOn (0x9n).
        NoteOn,

        // PolyKeyPressure (0xAn).
        PolyKeyPressure,

        // ControlChange (0xBn).
        ControlChange,

        // ProgramChange (0xCn).
        ProgramChange,

        // ChannelPressure (0xDn).
        ChannelPressure,

        // PitchBend (0xEn).
        PitchBend,

        // ----------------------------
        // System Exclusive Events
        // ----------------------------

        /// <summary>
        /// Normal System Exclusive event (0xF0).
        /// Manufacturer-specific data for configuring synthesizers and other devices.
        /// </summary>
        NormalSysEx,

        /// <summary>
        /// Escape System Exclusive event (0xF7).
        /// Used for arbitrary data or continuing SysEx messages.
        /// </summary>
        EscapeSysEx,

        // ----------------------------
        // System Common Messages
        // ----------------------------

        /// <summary>
        /// MTC Quarter Frame (0xF1).
        /// MIDI Time Code quarter-frame message for synchronization.
        /// </summary>
        MtcQuarterFrame,

        /// <summary>
        /// ProposedSong Position Pointer (0xF2).
        /// Specifies the song position in 16th notes.
        /// </summary>
        SongPositionPointer,

        /// <summary>
        /// ProposedSong Select (0xF3).
        /// Selects a song/sequence number.
        /// </summary>
        SongSelect,

        /// <summary>
        /// Tune Request (0xF6).
        /// Requests analog synthesizers to tune their oscillators.
        /// </summary>
        TuneRequest,

        // ----------------------------
        // System Real-Time Messages
        // ----------------------------

        /// <summary>
        /// Timing Clock (0xF8).
        /// Sent 24 times per quarter note for tempo synchronization.
        /// </summary>
        TimingClock,

        /// <summary>
        /// Start (0xFA).
        /// Starts sequence playback from the beginning.
        /// </summary>
        Start,

        /// <summary>
        /// Continue (0xFB).
        /// Continues sequence playback from the current position.
        /// </summary>
        Continue,

        /// <summary>
        /// Stop (0xFC).
        /// Stops sequence playback.
        /// </summary>
        Stop,

        /// <summary>
        /// Active Sensing (0xFE).
        /// Heartbeat message to indicate connection is alive.
        /// </summary>
        ActiveSensing,

        /// <summary>
        /// System Reset (0xFF).
        /// Resets all devices to power-on state.
        /// Note: 0xFF is also used as meta event marker in SMF.
        /// </summary>
        SystemReset,

        // ----------------------------
        // Safety / forward-compat
        // ----------------------------

        // Unknown.
        Unknown
    }
}
