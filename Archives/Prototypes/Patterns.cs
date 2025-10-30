//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text.Json;

//namespace Music.Generate
//{
//    // Lightweight DTOs that mirror the structure of the JSON metadata file
//    public sealed class PatternParameterDef
//    {
//        public string Name { get; set; } = string.Empty;
//        public string? Label { get; set; }
//        public string? Type { get; set; }
//        public string? Control { get; set; }
//        public JsonElement? Default { get; set; }
//        public Dictionary<string, JsonElement>? DependsOn { get; set; }
//        public JsonElement[]? Options { get; set; }
//        public int? Min { get; set; }
//        public int? Max { get; set; }
//        public string? Description { get; set; }
//    }

//    public sealed class PatternDef
//    {
//        public string Key { get; set; } = string.Empty;
//        public string DisplayName { get; set; } = string.Empty;
//        public string? Description { get; set; }
//        public List<PatternParameterDef> Parameters { get; set; } = new();
//    }

//    public sealed class PatternsFileRoot
//    {
//        public List<PatternDef> Patterns { get; set; } = new();
//    }

//    /// <summary>
//    /// Holds the loaded metadata for available pattern types and provides helpers
//    /// for the form to build the pattern-type dropdown and to generate parameter UI.
//    /// The class will try to load "pattern-definitions.json" from the application directory;
//    /// if not found it falls back to an embedded default JSON.
//    /// </summary>
//    public sealed class PatternsClass
//    {
//        private readonly Dictionary<string, PatternDef> _definitions = new(StringComparer.OrdinalIgnoreCase);

//        public IReadOnlyDictionary<string, PatternDef> Definitions => _definitions;

//        public PatternsClass()
//        {
//            LoadDefaults();
//        }

//        public void LoadFromFile(string path)
//        {
//            if (!File.Exists(path))
//                throw new FileNotFoundException("Pattern definitions file not found", path);

//            var json = File.ReadAllText(path);
//            LoadFromJson(json);
//        }

//        public void LoadFromJson(string json)
//        {
//            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
//            var root = JsonSerializer.Deserialize<PatternsFileRoot>(json, opts);
//            _definitions.Clear();
//            if (root?.Patterns != null)
//            {
//                foreach (var p in root.Patterns)
//                    _definitions[p.Key] = p;
//            }
//        }

//        private void LoadDefaults()
//        {
//            // Try to find an external file next to the app; otherwise use embedded default.
//            var candidate = Path.Combine(AppContext.BaseDirectory, "pattern-definitions.json");
//            if (File.Exists(candidate))
//            {
//                LoadFromFile(candidate);
//                return;
//            }

//            LoadFromJson(DefaultJson);
//        }

//        public IEnumerable<(string Key, string DisplayName)> GetDropdownItems()
//        {
//            return _definitions.Values.Select(d => (d.Key, d.DisplayName));
//        }

//        public bool TryGetDefinition(string key, out PatternDef? def)
//        {
//            return _definitions.TryGetValue(key, out def);
//        }

//        // Minimal built-in fallback (keeps the project usable even if external file is missing)
//        private const string DefaultJson = @"{
//  ""patterns"": [
//    {
//      ""key"": ""SetWholeNote"",
//      ""displayName"": ""Set Whole Note"",
//      ""description"": ""Insert whole notes into selected bars/voices."",
//      ""parameters"": [
//        { ""name"": ""Mode"", ""label"": ""Pitch Mode"", ""type"": ""enum"", ""control"": ""radio"", ""options"": [""Absolute"", ""KeyRelative""], ""default"": ""Absolute"" },

//        { ""name"": ""Step"", ""label"": ""Step"", ""type"": ""string"", ""control"": ""combobox"", ""options"": [""C"", ""D"", ""E"", ""F"", ""G"", ""A"", ""B""], ""default"": ""C"", ""dependsOn"": { ""Mode"": ""Absolute"" } },
//        { ""name"": ""Alter"", ""label"": ""Alter"", ""type"": ""int"", ""control"": ""combobox"", ""options"": [-1, 0, 1], ""default"": 0, ""dependsOn"": { ""Mode"": ""Absolute"" } },
//        { ""name"": ""OctaveAbsolute"", ""label"": ""Octave (Absolute)"", ""type"": ""int"", ""control"": ""numeric"", ""default"": 4, ""dependsOn"": { ""Mode"": ""Absolute"" } },

//        { ""name"": ""Degree"", ""label"": ""Degree"", ""type"": ""int"", ""control"": ""numeric"", ""default"": 1, ""min"": 1, ""max"": 7, ""dependsOn"": { ""Mode"": ""KeyRelative"" } },
//        { ""name"": ""OctaveKeyRelative"", ""label"": ""Octave (Key Relative)"", ""type"": ""int"", ""control"": ""numeric"", ""default"": 4, ""dependsOn"": { ""Mode"": ""KeyRelative"" } },

//        { ""name"": ""Voices"", ""label"": ""Voices"", ""type"": ""list<int>"", ""control"": ""text"", ""default"": ""1"", ""description"": ""Comma-separated voice numbers"" },
//        { ""name"": ""Staff"", ""label"": ""Staff"", ""type"": ""int"", ""control"": ""numeric"", ""default"": 1 },
//        { ""name"": ""Overwrite"", ""label"": ""Overwrite"", ""type"": ""bool"", ""control"": ""checkbox"", ""default"": true },

//        { ""name"": ""ApplyToAllParts"", ""label"": ""Apply To All Parts"", ""type"": ""bool"", ""control"": ""checkbox"", ""default"": true },
//        { ""name"": ""TargetPartIndex"", ""label"": ""Target Part Index"", ""type"": ""int"", ""control"": ""numeric"", ""default"": 0, ""dependsOn"": { ""ApplyToAllParts"": false } },

//        { ""name"": ""StartBar"", ""label"": ""Start Bar"", ""type"": ""int"", ""control"": ""numeric"", ""default"": 1, ""min"": 1 },
//        { ""name"": ""EndBar"", ""label"": ""End Bar"", ""type"": ""int"", ""control"": ""numeric"", ""default"": 1, ""min"": 1 }
//      ]
//    }
//  ]
//}";
//    }
//}