using Music.Designer;
using Music.Generator;

namespace Music.Writer.Generator
{
    /// <summary>
    /// Generates tracks for multiple parts based on groove patterns and harmony events.
    /// Story 7: Now supports controlled randomness for pitch variation.
    /// </summary>
    public static class GrooveDrivenGenerator
    {
        /// <summary>
        /// Generates all tracks based on harmony timeline and groove preset.
        /// </summary>
        /// <param name="harmonyTimeline">The harmony events defining chords per bar.</param>
        /// <param name="timeSignatureTimeline">Time signature info for tick calculations.</param>
        /// <param name="groovePreset">The groove preset defining onset patterns.</param>
        /// <returns>Generated tracks for each role.</returns>
        /// 

        // WHY ARE THERE 2 GENERATES - DUPLICATES!


        public static GeneratorResult Generate(
            HarmonyTrack harmonyTimeline,
            TimeSignatureTrack timeSignatureTimeline,
            GroovePreset groovePreset)
        {
            if (harmonyTimeline == null || harmonyTimeline.Events.Count == 0)
                throw new ArgumentException("Harmony timeline must have events", nameof(harmonyTimeline));
            if (timeSignatureTimeline == null || timeSignatureTimeline.Events.Count == 0)
                throw new ArgumentException("Time signature timeline must have events", nameof(timeSignatureTimeline));
            if (groovePreset == null)
                throw new ArgumentNullException(nameof(groovePreset));

            var timeSignature = timeSignatureTimeline.Events.First();
            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

            // Get total bars from harmony events
            int totalBars = harmonyTimeline.Events.Max(e => e.StartBar);

            // Use default randomization settings
            var settings = RandomizationSettings.Default;

            // Use only the anchor layer
            var layer = groovePreset.AnchorLayer;

            return new GeneratorResult
            {
                BassTrack = GenerateBassTrack(harmonyTimeline, layer.BassOnsets, ticksPerQuarterNote, ticksPerMeasure, totalBars, settings),
                GuitarTrack = GenerateGuitarTrack(harmonyTimeline, layer.CompOnsets, ticksPerQuarterNote, ticksPerMeasure, totalBars, settings),
                KeysTrack = GenerateKeysTrack(harmonyTimeline, layer.PadsOnsets, ticksPerQuarterNote, ticksPerMeasure, totalBars, settings),
                DrumTrack = GenerateDrumTrack(layer, ticksPerQuarterNote, ticksPerMeasure, totalBars, settings)
            };
        }

        /// <summary>
        /// Generates tracks for all parts in a groove-based arrangement.
        /// </summary>
        /// <param name="harmonyTimeline">Harmony events defining chord progression</param>
        /// <param name="timeSignatureTimeline">Time signature events</param>
        /// <param name="settings">Optional randomization settings (uses defaults if null)</param>
        /// <returns>Dictionary of part name to generated track</returns>
        public static Dictionary<string, SongTrack> GenerateAllParts(
            HarmonyTrack harmonyTimeline,
            TimeSignatureTrack timeSignatureTimeline,
            RandomizationSettings? settings = null)
        {
            var result = new Dictionary<string, SongTrack>(StringComparer.OrdinalIgnoreCase);
            var randomSettings = settings ?? RandomizationSettings.Default;

            // Get time signature for timing calculations
            var timeSignature = timeSignatureTimeline.Events.FirstOrDefault();
            if (timeSignature == null)
                return result;

            int ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote;
            int ticksPerMeasure = (ticksPerQuarterNote * 4 * timeSignature.Numerator) / timeSignature.Denominator;

            // Generate each part
            result["Bass"] = GenerateBassTrack(harmonyTimeline, ticksPerMeasure, ticksPerQuarterNote, randomSettings);
            result["Guitar"] = GenerateGuitarTrack(harmonyTimeline, ticksPerMeasure, ticksPerQuarterNote, randomSettings);
            result["Keys"] = GenerateKeysTrack(harmonyTimeline, ticksPerMeasure, ticksPerQuarterNote, randomSettings);

            return result;
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

        private static SongTrack? GenerateBassTrack(
            HarmonyTrack harmonyTimeline,
            List<decimal> bassOnsets,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            if (bassOnsets == null || bassOnsets.Count == 0)
                return null;

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

            return new SongTrack(notes) { MidiProgramNumber = 33 }; // Electric Bass
        }

        private static SongTrack GenerateBassTrack(
            HarmonyTrack harmonyTimeline,
            int ticksPerMeasure,
            int ticksPerQuarterNote,
            RandomizationSettings settings)
        {
            var notes = new List<SongTrackNoteEvent>();
            var randomizer = new PitchRandomizer(settings);
            int quarterNoteDuration = ticksPerQuarterNote;

            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                var ctx = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    baseOctave: 2);

                int measureTick = (harmonyEvent.StartBar - 1) * ticksPerMeasure;
                int beatTick = (harmonyEvent.StartBeat - 1) * ticksPerQuarterNote;
                int absolutePosition = measureTick + beatTick;

                // Generate 4 quarter notes per harmony event (standard bass pattern)
                for (int beat = 0; beat < 4; beat++)
                {
                    decimal onsetBeat = beat + 1;
                    int midiNote = randomizer.SelectBassPitch(ctx, harmonyEvent.StartBar, onsetBeat);

                    notes.Add(new SongTrackNoteEvent(
                        noteNumber: midiNote,
                        absolutePositionTicks: absolutePosition + (beat * quarterNoteDuration),
                        noteDurationTicks: quarterNoteDuration,
                        noteOnVelocity: 90,
                        isRest: false));
                }
            }

            return new SongTrack(notes) { MidiProgramNumber = 33 }; // Electric Bass
        }

        private static SongTrack? GenerateGuitarTrack(
            HarmonyTrack harmonyTimeline,
            List<decimal> compOnsets,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            if (compOnsets == null || compOnsets.Count == 0)
                return null;

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

            return new SongTrack(notes) { MidiProgramNumber = 27 }; // Electric Guitar
        }

        private static SongTrack GenerateGuitarTrack(
            HarmonyTrack harmonyTimeline,
            int ticksPerMeasure,
            int ticksPerQuarterNote,
            RandomizationSettings settings)
        {
            var notes = new List<SongTrackNoteEvent>();
            var randomizer = new PitchRandomizer(settings);
            int eighthNoteDuration = ticksPerQuarterNote / 2;
            int? previousPitchClass = null;

            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                var ctx = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    baseOctave: 4);

                int measureTick = (harmonyEvent.StartBar - 1) * ticksPerMeasure;
                int beatTick = (harmonyEvent.StartBeat - 1) * ticksPerQuarterNote;
                int absolutePosition = measureTick + beatTick;

                // Generate 8 eighth notes per harmony event
                for (int eighth = 0; eighth < 8; eighth++)
                {
                    decimal onsetBeat = 1m + (eighth * 0.5m);
                    var (midiNote, pitchClass) = randomizer.SelectGuitarPitch(
                        ctx, 
                        harmonyEvent.StartBar, 
                        onsetBeat, 
                        previousPitchClass);

                    notes.Add(new SongTrackNoteEvent(
                        noteNumber: midiNote,
                        absolutePositionTicks: absolutePosition + (eighth * eighthNoteDuration),
                        noteDurationTicks: eighthNoteDuration,
                        noteOnVelocity: 85,
                        isRest: false));

                    previousPitchClass = pitchClass;
                }
            }

            return new SongTrack(notes) { MidiProgramNumber = 27 }; // Electric Guitar (clean)
        }


        // TO DO WHY ARE THERE 2 of these methods????

        private static SongTrack? GenerateKeysTrack(
            HarmonyTrack harmonyTimeline,
            List<decimal> padsOnsets,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            if (padsOnsets == null || padsOnsets.Count == 0)
                return null;

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

            return new SongTrack(notes) { MidiProgramNumber = 18 }; // Rock Organ
        }

        private static SongTrack GenerateKeysTrack(
            HarmonyTrack harmonyTimeline,
            int ticksPerMeasure,
            int ticksPerQuarterNote,
            RandomizationSettings settings)
        {
            var notes = new List<SongTrackNoteEvent>();
            var randomizer = new PitchRandomizer(settings);
            int halfNoteDuration = ticksPerQuarterNote * 2;

            HarmonyEvent? previousHarmony = null;

            foreach (var harmonyEvent in harmonyTimeline.Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat))
            {
                var ctx = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    baseOctave: 4);

                int measureTick = (harmonyEvent.StartBar - 1) * ticksPerMeasure;
                int beatTick = (harmonyEvent.StartBeat - 1) * ticksPerQuarterNote;
                int absolutePosition = measureTick + beatTick;

                // Two half-note hits per harmony event
                for (int half = 0; half < 2; half++)
                {
                    decimal onsetBeat = 1m + (half * 2m);
                    bool isFirstOnset = half == 0 && (previousHarmony == null || 
                        harmonyEvent.StartBar != previousHarmony.StartBar ||
                        harmonyEvent.StartBeat != previousHarmony.StartBeat);

                    var chordMidiNotes = randomizer.SelectKeysVoicing(
                        ctx, 
                        harmonyEvent.StartBar, 
                        onsetBeat, 
                        isFirstOnset);

                    foreach (var midiNote in chordMidiNotes)
                    {
                        notes.Add(new SongTrackNoteEvent(
                            noteNumber: midiNote,
                            absolutePositionTicks: absolutePosition + (half * halfNoteDuration),
                            noteDurationTicks: halfNoteDuration,
                            noteOnVelocity: 75,
                            isRest: false));
                    }
                }

                previousHarmony = harmonyEvent;
            }

            return new SongTrack(notes) { MidiProgramNumber = 4 }; // Electric Piano
        }

        /// <summary>
        /// Generates drum track: kick, snare, hi-hat at groove onsets with controlled randomness.
        /// </summary>
        private static SongTrack? GenerateDrumTrack(
            GrooveLayer layer,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars,
            RandomizationSettings settings)
        {
            var notes = new List<SongTrackNoteEvent>();
            var randomizer = new PitchRandomizer(settings);

            // TO DO - There should be a map for the drum set notes - enum

            // MIDI drum note numbers (General MIDI)
            const int kickNote = 36;
            const int snareNote = 38;
            const int closedHiHatNote = 42;

            for (int bar = 1; bar <= totalBars; bar++)
            {
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