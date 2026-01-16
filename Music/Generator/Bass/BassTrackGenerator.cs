// AI: purpose=Generate bass track using BassPatternLibrary for pattern selection and BassChordChangeDetector for approach notes.
// AI: keep MIDI program number 33; patterns replace randomizer for more structured bass lines (Story 5.1 + 5.2); returns sorted by AbsoluteTimeTicks.
// AI: uses fixed approach note probability; no tension/energy variation.

using Music.MyMidi;
using Music.Song.Material;

namespace Music.Generator
{
    internal static class BassTrackGenerator
    {
        /// <summary>
        /// Generates bass track: pattern-based bass lines with optional approach notes to chord changes.
        /// Uses fixed approach note probability (slot-gated).
        /// Uses MotifRenderer when motif placed for Bass role.
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

            // DIAGNOSTIC: Log motif plan info at start
            if (motifPlan == null)
            {
                Tracer.DebugTrace("BassTrackGenerator: motifPlan is NULL");
            }
            else
            {
                Tracer.DebugTrace($"BassTrackGenerator: motifPlan has {motifPlan.Placements.Count} placements");
                foreach (var p in motifPlan.Placements)
                {
                    Tracer.DebugTrace($"  Placement: Role={p.MotifSpec.IntendedRole}, Section={p.AbsoluteSectionIndex}, Bar={p.StartBarWithinSection}, Duration={p.DurationBars}");
                }
            }

            var notes = new List<PartTrackEvent>();
            const int bassOctave = 2;

            // Policy setting: allow approach notes (default false for strict diatonic)
            bool allowApproaches = policy.AllowNonDiatonicChordTones; // Use policy flag

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var groovePreset = grooveTrack.GetActiveGroovePreset(bar);
                var bassOnsets = groovePreset.AnchorLayer.BassOnsets;
                if (bassOnsets == null || bassOnsets.Count == 0)
                    continue;

                // Get section
                MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse;
                Section? section = null;
                int absoluteSectionIndex = 0;
                if (sectionTrack.GetActiveSection(bar, out section) && section != null)
                {
                    sectionType = section.SectionType;
                    absoluteSectionIndex = sectionTrack.Sections.IndexOf(section);
                    if (absoluteSectionIndex < 0)
                        absoluteSectionIndex = 0;
                }

                // Story 9.2: Check if motif is placed for Bass role in this bar
                int barWithinSection = section != null ? (bar - section.StartBar) : 0;
                
                // DIAGNOSTIC: Log lookup attempt
                Tracer.DebugTrace($"Bar {bar}: Looking for Bass motif at section={absoluteSectionIndex}, barWithinSection={barWithinSection}");
                
                var motifPlacement = motifPlan?.GetPlacementForRoleAndBar("Bass", absoluteSectionIndex, barWithinSection);

                // DIAGNOSTIC: Log result
                if (motifPlacement != null)
                {
                    Tracer.DebugTrace($"  FOUND Bass motif: {motifPlacement.MotifSpec.Name}");
                }
                else
                {
                    Tracer.DebugTrace($"  No Bass motif found");
                }

                if (motifPlacement != null)
                {
                    // Render motif for this bar using MotifRenderer
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
                    continue; // Skip pattern-based generation for this bar
                }

                // Use fixed approach note probability
                int barIndexWithinSection = section != null ? (bar - section.StartBar) : 0;
                // Use fixed pull probability bias (no tension/energy variation)
                double pullProbabilityBias = 0.0;

                // Apply fixed busy probability to approach note decisions
                double baseBusyProbability = 0.5;
                double effectiveBusyProbability = ApplyTensionBiasToApproachProbability(
                    baseBusyProbability,
                    pullProbabilityBias);

                // Select bass pattern for this bar using BassPatternLibrary
                var bassPattern = BassPatternLibrary.SelectPattern(
                    groovePreset.Name,
                    sectionType,
                    bar,
                    allowPolicyGated: allowApproaches);

                // Build onset grid for this bar
                var onsetSlots = OnsetGrid.Build(bar, bassOnsets, barTrack);

                // Get active harmony for this bar
                var currentHarmony = harmonyTrack.GetActiveHarmonyEvent(bar, 1m);
                if (currentHarmony == null)
                    continue;

                var ctx = HarmonyPitchContextBuilder.Build(
                    currentHarmony.Key,
                    currentHarmony.Degree,
                    currentHarmony.Quality,
                    currentHarmony.Bass,
                    bassOctave,
                    policy);

                // Get root MIDI note for pattern rendering
                int rootMidi = ctx.ChordMidiNotes.Count > 0 ? ctx.ChordMidiNotes[0] : 36; // Default to C2

                // Render pattern into bass hits
                var patternHits = bassPattern.Render(rootMidi, onsetSlots.Count);

                // Story 7.3: Create deterministic RNG for busy probability checks
                var barRng = RandomHelpersOld.CreateLocalRng(settings.Seed, $"bass_{groovePreset.Name}_{sectionType}", bar, 0m);

                // Process each pattern hit and check for chord change opportunities
                foreach (var hit in patternHits)
                {
                    if (hit.SlotIndex < 0 || hit.SlotIndex >= onsetSlots.Count)
                        continue; // Skip invalid slot indices

                    var slot = onsetSlots[hit.SlotIndex];

                    // Check if chord change is imminent and approach should be inserted
                    bool isChangeImminent = BassChordChangeDetector.IsChangeImminent(
                        harmonyTrack,
                        slot.Bar,
                        slot.OnsetBeat,
                        currentHarmony,
                        lookaheadBeats: 2m);

                    // Apply busy probability to approach note insertion
                    bool busyAllowsApproach = barRng.NextDouble() < effectiveBusyProbability;

                    bool shouldInsertApproach = isChangeImminent &&
                        busyAllowsApproach &&
                        BassChordChangeDetector.ShouldInsertApproach(
                            onsetSlots,
                            hit.SlotIndex,
                            allowApproaches);

                    int midiNote;
                    if (shouldInsertApproach)
                    {
                        // Insert approach note to next chord
                        var nextHarmony = BassChordChangeDetector.GetNextHarmonyEvent(
                            harmonyTrack,
                            slot.Bar,
                            slot.OnsetBeat);

                        if (nextHarmony != null)
                        {
                            // Build context for next chord to get target root
                            var nextCtx = HarmonyPitchContextBuilder.Build(
                                nextHarmony.Key,
                                nextHarmony.Degree,
                                nextHarmony.Quality,
                                nextHarmony.Bass,
                                bassOctave,
                                policy);

                            int targetRoot = nextCtx.ChordMidiNotes.Count > 0 ? nextCtx.ChordMidiNotes[0] : rootMidi;
                            midiNote = BassChordChangeDetector.CalculateDiatonicApproach(targetRoot, approachFromBelow: true);
                        }
                        else
                        {
                            midiNote = hit.MidiNote; // Fallback to pattern note
                        }
                    }
                    else
                    {
                        midiNote = hit.MidiNote; // Use pattern note
                    }

                    // Apply bass range guardrail (clamp to valid range)
                    int originalMidi = midiNote;
                    midiNote = ApplyBassRangeGuardrail(midiNote);

                    // Validate note is in scale
                    int pc = PitchClassUtils.ToPitchClass(midiNote);
                    
                    // Calculate velocity with fixed base value
                    int baseVelocity = 95;
                    int velocity = Math.Clamp(baseVelocity, 1, 127);

                    var noteStart = (int)slot.StartTick;
                    var noteDuration = slot.DurationTicks;

                    // Prevent overlap: trim previous notes of the same pitch that would extend past this note-on
                    NoteOverlapHelper.PreventOverlap(notes, midiNote, noteStart);

                    notes.Add(new PartTrackEvent(
                        noteNumber: midiNote,
                        absoluteTimeTicks: noteStart,
                        noteDurationTicks: noteDuration,
                        noteOnVelocity: velocity));
                }
            }

            // Ensure events are sorted by AbsoluteTimeTicks before returning
            notes = notes.OrderBy(e => e.AbsoluteTimeTicks).ToList();

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        /// <summary>
        /// Applies bass range guardrail to keep notes within audible bass register.
        /// Bass stays in E1 (MIDI 28) to E3 (MIDI 52) range.
        /// This guardrail ensures pattern-generated notes stay in valid range.
        /// </summary>
        private static int ApplyBassRangeGuardrail(int midiNote)
        {
            // Define bass register limits
            const int MinBassMidi = 28;  // E1 - low limit for bass clarity
            const int MaxBassMidi = 52;  // E3 - high limit to avoid mid-range muddy zone

            // Clamp to bass range
            int adjustedNote = Math.Clamp(midiNote, MinBassMidi, MaxBassMidi);

            return adjustedNote;
        }

        /// <summary>
        /// Applies pull probability bias to approach note insertion.
        /// Bias from TensionHooksBuilder (with null tensionQuery, returns 0.0).
        /// CRITICAL: Bias only affects probability; never forces approach when slot invalid.
        /// </summary>
        private static double ApplyTensionBiasToApproachProbability(
            double baseProbability,
            double pullBias)
        {
            // pullBias is in range [-0.20, 0.20] per TensionHooksBuilder (0.0 when no tension query)
            double adjusted = baseProbability + pullBias;
            return Math.Clamp(adjusted, 0.0, 1.0);
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
            // Build onset grid from bass onsets
            var bassOnsets = groovePreset.AnchorLayer.BassOnsets;
            if (bassOnsets == null || bassOnsets.Count == 0)
                return new List<PartTrackEvent>();

            var onsetGrid = OnsetGrid.Build(bar, bassOnsets, barTrack);

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
                        baseOctave: 2, // Bass octave
                        policy);
                    harmonyContexts.Add(ctx);
                }
            }

            // Get tension hooks for velocity accent bias (no tension query = 0.0 bias)
            // Use fixed accent bias (no tension/energy variation)
            int tensionAccentBias = 0;

            // Render motif using MotifRenderer
            var motifTrack = MotifRenderer.Render(
                placement.MotifSpec,
                placement,
                harmonyContexts,
                onsetGrid,
                tensionAccentBias,
                settings.Seed);

            // Convert to list and return
            return motifTrack.PartTrackNoteEvents.ToList();
        }
    }
}
