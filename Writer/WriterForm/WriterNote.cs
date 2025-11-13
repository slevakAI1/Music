using System.Collections.Generic;

namespace Music.Writer
{
    /// <summary>
    /// Represents a single note. May or may not be part of a chord.
    /// </summary>
    public sealed class WriterNote
    {
        public char Step { get; set; }
        public int Octave { get; set; }
        public int Duration { get; set; }
        public bool IsChord { get; set; }
        public bool IsRest { get; set; }
        public int Alter { get; set; }

        // Tuplet properties
        public string? TupletNumber { get; set; }
        public int TupletActualNotes { get; set; }  // The 'm' in m:n (e.g., 3 in a triplet)
        public int TupletNormalNotes { get; set; }  // The 'n' in m:n (e.g., 2 in a triplet)
    }
}