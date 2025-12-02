namespace Music.Writer
{
    /// <summary>
    /// Configuration extracted from Writer for easier processing by SetNotes.
    /// </summary>
    public sealed class Phrase
    {
        // These should be resolved up front!
        public string MidiPartName { get; set; }
        //public string NotionPartName { get; set; }
        public byte MidiProgramNumber { get; set; }
        public List<NoteEvent> NoteEvents { get; set; } = new();
    }
}
