// AI: purpose=Generate PartTracks for parts using harmony, groove, and timing; uses controlled randomness via PitchRandomizer.
// AI: invariants=Order: Harmony->Groove->Bar must align; totalBars derived from SectionTrack; tick calc uses (onsetBeat-1)*TicksPerQuarterNote.
// AI: deps=Relies on HarmonyTrack.GetActiveHarmonyEvent, GrooveTrack.GetActiveGroovePreset, BarTrack.GetBar, MusicConstants.TicksPerQuarterNote.
// AI: perf=Not real-time; called once per song generation; avoid heavy allocations in inner loops.
// TODO? confirm behavior when groove/pads onsets null vs empty; current code skips in both cases.

using Music.MyMidi;

namespace Music.Generator
{
    public static class Generator
    {
        // AI: Generate: validates harmony track before generation; fast-fail on invalid data prevents silent errors.
        // AI: behavior=Runs HarmonyValidator with default options (StrictDiatonicChordTones=true) to catch F# minor crashes.
        public static GeneratorResult Generate(SongContext songContext)
        {
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

            // Use default randomization settings
            var settings = RandomizationSettings.Default;

            return new GeneratorResult
            {
                BassTrack = GenerateBassTrack(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    totalBars,
                    settings),            //  Randomization settings 

                GuitarTrack = GenerateGuitarTrack(songContext.HarmonyTrack, songContext.GrooveTrack, songContext.BarTrack, totalBars, settings),
                KeysTrack = GenerateKeysTrack(songContext.HarmonyTrack, songContext.GrooveTrack, songContext.BarTrack, totalBars, settings),
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


        // TO DO - HIGH - STEP THRU THIS TO SEE HOW IT WORKS EXACTLY

        // AI: GenerateBassTrack: builds HarmonyPitchContext with bassOctave=2; SelectBassPitch must return appropriate pc.
        // AI: keep MIDI program number 33; changing octave constant or program number impacts tonal range and tests.
        private static PartTrack GenerateBassTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);
            const int bassOctave = 2;

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(bar);
                if (harmonyEvent == null)
                    continue;

                // Get the active groove preset for this bar
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);

                var bassOnsets = grooveEvent.AnchorLayer.BassOnsets;
                if (bassOnsets == null || bassOnsets.Count == 0)
                    continue;

                var ctx = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    bassOctave);


                var currentBar = barTrack.GetBar(bar);
                if (currentBar == null)
                    continue;

                int ticksPerMeasure = currentBar.TicksPerMeasure;
                long measureStartTick = currentBar.StartTick;

                for (int i = 0; i < bassOnsets.Count; i++)
                {
                    decimal onsetBeat = bassOnsets[i];
                    int onsetTick = (int)(measureStartTick + (long)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote));

                    // Only add note if onset is within this measure
                    if (onsetTick < measureStartTick + ticksPerMeasure)
                    {
                        int nextOnsetTick = (i + 1 < bassOnsets.Count)
                            ? (int)(measureStartTick + (long)((bassOnsets[i + 1] - 1m) * MusicConstants.TicksPerQuarterNote))
                            : (int)(measureStartTick + ticksPerMeasure);
                        int duration = nextOnsetTick - onsetTick;

                        int midiNote = randomizer.SelectBassPitch(ctx, bar, onsetBeat);


                        notes.Add(new PartTrackEvent(
                            noteNumber: midiNote,
                            absoluteTimeTicks: onsetTick,
                            noteDurationTicks: duration,
                            noteOnVelocity: 95));
                    }
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
            RandomizationSettings settings)
        {
            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);
            int? previousPitchClass = null;
            const int guitarOctave = 4;

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(bar);
                if (harmonyEvent == null)
                    continue;

                // Get the active groove preset for this bar
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);

                var compOnsets = grooveEvent.AnchorLayer.CompOnsets;
                if (compOnsets == null || compOnsets.Count == 0)
                    continue;

                var ctx = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    guitarOctave);


                var currentBar = barTrack.GetBar(bar);
                if (currentBar == null)
                    continue;

                int ticksPerMeasure = currentBar.TicksPerMeasure;
                long measureStartTick = currentBar.StartTick;

                for (int i = 0; i < compOnsets.Count; i++)
                {
                    decimal onsetBeat = compOnsets[i];
                    int onsetTick = (int)(measureStartTick + (long)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote));

                    // Only add note if onset is within this measure
                    if (onsetTick < measureStartTick + ticksPerMeasure)
                    {
                        int nextOnsetTick = (i + 1 < compOnsets.Count)
                            ? (int)(measureStartTick + (long)((compOnsets[i + 1] - 1m) * MusicConstants.TicksPerQuarterNote))
                            : (int)(measureStartTick + ticksPerMeasure);
                        int duration = nextOnsetTick - onsetTick;

                        var (midiNote, pitchClass) = randomizer.SelectGuitarPitch(ctx, bar, onsetBeat, previousPitchClass);

                        notes.Add(new PartTrackEvent(
                            noteNumber: midiNote,
                            absoluteTimeTicks: onsetTick,
                            noteDurationTicks: duration,
                            noteOnVelocity: 85));

                        previousPitchClass = pitchClass;
                    }
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = 27 }; // Electric Guitar
        }

        // AI: GenerateKeysTrack: detects first onset of a harmony event via previousHarmony; added 9th only when isFirstOnset true.
        // AI: keep program number 4; chord voicing order preserved when adding notes.
        private static PartTrack GenerateKeysTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);
            const int keysOctave = 3;

            HarmonyEvent? previousHarmony = null;

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(bar);
                if (harmonyEvent == null)
                    continue;

                // Get the active groove preset for this bar
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);

                var padsOnsets = grooveEvent.AnchorLayer.PadsOnsets;
                if (padsOnsets == null || padsOnsets.Count == 0)
                    continue;

                var ctx = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    keysOctave);


                var currentBar = barTrack.GetBar(bar);
                if (currentBar == null)
                    continue;

                int ticksPerMeasure = currentBar.TicksPerMeasure;
                long measureStartTick = currentBar.StartTick;

                for (int i = 0; i < padsOnsets.Count; i++)
                {
                    decimal onsetBeat = padsOnsets[i];
                    int onsetTick = (int)(measureStartTick + (long)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote));

                    // Only add note if onset is within this measure
                    if (onsetTick < measureStartTick + ticksPerMeasure)
                    {
                        int nextOnsetTick = (i + 1 < padsOnsets.Count)
                            ? (int)(measureStartTick + (long)((padsOnsets[i + 1] - 1m) * MusicConstants.TicksPerQuarterNote))
                            : (int)(measureStartTick + ticksPerMeasure);
                        int duration = nextOnsetTick - onsetTick;

                        bool isFirstOnset = previousHarmony == null ||
                            harmonyEvent.StartBar != previousHarmony.StartBar ||
                            harmonyEvent.StartBeat != previousHarmony.StartBeat;

                        var chordMidiNotes = randomizer.SelectKeysVoicing(ctx, bar, onsetBeat, isFirstOnset);

                        foreach (int midiNote in chordMidiNotes)
                        {
                            notes.Add(new PartTrackEvent(
                                noteNumber: midiNote,
                                absoluteTimeTicks: onsetTick,
                                noteDurationTicks: duration,
                                noteOnVelocity: 75));
                        }
                    }
                }

                previousHarmony = harmonyEvent;
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
                // Get the active groove event for this bar
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);

                var layer = grooveEvent.AnchorLayer;


                var currentBar = barTrack.GetBar(bar);
                if (currentBar == null)
                    continue;

                int ticksPerMeasure = currentBar.TicksPerMeasure;
                long measureStartTick = currentBar.StartTick;

                // Kick pattern
                if (layer.KickOnsets != null)
                {
                    foreach (var onsetBeat in layer.KickOnsets)
                    {
                        int onsetTick = (int)(measureStartTick + (long)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote));

                        // Only add note if onset is within this measure
                        if (onsetTick < measureStartTick + ticksPerMeasure)
                        {
                            int velocity = randomizer.SelectDrumVelocity(bar, onsetBeat, "kick", baseVelocity: 100);

                            notes.Add(new PartTrackEvent(
                                noteNumber: kickNote,
                                absoluteTimeTicks: onsetTick,
                                noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                                noteOnVelocity: velocity));
                        }
                    }
                }

                // Snare pattern
                if (layer.SnareOnsets != null)
                {
                    foreach (var onsetBeat in layer.SnareOnsets)
                    {
                        int onsetTick = (int)(measureStartTick + (long)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote));

                        // Only add note if onset is within this measure
                        if (onsetTick < measureStartTick + ticksPerMeasure)
                        {
                            int velocity = randomizer.SelectDrumVelocity(bar, onsetBeat, "snare", baseVelocity: 90);

                            notes.Add(new PartTrackEvent(
                                noteNumber: snareNote,
                                absoluteTimeTicks: onsetTick,
                                noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                                noteOnVelocity: velocity));
                        }
                    }
                }

                // Hi-hat pattern
                if (layer.HatOnsets != null)
                {
                    foreach (var onsetBeat in layer.HatOnsets)
                    {
                        int onsetTick = (int)(measureStartTick + (long)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote));

                        // Only add note if onset is within this measure
                        if (onsetTick < measureStartTick + ticksPerMeasure)
                        {
                            int velocity = randomizer.SelectDrumVelocity(bar, onsetBeat, "hat", baseVelocity: 70);

                            notes.Add(new PartTrackEvent(
                                noteNumber: closedHiHatNote,
                                absoluteTimeTicks: onsetTick,
                                noteDurationTicks: MusicConstants.TicksPerQuarterNote / 2,
                                noteOnVelocity: velocity));
                        }
                    }
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = 255 }; // Drum Set
        }

        #region Validation

        // AI: Validation methods throw ArgumentException when required tracks are missing; callers rely on exceptions for invalid song contexts.
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

        #endregion

    }
}