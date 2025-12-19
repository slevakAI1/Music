using Music.Generator;
using System.Collections.Generic;

namespace Music.Generator
{
    /// <summary>
    /// Helper methods for controlled randomness in pitch generation.
    /// </summary>
    public static class RandomHelpers
    {
        /// <summary>
        /// Creates a stable local seed for per-decision RNG.
        /// Combining base seed with context ensures edits earlier don't reshuffle later decisions.
        /// </summary>
        /// <param name="baseSeed">Master seed from settings</param>
        /// <param name="partRole">Part identifier (e.g., "bass", "guitar", "keys")</param>
        /// <param name="bar">1-based bar number</param>
        /// <param name="onsetBeat">Beat position within the bar (can be fractional)</param>
        /// <returns>A deterministic seed for this specific decision point</returns>
        public static int StableSeed(int baseSeed, string partRole, int bar, decimal onsetBeat)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + baseSeed;
                h = h * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(partRole ?? "");
                h = h * 31 + bar;
                h = h * 31 + onsetBeat.GetHashCode();
                return h;
            }
        }

        /// <summary>
        /// Creates a stable local seed using absolute tick position.
        /// </summary>
        public static int StableSeed(int baseSeed, string partRole, long absoluteTicks)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + baseSeed;
                h = h * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(partRole ?? "");
                h = h * 31 + absoluteTicks.GetHashCode();
                return h;
            }
        }

        /// <summary>
        /// Creates a new SeededRandomSource for a specific decision point.
        /// Use this to ensure each pitch decision is independently reproducible.
        /// </summary>
        public static IRandomSource CreateLocalRng(int baseSeed, string partRole, int bar, decimal onsetBeat)
        {
            return new SeededRandomSource(StableSeed(baseSeed, partRole, bar, onsetBeat));
        }

        /// <summary>
        /// Creates a new SeededRandomSource using absolute tick position.
        /// </summary>
        public static IRandomSource CreateLocalRng(int baseSeed, string partRole, long absoluteTicks)
        {
            return new SeededRandomSource(StableSeed(baseSeed, partRole, absoluteTicks));
        }

        /// <summary>
        /// Determines if a beat position is a strong beat (on the beat, not subdivided).
        /// </summary>
        /// <param name="onsetBeat">Beat position (1-based or 0-based decimal)</param>
        /// <returns>True if this is a strong beat (integer position)</returns>
        public static bool IsStrongBeat(decimal onsetBeat)
        {
            return onsetBeat % 1m == 0m;
        }

        /// <summary>
        /// Performs a weighted random choice from a set of options.
        /// </summary>
        /// <param name="rng">Random source</param>
        /// <param name="items">Array of (value, weight) tuples</param>
        /// <returns>The chosen value</returns>
        public static int WeightedChoice(IRandomSource rng, (int value, double weight)[] items)
        {
            if (items == null || items.Length == 0)
                throw new ArgumentException("Items array cannot be empty", nameof(items));

            double totalWeight = 0;
            foreach (var item in items)
                totalWeight += item.weight;

            if (totalWeight <= 0)
                return items[0].value;

            double roll = rng.NextDouble() * totalWeight;
            double cumulative = 0;

            foreach (var item in items)
            {
                cumulative += item.weight;
                if (roll < cumulative)
                    return item.value;
            }

            // Fallback to last item (handles floating-point edge cases)
            return items[^1].value;
        }

        /// <summary>
        /// Checks if a pitch class is in the current key's scale.
        /// </summary>
        public static bool IsInScale(int pitchClass, HarmonyPitchContext ctx)
        {
            return ctx.KeyScalePitchClasses.Contains(pitchClass);
        }

        /// <summary>
        /// Checks if a pitch class is a chord tone in the current harmony.
        /// </summary>
        public static bool IsChordTone(int pitchClass, HarmonyPitchContext ctx)
        {
            return ctx.ChordPitchClasses.Contains(pitchClass);
        }

        /// <summary>
        /// Finds a MIDI note number with the given pitch class within the specified range,
        /// preferring notes closer to the center.
        /// </summary>
        /// <param name="pitchClass">Target pitch class (0-11)</param>
        /// <param name="minMidi">Minimum MIDI note (inclusive)</param>
        /// <param name="maxMidi">Maximum MIDI note (inclusive)</param>
        /// <param name="preferredCenter">Preferred center MIDI note</param>
        /// <returns>MIDI note number with the target pitch class</returns>
        public static int PickMidiNearRange(int pitchClass, int minMidi, int maxMidi, int preferredCenter)
        {
            // Normalize pitch class
            pitchClass = ((pitchClass % 12) + 12) % 12;

            // Find all MIDI notes in range with this pitch class
            var candidates = new List<int>();
            for (int midi = minMidi; midi <= maxMidi; midi++)
            {
                if (PitchClassUtils.ToPitchClass(midi) == pitchClass)
                    candidates.Add(midi);
            }

            if (candidates.Count == 0)
            {
                // Fallback: find the closest MIDI note with this pitch class
                int baseMidi = minMidi + pitchClass - PitchClassUtils.ToPitchClass(minMidi);
                if (baseMidi < minMidi) baseMidi += 12;
                return Math.Clamp(baseMidi, minMidi, maxMidi);
            }

            // Choose the candidate closest to preferred center
            int best = candidates[0];
            int bestDistance = Math.Abs(best - preferredCenter);

            foreach (var c in candidates)
            {
                int dist = Math.Abs(c - preferredCenter);
                if (dist < bestDistance)
                {
                    best = c;
                    bestDistance = dist;
                }
            }

            return best;
        }

        /// <summary>
        /// Randomly selects an element from a list.
        /// </summary>
        public static T ChooseRandom<T>(IRandomSource rng, IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Items list cannot be empty", nameof(items));

            return items[rng.NextInt(0, items.Count)];
        }

        /// <summary>
        /// Finds the index of a pitch class in the scale, or -1 if not found.
        /// </summary>
        public static int FindScaleIndex(int pitchClass, IReadOnlyList<int> scalePitchClasses)
        {
            for (int i = 0; i < scalePitchClasses.Count; i++)
            {
                if (scalePitchClasses[i] == pitchClass)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Gets diatonic neighbor pitch classes (scale-adjacent) for passing tone selection.
        /// </summary>
        /// <param name="currentPitchClass">Current pitch class</param>
        /// <param name="scalePitchClasses">The 7 pitch classes of the scale in order</param>
        /// <returns>List of neighbor pitch classes (0-2 elements)</returns>
        public static List<int> GetDiatonicNeighbors(int currentPitchClass, IReadOnlyList<int> scalePitchClasses)
        {
            var neighbors = new List<int>();
            int idx = FindScaleIndex(currentPitchClass, scalePitchClasses);

            if (idx < 0)
                return neighbors;

            // Get scale-adjacent neighbors (wrap around the 7-note scale)
            int prevIdx = (idx + 6) % 7; // idx - 1, wrapped
            int nextIdx = (idx + 1) % 7;

            neighbors.Add(scalePitchClasses[prevIdx]);
            neighbors.Add(scalePitchClasses[nextIdx]);

            return neighbors;
        }
    }
}