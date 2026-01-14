namespace Music.Generator
{
    // AI: Quality: pair of (ShortName, LongName). ShortName may be empty for 'Major' (default).
    public readonly record struct Quality(string ShortName, string LongName);

    // AI: All: canonical set and display order; keep this as authoritative list when adding/removing qualities.
    public static class ChordQuality
    {
        public static readonly IReadOnlyList<Quality> All = new Quality[]
        {
            // Triads
            new("", "Major"),
            new("m", "Minor"),
            new("dim", "Diminished"),
            new("aug", "Augmented"),
            new("sus2", "Sus2"),
            new("sus4", "Sus4"),
            new("5", "Power5"),

            // 6ths
            new("6", "Major6"),
            new("m6", "Minor6"),
            new("6/9", "Major6Add9"),

            // 7ths
            new("7", "Dominant7"),
            new("maj7", "Major7"),
            new("m7", "Minor7"),
            new("dim7", "Diminished7"),
            new("m7b5", "HalfDiminished7"),
            new("m(maj7)", "MinorMajor7"),

            // Extensions
            new("9", "Dominant9"),
            new("maj9", "Major9"),
            new("m9", "Minor9"),
            new("11", "Dominant11"),
            new("13", "Dominant13"),

            // Adds
            new("add9", "MajorAdd9"),
            new("add11", "MajorAdd11"),
        };

        // AI: ShortNames/LongNames are derived for convenience; do not modify them directly.
        public static readonly IReadOnlyList<string> ShortNames = All.Select(q => q.ShortName).ToList();
        public static readonly IReadOnlyList<string> LongNames = All.Select(q => q.LongName).ToList();

        // AI: Mappings use OrdinalIgnoreCase for robust input handling; update if case-sensitivity is required.
        private static readonly Dictionary<string, string> _shortToLong =
            All.ToDictionary(q => q.ShortName, q => q.LongName, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> _longToShort =
            All.ToDictionary(q => q.LongName, q => q.ShortName, StringComparer.OrdinalIgnoreCase);

        // AI: ToLongName: returns mapped long name or input unchanged for forward compatibility.
        public static string ToLongName(string shortName)
        {
            if (string.IsNullOrWhiteSpace(shortName))
                return shortName;
            return _shortToLong.TryGetValue(shortName.Trim(), out var longName) ? longName : shortName;
        }

        // AI: ToShortName: returns mapped short symbol or input unchanged for forward compatibility.
        public static string ToShortName(string longName)
        {
            if (string.IsNullOrWhiteSpace(longName))
                return longName;
            return _longToShort.TryGetValue(longName.Trim(), out var shortName) ? shortName : longName;
        }

        // AI: Normalize: returns canonical short name; empty/whitespace -> "" (Major); unknown returns trimmed input.
        public static string Normalize(string quality)
        {
            if (string.IsNullOrWhiteSpace(quality))
                return ""; // Default to Major

            var trimmed = quality.Trim();

            // Already a short name?
            if (_shortToLong.ContainsKey(trimmed))
                return _shortToLong.First(kvp =>
                    kvp.Key.Equals(trimmed, StringComparison.OrdinalIgnoreCase)).Key;

            // Long name?
            if (_longToShort.TryGetValue(trimmed, out var shortName))
                return shortName;

            // Unknown - return as-is for forward compatibility
            return trimmed;
        }

        // AI: IsValid: returns true only when input matches a known short or long name (case-insensitive); empty string valid (Major).
        // AI: behavior=Empty string "" is valid (Major short name); only null or whitespace-only strings are invalid.
        public static bool IsValid(string quality)
        {
            // Null check
            if (quality == null)
                return false;
            
            // Empty string is valid (Major chord short name "")
            if (quality == "")
                return true;
            
            // Whitespace-only strings are invalid
            if (string.IsNullOrWhiteSpace(quality))
                return false;
            
            var trimmed = quality.Trim();
            return _shortToLong.ContainsKey(trimmed) || _longToShort.ContainsKey(trimmed);
        }
    }
}