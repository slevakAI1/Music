
// TO DO - check this - may be included already now


namespace Music.Writer
{
    /// <summary>
    /// Represents the type of live and advanced MIDI events
    /// </summary>

    public enum MidiEventType_LiveAndAdvanced
    {
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
}
