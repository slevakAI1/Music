using Music.Design;
using MusicXml;
using System.Text;
using System.Xml;
using System.Security.Cryptography.Xml;

namespace Music.Services
{
    public class MusicXmlSerializerTests
    {
        // Import → serialize → compare with original; on mismatch, show first difference details
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

                if (string.Equals(originalXml, serializedXml, StringComparison.Ordinal))
                    return "Passed";

                // Save serialized output for inspection
                var tempOutPath = Path.Combine(Path.GetTempPath(), $"Serialized_{Path.GetFileName(path)}");
                File.WriteAllText(tempOutPath, serializedXml);

                // Build first-difference report
                var idx = FirstDifferenceIndex(originalXml, serializedXml);
                var (line, col) = GetLineColumn(originalXml, idx);

                var originalChar = CharAtOrEof(originalXml, idx);
                var serializedChar = CharAtOrEof(serializedXml, idx);

                var originalLineText = GetLineTextAtIndex(originalXml, idx);
                var serializedLineText = GetLineTextAtIndex(serializedXml, idx);

                var sb = new StringBuilder();
                sb.AppendLine("Failed: serialized output does not match original file contents.");
                sb.AppendLine($"First difference at absolute index {idx} (line {line}, column {col}).");
                sb.AppendLine($"Original char   : {DescribeChar(originalChar)}");
                sb.AppendLine($"Serialized char : {DescribeChar(serializedChar)}");
                sb.AppendLine();
                sb.AppendLine("Original line   : " + EscapeSnippet(originalLineText));
                sb.AppendLine("Serialized line : " + EscapeSnippet(serializedLineText));
                sb.AppendLine();
                sb.AppendLine("Note: Full serialized output saved to:");
                sb.Append(tempOutPath);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return BuildErrorDetails(ex);
            }
        }

        // Canonical XML (C14N). Removes XML decl/doctype differences, normalizes namespaces/attrs/empty elems/line endings.
        // Requires System.Security.Cryptography.Xml.
        private static byte[] CanonicalizeXml(string xml, bool exclusive = false)
        {
            var readerSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore, // avoid fetching external DTDs (MusicXML has a DOCTYPE)
                XmlResolver = null
            };

            using var sr = new StringReader(xml);
            using var xr = XmlReader.Create(sr, readerSettings);

            var doc = new XmlDocument
            {
                PreserveWhitespace = true, // let C14N govern whitespace handling
                XmlResolver = null
            };
            doc.Load(xr);

            Transform transform = exclusive
                ? new XmlDsigExcC14NTransform()
                : new XmlDsigC14NTransform();

            transform.LoadInput(doc);

            using var output = (Stream)transform.GetOutput(typeof(Stream));
            using var ms = new MemoryStream();
            output.CopyTo(ms);
            return ms.ToArray(); // canonical UTF-8 bytes
        }

        private static int FirstDifferenceIndex(string a, string b)
        {
            var min = Math.Min(a.Length, b.Length);
            for (int i = 0; i < min; i++)
            {
                if (a[i] != b[i]) return i;
            }
            return min; // difference is at end if lengths differ
        }

        private static (int line, int col) GetLineColumn(string text, int index)
        {
            // 1-based line/column; treat '\n' as line break; ignore '\r' (handles CRLF)
            int line = 1, col = 1;
            int len = Math.Min(index, text.Length);
            for (int i = 0; i < len; i++)
            {
                char c = text[i];
                if (c == '\n')
                {
                    line++;
                    col = 1;
                }
                else if (c != '\r')
                {
                    col++;
                }
            }
            return (line, col);
        }

        private static char? CharAtOrEof(string s, int index) =>
            index < s.Length ? s[index] : (char?)null;

        private static string DescribeChar(char? c)
        {
            if (c == null) return "∅ (end of string)";
            var ch = c.Value;
            string visible = ch switch
            {
                '\r' => "\\r",
                '\n' => "\\n",
                '\t' => "\\t",
                _ => char.IsControl(ch) ? $"\\u{(int)ch:X4}" : ch.ToString()
            };
            return $"'{visible}' (U+{(int)ch:X4})";
        }

        private static string GetLineTextAtIndex(string text, int index)
        {
            index = Math.Min(index, text.Length);
            int start = text.LastIndexOf('\n', Math.Max(0, index - 1));
            start = start == -1 ? 0 : start + 1;

            int end = text.IndexOf('\n', index);
            end = end == -1 ? text.Length : end;

            return text.Substring(start, end - start);
        }

        private static string EscapeSnippet(string s) =>
            s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");

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