//// AI: purpose=Apply deterministic strum/roll micro-timing offsets to chord voicings for humanized feel.
//// AI: invariants=Offsets are always positive; deterministic from seed; spread is proportional to note count.
//// AI: deps=Used by Generator for comp track; minimal state; can be expanded to keys/pads later.
//// AI: perf=Lightweight calculation; called per chord onset; no allocations beyond return list.

///*
// Notes about the strumming perception
//•	If the voicing count is consistently two but it sounds like different densities, that’s likely the strum offsets + sustain/velocity/timbre causing the perception. Try:
//•	Lowering StrumTimingEngine.DefaultMaxSpreadTicks (e.g., to 8) or using CalculateEvenStrumOffsets to make notes truly simultaneous.
//•	Increasing velocities for quieter notes so they aren’t masked by other instruments.
//•	Switching program to muted (28) or overdriven (29) — different patches emphasize the staggered offsets differently.
// */
//namespace Music.Generator
//{
//    /// <summary>
//    /// Generates deterministic micro-timing offsets for chord voicings to simulate strum/roll humanization.
//    /// </summary>
//    public static class StrumTimingEngine
//    {
//        // AI: Default spread: 0-30 ticks (~1/16th of a quarter note at 480 PPQ) for natural strum feel.
//        private const int DefaultMaxSpreadTicks = 30;

//        /// <summary>
//        /// Calculates strum timing offsets for a chord voicing.
//        /// Returns a list of tick offsets (one per note) to be added to the base onset time.
//        /// Offsets are deterministic based on bar, onset beat, and role.
//        /// </summary>
//        /// <param name="voicing">The MIDI notes in the chord (ordered low to high).</param>
//        /// <param name="bar">Bar number for deterministic seed.</param>
//        /// <param name="onsetBeat">Onset beat for deterministic seed.</param>
//        /// <param name="role">Part role (e.g., "comp") for deterministic seed.</param>
//        /// <param name="baseSeed">Base random seed from settings.</param>
//        /// <param name="maxSpreadTicks">Maximum spread across the chord in ticks (default 30).</param>
//        /// <returns>List of tick offsets, one per note in voicing order.</returns>
//        public static List<int> CalculateStrumOffsets(
//            IReadOnlyList<int> voicing,
//            int bar,
//            decimal onsetBeat,
//            string role,
//            int baseSeed,
//            int maxSpreadTicks = DefaultMaxSpreadTicks)
//        {
//            ArgumentNullException.ThrowIfNull(voicing);

//            if (voicing.Count == 0)
//                return new List<int>();

//            // Single note: no offset needed
//            if (voicing.Count == 1)
//                return new List<int> { 0 };

//            // Create deterministic Rng for this chord onset
//            var rng = RandomHelpersOld.CreateLocalRng(baseSeed, role, bar, onsetBeat);

//            // Strategy: distribute offsets across 0 to maxSpreadTicks
//            // Use slight randomization to avoid mechanical feel while keeping deterministic
//            var offsets = new List<int>(voicing.Count);

//            // Calculate base interval between notes
//            double baseInterval = (double)maxSpreadTicks / (voicing.Count - 1);

//            for (int i = 0; i < voicing.Count; i++)
//            {
//                // Calculate base offset for this note position
//                double baseOffset = i * baseInterval;

//                // Add small random variation (+/- 20% of base interval)
//                double variation = baseInterval * 0.2 * (rng.NextDouble() - 0.5);
                
//                // Clamp to valid range
//                int offset = (int)Math.Clamp(baseOffset + variation, 0, maxSpreadTicks);
                
//                offsets.Add(offset);
//            }

//            return offsets;
//        }

//        /// <summary>
//        /// Simplified version that returns evenly-spaced offsets without randomization.
//        /// Use when you want a perfectly mechanical strum pattern.
//        /// </summary>
//        public static List<int> CalculateEvenStrumOffsets(
//            int noteCount,
//            int maxSpreadTicks = DefaultMaxSpreadTicks)
//        {
//            if (noteCount <= 0)
//                return new List<int>();

//            if (noteCount == 1)
//                return new List<int> { 0 };

//            var offsets = new List<int>(noteCount);
//            double interval = (double)maxSpreadTicks / (noteCount - 1);

//            for (int i = 0; i < noteCount; i++)
//            {
//                offsets.Add((int)(i * interval));
//            }

//            return offsets;
//        }
//    }
//}
