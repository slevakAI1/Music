using Melanchall.DryWetMidi.Interaction;
using Music.Designer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Music.Writer
{
    public static class WriterFormExtensions
    {
        // Applies design-driven defaults into an existing Writer instance.
        // Accept a nullable receiver so callers holding a `Writer?` (e.g. Globals.GenerationData)
        // can call this extension directly without a null-forgiving operator.
        public static void UpdateFromDesigner(this WriterFormData? data, Designer.Designer? design)
        {
            if (data == null) return;

            // ========== PARTS SYNCHRONIZATION ==========
            // Update the Writer parts to be in sync with the Designer parts if there are differences.
            // Preserve existing checked states

            var availableParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (design?.PartSet?.Parts != null)
            {
                foreach (var v in design.PartSet.Parts)
                {
                    var name = v?.PartName;
                    if (!string.IsNullOrWhiteSpace(name))
                        availableParts.Add(name!);
                }
            }

            var existingParts = data.PartsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var newPartsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (existingParts.Count == 0)
            {
                // No existing state: default all parts to checked
                foreach (var name in availableParts)
                    newPartsState[name] = true;
            }
            else
            {
                // Preserve existing checked states for parts that still exist
                foreach (var name in availableParts)
                {
                    if (existingParts.TryGetValue(name, out var isChecked))
                        newPartsState[name] = isChecked;
                    else
                        newPartsState[name] = false; // New parts default to unchecked
                }
            }

            data.PartsState = newPartsState;

            // ========== SECTIONS SYNCHRONIZATION ==========
            // Update the Writer sections to be in sync with the Designer sections if there are differences.
            // Preserve existing checked states for sections

            var availableSections = new List<string>();
            if (design?.SectionSet?.Sections != null)
            {
                foreach (var section in design.SectionSet.Sections)
                {
                    var name = section?.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                        availableSections.Add(name!);
                }
            }

            var existingSections = data.SectionsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var newSectionsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (existingSections.Count == 0)
            {
                // No existing state: default all sections to unchecked (unlike parts, sections default to unchecked)
                foreach (var name in availableSections)
                    newSectionsState[name] = false;
            }
            else
            {
                // Preserve existing checked states for sections that still exist
                foreach (var name in availableSections)
                {
                    if (existingSections.TryGetValue(name, out var isChecked))
                        newSectionsState[name] = isChecked;
                    else
                        newSectionsState[name] = false; // New sections default to unchecked
                }
            }

            data.SectionsState = newSectionsState;

            // ========== END BAR ADJUSTMENT ==========
            // Ensure EndBar is within valid range based on design's total bars

            var total = design?.SectionSet?.TotalBars ?? 0;
            if (total > 0)
            {
                if (!data.EndBar.HasValue)
                    data.EndBar = total;
                else
                    data.EndBar = Math.Max(1, Math.Min(total, data.EndBar.Value));
            }
        }

        /// <summary>
        /// Converts Writer data to a Phrase for use with the phrase control.
        /// Similar to ToAppendNoteEventsParams but creates a single-part Phrase object.
        /// </summary>
        public static Phrase ToPhrase(this WriterFormData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            // Default part for new phrase
            var midiProgramName = "Acoustic Grand Piano";

            var phraseNotes = new List<PhraseNote>();

            // Constants
            const int ticksPerQuarterNote = 480;
            int currentPosition = 0;

            // Get tuplet settings from writer data
            bool isTuplet = !string.IsNullOrWhiteSpace(data.TupletNumber);

            if (data.IsChord ?? false)  // null = false
            {
                // Create Chord pitch events
                // Chords will be resolved into their component notes by ChordConverter
                var harmonicEvent = new HarmonicEvent
                {
                    Key = data.ChordKey ?? "C major",
                    Degree = data.ChordDegree ?? 1,
                    Quality = data.ChordQuality ?? "Major",
                    Bass = data.ChordBase ?? "root"
                };

                var chordNotes = ChordConverter.Convert(
                    harmonicEvent,
                    baseOctave: data.Octave,
                    noteValue: GetNoteValue(data.NoteValue));

                // Convert chord notes to PhraseNote with MIDI properties
                foreach (var chordNote in chordNotes)
                {
                    int noteDurationTicks = CalculateNoteDurationTicks(
                        GetNoteValue(data.NoteValue),
                        data.Dots,
                        ticksPerQuarterNote);

                    var phraseNote = new PhraseNote(
                        noteNumber: CalculateNoteNumber(chordNote.Step, chordNote.Alter, chordNote.Octave),
                        absolutePositionTicks: currentPosition,
                        noteDurationTicks: noteDurationTicks,
                        noteOnVelocity: 100,
                        isRest: false);

                    phraseNotes.Add(phraseNote);
                }

                // Advance position after chord
                currentPosition += CalculateNoteDurationTicks(
                    GetNoteValue(data.NoteValue),
                    data.Dots,
                    ticksPerQuarterNote);
            }
            else
            {
                // Single note or rest - create specified number of notes
                for (int i = 0; i < (data.NumberOfNotes.GetValueOrDefault(1)); i++)
                {
                    int noteDurationTicks = CalculateNoteDurationTicks(
                        GetNoteValue(data.NoteValue),
                        data.Dots,
                        ticksPerQuarterNote);

                    int noteNumber = data.IsRest ?? false
                        ? 60 // Default middle C for rests (not used but required)
                        : CalculateNoteNumber(data.Step, GetAlter(data.Accidental), data.Octave);

                    var phraseNote = new PhraseNote(
                        noteNumber: noteNumber,
                        absolutePositionTicks: currentPosition,
                        noteDurationTicks: noteDurationTicks,
                        noteOnVelocity: 100,
                        isRest: data.IsRest ?? false);

                    phraseNotes.Add(phraseNote);

                    currentPosition += noteDurationTicks;
                }
            }

            var phrase = new Phrase(midiProgramName, phraseNotes);

            return phrase;
        }

        private static int CalculateNoteNumber(char step, int alter, int octave)
        {
            // C4 = MIDI note 60
            int baseNote = step switch
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

        private static int CalculateNoteDurationTicks(int duration, int dots, int ticksPerQuarterNote)
        {
            // Duration: 1=whole, 2=half, 4=quarter, 8=eighth, etc.
            // Base ticks for this duration
            int baseTicks = (ticksPerQuarterNote * 4) / duration;

            // Apply dots: each dot adds half of the previous value
            int totalTicks = baseTicks;
            int dotValue = baseTicks;
            for (int i = 0; i < dots; i++)
            {
                dotValue /= 2;
                totalTicks += dotValue;
            }

            return totalTicks;
        }

        private static int GetNoteValue(string? noteValueString)
        {
            if (noteValueString != null && Music.MusicConstants.NoteValueMap.TryGetValue(noteValueString, out var nv))
            {
                return nv;
            }
            return 4; // default quarter note
        }

        private static int GetAlter(string? accidental)
        {
            return (accidental ?? "Natural") switch
            {
                var s when s.Equals("Sharp", StringComparison.OrdinalIgnoreCase) => 1,
                var s when s.Equals("Flat", StringComparison.OrdinalIgnoreCase) => -1,
                _ => 0
            };
        }
    }
}