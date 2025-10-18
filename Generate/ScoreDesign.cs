namespace Music.Generate
{
    /// <summary>
    /// Minimal, score-wide structure (top-level only). No voice/staff/part targeting.
    /// </summary>
    public sealed class ScoreDesign
    {
        public string DesignId { get; }
        //public string? SourcePath { get; init; }
        //public string? SourceHash { get; init; }

        private readonly List<Section> _sections = new();
        public IReadOnlyList<Section> Sections => _sections;

        // High-level collection of all voices used in the song (per MusicXML: voice is a string identifier).
        private readonly List<Voice> _voices = new();
        public IReadOnlyList<Voice> Voices => _voices;

        // High-level collection of chords (MusicXML 'harmony' compatible: root/kind/bass).
        private readonly List<Chord> _chords = new();
        public IReadOnlyList<Chord> Chords => _chords;

        public ScoreDesign(string? designId = null)
        {
            DesignId = designId ?? Guid.NewGuid().ToString("N");
        }

        // Allow managers to rebuild section plans
        public void ResetSections() => _sections.Clear();

        /// <summary>
        /// Add a top-level section that applies to the entire score.
        /// </summary>
        public Section AddSection(TopLevelSectionType type, MeasureRange span, string? name = null, IEnumerable<string>? tags = null)
        {
            var sec = new Section(
                Id: Guid.NewGuid().ToString("N"),
                Type: type,
                Span: span,
                Name: string.IsNullOrWhiteSpace(name) ? type.ToString() : name!,
                Tags: tags is null ? Array.Empty<string>() : new List<string>(tags).ToArray());

            _sections.Add(sec);
            return sec;
        }

        /// <summary>
        /// Add a voice identifier to the song's voice collection (MusicXML 'voice' value).
        /// Returns the created or existing voice entry.
        /// </summary>
        public Voice AddVoice(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Voice value must not be null or empty.", nameof(value));

            // Prevent duplicate voice values; return existing if present.
            foreach (var v in _voices)
            {
                if (string.Equals(v.Value, value, StringComparison.Ordinal))
                    return v;
            }

            var voice = new Voice(
                Id: Guid.NewGuid().ToString("N"),
                Value: value);

            _voices.Add(voice);
            return voice;
        }

        /// <summary>
        /// Add the default voice set for this app.
        /// Calls AddVoice for each entry and returns the full voice list.
        /// </summary>
        public IReadOnlyList<Voice> AddVoices()
        {
            AddVoice("Guitar");
            AddVoice("Drum Set");
            AddVoice("Keyboard");
            AddVoice("Base Guitar"); // per requirement
            return Voices;
        }

        /// <summary>
        /// Add a chord to the song's chord set (compatible with MusicXML 'harmony').
        /// Prevents duplicates by matching root/kind/bass. Returns the created or existing entry.
        /// </summary>
        public Chord AddChord(
            Step rootStep,
            int rootAlter,
            ChordKind kind,
            Step? bassStep = null,
            int? bassAlter = null,
            string? name = null)
        {
            // Check for existing equivalent chord (same identity fields).
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

            var chord = new Chord(
                Id: Guid.NewGuid().ToString("N"),
                RootStep: rootStep,
                RootAlter: rootAlter,
                Kind: kind,
                BassStep: bassStep,
                BassAlter: bassAlter,
                Name: string.IsNullOrWhiteSpace(name) ? BuildChordDisplayName(rootStep, rootAlter, kind, bassStep, bassAlter) : name!);

            _chords.Add(chord);
            return chord;
        }

        /// <summary>
        /// Initialize a default chord set. For now, adds a C major chord ("Middle C chord" as a class of harmony).
        /// Returns the full chord list.
        /// </summary>
        public IReadOnlyList<Chord> CreateChordSet()
        {
            // Middle C chord: represent as a C major harmony (octave/voicing applied later during note rendering).
            AddChord(Step.C, 0, ChordKind.Major, name: "C");
            return Chords;
        }

        private static string BuildChordDisplayName(Step rootStep, int rootAlter, ChordKind chordKind, Step? bassStep, int? bassAlter)
        {
            static string StepToText(Step s) => s switch
            {
                Step.A => "A",
                Step.B => "B",
                Step.C => "C",
                Step.D => "D",
                Step.E => "E",
                Step.F => "F",
                Step.G => "G",
                _ => "?"
            };

            static string AlterToText(int alter) => alter switch
            {
                < 0 => new string('b', -alter),
                > 0 => new string('#', alter),
                _ => ""
            };

            static string KindToSuffix(ChordKind k) => k switch
            {
                ChordKind.Major => "",
                ChordKind.Minor => "m",
                ChordKind.Augmented => "aug",
                ChordKind.Diminished => "dim",
                ChordKind.DominantSeventh => "7",
                ChordKind.MajorSeventh => "maj7",
                ChordKind.MinorSeventh => "m7",
                ChordKind.SuspendedFourth => "sus4",
                ChordKind.SuspendedSecond => "sus2",
                ChordKind.Power => "5",
                ChordKind.HalfDiminishedSeventh => "m7b5",
                ChordKind.DiminishedSeventh => "dim7",
                _ => ""
            };

            var root = StepToText(rootStep) + AlterToText(rootAlter);
            var kindSuffix = KindToSuffix(chordKind);
            var bass = bassStep is null ? "" : "/" + StepToText(bassStep.Value) + (bassAlter.HasValue ? AlterToText(bassAlter.Value) : "");
            return root + kindSuffix + bass;
        }

        public readonly record struct MeasureRange(int StartMeasure, int? EndMeasure, bool InclusiveEnd = true)
        {
            public bool IsOpenEnded => EndMeasure is null;
            public static MeasureRange Single(int measure) => new(measure, measure, true);
        }

        public sealed record Section(
            string Id,
            TopLevelSectionType Type,
            MeasureRange Span,
            string Name,
            string[] Tags
        );

        public sealed record Voice(
            string Id,
            string Value
        );

        public sealed record Chord(
            string Id,
            Step RootStep,
            int RootAlter,
            ChordKind Kind,
            Step? BassStep,
            int? BassAlter,
            string Name
        );

        public enum TopLevelSectionType
        {
            Intro,
            Verse,
            Chorus,
            Solo,
            Bridge,
            Outro,
            Custom
        }

        public enum Step
        {
            A, B, C, D, E, F, G
        }

        public enum ChordKind
        {
            Major,
            Minor,
            Augmented,
            Diminished,
            DominantSeventh,
            MajorSeventh,
            MinorSeventh,
            SuspendedFourth,
            SuspendedSecond,
            Power,
            HalfDiminishedSeventh,
            DiminishedSeventh
        }
    }
}