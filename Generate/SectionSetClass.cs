namespace Music.Generate
{
    // Score Sections
    public class SectionSetClass
    {
        public List<SectionClass> _sections = new();
        public IReadOnlyList<SectionClass> Sections => _sections;

        public void Reset() => _sections.Clear();

        public void Add(DesignEnums.eSectionType sectionType)
        {
            var section = new SectionClass();
            section.SectionType = sectionType;
            _sections.Add(section);
        }
    }
}