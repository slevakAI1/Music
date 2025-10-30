using Music.Design;
using MusicXml.Domain;

namespace Music
{
    public static class Globals
    {
        public static DesignerClass? Design { get; set; }

        // Holds the currently loaded MusicXML score for application-wide access
        public static Score? Score { get; set; }
    }
}