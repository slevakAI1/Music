// AI: purpose=Select optimal chord voicing by minimizing voice movement between consecutive chords.
// AI: invariants=Output ChordRealization uses same pitch classes as input HarmonyPitchContext; top note stays within target range.
// AI: deps=Relies on HarmonyPitchContext.ChordMidiNotes, ChordVoicingHelper for generating candidate voicings.
// AI: perf=Called per onset; generates multiple candidates and evaluates cost; keep candidate count reasonable.

namespace Music.Generator
{
    /// <summary>
    /// Selects chord voicings that minimize voice movement for smooth voice leading.
    /// </summary>
    public static class VoiceLeadingSelector
    {
        // AI: Select: main entry point; generates candidates and picks lowest-cost voicing.
        // AI: sectionProfile: reserved for Story 3.3 (register lift, density); currently unused.
        public static ChordRealization Select(
            ChordRealization? previousRealization,
            HarmonyPitchContext ctx,
            object? sectionProfile = null)
        {
            ArgumentNullException.ThrowIfNull(ctx);

            // Generate candidate voicings (different inversions/registers)
            var candidates = GenerateCandidateVoicings(ctx);

            if (candidates.Count == 0)
            {
                // Fallback: use context's default voicing
                return HarmonyPitchContextBuilder.ToChordRealization(ctx);
            }

            // If no previous voicing, pick the first candidate (arbitrary but deterministic)
            if (previousRealization == null || previousRealization.MidiNotes.Count == 0)
            {
                return candidates[0];
            }

            // Evaluate all candidates and select the one with lowest cost
            ChordRealization best = candidates[0];
            double lowestCost = double.MaxValue;

            foreach (var candidate in candidates)
            {
                double cost = CalculateVoiceLeadingCost(previousRealization, candidate);
                if (cost < lowestCost)
                {
                    lowestCost = cost;
                    best = candidate;
                }
            }

            return best;
        }

        // AI: GenerateCandidateVoicings: creates multiple voicings using different inversions and registers.
        // AI: keeps candidates within reasonable range; avoid generating too many to maintain performance.
        private static List<ChordRealization> GenerateCandidateVoicings(HarmonyPitchContext ctx)
        {
            var candidates = new List<ChordRealization>();

            // Inversions to try (based on chord size)
            string[] inversions = ctx.ChordPitchClasses.Count >= 4
                ? ["root", "3rd", "5th", "7th"]
                : ["root", "3rd", "5th"];

            // Octave shifts to try (base octave, one up, one down)
            int[] octaveShifts = [0, 1, -1];

            foreach (var inversion in inversions)
            {
                foreach (var octaveShift in octaveShifts)
                {
                    int targetOctave = ctx.BaseOctaveUsed + octaveShift;

                    // Skip invalid octaves
                    if (targetOctave < 1 || targetOctave > 7)
                        continue;

                    try
                    {
                        // Generate MIDI notes for this voicing
                        var midiNotes = ChordVoicingHelper.GenerateChordMidiNotes(
                            ctx.Key,
                            ctx.Degree,
                            ctx.Quality,
                            inversion,
                            targetOctave);

                        if (midiNotes.Count == 0)
                            continue;

                        // Create ChordRealization
                        var realization = new ChordRealization
                        {
                            MidiNotes = midiNotes,
                            Inversion = inversion,
                            RegisterCenterMidi = midiNotes[midiNotes.Count / 2],
                            HasColorTone = false, // Basic voicing, no color tones
                            ColorToneTag = null,
                            Density = midiNotes.Count
                        };

                        candidates.Add(realization);
                    }
                    catch
                    {
                        // Skip invalid voicings
                        continue;
                    }
                }
            }

            return candidates;
        }

        // AI: CalculateVoiceLeadingCost: MVP cost function minimizes total semitone movement.
        // AI: formula: sum of absolute differences + penalties for range violations and voice crossings.
        private static double CalculateVoiceLeadingCost(
            ChordRealization previous,
            ChordRealization candidate)
        {
            double cost = 0.0;

            // Primary cost: total absolute semitone movement
            cost += CalculateTotalMovement(previous.MidiNotes, candidate.MidiNotes);

            // Penalty: keep top note within reasonable range (avoid extreme registers)
            cost += CalculateTopNoteRangePenalty(candidate.MidiNotes);

            // Optional penalty: voice crossings (when voices swap order)
            cost += CalculateVoiceCrossingPenalty(previous.MidiNotes, candidate.MidiNotes);

            return cost;
        }

        // AI: CalculateTotalMovement: pairs notes by proximity and sums absolute differences.
        // AI: uses greedy matching: for each previous note, find nearest unmatched candidate note.
        private static double CalculateTotalMovement(
            IReadOnlyList<int> previousNotes,
            IReadOnlyList<int> candidateNotes)
        {
            if (previousNotes.Count == 0 || candidateNotes.Count == 0)
                return 0.0;

            double totalMovement = 0.0;
            var usedIndices = new HashSet<int>();

            // For each note in previous voicing, find closest note in candidate
            foreach (int prevNote in previousNotes)
            {
                int closestDistance = int.MaxValue;
                int closestIndex = -1;

                for (int i = 0; i < candidateNotes.Count; i++)
                {
                    if (usedIndices.Contains(i))
                        continue;

                    int distance = Math.Abs(candidateNotes[i] - prevNote);
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
            int densityChange = Math.Abs(candidateNotes.Count - previousNotes.Count);
            totalMovement += densityChange * 2.0; // Moderate penalty for density changes

            return totalMovement;
        }

        // AI: CalculateTopNoteRangePenalty: penalizes voicings with top note outside target range.
        // AI: target range for keys/pads: C4 (60) to C6 (84); adjust constants if roles differ.
        private static double CalculateTopNoteRangePenalty(IReadOnlyList<int> midiNotes)
        {
            if (midiNotes.Count == 0)
                return 0.0;

            int topNote = midiNotes.Max();

            const int idealMin = 60; // C4
            const int idealMax = 84; // C6

            if (topNote < idealMin)
                return (idealMin - topNote) * 1.5; // Penalty for too low
            if (topNote > idealMax)
                return (topNote - idealMax) * 1.5; // Penalty for too high

            return 0.0; // Within range, no penalty
        }

        // AI: CalculateVoiceCrossingPenalty: adds cost when voice ordering changes (voice crossing).
        // AI: compares sorted orders; differences indicate crossings; keep penalty moderate.
        private static double CalculateVoiceCrossingPenalty(
            IReadOnlyList<int> previousNotes,
            IReadOnlyList<int> candidateNotes)
        {
            if (previousNotes.Count == 0 || candidateNotes.Count == 0)
                return 0.0;

            // Simple heuristic: if the relative ordering of notes changes significantly, penalize
            var prevSorted = previousNotes.OrderBy(n => n).ToList();
            var candSorted = candidateNotes.OrderBy(n => n).ToList();

            int minCount = Math.Min(prevSorted.Count, candSorted.Count);
            int crossings = 0;

            for (int i = 1; i < minCount; i++)
            {
                // Check if interval direction changes (sign flip = crossing)
                int prevInterval = prevSorted[i] - prevSorted[i - 1];
                int candInterval = candSorted[i] - candSorted[i - 1];

                // If intervals differ significantly, it suggests voice crossing
                if (Math.Abs(prevInterval - candInterval) > 3)
                    crossings++;
            }

            return crossings * 1.0; // Moderate penalty per crossing
        }
    }
}
