// AI: purpose=Builder for OnsetGrid; constructs valid beat position set from subdivision policy.
// AI: deps=AllowedSubdivision flags; returns immutable OnsetGrid.
// AI: invariants=Quarter=1 div/beat, Eighth=2, Sixteenth=4, EighthTriplet=3, SixteenthTriplet=6.

namespace Music.Generator.Groove
{
    /// <summary>
    /// Builder for constructing OnsetGrid instances from subdivision policies.
    /// Story G2 extraction from DrumTrackGenerator.ApplySubdivisionFilter.
    /// </summary>
    public static class OnsetGridBuilder
    {
        /// <summary>
        /// Builds an onset grid from beats per bar and allowed subdivision flags.
        /// Returns a grid with no valid positions if AllowedSubdivision.None is specified.
        /// </summary>
        /// <param name="beatsPerBar">Number of beats per bar (e.g., 4 for 4/4 time)</param>
        /// <param name="allowedSubdivisions">Subdivision flags defining valid grid positions</param>
        /// <returns>Immutable onset grid defining valid beat positions</returns>
        public static OnsetGrid Build(int beatsPerBar, AllowedSubdivision allowedSubdivisions)
        {
            if (beatsPerBar <= 0)
                throw new ArgumentException("BeatsPerBar must be positive", nameof(beatsPerBar));

            var validPositions = new HashSet<double>();

            // If none specified, return empty grid (explicit deny) per GroovePlan.md policy.
            if (allowedSubdivisions == AllowedSubdivision.None)
                return new OnsetGrid(beatsPerBar, allowedSubdivisions, validPositions);

            // Quarter: 1 division per beat (positions 1, 2, 3, 4)
            if (allowedSubdivisions.HasFlag(AllowedSubdivision.Quarter))
                AddPositions(validPositions, beatsPerBar, divisionsPerBeat: 1);

            // Eighth: 2 divisions per beat (positions 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5)
            if (allowedSubdivisions.HasFlag(AllowedSubdivision.Eighth))
                AddPositions(validPositions, beatsPerBar, divisionsPerBeat: 2);

            // Sixteenth: 4 divisions per beat (positions 1, 1.25, 1.5, 1.75, 2, 2.25, ...)
            if (allowedSubdivisions.HasFlag(AllowedSubdivision.Sixteenth))
                AddPositions(validPositions, beatsPerBar, divisionsPerBeat: 4);

            // EighthTriplet: 3 divisions per beat (positions 1, 1.33, 1.67, 2, 2.33, ...)
            if (allowedSubdivisions.HasFlag(AllowedSubdivision.EighthTriplet))
                AddPositions(validPositions, beatsPerBar, divisionsPerBeat: 3);

            // SixteenthTriplet: 6 divisions per beat (finer triplet grid)
            if (allowedSubdivisions.HasFlag(AllowedSubdivision.SixteenthTriplet))
                AddPositions(validPositions, beatsPerBar, divisionsPerBeat: 6);

            return new OnsetGrid(beatsPerBar, allowedSubdivisions, validPositions);
        }

        private static void AddPositions(HashSet<double> validPositions, int beatsPerBar, int divisionsPerBeat)
        {
            if (divisionsPerBeat <= 0) return;

            for (int beat = 1; beat <= beatsPerBar; beat++)
            {
                for (int k = 0; k < divisionsPerBeat; k++)
                {
                    double pos = beat + (double)k / divisionsPerBeat;
                    // Only include positions within this bar
                    if (pos < beatsPerBar + 1 - 1e-9)
                        validPositions.Add(Math.Round(pos, 6));
                }
            }
        }
    }
}
