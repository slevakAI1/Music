using Music.Designer;

namespace Music.Writer
{
    /// <summary>
    /// Builds a HarmonyPitchContext from a HarmonyEvent.
    /// This is a lightweight helper that reuses the shared chord voicing infrastructure
    /// to extract pitch information for generators.
    /// </summary>
    public static class HarmonyPitchContextBuilder
    {
        /// <summary>
        /// Builds a HarmonyPitchContext from a HarmonyEvent.
        /// </summary>
        /// <param name="harmonyEvent">The harmony event to analyze</param>
        /// <param name="baseOctave">The base octave for chord voicing (default: 4)</param>
        /// <returns>A HarmonyPitchContext containing chord and scale pitch information</returns>
        /// <exception cref="ArgumentNullException">When harmonyEvent is null</exception>
        public static HarmonyPitchContext Build(HarmonyEvent harmonyEvent, int baseOctave = 4)
        {
            if (harmonyEvent == null)
                throw new ArgumentNullException(nameof(harmonyEvent));

            return Build(
                harmonyEvent.Key,
                harmonyEvent.Degree,
                harmonyEvent.Quality,
                harmonyEvent.Bass,
                baseOctave,
                harmonyEvent);
        }

        /// <summary>
        /// Builds a HarmonyPitchContext from harmony parameters.
        /// </summary>
        /// <param name="key">The key (e.g., "C major", "F# minor")</param>
        /// <param name="degree">The scale degree (1-7)</param>
        /// <param name="quality">The chord quality (e.g., "Major", "Minor7")</param>
        /// <param name="bass">The bass note option (e.g., "root", "3rd", "5th")</param>
        /// <param name="baseOctave">The base octave for chord voicing (default: 4)</param>
        /// <param name="sourceEvent">Optional source event for debugging</param>
        /// <returns>A HarmonyPitchContext containing chord and scale pitch information</returns>
        public static HarmonyPitchContext Build(
            string key,
            int degree,
            string quality,
            string bass,
            int baseOctave = 4,
            HarmonyEvent? sourceEvent = null)
        {
            // Step 1: Get chord tones as MIDI note numbers using the shared helper
            var chordMidiNotes = ChordVoicingHelper.GenerateChordMidiNotes(
                key: key,
                degree: degree,
                quality: quality,
                bass: bass,
                baseOctave: baseOctave);

            // Step 2: Convert to pitch classes (0-11), unique and sorted
            var chordPitchClasses = chordMidiNotes
                .Select(PitchClassUtils.ToPitchClass)
                .Distinct()
                .OrderBy(pc => pc)
                .ToList();

            // Step 3: Get the root pitch class from the key
            int rootPitchClass = PitchClassUtils.ParseKeyToPitchClass(key);

            // Step 4: Get the scale pitch classes for the key
            var keyScalePitchClasses = PitchClassUtils.GetScalePitchClassesForKey(key);

            // Step 5: Build and return the context
            return new HarmonyPitchContext
            {
                SourceEvent = sourceEvent,
                RootPitchClass = rootPitchClass,
                ChordPitchClasses = chordPitchClasses,
                KeyScalePitchClasses = keyScalePitchClasses,
                ChordMidiNotes = chordMidiNotes,
                BaseOctaveUsed = baseOctave,
                Key = key,
                Degree = degree,
                Quality = quality,
                Bass = bass
            };
        }
    }
}