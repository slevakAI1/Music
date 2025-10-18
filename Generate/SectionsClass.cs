namespace Music.Generate
{
    // Holds the top-level sections for the score
    public sealed class SectionsClass
    {
        private readonly List<ScoreDesign.Section> _sections = new();
        public IReadOnlyList<ScoreDesign.Section> Sections => _sections;

        public void Reset() => _sections.Clear();

        public ScoreDesign.Section AddSection(
            ScoreDesign.SectionType type,
            ScoreDesign.MeasureRange span,
            string? name = null,
            IEnumerable<string>? tags = null)
        {
            var sec = new ScoreDesign.Section(
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