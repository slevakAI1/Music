using System.Text;

namespace Music.Generate
{
    public static class DesignTextHelper
    {
        // Builds: VOICES + 2 newlines + SECTIONS + 2 newlines + HARMONIC TIMELINE
        public static string BuildCombinedText(ScoreDesignClass design)
        {
            var sb = new StringBuilder();

            // VOICES
            sb.Append("VOICES:\r\n");
            var first = true;
            foreach (var v in design.VoiceSet.Voices)
            {
                if (!first) sb.Append("\r\n");
                sb.Append(v?.VoiceName ?? string.Empty);
                first = false;
            }

            sb.Append("\r\n\r\n");

            // SECTIONS
            sb.Append("SECTIONS:\r\n");
            first = true;
            foreach (var s in design.Sections.Sections)
            {
                if (!first) sb.Append("\r\n");
                sb.Append(s?.SectionType.ToString());
                first = false;
            }

            sb.Append("\r\n\r\n");

            // HARMONIC TIMELINE
            sb.Append("HARMONIC TIMELINE:\r\n");
            first = true;
            var timeline = Globals.HarmonicTimeline;
            if (timeline != null)
            {
                foreach (var he in timeline.Events)
                {
                    if (!first) sb.Append("\r\n");
                    sb.Append($"Bar {he.StartBar} Beat {he.StartBeat}, {he.DurationBeats} beats | Key: {he.Key} | Degree: {he.Degree} | Quality: {he.Quality} | Bass: {he.Bass}");
                    first = false;
                }
            }

            return sb.ToString();
        }
    }
}