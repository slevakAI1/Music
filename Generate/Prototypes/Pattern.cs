using MusicXml.Domain;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Music.Generate
{
    /// <summary>
    /// Lightweight container for a pattern instance. Keeps all target/common properties
    /// strongly typed and stores all optional/variable parameters in a JSON-backed ParameterBag.
    /// Business logic (pitch resolution, key signature helpers, application logic) was removed
    /// so this class remains a pure data container.
    /// </summary>
    public sealed class Pattern
    {
        public enum PatternKind
        {
            SetWholeNote
            // Extendable for future pattern types
        }

        // Basic identity / selection
        public string Name { get; set; } = "Unnamed";
        public PatternKind Kind { get; set; } = PatternKind.SetWholeNote;

        // Targeting / common placement (these are required for all patterns)
        public List<int> Voices { get; set; } = new();
        public int Staff { get; set; } = 1;
        public bool Overwrite { get; set; } = true;

        // Targeting across parts
        public bool ApplyToAllParts { get; set; } = true;
        public int TargetPartIndex { get; set; } = 0;

        // Bar range (1-based)
        public int StartBar { get; set; } = 1;
        public int EndBar { get; set; } = 1;

        // Flexible bag for optional pattern parameters (UI-driven). Stored as a JsonObject so
        // it serializes/deserializes cleanly and can be persisted as JSON.
        public JsonObject? ParameterBag { get; set; } = new JsonObject();

        public Pattern()
        {
        }

        // Minimal validation focused on the always-required target properties.
        // All pattern-specific validation should be performed by the business layer
        // that interprets ParameterBag according to metadata.
        public bool TryValidate(out string? error)
        {
            error = null;

            if (StartBar <= 0)
            {
                error = "Start bar must be >= 1.";
                return false;
            }
            if (EndBar < StartBar)
            {
                error = "End bar must be greater than or equal to Start bar.";
                return false;
            }
            if (Voices == null || Voices.Count == 0)
            {
                error = "At least one voice is required.";
                return false;
            }
            foreach (var v in Voices)
            {
                if (v <= 0)
                {
                    error = "Voice numbers must be positive integers.";
                    return false;
                }
            }
            if (Staff <= 0)
            {
                error = "Staff must be a positive integer.";
                return false;
            }

            return true;
        }

        // ParameterBag helpers - convenience typed accessors for the UI / business layer.

        public void SetParameter<T>(string key, T value)
        {
            ParameterBag ??= new JsonObject();
            var node = JsonSerializer.SerializeToNode(value);
            ParameterBag[key] = node;
        }

        public bool TryGetParameter<T>(string key, out T? value)
        {
            value = default;
            if (ParameterBag == null) return false;
            if (!ParameterBag.TryGetPropertyValue(key, out var node) || node == null) return false;
            try
            {
                value = node.Deserialize<T>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public T? GetParameterOrDefault<T>(string key, T? defaultValue = default)
        {
            return TryGetParameter<T>(key, out var v) ? v : defaultValue;
        }

        public bool RemoveParameter(string key)
        {
            if (ParameterBag == null) return false;
            return ParameterBag.Remove(key);
        }
    }
}