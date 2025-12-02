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
    /// Uses a dictionary-based parameter system for flexibility.
    /// Use factory methods for creating event types with proper validation.
    /// </summary>
    public sealed record MidiEvent
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

        // ============================================================
        // Factory Methods - Meta Events
        // ============================================================

        /// <summary>
        /// Creates a text meta event (0x01).
        /// </summary>
        public static MidiEvent CreateText(long absoluteTime, string text) =>
            new()
            {
                Type = MidiEventType.Text,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = text }
            };

        /// <summary>
        /// Creates a copyright notice meta event (0x02).
        /// </summary>
        public static MidiEvent CreateCopyrightNotice(long absoluteTime, string text) =>
            new()
            {
                Type = MidiEventType.CopyrightNotice,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = text }
            };

        /// <summary>
        /// Creates a sequence/track name meta event (0x03).
        /// </summary>
        public static MidiEvent CreateSequenceTrackName(long absoluteTime, string name) =>
            new()
            {
                Type = MidiEventType.SequenceTrackName,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = name }
            };

        /// <summary>
        /// Creates an instrument name meta event (0x04).
        /// </summary>
        public static MidiEvent CreateInstrumentName(long absoluteTime, string name) =>
            new()
            {
                Type = MidiEventType.InstrumentName,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = name }
            };

        /// <summary>
        /// Creates a lyric meta event (0x05).
        /// </summary>
        public static MidiEvent CreateLyric(long absoluteTime, string lyric) =>
            new()
            {
                Type = MidiEventType.Lyric,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = lyric }
            };

        /// <summary>
        /// Creates a marker meta event (0x06).
        /// </summary>
        public static MidiEvent CreateMarker(long absoluteTime, string marker) =>
            new()
            {
                Type = MidiEventType.Marker,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = marker }
            };

        /// <summary>
        /// Creates a program name meta event (0x08).
        /// </summary>
        public static MidiEvent CreateProgramName(long absoluteTime, string programName) =>
            new()
            {
                Type = MidiEventType.ProgramName,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Text"] = programName }
            };

        /// <summary>
        /// Creates an end-of-track meta event (0x2F).
        /// </summary>
        public static MidiEvent CreateEndOfTrack(long absoluteTime = 0) =>
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
        public static MidiEvent CreateSetTempo(long absoluteTime, int? bpm = null, int? microsecondsPerQuarterNote = null)
        {
            var parameters = new Dictionary<string, object>();
            if (bpm.HasValue)
                parameters["BPM"] = bpm.Value;
            if (microsecondsPerQuarterNote.HasValue)
                parameters["MicrosecondsPerQuarterNote"] = microsecondsPerQuarterNote.Value;

            return new()
            {
                Type = MidiEventType.SetTempo,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Creates a time signature meta event (0x58).
        /// </summary>
        public static MidiEvent CreateTimeSignature(
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
        /// <param name="sharpsFlats">Number of sharps (positive) or flats (negative), -7 to +7</param>
        /// <param name="mode">0 for major, 1 for minor</param>
        public static MidiEvent CreateKeySignature(long absoluteTime, int sharpsFlats, int mode) =>
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
        public static MidiEvent CreateSequencerSpecific(long absoluteTime, byte[] data) =>
            new()
            {
                Type = MidiEventType.SequencerSpecific,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["Data"] = data }
            };

        // ============================================================
        // Factory Methods - Channel Voice Messages
        // ============================================================

        /// <summary>
        /// Creates a note-off event (0x8n).
        /// </summary>
        /// <param name="channel">MIDI channel (0-15)</param>
        /// <param name="noteNumber">MIDI note number (0-127). Can also provide "Note" for human-readable name.</param>
        /// <param name="velocity">Release velocity (0-127)</param>
        /// <param name="note">Optional human-readable note name (e.g., "C4")</param>
        public static MidiEvent CreateNoteOff(long absoluteTime, int channel, int noteNumber, int velocity = 0, string? note = null)
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
        /// <param name="channel">MIDI channel (0-15)</param>
        /// <param name="noteNumber">MIDI note number (0-127)</param>
        /// <param name="velocity">Note velocity (0-127, 0 is often treated as note-off)</param>
        /// <param name="note">Optional human-readable note name (e.g., "C4")</param>
        public static MidiEvent CreateNoteOn(long absoluteTime, int channel, int noteNumber, int velocity, string? note = null)
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
        /// <param name="channel">MIDI channel (0-15)</param>
        /// <param name="noteNumber">MIDI note number (0-127)</param>
        /// <param name="pressure">Pressure value (0-127)</param>
        /// <param name="note">Optional human-readable note name</param>
        public static MidiEvent CreatePolyKeyPressure(long absoluteTime, int channel, int noteNumber, int pressure, string? note = null)
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
        /// <param name="channel">MIDI channel (0-15)</param>
        /// <param name="controller">Controller number (0-127)</param>
        /// <param name="value">Controller value (0-127)</param>
        /// <param name="controllerName">Optional human-readable controller name</param>
        public static MidiEvent CreateControlChange(long absoluteTime, int channel, int controller, int value, string? controllerName = null)
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
        /// <param name="channel">MIDI channel (0-15)</param>
        /// <param name="program">Program number (0-127)</param>
        /// <param name="programName">Optional human-readable program name</param>
        public static MidiEvent CreateProgramChange(long absoluteTime, int channel, int program, string? programName = null)
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
        /// <param name="channel">MIDI channel (0-15)</param>
        /// <param name="pressure">Pressure value (0-127)</param>
        public static MidiEvent CreateChannelPressure(long absoluteTime, int channel, int pressure) =>
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
        /// <param name="channel">MIDI channel (0-15)</param>
        /// <param name="value">Pitch bend value (-8192 to +8191, 0 = center)</param>
        public static MidiEvent CreatePitchBend(long absoluteTime, int channel, int value) =>
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

        /// <summary>
        /// Creates an unknown event type for forward compatibility.
        /// </summary>
        public static MidiEvent CreateUnknown(long absoluteTime, byte[] rawData) =>
            new()
            {
                Type = MidiEventType.Unknown,
                AbsoluteTimeTicks = absoluteTime,
                Parameters = new() { ["RawData"] = rawData }
            };
    }
}
