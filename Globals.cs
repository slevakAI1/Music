using Music.Designer;
using MusicXml.Domain;
using Music.Writer;

namespace Music
{
    public static class Globals
    {
        public static Designer.Designer? Designer { get; set; }

        // Holds a list of MusicXML scores for application-wide access
        // Index 0 is used as the primary/current score for backward compatibility
        public static List<Score> ScoreList { get; set; } = new List<Score>();

        // Persist WriterForm's data application-wide (refactor: moved from form instance to Globals)
        public static Writer.WriterFormData? Writer { get; set; }
    }
}