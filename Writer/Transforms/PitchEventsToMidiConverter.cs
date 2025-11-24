using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Music.Tests;

namespace Music.Writer
{
    /// <summary>
    /// Converts AppendPitchEventsParams to a MIDI file (MidiSongDocument).
    /// </summary>
    public static class PitchEventsToMidiConverter
    {
        /// <summary>
        /// Converts pitch events to a MIDI document with default settings:
        /// - Time signature: 4/4
        /// - Treble clef (not stored in MIDI, implied)
        /// - Tempo: 112 BPM
        /// - Part: Piano right hand (single track)
        /// </summary>
        public static MidiSongDocument Convert(AppendPitchEventsParams config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var midiFile = new MidiFile();
            var trackChunk = new TrackChunk();

            // Set tempo: 112 BPM
            // Tempo is in microseconds per quarter note: 60,000,000 / BPM
            var microsecondsPerQuarterNote = 60_000_000 / 112;
            trackChunk.Events.Add(new SetTempoEvent(microsecondsPerQuarterNote));

            // Set time signature: 4/4
            trackChunk.Events.Add(new TimeSignatureEvent(4, 4));

            // Set track name
            trackChunk.Events.Add(new SequenceTrackNameEvent("Piano - Right Hand"));

            // Set program change for piano (MIDI program 0)
            trackChunk.Events.Add(new ProgramChangeEvent((SevenBitNumber)0));

            // Convert pitch events to MIDI notes
            long currentTime = 0;
            var ticksPerQuarterNote = midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision 
                ?? new TicksPerQuarterNoteTimeDivision(480);

            foreach (var pitchEvent in config.PitchEvents ?? Enumerable.Empty<PitchEvent>())
            {
                if (pitchEvent.IsRest)
                {
                    // For rests, just advance time without adding notes
                    currentTime += CalculateDuration(pitchEvent, ticksPerQuarterNote.TicksPerQuarterNote);
                }
                else
                {
                    // Calculate MIDI note number from pitch
                    var noteNumber = CalculateMidiNoteNumber(
                        pitchEvent.Step, 
                        pitchEvent.Alter, 
                        pitchEvent.Octave);

                    var duration = CalculateDuration(pitchEvent, ticksPerQuarterNote.TicksPerQuarterNote);

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

            midiFile.Chunks.Add(trackChunk);

            return new MidiSongDocument(midiFile);
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
        private static long CalculateDuration(PitchEvent pitchEvent, short ticksPerQuarterNote)
        {
            // Base duration in ticks (quarter note = ticksPerQuarterNote)
            var baseDuration = (ticksPerQuarterNote * 4.0) / pitchEvent.Duration;

            // Apply dots (each dot adds half of the previous value)
            var dottedMultiplier = 1.0;
            var dotValue = 0.5;
            for (int i = 0; i < pitchEvent.Dots; i++)
            {
                dottedMultiplier += dotValue;
                dotValue /= 2;
            }

            baseDuration *= dottedMultiplier;

            // Apply tuplet if present
            if (pitchEvent.TupletActualNotes > 0 && pitchEvent.TupletNormalNotes > 0)
            {
                baseDuration *= (double)pitchEvent.TupletNormalNotes / pitchEvent.TupletActualNotes;
            }

            return (long)Math.Round(baseDuration);
        }
    }
}