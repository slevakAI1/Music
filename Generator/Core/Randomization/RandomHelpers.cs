// AI: purpose=Deterministic per-decision RNG & helpers for pitch selection; callers expect reproducible choices.
// AI: invariants=StableSeed/CreateLocalRng determinism; PickMidiNearRange must pick a MIDI with target pc when possible.
// AI: deps=PitchClassUtils, HarmonyPitchContext shapes, RandomizationSettings.Seed; used heavily by PitchRandomizer.
// AI: perf=Hotpath per-note; avoid adding allocations or changing fallback determinism.
// TODO? confirm callers expect scalePitchClasses to be 7 items for diatonic helpers.

namespace Music.Generator
{
    public static class RandomHelpers
    {
        // AI: seed:bar/onset uses bar+decimal onset to separate subdivided beats; absoluteTicks overload for tick-based decisions.
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

        // AI: seed:ticks uses absoluteTicks for decisions where a tick timestamp is authoritative.
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

        // AI: CreateLocalRng returns a deterministic IRandomSource for the provided decision context.
        public static IRandomSource CreateLocalRng(int baseSeed, string partRole, int bar, decimal onsetBeat)
        {
            return new SeededRandomSource(StableSeed(baseSeed, partRole, bar, onsetBeat));
        }

        // AI: CreateLocalRng(ticks) variant; keep behavior identical to bar/onset variant for determinism.
        public static IRandomSource CreateLocalRng(int baseSeed, string partRole, long absoluteTicks)
        {
            return new SeededRandomSource(StableSeed(baseSeed, partRole, absoluteTicks));
        }

        // AI: strongBeat: true only when onsetBeat is an exact integer; relies on decimal modulus equality (no epsilon).
        public static bool IsStrongBeat(decimal onsetBeat)
        {
            return onsetBeat % 1m == 0m;
        }

        // AI: weightedChoice: if totalWeight <= 0 returns first item; keep this deterministic fallback.
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

        // AI: IsInScale/IsChordTone are thin wrappers; keep equality semantics intact.
        public static bool IsInScale(int pitchClass, HarmonyPitchContext ctx)
        {
            return ctx.KeyScalePitchClasses.Contains(pitchClass);
        }

        public static bool IsChordTone(int pitchClass, HarmonyPitchContext ctx)
        {
            return ctx.ChordPitchClasses.Contains(pitchClass);
        }

        // AI: pickMidi: normalize pitchClass, prefer candidate closest to preferredCenter, fallback builds baseMidi from minMidi.
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

        // AI: ChooseRandom throws on empty list; callers expect ArgumentException for invalid input.
        public static T ChooseRandom<T>(IRandomSource rng, IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Items list cannot be empty", nameof(items));

            return items[rng.NextInt(0, items.Count)];
        }

        // AI: FindScaleIndex linear search; returns -1 when pitchClass not present in scale.
        public static int FindScaleIndex(int pitchClass, IReadOnlyList<int> scalePitchClasses)
        {
            for (int i = 0; i < scalePitchClasses.Count; i++)
            {
                if (scalePitchClasses[i] == pitchClass)
                    return i;
            }
            return -1;
        }

        // AI: diatonicNeighbors expects scalePitchClasses to be ordered and typically length 7; returns prev and next, wrapped.
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