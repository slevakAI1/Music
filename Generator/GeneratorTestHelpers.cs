using Music.Designer;
using Music.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music.Generator
{
    public class GeneratorTestHelpers
    {
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

            // Staffs default - select staff 1
            data.SelectedStaffs = new List<int> { 1 };

            // End bar: default to design total bars when available
            var total = design?.SectionSet?.TotalBars ?? 0;
            if (total > 0)
                data.EndBar = total;
            else
                data.EndBar = null;

            // Other control defaults (mirror previous behavior)
            data.NumberOfNotes = 4;
            data.PitchAbsolute = true;
            data.Step = 'C';             // Now char type instead of string
            data.Accidental = "Natural"; // matches cbAccidental default index 0
            data.Pattern = "Set Note";   // matches cbPattern which contained "Set Note"

            // Set default note value (matches designer-loaded cbNoteValue items)
            data.NoteValue = "Quarter (1/4)";

            // Staff default
            //data.Staff = 1;??

            return data;
        }
    }
}
