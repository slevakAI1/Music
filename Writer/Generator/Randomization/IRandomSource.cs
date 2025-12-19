namespace Music.Writer.Generator.Randomization
{
    /// <summary>
    /// Abstraction for random number generation to enable deterministic testing.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>
        /// Returns a random integer in [minInclusive, maxExclusive).
        /// </summary>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>
        /// Returns a random double in [0, 1).
        /// </summary>
        double NextDouble();
    }
}