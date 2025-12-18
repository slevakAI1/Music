namespace Music.Writer
{
    /// <summary>
    /// Utility methods for working with pitch classes and scale construction.
    /// Pitch class: 0=C, 1=C#, 2=D, 3=D#, 4=E, 5=F, 6=F#, 7=G, 8=G#, 9=A, 10=A#, 11=B
    /// </summary>
    public static class PitchClassUtils
    {
        /// <summary>
        /// Converts a MIDI note number to its pitch class (0-11).
        /// </summary>
        /// <param name="midiNoteNumber">MIDI note number (0-127)</param>
        /// <returns>Pitch class (0-11)</returns>
        public static int ToPitchClass(int midiNoteNumber)
        {
            return midiNoteNumber % 12;
        }

        /// <summary>
        /// Gets the pitch class for a note name and alteration.
        /// </summary>
        /// <param name="noteName">Note name (C, D, E, F, G, A, B)</param>
        /// <param name="alteration">Alteration (-2=double flat, -1=flat, 0=natural, 1=sharp, 2=double sharp)</param>
        /// <returns>Pitch class (0-11)</returns>
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

        /// <summary>
        /// Parses a key string (e.g., "C major", "F# minor") and returns the root pitch class.
        /// </summary>
        /// <param name="keyString">Key string in format "NoteName [#/b] mode"</param>
        /// <returns>Root pitch class (0-11)</returns>
        public static int ParseKeyToPitchClass(string keyString)
        {
            if (string.IsNullOrWhiteSpace(keyString))
                throw new ArgumentException("Key string cannot be empty", nameof(keyString));

            var parts = keyString.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid key format: '{keyString}'. Expected 'Note mode'", nameof(keyString));

            var noteStr = parts[0];
            
            // Parse note name
            char noteName = char.ToUpper(noteStr[0]);
            int alteration = 0;

            // Parse alteration if present
            if (noteStr.Length == 2)
            {
                alteration = noteStr[1] switch
                {
                    '#' => 1,
                    'b' => -1,
                    _ => throw new ArgumentException($"Invalid accidental: {noteStr[1]}")
                };
            }
            else if (noteStr.Length > 2)
            {
                throw new ArgumentException($"Invalid note format: '{noteStr}'");
            }

            return GetPitchClass(noteName, alteration);
        }

        /// <summary>
        /// Gets the pitch classes for a major scale starting from the given root pitch class.
        /// </summary>
        /// <param name="rootPitchClass">Root pitch class (0-11)</param>
        /// <returns>List of 7 pitch classes in major scale order</returns>
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

        /// <summary>
        /// Gets the pitch classes for a natural minor scale starting from the given root pitch class.
        /// </summary>
        /// <param name="rootPitchClass">Root pitch class (0-11)</param>
        /// <returns>List of 7 pitch classes in natural minor scale order</returns>
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

        /// <summary>
        /// Gets scale pitch classes for a key string. Currently supports major and minor modes.
        /// </summary>
        /// <param name="keyString">Key string (e.g., "C major", "A minor")</param>
        /// <returns>List of pitch classes in scale order</returns>
        public static IReadOnlyList<int> GetScalePitchClassesForKey(string keyString)
        {
            int rootPitchClass = ParseKeyToPitchClass(keyString);
            
            // Determine mode
            bool isMajor = keyString.Contains("major", StringComparison.OrdinalIgnoreCase);
            
            return isMajor 
                ? GetMajorScalePitchClasses(rootPitchClass)
                : GetNaturalMinorScalePitchClasses(rootPitchClass);
        }
    }
}