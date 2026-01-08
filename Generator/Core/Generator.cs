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

            return new GeneratorResult
            {
                BassTrack = GenerateBassTrack(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    totalBars,
                    settings,
                    harmonyPolicy),            //  Randomization settings 

                GuitarTrack = GenerateGuitarTrack(songContext.HarmonyTrack, songContext.GrooveTrack, songContext.BarTrack, totalBars, settings, harmonyPolicy),
                KeysTrack = GenerateKeysTrack(songContext.HarmonyTrack, songContext.GrooveTrack, songContext.BarTrack, totalBars, settings, harmonyPolicy),
                DrumTrack = GenerateDrumTrack(songContext.HarmonyTrack, songContext.GrooveTrack, songContext.BarTrack, totalBars, settings)
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



        // AI: GenerateBassTrack: builds HarmonyPitchContext with bassOctave=2; SelectBassPitch must return appropriate pc.
        // AI: keep MIDI program number 33; changing octave constant or program number impacts tonal range and tests.
        private static PartTrack GenerateBassTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            int totalBars,
            RandomizationSettings settings,
            HarmonyPolicy policy)
        {
            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);
            const int bassOctave = 2;

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);
                var bassOnsets = grooveEvent.AnchorLayer.BassOnsets;
                if (bassOnsets == null || bassOnsets.Count == 0)
                    continue;

                // Build onset grid for this bar
                var onsetSlots = OnsetGrid.Build(bar, bassOnsets, barTrack);

                foreach (var slot in onsetSlots)
                {
                    // Find active harmony at this bar+beat
                    var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(slot.Bar, slot.OnsetBeat);
                    if (harmonyEvent == null)
                        continue;

                    var ctx = HarmonyPitchContextBuilder.Build(
                        harmonyEvent.Key,
                        harmonyEvent.Degree,
                        harmonyEvent.Quality,
                        harmonyEvent.Bass,
                        bassOctave,
                        policy);

                    int midiNote = randomizer.SelectBassPitch(ctx, slot.Bar, slot.OnsetBeat);

                    notes.Add(new PartTrackEvent(
                        noteNumber: midiNote,
                        absoluteTimeTicks: (int)slot.StartTick,
                        noteDurationTicks: slot.DurationTicks,
                        noteOnVelocity: 95));
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = 33 }; // Electric Bass
        }

        // AI: GenerateGuitarTrack: tracks previousPitchClass across onsets to enable passing tones; changing this state breaks guitar voicing.
        // AI: keep program number 27 for Electric Guitar to match intended timbre.
        private static PartTrack GenerateGuitarTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            int totalBars,
            RandomizationSettings settings,
            HarmonyPolicy policy)
        {
            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);
            int? previousPitchClass = null;
            const int guitarOctave = 4;

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);
                var compOnsets = grooveEvent.AnchorLayer.CompOnsets;
                if (compOnsets == null || compOnsets.Count == 0)
                    continue;

                // Build onset grid for this bar
                var onsetSlots = OnsetGrid.Build(bar, compOnsets, barTrack);

                foreach (var slot in onsetSlots)
                {
                    // Find active harmony at this bar+beat
                    var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(slot.Bar, slot.OnsetBeat);
                    if (harmonyEvent == null)
                        continue;

                    var ctx = HarmonyPitchContextBuilder.Build(
                        harmonyEvent.Key,
                        harmonyEvent.Degree,
                        harmonyEvent.Quality,
                        harmonyEvent.Bass,
                        guitarOctave,
                        policy);

                    var (midiNote, pitchClass) = randomizer.SelectGuitarPitch(ctx, slot.Bar, slot.OnsetBeat, previousPitchClass);

                    notes.Add(new PartTrackEvent(
                        noteNumber: midiNote,
                        absoluteTimeTicks: (int)slot.StartTick,
                        noteDurationTicks: slot.DurationTicks,
                        noteOnVelocity: 85));

                    previousPitchClass = pitchClass;
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = 27 }; // Electric Guitar
        }

        // AI: GenerateKeysTrack: uses VoiceLeadingSelector to maintain smooth voice leading across onsets.
        // AI: keep program number 4; tracks previous ChordRealization for voice-leading continuity.
        private static PartTrack GenerateKeysTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            int totalBars,
            RandomizationSettings settings,
            HarmonyPolicy policy)
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
                        var baseVoicing = randomizer.SelectKeysVoicing(ctx, slot.Bar, slot.OnsetBeat, isFirstOnset);
                        
                        // If we have previous voicing, apply voice leading to the base voicing
                        if (previousVoicing != null)
                        {
                            chordRealization = VoiceLeadingSelector.Select(previousVoicing, ctx);
                            
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
                        chordRealization = VoiceLeadingSelector.Select(previousVoicing, ctx);
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

            return new PartTrack(notes) { MidiProgramNumber = 4 }; // Electric Piano 1
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
            RandomizationSettings settings)
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

            return new PartTrack(notes) { MidiProgramNumber = 255 }; // Drum Set
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

    }
}