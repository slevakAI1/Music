// AI: purpose=Deterministic RNG wrapper over System.Random for reproducible generation; do not change semantics.
// AI: invariants=Same seed + same call sequence -> identical outputs; NextInt fallback when min>=max must stay.
// AI: deps=Relies on System.Random behavior; changing RNG or seed derivation breaks repeatability/tests.
// AI: thread-safety=Not thread-safe; intended for single-threaded use or one RNG per decision context only.
// AI: perf=Lightweight wrapper; not a hotpath by design; avoid adding allocations or locks here.
// AI: security=no PII logging; seed disclosure leaks determinism and may harm reproducibility/privacy.
// AI: change=Do NOT change seed handling or NextInt/min>=max fallback; tests and consumers rely on these.

namespace Music.Generator
{
    // AI: public API=SeededRandomSource(seed), NextInt(minInclusive,maxExclusive), NextDouble(); preserve behavior
    public sealed class SeededRandomSource
    {
        private readonly Random _rng;

        // AI: ctor: seed is a 32-bit int; changing how seed is derived or mixed breaks repeatability.
        public SeededRandomSource(int seed)
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