// AI: purpose=Generate PartTracks for parts using harmony, groove, and timing; uses controlled randomness via PitchRandomizer.
// AI: invariants=Order: Harmony->Groove->Bar must align; totalBars derived from SectionTrack; tick calc uses (onsetBeat-1)*TicksPerQuarterNote.
// AI: deps=Relies on HarmonyTrack.GetActiveHarmonyEvent, GrooveTrack.GetActiveGroovePreset, BarTrack.GetBar, MusicConstants.TicksPerQuarterNote.
// AI: perf=Not real-time; called once per song generation; avoid heavy allocations in inner loops.
// TODO? confirm behavior when groove/pads onsets null vs empty; current code skips in both cases.
// IMPORTANT: Generator MUST NOT rebuild or mutate `BarTrack`.
// The `BarTrack` is considered a read-only timing "ruler" for generation and must be built
// by the caller (e.g., editor/export pipeline) via `BarTrack.RebuildFromTimingTrack(...)` before
// calling `Generator.Generate(...)`. Rebuilding `BarTrack` inside the generator would mask
// upstream integrity issues and is intentionally avoided.

using Music.MyMidi;

namespace Music.Generator
{
    public static class Generator
    {
        // AI: Generate: validates harmony track before generation; fast-fail on invalid data prevents silent errors.
        // AI: behavior=Runs HarmonyValidator with default options (StrictDiatonicChordTones=true) to catch F# minor crashes.
        public static GeneratorResult Generate(SongContext songContext)
        {
            // validate songcontext is not null
            ValidateSongContext(songContext);
            ValidateSectionTrack(songContext.SectionTrack);
            ValidateHarmonyTrack(songContext.HarmonyTrack);
            ValidateTimeSignatureTrack(songContext.Song.TimeSignatureTrack);
            ValidateGrooveTrack(songContext.GrooveTrack);

            // Validate harmony events for musical correctness before generation
            var validationResult = HarmonyValidator.ValidateTrack(
                songContext.HarmonyTrack,
                new HarmonyValidationOptions
                {
                    ApplyFixes = false,
                    StrictDiatonicChordTones = false,
                    ClampInvalidBassToRoot = false,
                    AllowUnknownQuality = false
                });

            if (!validationResult.IsValid)
            {
                var errorMessage = "Harmony validation failed:\n" + string.Join("\n", validationResult.Errors);
                throw new InvalidOperationException(errorMessage);
            }

            // Get total bars from section track
            int totalBars = songContext.SectionTrack.TotalBars;

            // Use default randomization settings and harmony policy
            var settings = RandomizationSettings.Default;
            var harmonyPolicy = HarmonyPolicy.Default;

            // Resolve MIDI program numbers from VoiceSet
            int bassProgramNumber = GetProgramNumberForRole(songContext.Voices, "Bass", defaultProgram: 33);
            int compProgramNumber = GetProgramNumberForRole(songContext.Voices, "Comp", defaultProgram: 27);
            int padsProgramNumber = GetProgramNumberForRole(songContext.Voices, "Pads", defaultProgram: 4);
            int drumProgramNumber = GetProgramNumberForRole(songContext.Voices, "DrumKit", defaultProgram: 255);

            return new GeneratorResult
            {
                BassTrack = GenerateBassTrack(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    totalBars,
                    settings,
                    harmonyPolicy,
                    bassProgramNumber),            //  Randomization settings 

                GuitarTrack = GenerateGuitarTrack(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    songContext.SectionTrack,
                    totalBars,
                    settings,
                    harmonyPolicy,
                    compProgramNumber),

                KeysTrack = GenerateKeysTrack(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    songContext.SectionTrack,
                    totalBars,
                    settings,
                    harmonyPolicy,
                    padsProgramNumber),

                DrumTrack = GenerateDrumTrack(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    totalBars,
                    settings,
                    drumProgramNumber)
            };
        }

        // AI: GeneratorResult: required PartTracks returned; consumers expect these program numbers and ordering.
        public sealed class GeneratorResult
        {
            public required PartTrack BassTrack { get; init; }
            public required PartTrack GuitarTrack { get; init; }
            public required PartTrack KeysTrack { get; init; }
            public required PartTrack DrumTrack { get; init; }
        }



        // AI: GenerateBassTrack: uses BassPatternLibrary for pattern selection and BassChordChangeDetector for approach notes.
        // AI: keep MIDI program number 33; patterns replace randomizer for more structured bass lines (Story 5.1 + 5.2).
        private static PartTrack GenerateBassTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            int totalBars,
            RandomizationSettings settings,
            HarmonyPolicy policy,
            int midiProgramNumber)
        {
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

                // Get current section type for pattern selection (default to Verse if none)
                MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse;
                // Note: SectionTrack not passed to GenerateBassTrack yet, will use default for now
                // TODO: Pass SectionTrack to enable section-aware pattern selection

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

                    bool shouldInsertApproach = isChangeImminent &&
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

                    notes.Add(new PartTrackEvent(
                        noteNumber: midiNote,
                        absoluteTimeTicks: (int)slot.StartTick,
                        noteDurationTicks: slot.DurationTicks,
                        noteOnVelocity: 95));
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        // AI: GenerateGuitarTrack: uses CompRhythmPatternLibrary + CompVoicingSelector for multi-note comp voicings.
        // AI: keep program number 27 for Electric Guitar; tracks previousVoicing for voice-leading continuity across bars.
        // AI: applies strum timing offsets to chord voicings for humanized feel (Story 4.3).
        private static PartTrack GenerateGuitarTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
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

                // Get section type for pattern selection
                MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse; // Default
                if (sectionTrack.GetActiveSection(bar, out var section) && section != null)
                {
                    sectionType = section.SectionType;
                }

                // Get section profile for voicing selection
                SectionProfile? sectionProfile = SectionProfile.GetForSectionType(sectionType);

                // Get comp rhythm pattern for this bar
                var pattern = CompRhythmPatternLibrary.GetPattern(
                    groovePreset.Name,
                    sectionType,
                    bar);

                // Filter onset slots using pattern's included indices
                var filteredOnsets = new List<decimal>();
                for (int i = 0; i < pattern.IncludedOnsetIndices.Count; i++)
                {
                    int index = pattern.IncludedOnsetIndices[i];
                    if (index >= 0 && index < compOnsets.Count)
                    {
                        filteredOnsets.Add(compOnsets[index]);
                    }
                }

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

                    // Calculate strum timing offsets for this chord
                    var strumOffsets = StrumTimingEngine.CalculateStrumOffsets(
                        voicing,
                        slot.Bar,
                        slot.OnsetBeat,
                        "comp",
                        settings.Seed);

                    // Add all notes from the voicing with strum timing offsets
                    for (int i = 0; i < voicing.Count; i++)
                    {
                        int midiNote = voicing[i];
                        int strumOffset = strumOffsets[i];

                        notes.Add(new PartTrackEvent(
                            noteNumber: midiNote,
                            absoluteTimeTicks: (int)slot.StartTick + strumOffset,
                            noteDurationTicks: slot.DurationTicks,
                            noteOnVelocity: 85));
                    }

                    // Update previous voicing for next onset
                    previousVoicing = voicing;
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        // AI: GenerateKeysTrack: uses VoiceLeadingSelector and SectionProfile for dynamic voicing per section.
        // AI: keep program number 4; tracks previous ChordRealization for voice-leading continuity.
        private static PartTrack GenerateKeysTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
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
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);
                var padsOnsets = grooveEvent.AnchorLayer.PadsOnsets;
                if (padsOnsets == null || padsOnsets.Count == 0)
                    continue;

                // Get section profile for current bar
                SectionProfile? sectionProfile = null;
                if (sectionTrack.GetActiveSection(bar, out var section) && section != null)
                {
                    sectionProfile = SectionProfile.GetForSectionType(section.SectionType);
                }

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

                    foreach (int midiNote in chordRealization.MidiNotes)
                    {
                        notes.Add(new PartTrackEvent(
                            noteNumber: midiNote,
                            absoluteTimeTicks: (int)slot.StartTick,
                            noteDurationTicks: slot.DurationTicks,
                            noteOnVelocity: 75));
                    }

                    // Update previous voicing for next onset
                    previousVoicing = chordRealization;
                }

                // Update previousHarmony to the first event active at the bar start (bar,1)
                previousHarmony = harmonyTrack.GetActiveHarmonyEvent(bar, 1m);
            }

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        /// <summary>
        /// Generates drum track: kick, snare, hi-hat at groove onsets with controlled randomness.
        /// Updated to support groove track changes.
        /// </summary>
        private static PartTrack GenerateDrumTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            int totalBars,
            RandomizationSettings settings,
            int midiProgramNumber)
        {
            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);

            // MIDI drum note numbers (General MIDI)
            const int kickNote = 36;
            const int snareNote = 38;
            const int closedHiHatNote = 42;

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);
                var layer = grooveEvent.AnchorLayer;

                // Kick pattern
                if (layer.KickOnsets != null && layer.KickOnsets.Count > 0)
                {
                    var onsetSlots = OnsetGrid.Build(bar, layer.KickOnsets, barTrack);
                    foreach (var slot in onsetSlots)
                    {
                        int velocity = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "kick", baseVelocity: 100);

                        notes.Add(new PartTrackEvent(
                            noteNumber: kickNote,
                            absoluteTimeTicks: (int)slot.StartTick,
                            noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                            noteOnVelocity: velocity));
                    }
                }

                // Snare pattern
                if (layer.SnareOnsets != null && layer.SnareOnsets.Count > 0)
                {
                    var onsetSlots = OnsetGrid.Build(bar, layer.SnareOnsets, barTrack);
                    foreach (var slot in onsetSlots)
                    {
                        int velocity = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "snare", baseVelocity: 90);

                        notes.Add(new PartTrackEvent(
                            noteNumber: snareNote,
                            absoluteTimeTicks: (int)slot.StartTick,
                            noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                            noteOnVelocity: velocity));
                    }
                }

                // Hi-hat pattern
                if (layer.HatOnsets != null && layer.HatOnsets.Count > 0)
                {
                    var onsetSlots = OnsetGrid.Build(bar, layer.HatOnsets, barTrack);
                    foreach (var slot in onsetSlots)
                    {
                        int velocity = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "hat", baseVelocity: 70);

                        notes.Add(new PartTrackEvent(
                            noteNumber: closedHiHatNote,
                            absoluteTimeTicks: (int)slot.StartTick,
                            noteDurationTicks: MusicConstants.TicksPerQuarterNote / 2,
                            noteOnVelocity: velocity));
                    }
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        #region Validation

        // AI: Validation methods throw ArgumentException when required tracks are missing; callers rely on exceptions for invalid song contexts.

        private static void ValidateSectionTrack(SectionTrack sectionTrack)
        {
            if (sectionTrack == null || sectionTrack.Sections.Count == 0)
                throw new ArgumentException("Section track must have events", nameof(sectionTrack));
        }

        private static void ValidateHarmonyTrack(HarmonyTrack harmonyTrack)
        {
            if (harmonyTrack == null || harmonyTrack.Events.Count == 0)
                throw new ArgumentException("Harmony track must have events", nameof(harmonyTrack));
        }

        private static void ValidateTimeSignatureTrack(Timingtrack timeSignatureTrack)
        {
            if (timeSignatureTrack == null || timeSignatureTrack.Events.Count == 0)
                throw new ArgumentException("Time signature track must have events", nameof(timeSignatureTrack));
        }

        private static void ValidateGrooveTrack(GrooveTrack grooveTrack)
        {
            if (grooveTrack == null || grooveTrack.Events.Count == 0)
                throw new ArgumentException("Groove track must have events", nameof(grooveTrack));
        }

        // ValidateSongContext: ensures caller provided a non-null SongContext; throws ArgumentNullException when null.
        private static void ValidateSongContext(SongContext songContext)
        {
            ArgumentNullException.ThrowIfNull(songContext);
        }

        #endregion

        /// <summary>
        /// Resolves MIDI program number from VoiceSet by matching GrooveRole.
        /// Returns defaultProgram if role not found or voice name cannot be mapped.
        /// </summary>
        private static int GetProgramNumberForRole(VoiceSet voices, String grooveRole, int defaultProgram)
        {
            // Find voice with matching groove role
            var voice = voices.Voices.FirstOrDefault(v => 
                string.Equals(v.GrooveRole, grooveRole, StringComparison.OrdinalIgnoreCase));

            if (voice == null)
                return defaultProgram;

            // Look up MIDI program number from voice name
            var midiVoice = MidiVoices.MidiVoiceList()
                .FirstOrDefault(mv => string.Equals(mv.Name, voice.VoiceName, StringComparison.OrdinalIgnoreCase));

            return midiVoice?.ProgramNumber ?? defaultProgram;
        }
    }
}