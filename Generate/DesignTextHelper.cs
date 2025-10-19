using System.Text;

namespace Music.Generate
{
    public static class DesignTextHelper
    {
        // Builds a combined string: Voices (each on newline) + 2 newlines + Sections + 2 newlines + Chords
        public static string BuildCombinedText(ScoreDesignClass design)
        {
            var sb = new StringBuilder();

            // Voices
            var first = true;
            foreach (var v in design.VoiceSet.Voices)
            {
                if (!first) sb.Append("\r\n");
                sb.Append(v?.VoiceName ?? string.Empty);
                first = false;
            }

            sb.Append("\r\n\r\n");

            // Sections
            first = true;
            foreach (var s in design.Sections.Sections)
            {
                if (!first) sb.Append("\r\n");
                sb.Append(s?.SectionType.ToString());
                first = false;
            }

            sb.Append("\r\n\r\n");

            // Chords
            first = true;
            foreach (var c in design.ChordSet.Chords)
            {
                if (!first) sb.Append("\r\n");
                sb.Append(c?.ChordName ?? string.Empty);
                first = false;
            }

            return sb.ToString();
        }
    }
}