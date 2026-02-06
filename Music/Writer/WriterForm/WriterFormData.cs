// AI: purpose=Form DTO for writer UI; provides stable defaults so callers avoid null/fallback logic.
// AI: invariants=Getters return usable defaults; property renames or default changes break UI/serialization.
// AI: deps=Consumers: command handlers, WriterFormTransform, serializers; keep shape and default semantics stable.

namespace Music.Writer
{
    // AI: purpose=Lightweight form-bound data holder; no business logic or side-effects here.
    // AI: invariants=Backing fields nullable for serialization; getters must supply pragmatic defaults.
    public sealed class WriterFormData
    {
        private string? _pattern;
        private string? _sectionsText;

        private Dictionary<string, bool>? _partsState;
        private Dictionary<string, bool>? _sectionsState;
        private List<int>? _selectedStaffs;
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
        private bool? _isChord;
        private string? _chordKey;
        private int? _chordDegree;
        private string? _chordQuality;
        private string? _chordBase;

        private string? _noteValue;
        private int _dots;
        private string? _tupletNumber;
        private int? _tupletCount;
        private int? _tupletOf;
        private bool? _tieAcross;
        private bool? _fermata;
        private int? _numberOfNotes;

        // General / ProposedPattern
        public string? Pattern
        {
            get => _pattern;
            set => _pattern = value;
        }

        // Voices / scope
        // AI: parts=case-insensitive map; getter returns non-null empty map for callers; returned map is mutable.
        public Dictionary<string, bool>? PartsState
        {
            get => _partsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            set => _partsState = value;
        }

        // Sections / scope
        // AI: sections=case-insensitive map; getter returns non-null empty map for callers.
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

        public char Step
        {
            get => _step;
            set => _step = value;
        }

        // New: track whether Chord radio button is selected
        public bool? IsChord
        {
            get => _isChord;
            set => _isChord = value;
        }

        // AI: accidental=default "Natural" to avoid null checks; values: "Natural"/"Sharp"/"Flat".
        public string? Accidental
        {
            get => _accidental ?? "Natural";
            set => _accidental = value;
        }

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

        // AI: octave=resolve absolute then key-relative then 4; default 4 is stable and relied upon by callers.
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

        public int Dots
        {
            get => _dots;
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

        public int? NumberOfNotes
        {
            // AI: default=1 to avoid null checks by callers
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

        // AI: selectedStaffs=list of selected staff numbers; getter returns non-null empty list and is mutable.
        public List<int>? SelectedStaffs
        {
            get => _selectedStaffs ?? new List<int>();
            set => _selectedStaffs = value;
        }
    }
}
