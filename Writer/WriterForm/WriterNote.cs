using System.Collections.Generic;

namespace Music.Writer
{
    /// <summary>
    /// Represents a single note or chord with all its musical properties.
    /// </summary>
    public sealed class WriterNote
    {
        public char Step { get; set; }
        public int Octave { get; set; }
        public int NoteValue { get; set; }
        public int NumberOfNotes { get; set; }
        public bool IsChord { get; set; }
        public bool IsRest { get; set; }
        public int Alter { get; set; }
        public List<ChordConverter.ChordNote>? ChordNotes { get; set; }
    }
}