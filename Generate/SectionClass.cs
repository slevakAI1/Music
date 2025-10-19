namespace Music.Generate
{
    public class SectionClass
    {
        public enum eSectionType { Intro, Verse, Chorus, Solo, Bridge, Outro, Custom }

        public eSectionType SectionType {get; set; }
    }
}
