namespace Music.Generator
{
    public class Bar
    {
        public int BarNumber;

        public long EndTick;  
        
        public int Numerator;

        public int Denominator;

        // Computed properties
        private int? _ticksPerMeasure;
        private int? _ticksPerBeat;
        private int? _beatsPerBar;

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
        /// Beats per bar - for simple meters this equals the numerator.
        /// For compound meters (e.g., 6/8, 9/8, 12/8), this returns numerator / 3 to represent the compound beat groupings.
        /// </summary>
        public int BeatsPerBar => _beatsPerBar ??= CalculateBeatsPerBar();

        /// <summary>
        /// Absolute time position in ticks from the start of the track.
        /// This is the source of truth for event timing.
        /// </summary>
        public long StartTick { get; init; }

        private int CalculateBeatsPerBar()
        {
            // Detect common compound meters: 6/8, 9/8, 12/8
            // In compound meters, the beat is grouped in threes (dotted quarter = 3 eighths)
            if (Numerator >= 6 && Numerator % 3 == 0 && Denominator == 8)
            {
                return Numerator / 3;
            }

            // Default: simple meter - beats per bar equals numerator
            return Numerator;
        }
    }
}
