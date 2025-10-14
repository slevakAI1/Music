using System;

namespace Music.Errors
{
    internal class MidiImportException : Exception
    {
        public MidiImportException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}