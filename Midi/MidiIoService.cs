using Melanchall.DryWetMidi.Core;

namespace Music.MyMidi
{
    internal class MidiIoService 
    {
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

        // TODO THIS THROWS ERROR WHEN FILE EXISTS - HOW SHOULD IT WORK?
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