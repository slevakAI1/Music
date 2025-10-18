using MusicXml;
using MusicXml.Domain;

namespace Music.Services
{
    public class MusicXmlService : IMusicXmlService
    {
        /// <summary>
        /// The last MusicXML score successfully imported via this service.
        /// </summary>
        public Score? LastImportedScore { get; private set; }

        /// <summary>
        /// The path of the last imported MusicXML file.
        /// </summary>
        public string? LastImportedPath { get; private set; }

        /// <summary>
        /// Parses a MusicXML file using MusicXml.MusicXmlParser.GetScore(filePath).
        /// </summary>
        /// <param name="path">Absolute or relative path to a MusicXML file.</param>
        /// <returns>The parsed MusicXml.Domain.Score.</returns>
        public Score ImportFromMusicXml(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be null or empty.", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException("MusicXML file not found.", path);

            LastImportedScore = MusicXmlParser.GetScore(path);
            LastImportedPath = path;
            return LastImportedScore;
        }

        /// <summary>
        /// Exports the last imported MusicXML file to the provided path.
        /// This copies the original MusicXML file to the destination.
        /// </summary>
        public void ExportLastImportedScore(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be null or empty.", nameof(path));

            if (LastImportedPath is null || !File.Exists(LastImportedPath))
                throw new InvalidOperationException("No MusicXML score is available to export. Import a MusicXML file first.");

            // Overwrite the destination with the original MusicXML file.
            File.Copy(LastImportedPath, path, overwrite: true);
        }

        /// <summary>
        /// Exports the given MIDI document to a MusicXML file.
        /// Note: musicxml.net 3.1.0 exposes a parser; implementing a MIDI->MusicXML converter is out of scope here.
        /// </summary>
        public void ExportToMusicXml(string path, MidiSongDocument doc)
        {
            throw new NotImplementedException(
                "Export to MusicXML is not implemented. The musicxml.net 3.1.0 package provides parsing support. " +
                "A MIDI->MusicXML conversion step would be required to implement this method.");
        }
    }
}