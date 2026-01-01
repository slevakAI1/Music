// AI: purpose=Simple MIDI import/export service wrapping DryWetMidi to produce/consume MidiSongDocument.
// AI: invariants=Import returns a MidiSongDocument built from MidiFile.Read; Export writes MidiSongDocument.Raw (a MidiFile) to disk.
// AI: deps=Depends on Melanchall.DryWetMidi.Core and MidiSongDocument.Raw being a MidiFile; changing Raw breaks this service.
// AI: errors=All exceptions are wrapped in MidiImportException for consistent caller diagnostics; callers should inspect InnerException.
// AI: thread-safety=Not thread-safe; perform file I/O off the UI thread to avoid blocking.

using Melanchall.DryWetMidi.Core;

namespace Music.MyMidi
{
    public class MidiIoService 
    {
        // AI: ImportFromFile uses DryWetMidi's MidiFile.Read and returns a MidiSongDocument; errors are translated to MidiImportException.
        public MidiSongDocument ImportFromFile(string path)
        {
            try
            {
                var midiFile = MidiFile.Read(path);
                var doc = new MidiSongDocument(midiFile)
                {
                    FileName = Path.GetFileName(path)
                };
                return doc;
            }
            catch (Exception ex)
            {
                throw new MidiImportException($"Failed to import MIDI file: {path}", ex);
            }
        }

        // AI: ExportToFile writes the MidiFile stored in MidiSongDocument.Raw to the provided path and wraps exceptions.
        // AI: note=If underlying Write fails for existing files callers may choose to delete/backup before calling.
        public void ExportToFile(string path, MidiSongDocument doc)
        {
            try
            {
                doc.Raw.Write(path);
            }
            catch (Exception ex)
            {
                throw new MidiImportException($"Failed to export MIDI file: {path}", ex);
            }
        }
    }
}