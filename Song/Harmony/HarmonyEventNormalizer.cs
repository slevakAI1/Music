// AI: purpose=Canonical normalizer for HarmonyEvent fields; ensures Quality is short-name, Key is "X major|minor", Bass is known.
// AI: invariants=Normalize returns a new normalized event (HarmonyEvent uses init-only properties, no mutation possible).
// AI: deps=Relies on ChordQuality.Normalize for quality short-name conversion; changing that breaks this contract.
// AI: use-sites=UI editor load, import pipelines, any code accepting external/user HarmonyEvent data.
// AI: thread-safety=Stateless static methods; safe for concurrent calls on different HarmonyEvent instances.

namespace Music.Generator
{
    // AI: contract=Key normalized to "X major" or "X minor"; Quality to ChordQuality short name; Bass to known set or clamped to "root".
    public static class HarmonyEventNormalizer
    {
        // AI: KnownBassOptions: canonical bass inversion hints; keep synchronized with UI and voicing logic.
        private static readonly HashSet<string> KnownBassOptions = new(StringComparer.OrdinalIgnoreCase)
        {
            "root", "3rd", "5th", "7th", "9th", "11th", "13th"
        };

        // AI: Normalize: returns a new HarmonyEvent with normalized fields; original unchanged.
        // AI: use-case=HarmonyEvent has init-only properties so mutation is not possible; always returns new instance.
        public static HarmonyEvent Normalize(HarmonyEvent evt)
        {
            return new HarmonyEvent
            {
                StartBar = evt.StartBar,
                StartBeat = evt.StartBeat,
                DurationBeats = evt.DurationBeats,
                Key = NormalizeKey(evt.Key),
                Degree = evt.Degree,
                Quality = ChordQuality.Normalize(evt.Quality),
                Bass = NormalizeBass(evt.Bass)
            };
        }

        // AI: NormalizeKey: enforces "X major" or "X minor" format; preserves root note, defaults to "C major" if unparseable.
        // AI: behavior=Trims whitespace, case-insensitive "major"/"minor" replacement; unknown format returns "C major".
        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "C major";

            var trimmed = key.Trim();

            // Try to parse root and quality
            // Expected formats: "C major", "F# minor", "Bb Major", etc.
            var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
                return "C major"; // malformed

            var root = parts[0]; // e.g., "C", "F#", "Bb"
            var quality = parts[1]; // e.g., "major", "Major", "minor", "MIN"

            // Normalize quality to lowercase "major" or "minor"
            var normalizedQuality = quality.Equals("major", StringComparison.OrdinalIgnoreCase)
                ? "major"
                : quality.Equals("minor", StringComparison.OrdinalIgnoreCase)
                    ? "minor"
                    : "major"; // default to major if unrecognized

            return $"{root} {normalizedQuality}";
        }

        // AI: NormalizeBass: returns bass if in KnownBassOptions (case-insensitive), else defaults to "root".
        // AI: use-case=Prevents invalid bass hints from propagating into voicing logic.
        private static string NormalizeBass(string bass)
        {
            if (string.IsNullOrWhiteSpace(bass))
                return "root";

            var trimmed = bass.Trim();
            return KnownBassOptions.Contains(trimmed) ? trimmed.ToLowerInvariant() : "root";
        }
    }
}
