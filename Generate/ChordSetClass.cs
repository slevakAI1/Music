namespace Music.Generate
{
    // Holds the collection of chords (MusicXML harmony-compatible)
    public sealed class ChordSetClass
    {
        private readonly List<ChordClass> _chords = new();
        public IReadOnlyList<ChordClass> Chords => _chords;

        public void Reset() => _chords.Clear();

        public ChordClass AddChord(
            ScoreDesignClass.Step rootStep)
        {
            var chord = new ChordClass
            {
                ChordName = rootStep.ToString()
            };

            _chords.Add(chord);
            return chord;
        }

        public IReadOnlyList<ChordClass> AddDefaultChords()
        {
            AddChord(ScoreDesignClass.Step.C);
            return Chords;
        }
    }
}