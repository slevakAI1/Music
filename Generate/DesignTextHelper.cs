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
                if (s == null)
                {
                    first = false;
                    continue;
                }

                var title = string.IsNullOrWhiteSpace(s.Name)
                    ? s.SectionType.ToString()
                    : $"{s.SectionType} - {s.Name}";

                var barsText = s.BarCount == 1 ? "1 bar" : $"{s.BarCount} bars";
                sb.Append($"{title}: Bar {s.StartBar}, {barsText}");
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

            // ALIGNMENT VIEW (Sections vs Harmony)
            sb.Append("\r\n\r\n");
            sb.Append("ALIGNMENT (Sections vs Harmony):\r\n");
            if (timeline == null || timeline.Events.Count == 0)
            {
                sb.Append("(no harmonic timeline)\r\n");
            }
            else
            {
                int bpb = timeline.BeatsPerBar;
                foreach (var s in design.Sections.Sections)
                {
                    if (s == null) continue;

                    var title = string.IsNullOrWhiteSpace(s.Name)
                        ? s.SectionType.ToString()
                        : $"{s.SectionType} - {s.Name}";

                    var sectionStartAbs = (s.StartBar - 1) * bpb; // beat index (0-based)
                    var sectionEndAbsExcl = sectionStartAbs + s.BarCount * bpb; // end exclusive

                    sb.Append($"{title}: Bars {s.StartBar}-{s.StartBar + s.BarCount - 1}\r\n");

                    // Find overlapping event segments within this section
                    bool any = false;
                    foreach (var he in timeline.Events)
                    {
                        var evStartAbs = (he.StartBar - 1) * bpb + (he.StartBeat - 1);
                        var evEndAbsExcl = evStartAbs + he.DurationBeats;

                        // Intersect [sectionStartAbs, sectionEndAbsExcl) with [evStartAbs, evEndAbsExcl)
                        var segStartAbs = evStartAbs > sectionStartAbs ? evStartAbs : sectionStartAbs;
                        var segEndAbsExcl = evEndAbsExcl < sectionEndAbsExcl ? evEndAbsExcl : sectionEndAbsExcl;
                        if (segEndAbsExcl <= segStartAbs)
                            continue; // no overlap

                        any = true;

                        // Convert to bar:beat (inclusive end)
                        static void ToBarBeat(int abs, int beatsPerBar, out int bar, out int beat)
                        {
                            bar = abs / beatsPerBar + 1;
                            beat = abs % beatsPerBar + 1;
                        }

                        ToBarBeat(segStartAbs, bpb, out var sBar, out var sBeat);
                        var endInclAbs = segEndAbsExcl - 1;
                        ToBarBeat(endInclAbs, bpb, out var eBar, out var eBeat);

                        var range = sBar == eBar
                            ? $"{sBar}:{sBeat}-{eBeat}"
                            : $"{sBar}:{sBeat}-{eBar}:{eBeat}";

                        sb.Append($"  - [{range}] {he.Key}, Degree {he.Degree}, {he.Quality}, Bass: {he.Bass}\r\n");
                    }

                    if (!any)
                    {
                        sb.Append("  (no overlapping harmony)\r\n");
                    }
                }
            }

            return sb.ToString();
        }
    }
}