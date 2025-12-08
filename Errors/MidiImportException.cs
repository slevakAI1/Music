// CLEAN - is this necessary or useful?

namespace Music
{
    internal class MidiImportException : Exception
    {
        public MidiImportException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}