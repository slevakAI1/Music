// AI: purpose=Singleton Rng manager; Initialize() seeds per-Purpose RNGs for deterministic sequences.
// AI: invariants=Same init seed -> identical sequences per Purpose; call Initialize before use else throws.
// AI: deps=System.Random; Purpose seed derivation from master Rng; changing order breaks reproducibility.
// AI: thread-safety=Not thread-safe; Initialize once at app start; concurrent Next* needs external sync.
// AI: perf=Dict lookup + Rng call; lightweight; not hotpath; no allocs in Next* methods.
// AI: security=no PII logs; seed disclosure leaks all sequences; guard seed if privacy matters.
// AI: change=Do NOT change seed derivation order or Purpose enum order; tests rely on sequence.

namespace Music.Generator
{
    /// <summary>
    /// Purpose enum identifies distinct Rng sequences for different generation contexts.
    /// Story A2: Extended with groove-specific purposes for per-stream-key RNG instances.
    /// </summary>
    public enum RandomPurpose
    {
        DrumGenerator,

        // ===== Groove System Stream Keys (Story A2) =====
        // Each GrooveRngStreamKey gets its own RandomPurpose for independent sequences

        GrooveVariationGroupPick,
        GrooveCandidatePick,
        GrooveTieBreak,
        GroovePrunePick,
        GrooveDensityPick,
        GrooveVelocityJitter,
        GrooveGhostVelocityPick,
        GrooveTimingJitter,
        GrooveSwingJitter,
        GrooveFillPick,
        GrooveAccentPick,
        GrooveGhostNotePick,
        GrooveOrnamentPick,
        GrooveCymbalPick,
        GrooveDynamicsPick
    }

    // AI: public API=Initialize(seed), static NextInt(purpose[,min,max]), NextDouble(purpose); prefer static methods.
    // AI: example=Rng.Initialize(); int n = Rng.NextInt(DrumGenerator); // short syntax, no dict lookup in caller code.
    public sealed class Rng
    {
        private static Dictionary<RandomPurpose, Rng>? _instances;

        /// <summary>
        /// Gets the dictionary of per-purpose Rng instances. Throws if Initialize not called.
        /// </summary>
        public static IReadOnlyDictionary<RandomPurpose, Rng> RandomDictionary
        {
            get
            {
                if (_instances == null)
                    throw new InvalidOperationException("RandomSource.Initialize must be called before accessing RandomDictionary.");
                return _instances;
            }
        }

        /// <summary>
        /// Initializes the Rng dictionary with derived seeds for each Purpose. Call once at app start.
        /// Story A2: Extended with groove-specific RNG instances for per-stream-key determinism.
        /// </summary>
        /// <param name="seed">Master seed for deriving per-Purpose seeds; default 12345.</param>
        public static void Initialize(int seed = 12345)
        {
            Tracer.DebugTrace($"Rng.Initialize: seed={seed}");
            var masterRng = new Random(seed);
            
            _instances = new Dictionary<RandomPurpose, Rng>
            {
                [RandomPurpose.DrumGenerator] = new Rng(masterRng.Next()),

                // Groove system stream keys (Story A2) - each gets independent seed
                [RandomPurpose.GrooveVariationGroupPick] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveCandidatePick] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveTieBreak] = new Rng(masterRng.Next()),
                [RandomPurpose.GroovePrunePick] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveDensityPick] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveVelocityJitter] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveGhostVelocityPick] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveTimingJitter] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveSwingJitter] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveFillPick] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveAccentPick] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveGhostNotePick] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveOrnamentPick] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveCymbalPick] = new Rng(masterRng.Next()),
                [RandomPurpose.GrooveDynamicsPick] = new Rng(masterRng.Next())
            };
        }

        private readonly Random _rng;
        private static int _totalNextIntCalls = 0;
        private static int _totalNextDoubleCalls = 0;

        // AI: ctor: private; only Initialize creates instances; seed derived from master Rng.
        private Rng(int seed)
        {
            _rng = new Random(seed);
        }

        // AI: NextInt: returns [minInclusive, maxExclusive); if min>=max returns minInclusive (fallback).
        public int NextInt(int minInclusive, int maxExclusive)
        {
            _totalNextIntCalls++;
            if (minInclusive >= maxExclusive)
                return minInclusive;
            return _rng.Next(minInclusive, maxExclusive);
        }

        // AI: NextDouble: returns [0.0,1.0); preserve behavior.
        public double NextDouble()
        {
            _totalNextDoubleCalls++;
            return _rng.NextDouble();
        }

        public static void LogRngStats(string label)
        {
            Tracer.DebugTrace($"RNG Stats ({label}): NextInt called {_totalNextIntCalls} times, NextDouble called {_totalNextDoubleCalls} times");
        }

        public static void ResetRngStats()
        {
            _totalNextIntCalls = 0;
            _totalNextDoubleCalls = 0;
        }

        // AI: Static convenience methods for direct access without dictionary lookup.
        // AI: NextInt(purpose, min, max): looks up RNG by purpose and returns int in [min, max).
        public static int NextInt(RandomPurpose purpose, int minInclusive, int maxExclusive)
        {
            return RandomDictionary[purpose].NextInt(minInclusive, maxExclusive);
        }

        // AI: NextInt(purpose): convenience with default range [0, int.MaxValue); for unbounded random int.
        public static int NextInt(RandomPurpose purpose)
        {
            return RandomDictionary[purpose].NextInt(0, int.MaxValue);
        }

        // AI: NextDouble(purpose): looks up RNG by purpose and returns double in [0.0, 1.0).
        public static double NextDouble(RandomPurpose purpose)
        {
            return RandomDictionary[purpose].NextDouble();
        }
    }
}
