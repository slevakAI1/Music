using System.Text.Json;
using System.Linq;

namespace Music.Design
{
    // Loads voices from a Notion-style JSON file and exposes them grouped by category.
    // Supports a few flexible JSON shapes:
    // 1) Flat array:
    //    [ { "category": "Strings", "name": "Violin" }, ... ]
    // 2) Dictionary category -> voices:
    //    { "Strings": ["Violin", "Cello"], "Brass": ["Trumpet"] }
    // 3) Root with categories:
    //    { "categories": [ { "name": "Strings", "voices": ["Violin","Cello"] }, ... ] }
    internal static class VoiceCatalog
    {
        private sealed class FlatVoiceRecord
        {
            public string? Category { get; set; }
            public string? Name { get; set; }
        }

        private sealed class CategoryEntry
        {
            public string? Name { get; set; }
            public List<string>? Voices { get; set; }
        }

        private sealed class CategoryRoot
        {
            public List<CategoryEntry>? Categories { get; set; }
        }

        public static IReadOnlyDictionary<string, IReadOnlyList<string>> Load(out string? sourcePath)
        {
            sourcePath = null;
            var baseDir = AppContext.BaseDirectory;
            var probed = new List<string>();
            string? foundPath = null;
            string lastError = "unknown error";

            const string fileName = "Voices.Notion.json";

            // Probe 1: exact path in base directory
            var p1 = Path.Combine(baseDir, fileName);
            probed.Add(p1);
            if (File.Exists(p1)) foundPath = p1;

            // Probe 2: case-insensitive match in base directory
            if (foundPath == null)
            {
                var match = Directory.EnumerateFiles(baseDir, "*", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(p => string.Equals(Path.GetFileName(p), fileName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    probed.Add(match);
                    foundPath = match;
                }
            }

            // Probe 3: Design subfolder under base (canonical source location)
            if (foundPath == null)
            {
                var pDesign = Path.Combine(baseDir, "Design", fileName);
                probed.Add(pDesign);
                if (File.Exists(pDesign)) foundPath = pDesign;
            }

            // Probe 4: walk up parents (dev-time run) and look for Design/Voices.Notion.json,
            //          and the file itself in the parent roots
            if (foundPath == null)
            {
                var current = new DirectoryInfo(baseDir);
                for (int i = 0; i < 5 && current?.Parent != null; i++)
                {
                    current = current.Parent;
                    if (current == null) break;

                    var candidateDesign = Path.Combine(current.FullName, "Design", fileName);
                    probed.Add(candidateDesign);
                    if (File.Exists(candidateDesign)) { foundPath = candidateDesign; break; }

                    var candidateRoot = Path.Combine(current.FullName, fileName);
                    probed.Add(candidateRoot);
                    if (File.Exists(candidateRoot)) { foundPath = candidateRoot; break; }
                }
            }

            if (foundPath == null)
            {
                lastError = $"Voices.Notion.json not found. Probed: {string.Join(" | ", probed)}";
                return BuildErrorCatalog(lastError);
            }

            try
            {
                var text = File.ReadAllText(foundPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                // Try 1: flat array of objects with Category/Name
                if (TryParseFlat(text, options, out var flatDict))
                {
                    sourcePath = foundPath;
                    return flatDict;
                }

                // Try 2: dictionary<string, List<string>>
                if (TryParseDict(text, options, out var dictDict))
                {
                    sourcePath = foundPath;
                    return dictDict;
                }

                // Try 3: { "categories": [ { "name": "", "voices": [] } ] }
                if (TryParseRoot(text, options, out var rootDict))
                {
                    sourcePath = foundPath;
                    return rootDict;
                }

                lastError = $"Unrecognized JSON structure in {foundPath}. Expected flat array, dictionary, or {{ \"categories\": [...] }}.";
                return BuildErrorCatalog(lastError);
            }
            catch (Exception ex)
            {
                lastError = $"Failed to load {foundPath}: {ex.Message}";
                return BuildErrorCatalog(lastError);
            }
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildErrorCatalog(string message)
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["error"] = new List<string> { message }
            };
        }

        private static bool TryParseFlat(string json, JsonSerializerOptions options, out IReadOnlyDictionary<string, IReadOnlyList<string>> result)
        {
            result = new Dictionary<string, IReadOnlyList<string>>();
            try
            {
                var flat = JsonSerializer.Deserialize<List<FlatVoiceRecord>>(json, options);
                if (flat == null) return false;

                var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in flat)
                {
                    var cat = (item?.Category ?? "").Trim();
                    var name = (item?.Name ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(cat) || string.IsNullOrWhiteSpace(name)) continue;

                    if (!map.TryGetValue(cat, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        map[cat] = set;
                    }
                    set.Add(name);
                }

                result = map.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyList<string>)kvp.Value.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                    StringComparer.OrdinalIgnoreCase);

                return map.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseDict(string json, JsonSerializerOptions options, out IReadOnlyDictionary<string, IReadOnlyList<string>> result)
        {
            result = new Dictionary<string, IReadOnlyList<string>>();
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, options);
                if (dict == null) return false;

                var normalized = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var (k, v) in dict)
                {
                    if (string.IsNullOrWhiteSpace(k) || v == null) continue;
                    var clean = v
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (clean.Count > 0)
                        normalized[k.Trim()] = clean;
                }

                result = normalized;
                return normalized.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseRoot(string json, JsonSerializerOptions options, out IReadOnlyDictionary<string, IReadOnlyList<string>> result)
        {
            result = new Dictionary<string, IReadOnlyList<string>>();
            try
            {
                var root = JsonSerializer.Deserialize<CategoryRoot>(json, options);
                if (root?.Categories == null) return false;

                var normalized = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var cat in root.Categories)
                {
                    var name = (cat?.Name ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(name) || cat?.Voices == null) continue;

                    var clean = cat.Voices
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (clean.Count > 0)
                        normalized[name] = clean;
                }

                result = normalized;
                return normalized.Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}