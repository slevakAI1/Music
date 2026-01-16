// AI: THIS FILE IS DEPRECATED. Use Rng in Music.Generator.Core.Randomization instead. This class kept for backwards compatibility.
// AI: purpose=Deterministic IRandomSource wrapper over System.Random used for per-decision reproducible Rng.
// AI: invariants=Same seed + same sequence of calls => identical outputs; callers expect determinism for tests.
// AI: deps=Relies on System.Random implementation; changing Rng implementation or seed semantics breaks determinism.
// AI: thread-safety=Not thread-safe; intended for single-threaded or per-decision instances (one Rng per context).
// AI: behavior=NextInt returns minInclusive when minInclusive>=maxExclusive as a deliberate fallback; preserve it.

namespace Music.Generator
{
    public sealed class SeededRandomSourceOld : IRandomSourceOld
    {
        private readonly Random _rng;

        // AI: ctor: seed is a 32-bit int; changing how seed is derived or mixed breaks repeatability.
        public SeededRandomSourceOld(int seed)
        {
            _rng = new Random(seed);
        }

        // AI: NextInt: returns values in [minInclusive, maxExclusive). If min>=max, returns minInclusive (fallback).
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
                return minInclusive;
            return _rng.Next(minInclusive, maxExclusive);
        }

        // AI: NextDouble: returns double in [0.0,1.0); keep behavior identical.
        public double NextDouble() => _rng.NextDouble();
    }
}
