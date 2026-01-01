// AI: purpose=Data container for a concrete groove applied to song bars; used by Generator to map groove to MIDI onsets.
// AI: invariants=Bar indices are 1-based inclusive; InstanceId immutable after construction; lists may be empty and unsorted.
// AI: deps=Consumed by Generator and export/serialization; changing field names/types breaks consumers and persistence.
// AI: perf=Lightweight POCOs; collections initialized to avoid null checks; avoid adding heavy logic here.

namespace Music.Generator
{
    // AI: GrooveEvent stores anchor/tension layers and optional per-bar onsets; callers must validate ranges and normalize lists.
    public sealed class GrooveEvent
    {
        // AI: InstanceId generated in ctor using Guid("N"); treated as read-only identifier for persistence and refs.
        public string InstanceId { get; init; }

        // AI: SourcePresetName optional human label; may be empty when created programmatically.
        public string SourcePresetName { get; set; }

        // AI: StartBar is 1-based; class does not enforce StartBar <= EndBar; callers must ensure validity.
        public int StartBar { get; set; }

        // AI: EndBar inclusive 1-based end; consumers treat interval as [StartBar, EndBar].
        public int EndBar { get; set; }

        // AI: AnchorLayer is primary groove pattern; lists inside may be unsorted/duplicated and must be normalized by callers.
        public GrooveInstanceLayer AnchorLayer { get; set; }

        // AI: TensionLayer reserved for variations; merging rules left to consumers.
        public GrooveInstanceLayer TensionLayer { get; set; }

        // AI: BarOnsets keys are 1-based bar numbers; values may omit bars inside the range to indicate no onsets.
        public Dictionary<int, GrooveBarOnsets> BarOnsets { get; set; }

        // AI: ctor: initializes collections to empty to avoid null checks; preserves InstanceId immutability.
        public GrooveEvent()
        {
            InstanceId = Guid.NewGuid().ToString("N");
            SourcePresetName = string.Empty;
            StartBar = 1;
            EndBar = 1;
            AnchorLayer = new GrooveInstanceLayer();
            TensionLayer = new GrooveInstanceLayer();
            BarOnsets = new Dictionary<int, GrooveBarOnsets>();
        }
    }

    // AI: GrooveInstanceLayer holds per-role onset lists; lists may be empty and are not normalized here.
    public sealed class GrooveInstanceLayer
    {
        // AI: Onset values are domain units (e.g., beats or fractional bar offsets); callers should normalize/sort when needed.
        public List<decimal> KickOnsets { get; set; } = new();
        public List<decimal> SnareOnsets { get; set; } = new();
        public List<decimal> HatOnsets { get; set; } = new();
        public List<decimal> BassOnsets { get; set; } = new();
        public List<decimal> CompOnsets { get; set; } = new();
        public List<decimal> PadsOnsets { get; set; } = new();
    }

    // AI: GrooveBarOnsets: BarNumber is 1-based; lists contain onsets for that bar and may be unsorted/duplicated.
    public sealed class GrooveBarOnsets
    {
        public int BarNumber { get; set; }
        public List<decimal> KickOnsets { get; set; } = new();
        public List<decimal> SnareOnsets { get; set; } = new();
        public List<decimal> HatOnsets { get; set; } = new();
        public List<decimal> BassOnsets { get; set; } = new();
        public List<decimal> CompOnsets { get; set; } = new();
        public List<decimal> PadsOnsets { get; set; } = new();
    }
}