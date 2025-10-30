using System.Text;

namespace Music.Design
{
    /// <summary>
    /// THIS IS BASICALLY A DEBUG VIEW FOR THE DESIGN DATA STRUCTURE.
    /// </summary>
    public static class DesignerView
    {
        // Builds: VOICES + 2 newlines + SECTIONS + 2 newlines + HARMONIC TIMELINE + 2 newlines + ALIGNMENT + 2 newlines + TIME SIGNATURES + 2 newlines + TEMPO
        public static string CreateDesignView(DesignerData design)
        {
            var sb = new StringBuilder();

            // VOICES
            sb.Append("VOICES:\r\n");
            var first = true;
            foreach (var v in design.PartSet.Parts)
            {
                if (!first) sb.Append("\r\n");
                sb.Append(v?.PartName ?? string.Empty);
                first = false;
            }

            sb.Append("\r\n\r\n");

            // SECTIONS
            sb.Append("SECTIONS:\r\n");
            first = true;
            foreach (var s in design.SectionSet.Sections)
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
            var timeline = design.HarmonicTimeline;
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
                foreach (var s in design.SectionSet.Sections)
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

            // TIME SIGNATURES
            sb.Append("\r\n\r\n");
            sb.Append("TIME SIGNATURES:\r\n");
            var tsTimeline = design.TimeSignatureTimeline;
            if (tsTimeline == null || tsTimeline.Events.Count == 0)
            {
                sb.Append("(no time signature timeline)");
            }
            else
            {
                var firstTs = true;
                foreach (var se in tsTimeline.Events)
                {
                    if (!firstTs) sb.Append("\r\n");
                    // List event placement and span in beats. Meter detail (e.g., 4/4) can be added later if needed.
                    sb.Append($"Bar {se.StartBar} Beat {se.StartBeat}, {se.DurationBeats} beats");
                    firstTs = false;
                }
            }

            // TEMPO
            sb.Append("\r\n\r\n");
            sb.Append("TEMPO:\r\n");
            var tempoTimeline = design.TempoTimeline;
            if (tempoTimeline == null || tempoTimeline.Events.Count == 0)
            {
                sb.Append("(no tempo timeline)");
            }
            else
            {
                var firstTempo = true;
                foreach (var te in tempoTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
                {
                    if (!firstTempo) sb.Append("\r\n");
                    sb.Append($"Bar {te.StartBar} Beat {te.StartBeat}, {te.DurationBeats} beats | {te.TempoBpm} BPM");
                    firstTempo = false;
                }
            }

            return sb.ToString();
        }
    }
}