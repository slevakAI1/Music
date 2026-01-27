using System;
using System.IO;

namespace Music
{
    /// <summary>
    /// Simple debug trace helper that appends text to a file under the project "Errors" folder.
    /// </summary>
    public static class Tracer
    {
        /// <summary>
        /// Convenience helper that writes to the default debug trace file "DebugTrace.txt".
        /// </summary>
        /// <param name="text">The text to append.</param>
        public static void DebugTrace(string text)
        {
            Write("DebugTrace.txt", text);
        }

        /// <summary>
        /// Convenience helper that writes out-of-key warnings to "OutOfKeyWarnings.txt".
        /// </summary>
        /// <param name="text">The text to append.</param>
        public static void OutOfKeyWarnings(string text)
        {
            Write("OutOfKeyWarnings.txt", text);
        }

        /// <summary>
        /// Appends the provided text to the named file under the project "Errors" folder.
        /// If <paramref name="filename"/> is null or empty, "DebugTrace.txt" is used.
        /// </summary>
        /// <param name="filename">The file name to write to (e.g. "DebugTrace.txt").</param>
        /// <param name="text">The text to append.</param>
        public static void Write(string filename, string text)
        {
            ArgumentNullException.ThrowIfNull(filename);
            ArgumentNullException.ThrowIfNull(text);

            try
            {
                // Build path relative to project root: /Errors/{filename}
                string relativePath = Path.Combine("Errors", filename);

                // Resolve using project helper (assume Windows environment and helper exists)
                string fullPath = Helpers.ProjectPath(relativePath);

                var dir = Path.GetDirectoryName(fullPath) ?? Path.GetDirectoryName(relativePath) ??
                          Path.Combine(AppContext.BaseDirectory ?? Directory.GetCurrentDirectory());

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Use Eastern Time (US) for timestamp (Windows timezone ID)
                TimeZoneInfo estTz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                DateTimeOffset timestamp = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, estTz);

                // Prepend an ISO-8601 timestamp in Eastern time for context
                var entry = $"[{timestamp:O}] {text}{Environment.NewLine}";

                File.AppendAllText(fullPath, entry);
            }
            catch (Exception ex)
            {
                // If tracing fails, write to console as fallback
                Console.WriteLine($"[TRACE FAILED: {ex.Message}] {text}");
            }
        }
    }
}
