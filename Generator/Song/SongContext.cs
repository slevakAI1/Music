namespace Music.Generator
{
    /// <summary>
    /// Context object passed through the generation pipeline.
    /// Contains all the state needed for generators and critics to operate.
    /// </summary>
    public sealed class SongContext
    {
        /// <summary>
        /// The song being generated.
        /// </summary>
        public Song Song { get; set; }

        /// <summary>
        /// Pattern library for the current generation.
        /// </summary>
        public PatternLibrary PatternLibrary { get; set; }

        /// <summary>
        /// Active groove instances by bar range.
        /// </summary>
        public List<GrooveInstance> GrooveInstances { get; set; }

        /// <summary>
        /// Variation settings controlling surprise/randomness.
        /// </summary>
        public VariationSettings VariationSettings { get; set; }

        /// <summary>
        /// Current bar being processed.
        /// </summary>
        public int CurrentBar { get; set; }

        /// <summary>
        /// Current section being processed.
        /// </summary>
        public SongSection? CurrentSection { get; set; }

        /// <summary>
        /// Current harmony event at the processing position.
        /// </summary>
        public SongHarmonyEvent? CurrentHarmony { get; set; }

        /// <summary>
        /// Ticks per quarter note.
        /// </summary>
        public int TicksPerQuarterNote { get; set; }

        /// <summary>
        /// Current beats per bar (from time signature).
        /// </summary>
        public int BeatsPerBar { get; set; }

        public SongContext()
        {
            Song = new Song();
            PatternLibrary = new PatternLibrary();
            GrooveInstances = new List<GrooveInstance>();
            VariationSettings = new VariationSettings();
            TicksPerQuarterNote = 480;
            BeatsPerBar = 4;
            CurrentBar = 1;
        }
    }

    /// <summary>
    /// Settings controlling variation/surprise during generation.
    /// </summary>
    public sealed class VariationSettings
    {
        /// <summary>
        /// Base variation key for deterministic generation.
        /// </summary>
        public int VariationKey { get; set; }

        /// <summary>
        /// Overall surprise budget (0.0-1.0).
        /// </summary>
        public double SurpriseBudget { get; set; }

        /// <summary>
        /// Bass variation weights.
        /// </summary>
        public BassVariationWeights BassWeights { get; set; }

        /// <summary>
        /// Guitar/Comp variation settings.
        /// </summary>
        public CompVariationSettings CompSettings { get; set; }

        /// <summary>
        /// Keys/Pads variation settings.
        /// </summary>
        public KeysVariationSettings KeysSettings { get; set; }

        public VariationSettings()
        {
            VariationKey = 12345;
            SurpriseBudget = 0.2;
            BassWeights = new BassVariationWeights();
            CompSettings = new CompVariationSettings();
            KeysSettings = new KeysVariationSettings();
        }
    }

    /// <summary>
    /// Weights for bass pitch selection.
    /// </summary>
    public sealed class BassVariationWeights
    {
        public double RootWeight { get; set; } = 0.75;
        public double FifthWeight { get; set; } = 0.20;
        public double OctaveWeight { get; set; } = 0.05;
    }

    /// <summary>
    /// Settings for guitar/comp variation.
    /// </summary>
    public sealed class CompVariationSettings
    {
        public double PassingToneProbability { get; set; } = 0.20;
    }

    /// <summary>
    /// Settings for keys/pads variation.
    /// </summary>
    public sealed class KeysVariationSettings
    {
        public double Add9Probability { get; set; } = 0.10;
        public bool AllowInversionVariation { get; set; } = true;
    }
}