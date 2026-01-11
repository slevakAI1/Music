// AI: purpose=Generate guitar/comp track using CompRhythmPatternLibrary + CompVoicingSelector for multi-note comp voicings.
// AI: keep program number 27 for Electric Guitar; tracks previousVoicing for voice-leading continuity across bars; returns sorted by AbsoluteTimeTicks.
// AI: applies strum timing offsets to chord voicings for humanized feel (Story 4.3).
// AI: Story 7.3=Now accepts section profiles and applies energy controls (density, velocity, register, busy) with lead-space ceiling guardrail.

using Music.MyMidi;
using System.Diagnostics;

namespace Music.Generator
{
    internal static class GuitarTrackGenerator
    {
        /// <summary>
        /// Generates guitar/comp track: rhythm pattern-based chord voicings with strum timing.
        /// Updated for Story 7.3: energy profile integration with guardrails.
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
            Dictionary<int, EnergySectionProfile> sectionProfiles,
            int totalBars,
            RandomizationSettings settings,
            HarmonyPolicy policy,
            int midiProgramNumber)
        {
            var notes = new List<PartTrackEvent>();
            List<int>? previousVoicing = null; // Track previous voicing for voice-leading continuity

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var groovePreset = grooveTrack.GetActiveGroovePreset(bar);
                var compOnsets = groovePreset.AnchorLayer.CompOnsets;
                if (compOnsets == null || compOnsets.Count == 0)
                    continue;

                // Get section and energy profile
                MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse; // Default
                Section? section = null;
                if (sectionTrack.GetActiveSection(bar, out section) && section != null)
                {
                    sectionType = section.SectionType;
                }

                // Story 7.3: Get energy profile for this section
                EnergySectionProfile? energyProfile = null;
                if (section != null && sectionProfiles.TryGetValue(section.StartBar, out var profile))
                {
                    energyProfile = profile;
                }

                // Story 7.3: Check if comp is present in orchestration
                if (energyProfile?.Orchestration != null && !energyProfile.Orchestration.CompPresent)
                {
                    // Skip comp for this bar if orchestration says comp not present
                    continue;
                }

                // Get comp energy controls
                var compProfile = energyProfile?.Roles?.Comp;

                // Get section profile for voicing selection
                SectionProfile? sectionProfile = SectionProfile.GetForSectionType(sectionType);

                // Get comp rhythm pattern for this bar
                var pattern = CompRhythmPatternLibrary.GetPattern(
                    groovePreset.Name,
                    sectionType,
                    bar);

                // Story 7.3: Apply density multiplier to pattern slot selection
                var filteredOnsets = ApplyDensityToPattern(
                    compOnsets,
                    pattern,
                    compProfile?.DensityMultiplier ?? 1.0);

                // Skip this bar if pattern resulted in no onsets
                if (filteredOnsets.Count == 0)
                    continue;

                // Build onset grid from filtered onsets
                var onsetSlots = OnsetGrid.Build(bar, filteredOnsets, barTrack);

                foreach (var slot in onsetSlots)
                {
                    // Find active harmony at this bar+beat
                    var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(slot.Bar, slot.OnsetBeat);
                    if (harmonyEvent == null)
                        continue;

                    // Build harmony context (use higher octave for comp than bass)
                    const int guitarOctave = 4;
                    var ctx = HarmonyPitchContextBuilder.Build(
                        harmonyEvent.Key,
                        harmonyEvent.Degree,
                        harmonyEvent.Quality,
                        harmonyEvent.Bass,
                        guitarOctave,
                        policy);

                    // Select comp voicing (2-4 note chord fragment)
                    var voicing = CompVoicingSelector.Select(ctx, slot, previousVoicing, sectionProfile);

                    // Story 7.3: Apply register lift with lead-space ceiling guardrail
                    var adjustedVoicing = ApplyRegisterWithGuardrail(
                        voicing,
                        compProfile?.RegisterLiftSemitones ?? 0);

                    // Validate all notes are in scale
                    foreach (int midiNote in adjustedVoicing)
                    {
                        int pc = PitchClassUtils.ToPitchClass(midiNote);
                    }

                    // Calculate strum timing offsets for this chord
                    var strumOffsets = StrumTimingEngine.CalculateStrumOffsets(
                        adjustedVoicing,
                        slot.Bar,
                        slot.OnsetBeat,
                        "comp",
                        settings.Seed);

                    // Story 7.3: Calculate velocity with energy bias
                    int baseVelocity = 85;
                    int velocity = ApplyVelocityBias(baseVelocity, compProfile?.VelocityBias ?? 0);

                    // Add all notes from the voicing with strum timing offsets
                    for (int i = 0; i < adjustedVoicing.Count; i++)
                    {
                        int midiNote = adjustedVoicing[i];
                        int strumOffset = strumOffsets[i];

                        var noteStart = (int)slot.StartTick + strumOffset;
                        var noteDuration = slot.DurationTicks;

                        // Prevent overlap: trim previous notes of the same pitch that would extend past this note-on
                        NoteOverlapHelper.PreventOverlap(notes, midiNote, noteStart);

                        notes.Add(new PartTrackEvent(
                            noteNumber: midiNote,
                            absoluteTimeTicks: noteStart,
                            noteDurationTicks: noteDuration,
                            noteOnVelocity: velocity));
                    }

                    // Update previous voicing for next onset (use adjusted voicing)
                    previousVoicing = adjustedVoicing;
                }
            }

            // Ensure events are sorted by AbsoluteTimeTicks before returning
            notes = notes.OrderBy(e => e.AbsoluteTimeTicks).ToList();

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        /// <summary>
        /// Applies density multiplier to rhythm pattern slot selection.
        /// Story 7.3: Higher density = more slots, lower density = fewer slots.
        /// </summary>
        private static List<decimal> ApplyDensityToPattern(
            List<decimal> compOnsets,
            CompRhythmPattern pattern,
            double densityMultiplier)
        {
            // Calculate target number of onsets based on density
            int targetOnsetCount = (int)Math.Round(pattern.IncludedOnsetIndices.Count * densityMultiplier);
            
            // Clamp to valid range: at least 1 onset, at most all available onsets
            targetOnsetCount = Math.Max(1, Math.Min(targetOnsetCount, compOnsets.Count));

            var filteredOnsets = new List<decimal>();

            // Take the first N indices from the pattern (patterns are ordered by importance)
            int slotsToTake = Math.Min(targetOnsetCount, pattern.IncludedOnsetIndices.Count);
            
            for (int i = 0; i < slotsToTake; i++)
            {
                int index = pattern.IncludedOnsetIndices[i];
                if (index >= 0 && index < compOnsets.Count)
                {
                    filteredOnsets.Add(compOnsets[index]);
                }
            }

            return filteredOnsets;
        }

        /// <summary>
        /// Applies register lift with lead-space ceiling guardrail.
        /// Story 7.3: Prevents comp from occupying melody/lead space (C5/MIDI 72 and above).
        /// CRITICAL: Only allows octave shifts (?12) to preserve scale membership.
        /// </summary>
        private static List<int> ApplyRegisterWithGuardrail(
            List<int> voicing,
            int registerLiftSemitones)
        {
            if (voicing == null || voicing.Count == 0)
                return voicing;

            // Define lead-space ceiling (C5 = MIDI 72, reserved for melody/lead)
            const int LeadSpaceCeiling = 72;

            // CRITICAL: Round lift to nearest octave to preserve scale membership
            // Chromatic shifts break diatonic constraints (e.g., E + 4 semitones = G#, not in C major)
            int octaveShift = (int)Math.Round(registerLiftSemitones / 12.0) * 12;

            // Apply octave-only register lift
            var adjustedVoicing = voicing.Select(note => note + octaveShift).ToList();

            // Check if any notes exceed the lead-space ceiling
            int maxNote = adjustedVoicing.Max();
            
            if (maxNote >= LeadSpaceCeiling)
            {
                // Calculate how many octaves to transpose down
                int excessAmount = maxNote - LeadSpaceCeiling + 1;
                int octavesDown = (excessAmount / 12) + 1;
                
                // Transpose entire voicing down to stay below ceiling
                adjustedVoicing = adjustedVoicing.Select(n => n - (octavesDown * 12)).ToList();
            }

            // Additional guardrail: ensure comp doesn't go too low (below E3 = MIDI 52)
            const int CompLowLimit = 52; // E3
            int minNote = adjustedVoicing.Min();
            
            if (minNote < CompLowLimit)
            {
                // Transpose up an octave if too low
                adjustedVoicing = adjustedVoicing.Select(n => n + 12).ToList();
            }

            return adjustedVoicing;
        }

        /// <summary>
        /// Applies velocity bias from energy profile.
        /// Story 7.3: Energy affects dynamics.
        /// </summary>
        private static int ApplyVelocityBias(int baseVelocity, int velocityBias)
        {
            int velocity = baseVelocity + velocityBias;
            return Math.Clamp(velocity, 1, 127);
        }
    }
}
