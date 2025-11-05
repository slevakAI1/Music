using Music.Designer;
using MusicXml.Domain;
using Music.Generator;

namespace Music
{
    public static class Globals
    {
        public static Designer.Designer? Designer { get; set; }

        // Holds the currently loaded MusicXML score for application-wide access
        public static Score? Score { get; set; }

        // Persist GeneratorForm's data application-wide (refactor: moved from form instance to Globals)
        public static Generator.Generator? Generator { get; set; }
    }
}