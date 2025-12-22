namespace Music.Generator
{
    /// <summary>
    /// A concrete groove instance generated for a specific section of the song.
    /// Combines base preset with variations and tension layer sampling.
    /// </summary>
    public sealed class GrooveInstance
    {
        /// <summary>
        /// Unique identifier for this groove instance.
        /// </summary>
        public string InstanceId { get; init; }

        /// <summary>
        /// Name of the source preset.
        /// </summary>
        public string SourcePresetName { get; set; }

        /// <summary>
        /// Bar range where this groove instance applies.
        /// </summary>
        public int StartBar { get; set; }
        public int EndBar { get; set; }

        /// <summary>
        /// Anchor layer onsets per role (the stable backbone).
        /// </summary>
        public GrooveInstanceLayer AnchorLayer { get; set; }

        /// <summary>
        /// For future use. Empty for now!
        /// Tension layer onsets per role (sampled variations).
        /// </summary>
        public GrooveInstanceLayer TensionLayer { get; set; }

        /// <summary>
        /// Combined onsets per bar for each role.
        /// Key: bar number (1-based), Value: onsets for that bar.
        /// </summary>
        public Dictionary<int, GrooveBarOnsets> BarOnsets { get; set; }

        public GrooveInstance()
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

    /// <summary>
    /// Onset data for a single layer of a groove instance.
    /// </summary>
    public sealed class GrooveInstanceLayer
    {
        public List<decimal> KickOnsets { get; set; } = new();
        public List<decimal> SnareOnsets { get; set; } = new();
        public List<decimal> HatOnsets { get; set; } = new();
        public List<decimal> BassOnsets { get; set; } = new();
        public List<decimal> CompOnsets { get; set; } = new();
        public List<decimal> PadsOnsets { get; set; } = new();
    }

    /// <summary>
    /// All onsets for a specific bar, organized by role.
    /// </summary>
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