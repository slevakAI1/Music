namespace Music.Services
{
    /// <summary>
    /// Handles MIDI file import/export for future MusicXML, device playback, and DAW round-trip.
    /// </summary>
    public interface IMidiIoService
    {
        MidiSongDocument ImportFromFile(string path);
        void ExportToFile(string path, MidiSongDocument doc);
    }
}