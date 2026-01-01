// AI: purpose=Utilities for parsing keys, mapping notes to pitch classes, and building scale pitch-class lists.
// AI: invariants=Pitch classes are 0-11; ParseKey normalizes mode to 'major'|'minor' and validates format; functions throw on invalid input.
// AI: deps=Used by chord voicing and context builders; changing exception behavior or normalization breaks many callers.
// AI: perf=Lightweight helper used during context/build time, not tight audio hotpaths.

namespace Music.Generator
{
    // AI: ParsedKey: canonical parsed components from ParseKey (NoteLetter, Alteration, Mode).
    internal record struct ParsedKey(
        char NoteLetter,
        int Alteration,
        string Mode
    );

    public static class PitchClassUtils
    {
        // AI: ToPitchClass: returns 0-11; handles negative MIDI defensively via double-mod wrapping.
        public static int ToPitchClass(int midiNoteNumber)
        {
            // Handle negative numbers correctly (double modulo wrap)
            return (midiNoteNumber % 12 + 12) % 12;
        }

        // AI: GetPitchClass: maps note letter + alteration (-2..2) to 0-11; invalid letters throw ArgumentException.
        public static int GetPitchClass(char noteName, int alteration = 0)
        {
            int basePitch = char.ToUpper(noteName) switch
            {
                'C' => 0,
                'D' => 2,
                'E' => 4,
                'F' => 5,
                'G' => 7,
                'A' => 9,
                'B' => 11,
                _ => throw new ArgumentException($"Invalid note name: {noteName}", nameof(noteName))
            };

            // Apply alteration and wrap to 0-11
            int pitchClass = (basePitch + alteration) % 12;
            if (pitchClass < 0) pitchClass += 12;
            
            return pitchClass;
        }

        // AI: ParseKey: single source of truth for key parsing. Expects exactly two tokens: note (letter+optional '#'/'b') and mode.
        // AI: mode normalized to lowercase 'major' or 'minor'; accepts single accidental '#' or 'b' only; throws ArgumentException on invalid input.
        internal static ParsedKey ParseKey(string keyString)
        {
            if (string.IsNullOrWhiteSpace(keyString))
                throw new ArgumentException("Key string cannot be empty", nameof(keyString));

            var parts = keyString.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid key format: '{keyString}'. Expected 'Note mode'", nameof(keyString));

            var noteStr = parts[0];
            var modeStr = parts[1].Trim();

            // Validate mode and normalize to lowercase
            var normalizedMode = modeStr.ToLowerInvariant();
            if (normalizedMode != "major" && normalizedMode != "minor")
                throw new ArgumentException($"Invalid mode: '{modeStr}'. Expected 'major' or 'minor'", nameof(keyString));

            // Parse note name
            if (noteStr.Length == 0)
                throw new ArgumentException($"Empty note name in key: '{keyString}'", nameof(keyString));

            char noteLetter = char.ToUpper(noteStr[0]);
            if (noteLetter < 'A' || noteLetter > 'G')
                throw new ArgumentException($"Invalid note letter: '{noteLetter}'", nameof(keyString));

            int alteration = 0;

            // Parse alteration if present
            if (noteStr.Length == 2)
            {
                alteration = noteStr[1] switch
                {
                    '#' => 1,
                    'b' => -1,
                    _ => throw new ArgumentException($"Invalid accidental: '{noteStr[1]}'. Expected '#' or 'b'")
                };
            }
            else if (noteStr.Length > 2)
            {
                throw new ArgumentException($"Invalid note format: '{noteStr}'. Expected single letter + optional accidental");
            }

            // Return normalized mode (lowercase) to prevent drift
            return new ParsedKey(noteLetter, alteration, normalizedMode);
        }

        // AI: ParseKeyToPitchClass: convenience wrapper around ParseKey then GetPitchClass.
        public static int ParseKeyToPitchClass(string keyString)
        {
            var parsed = ParseKey(keyString);
            return GetPitchClass(parsed.NoteLetter, parsed.Alteration);
        }

        // AI: GetMajorScalePitchClasses: returns 7 pitch classes for major scale starting at root; validates root in 0..11.
        public static IReadOnlyList<int> GetMajorScalePitchClasses(int rootPitchClass)
        {
            if (rootPitchClass < 0 || rootPitchClass > 11)
                throw new ArgumentOutOfRangeException(nameof(rootPitchClass), "Pitch class must be 0-11");

            // Major scale intervals (whole-whole-half-whole-whole-whole-half)
            int[] intervals = { 0, 2, 4, 5, 7, 9, 11 };
            
            return intervals
                .Select(interval => (rootPitchClass + interval) % 12)
                .ToList();
        }

        // AI: GetNaturalMinorScalePitchClasses: returns 7 pitch classes for natural minor scale; validates root in 0..11.
        public static IReadOnlyList<int> GetNaturalMinorScalePitchClasses(int rootPitchClass)
        {
            if (rootPitchClass < 0 || rootPitchClass > 11)
                throw new ArgumentOutOfRangeException(nameof(rootPitchClass), "Pitch class must be 0-11");

            // Natural minor scale intervals (whole-half-whole-whole-half-whole-whole)
            int[] intervals = { 0, 2, 3, 5, 7, 8, 10 };
            
            return intervals
                .Select(interval => (rootPitchClass + interval) % 12)
                .ToList();
        }

        // AI: GetScalePitchClassesForKey: parses key then returns major or natural minor scale pcs based on mode.
        public static IReadOnlyList<int> GetScalePitchClassesForKey(string keyString)
        {
            var parsed = ParseKey(keyString);
            int rootPitchClass = GetPitchClass(parsed.NoteLetter, parsed.Alteration);
            
            // Mode already validated in ParseKey
            bool isMajor = parsed.Mode.Equals("major", StringComparison.OrdinalIgnoreCase);
            
            return isMajor 
                ? GetMajorScalePitchClasses(rootPitchClass)
                : GetNaturalMinorScalePitchClasses(rootPitchClass);
        }
    }
}