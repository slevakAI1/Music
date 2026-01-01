using Music.Generator;

// AI: purpose=global runtime singletons: holds current SongContext and transient WriterFormData; not persistent storage
// AI: invariants=only one active SongContext; callers expect null when no song loaded; do not auto-instantiate SongContext
// AI: deps=SongContext used throughout generator and UI; WriterFormData referenced by UI code and import/export flows
// AI: thread-safety=not synchronized; callers must marshal access to UI/main-thread when needed
// AI: security=Writer may contain user input; avoid logging/serializing it accidentally
// AI: change=when renaming these props update all references and any serialization or UI binding code

namespace Music
{
    public static class Globals
    {
        // AI: SongContext: authoritative in-memory song model; lifetime controlled by application; null==no song loaded
        public static SongContext? SongContext { get; set; }

        // AI: Writer: transient storage for WriterForm's state moved out of UI; treat as mutable UI state, not domain model
        public static Writer.WriterFormData? Writer { get; set; }
    }
}