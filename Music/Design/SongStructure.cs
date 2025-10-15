using System;
using System.Collections.Generic;

namespace Music.Design
{
    /// <summary>
    /// Minimal, score-wide structure (top-level only). No voice/staff/part targeting.
    /// </summary>
    public sealed class SongStructure
    {
        public string DesignId { get; }
        //public string? SourcePath { get; init; }
        //public string? SourceHash { get; init; }

        private readonly List<TopLevelSection> _sections = new();
        public IReadOnlyList<TopLevelSection> Sections => _sections;

        // High-level collection of all voices used in the song (per MusicXML: voice is a string identifier).
        private readonly List<Voice> _voices = new();
        public IReadOnlyList<Voice> Voices => _voices;

        // High-level collection of chords (MusicXML 'harmony' compatible: root/kind/bass).
        private readonly List<Chord> _chords = new();
        public IReadOnlyList<Chord> Chords => _chords;

        public SongStructure(string? designId = null) // string? sourcePath = null, string? sourceHash = null, 
        {
            DesignId = designId ?? Guid.NewGuid().ToString("N");
        //  SourcePath = sourcePath;
        //  SourceHash = sourceHash;
        }

        /// <summary>
        /// Build the standard top-level structure and return a printable summary.
        /// Structure: Intro → Verse → Chorus → Verse → Chorus → Bridge → Chorus → Outro
        /// Each section spans 4 measures.
        /// </summary>
        public string CreateStructure()
        {
            _sections.Clear();

            int measure = 1;
            void Add(TopLevelSectionType t, int lengthMeasures)
            {
                var span = new MeasureRange(measure, measure + lengthMeasures - 1, true);
                AddSection(t, span);
                measure += lengthMeasures;
            }

            Add(TopLevelSectionType.Intro, 4);
            Add(TopLevelSectionType.Verse, 8);
            Add(TopLevelSectionType.Chorus, 8);
            Add(TopLevelSectionType.Verse, 8);
            Add(TopLevelSectionType.Chorus, 8);
            Add(TopLevelSectionType.Bridge, 8);
            Add(TopLevelSectionType.Chorus, 8);
            Add(TopLevelSectionType.Outro, 4);

            // Build a simple "Intro → Verse → ..." summary string with bar counts
            var names = new List<string>(_sections.Count);
            foreach (var s in _sections)
            {
                int bars = s.Span.EndMeasure is int end
                    ? (s.Span.InclusiveEnd ? end - s.Span.StartMeasure + 1 : end - s.Span.StartMeasure)
                    : 0;
                names.Add($"{s.Type}, {bars}");
            }
            return string.Join("\r\n", names);
        }

        /// <summary>
        /// Add a top-level section that applies to the entire score.
        /// </summary>
        public TopLevelSection AddSection(TopLevelSectionType type, MeasureRange span, string? name = null, IEnumerable<string>? tags = null)
        {
            var sec = new TopLevelSection(
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
        /// Notes:
        /// - MusicXML harmony does not encode octave; voicing is applied later when rendering notes.
        /// - rootAlter and bassAlter use standard semitone offsets: -1=flat, 0=natural, +1=sharp, etc.
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

        /// <summary>
        /// Inclusive measure range; end may be open-ended (null).
        /// </summary>
        public readonly record struct MeasureRange(int StartMeasure, int? EndMeasure, bool InclusiveEnd = true)
        {
            public bool IsOpenEnded => EndMeasure is null;
            public static MeasureRange Single(int measure) => new(measure, measure, true);
        }

        /// <summary>
        /// One top-level structural section (e.g., Verse, Chorus) with an optional name and tags.
        /// </summary>
        public sealed record TopLevelSection(
            string Id,
            TopLevelSectionType Type,
            MeasureRange Span,
            string Name,
            string[] Tags
        );

        /// <summary>
        /// High-level voice entry capturing the MusicXML 'voice' value.
        /// </summary>
        public sealed record Voice(
            string Id,
            string Value
        );

        /// <summary>
        /// High-level chord entry compatible with MusicXML 'harmony' element.
        /// Octave/voicing are intentionally omitted (they belong to note rendering).
        /// </summary>
        public sealed record Chord(
            string Id,
            Step RootStep,
            int RootAlter,
            ChordKind Kind,
            Step? BassStep,
            int? BassAlter,
            string Name
        );

        /// <summary>
        /// Top-level structural labels for a song/score.
        /// </summary>
        public enum TopLevelSectionType
        {
            Intro,
            Verse,
    //      Refrain,
            Chorus,
            Solo,
            Bridge,
    //      Ending,
            Outro,
            Custom
        }

        /// <summary>
        /// Diatonic pitch steps used by MusicXML (A–G).
        /// </summary>
        public enum Step
        {
            A, B, C, D, E, F, G
        }

        /// <summary>
        /// Chord kinds mapped to MusicXML 'kind' values.
        /// This list can be extended as needed.
        /// </summary>
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