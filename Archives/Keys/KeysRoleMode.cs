//// AI: purpose=Deterministic selection of keys/pads playing mode based on section/busyProbability.
//// AI: invariants=Selection is deterministic by (sectionType, absoluteSectionIndex, barIndex, busyProbability, seed).
//// AI: change=Add new modes by extending enum and updating SelectMode logic.

//namespace Music.Generator
//{
//    /// <summary>
//    /// Distinct playing modes for keys/pads that produce audibly different results.
//    /// </summary>
//    public enum KeysRoleMode
//    {
//        /// <summary>
//        /// Sustain: hold chord across bar/half-bar, minimal re-attacks.
//        /// Typical for: low energy sections, intros, outros.
//        /// </summary>
//        Sustain,

//        /// <summary>
//        /// Pulse: re-strike on selected beats, moderate sustain.
//        /// Typical for: verses, mid-energy sections.
//        /// </summary>
//        Pulse,

//        /// <summary>
//        /// Rhythmic: follow pad onsets more closely, shorter notes.
//        /// Typical for: choruses, high-energy sections.
//        /// </summary>
//        Rhythmic,

//        /// <summary>
//        /// SplitVoicing: split voicing across 2 hits (low notes first, then upper).
//        /// Typical for: builds, transitions, dramatic moments.
//        /// </summary>
//        SplitVoicing
//    }

//    /// <summary>
//    /// Deterministic selector for keys/pads playing mode.
//    /// </summary>
//    public static class KeysRoleModeSelector
//    {
//        /// <summary>
//        /// Selects keys mode deterministically from context.
//        /// </summary>
//        /// <param name="sectionType">Current section type (Verse, Chorus, etc.)</param>
//        /// <param name="absoluteSectionIndex">0-based index of section in song</param>
//        /// <param name="barIndexWithinSection">0-based bar index within section</param>
//        /// <param name="busyProbability">Keys busy probability [0..1]</param>
//        /// <param name="seed">Master seed for deterministic variation</param>
//        /// <returns>Selected KeysRoleMode</returns>
//        public static KeysRoleMode SelectMode(
//            MusicConstants.eSectionType sectionType,
//            int absoluteSectionIndex,
//            int barIndexWithinSection,
//            double busyProbability,
//            int seed)
//        {
//            // Clamp inputs
//            busyProbability = Math.Clamp(busyProbability, 0.0, 1.0);
            
//            // Activity score based on busy probability
//            double activityScore = busyProbability;

//            // Section-type specific thresholds and biases
//            KeysRoleMode baseMode = sectionType switch
//            {
//                MusicConstants.eSectionType.Intro => activityScore < 0.5 ? KeysRoleMode.Sustain : KeysRoleMode.Pulse,
//                MusicConstants.eSectionType.Verse => SelectVerseMode(activityScore),
//                MusicConstants.eSectionType.Chorus => SelectChorusMode(activityScore),
//                MusicConstants.eSectionType.Bridge => SelectBridgeMode(activityScore, barIndexWithinSection, seed),
//                MusicConstants.eSectionType.Outro => KeysRoleMode.Sustain,
//                MusicConstants.eSectionType.Solo => KeysRoleMode.Sustain, // Back off for solo
//                _ => KeysRoleMode.Pulse
//            };

//            return baseMode;
//        }

//        private static KeysRoleMode SelectVerseMode(double activityScore)
//        {
//            return activityScore switch
//            {
//                < 0.35 => KeysRoleMode.Sustain,
//                < 0.65 => KeysRoleMode.Pulse,
//                _ => KeysRoleMode.Rhythmic
//            };
//        }

//        private static KeysRoleMode SelectChorusMode(double activityScore)
//        {
//            return activityScore switch
//            {
//                < 0.4 => KeysRoleMode.Pulse,
//                _ => KeysRoleMode.Rhythmic
//            };
//        }

//        private static KeysRoleMode SelectBridgeMode(double activityScore, int barIndexWithinSection, int seed)
//        {
//            // Bridge: consider SplitVoicing for dramatic effect on first bar
//            if (barIndexWithinSection == 0 && activityScore > 0.5)
//            {
//                int hash = HashCode.Combine(seed, barIndexWithinSection);
//                if ((hash % 100) < 40) // 40% chance of split voicing at bridge start
//                {
//                    return KeysRoleMode.SplitVoicing;
//                }
//            }
            
//            return activityScore > 0.5 ? KeysRoleMode.Rhythmic : KeysRoleMode.Pulse;
//        }
//    }
//}
