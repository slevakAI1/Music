using Music.Design;
using MusicXml;
using System.Text;
using DiffPlex;
using System.Windows.Forms;

namespace Music.Services
{
    public class MusicXmlSerializerTests
    {
        // Load → serialize → DiffPlex all differences → show each in a dialog (no special-casing)
        public string TestSerializer(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("Path must not be null or empty.", nameof(path));
                if (!File.Exists(path))
                    throw new FileNotFoundException("MusicXML file not found.", path);

                var originalXml = File.ReadAllText(path);
                var score = MusicXmlParser.GetScore(path);
                var serializedXml = MusicXmlScoreSerializer.Serialize(score);

                var differ = new Differ();
                var diff = differ.CreateCharacterDiffs(originalXml, serializedXml, ignoreWhitespace: false, ignoreCase: false);

                // No differences → pass
                if (diff.DiffBlocks.Count == 0)
                    return "Passed";

                // Save serialized output once for inspection
                var tempOutPath = Path.Combine(Path.GetTempPath(), $"Serialized_{Path.GetFileName(path)}");
                File.WriteAllText(tempOutPath, serializedXml);

                int shown = 0;
                foreach (var block in diff.DiffBlocks)
                {
                    // Safely slice the differing segments
                    static string Slice(string s, int start, int length)
                    {
                        if (s.Length == 0) return string.Empty;
                        start = Math.Clamp(start, 0, s.Length);
                        length = Math.Max(0, length);
                        int end = Math.Clamp(start + length, 0, s.Length);
                        return s.Substring(start, end - start);
                    }

                    var origSeg = Slice(originalXml, block.DeleteStartA, block.DeleteCountA);
                    var newSeg  = Slice(serializedXml, block.InsertStartB, block.InsertCountB);

                    var sb = new StringBuilder();
                    sb.AppendLine("Serialized output does not match original file contents.");
                    sb.AppendLine($"Original index: {block.DeleteStartA}, count: {block.DeleteCountA}");
                    sb.AppendLine($"Serialized index: {block.InsertStartB}, count: {block.InsertCountB}");
                    sb.AppendLine();
                    sb.AppendLine("Original segment:");
                    sb.AppendLine(origSeg);
                    sb.AppendLine();
                    sb.AppendLine("Serialized segment:");
                    sb.AppendLine(newSeg);
                    sb.AppendLine();
                    sb.AppendLine("Note: Full serialized output saved to:");
                    sb.AppendLine(tempOutPath);
                    sb.AppendLine();
                    sb.Append("Click OK for next difference or Cancel to stop.");

                    var result = MessageBox.Show(
                        sb.ToString(),
                        $"Serializer Differences ({shown + 1})",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning);

                    shown++;

                    if (result == DialogResult.Cancel)
                        break;
                }

                return $"Completed. Shown {shown} difference(s).";
            }
            catch (Exception ex)
            {
                return BuildErrorDetails(ex);
            }
        }

        private static string BuildErrorDetails(Exception ex)
        {
            var sb = new StringBuilder();
            var nl = Environment.NewLine;

            void AppendException(Exception e, int depth)
            {
                var indent = new string(' ', depth * 2);
                sb.Append(indent).Append("Exception: ").Append(e.GetType().FullName).Append(nl);
                sb.Append(indent).Append("Message: ").Append(e.Message).Append(nl);
                sb.Append(indent).Append("StackTrace:").Append(nl);
                sb.Append(indent).Append(e.StackTrace).Append(nl);

                if (e is AggregateException agg && agg.InnerExceptions?.Count > 0)
                {
                    sb.Append(indent).Append("Aggregate Inner Exceptions:").Append(nl);
                    for (int i = 0; i < agg.InnerExceptions.Count; i++)
                    {
                        sb.Append(indent).Append($"[{i}]").Append(nl);
                        AppendException(agg.InnerExceptions[i], depth + 1);
                    }
                }
                else if (e.InnerException != null)
                {
                    sb.Append(indent).Append("Inner Exception:").Append(nl);
                    AppendException(e.InnerException, depth + 1);
                }
            }

            AppendException(ex, 0);
            return sb.ToString();
        }
    }
}