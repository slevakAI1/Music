// AI: purpose=Generate bass track using BassPatternLibrary for pattern selection and BassChordChangeDetector for approach notes.
// AI: keep MIDI program number 33; patterns replace randomizer for more structured bass lines (Story 5.1 + 5.2); returns sorted by AbsoluteTimeTicks.
// AI: Story 7.3=Now accepts section profiles and applies energy controls (density, velocity, busy) with bass range guardrails.
// AI: Story 7.5.6=Now accepts tension query; applies tension PullProbabilityBias to approach note insertion (only when groove slot allows).

using Music.MyMidi;

namespace Music.Generator
{
    internal static class BassTrackGenerator
    {
        /// <summary>
        /// Generates bass track: pattern-based bass lines with optional approach notes to chord changes.
        /// Updated for Story 7.3: energy profile integration with guardrails.
        /// Updated for Story 7.5.6: tension hooks for pickup/approach bias (slot-gated).
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
            const int bassOctave = 2;

            // Policy setting: allow approach notes (default false for strict diatonic)
            bool allowApproaches = policy.AllowNonDiatonicChordTones; // Use policy flag

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);
                var bassOnsets = grooveEvent.AnchorLayer.BassOnsets;
                if (bassOnsets == null || bassOnsets.Count == 0)
                    continue;

                // Get section and energy profile
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

                // Story 7.3: Get energy profile for this section
                EnergySectionProfile? energyProfile = null;
                if (section != null && sectionProfiles.TryGetValue(section.StartBar, out var profile))
                {
                    energyProfile = profile;
                }

                // Story 7.3: Check if bass is present in orchestration
                if (energyProfile?.Orchestration != null && !energyProfile.Orchestration.BassPresent)
                {
                    // Skip bass for this bar if orchestration says bass not present
                    continue;
                }

                // Get bass energy controls
                var bassProfile = energyProfile?.Roles?.Bass;

                // Story 7.5.6: Derive tension hooks for this bar to bias approach note probability
                int barIndexWithinSection = section != null ? (bar - section.StartBar) : 0;
                var hooks = TensionHooksBuilder.Create(
                    tensionQuery,
                    absoluteSectionIndex,
                    barIndexWithinSection,
                    energyProfile,
                    microTensionPhraseRampIntensity);

                // Story 7.3: Apply busy probability to approach note decisions
                // Story 7.5.6: Further biased by tension PullProbabilityBias (phrase-end anticipation)
                double baseBusyProbability = bassProfile?.BusyProbability ?? 0.5;
                double effectiveBusyProbability = ApplyTensionBiasToApproachProbability(
                    baseBusyProbability,
                    hooks.PullProbabilityBias);

                // Select bass pattern for this bar using BassPatternLibrary
                var bassPattern = BassPatternLibrary.SelectPattern(
                    grooveEvent.Name,
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
                var barRng = RandomHelpers.CreateLocalRng(settings.Seed, $"bass_{grooveEvent.Name}_{sectionType}", bar, 0m);

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

                    // Story 7.3: Apply busy probability to approach note insertion
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

                    // Story 7.3: Apply bass range guardrail (no register lift for bass, but clamp to valid range)
                    int originalMidi = midiNote;
                    midiNote = ApplyBassRangeGuardrail(midiNote);

                    // Validate note is in scale
                    int pc = PitchClassUtils.ToPitchClass(midiNote);
                    
                    // Story 7.3: Calculate velocity with energy bias
                    int baseVelocity = 95;
                    int velocity = ApplyVelocityBias(baseVelocity, bassProfile?.VelocityBias ?? 0);

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
        /// Story 7.3: Bass stays in E1 (MIDI 28) to E3 (MIDI 52) range.
        /// Note: RegisterLift is 0 for bass in energy profile, so no lift applied.
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
        /// Applies velocity bias from energy profile.
        /// Story 7.3: Energy affects dynamics.
        /// </summary>
        private static int ApplyVelocityBias(int baseVelocity, int velocityBias)
        {
            int velocity = baseVelocity + velocityBias;
            return Math.Clamp(velocity, 1, 127);
        }

        /// <summary>
        /// Applies tension pull probability bias to approach note insertion.
        /// Story 7.5.6: Tension hooks increase pickup/approach probability at phrase peaks/ends.
        /// CRITICAL: Bias only affects probability; never forces approach when slot invalid.
        /// </summary>
        private static double ApplyTensionBiasToApproachProbability(
            double baseProbability,
            double tensionPullBias)
        {
            // tensionPullBias is in range [-0.20, 0.20] per TensionHooksBuilder
            double adjusted = baseProbability + tensionPullBias;
            return Math.Clamp(adjusted, 0.0, 1.0);
        }
    }
}
