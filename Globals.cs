using Music.Designer;
using MusicXml.Domain;
using Music.Writer;

namespace Music
{
    public static class Globals
    {
        public static Designer.Designer? Designer { get; set; }

        // Holds the currently loaded MusicXML score for application-wide access
        public static Score? Score { get; set; }

        // Persist WriterForm's data application-wide (refactor: moved from form instance to Globals)
        public static Writer.WriterData? Writer { get; set; }
    }
}