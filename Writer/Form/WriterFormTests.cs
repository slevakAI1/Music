using Music.Designer;

namespace Music.Writer
{
    public class WriterFormTests
    {
        // ==================================   T E S T   H E L P E R S   ==================================
  
        public static WriterFormData SetTestWriterG1(Designer.Designer? designer)
        {
            var data = new WriterFormData();

            //===================================================================
            // Voices: select all named voices from the design

            var partNames = new List<string>();
            if (designer?.Voices?.Voices != null)
            {
                foreach (var v in designer.Voices.Voices)
                {
                    var name = v?.VoiceName;
                    if (!string.IsNullOrWhiteSpace(name))
                        partNames.Add(name!);
                }
            }

            // Populate PartsState with all parts checked
            var partsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in partNames)
                partsState[p] = true;

            data.PartsState = partsState;

            //===================================================================
            // Populate SectionsState with all sections from design

            var sectionNames = new List<string>();
            if (designer?.SectionTimeline?.Sections != null)
            {
                foreach (var s in designer.SectionTimeline.Sections)
                {
                    var name = s?.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                        sectionNames.Add(name!);
                }
            }

            // Create SectionsState dictionary; default checked state is false (unchecked)
            var sectionsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var sec in sectionNames)
                sectionsState[sec] = false;

            data.SectionsState = sectionsState;

            //===================================================================

            // Staffs default - select staff 1
            data.SelectedStaffs = new List<int> { 1 };

            // End bar: default to design total bars when available
            var total = designer?.SectionTimeline?.TotalBars ?? 0;
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
