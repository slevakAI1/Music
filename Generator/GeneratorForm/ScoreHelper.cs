using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Music.Designer;
using MusicXml.Domain;

namespace Music.Generator
{
    internal static class ScoreHelper
    {
        public static Score? NewScore(
            Form owner,
            Designer.Designer? design,
            CheckedListBox cbPart,
            Label lblEndBarTotal)
        {
            // Create a fresh Score instance and assign to the local cache (returned to caller)
            var score = new Score
            {
                MovementTitle = string.Empty,
                Identification = new Identification(),
                Parts = new List<Part>()
            };

            // Prefer the passed design, fall back to Globals if missing
            var usedDesign = design ?? Globals.Designer;
            if (usedDesign == null)
            {
                MessageBox.Show(owner, "No design available. Create or set a design before creating a new score.", "No Design", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show(owner, "Design contains no voices to create parts from.", "No Parts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            // Ensure parts exist in the new score (this will add Part entries for each name)
            try
            {
                ScorePartsHelper.EnsurePartsExist(score, partNames);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, $"Error creating parts: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    first.Attributes.Divisions = 4;
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

            MessageBox.Show(owner, "New score created from design.", "New Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return score;
        }
    }
}