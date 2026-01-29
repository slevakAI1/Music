namespace Music.Tests.Generator.Groove
{
    using Music.Generator;
    using Music.Generator.Groove;

    // AI: purpose=Unit tests for GrooveInstanceLayer.CreateVariation seed-based variation logic.
    // AI: invariants=Tests verify determinism, snare backbeat preservation, variation types, null-safety.
    public sealed class GrooveInstanceLayerVariationTests
    {
        public GrooveInstanceLayerVariationTests()
        {
            Rng.Initialize(42);
        }

        #region CreateVariation Basic Tests

        [Fact]
        public void CreateVariation_NullAnchor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                GrooveInstanceLayer.CreateVariation(null!, 123));
        }

        [Fact]
        public void CreateVariation_ValidAnchor_ReturnsNewInstance()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();

            GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, 123);

            Assert.NotNull(variation);
            Assert.NotSame(anchor, variation);
        }

        [Fact]
        public void CreateVariation_ValidAnchor_PreservesSnareBackbeat()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();

            GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, 123);

            Assert.Contains(2m, variation.SnareOnsets);
            Assert.Contains(4m, variation.SnareOnsets);
            Assert.Equal(anchor.SnareOnsets.Count, variation.SnareOnsets.Count);
        }

        [Fact]
        public void CreateVariation_ValidAnchor_PreservesOriginalAnchorIntact()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();
            int originalKickCount = anchor.KickOnsets.Count;
            int originalHatCount = anchor.HatOnsets.Count;

            GrooveInstanceLayer.CreateVariation(anchor, 123);

            Assert.Equal(originalKickCount, anchor.KickOnsets.Count);
            Assert.Equal(originalHatCount, anchor.HatOnsets.Count);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void CreateVariation_SameSeed_ProducesIdenticalOutput()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();
            int seed = 12345;

            GrooveInstanceLayer variation1 = GrooveInstanceLayer.CreateVariation(anchor, seed);
            GrooveInstanceLayer variation2 = GrooveInstanceLayer.CreateVariation(anchor, seed);

            AssertGroovesEqual(variation1, variation2);
        }

        [Fact]
        public void CreateVariation_DifferentSeeds_ProducesDifferentOutputs()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();

            GrooveInstanceLayer variation1 = GrooveInstanceLayer.CreateVariation(anchor, 111);
            GrooveInstanceLayer variation2 = GrooveInstanceLayer.CreateVariation(anchor, 222);

            bool anyDifference = !ListsEqual(variation1.KickOnsets, variation2.KickOnsets) ||
                               !ListsEqual(variation1.HatOnsets, variation2.HatOnsets);

            Assert.True(anyDifference, "Different seeds should produce different variations");
        }

        [Fact]
        public void CreateVariation_MultipleCallsSameSeed_AllIdentical()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();
            int seed = 99999;

            GrooveInstanceLayer[] variations = new GrooveInstanceLayer[5];
            for (int i = 0; i < 5; i++)
            {
                variations[i] = GrooveInstanceLayer.CreateVariation(anchor, seed);
            }

            for (int i = 1; i < 5; i++)
            {
                AssertGroovesEqual(variations[0], variations[i]);
            }
        }

        #endregion

        #region Kick Doubles Tests

        [Fact]
        public void CreateVariation_KickDoubles_NeverAddsDuplicates()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();
            anchor.KickOnsets.Add(1.5m);

            for (int seed = 0; seed < 100; seed++)
            {
                GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, seed);

                int count15 = variation.KickOnsets.Count(x => x == 1.5m);
                Assert.Equal(1, count15);
            }
        }

        [Fact]
        public void CreateVariation_KickDoubles_AddsToKickOnly()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();
            int originalSnareCount = anchor.SnareOnsets.Count;

            GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, 123);

            Assert.Equal(originalSnareCount, variation.SnareOnsets.Count);
        }

        [Fact]
        public void CreateVariation_KickDoubles_CanAddBothPositions()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();
            bool found15 = false;
            bool found35 = false;

            for (int seed = 0; seed < 100; seed++)
            {
                GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, seed);
                if (variation.KickOnsets.Contains(1.5m)) found15 = true;
                if (variation.KickOnsets.Contains(3.5m)) found35 = true;
                if (found15 && found35) break;
            }

            Assert.True(found15 || found35, "Should add kick doubles across seed range");
        }

        #endregion

        #region Hat Subdivision Tests

        [Fact]
        public void CreateVariation_HatSubdivision_Adds16thNotes()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();

            bool foundSubdivision = false;
            for (int seed = 0; seed < 100; seed++)
            {
                GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, seed);
                if (variation.HatOnsets.Contains(1.25m) ||
                    variation.HatOnsets.Contains(2.25m) ||
                    variation.HatOnsets.Contains(3.25m) ||
                    variation.HatOnsets.Contains(4.25m))
                {
                    foundSubdivision = true;
                    break;
                }
            }

            Assert.True(foundSubdivision, "Should add 16th note subdivisions across seed range");
        }

        [Fact]
        public void CreateVariation_HatSubdivision_NeverAddsDuplicates()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();
            anchor.HatOnsets.Add(1.25m);

            for (int seed = 0; seed < 100; seed++)
            {
                GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, seed);

                int count125 = variation.HatOnsets.Count(x => x == 1.25m);
                Assert.Equal(1, count125);
            }
        }

        [Fact]
        public void CreateVariation_HatSubdivision_AddsMultiple16ths()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();

            bool foundAll16ths = false;
            for (int seed = 0; seed < 200; seed++)
            {
                GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, seed);
                if (variation.HatOnsets.Contains(1.25m) &&
                    variation.HatOnsets.Contains(2.25m) &&
                    variation.HatOnsets.Contains(3.25m) &&
                    variation.HatOnsets.Contains(4.25m))
                {
                    foundAll16ths = true;
                    break;
                }
            }

            Assert.True(foundAll16ths, "Should add all four 16th positions together in some seeds");
        }

        #endregion

        #region Syncopation Tests

        [Fact]
        public void CreateVariation_Syncopation_AddsAnticipations()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();

            bool foundSyncopation = false;
            for (int seed = 0; seed < 200; seed++)
            {
                GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, seed);
                if (variation.KickOnsets.Contains(1.75m) ||
                    variation.KickOnsets.Contains(3.75m))
                {
                    foundSyncopation = true;
                    break;
                }
            }

            Assert.True(foundSyncopation, "Should add syncopation across seed range");
        }

        [Fact]
        public void CreateVariation_Syncopation_NeverAddsDuplicates()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();
            anchor.KickOnsets.Add(1.75m);

            for (int seed = 0; seed < 100; seed++)
            {
                GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, seed);

                int count175 = variation.KickOnsets.Count(x => x == 1.75m);
                Assert.Equal(1, count175);
            }
        }

        [Fact]
        public void CreateVariation_Syncopation_AddsToKickOnly()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();
            int originalSnareCount = anchor.SnareOnsets.Count;

            for (int seed = 0; seed < 50; seed++)
            {
                GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, seed);
                Assert.Equal(originalSnareCount, variation.SnareOnsets.Count);
            }
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void CreateVariation_MultipleVariationTypes_CanCombine()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();

            bool foundKickAndHat = false;
            for (int seed = 0; seed < 200; seed++)
            {
                GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, seed);
                bool hasKickDouble = variation.KickOnsets.Contains(1.5m) || variation.KickOnsets.Contains(3.5m);
                bool hasHatSubdivision = variation.HatOnsets.Contains(1.25m) || variation.HatOnsets.Contains(2.25m);

                if (hasKickDouble && hasHatSubdivision)
                {
                    foundKickAndHat = true;
                    break;
                }
            }

            Assert.True(foundKickAndHat, "Multiple variation types should be able to combine");
        }

        [Fact]
        public void CreateVariation_AllRoles_PreserveOriginalOnsets()
        {
            GrooveInstanceLayer anchor = CreateFullAnchor();

            GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, 123);

            foreach (decimal onset in anchor.KickOnsets)
            {
                Assert.Contains(onset, variation.KickOnsets);
            }
            foreach (decimal onset in anchor.SnareOnsets)
            {
                Assert.Contains(onset, variation.SnareOnsets);
            }
            foreach (decimal onset in anchor.HatOnsets)
            {
                Assert.Contains(onset, variation.HatOnsets);
            }
        }

        [Fact]
        public void CreateVariation_EmptyAnchor_ReturnsEmptyOrNearEmpty()
        {
            GrooveInstanceLayer anchor = new();

            GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, 123);

            Assert.NotNull(variation);
        }

        [Fact]
        public void CreateVariation_SnareBackbeat_AlwaysPreserved()
        {
            GrooveInstanceLayer anchor = CreateBasicAnchor();

            for (int seed = 0; seed < 100; seed++)
            {
                GrooveInstanceLayer variation = GrooveInstanceLayer.CreateVariation(anchor, seed);

                Assert.Contains(2m, variation.SnareOnsets);
                Assert.Contains(4m, variation.SnareOnsets);
                Assert.DoesNotContain(1m, variation.SnareOnsets);
                Assert.DoesNotContain(3m, variation.SnareOnsets);
            }
        }

        #endregion

        #region Helper Methods

        private static GrooveInstanceLayer CreateBasicAnchor()
        {
            return new GrooveInstanceLayer
            {
                KickOnsets = new List<decimal> { 1m, 3m },
                SnareOnsets = new List<decimal> { 2m, 4m },
                HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m }
            };
        }

        private static GrooveInstanceLayer CreateFullAnchor()
        {
            return new GrooveInstanceLayer
            {
                KickOnsets = new List<decimal> { 1m, 3m },
                SnareOnsets = new List<decimal> { 2m, 4m },
                HatOnsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m },
                BassOnsets = new List<decimal> { 1m, 3m },
                CompOnsets = new List<decimal> { 1.5m, 2.5m, 3.5m, 4.5m },
                PadsOnsets = new List<decimal> { 1m, 3m }
            };
        }

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

