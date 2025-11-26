using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Music.Tests;
using Music.Domain;

namespace Music.Writer
{
    /// <summary>
    /// Converts set of Phrases to a MIDI file (MidiSongDocument).
    /// </summary>
    public static class PhrasesToMidiConverter
    {
        public static MidiSongDocument Convert(Phrase phrase)
        {
            if (phrase == null)
                throw new ArgumentNullException(nameof(phrase));

            return Convert(new List<Phrase> { phrase });
        }

        public static MidiSongDocument Convert(List<Phrase> phrases)
        {
            if (phrases == null)
                throw new ArgumentNullException(nameof(phrases));

            var midiFile = new MidiFile();
            var ticksPerQuarterNote = (midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision
                ?? new TicksPerQuarterNoteTimeDivision(480)).TicksPerQuarterNote;

            // Global tempo / time signature track (track 0)
            var tempoTrack = new TrackChunk();
            var microsecondsPerQuarterNote = 60_000_000 / 112;
            tempoTrack.Events.Add(new SetTempoEvent(microsecondsPerQuarterNote) { DeltaTime = 0 });
            tempoTrack.Events.Add(new TimeSignatureEvent(4, 4) { DeltaTime = 0 });
            // EndOfTrackEvent has an internal ctor in referenced DryWetMidi build; create via reflection
            tempoTrack.Events.Add(CreateEndOfTrackEvent());
            midiFile.Chunks.Add(tempoTrack);

            int trackNumber = 1;
            int channelCursor = 0;

            foreach (var phrase in phrases)
            {
                if (channelCursor == 9) channelCursor++; // skip percussion channel unless intentionally used
                var trackChunk = CreateTrackFromPhrase(phrase, trackNumber, ticksPerQuarterNote, channelCursor);
                midiFile.Chunks.Add(trackChunk);
                trackNumber++;
                channelCursor = (channelCursor + 1) % 16;
            }

            return new MidiSongDocument(midiFile);
        }

        private static TrackChunk CreateTrackFromPhrase(
            Phrase phrase,
            int trackNumber,
            short ticksPerQuarterNote,
            int channel)
        {
            var trackChunk = new TrackChunk();

            var partName = string.IsNullOrWhiteSpace(phrase.MidiPartName) ? "Acoustic Grand Piano" : phrase.MidiPartName;
            var trackName = $"{partName} - Track {trackNumber}";
            trackChunk.Events.Add(new SequenceTrackNameEvent(trackName) { DeltaTime = 0 });

            byte programNumber = ResolveProgramNumber(phrase);
            trackChunk.Events.Add(new ProgramChangeEvent((SevenBitNumber)programNumber)
            {
                Channel = (FourBitNumber)channel,
                DeltaTime = 0
            });

            long runningDelta = 0;

            foreach (var noteEvent in phrase.NoteEvents ?? Enumerable.Empty<NoteEvent>())
            {
                if (noteEvent.IsRest)
                {
                    runningDelta += CalculateDuration(noteEvent, ticksPerQuarterNote);
                    continue;
                }

                var noteNumber = CalculateMidiNoteNumber(noteEvent.Step, noteEvent.Alter, noteEvent.Octave);
                var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);

                trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)noteNumber, (SevenBitNumber)100)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = runningDelta
                });

                trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)noteNumber, (SevenBitNumber)0)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = duration
                });

                runningDelta = 0;
            }

            // Add end-of-track meta event (created via reflection due to internal ctor)
            trackChunk.Events.Add(CreateEndOfTrackEvent());
            return trackChunk;
        }

        // Reflection helper to instantiate EndOfTrackEvent with internal constructor
        private static MidiEvent CreateEndOfTrackEvent() =>
            (MidiEvent)Activator.CreateInstance(typeof(EndOfTrackEvent), nonPublic: true)!;

        private static byte ResolveProgramNumber(Phrase phrase)
        {
            try
            {
                var prop = typeof(Phrase).GetProperty("MidiProgramNumber");
                if (prop != null)
                {
                    var val = prop.GetValue(phrase);
                    if (val is byte b) return b;
                    if (val is sbyte sb) return (byte)sb;
                    if (val is int i && i is >= 0 and <= 127) return (byte)i;
                    if (val is string s && byte.TryParse(s, out var parsed)) return parsed;
                }
            }
            catch { }
            return GetMidiProgramNumber(phrase.MidiPartName);
        }

        private static byte GetMidiProgramNumber(string instrumentName)
        {
            if (string.IsNullOrWhiteSpace(instrumentName))
                return 0;

            var instruments = MidiInstrument.GetGeneralMidiInstruments();
            var instrument = instruments.FirstOrDefault(i =>
                i.Name.Equals(instrumentName, StringComparison.OrdinalIgnoreCase));

            return instrument?.ProgramNumber ?? 0;
        }

        private static int CalculateMidiNoteNumber(char step, int alter, int octave)
        {
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
            return (octave + 1) * 12 + baseNote + alter;
        }

        private static long CalculateDuration(NoteEvent noteEvent, short ticksPerQuarterNote)
        {
            var baseDuration = (ticksPerQuarterNote * 4.0) / noteEvent.Duration;

            var dottedMultiplier = 1.0;
            var dotValue = 0.5;
            for (int i = 0; i < noteEvent.Dots; i++)
            {
                dottedMultiplier += dotValue;
                dotValue /= 2;
            }

            baseDuration *= dottedMultiplier;

            if (noteEvent.TupletActualNotes > 0 && noteEvent.TupletNormalNotes > 0)
                baseDuration *= (double)noteEvent.TupletNormalNotes / noteEvent.TupletActualNotes;

            return (long)Math.Round(baseDuration);
        }
    }
}