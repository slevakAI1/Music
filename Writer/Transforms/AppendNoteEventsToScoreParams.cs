namespace Music.Writer
{
    /// <summary>
    /// Configuration extracted from Writer for easier processing by SetNotes.
    /// </summary>
    public sealed class AppendNoteEventsToScoreParams
    {
        // TARGETS
        public List<string> Parts { get; set; } = new();
        public List<int> Staffs { get; set; } = new();
        public int StartBar { get; set; }
        public int StartBeat { get; set; }

        // List of notes to insert
        public List<NoteEvent> NoteEvents { get; set; } = new();
    }

    /// <summary>
    /// Configuration extracted from Writer for easier processing by SetNotes.
    /// </summary>
    public sealed class Phrase
    {
        // These should be resolved up front!
        public string MidiPartName { get; set; }
        public string NotionPartName { get; set; }
        public string MidiProgramNumber { get; set; }
        public List<NoteEvent> NoteEvents { get; set; } = new();
    }
}



