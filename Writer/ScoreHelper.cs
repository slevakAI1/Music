using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MusicXml.Domain;
using Music;
using Music.Designer;

namespace Music.Writer
{
    internal static class ScoreHelper
    {
        public static Score? CreateNewScore(
            Designer.Designer? design,
            ref MeasureMeta measureMeta,
            string movementTitle)
        {
            // Clear the UsedDivisionsPerMeasure since this is a new score with no notes
            measureMeta = new MeasureMeta();

            // Create a fresh Score instance and assign to the local cache (returned to caller)
            var score = new Score
            {
                MovementTitle = movementTitle,
                Identification = new Identification(),
                Parts = new List<Part>()
            };

            // TODO check this logic

            // Prefer the passed design, fall back to Globals if missing
            var usedDesign = design ?? Globals.Designer;
            if (usedDesign == null)
            {
                MessageBoxHelper.Show("No design available. Create or set a design before creating a new score.", "No Design", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            // Collect part names from design voices (same logic as PopulatePartsFromDesign)
            var partNames = new List<string>();
            if (usedDesign.PartSet?.Parts != null)
            {
                foreach (var v in usedDesign.PartSet.Parts)
                {
                    var name = v?.PartName;
                    if (!string.IsNullOrWhiteSpace(name))
                        partNames.Add(name);
                }
            }

            if (partNames.Count == 0)
            {
                MessageBoxHelper.Show("Design contains no voices to create parts from.", "No Parts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            // Ensure parts exist in the new score (this will add Part entries for each name)
            try
            {
                EnsurePartsExist(score, partNames);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating parts: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            // Load the two-staff voice catalog once
            var twoStaffVoices = VoiceCatalog.GetTwoStaffVoices();

            // Determine how many measures to create from the design's section set
            var totalBars = usedDesign.SectionSet?.TotalBars ?? 0;

            // Ensure each part has a Measures list and enough Measure entries
            foreach (var part in score.Parts ?? Enumerable.Empty<Part>())
            {
                part.Measures ??= new List<Measure>();
                while (part.Measures.Count < totalBars)
                {
                    part.Measures.Add(new Measure());
                }
            }

            // -- Populate attributes for measures
            if (score.Parts != null)
            {
                foreach (var p in score.Parts)
                {
                    if (p.Measures == null || p.Measures.Count == 0) continue;

                    // Check if this voice requires two staves
                    var needsTwoStaves = twoStaffVoices.Contains(p.Name, StringComparer.OrdinalIgnoreCase);

                    // Ensure first measure has full attributes
                    var first = p.Measures[0];
                    first.Attributes ??= new MeasureAttributes();
                    // Use single source of truth for divisions
                    first.Attributes.Divisions = MusicConstants.DefaultDivisions;
                    first.Attributes.Key ??= new Key { Fifths = 0, Mode = "major" };
                    first.Attributes.Time ??= new Time();
                    first.Attributes.Time.Beats = 4;
                    first.Attributes.Time.Mode = "4";

                    if (needsTwoStaves)
                    {
                        // Two-staff instrument: set staff count and both clefs
                        first.Attributes.Staves = 2;
                        
                        // Add two clefs: treble (staff 1) and bass (staff 2)
                        first.Attributes.Clefs = new List<Clef>
                        {
                            new Clef { Sign = "G", Line = 2, Number = 1 },  // Treble clef, staff 1
                            new Clef { Sign = "F", Line = 4, Number = 2 }   // Bass clef, staff 2
                        };

                        // Set legacy Clef property to first clef for backward compatibility
                        first.Attributes.Clef = first.Attributes.Clefs[0];
                    }
                    else
                    {
                        // Single-staff instrument: treble clef only
                        first.Attributes.Clef = new Clef { Sign = "G", Line = 2 };
                    }

                    var divisions = first.Attributes.Divisions;

                    // For each subsequent measure create a minimal attributes object containing only Divisions.
                    for (int i = 1; i < p.Measures.Count; i++)
                    {
                        var m = p.Measures[i];
                        if (m == null)
                        {
                            m = new Measure();
                            p.Measures[i] = m;
                        }

                        // Ensure only Divisions is present for subsequent measures
                        m.Attributes = new MeasureAttributes
                        {
                            Divisions = divisions
                        };
                    }
                }
            }

            // Apply tempo to score here. Adding to first measure of each part. Will only serialize once though.
            try
            {
                var te = usedDesign?.TempoTimeline?.Events?.FirstOrDefault();
                if (te != null)
                {
                    var dir = new Direction
                    {
                        DirectionType = new DirectionType
                        {
                            Metronome = new Metronome
                            {
                                BeatUnit = "quarter",
                                PerMinute = te.TempoBpm
                            }
                        },
                        Sound = new Sound
                        {
                            Tempo = te.TempoBpm
                        }
                    };

                    if (score.Parts != null)
                    {
                        foreach (var p in score.Parts)
                        {
                            p.Measures[0].Direction = dir;
                        }
                    }
                }
            }
            catch
            {
                // Swallow any exceptions from optional tempo application; don't block main operation.
            }


            //=================================
            // TODO : Populate the entire score with 16 note rests per measure per part by calling NoteWriter.Insert()


            //=================================

            return score;
        }

        /// <summary>
        /// Adds a score to the score list at index 1, pushing existing items down.
        /// Index 0 is always reserved for the current working score.
        /// </summary>
        /// <param name="score">The score to add. Must not be null and must have a non-empty MovementTitle.</param>
        /// <param name="scoreList">The target score list. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when score or scoreList is null.</exception>
        /// <exception cref="ArgumentException">Thrown when score.MovementTitle is null or empty.</exception>
        public static void AddScoreToScoreList(Score score, List<Score> scoreList)
        {
            if (score == null)
                throw new ArgumentNullException(nameof(score), "Score cannot be null.");

            if (scoreList == null)
                throw new ArgumentNullException(nameof(scoreList), "Score list cannot be null.");

            if (string.IsNullOrWhiteSpace(score.MovementTitle))
                throw new ArgumentException("Score MovementTitle cannot be empty.", nameof(score));

            // Insert at index 1, pushing existing items down while leaving scoreList[0] (current) intact
            const int insertIndex = 1;
            if (insertIndex > scoreList.Count)
                scoreList.Add(score);
            else
                scoreList.Insert(insertIndex, score);
        }

        /// <summary>
        /// Ensures score parts exist for the requested part names.
        /// Callers may reuse this helper when parts need to be created before further modification.
        /// </summary>
        public static void EnsurePartsExist(Score score, IEnumerable<string> designPartNames)
        {
            if (score == null) throw new ArgumentNullException(nameof(score));
            if (designPartNames == null) throw new ArgumentNullException(nameof(designPartNames));

            // TODO this is outside scope. Adding parts should be a separate operation.
            if (designPartNames.Count() == 0)
            {
                throw new ArgumentException("At least one part must be selected.", nameof(designPartNames));
            }

            score.Parts ??= new List<Part>();

            // Build a case-insensitive set of existing part names for quick lookup.
            var scorePartNames = new HashSet<string>(
                score.Parts.Where(p => !string.IsNullOrWhiteSpace(p?.Name)).Select(p => p.Name!),
                StringComparer.OrdinalIgnoreCase);

            int count = 0;
            foreach (var partName in designPartNames)
            {
                count++;  // THIS ASSUMES that parts are added in sequence 1,2,3,...and some may not be affected
                          // by this operation
                if (scorePartNames.Contains(partName)) continue;
                var newPart = new Part
                {
                    Id = count.ToString(),
                    Name = partName,
                    InstrumentName = partName,
                    MidiChannel = count,
                    Measures = new List<Measure>()
                };

                score.Parts.Add(newPart);
                scorePartNames.Add(partName);
            }
        }
    }
}