// AI: purpose=Global minimal app state holding the currently loaded MIDI document for UI and services.
// AI: invariants=CurrentSong is nullable; callers must null-check before use; this is process-wide singleton state.
// AI: thread-safety=Not synchronized. Access is expected on the UI thread or callers must handle concurrency.
// AI: deps=Referenced by UI, playback, and IO services; renaming this type or property breaks callers and tests.

namespace Music.MyMidi
{
    internal static class AppState
    {
        // AI: CurrentSong: set after import; mutable and may be replaced; avoid storing transient mutable playback state here.
        public static MidiSongDocument? CurrentSong { get; set; }
    }
}