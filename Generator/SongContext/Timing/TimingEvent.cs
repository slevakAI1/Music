namespace Music.Generator
{
    // One time signature event, potentially spanning multiple bars
    public sealed class TimingEvent
    {
        // Placement (1-based bar/beat)
        public int StartBar { get; init; }
        public int StartBeat { get; init; } = 1;

        // Time signature numerator/denominator
        public int Numerator { get; init; } = 4;
        public int Denominator { get; init; } = 4;

        // Computed properties
        private int? _ticksPerMeasure;

        /// <summary>
        /// Ticks per measure calculated once and cached.
        /// Formula: ticksPerQuarterNote * (numerator * 4 / denominator)
        /// </summary>
        public int TicksPerMeasure => _ticksPerMeasure ??= (MusicConstants.TicksPerQuarterNote * 4 * Numerator) / Denominator;
    }
}
