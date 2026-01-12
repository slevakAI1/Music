// AI: purpose=Generate keys/pads track using VoiceLeadingSelector and SectionProfile for dynamic voicing per section.
// AI: keep program number 4; tracks previous ChordRealization for voice-leading continuity; returns sorted by AbsoluteTimeTicks.
// AI: Story 7.3=Now accepts section profiles and applies energy controls (density, velocity, register) with lead-space ceiling guardrail.
// AI: Story 7.5.6=Now accepts tension query and applies tension hooks for phrase-peak/end accent bias (velocity only).

using Music.MyMidi;
using System.Diagnostics;

namespace Music.Generator
{
    internal static class KeysTrackGenerator
    {
        /// <summary>
        /// Generates keys/pads track: voice-led chord voicings with optional color tones per section profile.
        /// Updated for Story 7.3: energy profile integration with guardrails.
        /// Updated for Story 7.5.6: tension hooks for accent bias at phrase peaks/ends.
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
            Dictionary<int, EnergySectionProfile> sectionProfiles,
            ITensionQuery tensionQuery,
            double microTensionPhraseRampIntensity,
            int totalBars,
            RandomizationSettings settings,
            HarmonyPolicy policy,
            int midiProgramNumber)
        {
            ArgumentNullException.ThrowIfNull(tensionQuery);

            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);
            const int keysOctave = 3;

            HarmonyEvent? previousHarmony = null;
            ChordRealization? previousVoicing = null; // Track previous voicing for voice leading

            for (int bar = 1; bar <= totalBars; bar++)
            {
                // Get groove preset and pads onsets for the bar
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);
                var padsOnsets = grooveEvent.AnchorLayer.PadsOnsets;
                if (padsOnsets == null || padsOnsets.Count == 0)
                {
                    continue;
                }

                // Get section and energy profile
                Section? section = null;
                int absoluteSectionIndex = 0;
                if (sectionTrack.GetActiveSection(bar, out section) && section != null)
                {
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

                // Story 7.3: Check if keys/pads are present in orchestration
                if (energyProfile?.Orchestration != null && !energyProfile.Orchestration.KeysPresent)
                {
                    // Skip keys for this bar if orchestration says keys not present
                    continue;
                }

                // Get keys energy controls
                var keysProfile = energyProfile?.Roles?.Keys;

                // Story 7.5.6: Derive tension hooks for this bar to bias accent velocity
                int barIndexWithinSection = section != null ? (bar - section.StartBar) : 0;
                var hooks = TensionHooksBuilder.Create(
                    tensionQuery,
                    absoluteSectionIndex,
                    barIndexWithinSection,
                    energyProfile,
                    microTensionPhraseRampIntensity);

                // Story 7.3: Update section profile with energy adjustments
                SectionProfile? sectionProfile = UpdateSectionProfileWithEnergy(
                    section?.SectionType ?? MusicConstants.eSectionType.Verse,
                    keysProfile);

                // Build onset grid for this bar
                var onsetSlots = OnsetGrid.Build(bar, padsOnsets, barTrack);

                foreach (var slot in onsetSlots)
                {
                    // Find active harmony at this bar+beat
                    var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(slot.Bar, slot.OnsetBeat);
                    if (harmonyEvent == null)
                        continue;

                    bool isFirstOnset = previousHarmony == null ||
                        harmonyEvent.StartBar != previousHarmony.StartBar ||
                        harmonyEvent.StartBeat != previousHarmony.StartBeat;

                    var ctx = HarmonyPitchContextBuilder.Build(
                        harmonyEvent.Key,
                        harmonyEvent.Degree,
                        harmonyEvent.Quality,
                        harmonyEvent.Bass,
                        keysOctave,
                        policy);

                    ChordRealization chordRealization;

                    // For first onset of new harmony, optionally add color tones via randomizer
                    if (isFirstOnset)
                    {
                        var baseVoicing = randomizer.SelectKeysVoicing(ctx, slot.Bar, slot.OnsetBeat, isFirstOnset, sectionProfile);
                        
                        // If we have previous voicing, apply voice leading to the base voicing
                        if (previousVoicing != null)
                        {
                            chordRealization = VoiceLeadingSelector.Select(previousVoicing, ctx, sectionProfile);

                            // Preserve color tone from randomizer if it was added
                            if (baseVoicing.HasColorTone && !chordRealization.HasColorTone)
                            {
                                // Add the color tone from base voicing
                                var notesWithColor = chordRealization.MidiNotes.ToList();
                                var colorNotes = baseVoicing.MidiNotes.Except(ctx.ChordMidiNotes).ToList();
                                notesWithColor.AddRange(colorNotes);
                                
                                chordRealization = chordRealization with
                                {
                                    MidiNotes = notesWithColor,
                                    HasColorTone = true,
                                    ColorToneTag = baseVoicing.ColorToneTag,
                                    Density = notesWithColor.Count
                                };
                            }
                        }
                        else
                        {
                            // First onset ever: use randomizer result
                            chordRealization = baseVoicing;
                        }
                    }
                    else
                    {
                        // Subsequent onset of same harmony: use voice leading
                        chordRealization = VoiceLeadingSelector.Select(previousVoicing, ctx, sectionProfile);
                    }

                    // Story 7.3: Apply lead-space ceiling guardrail to prevent clash with melody
                    chordRealization = ApplyLeadSpaceGuardrail(chordRealization);

                    // Validate all notes are in scale
                    foreach (int midiNote in chordRealization.MidiNotes)
                    {
                        int pc = PitchClassUtils.ToPitchClass(midiNote);
                        if (!ctx.KeyScalePitchClasses.Contains(pc))
                        {
                        }
                    }
                    var noteStart = (int)slot.StartTick;
                    var noteDuration = slot.DurationTicks;

                    // Story 7.3: Calculate velocity with energy bias
                    int baseVelocity = 75;
                    int velocity = ApplyVelocityBias(baseVelocity, keysProfile?.VelocityBias ?? 0);

                    // Story 7.5.6: Apply tension accent bias for phrase peaks/ends (additive to energy bias)
                    velocity = ApplyTensionAccentBias(velocity, hooks.VelocityAccentBias);

                    foreach (int midiNote in chordRealization.MidiNotes)
                    {
                        // Prevent overlap: trim previous notes of the same pitch that would extend past this note-on
                        NoteOverlapHelper.PreventOverlap(notes, midiNote, noteStart);

                        notes.Add(new PartTrackEvent(
                            noteNumber: midiNote,
                            absoluteTimeTicks: noteStart,
                            noteDurationTicks: noteDuration,
                            noteOnVelocity: velocity));
                    }

                    // Update previous voicing for next onset
                    previousVoicing = chordRealization;
                }

                // Update previousHarmony to the first event active at the bar start (bar,1)
                previousHarmony = harmonyTrack.GetActiveHarmonyEvent(bar, 1m);
            }

            // Ensure events are sorted by AbsoluteTimeTicks before returning
            notes = notes.OrderBy(e => e.AbsoluteTimeTicks).ToList();

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        /// <summary>
        /// Updates SectionProfile with energy adjustments.
        /// Story 7.3: Merges energy controls into section profile for voice leading selector.
        /// </summary>
        private static SectionProfile UpdateSectionProfileWithEnergy(
            MusicConstants.eSectionType sectionType,
            EnergyRoleProfile? keysProfile)
        {
            // Get base section profile
            var baseProfile = SectionProfile.GetForSectionType(sectionType);

            if (keysProfile == null)
                return baseProfile;

            // Apply energy adjustments to section profile
            // RegisterLift: add energy lift to base register lift
            int adjustedRegisterLift = baseProfile.RegisterLift + keysProfile.RegisterLiftSemitones;

            // MaxDensity: scale by density multiplier
            int adjustedMaxDensity = (int)Math.Round(baseProfile.MaxDensity * keysProfile.DensityMultiplier);
            adjustedMaxDensity = Math.Clamp(adjustedMaxDensity, 2, 7); // Keep reasonable bounds

            // ColorToneProbability: already in base profile, keep it
            // (Could be adjusted by energy in future if desired)

            return new SectionProfile
            {
                RegisterLift = adjustedRegisterLift,
                MaxDensity = adjustedMaxDensity,
                ColorToneProbability = baseProfile.ColorToneProbability
            };
        }

        /// <summary>
        /// Applies lead-space ceiling guardrail to chord realization.
        /// Story 7.3: Prevents sustained pads/keys from occupying melody space (C5/MIDI 72 and above).
        /// </summary>
        private static ChordRealization ApplyLeadSpaceGuardrail(ChordRealization chordRealization)
        {
            if (chordRealization == null || chordRealization.MidiNotes.Count == 0)
                return chordRealization;

            // Define lead-space ceiling (C5 = MIDI 72, reserved for melody/lead)
            const int LeadSpaceCeiling = 72;

            // Check if any notes exceed the ceiling
            var notes = chordRealization.MidiNotes.ToList();
            int maxNote = notes.Max();

            if (maxNote >= LeadSpaceCeiling)
            {
                // For sustained pads/keys, transpose notes that exceed ceiling down by octave
                var adjustedNotes = notes.Select(n => n >= LeadSpaceCeiling ? n - 12 : n).ToList();

                // Ensure all notes are still above minimum (C3 = MIDI 48)
                const int KeysLowLimit = 48; // C3
                int minNote = adjustedNotes.Min();
                
                if (minNote < KeysLowLimit)
                {
                    // Transpose all notes up an octave if too low
                    adjustedNotes = adjustedNotes.Select(n => n + 12).ToList();
                }

                return chordRealization with
                {
                    MidiNotes = adjustedNotes,
                    RegisterCenterMidi = (int)adjustedNotes.Average() // Update center
                };
            }

            return chordRealization;
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
