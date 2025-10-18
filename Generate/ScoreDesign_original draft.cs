//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Music.Design
//{
//    /// <summary>
//    /// A design-layer document that decorates a score with high-level, notation-aware sections
//    /// and flexible annotations (e.g., chords, rhythm patterns). Decoupled from MusicXML types.
//    /// </summary>
//    public sealed class ScoreDesign
//    {
//        public string DesignId { get; }
//        public string? SourcePath { get; init; }
//        public string? SourceHash { get; init; }

//        private readonly List<Section> _sections = new();
//        private readonly List<Annotation> _annotations = new();
//        private readonly Dictionary<string, string> _metadata = new(StringComparer.OrdinalIgnoreCase);

//        public IReadOnlyList<Section> Sections => _sections;
//        public IReadOnlyList<Annotation> Annotations => _annotations;
//        public IReadOnlyDictionary<string, string> Metadata => _metadata;

//        public ScoreDesign(string? sourcePath = null, string? sourceHash = null, string? designId = null)
//        {
//            DesignId = designId ?? Guid.NewGuid().ToString("N");
//            SourcePath = sourcePath;
//            SourceHash = sourceHash;
//        }

//        // Sections
//        public Section AddSection(string name, SectionType type, Range<MusicalPosition> span, VoiceScope? scope = null, IEnumerable<string>? tags = null)
//        {
//            var sec = new Section(
//                Id: Guid.NewGuid().ToString("N"),
//                Name: name,
//                Type: type,
//                Span: span,
//                Scope: scope ?? VoiceScope.All(),
//                Tags: (tags ?? Array.Empty<string>()).ToArray());

//            _sections.Add(sec);
//            return sec;
//        }

//        // Annotations (extensible: add specific types like ChordAnnotation below and pass instances here)
//        public T AddAnnotation<T>(T annotation) where T : Annotation
//        {
//            if (annotation is null) throw new ArgumentNullException(nameof(annotation));
//            _annotations.Add(annotation);
//            return annotation;
//        }

//        public void SetMetadata(string key, string value)
//        {
//            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Metadata key must not be empty.", nameof(key));
//            _metadata[key] = value;
//        }

//        // --------------- Core Types (nested for cohesion and single-file drop-in) ----------------

//        /// <summary>
//        /// Musical position using measure and fractional beat (notation axis).
//        /// </summary>
//        public readonly record struct MusicalPosition(int MeasureNumber, Fraction Beat)
//        {
//            public static MusicalPosition FromBeat(int measure, int numerator, int denominator) =>
//                new(measure, new Fraction(numerator, denominator));
//        }

//        /// <summary>
//        /// Simple rational number for beats (e.g., 1/4 = quarter, 3/8 = three eighths).
//        /// </summary>
//        public readonly record struct Fraction(int Numerator, int Denominator)
//        {
//            public bool IsValid => Denominator > 0;

//            public Fraction Normalize()
//            {
//                if (!IsValid) return this;
//                var n = Numerator;
//                var d = Denominator;
//                var g = Gcd(Math.Abs(n), d);
//                return new Fraction(n / g, d / g);

//                static int Gcd(int a, int b)
//                {
//                    while (b != 0) { var t = b; b = a % b; a = t; }
//                    return Math.Max(a, 1);
//                }
//            }

//            public override string ToString() => $"{Numerator}/{Denominator}";
//        }

//        /// <summary>
//        /// Generic range with optional open end and inclusive-end semantics.
//        /// </summary>
//        public readonly record struct Range<T>(T Start, T? End, bool InclusiveEnd = true)
//        {
//            public bool IsOpenEnded => End is null;
//        }

//        /// <summary>
//        /// Targets parts/staves/voices by identifiers. Supports "All" flags.
//        /// </summary>
//        public sealed record VoiceScope(
//            IReadOnlySet<string> Parts,
//            IReadOnlySet<int> Staves,
//            IReadOnlySet<int> Voices,
//            bool AllParts = false,
//            bool AllStaves = false,
//            bool AllVoices = false)
//        {
//            public static VoiceScope All() =>
//                new(HashSetEmpty<string>(), HashSetEmpty<int>(), HashSetEmpty<int>(), true, true, true);

//            public static VoiceScope ForPart(string partId) =>
//                new(HashSetOf(partId), HashSetEmpty<int>(), HashSetEmpty<int>());

//            public static VoiceScope ForVoice(string partId, int voiceNumber) =>
//                new(HashSetOf(partId), HashSetEmpty<int>(), HashSetOf(voiceNumber));

//            private static IReadOnlySet<T> HashSetEmpty<T>() => new HashSet<T>();
//            private static IReadOnlySet<T> HashSetOf<T>(T item) => new HashSet<T> { item };
//        }

//        /// <summary>
//        /// Hierarchical, named musical region (e.g., Chorus, Verse, Intro), with scope and tags.
//        /// </summary>
//        public sealed record Section(
//            string Id,
//            string Name,
//            SectionType Type,
//            Range<MusicalPosition> Span,
//            VoiceScope Scope,
//            string[] Tags)
//        {
//            private readonly List<Section> _children = new();
//            public IReadOnlyList<Section> Children => _children;

//            public Section AddChild(string name, SectionType type, Range<MusicalPosition> span, VoiceScope? scope = null, IEnumerable<string>? tags = null)
//            {
//                var child = new Section(
//                    Id: Guid.NewGuid().ToString("N"),
//                    Name: name,
//                    Type: type,
//                    Span: span,
//                    Scope: scope ?? Scope,
//                    Tags: (tags ?? Array.Empty<string>()).ToArray());
//                _children.Add(child);
//                return child;
//            }
//        }

//        public enum SectionType
//        {
//            Intro,
//            Verse,
//            Refrain,
//            Chorus,
//            Bridge,
//            Ending,
//            Outro,
//            Custom
//        }

//        /// <summary>
//        /// Base type for annotations that attach behavior/intent to a range and scope.
//        /// </summary>
//        public abstract record Annotation(
//            string Id,
//            string Kind,
//            Range<MusicalPosition> Span,
//            VoiceScope Scope,
//            int Priority = 0,
//            string[]? Tags = null)
//        {
//            public string[] EffectiveTags { get; init; } = Tags ?? Array.Empty<string>();
//        }

//        /// <summary>
//        /// Example: chord-level intent over a span (bar, partial bar, multi-bar).
//        /// </summary>
//        public sealed record ChordAnnotation(
//            string Id,
//            string Symbol,                                // e.g., "Cmaj7/G"
//            Range<MusicalPosition> Span,
//            VoiceScope Scope,
//            string? Key = null,                            // optional tonic/context
//            string[]? Functions = null,                    // e.g., "T", "SD", "D"
//            int Priority = 0,
//            string[]? Tags = null)
//            : Annotation(Id, "Chord", Span, Scope, Priority, Tags)
//        {
//            public string? KeyCenter { get; init; } = Key;
//            public string[] HarmonicFunctions { get; init; } = Functions ?? Array.Empty<string>();
//        }
//    }
//}