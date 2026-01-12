using System.Text.Json;
using System.Text.Json.Serialization;

namespace Music.Generator
{
    // AI: purpose=Load voice catalog from Voices.Notion.json and expose categories of voice names.
    // AI: invariants=Result cached after first load; returned dict keys are category names (case-insensitive).
    // AI: deps=Reads file at project root via MusicConstants.VoicesNotionJsonRelativePath; JSON schema must match VoiceData.
    // AI: errors=On load/parse error returns catalog with single "error" key containing diagnostic message; caches that result.
    // AI: perf=Designed for cold init then cached; avoid changing file path computation which other tools expect.
    internal static class VoiceCatalog
    {
        // AI: VoiceData mirrors JSON shape; update these properties if the external schema changes.
        private sealed class VoiceData
        {
            [JsonPropertyName("product")]
            public string? Product { get; set; }

            [JsonPropertyName("schemaVersion")]
            public int? SchemaVersion { get; set; }

            [JsonPropertyName("twoStaffVoices")]
            public List<string>? TwoStaffVoices { get; set; }

            [JsonPropertyName("categories")]
            public List<CategoryEntry>? Categories { get; set; }
        }

        // AI: CategoryEntry maps each JSON category; Names trimmed and Voices deduped case-insensitively.
        private sealed class CategoryEntry
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("voices")]
            public List<string>? Voices { get; set; }
        }

        // AI: cached state: _cachedCatalog and _cachedData persist across calls to avoid repeated I/O.
        private static VoiceData? _cachedData;
        private static IReadOnlyDictionary<string, IReadOnlyList<string>>? _cachedCatalog;
        private static string? _cachedSourcePath;

        // AI: Load: returns cached catalog if available; otherwise reads JSON, deserializes, builds and caches catalog.
        // AI: Note: file path resolution uses AppContext.BaseDirectory -> project root; keep this logic in sync with tooling.
        public static IReadOnlyDictionary<string, IReadOnlyList<string>> Load(out string? sourcePath)
        {
            if (_cachedCatalog != null)
            {
                sourcePath = _cachedSourcePath;
                return _cachedCatalog;
            }

            // Use the project-root-based location (consistent with other code paths in the app).
            var filePath = Music.Helpers.ProjectPath(MusicConstants.VoicesNotionJsonRelativePath);

            _cachedSourcePath = filePath;
            sourcePath = filePath;

            try
            {
                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                _cachedData = JsonSerializer.Deserialize<VoiceData>(json, options);

                if (_cachedData?.Categories == null)
                {
                    _cachedCatalog = BuildErrorCatalog($"Failed to deserialize {filePath}: no categories found.");
                    return _cachedCatalog;
                }

                var catalog = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var category in _cachedData.Categories)
                {
                    var name = category?.Name?.Trim();
                    if (string.IsNullOrWhiteSpace(name) || category?.Voices == null)
                        continue;

                    var voices = category.Voices
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Select(v => v.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (voices.Count > 0)
                        catalog[name] = voices;
                }

                _cachedCatalog = catalog;
                return _cachedCatalog;
            }
            catch (Exception ex)
            {
                _cachedCatalog = BuildErrorCatalog($"Error loading {filePath}: {ex.Message}");
                return _cachedCatalog;
            }
        }

        // AI: GetTwoStaffVoices: ensures Load has been called and returns two-staff voice list or empty list.
        public static IReadOnlyList<string> GetTwoStaffVoices()
        {
            // Ensure data is loaded
            if (_cachedData == null)
            {
                Load(out _);
            }

            return _cachedData?.TwoStaffVoices ?? new List<string>();
        }

        // AI: BuildErrorCatalog: create a stable error-shaped catalog for callers to display diagnostic info.
        private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildErrorCatalog(string message)
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["error"] = new List<string> { message }
            };
        }
    }
}