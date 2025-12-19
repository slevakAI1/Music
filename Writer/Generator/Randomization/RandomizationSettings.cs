namespace Music.Writer.Generator.Randomization
{
    /// <summary>
    /// Configuration for controlled randomness in pitch generation.
    /// All probabilities/weights have safe defaults for minimal variation.
    /// </summary>
    public sealed class RandomizationSettings
    {
        /// <summary>
        /// Master seed for deterministic generation.
        /// Same seed + same inputs => identical output.
        /// </summary>
        public int Seed { get; init; } = 12345;

        // ========== BASS SETTINGS ==========

        /// <summary>Weight for choosing root note in bass (0.0–1.0).</summary>
        public double BassRootWeight { get; init; } = 0.75;

        /// <summary>Weight for choosing fifth in bass (0.0–1.0).</summary>
        public double BassFifthWeight { get; init; } = 0.20;

        /// <summary>Weight for choosing octave (root in higher register) in bass (0.0–1.0).</summary>
        public double BassOctaveWeight { get; init; } = 0.05;

        // ========== GUITAR SETTINGS ==========

        /// <summary>
        /// Probability of using a diatonic passing tone on weak beats (0.0–1.0).
        /// Only applies when previous pitch is available.
        /// </summary>
        public double GuitarPassingToneProbability { get; init; } = 0.20;

        // ========== KEYS/PADS SETTINGS ==========

        /// <summary>
        /// Probability of adding a diatonic 9th to chord voicing (0.0–1.0).
        /// Only applied on first onset of a harmony event.
        /// </summary>
        public double KeysAdd9Probability { get; init; } = 0.10;

        /// <summary>
        /// Creates settings with all defaults (minimal randomness).
        /// </summary>
        public static RandomizationSettings Default => new();
    }
}