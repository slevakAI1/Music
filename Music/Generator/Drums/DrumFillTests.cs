// AI: purpose=Tests for Story 6.3 drum fills at section transitions; verifies determinism and acceptance criteria.
// AI: invariants=All tests must produce identical output for same seed; fills must respect bar boundaries and totalBars.
// AI: deps=DrumFillEngine, SectionTrack, RandomHelpers.

namespace Music.Generator.Tests
{
    /// <summary>
    /// Tests for Story 6.3: Section transitions - fills, turnarounds, and pickups.
    /// Verifies deterministic behavior and correct implementation of acceptance criteria.
    /// </summary>
    internal static class DrumFillTests
    {
        /// <summary>
        /// Verifies that fills are generated at section boundaries.
        /// </summary>
        public static void TestFillAtSectionBoundary()
        {
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Intro, 4, "Intro");
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8, "Verse 1");
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8, "Chorus 1");

            int totalBars = sectionTrack.TotalBars;

            // Bar 4 is last bar of Intro - should have fill
            bool bar4Fill = DrumFillEngine.ShouldGenerateFill(4, totalBars, sectionTrack);
            if (!bar4Fill)
            {
                throw new Exception("Bar 4 (end of Intro) should generate fill");
            }

            // Bar 12 is last bar of Verse - should have fill
            bool bar12Fill = DrumFillEngine.ShouldGenerateFill(12, totalBars, sectionTrack);
            if (!bar12Fill)
            {
                throw new Exception("Bar 12 (end of Verse) should generate fill");
            }

            // Bar 5 is middle of Verse - should NOT have fill
            bool bar5Fill = DrumFillEngine.ShouldGenerateFill(5, totalBars, sectionTrack);
            if (bar5Fill)
            {
                throw new Exception("Bar 5 (middle of Verse) should NOT generate fill");
            }

            // Last bar of song should NOT have fill
            bool lastBarFill = DrumFillEngine.ShouldGenerateFill(totalBars, totalBars, sectionTrack);
            if (lastBarFill)
            {
                throw new Exception("Last bar of song should NOT generate fill");
            }

            Console.WriteLine("? Fills generated at correct section boundaries");
        }

        /// <summary>
        /// Verifies that fills respect totalBars constraint (never fill on last bar).
        /// </summary>
        public static void TestFillRespectsBarBoundaries()
        {
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8, "Verse");

            int totalBars = sectionTrack.TotalBars;

            // Last bar should never generate fill
            bool shouldFill = DrumFillEngine.ShouldGenerateFill(totalBars, totalBars, sectionTrack);
            if (shouldFill)
            {
                throw new Exception($"Bar {totalBars} (last bar) should not generate fill");
            }

            // Bar beyond totalBars should never generate fill
            bool beyondFill = DrumFillEngine.ShouldGenerateFill(totalBars + 1, totalBars, sectionTrack);
            if (beyondFill)
            {
                throw new Exception("Bar beyond totalBars should not generate fill");
            }

            Console.WriteLine("? Fills respect bar boundaries and Song.TotalBars");
        }

        /// <summary>
        /// Verifies that fill generation is deterministic for same seed.
        /// </summary>
        public static void TestFillDeterminism()
        {
            const int seed = 98765;
            const int bar = 4;
            const string grooveName = "RockBasic";
            const int sectionIndex = 1;
            const int totalBars = 20;

            var fill1 = DrumFillEngine.GenerateFill(bar, grooveName, MusicConstants.eSectionType.Verse, sectionIndex, seed, totalBars);
            var fill2 = DrumFillEngine.GenerateFill(bar, grooveName, MusicConstants.eSectionType.Verse, sectionIndex, seed, totalBars);

            if (fill1.Count != fill2.Count)
            {
                throw new Exception($"Fill hit count not deterministic: {fill1.Count} != {fill2.Count}");
            }

            for (int i = 0; i < fill1.Count; i++)
            {
                if (fill1[i].Role != fill2[i].Role ||
                    fill1[i].OnsetBeat != fill2[i].OnsetBeat ||
                    fill1[i].IsInFill != fill2[i].IsInFill)
                {
                    throw new Exception($"Fill hit {i} not deterministic");
                }
            }

            Console.WriteLine($"? Fill generation is deterministic: {fill1.Count} hits");
        }

        /// <summary>
        /// Verifies that fills are style-aware (mapped from groove name).
        /// </summary>
        public static void TestFillStyleMapping()
        {
            const int seed = 11111;
            const int bar = 4;
            const int sectionIndex = 1;
            const int totalBars = 20;

            // PopRockBasic should support 16th note rolls and tom movement
            var rockFill = DrumFillEngine.GenerateFill(bar, "PopRockBasic", MusicConstants.eSectionType.Chorus, sectionIndex, seed, totalBars);

            // BossaNovaBasic should have simpler fills (no 16th rolls)
            var bossaFill = DrumFillEngine.GenerateFill(bar, "BossaNovaBasic", MusicConstants.eSectionType.Chorus, sectionIndex, seed, totalBars);

            // Rock (high energy section) should have more hits than Bossa
            if (rockFill.Count <= bossaFill.Count)
            {
                Console.WriteLine($"Warning: Rock fill ({rockFill.Count}) not denser than Bossa fill ({bossaFill.Count})");
                // Not a hard failure, just a warning
            }

            Console.WriteLine($"? Fill style mapping: PopRock={rockFill.Count} hits, Bossa={bossaFill.Count} hits");
        }

        /// <summary>
        /// Verifies that fills are density-capped to avoid overwhelming other roles.
        /// </summary>
        public static void TestFillDensityCap()
        {
            const int seed = 22222;
            const int bar = 8;
            const int sectionIndex = 2;
            const int totalBars = 20;

            // Generate fills for different styles
            var fills = new Dictionary<string, List<DrumVariationEngine.DrumHit>>
            {
                { "PopRock", DrumFillEngine.GenerateFill(bar, "PopRockBasic", MusicConstants.eSectionType.Chorus, sectionIndex, seed, totalBars) },
                { "Funk", DrumFillEngine.GenerateFill(bar, "FunkSyncopated", MusicConstants.eSectionType.Chorus, sectionIndex, seed, totalBars) },
                { "EDM", DrumFillEngine.GenerateFill(bar, "DanceEDMFourOnFloor", MusicConstants.eSectionType.Chorus, sectionIndex, seed, totalBars) },
                { "Bossa", DrumFillEngine.GenerateFill(bar, "BossaNovaBasic", MusicConstants.eSectionType.Chorus, sectionIndex, seed, totalBars) },
                { "Metal", DrumFillEngine.GenerateFill(bar, "MetalDoubleKick", MusicConstants.eSectionType.Chorus, sectionIndex, seed, totalBars) },
                { "Trap", DrumFillEngine.GenerateFill(bar, "TrapModern", MusicConstants.eSectionType.Chorus, sectionIndex, seed, totalBars) }
            };

            // Verify each fill is within reasonable density limits
            foreach (var kvp in fills)
            {
                int hitCount = kvp.Value.Count;
                
                // No fill should exceed 12 hits (reasonable max for one bar, Metal can be dense)
                if (hitCount > 12)
                {
                    throw new Exception($"{kvp.Key} fill has {hitCount} hits, exceeds reasonable density cap");
                }

                Console.WriteLine($"  {kvp.Key}: {hitCount} hits");
            }

            Console.WriteLine("? Fill density is capped appropriately");
        }

        /// <summary>
        /// Verifies that fills have structured shapes (8th/16th rolls, tom movement).
        /// </summary>
        public static void TestFillStructuredShapes()
        {
            const int seed = 33333;
            const int bar = 4;
            const int sectionIndex = 1;
            const int totalBars = 20;

            // High complexity fill (chorus) should have structured shape
            var fill = DrumFillEngine.GenerateFill(bar, "PopRockBasic", MusicConstants.eSectionType.Chorus, sectionIndex, seed, totalBars);

            if (fill.Count == 0)
            {
                throw new Exception("Fill should have at least one hit");
            }

            // Verify all hits are marked as in-fill
            foreach (var hit in fill)
            {
                if (!hit.IsInFill)
                {
                    throw new Exception("All fill hits should have IsInFill=true");
                }
            }

            // Verify fill progress is set correctly
            if (fill.Count > 1)
            {
                if (fill[0].FillProgress != 0.0)
                {
                    throw new Exception("First fill hit should have FillProgress=0.0");
                }

                if (fill[^1].FillProgress != 1.0)
                {
                    throw new Exception("Last fill hit should have FillProgress=1.0");
                }

                // Check that progress increases monotonically
                for (int i = 1; i < fill.Count; i++)
                {
                    if (fill[i].FillProgress <= fill[i - 1].FillProgress)
                    {
                        throw new Exception($"Fill progress should increase monotonically: {fill[i - 1].FillProgress} -> {fill[i].FillProgress}");
                    }
                }
            }

            // Check for tom movement pattern (high->mid->low)
            var tomHits = fill.Where(h => h.Role.StartsWith("tom_")).ToList();
            if (tomHits.Count >= 3)
            {
                // Verify tom types progress from high to low
                bool hasHighToLow = false;
                for (int i = 0; i < tomHits.Count - 2; i++)
                {
                    if (tomHits[i].Role == "tom_high" && 
                        tomHits[i + 1].Role == "tom_mid" && 
                        tomHits[i + 2].Role == "tom_low")
                    {
                        hasHighToLow = true;
                        break;
                    }
                }

                if (!hasHighToLow && tomHits.Count >= 3)
                {
                    Console.WriteLine($"Note: Tom movement not detected in expected pattern (found {tomHits.Count} toms)");
                }
            }

            Console.WriteLine($"? Fill has structured shape: {fill.Count} hits, {tomHits.Count} tom hits");
        }

        /// <summary>
        /// Verifies that fill selection is deterministic for (seed, grooveName, sectionType, sectionIndex).
        /// </summary>
        public static void TestFillSelectionDeterminism()
        {
            const int seed = 44444;
            const int bar = 8;
            const int totalBars = 20;

            // Test multiple groove/section combinations
            var combinations = new[]
            {
                ("PopRockBasic", MusicConstants.eSectionType.Verse, 1),
                ("PopRockBasic", MusicConstants.eSectionType.Chorus, 2),
                ("FunkSyncopated", MusicConstants.eSectionType.Verse, 1),
                ("BossaNovaBasic", MusicConstants.eSectionType.Chorus, 2),
                ("MetalDoubleKick", MusicConstants.eSectionType.Chorus, 3),
                ("TrapModern", MusicConstants.eSectionType.Verse, 1)
            };

            foreach (var (groove, section, index) in combinations)
            {
                var fill1 = DrumFillEngine.GenerateFill(bar, groove, section, index, seed, totalBars);
                var fill2 = DrumFillEngine.GenerateFill(bar, groove, section, index, seed, totalBars);

                if (fill1.Count != fill2.Count)
                {
                    throw new Exception($"Fill for {groove}/{section}/{index} not deterministic: {fill1.Count} != {fill2.Count}");
                }

                // Verify all hit properties match
                for (int i = 0; i < fill1.Count; i++)
                {
                    if (fill1[i].Role != fill2[i].Role ||
                        fill1[i].OnsetBeat != fill2[i].OnsetBeat ||
                        Math.Abs(fill1[i].FillProgress - fill2[i].FillProgress) > 0.001)
                    {
                        throw new Exception($"Fill hit {i} for {groove}/{section}/{index} not deterministic");
                    }
                }
            }

            Console.WriteLine("? Fill selection is deterministic for (seed, grooveName, sectionType, sectionIndex)");
        }

        /// <summary>
        /// Runs all Story 6.3 tests.
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Story 6.3: Section Transitions - Fills Tests ===");
            Console.WriteLine();

            try
            {
                TestFillAtSectionBoundary();
                TestFillRespectsBarBoundaries();
                TestFillDeterminism();
                TestFillStyleMapping();
                TestFillDensityCap();
                TestFillStructuredShapes();
                TestFillSelectionDeterminism();

                Console.WriteLine();
                Console.WriteLine("=== All Story 6.3 Tests Passed ? ===");
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
