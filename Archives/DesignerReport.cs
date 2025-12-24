//using System.Text;

//namespace Music.SongContext
//{
//    /// <summary>
//    /// THIS IS BASICALLY A REPORT FOR THE DESIGN DATA STRUCTURE.
//    /// </summary>
//    public static class DesignerReport
//    {
//        // Builds: VOICES + 2 newlines + SECTIONS + 2 newlines + HARMONY track + 2 newlines + ALIGNMENT + 2 newlines + TIME SIGNATURES + 2 newlines + TEMPO
//        public static string CreateDesignerReport(SongContext designer)
//        {
//            if (designer == null)
//                return "(no design loaded)";

//            var sb = new StringBuilder();

//            // VOICES
//            sb.Append("VOICES:\r\n");
//            var first = true;
//            foreach (var v in designer.Voices.Voices)
//            {
//                if (!first) sb.Append("\r\n");
//                sb.Append(v?.VoiceName ?? string.Empty);
//                first = false;
//            }

//            sb.Append("\r\n\r\n");

//            // SECTIONS
//            sb.Append("SECTIONS:\r\n");
//            first = true;
//            foreach (var s in designer.SectionTrack.Sections)
//            {
//                if (!first) sb.Append("\r\n");
//                if (s == null)
//                {
//                    first = false;
//                    continue;
//                }

//                var title = string.IsNullOrWhiteSpace(s.Name)
//                    ? s.SectionType.ToString()
//                    : $"{s.SectionType} - {s.Name}";

//                var barsText = s.BarCount == 1 ? "1 bar" : $"{s.BarCount} bars";
//                sb.Append($"{title}: Bar {s.StartBar}, {barsText}");
//                first = false;
//            }

//            sb.Append("\r\n\r\n");

//            // HARMONY track
//            sb.Append("HARMONY track:\r\n");
//            first = true;
//            var track = designer.HarmonyTrack;
//            if (timeline != null)
//            {
//                foreach (var he in track.Events)
//                {
//                    if (!first) sb.Append("\r\n");
//                    sb.Append($"Bar {he.StartBar} Beat {he.StartBeat}, {he.DurationBeats} beats | Key: {he.Key} | Degree: {he.Degree} | Quality: {he.Quality} | Bass: {he.Bass}");
//                    first = false;
//                }
//            }

//            // ALIGNMENT VIEW (Sections vs Harmony)
//            sb.Append("\r\n\r\n");
//            sb.Append("ALIGNMENT (Sections vs Harmony):\r\n");
//            if (timeline == null || track.Events.Count == 0)
//            {
//                sb.Append("(no harmony track)\r\n");
//            }
//            else
//            {
//                int bpb = track.BeatsPerBar;
//                foreach (var s in designer.SectionTrack.Sections)
//                {
//                    if (s == null) continue;

//                    var title = string.IsNullOrWhiteSpace(s.Name)
//                        ? s.SectionType.ToString()
//                        : $"{s.SectionType} - {s.Name}";

//                    var sectionStartAbs = (s.StartBar - 1) * bpb; // beat index (0-based)
//                    var sectionEndAbsExcl = sectionStartAbs + s.BarCount * bpb; // end exclusive

//                    sb.Append($"{title}: Bars {s.StartBar}-{s.StartBar + s.BarCount - 1}\r\n");

//                    // Find overlapping event segments within this section
//                    bool any = false;
//                    foreach (var he in track.Events)
//                    {
//                        var evStartAbs = (he.StartBar - 1) * bpb + (he.StartBeat - 1);
//                        var evEndAbsExcl = evStartAbs + he.DurationBeats;

//                        var segStartAbs = evStartAbs > sectionStartAbs ? evStartAbs : sectionStartAbs;
//                        var segEndAbsExcl = evEndAbsExcl < sectionEndAbsExcl ? evEndAbsExcl : sectionEndAbsExcl;
//                        if (segEndAbsExcl <= segStartAbs)
//                            continue; // no overlap

//                        any = true;

//                        // Execute to bar:beat (inclusive end)
//                        static void ToBarBeat(int abs, int beatsPerBar, out int bar, out int beat)
//                        {
//                            bar = abs / beatsPerBar + 1;
//                            beat = abs % beatsPerBar + 1;
//                        }

//                        ToBarBeat(segStartAbs, bpb, out var sBar, out var sBeat);
//                        var endInclAbs = segEndAbsExcl - 1;
//                        ToBarBeat(endInclAbs, bpb, out var eBar, out var eBeat);

//                        var range = sBar == eBar
//                            ? $"{sBar}:{sBeat}-{eBeat}"
//                            : $"{sBar}:{sBeat}-{eBar}:{eBeat}";

//                        sb.Append($"  - [{range}] {he.Key}, Degree {he.Degree}, {he.Quality}, Bass: {he.Bass}\r\n");
//                    }

//                    if (!any)
//                    {
//                        sb.Append("  (no overlapping harmony)\r\n");
//                    }
//                }
//            }

//            // TIME SIGNATURES
//            sb.Append("\r\n\r\n");
//            sb.Append("TIME SIGNATURES:\r\n");
//            var tsTimeline = designer.TimeSignatureTrack;
//            if (tsTimeline == null || tsTimeline.Events.Count == 0)
//            {
//                sb.Append("(no time signature track)");
//            }
//            else
//            {
//                var firstTs = true;
//                foreach (var se in tsTimeline.Events)
//                {
//                    if (!firstTs) sb.Append("\r\n");
//                    // List event placement and span in beats. Meter detail (e.g., 4/4) can be added later if needed.
//                    sb.Append($"Bar {se.StartBar} Beat {se.StartBeat}");
//                    firstTs = false;
//                }
//            }

//            // TEMPO
//            sb.Append("\r\n\r\n");
//            sb.Append("TEMPO:\r\n");
//            var tempoTrack = designer.TempoTrack;
//            if (tempoTrack == null || tempoTrack.Events.Count == 0)
//            {
//                sb.Append("(no tempo track)");
//            }
//            else
//            {
//                var firstTempo = true;
//                foreach (var te in tempoTrack.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
//                {
//                    if (!firstTempo) sb.Append("\r\n");
//                    sb.Append($"Bar {te.StartBar} Beat {te.StartBeat}, | {te.TempoBpm} BPM");
//                    firstTempo = false;
//                }
//            }

//            return sb.ToString();
//        }
//    }
//}