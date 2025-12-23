namespace Music.Generator
{
    /// <summary>
    /// Provides pitch information for a harmony event, including chord tones and scale tones.
    /// This is a read-only context object used by generators to make pitch choices.
    /// </summary>
    public sealed class HarmonyPitchContext
    {
        /// <summary>
        /// The source harmony event (optional, useful for debugging).
        /// </summary>
        public Music.Generator.HarmonyEvent? SourceEvent { get; init; }

        /// <summary>
        /// The root pitch class of the key (0-11, where 0=C).
        /// This is the tonic of the key signature, not the chord root.
        /// </summary>
        public int KeyRootPitchClass { get; init; }

        /// <summary>
        /// The root pitch class of the chord (0-11, where 0=C).
        /// This is calculated from the scale degree applied to the key.
        /// For example: C major, degree 5 → chord root is G (pitch class 7).
        /// </summary>
        public int ChordRootPitchClass { get; init; }

        /// <summary>
        /// Pitch classes of the chord tones (0-11), sorted and unique.
        /// </summary>
        public IReadOnlyList<int> ChordPitchClasses { get; init; } = Array.Empty<int>();

        /// <summary>
        /// Pitch classes of the key scale (0-11), sorted and unique.
        /// For MVP, this is the major scale for major keys and natural minor for minor keys.
        /// </summary>
        public IReadOnlyList<int> KeyScalePitchClasses { get; init; } = Array.Empty<int>();

        /// <summary>
        /// Actual MIDI note numbers for the chord tones in a usable register.
        /// These are the exact notes returned by the chord conversion system.
        /// </summary>
        public IReadOnlyList<int> ChordMidiNotes { get; init; } = Array.Empty<int>();

        /// <summary>
        /// The base octave used when generating the chord MIDI notes.
        /// </summary>
        public int BaseOctaveUsed { get; init; }

        /// <summary>
        /// The key string from the harmony event (e.g., "C major", "A minor").
        /// </summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>
        /// The scale degree (1-7) from the harmony event.
        /// </summary>
        public int Degree { get; init; }

        /// <summary>
        /// The chord quality from the harmony event (e.g., "Major", "Minor7").
        /// </summary>
        public string Quality { get; init; } = string.Empty;

        /// <summary>
        /// The bass/inversion setting from the harmony event (e.g., "root", "3rd").
        /// </summary>
        public string Bass { get; init; } = string.Empty;
    }
}