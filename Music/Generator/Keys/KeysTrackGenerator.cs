// AI: purpose=Generate keys/pads track using VoiceLeadingSelector and SectionProfile for dynamic voicing per section.
// AI: keep program number 4; tracks previous ChordRealization for voice-leading continuity; returns sorted by AbsoluteTimeTicks.
// AI: uses fixed busy probability; no tension/energy variation.
// AI: uses KeysRoleMode system for audibly distinct playing behaviors (Sustain/Pulse/Rhythmic/SplitVoicing).

using Music.MyMidi;
using Music.Song.Material;
using System.Diagnostics;

namespace Music.Generator
{
    internal static class KeysTrackGenerator
    {
        /// <summary>
        /// Generates keys/pads track: voice-led chord voicings with optional color tones per section profile.
        /// Uses fixed busy probability (no tension/energy variation).
        /// Uses KeysRoleMode system for distinct playing behaviors.
        /// Uses MotifRenderer when motif placed for Keys role.
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
            MotifPlacementPlan? motifPlan,
            MotifPresenceMap? motifPresence,
            int totalBars,
            RandomizationSettings settings,
            HarmonyPolicy policy,
            int midiProgramNumber)
        {

            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);
            const int keysOctave = 3;

            HarmonyEvent? previousHarmony = null;
            ChordRealization? previousVoicing = null; // Track previous voicing for voice leading

            for (int bar = 1; bar <= totalBars; bar++)
            {
                // Get groove preset and pads onsets for the bar
                var groovePreset = grooveTrack.GetActiveGroovePreset(bar);
                var padsOnsets = groovePreset.AnchorLayer.PadsOnsets;
                if (padsOnsets == null || padsOnsets.Count == 0)
                {
                    continue;
                }

                // Get section
                Section? section = null;
                int absoluteSectionIndex = 0;
                if (sectionTrack.GetActiveSection(bar, out section) && section != null)
                {
                    absoluteSectionIndex = sectionTrack.Sections.IndexOf(section);
                    if (absoluteSectionIndex < 0)
                        absoluteSectionIndex = 0;
                }

                // Story 9.2: Check if motif is placed for Keys role in this bar
                int barIndexWithinSection = section != null ? (bar - section.StartBar) : 0;
                var motifPlacement = motifPlan?.GetPlacementForRoleAndBar("Keys", absoluteSectionIndex, barIndexWithinSection);

                if (motifPlacement != null)
                {
                    // Render motif for this bar
                    var motifNotes = RenderMotifForBar(
                        motifPlacement,
                        harmonyTrack,
                        groovePreset,
                        barTrack,
                        bar,
                        barIndexWithinSection,
                        absoluteSectionIndex,
                        settings,
                        policy);

                    notes.AddRange(motifNotes);
                    previousVoicing = null; // Reset voice leading after motif
                    continue;
                }

                // Use fixed accent bias (no tension/energy variation)
                int tensionAccentBias = 0;

                // Select keys mode based on section/busyProbability
                var mode = KeysRoleModeSelector.SelectMode(
                    section?.SectionType ?? MusicConstants.eSectionType.Verse,
                    absoluteSectionIndex,
                    barIndexWithinSection,
                    0.5,
                    settings.Seed);

                // Realize mode into onset selection and duration
                var realization = KeysModeRealizer.Realize(
                    mode,
                    padsOnsets,
                    1.0,
                    bar,
                    settings.Seed);

                // Story 9.3: Apply ducking when lead motif active (shorten sustain, thin onsets)
                bool hasLeadMotif = motifPresence?.HasLeadMotif(absoluteSectionIndex, barIndexWithinSection) ?? false;
                if (hasLeadMotif && realization.SelectedOnsets.Count > 1)
                {
                    // Shorten duration to create breathing room for lead
                    double durationMultiplier = Math.Max(0.5, realization.DurationMultiplier * 0.75);

                    // Thin weak-beat onsets if density is high (keep at least 1 onset)
                    var duckedOnsets = realization.SelectedOnsets;
                    if (realization.SelectedOnsets.Count > 2)
                    {
                        var localRng = RandomHelpers.CreateLocalRng(settings.Seed, $"keys_duck_{bar}", bar, 0m);
                        duckedOnsets = realization.SelectedOnsets
                            .Where((onset, idx) =>
                            {
                                // Keep first onset (typically strong beat)
                                if (idx == 0)
                                    return true;

                                // Thin remaining onsets (keep 70%)
                                return localRng.NextDouble() < 0.7;
                            })
                            .ToList();
                    }

                    if (duckedOnsets.Count > 0)
                    {
                        realization = new KeysRealizationResult
                        {
                            SelectedOnsets = duckedOnsets,
                            DurationMultiplier = durationMultiplier,
                            SplitUpperOnsetIndex = realization.SplitUpperOnsetIndex
                        };
                    }
                }

                // Skip if no onsets selected
                if (realization.SelectedOnsets.Count == 0)
                    continue;

                // Get section profile
                SectionProfile? sectionProfile = SectionProfile.GetForSectionType(
                    section?.SectionType ?? MusicConstants.eSectionType.Verse);

                // Story 8.0.6: Build onset grid from realized onsets
                var onsetSlots = OnsetGrid.Build(bar, realization.SelectedOnsets, barTrack);

                // Story 8.0.6: Track slot index for SplitVoicing mode
                int slotIndex = 0;

                foreach (var slot in onsetSlots)
                {
                    // Find active harmony at this bar+beat
                    var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(slot.Bar, slot.OnsetBeat);
                    if (harmonyEvent == null)
                    {
                        slotIndex++;
                        continue;
                    }

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

                    // Apply lead-space ceiling guardrail to prevent clash with melody
                    chordRealization = ApplyLeadSpaceGuardrail(chordRealization);

                    // For SplitVoicing mode, split voicing between onsets
                    bool isSplitUpperOnset = mode == KeysRoleMode.SplitVoicing && 
                        slotIndex == realization.SplitUpperOnsetIndex;

                    List<int> notesToPlay;
                    if (mode == KeysRoleMode.SplitVoicing && realization.SplitUpperOnsetIndex >= 0)
                    {
                        // Split the voicing
                        var sortedNotes = chordRealization.MidiNotes.OrderBy(n => n).ToList();
                        int splitPoint = sortedNotes.Count / 2;

                        if (isSplitUpperOnset)
                        {
                            // Use only upper half of voicing
                            notesToPlay = sortedNotes.Skip(splitPoint).ToList();
                        }
                        else
                        {
                            // Use only lower half of voicing (include middle note for odd counts)
                            notesToPlay = sortedNotes.Take(splitPoint + 1).ToList();
                        }
                    }
                    else
                    {
                        // Use full voicing
                        notesToPlay = chordRealization.MidiNotes.ToList();
                    }

                    // Validate all notes are in scale
                    foreach (int midiNote in chordRealization.MidiNotes)
                    {
                        int pc = PitchClassUtils.ToPitchClass(midiNote);
                        if (!ctx.KeyScalePitchClasses.Contains(pc))
                        {
                        }
                    }
                    var noteStart = (int)slot.StartTick;

                    // Apply mode duration multiplier
                    var noteDuration = (int)(slot.DurationTicks * realization.DurationMultiplier);
                    // Clamp to avoid overlapping into next bar
                    var maxDuration = (int)barTrack.GetBarEndTick(bar) - noteStart;
                    noteDuration = Math.Clamp(noteDuration, 60, Math.Max(60, maxDuration));

                    // Calculate base velocity and apply tension accent bias
                    int baseVelocity = 75;
                    int velocity = ApplyTensionAccentBias(baseVelocity, tensionAccentBias);

                    // Use split notes for SplitVoicing mode, full voicing otherwise
                    foreach (int midiNote in notesToPlay)
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

                    // Increment slot index
                    slotIndex++;
                }

                // Update previousHarmony to the first event active at the bar start (bar,1)
                previousHarmony = harmonyTrack.GetActiveHarmonyEvent(bar, 1m);
            }

            // Ensure events are sorted by AbsoluteTimeTicks before returning
            notes = notes.OrderBy(e => e.AbsoluteTimeTicks).ToList();

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        /// <summary>
        /// Applies lead-space ceiling guardrail to chord realization.
        /// Prevents sustained pads/keys from occupying melody space (C5/MIDI 72 and above).
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
        /// Applies tension accent bias to velocity.
        /// Tension hooks provide phrase-peak/end accent bias.
        /// </summary>
        private static int ApplyTensionAccentBias(int velocity, int tensionAccentBias)
        {
            velocity += tensionAccentBias;
            return Math.Clamp(velocity, 1, 127);
        }

        /// <summary>
        /// Renders motif notes for a specific bar using MotifRenderer.
        /// Converts motif spec to actual note events for this bar.
        /// </summary>
        private static List<PartTrackEvent> RenderMotifForBar(
            MotifPlacement placement,
            HarmonyTrack harmonyTrack,
            GroovePreset groovePreset,
            BarTrack barTrack,
            int bar,
            int barWithinSection,
            int absoluteSectionIndex,
            RandomizationSettings settings,
            HarmonyPolicy policy)
        {
            // Build onset grid from pads onsets
            var padsOnsets = groovePreset.AnchorLayer.PadsOnsets;
            if (padsOnsets == null || padsOnsets.Count == 0)
                return new List<PartTrackEvent>();

            var onsetGrid = OnsetGrid.Build(bar, padsOnsets, barTrack);

            // Build harmony contexts for this bar
            var harmonyContexts = new List<HarmonyPitchContext>();
            foreach (var slot in onsetGrid)
            {
                var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(slot.Bar, slot.OnsetBeat);
                if (harmonyEvent != null)
                {
                    var ctx = HarmonyPitchContextBuilder.Build(
                        harmonyEvent.Key,
                        harmonyEvent.Degree,
                        harmonyEvent.Quality,
                        harmonyEvent.Bass,
                        baseOctave: 3, // Keys octave
                        policy);
                    harmonyContexts.Add(ctx);
                }
            }

            // Get tension hooks for velocity accent bias (no tension query = 0.0 bias)
            // Use fixed accent bias (no tension/energy variation)
            int tensionAccentBias = 0;

            // Render motif
            var motifTrack = MotifRenderer.Render(
                placement.MotifSpec,
                placement,
                harmonyContexts,
                onsetGrid,
                tensionAccentBias,
                settings.Seed);

            return motifTrack.PartTrackNoteEvents.ToList();
        }
    }
}
