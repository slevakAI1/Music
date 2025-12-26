using Music.MyMidi;

namespace Music.Generator
{
    /// <summary>
    /// Generates tracks for multiple parts based on groove patterns and harmony events.
    /// Story 7: Now supports controlled randomness for pitch variation.
    /// Updated to support groove track with multiple groove changes throughout the song.
    /// </summary>
    public static class Generator
    {
        /// <summary>
        /// Generates all tracks based on SongContext.
        /// </summary>
        /// <param name="songContext">The song context containing harmony, time signature, and groove tracks.</param>
        /// <returns>Generated tracks for each role.</returns>
        public static GeneratorResult Generate(SongContext songContext)
        {
            ValidateHarmonyTrack(songContext.HarmonyTrack);
            ValidateTimeSignatureTrack(songContext.Song.TimeSignatureTrack);
            ValidateGrooveTrack(songContext.GrooveTrack);

            // Get total bars from harmony events
            int totalBars = songContext.HarmonyTrack.Events.Max(e => e.StartBar);

            // Use default randomization settings
            var settings = RandomizationSettings.Default;

            return new GeneratorResult
            {
                BassTrack = GenerateBassTrack(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.Song.TimeSignatureTrack,
                    totalBars,
                    settings),            //  Randomization settings 

                GuitarTrack = GenerateGuitarTrack(songContext.HarmonyTrack, songContext.GrooveTrack, songContext.Song.TimeSignatureTrack, totalBars, settings),
                KeysTrack = GenerateKeysTrack(songContext.HarmonyTrack, songContext.GrooveTrack, songContext.Song.TimeSignatureTrack, totalBars, settings),
                DrumTrack = GenerateDrumTrack(songContext.HarmonyTrack, songContext.GrooveTrack, songContext.Song.TimeSignatureTrack, totalBars, settings)
            };
        }

        /// <summary>
        /// Result of generating all tracks for a groove preset.
        /// </summary>
        public sealed class GeneratorResult
        {
            public required PartTrack BassTrack { get; init; }
            public required PartTrack GuitarTrack { get; init; }
            public required PartTrack KeysTrack { get; init; }
            public required PartTrack DrumTrack { get; init; }
        }


        // TO DO - HIGH - STEP THRU THIS TO SEE HOW IT WORKS EXACTLY

        private static PartTrack GenerateBassTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            TimeSignatureTrack timeSignatureTrack,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);
            const int bassOctave = 2;

            int measureStartTick = 0;

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

                // Get ticks per measure for the active time signature at this bar
                var timeSignature = timeSignatureTrack.GetActiveTimeSignatureEvent(bar);
                if (timeSignature == null)
                    continue;
                int ticksPerMeasure = (MusicConstants.TicksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

                for (int i = 0; i < bassOnsets.Count; i++)
                {
                    decimal onsetBeat = bassOnsets[i];
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);

                    // Only add note if onset is within this measure
                    if (onsetTick < measureStartTick + ticksPerMeasure)
                    {
                        int nextOnsetTick = (i + 1 < bassOnsets.Count)
                            ? measureStartTick + (int)((bassOnsets[i + 1] - 1m) * MusicConstants.TicksPerQuarterNote)
                            : measureStartTick + ticksPerMeasure;
                        int duration = nextOnsetTick - onsetTick;

                        int midiNote = randomizer.SelectBassPitch(ctx, bar, onsetBeat);


                        notes.Add(new PartTrackEvent(
                            noteNumber: midiNote,
                            absoluteTimeTicks: onsetTick,
                            noteDurationTicks: duration,
                            noteOnVelocity: 95));
                    }
                }

                measureStartTick += ticksPerMeasure;
            }

            return new PartTrack(notes) { MidiProgramNumber = 33 }; // Electric Bass
        }

        private static PartTrack GenerateGuitarTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            TimeSignatureTrack timeSignatureTrack,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);
            int? previousPitchClass = null;
            const int guitarOctave = 4;

            int measureStartTick = 0;

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


                // Get ticks per measure for the active time signature at this bar
                var timeSignature = timeSignatureTrack.GetActiveTimeSignatureEvent(bar);
                if (timeSignature == null)
                    continue;
                int ticksPerMeasure = (MusicConstants.TicksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

                for (int i = 0; i < compOnsets.Count; i++)
                {
                    decimal onsetBeat = compOnsets[i];
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);

                    // Only add note if onset is within this measure
                    if (onsetTick < measureStartTick + ticksPerMeasure)
                    {
                        int nextOnsetTick = (i + 1 < compOnsets.Count)
                            ? measureStartTick + (int)((compOnsets[i + 1] - 1m) * MusicConstants.TicksPerQuarterNote)
                            : measureStartTick + ticksPerMeasure;
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

                measureStartTick += ticksPerMeasure;
            }

            return new PartTrack(notes) { MidiProgramNumber = 27 }; // Electric Guitar
        }

        private static PartTrack GenerateKeysTrack(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            TimeSignatureTrack timeSignatureTrack,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);
            const int keysOctave = 3;

            HarmonyEvent? previousHarmony = null;
            int measureStartTick = 0;

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

                // Get ticks per measure for the active time signature at this bar
                var timeSignature = timeSignatureTrack.GetActiveTimeSignatureEvent(bar);
                if (timeSignature == null)
                    continue;
                int ticksPerMeasure = (MusicConstants.TicksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

                for (int i = 0; i < padsOnsets.Count; i++)
                {
                    decimal onsetBeat = padsOnsets[i];
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);

                    // Only add note if onset is within this measure
                    if (onsetTick < measureStartTick + ticksPerMeasure)
                    {
                        int nextOnsetTick = (i + 1 < padsOnsets.Count)
                            ? measureStartTick + (int)((padsOnsets[i + 1] - 1m) * MusicConstants.TicksPerQuarterNote)
                            : measureStartTick + ticksPerMeasure;
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
                measureStartTick += ticksPerMeasure;
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
            TimeSignatureTrack timeSignatureTrack,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);

            // MIDI drum note numbers (General MIDI)
            const int kickNote = 36;
            const int snareNote = 38;
            const int closedHiHatNote = 42;

            int measureStartTick = 0;

            for (int bar = 1; bar <= totalBars; bar++)
            {
                // Get the active groove event for this bar
                var grooveEvent = grooveTrack.GetActiveGroovePreset(bar);

                var layer = grooveEvent.AnchorLayer;

                // Get ticks per measure for the active time signature at this bar
                var timeSignature = timeSignatureTrack.GetActiveTimeSignatureEvent(bar);
                if (timeSignature == null)
                    continue;
                int ticksPerMeasure = (MusicConstants.TicksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

                // Kick pattern
                if (layer.KickOnsets != null)
                {
                    foreach (var onsetBeat in layer.KickOnsets)
                    {
                        int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);

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
                        int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);

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
                        int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);

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

                measureStartTick += ticksPerMeasure;
            }

            return new PartTrack(notes) { MidiProgramNumber = 255 }; // Drum Set
        }

        #region Validation

        private static void ValidateHarmonyTrack(HarmonyTrack harmonyTrack)
        {
            if (harmonyTrack == null || harmonyTrack.Events.Count == 0)
                throw new ArgumentException("Harmony track must have events", nameof(harmonyTrack));
        }

        private static void ValidateTimeSignatureTrack(TimeSignatureTrack timeSignatureTrack)
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