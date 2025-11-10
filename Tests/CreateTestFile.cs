using MusicXml.Domain;

namespace Music.Tests
{
    public static class CreateTestFile
    {
        public static Score CreateSingleMeasureCChordKeyboardScore()
        {
            // Measure attributes: C major, 4/4, treble clef, divisions=8 (supports up to 32nd notes)
            var attributes = new MeasureAttributes
            {
                Divisions = 8,  // Changed from 1 to 8
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

            // Build the first part with the first measure plus 7 empty measures
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

            // Second part "Trumpet" with the same pattern
            var trumpetMeasure = new Measure
            {
                Attributes = attributes
            };

            var tNoteC = WholeNote('C', 4, isChordTone: false);
            var tNoteE = WholeNote('E', 4, isChordTone: true);
            var tNoteG = WholeNote('G', 4, isChordTone: true);

            trumpetMeasure.MeasureElements.Add(new MeasureElement { Type = MeasureElementType.Note, Element = tNoteC });
            trumpetMeasure.MeasureElements.Add(new MeasureElement { Type = MeasureElementType.Note, Element = tNoteE });
            trumpetMeasure.MeasureElements.Add(new MeasureElement { Type = MeasureElementType.Note, Element = tNoteG });

            var trumpetMeasures = new List<Measure> { trumpetMeasure };
            for (int i = 0; i < 7; i++)
            {
                trumpetMeasures.Add(new Measure { Attributes = attributes });
            }

            var trumpetPart = new Part
            {
                Id = "P2",
                Name = "Trumpet",
                InstrumentName = "Trumpet",
                MidiChannel = 2,
                Measures = trumpetMeasures
            };

            var score = new Score
            {
                MovementTitle = "One-bar C Major (Violin + Trumpet)",
                Identification = new Identification(),
                Parts = new List<Part> { part, trumpetPart }
            };

            return score;
        }
    }
}