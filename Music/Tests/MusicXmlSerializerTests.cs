using Music.Design;
using MusicXml;
using System.Text;

namespace Music.Services
{
    public class MusicXmlSerializerTests
    {
        // Import → serialize → simple string compare to original file contents
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

                return string.Equals(originalXml, serializedXml, StringComparison.Ordinal)
                    ? "Passed"
                    : "Failed: serialized output does not match original file contents.";
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