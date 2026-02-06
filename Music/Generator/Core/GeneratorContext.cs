// AI: purpose=Shared generator context for operators; immutable to ensure deterministic decisions.
// AI: invariants=Seed and RngStreamKey are required; keep RngStreamKey format stable for RNG isolation.
// AI: deps=Rng system for stream keys; extend via derived contexts for instruments when needed.
namespace Music.Generator.Core
{
    // AI: contract=Holds Seed and RngStreamKey; do not rename properties (persisted/serialized expectations)
    public record GeneratorContext
    {
        // AI: invariant=Master seed for deterministic generation; required
        public required int Seed { get; init; }

        // AI: invariant=RNG stream key used to isolate RNG streams; required, keep format stable
        public required string RngStreamKey { get; init; }

        // AI: helper=Create a minimal test context; preserves RngStreamKey format "Test_{barNumber}"
        public static GeneratorContext CreateMinimal(
            int barNumber = 1,
            int seed = 42)
        {
            return new GeneratorContext
            {
                Seed = seed,
                RngStreamKey = $"Test_{barNumber}"
            };
        }
    }
}
