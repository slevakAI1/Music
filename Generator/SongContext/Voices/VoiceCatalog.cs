using System.Text.Json;
using System.Text.Json.Serialization;

namespace Music.Generator
{
    // Loads voices from Voices.Notion.json and exposes them grouped by category.
    internal static class VoiceCatalog
    {
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

        private sealed class CategoryEntry
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("voices")]
            public List<string>? Voices { get; set; }
        }

        private static VoiceData? _cachedData;
        private static IReadOnlyDictionary<string, IReadOnlyList<string>>? _cachedCatalog;
        private static string? _cachedSourcePath;

        public static IReadOnlyDictionary<string, IReadOnlyList<string>> Load(out string? sourcePath)
        {
            if (_cachedCatalog != null)
            {
                sourcePath = _cachedSourcePath;
                return _cachedCatalog;
            }

            // Use the project-root-based location (consistent with other code paths in the app).
            var baseDir = AppContext.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            var filePath = Path.Combine(projectRoot, MusicConstants.VoicesNotionJsonRelativePath);

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

        /// <summary>
        /// Returns the list of voice names that require two staves (e.g., Piano, Harp).
        /// </summary>
        public static IReadOnlyList<string> GetTwoStaffVoices()
        {
            // Ensure data is loaded
            if (_cachedData == null)
            {
                Load(out _);
            }

            return _cachedData?.TwoStaffVoices ?? new List<string>();
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildErrorCatalog(string message)
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["error"] = new List<string> { message }
            };
        }
    }
}