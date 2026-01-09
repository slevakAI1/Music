// AI: purpose=Select comp voicings as chord fragments (2-4 notes) with guide tone emphasis and voice-leading continuity.
// AI: invariants=Output MIDI notes are 2-4 count; top note stays below lead ceiling (~A5/81); lowest note above muddy threshold (~E3/52).
// AI: deps=Relies on HarmonyPitchContext, OnsetSlot.IsStrongBeat, SectionProfile; used by Generator.GenerateGuitarTrack.
// AI: perf=Called per comp onset; keep candidate generation minimal; voice-leading cost should be fast heuristic.

namespace Music.Generator
{
    /// <summary>
    /// Selects comp voicings as chord fragments emphasizing guide tones (3rd/7th) with voice-leading continuity.
    /// </summary>
    public static class CompVoicingSelector
    {
        // MIDI range constraints for comp voicings
        private const int MinCompMidi = 52;  // E3 - avoid muddy low voicings
        private const int MaxCompMidi = 81;  // A5 - keep below lead space
        private const int PreferredCompCenter = 60; // C4 - typical comp register

        // Guide tone preference weights
        private const double GuideToneWeight = 3.0;
        private const double ChordToneWeight = 2.0;
        private const double RootWeight = 1.0;  // Lower weight for root (often omitted)

        /// <summary>
        /// Selects a comp voicing as a chord fragment (2-4 notes) based on context and previous voicing.
        /// </summary>
        /// <param name="ctx">Harmony pitch context for the current onset.</param>
        /// <param name="slot">Onset slot providing timing and strong beat information.</param>
        /// <param name="previousVoicing">Previous comp voicing for voice-leading continuity (null if first onset).</param>
        /// <param name="sectionProfile">Section profile for density and register hints (optional).</param>
        /// <returns>List of MIDI note numbers (2-4 notes) for comp voicing.</returns>
        public static List<int> Select(
            HarmonyPitchContext ctx,
            OnsetSlot slot,
            List<int>? previousVoicing,
            SectionProfile? sectionProfile = null)
        {
            ArgumentNullException.ThrowIfNull(ctx);

            // Determine target density (2-4 notes for comp fragments)
            int targetDensity = DetermineCompDensity(slot.IsStrongBeat, sectionProfile);

            // Generate candidate voicings
            var candidates = GenerateCandidateCompVoicings(ctx, targetDensity, slot.IsStrongBeat, sectionProfile);

            if (candidates.Count == 0)
            {
                // Fallback: build minimal two-note voicing from available chord tones
                return BuildFallbackVoicing(ctx, targetDensity);
            }

            // If no previous voicing, select first candidate (deterministic)
            if (previousVoicing == null || previousVoicing.Count == 0)
            {
                return candidates[0];
            }

            // Select candidate with best voice-leading cost
            List<int> bestVoicing = candidates[0];
            double lowestCost = CalculateVoiceLeadingCost(previousVoicing, candidates[0]);

            for (int i = 1; i < candidates.Count; i++)
            {
                double cost = CalculateVoiceLeadingCost(previousVoicing, candidates[i]);
                if (cost < lowestCost)
                {
                    lowestCost = cost;
                    bestVoicing = candidates[i];
                }
            }

            // Debug: write selected voicing count and notes to Debug output window
            System.Diagnostics.Debug.WriteLine($"CompVoicingSelector: bestVoicing.Count={bestVoicing.Count}; notes={string.Join(",", bestVoicing)}");

            return bestVoicing;
        }

        /// <summary>
        /// Determines comp density based on beat strength and section profile.
        /// </summary>
        private static int DetermineCompDensity(bool isStrongBeat, SectionProfile? sectionProfile)
        {
            // Base density: strong beats get fuller voicings
            int baseDensity = isStrongBeat ? 3 : 2;

            // Apply section profile density adjustments
            if (sectionProfile != null)
            {
                // Use section MaxDensity as a cap, but comp typically stays sparse (2-4 notes)
                int maxCompDensity = Math.Min(4, sectionProfile.MaxDensity);
                baseDensity = Math.Min(baseDensity, maxCompDensity);
            }

            return Math.Clamp(baseDensity, 2, 4);
        }

        /// <summary>
        /// Generates candidate comp voicings as chord fragments.
        /// </summary>
        private static List<List<int>> GenerateCandidateCompVoicings(
            HarmonyPitchContext ctx,
            int targetDensity,
            bool isStrongBeat,
            SectionProfile? sectionProfile)
        {
            var candidates = new List<List<int>>();

            // Get available chord tones in comp range
            var availableChordTones = GetCompRangeMidiNotes(ctx.ChordMidiNotes);
            if (availableChordTones.Count < 2)
            {
                // Not enough tones in range
                return candidates;
            }

            // Identify guide tones (3rd and 7th) and root
            var guideTones = IdentifyGuideTones(availableChordTones, ctx);
            var rootNotes = IdentifyRootNotes(availableChordTones, ctx);

            // Strategy 1: Guide tone voicings (3rd + 7th) - preferred for strong beats
            if (isStrongBeat && guideTones.Count >= 2)
            {
                candidates.AddRange(BuildGuideToneVoicings(guideTones, availableChordTones, targetDensity, rootNotes));
            }

            // Strategy 2: Chord fragment voicings (skip root when possible)
            candidates.AddRange(BuildChordFragmentVoicings(availableChordTones, targetDensity, rootNotes, guideTones));

            // Strategy 3: Root-based voicings (fallback for sparse contexts)
            if (rootNotes.Count > 0)
            {
                candidates.AddRange(BuildRootBasedVoicings(rootNotes, availableChordTones, targetDensity));
            }

            // Filter and validate all candidates
            var validCandidates = candidates
                .Where(v => IsValidCompVoicing(v))
                .Distinct(new ListComparer<int>())
                .ToList();

            return validCandidates;
        }

        /// <summary>
        /// Filters chord MIDI notes to comp register range.
        /// </summary>
        private static List<int> GetCompRangeMidiNotes(IReadOnlyList<int> chordMidiNotes)
        {
            return chordMidiNotes
                .Where(midi => midi >= MinCompMidi && midi <= MaxCompMidi)
                .Distinct()
                .OrderBy(midi => midi)
                .ToList();
        }

        /// <summary>
        /// Identifies guide tones (3rd and 7th) from available chord tones.
        /// </summary>
        private static List<int> IdentifyGuideTones(List<int> availableTones, HarmonyPitchContext ctx)
        {
            var guideTones = new List<int>();

            if (ctx.ChordPitchClasses.Count < 2)
                return guideTones;

            // 3rd is typically the second pitch class (index 1)
            // 7th is typically the fourth pitch class (index 3) for 7th chords
            var thirdPc = ctx.ChordPitchClasses.Count > 1 ? ctx.ChordPitchClasses[1] : -1;
            var seventhPc = ctx.ChordPitchClasses.Count > 3 ? ctx.ChordPitchClasses[3] : -1;

            foreach (var midi in availableTones)
            {
                int pc = PitchClassUtils.ToPitchClass(midi);
                if (pc == thirdPc || pc == seventhPc)
                {
                    guideTones.Add(midi);
                }
            }

            return guideTones;
        }

        /// <summary>
        /// Identifies root notes from available chord tones.
        /// </summary>
        private static List<int> IdentifyRootNotes(List<int> availableTones, HarmonyPitchContext ctx)
        {
            var rootNotes = new List<int>();
            int rootPc = ctx.ChordRootPitchClass;

            foreach (var midi in availableTones)
            {
                if (PitchClassUtils.ToPitchClass(midi) == rootPc)
                {
                    rootNotes.Add(midi);
                }
            }

            return rootNotes;
        }

        /// <summary>
        /// Builds voicings centered on guide tones (3rd + 7th).
        /// </summary>
        private static List<List<int>> BuildGuideToneVoicings(
            List<int> guideTones,
            List<int> availableChordTones,
            int targetDensity,
            List<int> rootNotes)
        {
            var voicings = new List<List<int>>();

            // Two-note guide tone voicing (3rd + 7th)
            if (targetDensity >= 2 && guideTones.Count >= 2)
            {
                voicings.Add(new List<int> { guideTones[0], guideTones[1] });
            }

            // Three-note: guide tones + one additional chord tone (prefer non-root)
            if (targetDensity >= 3 && guideTones.Count >= 2)
            {
                var additionalTones = availableChordTones
                    .Where(midi => !guideTones.Contains(midi) && !rootNotes.Contains(midi))
                    .ToList();

                if (additionalTones.Count > 0)
                {
                    voicings.Add(new List<int> { guideTones[0], guideTones[1], additionalTones[0] }.OrderBy(x => x).ToList());
                }
            }

            // Four-note: guide tones + two additional chord tones (still prefer to skip root)
            if (targetDensity >= 4 && guideTones.Count >= 2)
            {
                var additionalTones = availableChordTones
                    .Where(midi => !guideTones.Contains(midi) && !rootNotes.Contains(midi))
                    .Take(2)
                    .ToList();

                if (additionalTones.Count >= 2)
                {
                    voicings.Add(new List<int> { guideTones[0], guideTones[1], additionalTones[0], additionalTones[1] }.OrderBy(x => x).ToList());
                }
            }

            return voicings;
        }

        /// <summary>
        /// Builds chord fragment voicings (prefer skipping root).
        /// </summary>
        private static List<List<int>> BuildChordFragmentVoicings(
            List<int> availableChordTones,
            int targetDensity,
            List<int> rootNotes,
            List<int> guideTones)
        {
            var voicings = new List<List<int>>();

            // Strategy: Take chord tones in clusters, preferring to omit root
            var nonRootTones = availableChordTones
                .Where(midi => !rootNotes.Contains(midi))
                .ToList();

            // Build voicings from clusters of adjacent non-root tones
            if (nonRootTones.Count >= targetDensity)
            {
                for (int i = 0; i <= nonRootTones.Count - targetDensity; i++)
                {
                    var voicing = nonRootTones.Skip(i).Take(targetDensity).ToList();
                    voicings.Add(voicing);
                }
            }

            // Also try mixed voicings (include root only if needed for density)
            if (nonRootTones.Count < targetDensity && rootNotes.Count > 0)
            {
                var mixed = nonRootTones.Take(targetDensity - 1).ToList();
                mixed.AddRange(rootNotes.Take(1));
                voicings.Add(mixed.OrderBy(x => x).ToList());
            }

            return voicings;
        }

        /// <summary>
        /// Builds root-based voicings (fallback when guide tones unavailable).
        /// </summary>
        private static List<List<int>> BuildRootBasedVoicings(
            List<int> rootNotes,
            List<int> availableChordTones,
            int targetDensity)
        {
            var voicings = new List<List<int>>();

            if (rootNotes.Count == 0 || availableChordTones.Count < targetDensity)
                return voicings;

            // Root + adjacent chord tones
            var voicing = new List<int> { rootNotes[0] };
            voicing.AddRange(availableChordTones
                .Where(midi => midi != rootNotes[0])
                .Take(targetDensity - 1));

            if (voicing.Count >= 2)
            {
                voicings.Add(voicing.OrderBy(x => x).ToList());
            }

            return voicings;
        }

        /// <summary>
        /// Validates a comp voicing against range and spacing constraints.
        /// </summary>
        private static bool IsValidCompVoicing(List<int> voicing)
        {
            if (voicing.Count < 2 || voicing.Count > 4)
                return false;

            // Check range constraints
            int lowest = voicing.Min();
            int highest = voicing.Max();

            if (lowest < MinCompMidi || highest > MaxCompMidi)
                return false;

            // Check for muddy close voicings below E3 (52)
            // If lowest note is in the muddy zone, ensure voicing isn't too dense/close
            if (lowest < MinCompMidi + 5) // E3 to G#3 is borderline
            {
                // Ensure at least 4 semitones between lowest two notes
                var sorted = voicing.OrderBy(x => x).ToList();
                if (sorted.Count >= 2 && (sorted[1] - sorted[0]) < 4)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Builds a fallback voicing when candidate generation fails.
        /// </summary>
        private static List<int> BuildFallbackVoicing(HarmonyPitchContext ctx, int targetDensity)
        {
            var voicing = new List<int>();

            // Try to use available chord tones within range
            foreach (var midi in ctx.ChordMidiNotes)
            {
                if (midi >= MinCompMidi && midi <= MaxCompMidi)
                {
                    voicing.Add(midi);
                    if (voicing.Count >= targetDensity)
                        break;
                }
            }

            // If still not enough, transpose chord tones into range
            if (voicing.Count < 2 && ctx.ChordMidiNotes.Count >= 2)
            {
                int pc1 = PitchClassUtils.ToPitchClass(ctx.ChordMidiNotes[0]);
                int pc2 = PitchClassUtils.ToPitchClass(ctx.ChordMidiNotes[1]);

                int midi1 = FindMidiInRange(pc1, MinCompMidi, MaxCompMidi, PreferredCompCenter);
                int midi2 = FindMidiInRange(pc2, MinCompMidi, MaxCompMidi, PreferredCompCenter);

                voicing.Add(midi1);
                voicing.Add(midi2);
            }

            return voicing.Distinct().OrderBy(x => x).ToList();
        }

        /// <summary>
        /// Finds a MIDI note with the given pitch class within the specified range.
        /// </summary>
        private static int FindMidiInRange(int pitchClass, int minMidi, int maxMidi, int preferredCenter)
        {
            pitchClass = ((pitchClass % 12) + 12) % 12;

            // Find all candidates
            var candidates = new List<int>();
            for (int midi = minMidi; midi <= maxMidi; midi++)
            {
                if (PitchClassUtils.ToPitchClass(midi) == pitchClass)
                {
                    candidates.Add(midi);
                }
            }

            if (candidates.Count == 0)
            {
                // Fallback: compute nearest MIDI with this pitch class
                int baseMidi = minMidi + pitchClass;
                while (baseMidi < minMidi) baseMidi += 12;
                return Math.Clamp(baseMidi, minMidi, maxMidi);
            }

            // Return candidate closest to preferred center
            return candidates.OrderBy(c => Math.Abs(c - preferredCenter)).First();
        }

        /// <summary>
        /// Calculates voice-leading cost between two voicings (total semitone movement).
        /// </summary>
        private static double CalculateVoiceLeadingCost(List<int> previousVoicing, List<int> candidateVoicing)
        {
            if (previousVoicing.Count == 0 || candidateVoicing.Count == 0)
                return 0.0;

            double totalMovement = 0.0;
            var usedIndices = new HashSet<int>();

            // For each previous note, find nearest unmatched candidate note
            foreach (int prevNote in previousVoicing)
            {
                int closestDistance = int.MaxValue;
                int closestIndex = -1;

                for (int i = 0; i < candidateVoicing.Count; i++)
                {
                    if (usedIndices.Contains(i))
                        continue;

                    int distance = Math.Abs(candidateVoicing[i] - prevNote);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestIndex = i;
                    }
                }

                if (closestIndex >= 0)
                {
                    totalMovement += closestDistance;
                    usedIndices.Add(closestIndex);
                }
            }

            // Penalty for density changes (adding/removing notes)
            int densityChange = Math.Abs(candidateVoicing.Count - previousVoicing.Count);
            totalMovement += densityChange * 2.0;

            return totalMovement;
        }

        /// <summary>
        /// Comparer for detecting duplicate voicings in candidate lists.
        /// </summary>
        private class ListComparer<T> : IEqualityComparer<List<T>> where T : IEquatable<T>
        {
            public bool Equals(List<T>? x, List<T>? y)
            {
                if (x == null || y == null)
                    return x == y;

                return x.SequenceEqual(y);
            }

            public int GetHashCode(List<T> obj)
            {
                unchecked
                {
                    int hash = 17;
                    foreach (var item in obj)
                    {
                        hash = hash * 31 + (item?.GetHashCode() ?? 0);
                    }
                    return hash;
                }
            }
        }
    }
}
