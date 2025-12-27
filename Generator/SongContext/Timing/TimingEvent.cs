namespace Music.Generator
{
    // One time signature event, potentially spanning multiple bars
    public sealed class TimingEvent
    {
        // Placement (1-based bar/beat)
        public int StartBar { get; init; }

        // Time signature numerator/denominator
        public int Numerator { get; init; } = 4;
        public int Denominator { get; init; } = 4;

        // Computed properties
        private int? _ticksPerMeasure;
        private int? _ticksPerBeat;

        /// <summary>
        /// Ticks per measure calculated once and cached.
        /// Formula: ticksPerQuarterNote * (numerator * 4 / denominator)
        /// </summary>
        public int TicksPerMeasure => _ticksPerMeasure ??= (MusicConstants.TicksPerQuarterNote * 4 * Numerator) / Denominator;

        /// <summary>
        /// Ticks per beat (the beat unit defined by the time signature).
        /// Derived from TicksPerMeasure divided by the numerator for consistency.
        /// </summary>
        public int TicksPerBeat => _ticksPerBeat ??= TicksPerMeasure / Numerator;

        /// <summary>
        /// Absolute time position in ticks from the start of the track.
        /// This is the source of truth for event timing.
        /// </summary>
        public long AbsoluteTimeTicks { get; init; }

    }
}
