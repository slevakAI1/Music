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
    }
}