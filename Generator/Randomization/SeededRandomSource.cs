namespace Music.Writer.Generator.Randomization
{
    /// <summary>
    /// Deterministic random source using a fixed seed.
    /// Same seed + same calls => identical output.
    /// </summary>
    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly Random _rng;

        public SeededRandomSource(int seed)
        {
            _rng = new Random(seed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
                return minInclusive;
            return _rng.Next(minInclusive, maxExclusive);
        }

        public double NextDouble() => _rng.NextDouble();
    }
}