namespace Music.Writer
{
    /// <summary>
    /// Configuration extracted from Writer for easier processing by SetNotes.
    /// </summary>
    public sealed class AppendNotesParams
    {
        // TARGETS
        public List<string> Parts { get; set; } = new();
        public List<int> Staffs { get; set; } = new();
        public int StartBar { get; set; }
        public int StartBeat { get; set; }

        // List of notes to insert
        public List<PitchEvent> Notes { get; set; } = new();
    }
}
