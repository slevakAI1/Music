using System;
using System.Collections.Generic;

namespace Music.Generate
{
    // Holds the top-level sections for the score
    public sealed class SongStructure
    {
        private readonly List<ScoreDesign.TopLevelSection> _sections = new();
        public IReadOnlyList<ScoreDesign.TopLevelSection> Sections => _sections;

        public void Reset() => _sections.Clear();

        public ScoreDesign.TopLevelSection AddSection(
            ScoreDesign.TopLevelSectionType type,
            ScoreDesign.MeasureRange span,
            string? name = null,
            IEnumerable<string>? tags = null)
        {
            var sec = new ScoreDesign.TopLevelSection(
                Id: Guid.NewGuid().ToString("N"),
                Type: type,
                Span: span,
                Name: string.IsNullOrWhiteSpace(name) ? type.ToString() : name!,
                Tags: tags is null ? Array.Empty<string>() : new List<string>(tags).ToArray());

            _sections.Add(sec);
            return sec;
        }
    }
}