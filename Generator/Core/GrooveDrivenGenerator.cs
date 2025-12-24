namespace Music.Generator
{
    /// <summary>
    /// Generates tracks for multiple parts based on groove patterns and harmony events.
    /// Story 7: Now supports controlled randomness for pitch variation.
    /// Updated to support groove timeline with multiple groove changes throughout the song.
    /// </summary>
    public static class GrooveDrivenGenerator
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

            var timeSignature = songContext.Song.TimeSignatureTrack.Events.First();
            int ticksPerMeasure = (MusicConstants.TicksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

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
                    ticksPerMeasure,      //  This needs to go - see to do about it 
                    totalBars,            //  Ok - for test parameter
                    settings),            //  Randomization settings 

                GuitarTrack = GenerateGuitarTrack(songContext.HarmonyTrack, songContext.GrooveTrack, songContext.Song.TimeSignatureTrack, ticksPerMeasure, totalBars, settings),
                KeysTrack = GenerateKeysTrack(songContext.HarmonyTrack, songContext.GrooveTrack, songContext.Song.TimeSignatureTrack, ticksPerMeasure, totalBars, settings),
                DrumTrack = GenerateDrumTrack(songContext.HarmonyTrack, songContext.GrooveTrack, songContext.Song.TimeSignatureTrack, ticksPerMeasure, totalBars, settings)
            };
        }

        /// <summary>
        /// Result of generating all tracks for a groove preset.
        /// </summary>
        public sealed class GeneratorResult
        {
            public PartTrack BassTrack { get; init; }
            public PartTrack GuitarTrack { get; init; }
            public PartTrack KeysTrack { get; init; }
            public PartTrack DrumTrack { get; init; }
        }

        private static PartTrack GenerateBassTrack(
            HarmonyTrack harmonyTimeline,
            GrooveTrack grooveTrack,
            TimeSignatureTrack timeSignatureTrack,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<PartTrackNoteEvent>();
            var randomizer = new PitchRandomizer(settings);
            const int bassOctave = 2;

            var harmonyByBar = harmonyTimeline.Events
                .OrderBy(e => e.StartBar)
                .ThenBy(e => e.StartBeat)
                .ToList();

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var harmonyEvent = GetActiveHarmonyForBar(harmonyByBar, bar);
                if (harmonyEvent == null)
                    continue;

                // Get the active groove preset for this bar
                var groovePreset = grooveTrack.GetActiveGroovePreset(bar);

                var bassOnsets = groovePreset.AnchorLayer.BassOnsets;
                if (bassOnsets == null || bassOnsets.Count == 0)
                    continue;

                var ctx = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    bassOctave);

                int measureStartTick = (bar - 1) * ticksPerMeasure;

                for (int i = 0; i < bassOnsets.Count; i++)
                {
                    decimal onsetBeat = bassOnsets[i];
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);

                    int nextOnsetTick = (i + 1 < bassOnsets.Count)
                        ? measureStartTick + (int)((bassOnsets[i + 1] - 1m) * MusicConstants.TicksPerQuarterNote)
                        : measureStartTick + ticksPerMeasure;
                    int duration = nextOnsetTick - onsetTick;

                    int midiNote = randomizer.SelectBassPitch(ctx, bar, onsetBeat);

                    notes.Add(new PartTrackNoteEvent(
                        noteNumber: midiNote,
                        absolutePositionTicks: onsetTick,
                        noteDurationTicks: duration,
                        noteOnVelocity: 95,
                        isRest: false));
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = 33 }; // Electric Bass
        }

        private static PartTrack GenerateGuitarTrack(
            HarmonyTrack harmonyTimeline,
            GrooveTrack grooveTrack,
            TimeSignatureTrack timeSignatureTrack,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<PartTrackNoteEvent>();
            var randomizer = new PitchRandomizer(settings);
            int? previousPitchClass = null;
            const int guitarOctave = 4;

            var harmonyByBar = harmonyTimeline.Events
                .OrderBy(e => e.StartBar)
                .ThenBy(e => e.StartBeat)
                .ToList();

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var harmonyEvent = GetActiveHarmonyForBar(harmonyByBar, bar);
                if (harmonyEvent == null)
                    continue;

                // Get the active groove preset for this bar
                var groovePreset = grooveTrack.GetActiveGroovePreset(bar);

                var compOnsets = groovePreset.AnchorLayer.CompOnsets;
                if (compOnsets == null || compOnsets.Count == 0)
                    continue;

                var ctx = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    guitarOctave);



                // NOTE TO DO - ticks per measure can vary based on the time signature events
                // The loop that writes notes will need to get this value from the time signature active in each bar as it loops
                // this could be tricky. Example ticksPerMeasure will be different for 3/4 vs 4/4 time signatures
                // that can occur in the same track.
                //

                int measureStartTick = (bar - 1) * ticksPerMeasure;

                for (int i = 0; i < compOnsets.Count; i++)
                {
                    decimal onsetBeat = compOnsets[i];
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);

                    int nextOnsetTick = (i + 1 < compOnsets.Count)
                        ? measureStartTick + (int)((compOnsets[i + 1] - 1m) * MusicConstants.TicksPerQuarterNote)
                        : measureStartTick + ticksPerMeasure;
                    int duration = nextOnsetTick - onsetTick;

                    var (midiNote, pitchClass) = randomizer.SelectGuitarPitch(ctx, bar, onsetBeat, previousPitchClass);

                    notes.Add(new PartTrackNoteEvent(
                        noteNumber: midiNote,
                        absolutePositionTicks: onsetTick,
                        noteDurationTicks: duration,
                        noteOnVelocity: 85,
                        isRest: false));

                    previousPitchClass = pitchClass;
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = 27 }; // Electric Guitar
        }

        private static PartTrack GenerateKeysTrack(
            HarmonyTrack harmonyTimeline,
            GrooveTrack grooveTrack,
            TimeSignatureTrack timeSignatureTrack,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<PartTrackNoteEvent>();
            var randomizer = new PitchRandomizer(settings);
            const int keysOctave = 4;

            var harmonyByBar = harmonyTimeline.Events
                .OrderBy(e => e.StartBar)
                .ThenBy(e => e.StartBeat)
                .ToList();

            HarmonyEvent? previousHarmony = null;

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var harmonyEvent = GetActiveHarmonyForBar(harmonyByBar, bar);
                if (harmonyEvent == null)
                    continue;

                // Get the active groove preset for this bar
                var groovePreset = grooveTrack.GetActiveGroovePreset(bar);

                var padsOnsets = groovePreset.AnchorLayer.PadsOnsets;
                if (padsOnsets == null || padsOnsets.Count == 0)
                    continue;

                var ctx = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    keysOctave);

                int measureStartTick = (bar - 1) * ticksPerMeasure;

                for (int i = 0; i < padsOnsets.Count; i++)
                {
                    decimal onsetBeat = padsOnsets[i];
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);

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
                        notes.Add(new PartTrackNoteEvent(
                            noteNumber: midiNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: duration,
                            noteOnVelocity: 75,
                            isRest: false));
                    }
                }

                previousHarmony = harmonyEvent;
            }

            return new PartTrack(notes) { MidiProgramNumber = 4 }; // Electric Piano 1
        }

        /// <summary>
        /// Generates drum track: kick, snare, hi-hat at groove onsets with controlled randomness.
        /// Updated to support groove timeline changes.
        /// </summary>
        private static PartTrack GenerateDrumTrack(
            HarmonyTrack harmonyTimeline,
            GrooveTrack grooveTrack,
            TimeSignatureTrack timeSignatureTrack,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<PartTrackNoteEvent>();
            var randomizer = new PitchRandomizer(settings);

            // MIDI drum note numbers (General MIDI)
            const int kickNote = 36;
            const int snareNote = 38;
            const int closedHiHatNote = 42;

            for (int bar = 1; bar <= totalBars; bar++)
            {
                // Get the active groove preset for this bar
                var groovePreset = grooveTrack.GetActiveGroovePreset(bar);

                var layer = groovePreset.AnchorLayer;
                int measureStartTick = (bar - 1) * ticksPerMeasure;

                // Kick pattern
                if (layer.KickOnsets != null)
                {
                    foreach (var onsetBeat in layer.KickOnsets)
                    {
                        int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);
                        
                        // Apply slight velocity randomization for humanization
                        int velocity = randomizer.SelectDrumVelocity(bar, onsetBeat, "kick", baseVelocity: 100);
                        
                        notes.Add(new PartTrackNoteEvent(
                            noteNumber: kickNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                            noteOnVelocity: velocity,
                            isRest: false));
                    }
                }

                // Snare pattern
                if (layer.SnareOnsets != null)
                {
                    foreach (var onsetBeat in layer.SnareOnsets)
                    {
                        int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);
                        
                        int velocity = randomizer.SelectDrumVelocity(bar, onsetBeat, "snare", baseVelocity: 90);
                        
                        notes.Add(new PartTrackNoteEvent(
                            noteNumber: snareNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                            noteOnVelocity: velocity,
                            isRest: false));
                    }
                }

                // Hi-hat pattern
                if (layer.HatOnsets != null)
                {
                    foreach (var onsetBeat in layer.HatOnsets)
                    {
                        int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);
                        
                        int velocity = randomizer.SelectDrumVelocity(bar, onsetBeat, "hat", baseVelocity: 70);
                        
                        notes.Add(new PartTrackNoteEvent(
                            noteNumber: closedHiHatNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: MusicConstants.TicksPerQuarterNote / 2, // shorter duration for hi-hat
                            noteOnVelocity: velocity,
                            isRest: false));
                    }
                }
            }

            return new PartTrack(notes) { MidiProgramNumber = 255 }; // Drum Set
        }


        // TO DO MOVE THIS TO HARMONYTRACK CLASS! IT SHOULD WORK LIKE GROOVE DOES! then wont need in every bar

        /// <summary>
        /// Gets the active harmony event for a given bar.
        /// Returns the most recent harmony event that starts on or before this bar.
        /// </summary>
        private static HarmonyEvent? GetActiveHarmonyForBar(List<HarmonyEvent> harmonyEvents, int bar)
        {
            HarmonyEvent? active = null;
            foreach (var evt in harmonyEvents)
            {
                if (evt.StartBar <= bar)
                    active = evt;
                else
                    break;
            }
            return active;
        }
        #region Validation

        private static void ValidateHarmonyTrack(HarmonyTrack harmonyTrack)
        {
            if (harmonyTrack == null || harmonyTrack.Events.Count == 0)
                throw new ArgumentException("Harmony timeline must have events", nameof(harmonyTrack));
        }

        private static void ValidateTimeSignatureTrack(TimeSignatureTrack timeSignatureTrack)
        {
            if (timeSignatureTrack == null || timeSignatureTrack.Events.Count == 0)
                throw new ArgumentException("Time signature timeline must have events", nameof(timeSignatureTrack));
        }

        private static void ValidateGrooveTrack(GrooveTrack grooveTrack)
        {
            if (grooveTrack == null || grooveTrack.Events.Count == 0)
                throw new ArgumentException("Groove track must have events", nameof(grooveTrack));
        }

        #endregion

    }
}