using System;

namespace Music
{
    internal class MidiImportException : Exception
    {
        public MidiImportException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}