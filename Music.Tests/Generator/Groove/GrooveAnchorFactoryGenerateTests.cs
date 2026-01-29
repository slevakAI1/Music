namespace Music.Tests.Generator.Groove
{
    using Music.Generator;
    using Music.Generator.Groove;

    // AI: purpose=Unit tests for GrooveAnchorFactory.Generate facade method (genre+seed to groove).
    // AI: invariants=Tests verify determinism, variation application, null-safety, error handling.
    public sealed class GrooveAnchorFactoryGenerateTests
    {
        public GrooveAnchorFactoryGenerateTests()
        {
            Rng.Initialize(42);
        }

        #region Generate Basic Tests

        [Fact]
        public void Generate_ValidGenreAndSeed_ReturnsGroove()
        {
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", 123);

            Assert.NotNull(groove);
            Assert.NotNull(groove.KickOnsets);
            Assert.NotNull(groove.SnareOnsets);
            Assert.NotNull(groove.HatOnsets);
        }

        [Fact]
        public void Generate_PopRock_ContainsSnareBackbeat()
        {
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", 123);

            Assert.Contains(2m, groove.SnareOnsets);
            Assert.Contains(4m, groove.SnareOnsets);
        }

        [Fact]
        public void Generate_PopRock_ContainsKickOnsets()
        {
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", 123);

            Assert.Contains(1m, groove.KickOnsets);
            Assert.Contains(3m, groove.KickOnsets);
        }

        [Fact]
        public void Generate_NullGenre_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                GrooveAnchorFactory.Generate(null!, 123));
        }

        [Fact]
        public void Generate_UnknownGenre_ThrowsArgumentException()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                GrooveAnchorFactory.Generate("UnknownGenre", 123));

            Assert.Contains("Unknown genre", ex.Message);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void Generate_SameGenreAndSeed_ProducesIdenticalOutput()
        {
            GrooveInstanceLayer groove1 = GrooveAnchorFactory.Generate("PopRock", 12345);
            GrooveInstanceLayer groove2 = GrooveAnchorFactory.Generate("PopRock", 12345);

            AssertGroovesEqual(groove1, groove2);
        }

        [Fact]
        public void Generate_DifferentSeeds_ProducesDifferentOutputs()
        {
            GrooveInstanceLayer groove1 = GrooveAnchorFactory.Generate("PopRock", 111);
            GrooveInstanceLayer groove2 = GrooveAnchorFactory.Generate("PopRock", 222);

            bool anyDifference = !ListsEqual(groove1.KickOnsets, groove2.KickOnsets) ||
                               !ListsEqual(groove1.HatOnsets, groove2.HatOnsets);

            Assert.True(anyDifference, "Different seeds should produce different grooves");
        }

        [Fact]
        public void Generate_MultipleCallsSameSeed_AllIdentical()
        {
            GrooveInstanceLayer[] grooves = new GrooveInstanceLayer[5];
            int seed = 99999;

            for (int i = 0; i < 5; i++)
            {
                grooves[i] = GrooveAnchorFactory.Generate("PopRock", seed);
            }

            for (int i = 1; i < 5; i++)
            {
                AssertGroovesEqual(grooves[0], grooves[i]);
            }
        }

        [Fact]
        public void Generate_ConsecutiveSeeds_ProduceVariedResults()
        {
            List<GrooveInstanceLayer> grooves = new();
            for (int seed = 0; seed < 10; seed++)
            {
                grooves.Add(GrooveAnchorFactory.Generate("PopRock", seed));
            }

            int identicalCount = 0;
            for (int i = 1; i < grooves.Count; i++)
            {
                if (ListsEqual(grooves[0].KickOnsets, grooves[i].KickOnsets) &&
                    ListsEqual(grooves[0].HatOnsets, grooves[i].HatOnsets))
                {
                    identicalCount++;
                }
            }

            Assert.True(identicalCount < 9, "Not all grooves should be identical with different seeds");
        }

        #endregion

        #region Integration with Variation Tests

        [Fact]
        public void Generate_AppliesVariation_DifferentFromAnchor()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", 123);

            bool hasVariation = groove.KickOnsets.Count != anchor.KickOnsets.Count ||
                              groove.HatOnsets.Count != anchor.HatOnsets.Count;

            Assert.True(hasVariation, "Generated groove should potentially differ from anchor");
        }

        [Fact]
        public void Generate_PreservesAnchorSnareBackbeat()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");

            for (int seed = 0; seed < 50; seed++)
            {
                GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", seed);

                Assert.Equal(anchor.SnareOnsets.Count, groove.SnareOnsets.Count);
                Assert.Contains(2m, groove.SnareOnsets);
                Assert.Contains(4m, groove.SnareOnsets);
            }
        }

        [Fact]
        public void Generate_IncludesAnchorOnsets_PlusVariations()
        {
            GrooveInstanceLayer anchor = GrooveAnchorFactory.GetAnchor("PopRock");
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", 123);

            foreach (decimal onset in anchor.KickOnsets)
            {
                Assert.Contains(onset, groove.KickOnsets);
            }
            foreach (decimal onset in anchor.SnareOnsets)
            {
                Assert.Contains(onset, groove.SnareOnsets);
            }
        }

        [Fact]
        public void Generate_CanProduceVariedKickPatterns()
        {
            HashSet<int> kickCounts = new();

            for (int seed = 0; seed < 100; seed++)
            {
                GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", seed);
                kickCounts.Add(groove.KickOnsets.Count);
            }

            Assert.True(kickCounts.Count > 1, "Should produce varied kick counts across seeds");
        }

        [Fact]
        public void Generate_CanProduceVariedHatPatterns()
        {
            HashSet<int> hatCounts = new();

            for (int seed = 0; seed < 100; seed++)
            {
                GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", seed);
                hatCounts.Add(groove.HatOnsets.Count);
            }

            Assert.True(hatCounts.Count > 1, "Should produce varied hat counts across seeds");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Generate_SeedZero_ProducesValidGroove()
        {
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", 0);

            Assert.NotNull(groove);
            Assert.True(groove.KickOnsets.Count > 0);
            Assert.True(groove.SnareOnsets.Count > 0);
        }

        [Fact]
        public void Generate_NegativeSeed_ProducesValidGroove()
        {
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", -12345);

            Assert.NotNull(groove);
            Assert.Contains(2m, groove.SnareOnsets);
            Assert.Contains(4m, groove.SnareOnsets);
        }

        [Fact]
        public void Generate_MaxIntSeed_ProducesValidGroove()
        {
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", int.MaxValue);

            Assert.NotNull(groove);
            Assert.True(groove.KickOnsets.Count >= 2);
        }

        [Fact]
        public void Generate_MinIntSeed_ProducesValidGroove()
        {
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", int.MinValue);

            Assert.NotNull(groove);
            Assert.True(groove.SnareOnsets.Count >= 2);
        }

        #endregion

        #region Query Method Integration Tests

        [Fact]
        public void Generate_ResultCompatibleWithQueryMethods()
        {
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", 123);

            IReadOnlyList<decimal> kickOnsets = groove.GetOnsets(GrooveRoles.Kick);
            bool hasSnare = groove.HasRole(GrooveRoles.Snare);
            IReadOnlySet<string> activeRoles = groove.GetActiveRoles();

            Assert.NotEmpty(kickOnsets);
            Assert.True(hasSnare);
            Assert.Contains(GrooveRoles.Kick, activeRoles);
            Assert.Contains(GrooveRoles.Snare, activeRoles);
        }

        [Fact]
        public void Generate_AllRolesActive()
        {
            GrooveInstanceLayer groove = GrooveAnchorFactory.Generate("PopRock", 456);

            Assert.True(groove.HasRole(GrooveRoles.Kick));
            Assert.True(groove.HasRole(GrooveRoles.Snare));
            Assert.True(groove.HasRole(GrooveRoles.ClosedHat));
        }

        #endregion

        #region Helper Methods

        private static void AssertGroovesEqual(GrooveInstanceLayer groove1, GrooveInstanceLayer groove2)
        {
            Assert.True(ListsEqual(groove1.KickOnsets, groove2.KickOnsets), "Kick onsets should be equal");
            Assert.True(ListsEqual(groove1.SnareOnsets, groove2.SnareOnsets), "Snare onsets should be equal");
            Assert.True(ListsEqual(groove1.HatOnsets, groove2.HatOnsets), "Hat onsets should be equal");
            Assert.True(ListsEqual(groove1.BassOnsets, groove2.BassOnsets), "Bass onsets should be equal");
            Assert.True(ListsEqual(groove1.CompOnsets, groove2.CompOnsets), "Comp onsets should be equal");
            Assert.True(ListsEqual(groove1.PadsOnsets, groove2.PadsOnsets), "Pads onsets should be equal");
        }

        private static bool ListsEqual(List<decimal> list1, List<decimal> list2)
        {
            if (list1.Count != list2.Count) return false;

            var sorted1 = list1.OrderBy(x => x).ToList();
            var sorted2 = list2.OrderBy(x => x).ToList();

            for (int i = 0; i < sorted1.Count; i++)
            {
                if (sorted1[i] != sorted2[i]) return false;
            }

            return true;
        }

        #endregion
    }
}

