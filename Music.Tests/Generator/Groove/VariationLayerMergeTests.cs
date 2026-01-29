// AI: purpose=Unit tests for Story B1 variation layer merge (GrooveVariationLayerMerger).
// AI: deps=xunit for test framework; Music.Generator for types under test.
// AI: change=Story B1 acceptance criteria: test additive union, replace, tag-gated apply/skip, deterministic ordering.

using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
using Xunit;

namespace Music.Generator.Tests
{
    /// <summary>
    /// Story B1: Tests for variation layer merge.
    /// Verifies additive union, replace behavior, tag gating, and deterministic ordering.
    /// </summary>
    public class VariationLayerMergeTests
    {
        #region Additive Union Behavior Tests

        [Fact]
        public void MergeLayersForBar_AdditiveLayer_UnionsWithExistingGroups()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayer("Layer1", isAdditive: true, groups: new[] { "GroupA", "GroupB" }),
                    CreateLayer("Layer2", isAdditive: true, groups: new[] { "GroupC", "GroupD" })
                }
            };
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Contains(result, g => g.GroupId == "GroupA");
            Assert.Contains(result, g => g.GroupId == "GroupB");
            Assert.Contains(result, g => g.GroupId == "GroupC");
            Assert.Contains(result, g => g.GroupId == "GroupD");
        }

        [Fact]
        public void MergeLayersForBar_AdditiveLayer_DedupesByGroupId()
        {
            // Arrange - Both layers contain "GroupA"
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayer("Layer1", isAdditive: true, groups: new[] { "GroupA", "GroupB" }),
                    CreateLayer("Layer2", isAdditive: true, groups: new[] { "GroupA", "GroupC" })
                }
            };
            var enabledTags = new HashSet<string>();


            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - GroupA should appear only once (first-wins)
            Assert.Equal(3, result.Count);
            Assert.Single(result, g => g.GroupId == "GroupA");
            Assert.Contains(result, g => g.GroupId == "GroupB");
            Assert.Contains(result, g => g.GroupId == "GroupC");
        }

        [Fact]
        public void MergeLayersForBar_AdditiveLayer_FirstWinsOnDuplicate()
        {
            // Arrange - GroupA in Layer1 has bias 0.5, GroupA in Layer2 has bias 0.9
            var layer1GroupA = new DrumCandidateGroup { GroupId = "GroupA", BaseProbabilityBias = 0.5 };
            var layer2GroupA = new DrumCandidateGroup { GroupId = "GroupA", BaseProbabilityBias = 0.9 };

            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    new GrooveVariationLayer
                    {
                        LayerId = "Layer1",
                        IsAdditiveOnly = true,
                        CandidateGroups = new List<DrumCandidateGroup> { layer1GroupA }
                    },
                    new GrooveVariationLayer
                    {
                        LayerId = "Layer2",
                        IsAdditiveOnly = true,
                        CandidateGroups = new List<DrumCandidateGroup> { layer2GroupA }
                    }
                }
            };
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - First layer's GroupA should win (bias 0.5)
            Assert.Single(result);
            Assert.Equal(0.5, result[0].BaseProbabilityBias);
        }

        #endregion

        #region Replace Behavior Tests

        [Fact]
        public void MergeLayersForBar_ReplaceLayer_ClearsExistingGroups()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayer("Layer1", isAdditive: true, groups: new[] { "GroupA", "GroupB" }),
                    CreateLayer("Layer2", isAdditive: false, groups: new[] { "GroupC" }) // Replace
                }
            };
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - Only GroupC should remain after replace
            Assert.Single(result);
            Assert.Equal("GroupC", result[0].GroupId);
        }

        [Fact]
        public void MergeLayersForBar_ReplaceLayer_FollowedByAdditive_UnionsCorrectly()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayer("Layer1", isAdditive: true, groups: new[] { "GroupA" }),
                    CreateLayer("Layer2", isAdditive: false, groups: new[] { "GroupB" }), // Replace
                    CreateLayer("Layer3", isAdditive: true, groups: new[] { "GroupC" })   // Additive after replace
                }
            };
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - GroupB and GroupC should be present (GroupA was cleared by replace)
            Assert.Equal(2, result.Count);
            Assert.Contains(result, g => g.GroupId == "GroupB");
            Assert.Contains(result, g => g.GroupId == "GroupC");
            Assert.DoesNotContain(result, g => g.GroupId == "GroupA");
        }

        [Fact]
        public void MergeLayersForBar_MultipleReplaceLayers_LastWins()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayer("Layer1", isAdditive: false, groups: new[] { "GroupA" }),
                    CreateLayer("Layer2", isAdditive: false, groups: new[] { "GroupB" }),
                    CreateLayer("Layer3", isAdditive: false, groups: new[] { "GroupC" })
                }
            };
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - Only last replace layer's groups remain
            Assert.Single(result);
            Assert.Equal("GroupC", result[0].GroupId);
        }

        #endregion

        #region Tag-Gated Apply/Skip Tests

        [Fact]
        public void MergeLayersForBar_LayerWithRequiredTags_AppliesWhenTagsEnabled()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayerWithTags("Layer1", isAdditive: true, requiredTags: new[] { "Fill" }, groups: new[] { "GroupA" })
                }
            };
            var enabledTags = new HashSet<string> { "Fill" };

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - Layer applies because Fill tag is enabled
            Assert.Single(result);
            Assert.Equal("GroupA", result[0].GroupId);
        }

        [Fact]
        public void MergeLayersForBar_LayerWithRequiredTags_SkipsWhenTagsMissing()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayerWithTags("Layer1", isAdditive: true, requiredTags: new[] { "Fill" }, groups: new[] { "GroupA" })
                }
            };
            var enabledTags = new HashSet<string>(); // No tags enabled

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - Layer skipped because Fill tag is not enabled
            Assert.Empty(result);
        }

        [Fact]
        public void MergeLayersForBar_LayerRequiresAllTags_SkipsWhenPartialMatch()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayerWithTags("Layer1", isAdditive: true, requiredTags: new[] { "Fill", "Drive" }, groups: new[] { "GroupA" })
                }
            };
            var enabledTags = new HashSet<string> { "Fill" }; // Only Fill, missing Drive

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - Layer skipped because Drive tag is missing
            Assert.Empty(result);
        }

        [Fact]
        public void MergeLayersForBar_LayerRequiresAllTags_AppliesWhenAllPresent()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayerWithTags("Layer1", isAdditive: true, requiredTags: new[] { "Fill", "Drive" }, groups: new[] { "GroupA" })
                }
            };
            var enabledTags = new HashSet<string> { "Fill", "Drive", "Extra" }; // Has all required + extra

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - Layer applies because all required tags are enabled
            Assert.Single(result);
            Assert.Equal("GroupA", result[0].GroupId);
        }

        [Fact]
        public void MergeLayersForBar_LayerWithEmptyRequiredTags_AlwaysApplies()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayerWithTags("Layer1", isAdditive: true, requiredTags: Array.Empty<string>(), groups: new[] { "GroupA" })
                }
            };
            var enabledTags = new HashSet<string>(); // No tags enabled

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - Layer applies because empty AppliesWhenTagsAll means "always applies"
            Assert.Single(result);
        }

        [Fact]
        public void MergeLayersForBar_MixedTagGating_SelectivelyApplies()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayer("BaseLayer", isAdditive: true, groups: new[] { "GroupBase" }),
                    CreateLayerWithTags("FillLayer", isAdditive: true, requiredTags: new[] { "Fill" }, groups: new[] { "GroupFill" }),
                    CreateLayerWithTags("DriveLayer", isAdditive: true, requiredTags: new[] { "Drive" }, groups: new[] { "GroupDrive" })
                }
            };
            var enabledTags = new HashSet<string> { "Fill" }; // Only Fill enabled

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - BaseLayer and FillLayer apply; DriveLayer skipped
            Assert.Equal(2, result.Count);
            Assert.Contains(result, g => g.GroupId == "GroupBase");
            Assert.Contains(result, g => g.GroupId == "GroupFill");
            Assert.DoesNotContain(result, g => g.GroupId == "GroupDrive");
        }

        [Fact]
        public void MergeLayersForBar_SkippedReplaceLayer_DoesNotClearWorkingSet()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayer("BaseLayer", isAdditive: true, groups: new[] { "GroupA" }),
                    CreateLayerWithTags("ReplaceLayer", isAdditive: false, requiredTags: new[] { "Replace" }, groups: new[] { "GroupB" })
                }
            };
            var enabledTags = new HashSet<string>(); // Replace tag not enabled

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - Replace layer skipped, so GroupA remains
            Assert.Single(result);
            Assert.Equal("GroupA", result[0].GroupId);
        }

        #endregion

        #region Deterministic Ordering Tests

        [Fact]
        public void MergeLayersForBar_Ordering_SortsByLayerOrderThenGroupId()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayer("Layer1", isAdditive: true, groups: new[] { "GroupC", "GroupA" }),
                    CreateLayer("Layer2", isAdditive: true, groups: new[] { "GroupB", "GroupD" })
                }
            };
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - Sorted by layer order first, then by GroupId
            Assert.Equal(4, result.Count);
            // Layer 1 groups (sorted by GroupId): GroupA, GroupC
            // Layer 2 groups (sorted by GroupId): GroupB, GroupD
            Assert.Equal("GroupA", result[0].GroupId);
            Assert.Equal("GroupC", result[1].GroupId);
            Assert.Equal("GroupB", result[2].GroupId);
            Assert.Equal("GroupD", result[3].GroupId);
        }

        [Fact]
        public void MergeLayersForBar_Ordering_IsDeterministic()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayer("Layer1", isAdditive: true, groups: new[] { "ZZZ", "AAA", "MMM" }),
                    CreateLayer("Layer2", isAdditive: true, groups: new[] { "BBB", "YYY" })
                }
            };
            var enabledTags = new HashSet<string>();

            // Act - Run multiple times
            var result1 = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);
            var result2 = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);
            var result3 = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - All runs produce identical ordering
            Assert.Equal(result1.Select(g => g.GroupId), result2.Select(g => g.GroupId));
            Assert.Equal(result2.Select(g => g.GroupId), result3.Select(g => g.GroupId));
        }

        [Fact]
        public void MergeLayersForBar_Ordering_PreservesLayerOrder()
        {
            // Arrange - Groups from later layers should come after groups from earlier layers
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    CreateLayer("Layer1", isAdditive: true, groups: new[] { "GroupZ" }),
                    CreateLayer("Layer2", isAdditive: true, groups: new[] { "GroupA" })
                }
            };
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert - GroupZ (Layer1) should come before GroupA (Layer2) despite alphabetical order
            Assert.Equal(2, result.Count);
            Assert.Equal("GroupZ", result[0].GroupId);
            Assert.Equal("GroupA", result[1].GroupId);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void MergeLayersForBar_EmptyCatalog_ReturnsEmptyList()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog();
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void MergeLayersForBar_NullCatalog_ThrowsArgumentNullException()
        {
            // Arrange
            var enabledTags = new HashSet<string>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                GrooveVariationLayerMerger.MergeLayersForBar(null!, enabledTags));
        }

        [Fact]
        public void MergeLayersForBar_NullEnabledTags_ThrowsArgumentNullException()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                GrooveVariationLayerMerger.MergeLayersForBar(catalog, null!));
        }

        [Fact]
        public void MergeLayersForBar_EmptyLayerGroups_HandlesGracefully()
        {
            // Arrange
            var catalog = new GrooveVariationCatalog
            {
                HierarchyLayers = new List<GrooveVariationLayer>
                {
                    new GrooveVariationLayer
                    {
                        LayerId = "EmptyLayer",
                        IsAdditiveOnly = true,
                        CandidateGroups = new List<DrumCandidateGroup>()
                    }
                }
            };
            var enabledTags = new HashSet<string>();

            // Act
            var result = GrooveVariationLayerMerger.MergeLayersForBar(catalog, enabledTags);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Test Helpers

        private static GrooveVariationLayer CreateLayer(string layerId, bool isAdditive, string[] groups)
        {
            return new GrooveVariationLayer
            {
                LayerId = layerId,
                IsAdditiveOnly = isAdditive,
                AppliesWhenTagsAll = new List<string>(),
                CandidateGroups = groups.Select(g => new DrumCandidateGroup { GroupId = g }).ToList()
            };
        }

        private static GrooveVariationLayer CreateLayerWithTags(string layerId, bool isAdditive, string[] requiredTags, string[] groups)
        {
            return new GrooveVariationLayer
            {
                LayerId = layerId,
                IsAdditiveOnly = isAdditive,
                AppliesWhenTagsAll = requiredTags.ToList(),
                CandidateGroups = groups.Select(g => new DrumCandidateGroup { GroupId = g }).ToList()
            };
        }

        #endregion
    }
}

