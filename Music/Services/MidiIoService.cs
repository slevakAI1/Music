using Melanchall.DryWetMidi.Core;
using Music.Errors;
using System;
using System.IO;

namespace Music.Services
{
    internal class MidiIoService : IMidiIoService
    {
        public Music.MidiSongDocument ImportFromFile(string path)
        {
            try
            {
                var midiFile = MidiFile.Read(path);
                var doc = new Music.MidiSongDocument(midiFile)
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

        public void ExportToFile(string path, Music.MidiSongDocument doc)
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