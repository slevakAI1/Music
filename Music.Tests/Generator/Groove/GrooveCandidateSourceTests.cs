// AI: purpose=Unit tests for Story B4 operator candidate source hook (IGrooveCandidateSource, CatalogGrooveCandidateSource).
// AI: deps=xunit for test framework; Music.Generator for types under test.
// AI: change=Story B4 acceptance criteria: test adapter output equals direct catalog processing.

using Xunit;

namespace Music.Generator.Tests
{
    /// <summary>
    /// Story B4: Tests for operator candidate source hook.
    /// Verifies IGrooveCandidateSource interface and CatalogGrooveCandidateSource adapter.
    /// </summary>
    public class GrooveCandidateSourceTests
    {
        #region Adapter Output Equivalence Tests

        [Fact]
        public void CatalogAdapter_OutputEqualsDirectProcessing()
        {
            // Arrange - Create catalog with layers
            var catalog = CreateTestCatalog();
            var segmentProfile = new SegmentGrooveProfile
            {
                EnabledVariationTags = new List<string> { "Drive" }
            };
            var barContext = new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: segmentProfile,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);

            // Act - Get groups via adapter
            var adapter = new CatalogGrooveCandidateSource(catalog);
            var adapterResult = adapter.GetCandidateGroups(barContext, "Kick");

            // Act - Get groups via direct processing (what adapter does internally)
            var enabledTags = GrooveCandidateFilter.ResolveEnabledTags(
                segmentProfile, null, null, isInFillWindow: false);
            var mergedGroups = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);
            var directResult = GrooveCandidateFilter.FilterGroupsAndCandidates(mergedGroups, enabledTags);

            // Assert - Adapter output equals direct processing
            Assert.Equal(directResult.Count, adapterResult.Count);
            for (int i = 0; i < directResult.Count; i++)
            {
                Assert.Equal(directResult[i].GroupId, adapterResult[i].GroupId);
                Assert.Equal(directResult[i].Candidates.Count, adapterResult[i].Candidates.Count);
            }
        }

        [Fact]
        public void CatalogAdapter_WithPhraseHookPolicy_IncludesFillTags()
        {
            // Arrange
            var catalog = CreateTestCatalogWithFillGroups();
            var segmentProfile = new SegmentGrooveProfile
            {
                EnabledVariationTags = new List<string> { "Drive" }
            };
            var phraseHookPolicy = new GroovePhraseHookPolicy
            {
                EnabledFillTags = new List<string> { "Fill" }
            };
            var barContext = new GrooveBarContext(
                BarNumber: 4,
                Section: null,
                SegmentProfile: segmentProfile,
                BarWithinSection: 4,
                BarsUntilSectionEnd: 1); // Last bar - in fill window

            // Act
            var adapter = new CatalogGrooveCandidateSource(catalog, phraseHookPolicy);
            var result = adapter.GetCandidateGroups(barContext, "Kick");

            // Assert - Should include both Drive and Fill groups
            Assert.Equal(2, result.Count);
            Assert.Contains(result, g => g.GroupId == "DriveGroup");
            Assert.Contains(result, g => g.GroupId == "FillGroup");
        }

        [Fact]
        public void CatalogAdapter_WithPolicyProvider_UsesPolicyOverride()
        {
            // Arrange
            var catalog = CreateTestCatalog();
            var segmentProfile = new SegmentGrooveProfile
            {
                EnabledVariationTags = new List<string> { "Drive" }
            };
            var policyProvider = new TestPolicyProvider(new GroovePolicyDecision
            {
                EnabledVariationTagsOverride = new List<string> { "Ghost" }
            });
            var barContext = new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: segmentProfile,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);

            // Act
            var adapter = new CatalogGrooveCandidateSource(catalog, policyProvider: policyProvider);
            var result = adapter.GetCandidateGroups(barContext, "Kick");

            // Assert - Policy override should take precedence
            Assert.Single(result);
            Assert.Equal("GhostGroup", result[0].GroupId);
        }

        #endregion

        #region Layer Merging Tests

        [Fact]
        public void CatalogAdapter_AdditiveLayer_UnionsGroups()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    new GrooveVariationLayer
                    {
                        LayerId = "Base",
                        IsAdditiveOnly = true,
                        CandidateGroups = new List<GrooveCandidateGroup>
                        {
                            CreateGroup("GroupA", Array.Empty<string>())
                        }
                    },
                    new GrooveVariationLayer
                    {
                        LayerId = "Refinement",
                        IsAdditiveOnly = true,
                        CandidateGroups = new List<GrooveCandidateGroup>
                        {
                            CreateGroup("GroupB", Array.Empty<string>())
                        }
                    }
                }
            };
            var barContext = CreateBarContext();

            // Act
            var adapter = new CatalogGrooveCandidateSource(catalog);
            var result = adapter.GetCandidateGroups(barContext, "Kick");

            // Assert - Both groups present
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void CatalogAdapter_ReplaceLayer_ClearsEarlierGroups()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    new GrooveVariationLayer
                    {
                        LayerId = "Base",
                        IsAdditiveOnly = true,
                        CandidateGroups = new List<GrooveCandidateGroup>
                        {
                            CreateGroup("GroupA", Array.Empty<string>())
                        }
                    },
                    new GrooveVariationLayer
                    {
                        LayerId = "Replace",
                        IsAdditiveOnly = false, // Replace
                        CandidateGroups = new List<GrooveCandidateGroup>
                        {
                            CreateGroup("GroupB", Array.Empty<string>())
                        }
                    }
                }
            };
            var barContext = CreateBarContext();

            // Act
            var adapter = new CatalogGrooveCandidateSource(catalog);
            var result = adapter.GetCandidateGroups(barContext, "Kick");

            // Assert - Only GroupB remains
            Assert.Single(result);
            Assert.Equal("GroupB", result[0].GroupId);
        }

        #endregion

        #region Tag Filtering Tests

        [Fact]
        public void CatalogAdapter_FiltersByEnabledTags()
        {
            // Arrange
            var catalog = CreateTestCatalog();
            var segmentProfile = new SegmentGrooveProfile
            {
                EnabledVariationTags = new List<string> { "Drive" }
            };
            var barContext = new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: segmentProfile,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);

            // Act
            var adapter = new CatalogGrooveCandidateSource(catalog);
            var result = adapter.GetCandidateGroups(barContext, "Kick");

            // Assert - Only Drive group matches
            Assert.Single(result);
            Assert.Equal("DriveGroup", result[0].GroupId);
        }

        [Fact]
        public void CatalogAdapter_EmptyTagGroups_AlwaysIncluded()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    new GrooveVariationLayer
                    {
                        LayerId = "Base",
                        IsAdditiveOnly = true,
                        CandidateGroups = new List<GrooveCandidateGroup>
                        {
                            CreateGroup("AlwaysGroup", Array.Empty<string>()) // Empty = match all
                        }
                    }
                }
            };
            var segmentProfile = new SegmentGrooveProfile
            {
                EnabledVariationTags = new List<string> { "SomeTag" }
            };
            var barContext = new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: segmentProfile,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);

            // Act
            var adapter = new CatalogGrooveCandidateSource(catalog);
            var result = adapter.GetCandidateGroups(barContext, "Kick");

            // Assert - Empty tag group included
            Assert.Single(result);
            Assert.Equal("AlwaysGroup", result[0].GroupId);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void CatalogAdapter_SameInputs_SameOutput()
        {
            // Arrange
            var catalog = CreateTestCatalog();
            var barContext = CreateBarContext();
            var adapter = new CatalogGrooveCandidateSource(catalog);

            // Act - Run multiple times
            var result1 = adapter.GetCandidateGroups(barContext, "Kick");
            var result2 = adapter.GetCandidateGroups(barContext, "Kick");
            var result3 = adapter.GetCandidateGroups(barContext, "Kick");

            // Assert - All runs produce identical results
            Assert.Equal(result1.Select(g => g.GroupId), result2.Select(g => g.GroupId));
            Assert.Equal(result2.Select(g => g.GroupId), result3.Select(g => g.GroupId));
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void CatalogAdapter_EmptyCatalog_ReturnsEmpty()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog();
            var barContext = CreateBarContext();

            // Act
            var adapter = new CatalogGrooveCandidateSource(catalog);
            var result = adapter.GetCandidateGroups(barContext, "Kick");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void CatalogAdapter_NullCatalog_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CatalogGrooveCandidateSource(null!));
        }

        [Fact]
        public void CatalogAdapter_NullBarContext_ThrowsArgumentNullException()
        {
            // Arrange
            var catalog = CreateTestCatalog();
            var adapter = new CatalogGrooveCandidateSource(catalog);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                adapter.GetCandidateGroups(null!, "Kick"));
        }

        [Fact]
        public void CatalogAdapter_NullRole_ThrowsArgumentNullException()
        {
            // Arrange
            var catalog = CreateTestCatalog();
            var barContext = CreateBarContext();
            var adapter = new CatalogGrooveCandidateSource(catalog);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                adapter.GetCandidateGroups(barContext, null!));
        }

        [Fact]
        public void CatalogAdapter_NoMatchingTags_ReturnsEmpty()
        {
            // Arrange
            var catalog = CreateTestCatalog();
            var segmentProfile = new SegmentGrooveProfile
            {
                EnabledVariationTags = new List<string> { "NonExistentTag" }
            };
            var barContext = new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: segmentProfile,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);

            // Act
            var adapter = new CatalogGrooveCandidateSource(catalog);
            var result = adapter.GetCandidateGroups(barContext, "Kick");

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Interface Contract Tests

        [Fact]
        public void CatalogAdapter_ImplementsInterface()
        {
            // Arrange
            var catalog = CreateTestCatalog();

            // Act
            IGrooveCandidateSource source = new CatalogGrooveCandidateSource(catalog);

            // Assert
            Assert.NotNull(source);
        }

        #endregion

        #region Test Helpers

        private static GrooveVariationCatalog CreateTestCatalog()
        {
            return new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    new GrooveVariationLayer
                    {
                        LayerId = "Base",
                        IsAdditiveOnly = true,
                        CandidateGroups = new List<GrooveCandidateGroup>
                        {
                            CreateGroup("DriveGroup", new[] { "Drive" }),
                            CreateGroup("GhostGroup", new[] { "Ghost" })
                        }
                    }
                }
            };
        }

        private static GrooveVariationCatalog CreateTestCatalogWithFillGroups()
        {
            return new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    new GrooveVariationLayer
                    {
                        LayerId = "Base",
                        IsAdditiveOnly = true,
                        CandidateGroups = new List<GrooveCandidateGroup>
                        {
                            CreateGroup("DriveGroup", new[] { "Drive" }),
                            CreateGroup("FillGroup", new[] { "Fill" })
                        }
                    }
                }
            };
        }

        private static GrooveCandidateGroup CreateGroup(string groupId, string[] tags)
        {
            return new GrooveCandidateGroup
            {
                GroupId = groupId,
                GroupTags = tags.ToList(),
                BaseProbabilityBias = 1.0,
                Candidates = new List<GrooveOnsetCandidate>
                {
                    new GrooveOnsetCandidate
                    {
                        OnsetBeat = 1.0m,
                        ProbabilityBias = 0.5,
                        Tags = new List<string>()
                    }
                }
            };
        }

        private static GrooveBarContext CreateBarContext()
        {
            return new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 4);
        }

        /// <summary>
        /// Test policy provider that returns a fixed policy decision.
        /// </summary>
        private sealed class TestPolicyProvider : IGroovePolicyProvider
        {
            private readonly GroovePolicyDecision _decision;

            public TestPolicyProvider(GroovePolicyDecision decision)
            {
                _decision = decision;
            }

            public GroovePolicyDecision? GetPolicy(GrooveBarContext barContext, string role)
            {
                return _decision;
            }
        }

        #endregion
    }
}
