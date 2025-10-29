using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Music.Generate
{
    // Data holder object for GenerateForm user-editable values.
    // - All properties are simple data types (strings, ints, bools, List<string>)
    // - Value types are nullable so Apply/Restore only writes when a property has a value.
    public sealed class GenerationData
    {
        // General / Pattern
        public string? Pattern { get; set; }

        // Parts / scope
        public List<string>? SelectedParts { get; set; }
        public bool? AllPartsChecked { get; set; }
        public bool? AllStaffChecked { get; set; } // corresponds to `checkBox1` in designer (labeled "All")

        // Staff / sections / bars / beats
        public int? Staff { get; set; }
        public string? SectionsText { get; set; }
        public int? StartBar { get; set; }
        public int? EndBar { get; set; }
        public int? StartBeat { get; set; }
        public int? EndBeat { get; set; }

        // Overwrite existing notes
        public bool? OverwriteExisting { get; set; }

        // Pitch options
        public bool? PitchAbsolute { get; set; } // true = Absolute, false = Key-relative
        public string? Step { get; set; } // e.g., "C"
        public string? Accidental { get; set; } // "Natural"/"Sharp"/"Flat"
        public int? OctaveAbsolute { get; set; }
        public int? DegreeKeyRelative { get; set; }
        public int? OctaveKeyRelative { get; set; }

        // Rhythm options
        public string? NoteValue { get; set; } // selected key from cbNoteValue
        public int? Dots { get; set; }
        public bool? TupletEnabled { get; set; }
        public int? TupletCount { get; set; }
        public int? TupletOf { get; set; }
        public bool? TieAcross { get; set; }
        public bool? Fermata { get; set; }
        public int? NumberOfNotes { get; set; }
    }
}