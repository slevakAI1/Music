// AI: purpose=wrapper for exceptions encountered during MIDI import; used to signal import-specific failures
// AI: invariants=internal-only; preserve inner exception and original stacktrace; avoid adding behavior here
// AI: change=if adding metadata (file,line,track) update import pipeline and any catch blocks that inspect exception
// AI: security=may include file/path info; sanitize before sending to telemetry or logs exposed externally

namespace Music
{
    internal class MidiImportException : Exception
    {
        public MidiImportException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}