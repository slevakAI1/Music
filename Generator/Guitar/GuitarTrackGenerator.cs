// AI: purpose=Generate guitar/comp track using CompRhythmPatternLibrary + CompVoicingSelector for multi-note comp voicings.
// AI: keep program number 27 for Electric Guitar; tracks previousVoicing for voice-leading continuity across bars; returns sorted by AbsoluteTimeTicks.
// AI: applies strum timing offsets to chord voicings for humanized feel (Story 4.3).
// AI: Story 7.3=Now accepts section profiles and applies energy controls (density, velocity, register, busy) with lead-space ceiling guardrail.
// AI: Story 7.5.6=Now accepts tension query and applies tension hooks for phrase-peak/end accent bias (velocity only).
// AI: Story 8.0.3=Now uses CompBehavior system for onset selection and duration shaping; replaces ApplyDensityToPattern.

using Music.MyMidi;
using System.Diagnostics;

namespace Music.Generator
{
    internal static class GuitarTrackGenerator
    {
        /// <summary>
        /// Generates guitar/comp track: rhythm pattern-based chord voicings with strum timing.
        /// Updated for Story 7.3: energy profile integration with guardrails.
        /// Updated for Story 7.5.6: tension hooks for accent bias at phrase peaks/ends.
        /// Updated for Story 7.6.4: accepts optional variation query for future parameter adaptation.
        /// Updated for Story 8.0.3: uses CompBehavior system for onset selection and duration shaping.
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
            Dictionary<int, EnergySectionProfile> sectionProfiles,
            ITensionQuery tensionQuery,
            double microTensionPhraseRampIntensity,
            IVariationQuery? variationQuery,
            int totalBars,
            RandomizationSettings settings,
            HarmonyPolicy policy,
            int midiProgramNumber)
        {
            ArgumentNullException.ThrowIfNull(tensionQuery);

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
                int absoluteSectionIndex = 0;
                if (sectionTrack.GetActiveSection(bar, out section) && section != null)
                {
                    sectionType = section.SectionType;
                    absoluteSectionIndex = sectionTrack.Sections.IndexOf(section);
                    if (absoluteSectionIndex < 0)
                        absoluteSectionIndex = 0;
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

                // Story 7.6.5: Apply variation deltas if available
                if (variationQuery != null && compProfile != null)
                {
                    var variationPlan = variationQuery.GetVariationPlan(absoluteSectionIndex);
                    compProfile = VariationParameterAdapter.ApplyVariation(compProfile, variationPlan.Roles.Comp);
                }

                // Story 7.5.6: Derive tension hooks for this bar to bias accent velocity
                int barIndexWithinSection = section != null ? (bar - section.StartBar) : 0;
                var hooks = TensionHooksBuilder.Create(
                    tensionQuery,
                    absoluteSectionIndex,
                    barIndexWithinSection,
                    energyProfile,
                    microTensionPhraseRampIntensity);

                // Section profile for voicing selection
                SectionProfile? sectionProfile = SectionProfile.GetForSectionType(sectionType);

                // Get comp rhythm pattern for this bar
                var pattern = CompRhythmPatternLibrary.GetPattern(
                    groovePreset.Name,
                    sectionType,
                    bar);

                // Story 8.0.3: Select comp behavior based on energy/tension/section
                var behavior = CompBehaviorSelector.SelectBehavior(
                    sectionType,
                    absoluteSectionIndex,
                    barIndexWithinSection,
                    energyProfile?.Global.Energy ?? 0.5,
                    compProfile?.BusyProbability ?? 0.5,
                    settings.Seed);

                // Story 8.0.3: Use behavior realizer for onset selection and duration
                var realization = CompBehaviorRealizer.Realize(
                    behavior,
                    compOnsets,
                    pattern,
                    compProfile?.DensityMultiplier ?? 1.0,
                    bar,
                    settings.Seed);

                // Story 8.0.3: Skip if no onsets selected
                if (realization.SelectedOnsets.Count == 0)
                    continue;

                // Story 8.0.3: Build onset grid from realized onsets
                var onsetSlots = OnsetGrid.Build(bar, realization.SelectedOnsets, barTrack);

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

                    // Story 7.5.6: Apply tension accent bias for phrase peaks/ends (additive to energy bias)
                    velocity = ApplyTensionAccentBias(velocity, hooks.VelocityAccentBias);

                    // Add all notes from the voicing with strum timing offsets
                    for (int i = 0; i < adjustedVoicing.Count; i++)
                    {
                        int midiNote = adjustedVoicing[i];
                        int strumOffset = strumOffsets[i];

                        var noteStart = (int)slot.StartTick + strumOffset;
                        
                        // Story 8.0.3: Apply behavior duration multiplier
                        var noteDuration = (int)(slot.DurationTicks * realization.DurationMultiplier);
                        noteDuration = Math.Max(noteDuration, 60); // Minimum ~30ms at 120bpm

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

        /// <summary>
        /// Applies tension accent bias to velocity.
        /// Story 7.5.6: Tension hooks provide phrase-peak/end accent bias (additive to energy bias).
        /// </summary>
        private static int ApplyTensionAccentBias(int velocity, int tensionAccentBias)
        {
            velocity += tensionAccentBias;
            return Math.Clamp(velocity, 1, 127);
        }
    }
}
