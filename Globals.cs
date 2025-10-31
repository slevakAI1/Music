using Music.Designer;
using MusicXml.Domain;
using Music.Generator;

namespace Music
{
    public static class Globals
    {
        public static DesignerData? Design { get; set; }

        // Holds the currently loaded MusicXML score for application-wide access
        public static Score? Score { get; set; }

        // Persist GeneratorForm's data application-wide (refactor: moved from form instance to Globals)
        public static GeneratorData? GenerationData { get; set; }
    }
}