using System.Collections.Generic;
using MusicXml.Domain;
using Music;

namespace Music.Tests
{
    // This one has the part named "Violins I" to match Notion sample
    public static class CreateTestFile2
    {
        public static Score CreateSingleMeasureCChordKeyboardScore()
        {
            // Measure attributes: C major, 4/4, treble clef, divisions=480 (fixed default)
            var attributes = new MeasureAttributes
            {
                Divisions = Music.MusicConstants.TicksPerQuarterNote,  // use centralized constant
                Key = new Key { Fifths = 0, Mode = "major" },
                Time = new Time { Beats = 4, Mode = "4" },
                Clef = new Clef { Sign = "G", Line = 2 }
            };

            var measure = new Measure
            {
                Attributes = attributes
            };

            // Helper to create a whole-note pitch
            Note WholeNote(char step, int octave, bool isChordTone)
            {
                return new Note
                {
                    Type = "whole",
                    Duration = 4,
                    Voice = 1,
                    Staff = 1,
                    IsChordTone = isChordTone,
                    Pitch = new Pitch
                    {
                        Step = step,
                        Octave = octave,
                        Alter = 0
                    }
                };
            }

            // C major triad: C4, E4, G4; mark E and G as chord tones
            var noteC = WholeNote('C', 4, isChordTone: false);
            var noteE = WholeNote('E', 4, isChordTone: true);
            var noteG = WholeNote('G', 4, isChordTone: true);

            measure.MeasureElements.Add(new MeasureElement { Type = MeasureElementType.Note, Element = noteC });
            measure.MeasureElements.Add(new MeasureElement { Type = MeasureElementType.Note, Element = noteE });
            measure.MeasureElements.Add(new MeasureElement { Type = MeasureElementType.Note, Element = noteG });

            // Build the part with the first measure plus 7 empty measures
            var measures = new List<Measure> { measure };
            for (int i = 0; i < 7; i++)
            {
                measures.Add(new Measure { Attributes = attributes });
            }

            var part = new Part
            {
                Id = "P1",
                Name = "Violins I",          // match Notion sample
                InstrumentName = "Violins I",
                MidiChannel = 1,
                Measures = measures
            };

            var score = new Score
            {
                MovementTitle = "One-bar C Major (Keyboard)",
                Identification = new Identification(),
                Parts = new List<Part> { part }
            };

            return score;
        }
    }
}