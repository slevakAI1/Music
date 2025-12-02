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
        public List<PhraseNote> NoteEvents { get; set; } = new();
    }
}



