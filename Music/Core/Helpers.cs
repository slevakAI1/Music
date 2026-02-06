// AI: purpose=Resolve project-relative file paths from running AppContext.BaseDirectory for dev/debug utilities.
// AI: invariants=Returns an absolute path under project root; does not create files; filename must be non-null.
// AI: deps=Relies on AppContext.BaseDirectory layout (bin/<cfg>/netX); adjust if publish layout differs.
// AI: security=Do not pass untrusted user input; path traversal via ".." may escape project root.
namespace Music
{
    internal static class Helpers
    {
        // AI: purpose=Combine project root (three levels up from BaseDirectory) with provided filename and normalize.
        // AI: errors=Throws ArgumentNullException when filename is null; does not check file existence.
        public static string ProjectPath(string filename)
        {
            ArgumentNullException.ThrowIfNull(filename);
            var baseDir = AppContext.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, filename));
        }
    }
}
