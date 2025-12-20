using Music.Designer;
using Music.Generator;

namespace Music.Writer.Generator
{
    /// <summary>
    /// Generates tracks for multiple parts based on groove patterns and harmony events.
    /// Story 7: Now supports controlled randomness for pitch variation.
    /// Updated to support groove timeline with multiple groove changes throughout the song.
    /// </summary>
    public static class GrooveDrivenGenerator
    {
        /// <summary>
        /// Generates all tracks based on harmony timeline and groove track timeline.
        /// </summary>
        /// <param name="harmonyTimeline">The harmony events defining chords per bar.</param>
        /// <param name="timeSignatureTimeline">Time signature info for tick calculations.</param>
        /// <param name="grooveTrack">The groove track timeline defining which groove preset is active at each bar.</param>
        /// <returns>Generated tracks for each role.</returns>
        public static GeneratorResult Generate(
            HarmonyTrack harmonyTimeline,
            TimeSignatureTrack timeSignatureTimeline,
            GrooveTrack grooveTrack)
        {
            if (harmonyTimeline == null || harmonyTimeline.Events.Count == 0)
                throw new ArgumentException("Harmony timeline must have events", nameof(harmonyTimeline));
            if (timeSignatureTimeline == null || timeSignatureTimeline.Events.Count == 0)
                throw new ArgumentException("Time signature timeline must have events", nameof(timeSignatureTimeline));
            if (grooveTrack == null || grooveTrack.Events.Count == 0)
                throw new ArgumentException("Groove track must have events", nameof(grooveTrack));

            var timeSignature = timeSignatureTimeline.Events.First();
            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

            // Get total bars from harmony events
            int totalBars = harmonyTimeline.Events.Max(e => e.StartBar);

            // Use default randomization settings
            var settings = RandomizationSettings.Default;

            // Ensure groove track is indexed for fast lookups
            grooveTrack.EnsureIndexed();

            return new GeneratorResult
            {
                BassTrack = GenerateBassTrack(harmonyTimeline, grooveTrack, ticksPerQuarterNote, ticksPerMeasure, totalBars, settings),
                GuitarTrack = GenerateGuitarTrack(harmonyTimeline, grooveTrack, ticksPerQuarterNote, ticksPerMeasure, totalBars, settings),
                KeysTrack = GenerateKeysTrack(harmonyTimeline, grooveTrack, ticksPerQuarterNote, ticksPerMeasure, totalBars, settings),
                DrumTrack = GenerateDrumTrack(harmonyTimeline, grooveTrack, ticksPerQuarterNote, ticksPerMeasure, totalBars, settings)
            };
        }

        /// <summary>
        /// Result of generating all tracks for a groove preset.
        /// </summary>
        public sealed class GeneratorResult
        {
            public SongTrack? BassTrack { get; init; }
            public SongTrack? GuitarTrack { get; init; }
            public SongTrack? KeysTrack { get; init; }
            public SongTrack? DrumTrack { get; init; }
        }

        /// <summary>
        /// Helper method to get the active groove preset for a given bar.
        /// </summary>
        private static GroovePreset? GetActiveGroovePreset(GrooveTrack grooveTrack, int bar)
        {
            if (grooveTrack.TryGetAtBar(bar, out var grooveEvent) && grooveEvent != null)
            {
                var preset = GroovePresets.GetByName(grooveEvent.GroovePresetName);
                if (preset != null)
                    return preset;
            }

            // Fallback to PopRockBasic if no groove found or preset name is invalid
            return GroovePresets.GetPopRockBasic();
        }

        private static SongTrack? GenerateBassTrack(
            HarmonyTrack harmonyTimeline,
            GrooveTrack grooveTrack,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<SongTrackNoteEvent>();
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
                var groovePreset = GetActiveGroovePreset(grooveTrack, bar);
                if (groovePreset == null)
                    continue;

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
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * ticksPerQuarterNote);

                    int nextOnsetTick = (i + 1 < bassOnsets.Count)
                        ? measureStartTick + (int)((bassOnsets[i + 1] - 1m) * ticksPerQuarterNote)
                        : measureStartTick + ticksPerMeasure;
                    int duration = nextOnsetTick - onsetTick;

                    int midiNote = randomizer.SelectBassPitch(ctx, bar, onsetBeat);

                    notes.Add(new SongTrackNoteEvent(
                        noteNumber: midiNote,
                        absolutePositionTicks: onsetTick,
                        noteDurationTicks: duration,
                        noteOnVelocity: 95,
                        isRest: false));
                }
            }

            if (notes.Count == 0)
                return null;

            return new SongTrack(notes) { MidiProgramNumber = 33 }; // Electric Bass
        }

        private static SongTrack? GenerateGuitarTrack(
            HarmonyTrack harmonyTimeline,
            GrooveTrack grooveTrack,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<SongTrackNoteEvent>();
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
                var groovePreset = GetActiveGroovePreset(grooveTrack, bar);
                if (groovePreset == null)
                    continue;

                var compOnsets = groovePreset.AnchorLayer.CompOnsets;
                if (compOnsets == null || compOnsets.Count == 0)
                    continue;

                var ctx = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    guitarOctave);

                int measureStartTick = (bar - 1) * ticksPerMeasure;

                for (int i = 0; i < compOnsets.Count; i++)
                {
                    decimal onsetBeat = compOnsets[i];
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * ticksPerQuarterNote);

                    int nextOnsetTick = (i + 1 < compOnsets.Count)
                        ? measureStartTick + (int)((compOnsets[i + 1] - 1m) * ticksPerQuarterNote)
                        : measureStartTick + ticksPerMeasure;
                    int duration = nextOnsetTick - onsetTick;

                    var (midiNote, pitchClass) = randomizer.SelectGuitarPitch(ctx, bar, onsetBeat, previousPitchClass);

                    notes.Add(new SongTrackNoteEvent(
                        noteNumber: midiNote,
                        absolutePositionTicks: onsetTick,
                        noteDurationTicks: duration,
                        noteOnVelocity: 85,
                        isRest: false));

                    previousPitchClass = pitchClass;
                }
            }

            if (notes.Count == 0)
                return null;

            return new SongTrack(notes) { MidiProgramNumber = 27 }; // Electric Guitar
        }

        private static SongTrack? GenerateKeysTrack(
            HarmonyTrack harmonyTimeline,
            GrooveTrack grooveTrack,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<SongTrackNoteEvent>();
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
                var groovePreset = GetActiveGroovePreset(grooveTrack, bar);
                if (groovePreset == null)
                    continue;

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
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * ticksPerQuarterNote);

                    int nextOnsetTick = (i + 1 < padsOnsets.Count)
                        ? measureStartTick + (int)((padsOnsets[i + 1] - 1m) * ticksPerQuarterNote)
                        : measureStartTick + ticksPerMeasure;
                    int duration = nextOnsetTick - onsetTick;

                    bool isFirstOnset = previousHarmony == null || 
                        harmonyEvent.StartBar != previousHarmony.StartBar ||
                        harmonyEvent.StartBeat != previousHarmony.StartBeat;

                    var chordMidiNotes = randomizer.SelectKeysVoicing(ctx, bar, onsetBeat, isFirstOnset);

                    foreach (int midiNote in chordMidiNotes)
                    {
                        notes.Add(new SongTrackNoteEvent(
                            noteNumber: midiNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: duration,
                            noteOnVelocity: 75,
                            isRest: false));
                    }
                }

                previousHarmony = harmonyEvent;
            }

            if (notes.Count == 0)
                return null;

            return new SongTrack(notes) { MidiProgramNumber = 4 }; // Electric Piano 1
        }

        /// <summary>
        /// Generates drum track: kick, snare, hi-hat at groove onsets with controlled randomness.
        /// Updated to support groove timeline changes.
        /// </summary>
        private static SongTrack? GenerateDrumTrack(
            HarmonyTrack harmonyTimeline,
            GrooveTrack grooveTrack,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<SongTrackNoteEvent>();
            var randomizer = new PitchRandomizer(settings);

            // MIDI drum note numbers (General MIDI)
            const int kickNote = 36;
            const int snareNote = 38;
            const int closedHiHatNote = 42;

            for (int bar = 1; bar <= totalBars; bar++)
            {
                // Get the active groove preset for this bar
                var groovePreset = GetActiveGroovePreset(grooveTrack, bar);
                if (groovePreset == null)
                    continue;

                var layer = groovePreset.AnchorLayer;
                int measureStartTick = (bar - 1) * ticksPerMeasure;

                // Kick pattern
                if (layer.KickOnsets != null)
                {
                    foreach (var onsetBeat in layer.KickOnsets)
                    {
                        int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * ticksPerQuarterNote);
                        
                        // Apply slight velocity randomization for humanization
                        int velocity = randomizer.SelectDrumVelocity(bar, onsetBeat, "kick", baseVelocity: 100);
                        
                        notes.Add(new SongTrackNoteEvent(
                            noteNumber: kickNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: ticksPerQuarterNote,
                            noteOnVelocity: velocity,
                            isRest: false));
                    }
                }

                // Snare pattern
                if (layer.SnareOnsets != null)
                {
                    foreach (var onsetBeat in layer.SnareOnsets)
                    {
                        int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * ticksPerQuarterNote);
                        
                        int velocity = randomizer.SelectDrumVelocity(bar, onsetBeat, "snare", baseVelocity: 90);
                        
                        notes.Add(new SongTrackNoteEvent(
                            noteNumber: snareNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: ticksPerQuarterNote,
                            noteOnVelocity: velocity,
                            isRest: false));
                    }
                }

                // Hi-hat pattern
                if (layer.HatOnsets != null)
                {
                    foreach (var onsetBeat in layer.HatOnsets)
                    {
                        int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * ticksPerQuarterNote);
                        
                        int velocity = randomizer.SelectDrumVelocity(bar, onsetBeat, "hat", baseVelocity: 70);
                        
                        notes.Add(new SongTrackNoteEvent(
                            noteNumber: closedHiHatNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: ticksPerQuarterNote / 2, // shorter duration for hi-hat
                            noteOnVelocity: velocity,
                            isRest: false));
                    }
                }
            }

            if (notes.Count == 0)
                return null;

            return new SongTrack(notes) { MidiProgramNumber = 255 }; // Drum Set
        }

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
    }
}