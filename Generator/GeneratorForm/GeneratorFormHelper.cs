using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Music.Design;
using MusicXml.Domain;

namespace Music.Generate
{
    internal static class GeneratorFormHelper
    {
        // New: merge design-driven defaults into an existing GeneratorData instance.
        // This does not blindly overwrite unrelated persisted fields — it seeds or clamps
        // only the values the design is authoritative for (available part names, end bar, and related flags).
        public static void UpdateGeneratorDataFromDesignData(GeneratorData data, DesignerData? design)
        {
            if (data == null) return;

            // Build set of available part names from the design
            var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (design?.PartSet?.Parts != null)
            {
                foreach (var v in design.PartSet.Parts)
                {
                    var name = v?.PartName;
                    if (!string.IsNullOrWhiteSpace(name))
                        available.Add(name!);
                }
            }

            // Merge existing PartsState with available parts.
            var existing = data.PartsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var newState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (existing.Count == 0)
            {
                // No persisted state: default to all available parts checked
                foreach (var name in available)
                {
                    newState[name] = true;
                }
            }
            else
            {
                // Preserve checked state for parts that still exist; do not carry over missing parts.
                foreach (var name in available)
                {
                    if (existing.TryGetValue(name, out var isChecked))
                        newState[name] = isChecked;
                    else
                        newState[name] = false; // new part added to design defaults to unchecked
                }
            }

            data.PartsState = newState;

            // If the AllPartsChecked flag wasn't set previously, default to true when there are available parts.
            if (!data.AllPartsChecked.HasValue)
                data.AllPartsChecked = available.Count > 0;

            // Preserve any explicit AllStaffChecked setting; if unset keep the previous default behavior of true.
            if (!data.AllStaffChecked.HasValue)
                data.AllStaffChecked = true;

            // End bar: if design has a total, clamp or seed the DTO's EndBar to it.
            var total = design?.SectionSet?.TotalBars ?? 0;
            if (total > 0)
            {
                if (!data.EndBar.HasValue)
                    data.EndBar = total;
                else
                    data.EndBar = Math.Max(1, Math.Min(total, data.EndBar.Value));
            }
        }


        // ==================================   T E S T   H E L P E R S   ==================================

        // NOTE: This helper now builds and returns GenerationData instead of manipulating controls.
        // The caller (form) should apply the returned GenerationData to controls via ApplyFormData(...)
        public static GeneratorData SetTestGeneratorG1(DesignerData? design)
        {
            var data = new GeneratorData();

            // Parts: select all named voices from the design
            var partNames = new List<string>();
            if (design?.PartSet?.Parts != null)
            {
                foreach (var v in design.PartSet.Parts)
                {
                    var name = v?.PartName;
                    if (!string.IsNullOrWhiteSpace(name))
                        partNames.Add(name!);
                }
            }

            // Populate PartsState with all parts checked
            var partsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in partNames)
                partsState[p] = true;

            data.PartsState = partsState;
            data.AllPartsChecked = partNames.Count > 0;
            data.AllStaffChecked = true; // keep previous behavior of checking "All staff" by default

            // End bar: default to design total bars when available
            var total = design?.SectionSet?.TotalBars ?? 0;
            if (total > 0)
                data.EndBar = total;
            else
                data.EndBar = null;

            // Other control defaults (mirror previous behavior)
            data.NumberOfNotes = 4;
            data.PitchAbsolute = true;
            data.Step = "C";             // matches cbStep default index 0
            data.Accidental = "Natural"; // matches cbAccidental default index 0
            data.Pattern = "Set Note";   // matches cbPattern which contained "Set Note"

            // Set default note value (matches designer-loaded cbNoteValue items)
            data.NoteValue = "Quarter (1/4)";

            // Staff default
            data.Staff = 1;

            return data;
        }

        // ==================================   S C O R E   H E L P E R S   ==================================

        public static Score? NewScore(
            Form owner,
            DesignerData?
            design,
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
            var usedDesign = design ?? Globals.Design;
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

            // -- New: populate attributes for measures
            // Set a full attributes snapshot on the first measure (divisions, key, time, clef).
            // For every subsequent measure in the part set only an Attributes with Divisions is created
            // so the serialized MusicXML shows <attributes><divisions>...</divisions></attributes> per measure.
            if (score.Parts != null)
            {
                foreach (var p in score.Parts)
                {
                    if (p.Measures == null || p.Measures.Count == 0) continue;

                    // Ensure first measure has full attributes (4/4, divisions=4)
                    var first = p.Measures[0];
                    first.Attributes ??= new MeasureAttributes();
                    first.Attributes.Divisions = 4;
                    first.Attributes.Key ??= new Key { Fifths = 0, Mode = "major" };
                    first.Attributes.Time ??= new Time();
                    first.Attributes.Time.Beats = 4;
                    first.Attributes.Time.Mode = "4";
                    first.Attributes.Clef ??= new Clef { Sign = "G", Line = 2 };

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