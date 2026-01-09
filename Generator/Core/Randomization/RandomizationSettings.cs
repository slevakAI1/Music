// AI: purpose=Config for controlled randomness used by PitchRandomizer/RandomHelpers; affects generated MIDI output deterministically.
// AI: invariants=weights/probabilities expected in [0,1]; callers assume immutability and stable Seed behavior.
// AI: deps=consumed by PitchRandomizer and RandomHelpers; renaming or altering fields breaks determinism and tests.
// AI: perf=read-mostly config; avoid runtime validation that changes allocation or behavior.

namespace Music.Generator
{
    // AI: class=immutable init-only settings; changing defaults will materially change generated music across repo.
    public sealed class RandomizationSettings
    {
        // AI: Seed: master seed for deterministic generation; same seed+inputs => identical output across runs.
        public int Seed { get; init; } = 129345;

        // ========== BASS SETTINGS ==========

        // AI: BassRootWeight expected 0..1; higher prefers root; weights are consumed by WeightedChoice.
        public double BassRootWeight { get; init; } = 0.75;

        // AI: BassFifthWeight expected 0..1; represents preference for fifth on bass choices.
        public double BassFifthWeight { get; init; } = 0.20;

        // AI: BassOctaveWeight expected 0..1; selects root an octave higher; small by default.
        public double BassOctaveWeight { get; init; } = 0.05;

        // ========== GUITAR SETTINGS ==========

        // AI: GuitarPassingToneProbability expected 0..1; applies only on weak beats when previous pitch exists.
        public double GuitarPassingToneProbability { get; init; } = 0.20;

        // ========== KEYS/PADS SETTINGS ==========

        // AI: KeysAdd9Probability expected 0..1; only considered on first onset of a harmony event.
        public double KeysAdd9Probability { get; init; } = 0.10;

        // AI: Default returns a minimal-randomness settings instance; callers expect stable defaults.
        public static RandomizationSettings Default => new();
    }
}