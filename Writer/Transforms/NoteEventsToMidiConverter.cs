using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Music.Tests;
using Music.Domain;

namespace Music.Writer
{
    /// <summary>
    /// Converts Phrase or AppendNoteEventsToScoreParams to a MIDI file (MidiSongDocument).
    /// </summary>
    public static class NoteEventsToMidiConverter
    {
        /// <summary>
        /// Converts a single pitch event config to a MIDI document with default settings:
        /// - Time signature: 4/4
        /// - Treble clef (not stored in MIDI, implied)
        /// - Tempo: 112 BPM
        /// - Part: Uses instrument from Parts[0] or defaults to Piano
        /// </summary>
        public static MidiSongDocument Convert(AppendNoteEventsToScoreParams config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return Convert(new List<AppendNoteEventsToScoreParams> { config });
        }

        /// <summary>
        /// Converts multiple pitch event configs to a MIDI document with separate tracks.
        /// Each AppendNoteEventsToScoreParams becomes its own MIDI track.
        /// Assumes:
        /// - Only one staff is selected per config (either staff 1 or staff 2, not both)
        /// - Only one part per config
        /// Default settings:
        /// - Time signature: 4/4
        /// - Tempo: 112 BPM
        /// </summary>
        public static MidiSongDocument Convert(List<AppendNoteEventsToScoreParams> configs)
        {
            if (configs == null)
                throw new ArgumentNullException(nameof(configs));

            var midiFile = new MidiFile();
            var ticksPerQuarterNote = (midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision 
                ?? new TicksPerQuarterNoteTimeDivision(480)).TicksPerQuarterNote;

            // Set tempo and time signature globally (tempo track)
            var tempoTrack = new TrackChunk();
            
            // Set tempo: 112 BPM
            var microsecondsPerQuarterNote = 60_000_000 / 112;
            tempoTrack.Events.Add(new SetTempoEvent(microsecondsPerQuarterNote));

            // Set time signature: 4/4
            tempoTrack.Events.Add(new TimeSignatureEvent(4, 4));

            midiFile.Chunks.Add(tempoTrack);

            // Create a track for each config
            int trackNumber = 1;
            foreach (var config in configs)
            {
                var trackChunk = CreateTrackFromConfig(config, trackNumber, ticksPerQuarterNote);
                midiFile.Chunks.Add(trackChunk);
                trackNumber++;
            }

            return new MidiSongDocument(midiFile);
        }

        /// <summary>
        /// Converts a single Phrase to a MIDI document.
        /// </summary>
        public static MidiSongDocument Convert(Phrase phrase)
        {
            if (phrase == null)
                throw new ArgumentNullException(nameof(phrase));

            return Convert(new List<Phrase> { phrase });
        }

        /// <summary>
        /// Converts multiple Phrase objects to a MIDI document with separate tracks.
        /// Each Phrase becomes its own MIDI track.
        /// Default settings:
        /// - Time signature: 4/4
        /// - Tempo: 112 BPM
        /// </summary>
        public static MidiSongDocument Convert(List<Phrase> phrases)
        {
            if (phrases == null)
                throw new ArgumentNullException(nameof(phrases));

            var midiFile = new MidiFile();
            var ticksPerQuarterNote = (midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision
                ?? new TicksPerQuarterNoteTimeDivision(480)).TicksPerQuarterNote;

            // Set tempo and time signature globally (tempo track)
            var tempoTrack = new TrackChunk();

            // Set tempo: 112 BPM
            var microsecondsPerQuarterNote = 60_000_000 / 112;
            tempoTrack.Events.Add(new SetTempoEvent(microsecondsPerQuarterNote));

            // Set time signature: 4/4
            tempoTrack.Events.Add(new TimeSignatureEvent(4, 4));

            midiFile.Chunks.Add(tempoTrack);

            // Create a track for each phrase
            int trackNumber = 1;
            foreach (var phrase in phrases)
            {
                var trackChunk = CreateTrackFromPhrase(phrase, trackNumber, ticksPerQuarterNote);
                midiFile.Chunks.Add(trackChunk);
                trackNumber++;
            }

            return new MidiSongDocument(midiFile);
        }

        /// <summary>
        /// Creates a single MIDI track from an AppendNoteEventsToScoreParams configuration.
        /// </summary>
        private static TrackChunk CreateTrackFromConfig(
            AppendNoteEventsToScoreParams config, 
            int trackNumber, 
            short ticksPerQuarterNote)
        {
            var trackChunk = new TrackChunk();

            // Determine track name and program number from part
            var partName = config.Parts?.FirstOrDefault() ?? "Acoustic Grand Piano";
            var staffNumber = config.Staffs?.FirstOrDefault() ?? 1;
            var trackName = $"{partName} - Staff {staffNumber}";
            
            trackChunk.Events.Add(new SequenceTrackNameEvent(trackName));

            // Get MIDI program number from instrument name in Parts[0]
            byte programNumber = GetMidiProgramNumber(partName);
            
            // Set program change to the selected instrument
            trackChunk.Events.Add(new ProgramChangeEvent((SevenBitNumber)programNumber));

            // Convert pitch events to MIDI notes
            long currentTime = 0;

            foreach (var noteEvent in config.NoteEvents ?? Enumerable.Empty<NoteEvent>())
            {
                if (noteEvent.IsRest)
                {
                    // For rests, just advance time without adding notes
                    currentTime += CalculateDuration(noteEvent, ticksPerQuarterNote);
                }
                else
                {
                    // Calculate MIDI note number from pitch
                    var noteNumber = CalculateMidiNoteNumber(
                        noteEvent.Step, 
                        noteEvent.Alter, 
                        noteEvent.Octave);

                    var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);

                    // Add Note On event
                    trackChunk.Events.Add(new NoteOnEvent(
                        (SevenBitNumber)noteNumber,
                        (SevenBitNumber)100) // velocity
                    { DeltaTime = currentTime });

                    // Add Note Off event
                    trackChunk.Events.Add(new NoteOffEvent(
                        (SevenBitNumber)noteNumber,
                        (SevenBitNumber)0)
                    { DeltaTime = duration });

                    currentTime = 0; // Delta times are relative, reset after note off
                }
            }

            return trackChunk;
        }

        /// <summary>
        /// Creates a single MIDI track from a Phrase.
        /// Uses Phrase.MidiPartName for track name and derives ProgramChange from MidiProgramNumber if available; 
        /// falls back to MidiPartName lookup if needed.
        /// </summary>
        private static TrackChunk CreateTrackFromPhrase(
            Phrase phrase,
            int trackNumber,
            short ticksPerQuarterNote)
        {
            var trackChunk = new TrackChunk();

            var partName = string.IsNullOrWhiteSpace(phrase.MidiPartName) ? "Acoustic Grand Piano" : phrase.MidiPartName;
            var trackName = $"{partName} - Track {trackNumber}";
            trackChunk.Events.Add(new SequenceTrackNameEvent(trackName));

            // Prefer explicit MIDI program number if present; otherwise resolve by instrument name
            byte programNumber = ResolveProgramNumber(phrase);

            // Set program change to the selected instrument
            trackChunk.Events.Add(new ProgramChangeEvent((SevenBitNumber)programNumber));

            // Convert pitch events to MIDI notes
            long currentTime = 0;

            foreach (var noteEvent in phrase.NoteEvents ?? Enumerable.Empty<NoteEvent>())
            {
                if (noteEvent.IsRest)
                {
                    // For rests, just advance time without adding notes
                    currentTime += CalculateDuration(noteEvent, ticksPerQuarterNote);
                }
                else
                {
                    // Calculate MIDI note number from pitch
                    var noteNumber = CalculateMidiNoteNumber(
                        noteEvent.Step,
                        noteEvent.Alter,
                        noteEvent.Octave);

                    var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);

                    // Add Note On event
                    trackChunk.Events.Add(new NoteOnEvent(
                        (SevenBitNumber)noteNumber,
                        (SevenBitNumber)100) // velocity
                    { DeltaTime = currentTime });

                    // Add Note Off event
                    trackChunk.Events.Add(new NoteOffEvent(
                        (SevenBitNumber)noteNumber,
                        (SevenBitNumber)0)
                    { DeltaTime = duration });

                    currentTime = 0; // Delta times are relative, reset after note off
                }
            }

            return trackChunk;
        }

        /// <summary>
        /// Resolves MIDI program number from Phrase.
        /// Tries Phrase.MidiProgramNumber (byte or parseable string). Falls back to instrument name lookup by Phrase.MidiPartName.
        /// </summary>
        private static byte ResolveProgramNumber(Phrase phrase)
        {
            // If MidiProgramNumber exists, use it. It may be byte or string; attempt parsing.
            try
            {
                var prop = typeof(Phrase).GetProperty("MidiProgramNumber");
                if (prop != null)
                {
                    var val = prop.GetValue(phrase);
                    if (val is byte b) return b;
                    if (val is sbyte sb) return (byte)sb;
                    if (val is int i && i >= 0 && i <= 127) return (byte)i;
                    if (val is string s && byte.TryParse(s, out var parsed)) return parsed;
                }
            }
            catch
            {
                // ignore reflection errors, fall back to name
            }

            // Fallback: use instrument name
            return GetMidiProgramNumber(phrase.MidiPartName);
        }

        /// <summary>
        /// Gets the MIDI program number from an instrument name.
        /// Returns 0 (Acoustic Grand Piano) if the instrument name is not found.
        /// </summary>
        private static byte GetMidiProgramNumber(string instrumentName)
        {
            if (string.IsNullOrWhiteSpace(instrumentName))
                return 0;

            // Get all MIDI instruments
            var instruments = MidiInstrument.GetGeneralMidiInstruments();
            
            // Find matching instrument by name (case-insensitive)
            var instrument = instruments.FirstOrDefault(i => 
                i.Name.Equals(instrumentName, StringComparison.OrdinalIgnoreCase));

            // Return program number or default to piano (0)
            return instrument?.ProgramNumber ?? 0;
        }

        /// <summary>
        /// Calculates MIDI note number from step, alter, and octave.
        /// MIDI note 60 = Middle C (C4)
        /// </summary>
        private static int CalculateMidiNoteNumber(char step, int alter, int octave)
        {
            // Map step to base note number within octave
            var baseNote = char.ToUpper(step) switch
            {
                'C' => 0,
                'D' => 2,
                'E' => 4,
                'F' => 5,
                'G' => 7,
                'A' => 9,
                'B' => 11,
                _ => 0
            };

            // MIDI note number = (octave + 1) * 12 + baseNote + alter
            // Octave 4 starts at MIDI note 60 (middle C)
            return (octave + 1) * 12 + baseNote + alter;
        }

        /// <summary>
        /// Calculates duration in MIDI ticks based on note value and dots.
        /// Duration codes: 1=whole, 2=half, 4=quarter, 8=eighth, etc.
        /// </summary>
        private static long CalculateDuration(NoteEvent noteEvent, short ticksPerQuarterNote)
        {
            // Base duration in ticks (quarter note = ticksPerQuarterNote)
            var baseDuration = (ticksPerQuarterNote * 4.0) / noteEvent.Duration;

            // Apply dots (each dot adds half of the previous value)
            var dottedMultiplier = 1.0;
            var dotValue = 0.5;
            for (int i = 0; i < noteEvent.Dots; i++)
            {
                dottedMultiplier += dotValue;
                dotValue /= 2;
            }

            baseDuration *= dottedMultiplier;

            // Apply tuplet if present
            if (noteEvent.TupletActualNotes > 0 && noteEvent.TupletNormalNotes > 0)
            {
                baseDuration *= (double)noteEvent.TupletNormalNotes / noteEvent.TupletActualNotes;
            }

            return (long)Math.Round(baseDuration);
        }
    }
}