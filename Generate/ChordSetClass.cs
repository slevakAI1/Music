namespace Music.Generate
{
    // Holds the collection of chords (MusicXML harmony-compatible)
    public sealed class ChordSetClass
    {
        private readonly List<ScoreDesignClass.Chord> _chords = new();
        public IReadOnlyList<ScoreDesignClass.Chord> Chords => _chords;

        public void Reset() => _chords.Clear();

        public ScoreDesignClass.Chord AddChord(
            ScoreDesignClass.Step rootStep,
            int rootAlter,
            ScoreDesignClass.ChordKind kind,
            ScoreDesignClass.Step? bassStep = null,
            int? bassAlter = null,
            string? name = null)
        {
            foreach (var c in _chords)
            {
                if (c.RootStep == rootStep &&
                    c.RootAlter == rootAlter &&
                    c.Kind == kind &&
                    c.BassStep == bassStep &&
                    c.BassAlter == bassAlter)
                {
                    return c;
                }
            }

            var chord = new ScoreDesignClass.Chord(
                Id: Guid.NewGuid().ToString("N"),
                RootStep: rootStep,
                RootAlter: rootAlter,
                Kind: kind,
                BassStep: bassStep,
                BassAlter: bassAlter,
                Name: string.IsNullOrWhiteSpace(name)
                    ? BuildChordDisplayName(rootStep, rootAlter, kind, bassStep, bassAlter)
                    : name!);

            _chords.Add(chord);
            return chord;
        }

        public IReadOnlyList<ScoreDesignClass.Chord> AddDefaultChords()
        {
            AddChord(ScoreDesignClass.Step.C, 0, ScoreDesignClass.ChordKind.Major, name: "C");
            return Chords;
        }

        private static string BuildChordDisplayName(ScoreDesignClass.Step rootStep, int rootAlter, ScoreDesignClass.ChordKind chordKind, ScoreDesignClass.Step? bassStep, int? bassAlter)
        {
            static string StepToText(ScoreDesignClass.Step s) => s switch
            {
                ScoreDesignClass.Step.A => "A",
                ScoreDesignClass.Step.B => "B",
                ScoreDesignClass.Step.C => "C",
                ScoreDesignClass.Step.D => "D",
                ScoreDesignClass.Step.E => "E",
                ScoreDesignClass.Step.F => "F",
                ScoreDesignClass.Step.G => "G",
                _ => "?"
            };

            static string AlterToText(int alter) => alter switch
            {
                < 0 => new string('b', -alter),
                > 0 => new string('#', alter),
                _ => ""
            };

            static string KindToSuffix(ScoreDesignClass.ChordKind k) => k switch
            {
                ScoreDesignClass.ChordKind.Major => "",
                ScoreDesignClass.ChordKind.Minor => "m",
                ScoreDesignClass.ChordKind.Augmented => "aug",
                ScoreDesignClass.ChordKind.Diminished => "dim",
                ScoreDesignClass.ChordKind.DominantSeventh => "7",
                ScoreDesignClass.ChordKind.MajorSeventh => "maj7",
                ScoreDesignClass.ChordKind.MinorSeventh => "m7",
                ScoreDesignClass.ChordKind.SuspendedFourth => "sus4",
                ScoreDesignClass.ChordKind.SuspendedSecond => "sus2",
                ScoreDesignClass.ChordKind.Power => "5",
                ScoreDesignClass.ChordKind.HalfDiminishedSeventh => "m7b5",
                ScoreDesignClass.ChordKind.DiminishedSeventh => "dim7",
                _ => ""
            };

            var root = StepToText(rootStep) + AlterToText(rootAlter);
            var kindSuffix = KindToSuffix(chordKind);
            var bass = bassStep is null ? "" : "/" + StepToText(bassStep.Value) + (bassAlter.HasValue ? AlterToText(bassAlter.Value) : "");
            return root + kindSuffix + bass;
        }
    }
}