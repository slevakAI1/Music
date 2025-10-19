namespace Music.Generate
{
    // Score Sections
    public sealed class SectionSetClass
    {
        private readonly List<ScoreDesignClass.Section> _sections = new();
        public IReadOnlyList<ScoreDesignClass.Section> Sections => _sections;

        public void Reset() => _sections.Clear();

        public ScoreDesignClass.Section AddSection(
            ScoreDesignClass.SectionType type,
            ScoreDesignClass.MeasureRange span,
            string? name = null,
            IEnumerable<string>? tags = null)
        {
            var sec = new ScoreDesignClass.Section(
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