// AI: THIS FILE IS DEPRECATED. Use Rng in Music.Generator.Core.Randomization instead. This class kept for backwards compatibility.
// AI: purpose=Pluggable Rng interface to allow deterministic seeds and replaceable generators for music logic.
// AI: invariants=Deterministic for same seed; implementations must preserve inclusive/exclusive semantics; do not mutate global state.
// AI: deps=Used by music generation for probabilistic choices; no I/O; contract changes require updating callers/tests.
// AI: perf=Called frequently in generation; implementations should be low-allocation and fast.

namespace Music.Generator
{
    // AI: contract=NextInt returns int in [minInclusive,maxExclusive); NextDouble returns double in [0,1).
    // AI: errors=Implementations may throw for invalid ranges (min>=max); callers should validate inputs.
    // AI: thread=No built-in thread-safety guarantee; synchronize if same instance is shared across threads.
    public interface IRandomSourceOld
    {
        int NextInt(int minInclusive, int maxExclusive);

        double NextDouble();
    }
}