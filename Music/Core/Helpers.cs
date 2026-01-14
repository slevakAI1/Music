using System;
using System.IO;

namespace Music
{
    internal static class Helpers
    {
        public static string ProjectPath(string filename)
        {
            ArgumentNullException.ThrowIfNull(filename);
            var baseDir = AppContext.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, filename));
        }
    }
}
