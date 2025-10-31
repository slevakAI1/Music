using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Music.Generate
{
    // Data holder object for GenerateForm user-editable values.
    // - All properties are simple data types (strings, ints, bools, Dictionary<string,bool>)
    // - Value types remain nullable for compatibility, but getters now provide sensible defaults
    //   so callers can rely on the property value without repeating the same fallback logic.
    public sealed class GeneratorData
    {
        // Backing fields
        private Dictionary<string, bool>? _partsState;
        private int? _staff;
        private int? _startBar;
        private int? _endBar;
        private int? _startBeat;
        private int? _endBeat;
        private bool? _overwriteExisting;
        private bool? _pitchAbsolute;
        private string? _step;
        private string? _accidental;
        private int? _octaveAbsolute;
        private int? _degreeKeyRelative;
        private int? _octaveKeyRelative;
        private string? _noteValue;
        private int? _dots;
        private bool? _tupletEnabled;
        private int? _tupletCount;
        private int? _tupletOf;
        private bool? _tieAcross;
        private bool? _fermata;
        private int? _numberOfNotes;
        private string? _pattern;
        private bool? _allPartsChecked;
        private bool? _allStaffChecked;
        private string? _sectionsText;

        // General / Pattern
        public string? Pattern
        {
            get => _pattern;
            set => _pattern = value;
        }

        // Parts / scope
        // New: map of part name to checked state. Getter returns an empty dictionary when not set.
        public Dictionary<string, bool>? PartsState
        {
            get => _partsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            set => _partsState = value;
        }

        public bool? AllPartsChecked
        {
            get => _allPartsChecked;
            set => _allPartsChecked = value;
        }

        public bool? AllStaffChecked
        {
            get => _allStaffChecked;
            set => _allStaffChecked = value;
        } // corresponds to `checkBox1` in designer (labeled "All")

        // Staff / sections / bars / beats
        // Provide sensible defaults in getters so callers need not repeat fallback code.
        public int? Staff
        {
            get => _staff ?? 1;
            set => _staff = value;
        }

        public string? SectionsText
        {
            get => _sectionsText;
            set => _sectionsText = value;
        }

        public int? StartBar
        {
            get => _startBar ?? 1;
            set => _startBar = value;
        }

        public int? EndBar
        {
            // If EndBar not specified, default to StartBar (which itself defaults to 1)
            get => _endBar ?? StartBar;
            set => _endBar = value;
        }

        public int? StartBeat
        {
            get => _startBeat ?? 1;
            set => _startBeat = value;
        }

        public int? EndBeat
        {
            get => _endBeat ?? StartBeat;
            set => _endBeat = value;
        }

        // Overwrite existing notes
        public bool? OverwriteExisting
        {
            get => _overwriteExisting;
            set => _overwriteExisting = value;
        }

        // Pitch options
        public bool? PitchAbsolute
        {
            get => _pitchAbsolute;
            set => _pitchAbsolute = value;
        } // true = Absolute, false = Key-relative

        public string? Step
        {
            get => _step;
            set => _step = value;
        } // e.g., "C"

        // Always return a non-null accidental string for consumers (default "Natural").
        public string? Accidental
        {
            get => _accidental ?? "Natural";
            set => _accidental = value;
        } // "Natural"/"Sharp"/"Flat"

        public int? OctaveAbsolute
        {
            get => _octaveAbsolute;
            set => _octaveAbsolute = value;
        }

        public int? DegreeKeyRelative
        {
            get => _degreeKeyRelative;
            set => _degreeKeyRelative = value;
        }

        public int? OctaveKeyRelative
        {
            get => _octaveKeyRelative;
            set => _octaveKeyRelative = value;
        }

        // Derived effective octave (combines absolute/key-relative fallbacks).
        // Consumers that previously did: data.OctaveAbsolute ?? data.OctaveKeyRelative ?? 4
        // can now use this property to get the resolved octave.
        public int Octave
        {
            get => _octaveAbsolute ?? _octaveKeyRelative ?? 4;
        }

        // Rhythm options
        public string? NoteValue
        {
            get => _noteValue;
            set => _noteValue = value;
        } // selected key from cbNoteValue

        public int? Dots
        {
            get => _dots ?? 0;
            set => _dots = value;
        }

        public bool? TupletEnabled
        {
            get => _tupletEnabled;
            set => _tupletEnabled = value;
        }

        public int? TupletCount
        {
            get => _tupletCount ?? 0;
            set => _tupletCount = value;
        }

        public int? TupletOf
        {
            get => _tupletOf ?? 0;
            set => _tupletOf = value;
        }

        public bool? TieAcross
        {
            get => _tieAcross;
            set => _tieAcross = value;
        }

        public bool? Fermata
        {
            get => _fermata;
            set => _fermata = value;
        }

        // Always provide a default of 1 note when not explicitly set.
        public int? NumberOfNotes
        {
            get => _numberOfNotes ?? 1;
            set => _numberOfNotes = value;
        }
    }
}