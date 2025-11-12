using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Music.Writer
{
    // Data holder object for WriterForm user-editable values.
    // - All properties are simple data types (strings, ints, bools, Dictionary<string,bool>)
    // - Value types remain nullable for compatibility, but getters now provide sensible defaults
    //   so callers can rely on the property value without repeating the same fallback logic.
    public sealed class WriterData
    {
        // Backing fields
        private Dictionary<string, bool>? _partsState;
        private Dictionary<string, bool>? _sectionsState;
        private List<int>? _selectedStaffs; // Changed from Dictionary<int, bool>? _staffsState
        private int? _startBar;
        private int? _endBar;
        private int? _startBeat;
        private bool? _overwriteExisting;
        private bool? _pitchAbsolute;
        private char _step;
        private string? _accidental;
        private int? _octaveAbsolute;
        private int? _degreeKeyRelative;
        private int? _octaveKeyRelative;
        private string? _noteValue;
        private int? _dots;
        private string? _tupletNumber;
        private int? _tupletCount;
        private int? _tupletOf;
        private bool? _tieAcross;
        private bool? _fermata;
        private int? _numberOfNotes;
        private string? _pattern;
        private string? _sectionsText;
        private bool? _isRest;
        private bool? _isChord; // new backing field
        private string? _chordKey;
        private int? _chordDegree;
        private string? _chordQuality;
        private string? _chordBase;

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

        // Sections / scope
        // New: map of section name to checked state. Getter returns an empty dictionary when not set.
        public Dictionary<string, bool>? SectionsState
        {
            get => _sectionsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            set => _sectionsState = value;
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

        // Changed from char? to char. Default to '\0' when IsRest is true.
        public char Step
        {
            get => _step;
            set => _step = value;
        }

        // Single added field: track IsRest radiobutton state
        public bool? IsRest
        {
            get => _isRest;
            set => _isRest = value;
        }

        // New: track whether Chord radio button is selected
        public bool? IsChord
        {
            get => _isChord;
            set => _isChord = value;
        }

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

        public string? TupletNumber
        {
            get => _tupletNumber;
            set => _tupletNumber = value;
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

        // Chord options
        public string? ChordKey
        {
            get => _chordKey;
            set => _chordKey = value;
        }

        public int? ChordDegree
        {
            get => _chordDegree ?? 1;
            set => _chordDegree = value;
        }

        public string? ChordQuality
        {
            get => _chordQuality;
            set => _chordQuality = value;
        }

        public string? ChordBase
        {
            get => _chordBase;
            set => _chordBase = value;
        }

        // Staff selection - list of selected staff numbers
        public List<int>? SelectedStaffs
        {
            get => _selectedStaffs ?? new List<int>();
            set => _selectedStaffs = value;
        }
    }
}