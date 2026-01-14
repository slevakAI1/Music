// AI: purpose=Test helpers to populate WriterFormData from a SongContext for UI/tests.
// AI: invariants=Produced WriterFormData uses designer values; PartsState/SectionsState keys are case-insensitive; defaults stable across tests.
// AI: deps=Relies on SongContext.Voices and SectionTrack.Sections; changing those shapes breaks tests.
// AI: change=If adding new WriterFormData fields update SetTestWriterG1 and WriterFormTransform.

using Music.Generator;

namespace Music.Writer
{
    public class WriterFormTests
    {
        // AI: SetTestWriterG1: builds a WriterFormData from a designer context; selects all voices and enumerates sections.
        // AI: behavior=Leaves SectionsState unchecked by default; selects staff 1 and defaults common rhythm/pitch controls.
        public static WriterFormData SetTestWriterG1(SongContext? designer)
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
            if (designer?.SectionTrack?.Sections != null)
            {
                foreach (var s in designer.SectionTrack.Sections)
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
            var total = designer?.SectionTrack?.TotalBars ?? 0;
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
