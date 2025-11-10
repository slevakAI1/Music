using System.Text;
using MusicXml.Domain;

namespace Music.Writer
{
    /// <summary>
    /// Generates reports for Score objects.
    /// </summary>
    public static class ScoreReport
    {
        /// <summary>
        /// Generates a hierarchical report of the score contents.
        /// </summary>
        /// <param name="score">The score to report on.</param>
        /// <returns>A formatted string containing the score report.</returns>
        public static string Run(Score? score)
        {
            if (score == null)
                return "(no score loaded)";

            if (score.Parts == null || score.Parts.Count == 0)
                return "(no parts in score)";

            var sb = new StringBuilder();
            bool firstPart = true;

            foreach (var part in score.Parts)
            {
                if (!firstPart)
                    sb.Append("\r\n");

                // Part name
                var partName = !string.IsNullOrWhiteSpace(part.Name) ? part.Name : "(unnamed part)";
                sb.Append($"{partName}\r\n");

                // Count measures
                int measureCount = part.Measures?.Count ?? 0;
                sb.Append($"   Measures: {measureCount}\r\n");

                // Count notes, rests, and chords
                int totalNotes = 0;
                int totalRests = 0;
                int totalChords = 0;

                if (part.Measures != null)
                {
                    foreach (var measure in part.Measures)
                    {
                        if (measure.MeasureElements == null)
                            continue;

                        bool inChord = false;

                        foreach (var element in measure.MeasureElements)
                        {
                            if (element.Type != MeasureElementType.Note)
                                continue;

                            if (element.Element is Note note)
                            {
                                if (note.IsRest)
                                {
                                    totalRests++;
                                    inChord = false;
                                }
                                else
                                {
                                    totalNotes++;

                                    // First note of a chord (IsChordTone = false) starts a new chord
                                    if (!note.IsChordTone)
                                    {
                                        if (inChord)
                                        {
                                            // Previous chord has ended, count it
                                            totalChords++;
                                        }
                                        // Check if next note is a chord tone to determine if this starts a chord
                                        inChord = false;
                                    }
                                    else
                                    {
                                        // This is a chord tone, so we're in a chord
                                        if (!inChord)
                                        {
                                            // This chord tone indicates the previous note started a chord
                                            totalChords++;
                                            inChord = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                sb.Append($"   Total Notes: {totalNotes}\r\n");
                sb.Append($"   Total Rests: {totalRests}\r\n");
                sb.Append($"   Total Chords: {totalChords}");

                firstPart = false;
            }

            return sb.ToString();
        }
    }
}