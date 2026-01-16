// AI: purpose=Tests for Story 6.2 drum timing and velocity humanization; verifies determinism and correctness.
// AI: invariants=All tests must produce identical output for same seed; timing/velocity must respect constraints.
// AI: deps=DrumMicroTimingEngine, DrumVelocityShaper, RandomHelpers.

namespace Music.Generator.Tests
{
    /// <summary>
    /// Tests for Story 6.2: Timing & Dynamics Humanization Layer.
    /// Verifies deterministic behavior and correct implementation of acceptance criteria.
    /// </summary>
    internal static class DrumHumanizationTests
    {
        /// <summary>
        /// Verifies that micro-timing offsets are deterministic for the same seed.
        /// </summary>
        public static void TestMicroTimingDeterminism()
        {
            const int seed = 12345;
            const string grooveStyle = "rock";
            const int bar = 1;
            const decimal onset = 1.0m;
            const bool isStrongBeat = true;

            // Generate timing offset twice with same parameters
            int offset1 = DrumMicroTimingEngine.GetTimingOffset("kick", grooveStyle, bar, onset, seed, isStrongBeat);
            int offset2 = DrumMicroTimingEngine.GetTimingOffset("kick", grooveStyle, bar, onset, seed, isStrongBeat);

            if (offset1 != offset2)
            {
                throw new Exception($"Micro-timing not deterministic: {offset1} != {offset2}");
            }

            Console.WriteLine($"? Micro-timing determinism verified: offset={offset1} ticks");
        }

        /// <summary>
        /// Verifies that timing offsets respect max deviation constraints.
        /// </summary>
        public static void TestMicroTimingConstraints()
        {
            const int seed = 54321;
            const string grooveStyle = "funk";
            const int bar = 2;

            // Test multiple roles
            var roles = new[] { "kick", "snare", "hat", "ride" };
            var maxOffsets = new Dictionary<string, int>
            {
                { "kick", 8 },
                { "snare", 6 },
                { "hat", 5 },
                { "ride", 4 }
            };

            foreach (var role in roles)
            {
                // Test across multiple onsets
                for (decimal onset = 1.0m; onset <= 4.0m; onset += 0.5m)
                {
                    bool isStrongBeat = onset % 1.0m == 0m;
                    int offset = DrumMicroTimingEngine.GetTimingOffset(role, grooveStyle, bar, onset, seed, isStrongBeat);

                    int maxAllowed = maxOffsets[role];
                    if (isStrongBeat)
                        maxAllowed = (int)(maxAllowed * 0.6);

                    if (Math.Abs(offset) > maxAllowed)
                    {
                        throw new Exception($"{role} timing offset {offset} exceeds max {maxAllowed} at onset {onset}");
                    }
                }
            }

            Console.WriteLine("? Micro-timing constraints verified for all roles");
        }

        /// <summary>
        /// Verifies that velocity shaping is deterministic for the same seed.
        /// </summary>
        public static void TestVelocityShapingDeterminism()
        {
            const int seed = 98765;
            const int bar = 3;
            const decimal onset = 2.0m;
            const int baseVelocity = 80;

            // Generate velocity twice with same parameters
            int vel1 = DrumVelocityShaper.ShapeVelocity(
                "snare", baseVelocity, bar, onset, seed, 
                MusicConstants.eSectionType.Chorus, isStrongBeat: true);
            
            int vel2 = DrumVelocityShaper.ShapeVelocity(
                "snare", baseVelocity, bar, onset, seed, 
                MusicConstants.eSectionType.Chorus, isStrongBeat: true);

            if (vel1 != vel2)
            {
                throw new Exception($"Velocity shaping not deterministic: {vel1} != {vel2}");
            }

            Console.WriteLine($"? Velocity shaping determinism verified: velocity={vel1}");
        }

        /// <summary>
        /// Verifies that ghost notes are consistently quiet.
        /// </summary>
        public static void TestGhostNoteVelocity()
        {
            const int seed = 11111;
            const int baseVelocity = 90;

            // Test multiple ghost notes
            for (int bar = 1; bar <= 5; bar++)
            {
                for (decimal onset = 1.5m; onset <= 4.5m; onset += 1.0m)
                {
                    int ghostVel = DrumVelocityShaper.GhostNoteVelocity(baseVelocity, seed, bar, onset);

                    // Ghost notes should be 25-35% of base with some variation
                    if (ghostVel < 15 || ghostVel > 40)
                    {
                        throw new Exception($"Ghost note velocity {ghostVel} out of expected range [15-40] at bar {bar}, onset {onset}");
                    }
                }
            }

            Console.WriteLine("? Ghost note velocities consistently quiet");
        }

        /// <summary>
        /// Verifies that hand pattern accents work correctly for hats/ride.
        /// </summary>
        public static void TestHandPatternAccents()
        {
            const int seed = 22222;
            const int baseVelocity = 70;

            // Test that backbeat (beats 2 and 4) gets accented
            int vel1 = DrumVelocityShaper.ShapeVelocity(
                "hat", baseVelocity, 1, 1.0m, seed, 
                MusicConstants.eSectionType.Verse, isStrongBeat: true);

            int vel2 = DrumVelocityShaper.ShapeVelocity(
                "hat", baseVelocity, 1, 2.0m, seed, 
                MusicConstants.eSectionType.Verse, isStrongBeat: true);

            // Beat 2 should be louder than beat 1 (backbeat accent)
            if (vel2 <= vel1)
            {
                throw new Exception($"Beat 2 velocity {vel2} not accented vs beat 1 velocity {vel1}");
            }

            Console.WriteLine($"? Hand pattern accents verified: beat 1={vel1}, beat 2={vel2} (accented)");
        }

        /// <summary>
        /// Verifies that fill crescendo increases velocity progressively.
        /// </summary>
        public static void TestFillCrescendo()
        {
            const int seed = 33333;
            const int baseVelocity = 80;

            // Test velocity progression through fill
            int vel0 = DrumVelocityShaper.ShapeVelocity(
                "snare", baseVelocity, 1, 1.0m, seed, 
                MusicConstants.eSectionType.Verse, isStrongBeat: true, 
                isInFill: true, fillProgress: 0.0);

            int vel50 = DrumVelocityShaper.ShapeVelocity(
                "snare", baseVelocity, 1, 2.0m, seed, 
                MusicConstants.eSectionType.Verse, isStrongBeat: true, 
                isInFill: true, fillProgress: 0.5);

            int vel100 = DrumVelocityShaper.ShapeVelocity(
                "snare", baseVelocity, 1, 3.0m, seed, 
                MusicConstants.eSectionType.Verse, isStrongBeat: true, 
                isInFill: true, fillProgress: 1.0);

            // Velocity should increase as fill progresses
            if (!(vel0 < vel50 && vel50 < vel100))
            {
                throw new Exception($"Fill crescendo not working: {vel0} -> {vel50} -> {vel100}");
            }

            Console.WriteLine($"? Fill crescendo verified: {vel0} -> {vel50} -> {vel100}");
        }

        /// <summary>
        /// Verifies that velocity values are always in valid MIDI range [1..127].
        /// </summary>
        public static void TestVelocityRange()
        {
            const int seed = 44444;

            // Test extreme base velocities
            var testVelocities = new[] { 1, 50, 100, 127 };
            var roles = new[] { "kick", "snare", "hat", "ride" };
            var sections = new[] { 
                MusicConstants.eSectionType.Intro,
                MusicConstants.eSectionType.Verse, 
                MusicConstants.eSectionType.Chorus,
                MusicConstants.eSectionType.Outro 
            };

            foreach (var baseVel in testVelocities)
            {
                foreach (var role in roles)
                {
                    foreach (var section in sections)
                    {
                        int vel = DrumVelocityShaper.ShapeVelocity(
                            role, baseVel, 1, 1.0m, seed, section, isStrongBeat: true);

                        if (vel < 1 || vel > 127)
                        {
                            throw new Exception($"Velocity {vel} out of MIDI range [1..127] for {role} in {section}");
                        }
                    }
                }
            }

            Console.WriteLine("? All velocities within valid MIDI range [1..127]");
        }

        /// <summary>
        /// Runs all Story 6.2 tests.
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Story 6.2: Timing & Dynamics Humanization Tests ===");
            Console.WriteLine();

            try
            {
                TestMicroTimingDeterminism();
                TestMicroTimingConstraints();
                TestVelocityShapingDeterminism();
                TestGhostNoteVelocity();
                TestHandPatternAccents();
                TestFillCrescendo();
                TestVelocityRange();

                Console.WriteLine();
                Console.WriteLine("=== All Story 6.2 Tests Passed ? ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"=== Test Failed: {ex.Message} ===");
                throw;
            }
        }
    }
}
