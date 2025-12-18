using Music.Designer;

namespace Music.Writer.Generator
{
    /// <summary>
    /// Generates phrases for multiple parts using a GroovePreset and harmony timeline.
    /// Pitch selection is deterministic (root-based for bass, cycling chord tones for melody instruments).
    /// </summary>
    public static class GrooveDrivenGenerator
    {
        /// <summary>
        /// Result of generating all phrases for a groove preset.
        /// </summary>
        public sealed class GeneratorResult
        {
            public Phrase? BassPhrase { get; init; }
            public Phrase? GuitarPhrase { get; init; }
            public Phrase? KeysPhrase { get; init; }
            public Phrase? DrumPhrase { get; init; }
        }

        /// <summary>
        /// Generates all phrases based on harmony timeline and groove preset.
        /// </summary>
        /// <param name="harmonyTimeline">The harmony events defining chords per bar.</param>
        /// <param name="timeSignatureTimeline">Time signature info for tick calculations.</param>
        /// <param name="groovePreset">The groove preset defining onset patterns.</param>
        /// <returns>Generated phrases for each role.</returns>
        public static GeneratorResult Generate(
            HarmonyTimeline harmonyTimeline,
            TimeSignatureTimeline timeSignatureTimeline,
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

            // Use only the anchor layer for deterministic generation
            var layer = groovePreset.AnchorLayer;

            return new GeneratorResult
            {
                BassPhrase = GenerateBassPhrase(harmonyTimeline, layer.BassOnsets, ticksPerQuarterNote, ticksPerMeasure, totalBars),
                GuitarPhrase = GenerateGuitarPhrase(harmonyTimeline, layer.CompOnsets, ticksPerQuarterNote, ticksPerMeasure, totalBars),
                KeysPhrase = GenerateKeysPhrase(harmonyTimeline, layer.PadsOnsets, ticksPerQuarterNote, ticksPerMeasure, totalBars),
                DrumPhrase = GenerateDrumPhrase(layer, ticksPerQuarterNote, ticksPerMeasure, totalBars)
            };
        }

        /// <summary>
        /// Generates bass phrase: root note at each groove onset.
        /// </summary>
        private static Phrase? GenerateBassPhrase(
            HarmonyTimeline harmonyTimeline,
            List<decimal> bassOnsets,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars)
        {
            if (bassOnsets == null || bassOnsets.Count == 0)
                return null;

            var notes = new List<PhraseNote>();
            const int bassOctave = 2;
            const int defaultVelocity = 95;

            // Build a bar -> harmony event lookup
            var harmonyByBar = harmonyTimeline.Events
                .OrderBy(e => e.StartBar)
                .ThenBy(e => e.StartBeat)
                .ToList();

            for (int bar = 1; bar <= totalBars; bar++)
            {
                // Find active harmony for this bar
                var harmonyEvent = GetActiveHarmonyForBar(harmonyByBar, bar);
                if (harmonyEvent == null)
                    continue;

                // Get chord root pitch class
                var pitchContext = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    bassOctave);

                // Root MIDI note = chord root pitch class + octave
                int rootMidiNote = pitchContext.ChordRootPitchClass + ((bassOctave + 1) * 12);

                int measureStartTick = (bar - 1) * ticksPerMeasure;

                // Generate note at each onset
                for (int i = 0; i < bassOnsets.Count; i++)
                {
                    // Convert 1-based beat position to 0-based tick offset
                    decimal onsetBeat = bassOnsets[i];
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * ticksPerQuarterNote);

                    // Duration: until next onset or end of measure
                    int nextOnsetTick = (i + 1 < bassOnsets.Count)
                        ? measureStartTick + (int)((bassOnsets[i + 1] - 1m) * ticksPerQuarterNote)
                        : measureStartTick + ticksPerMeasure;
                    int duration = nextOnsetTick - onsetTick;

                    notes.Add(new PhraseNote(
                        noteNumber: rootMidiNote,
                        absolutePositionTicks: onsetTick,
                        noteDurationTicks: duration,
                        noteOnVelocity: defaultVelocity,
                        isRest: false));
                }
            }

            return new Phrase(notes) { MidiProgramNumber = 33 }; // Electric Bass
        }

        /// <summary>
        /// Generates guitar phrase: cycling chord tones at each groove onset.
        /// </summary>
        private static Phrase? GenerateGuitarPhrase(
            HarmonyTimeline harmonyTimeline,
            List<decimal> compOnsets,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars)
        {
            if (compOnsets == null || compOnsets.Count == 0)
                return null;

            var notes = new List<PhraseNote>();
            const int guitarOctave = 4;
            const int defaultVelocity = 85;

            var harmonyByBar = harmonyTimeline.Events
                .OrderBy(e => e.StartBar)
                .ThenBy(e => e.StartBeat)
                .ToList();

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var harmonyEvent = GetActiveHarmonyForBar(harmonyByBar, bar);
                if (harmonyEvent == null)
                    continue;

                // Get chord MIDI notes
                var pitchContext = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    guitarOctave);

                var chordNotes = pitchContext.ChordMidiNotes;
                if (chordNotes.Count == 0)
                    continue;

                int measureStartTick = (bar - 1) * ticksPerMeasure;

                for (int i = 0; i < compOnsets.Count; i++)
                {
                    // Convert 1-based beat position to 0-based tick offset
                    decimal onsetBeat = compOnsets[i];
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * ticksPerQuarterNote);

                    // Cycle through chord tones
                    int noteIndex = i % chordNotes.Count;
                    int midiNote = chordNotes[noteIndex];

                    // Duration: until next onset or end of measure
                    int nextOnsetTick = (i + 1 < compOnsets.Count)
                        ? measureStartTick + (int)((compOnsets[i + 1] - 1m) * ticksPerQuarterNote)
                        : measureStartTick + ticksPerMeasure;
                    int duration = nextOnsetTick - onsetTick;

                    notes.Add(new PhraseNote(
                        noteNumber: midiNote,
                        absolutePositionTicks: onsetTick,
                        noteDurationTicks: duration,
                        noteOnVelocity: defaultVelocity,
                        isRest: false));
                }
            }

            return new Phrase(notes) { MidiProgramNumber = 27 }; // Electric Guitar
        }

        /// <summary>
        /// Generates keys phrase: chord blocks at each groove onset.
        /// </summary>
        private static Phrase? GenerateKeysPhrase(
            HarmonyTimeline harmonyTimeline,
            List<decimal> padsOnsets,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars)
        {
            if (padsOnsets == null || padsOnsets.Count == 0)
                return null;

            var notes = new List<PhraseNote>();
            const int keysOctave = 4;
            const int defaultVelocity = 75;

            var harmonyByBar = harmonyTimeline.Events
                .OrderBy(e => e.StartBar)
                .ThenBy(e => e.StartBeat)
                .ToList();

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var harmonyEvent = GetActiveHarmonyForBar(harmonyByBar, bar);
                if (harmonyEvent == null)
                    continue;

                // Get chord MIDI notes
                var pitchContext = HarmonyPitchContextBuilder.Build(
                    harmonyEvent.Key,
                    harmonyEvent.Degree,
                    harmonyEvent.Quality,
                    harmonyEvent.Bass,
                    keysOctave);

                var chordNotes = pitchContext.ChordMidiNotes;
                if (chordNotes.Count == 0)
                    continue;

                int measureStartTick = (bar - 1) * ticksPerMeasure;

                for (int i = 0; i < padsOnsets.Count; i++)
                {
                    // Convert 1-based beat position to 0-based tick offset
                    decimal onsetBeat = padsOnsets[i];
                    int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * ticksPerQuarterNote);

                    // Duration: until next onset or end of measure
                    int nextOnsetTick = (i + 1 < padsOnsets.Count)
                        ? measureStartTick + (int)((padsOnsets[i + 1] - 1m) * ticksPerQuarterNote)
                        : measureStartTick + ticksPerMeasure;
                    int duration = nextOnsetTick - onsetTick;

                    // Add all chord tones as a block
                    foreach (int midiNote in chordNotes)
                    {
                        notes.Add(new PhraseNote(
                            noteNumber: midiNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: duration,
                            noteOnVelocity: defaultVelocity,
                            isRest: false));
                    }
                }
            }

            return new Phrase(notes) { MidiProgramNumber = 18 }; // Rock Organ
        }

        /// <summary>
        /// Generates drum phrase: kick, snare, hi-hat at groove onsets.
        /// </summary>
        private static Phrase? GenerateDrumPhrase(
            GrooveLayer layer,
            int ticksPerQuarterNote,
            int ticksPerMeasure,
            int totalBars)
        {
            var notes = new List<PhraseNote>();

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
                        notes.Add(new PhraseNote(
                            noteNumber: kickNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: ticksPerQuarterNote,
                            noteOnVelocity: 100,
                            isRest: false));
                    }
                }

                // Snare pattern
                if (layer.SnareOnsets != null)
                {
                    foreach (var onsetBeat in layer.SnareOnsets)
                    {
                        int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * ticksPerQuarterNote);
                        notes.Add(new PhraseNote(
                            noteNumber: snareNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: ticksPerQuarterNote,
                            noteOnVelocity: 90,
                            isRest: false));
                    }
                }

                // Hi-hat pattern
                if (layer.HatOnsets != null)
                {
                    foreach (var onsetBeat in layer.HatOnsets)
                    {
                        int onsetTick = measureStartTick + (int)((onsetBeat - 1m) * ticksPerQuarterNote);
                        notes.Add(new PhraseNote(
                            noteNumber: closedHiHatNote,
                            absolutePositionTicks: onsetTick,
                            noteDurationTicks: ticksPerQuarterNote / 2, // shorter duration for hi-hat
                            noteOnVelocity: 70,
                            isRest: false));
                    }
                }
            }

            if (notes.Count == 0)
                return null;

            return new Phrase(notes) { MidiProgramNumber = 255 }; // Drum Set
        }

        /// <summary>
        /// Gets the active harmony event for a given bar.
        /// Returns the most recent harmony event that starts on or before this bar.
        /// </summary>
        private static HarmonyEvent? GetActiveHarmonyForBar(List<HarmonyEvent> harmonyEvents, int bar)
        {
            // Find the harmony event active at this bar
            // (last event where StartBar <= bar)
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