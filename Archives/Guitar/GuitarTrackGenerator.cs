// AI: purpose=Generate guitar/comp track using CompRhythmPatternLibrary + CompVoicingSelector for multi-note comp voicings.
// AI: keep program number 27 for Electric Guitar; tracks previousVoicing for voice-leading continuity across bars; returns sorted by AbsoluteTimeTicks.
// AI: applies strum timing offsets to chord voicings for humanized feel (Story 4.3).
// AI: uses fixed busy probability; no tension/energy variation.
// AI: uses CompBehavior system for onset selection and duration shaping; replaces ApplyDensityToPattern.

using Music.MyMidi;
using Music.Song.Material;
using System.Diagnostics;

namespace Music.Generator
{
    internal static class GuitarTrackGenerator
    {
        /// <summary>
        /// Generates guitar/comp track: rhythm pattern-based chord voicings with strum timing.
        /// Uses fixed busy probability (no tension/energy variation).
        /// Uses CompBehavior system for onset selection and duration shaping.
        /// Uses MotifRenderer when motif placed for Comp role.
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
            MotifPlacementPlan? motifPlan,
            MotifPresenceMap? motifPresence,
            int totalBars,
            RandomizationSettingsOld settings,
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
                int absoluteSectionIndex = 0;
                if (sectionTrack.GetActiveSection(bar, out section) && section != null)
                {
                    sectionType = section.SectionType;
                    absoluteSectionIndex = sectionTrack.Sections.IndexOf(section);
                    if (absoluteSectionIndex < 0)
                        absoluteSectionIndex = 0;
                }

                // Story 9.2: Check if motif is placed for Comp role in this bar
                int barWithinSection = section != null ? (bar - section.StartBar) : 0;
                var motifPlacement = motifPlan?.GetPlacementForRoleAndBar("Comp", absoluteSectionIndex, barWithinSection);

                if (motifPlacement != null)
                {
                    // Render motif for this bar
                    var motifNotes = RenderMotifForBar(
                        motifPlacement,
                        harmonyTrack,
                        groovePreset,
                        barTrack,
                        bar,
                        barWithinSection,
                        absoluteSectionIndex,
                        settings,
                        policy);

                    notes.AddRange(motifNotes);
                    previousVoicing = null; // Reset voice leading after motif
                    continue;
                }

                // Use fixed approach for velocity accent (no tension/energy variation)
                int barIndexWithinSection = section != null ? (bar - section.StartBar) : 0;
                // Use fixed accent bias (no tension/energy variation)
                int tensionAccentBias = 0;

                // Section profile for voicing selection
                SectionProfile? sectionProfile = SectionProfile.GetForSectionType(sectionType);

                // Get comp rhythm pattern for this bar
                var pattern = CompRhythmPatternLibrary.GetPattern(
                    groovePreset.Name,
                    sectionType,
                    bar);

                // Select comp behavior based on section/busyProbability
                var behavior = CompBehaviorSelector.SelectBehavior(
                    sectionType,
                    absoluteSectionIndex,
                    barIndexWithinSection,
                    0.5,
                    settings.Seed);

                // Story 8.0.3: Use behavior realizer for onset selection and duration
                var realization = CompBehaviorRealizer.Realize(
                    behavior,
                    compOnsets,
                    pattern,
                    1.0,
                    bar,
                    settings.Seed);

                // Story 9.3: Apply ducking when lead motif active
                bool hasLeadMotif = motifPresence?.HasLeadMotif(absoluteSectionIndex, barIndexWithinSection) ?? false;
                if (hasLeadMotif && realization.SelectedOnsets.Count > 2)
                {
                    // Thin weak-beat onsets by ~30% when lead motif active
                    var localRng = RandomHelpersOld.CreateLocalRng(settings.Seed, $"comp_duck_{bar}", bar, 0m);
                    var duckedOnsets = realization.SelectedOnsets
                        .Where((onset, idx) =>
                        {
                            // Always keep strong beats (1 and 3)
                            decimal beat = onset - (int)onset + 1; // Convert to 1-based beat
                            bool isStrongBeat = beat == 1m || beat == 3m;
                            if (isStrongBeat)
                                return true;

                            // Thin weak beats deterministically (keep 70%)
                            return localRng.NextDouble() < 0.7;
                        })
                        .ToList();

                    // Update realization with ducked onsets (keep original duration multiplier)
                    if (duckedOnsets.Count > 0)
                    {
                        realization = new CompRealizationResult
                        {
                            SelectedOnsets = duckedOnsets,
                            DurationMultiplier = realization.DurationMultiplier
                        };
                    }
                }

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

                    // Validate all notes are in scale
                    foreach (int midiNote in voicing)
                    {
                        int pc = PitchClassUtils.ToPitchClass(midiNote);
                    }

                    // Calculate strum timing offsets for this chord
                    var strumOffsets = StrumTimingEngine.CalculateStrumOffsets(
                        voicing,
                        slot.Bar,
                        slot.OnsetBeat,
                        "comp",
                        settings.Seed);

                    // Calculate base velocity
                    int baseVelocity = 85;
                    int velocity = baseVelocity;

                    // Story 7.5.6: Apply tension accent bias for phrase peaks/ends
                    velocity = ApplyTensionAccentBias(velocity, tensionAccentBias);

                    // Add all notes from the voicing with strum timing offsets
                    for (int i = 0; i < voicing.Count; i++)
                    {
                        int midiNote = voicing[i];
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

                    // Update previous voicing for next onset
                    previousVoicing = voicing;
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
        /// Applies tension accent bias to velocity.
        /// Story 7.5.6: Tension hooks provide phrase-peak/end accent bias.
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
            RandomizationSettingsOld settings,
            HarmonyPolicy policy)
        {
            // Build onset grid from comp onsets
            var compOnsets = groovePreset.AnchorLayer.CompOnsets;
            if (compOnsets == null || compOnsets.Count == 0)
                return new List<PartTrackEvent>();

            var onsetGrid = OnsetGrid.Build(bar, compOnsets, barTrack);

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
                        baseOctave: 4, // Guitar octave
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
