namespace Music
{
    /// <summary>
    /// Centralized chord quality definitions. Single source of truth for all chord quality strings.
    /// Short names (e.g., "maj", "min7") are used internally; long names (e.g., "Major", "Minor7") are for UI display.
    /// </summary>
    public static class ChordQuality
    {
        /// <summary>
        /// Represents a chord quality with short and long name variants.
        /// </summary>
        public readonly record struct Quality(string ShortName, string LongName);

        /// <summary>
        /// All supported chord qualities. Short names are the canonical internal format.
        /// </summary>
        public static readonly IReadOnlyList<Quality> All = new Quality[]
        {
            // Triads
            new("maj", "Major"),
            new("min", "Minor"),
            new("dim", "Diminished"),
            new("aug", "Augmented"),
            new("sus2", "Sus2"),
            new("sus4", "Sus4"),
            new("5", "Power5"),

            // 6ths
            new("maj6", "Major6"),
            new("min6", "Minor6"),
            new("6/9", "Major6Add9"),

            // 7ths
            new("dom7", "Dominant7"),
            new("maj7", "Major7"),
            new("min7", "Minor7"),
            new("dim7", "Diminished7"),
            new("hdim7", "HalfDiminished7"),
            new("minMaj7", "MinorMajor7"),

            // Extensions
            new("9", "Dominant9"),
            new("maj9", "Major9"),
            new("min9", "Minor9"),
            new("11", "Dominant11"),
            new("13", "Dominant13"),

            // Adds
            new("add9", "MajorAdd9"),
            new("add11", "MajorAdd11"),
        };

        /// <summary>
        /// All short names for dropdowns and lookups.
        /// </summary>
        public static readonly IReadOnlyList<string> ShortNames = All.Select(q => q.ShortName).ToList();

        /// <summary>
        /// All long names for UI display.
        /// </summary>
        public static readonly IReadOnlyList<string> LongNames = All.Select(q => q.LongName).ToList();

        private static readonly Dictionary<string, string> _shortToLong = 
            All.ToDictionary(q => q.ShortName, q => q.LongName, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> _longToShort = 
            All.ToDictionary(q => q.LongName, q => q.ShortName, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Converts a short name to its long name equivalent.
        /// Returns the input unchanged if not found (for forward compatibility).
        /// </summary>
        public static string ToLongName(string shortName)
        {
            if (string.IsNullOrWhiteSpace(shortName))
                return shortName;
            return _shortToLong.TryGetValue(shortName.Trim(), out var longName) ? longName : shortName;
        }

        /// <summary>
        /// Converts a long name to its short name equivalent.
        /// Returns the input unchanged if not found (for forward compatibility).
        /// </summary>
        public static string ToShortName(string longName)
        {
            if (string.IsNullOrWhiteSpace(longName))
                return longName;
            return _longToShort.TryGetValue(longName.Trim(), out var shortName) ? shortName : longName;
        }

        /// <summary>
        /// Normalizes any quality string (short or long) to its canonical short name.
        /// </summary>
        public static string Normalize(string quality)
        {
            if (string.IsNullOrWhiteSpace(quality))
                return "maj"; // Default

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

        /// <summary>
        /// Checks if the given string is a valid chord quality (short or long name).
        /// </summary>
        public static bool IsValid(string quality)
        {
            if (string.IsNullOrWhiteSpace(quality))
                return false;
            var trimmed = quality.Trim();
            return _shortToLong.ContainsKey(trimmed) || _longToShort.ContainsKey(trimmed);
        }
    }
}