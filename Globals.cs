using Music.Design;
using MusicXml.Domain;

namespace Music
{
    public static class Globals
    {
        public static DesignClass? Design { get; set; }

        // Holds the currently loaded MusicXML score for application-wide access
        public static Score? CurrentScore { get; set; }
    }
}