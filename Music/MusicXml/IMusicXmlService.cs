using MusicXml.Domain;
using MusicXml;

namespace MusicXml
{
    public interface IMusicXmlService
    {
        // Import returns the parsed MusicXml.Domain.Score and also stores it internally.
        Score ImportFromMusicXml(string path);

        // Path of the last imported MusicXML file.
        string? LastImportedPath { get; }

        // Export the last imported MusicXML score file to the specified path.
        void ExportLastImportedScore(string path);

        // Export remains unimplemented for MIDI->MusicXML.
        //void ExportToMusicXml(string path, MidiSongDocument doc);
    }
}