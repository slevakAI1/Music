using System.Collections.Generic;
using MusicXml.Domain;

namespace Music.Design
{
    public static class MusicXmlScoreFactory
    {
        public static Score CreateSingleMeasureCChordKeyboardScore()
        {
            var part = new Part
            {
                Id = "P1",
                Name = "Keyboard",
                Measures = new List<Measure>
                {
                    new Measure()
                }
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