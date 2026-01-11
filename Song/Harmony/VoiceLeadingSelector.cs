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
        // AI: sectionProfile: used to apply register lift and density constraints per section.
        public static ChordRealization Select(
            ChordRealization? previousRealization,
            HarmonyPitchContext ctx,
            SectionProfile? sectionProfile = null)
        {
            ArgumentNullException.ThrowIfNull(ctx);

            System.Diagnostics.Debug.WriteLine($"\n[VoiceLeadingSelector] ===== SELECT START =====");
            System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] Context: Key={ctx.Key}, Degree={ctx.Degree}, Quality={ctx.Quality}, Bass={ctx.Bass}");
            System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] Context Chord Pitch Classes: {string.Join(", ", ctx.ChordPitchClasses)}");
            System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] Context Chord MIDI Notes: {string.Join(", ", ctx.ChordMidiNotes)}");
            if (sectionProfile != null)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] Section Profile: RegisterLift={sectionProfile.RegisterLift}, MaxDensity={sectionProfile.MaxDensity}");
            }

            // Generate candidate voicings (different inversions/registers)
            var candidates = GenerateCandidateVoicings(ctx, sectionProfile);

            System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] Generated {candidates.Count} candidate voicings");

            if (candidates.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] WARNING: No candidates generated, using fallback");
                // Fallback: use context's default voicing
                return HarmonyPitchContextBuilder.ToChordRealization(ctx);
            }

            // If no previous voicing, pick the first candidate (arbitrary but deterministic)
            if (previousRealization == null || previousRealization.MidiNotes.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] No previous voicing, selecting first candidate");
                var selected = candidates[0];
                System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] SELECTED: {string.Join(", ", selected.MidiNotes)} (Pitch Classes: {string.Join(", ", selected.MidiNotes.Select(m => m % 12))})");
                System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] ===== SELECT COMPLETE =====\n");
                return selected;
            }

            System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] Previous voicing: {string.Join(", ", previousRealization.MidiNotes)}");

            // Evaluate all candidates and select the one with lowest cost
            ChordRealization best = candidates[0];
            double lowestCost = double.MaxValue;

            foreach (var candidate in candidates)
            {
                double cost = CalculateVoiceLeadingCost(previousRealization, candidate, sectionProfile);
                System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] Candidate: {string.Join(", ", candidate.MidiNotes)} Cost={cost:F2}");
                if (cost < lowestCost)
                {
                    lowestCost = cost;
                    best = candidate;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] BEST: {string.Join(", ", best.MidiNotes)} Cost={lowestCost:F2} (Pitch Classes: {string.Join(", ", best.MidiNotes.Select(m => m % 12))})");
            System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector] ===== SELECT COMPLETE =====\n");

            return best;
        }

        // AI: GenerateCandidateVoicings: creates multiple voicings using different inversions and registers.
        // AI: applies section profile register lift and density constraints when generating candidates.
        private static List<ChordRealization> GenerateCandidateVoicings(
            HarmonyPitchContext ctx, 
            SectionProfile? sectionProfile)
        {
            System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector.GenerateCandidateVoicings] Starting...");
            var candidates = new List<ChordRealization>();

            // Apply section profile register lift to base octave
            int baseOctave = ctx.BaseOctaveUsed;
            if (sectionProfile != null)
            {
                // Register lift: convert semitones to octave shift
                baseOctave += sectionProfile.RegisterLift / 12;
                System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector.GenerateCandidateVoicings] Applied RegisterLift: {sectionProfile.RegisterLift} semitones -> BaseOctave adjusted from {ctx.BaseOctaveUsed} to {baseOctave}");
            }

            // Inversion indices to try (0 = root position, 1 = first inversion, etc.)
            int maxInversion = ctx.ChordPitchClasses.Count >= 4 ? 3 : 2;
            
            // Inversion labels for tracking
            string[] inversionLabels = ["root", "1st", "2nd", "3rd"];

            // Octave shifts to try (base octave, one up, one down)
            int[] octaveShifts = [0, 1, -1];

            foreach (var octaveShift in octaveShifts)
            {
                int targetOctave = baseOctave + octaveShift;

                // Skip invalid octaves
                if (targetOctave < 1 || targetOctave > 7)
                {
                    System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector.GenerateCandidateVoicings] Skipping invalid octave: {targetOctave}");
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector.GenerateCandidateVoicings] Trying octave {targetOctave} (shift {octaveShift})");

                try
                {
                    // Generate root position MIDI notes using correct bass parameter
                    var rootPositionNotes = ChordVoicingHelper.GenerateChordMidiNotes(
                        ctx.Key,
                        ctx.Degree,
                        ctx.Quality,
                        ctx.Bass,
                        targetOctave);

                    if (rootPositionNotes.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector.GenerateCandidateVoicings] No notes generated for octave {targetOctave}");
                        continue;
                    }

                    System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector.GenerateCandidateVoicings] Root position notes: {string.Join(", ", rootPositionNotes)}");

                    // Generate candidates for each inversion
                    for (int inversionIndex = 0; inversionIndex <= maxInversion && inversionIndex < rootPositionNotes.Count; inversionIndex++)
                    {
                        // Apply inversion by rotating notes and adjusting octaves
                        var midiNotes = ApplyInversion(rootPositionNotes, inversionIndex);

                        System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector.GenerateCandidateVoicings] Inversion {inversionIndex}: {string.Join(", ", midiNotes)} (Pitch Classes: {string.Join(", ", midiNotes.Select(m => m % 12))})");

                        // Apply density constraint from section profile
                        if (sectionProfile != null && midiNotes.Count > sectionProfile.MaxDensity)
                        {
                            System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector.GenerateCandidateVoicings] Trimming from {midiNotes.Count} to {sectionProfile.MaxDensity} notes for density constraint");
                            // Trim to max density by keeping lowest notes (bass/guide tones)
                            midiNotes = midiNotes.Take(sectionProfile.MaxDensity).ToList();
                        }

                        string inversionLabel = inversionIndex < inversionLabels.Length 
                            ? inversionLabels[inversionIndex] 
                            : $"{inversionIndex}th";

                        // Create ChordRealization
                        var realization = new ChordRealization
                        {
                            MidiNotes = midiNotes,
                            Inversion = inversionLabel,
                            RegisterCenterMidi = midiNotes[midiNotes.Count / 2],
                            HasColorTone = false, // Basic voicing, no color tones
                            ColorToneTag = null,
                            Density = midiNotes.Count
                        };

                        candidates.Add(realization);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector.GenerateCandidateVoicings] Error generating voicing for octave {targetOctave}: {ex.Message}");
                    // Skip invalid voicings
                    continue;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[VoiceLeadingSelector.GenerateCandidateVoicings] Total candidates generated: {candidates.Count}");
            return candidates;
        }

        /// <summary>
        /// Applies inversion to chord MIDI notes by rotating and adjusting octaves.
        /// Inversion 0 = root position, 1 = first inversion (move lowest note up an octave), etc.
        /// </summary>
        private static List<int> ApplyInversion(List<int> rootPositionNotes, int inversionIndex)
        {
            if (inversionIndex == 0 || rootPositionNotes.Count <= 1)
            {
                return new List<int>(rootPositionNotes);
            }

            var notes = new List<int>(rootPositionNotes);
            
            // For each inversion step, move the lowest note up an octave
            for (int i = 0; i < inversionIndex && i < notes.Count; i++)
            {
                // Move the current lowest note up an octave
                notes[i] += 12;
            }

            // Sort to maintain ascending order
            notes.Sort();
            
            return notes;
        }

        // AI: CalculateVoiceLeadingCost: MVP cost function minimizes total semitone movement.
        // AI: formula: sum of absolute differences + penalties for range violations and voice crossings.
        private static double CalculateVoiceLeadingCost(
            ChordRealization previous,
            ChordRealization candidate,
            SectionProfile? sectionProfile)
        {
            double cost = 0.0;

            // Primary cost: total absolute semitone movement
            cost += CalculateTotalMovement(previous.MidiNotes, candidate.MidiNotes);

            // Penalty: keep top note within reasonable range (adjust by register lift)
            cost += CalculateTopNoteRangePenalty(candidate.MidiNotes, sectionProfile);

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
        // AI: target range adjusted by section profile register lift; base range C4 (60) to C6 (84).
        private static double CalculateTopNoteRangePenalty(IReadOnlyList<int> midiNotes, SectionProfile? sectionProfile)
        {
            if (midiNotes.Count == 0)
                return 0.0;

            int topNote = midiNotes.Max();

            // Base ideal range for keys/pads
            int idealMin = 60; // C4
            int idealMax = 84; // C6

            // Apply register lift from section profile
            if (sectionProfile != null)
            {
                idealMin += sectionProfile.RegisterLift;
                idealMax += sectionProfile.RegisterLift;
            }

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
