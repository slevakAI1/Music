using DiffPlex;
using Music.Design;
using MusicXml;
using System.Text;

namespace Music.Tests
{
    public class MusicXmlSerializerTests
    {
        // Load → serialize → DiffPlex line-diff → show each diff block once (no per-character stepping)
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
                var lineDiff = differ.CreateLineDiffs(originalXml, serializedXml, ignoreWhitespace: false, ignoreCase: false);

                if (lineDiff.DiffBlocks.Count == 0)
                    return "Passed";

                // Save serialized output once for inspection
                var tempOutPath = Path.Combine(Path.GetTempPath(), $"Serialized_{Path.GetFileName(path)}");
                File.WriteAllText(tempOutPath, serializedXml);

                // Normalize to '\n' then split to arrays for line slicing
                static string[] ToLines(string s) =>
                    s.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

                var oldLines = ToLines(originalXml);
                var newLines = ToLines(serializedXml);

                int shown = 0;
                foreach (var block in lineDiff.DiffBlocks)
                {
                    string origSeg = string.Join(Environment.NewLine, oldLines.Skip(block.DeleteStartA).Take(block.DeleteCountA));
                    string newSeg  = string.Join(Environment.NewLine, newLines.Skip(block.InsertStartB).Take(block.InsertCountB));

                    var sb = new StringBuilder();
                    sb.AppendLine("Serialized output does not match original file contents.");
                    sb.AppendLine($"Original lines: start {block.DeleteStartA}, count {block.DeleteCountA}");
                    sb.AppendLine($"Serialized lines: start {block.InsertStartB}, count {block.InsertCountB}");
                    sb.AppendLine();
                    sb.AppendLine("Original block:");
                    sb.AppendLine(origSeg.Length == 0 ? "∅" : origSeg);
                    sb.AppendLine();
                    sb.AppendLine("Serialized block:");
                    sb.AppendLine(newSeg.Length == 0 ? "∅" : newSeg);
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
                    if (result == DialogResult.Cancel) break;
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