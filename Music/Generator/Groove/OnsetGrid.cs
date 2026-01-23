// AI: purpose=Reusable onset grid for validating beat positions against subdivision policy; generator-agnostic.
// AI: deps=AllowedSubdivision from groove model; used by all generators (drums, comp, melody, motifs).
// AI: invariants=Epsilon comparison for recurring fractions (1/3, 1/6); 1-based beat positions.

namespace Music.Generator.Groove
{
    /// <summary>
    /// Represents an onset grid defining valid beat positions based on subdivision policy.
    /// Immutable after construction. Used across all generators for consistent rhythm quantization.
    /// Story G2 extraction from DrumTrackGenerator.ApplySubdivisionFilter.
    /// </summary>
    public sealed class OnsetGrid
    {
        private const double Epsilon = 0.002; // Tolerance for recurring fractions (1/3, 1/6)
        private readonly HashSet<double> _validPositions;
        private readonly int _beatsPerBar;

        /// <summary>
        /// Gets the number of beats per bar this grid was built for.
        /// </summary>
        public int BeatsPerBar => _beatsPerBar;

        /// <summary>
        /// Gets the subdivision flags this grid was built from.
        /// </summary>
        public AllowedSubdivision AllowedSubdivisions { get; }

        /// <summary>
        /// Gets a read-only collection of valid beat positions within a single bar.
        /// Positions are 1-based (beat 1 is the downbeat).
        /// </summary>
        public IReadOnlySet<double> ValidPositions => _validPositions;

        internal OnsetGrid(int beatsPerBar, AllowedSubdivision allowedSubdivisions, HashSet<double> validPositions)
        {
            if (beatsPerBar <= 0)
                throw new ArgumentException("BeatsPerBar must be positive", nameof(beatsPerBar));
            if (validPositions == null)
                throw new ArgumentNullException(nameof(validPositions));

            _beatsPerBar = beatsPerBar;
            AllowedSubdivisions = allowedSubdivisions;
            _validPositions = validPositions;
        }

        /// <summary>
        /// Checks if the specified beat position is allowed by this grid.
        /// Uses epsilon comparison to handle recurring fractions (triplets).
        /// </summary>
        /// <param name="beat">1-based beat position within bar (e.g., 1.0, 1.5, 2.33)</param>
        /// <returns>True if the beat position is valid on this grid</returns>
        public bool IsAllowed(decimal beat)
        {
            double beatVal = (double)beat;
            return _validPositions.Any(p => Math.Abs(p - beatVal) <= Epsilon);
        }

        /// <summary>
        /// Finds the nearest valid grid position to the specified beat.
        /// Returns null if grid is empty.
        /// </summary>
        /// <param name="beat">1-based beat position</param>
        /// <returns>Nearest valid beat position, or null if grid is empty</returns>
        public decimal? SnapToGrid(decimal beat)
        {
            if (_validPositions.Count == 0)
                return null;

            double beatVal = (double)beat;
            double nearest = _validPositions.MinBy(p => Math.Abs(p - beatVal));
            return (decimal)nearest;
        }
    }
}
