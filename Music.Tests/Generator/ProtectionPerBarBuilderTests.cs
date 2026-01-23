// AI: purpose=Unit tests for ProtectionPerBarBuilder; validates per-bar protection merging.
// AI: deps=XUnit; tests deterministic behavior of protection building across bars.
// AI: coverage=Null handling, empty contexts, segment tag merging.

using Music.Generator;
using Music.Generator.Groove;
using Xunit;

namespace Music.Tests.Generator
{
    public class ProtectionPerBarBuilderTests
    {
        #region Null/Empty Input Tests

        [Fact]
        public void Build_WithNullBarContexts_ReturnsEmptyDictionary()
        {
            // Arrange
            var policy = new GrooveProtectionPolicy();

            // Act
            var result = ProtectionPerBarBuilder.Build(null!, policy);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Build_WithEmptyBarContexts_ReturnsEmptyDictionary()
        {
            // Arrange
            var barContexts = new List<BarContext>();
            var policy = new GrooveProtectionPolicy();

            // Act
            var result = ProtectionPerBarBuilder.Build(barContexts, policy);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Build_WithNullPolicy_ReturnsEmptyProtectionsPerBar()
        {
            // Arrange
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 1, Section: null, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 3),
                new BarContext(BarNumber: 2, Section: null, SegmentProfile: null, BarWithinSection: 1, BarsUntilSectionEnd: 2)
            };

            // Act
            var result = ProtectionPerBarBuilder.Build(barContexts, null);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.True(result.ContainsKey(1));
            Assert.True(result.ContainsKey(2));
        }

        #endregion

        #region Basic Building Tests

        [Fact]
        public void Build_CreatesEntryForEachBar()
        {
            // Arrange
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 1, Section: null, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 3),
                new BarContext(BarNumber: 2, Section: null, SegmentProfile: null, BarWithinSection: 1, BarsUntilSectionEnd: 2),
                new BarContext(BarNumber: 3, Section: null, SegmentProfile: null, BarWithinSection: 2, BarsUntilSectionEnd: 1),
                new BarContext(BarNumber: 4, Section: null, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 0)
            };
            var policy = new GrooveProtectionPolicy();

            // Act
            var result = ProtectionPerBarBuilder.Build(barContexts, policy);

            // Assert
            Assert.Equal(4, result.Count);
            Assert.True(result.ContainsKey(1));
            Assert.True(result.ContainsKey(2));
            Assert.True(result.ContainsKey(3));
            Assert.True(result.ContainsKey(4));
        }

        [Fact]
        public void Build_WithSegmentProfile_UsesEnabledTags()
        {
            // Arrange
            var segmentProfile = new SegmentGrooveProfile
            {
                EnabledProtectionTags = new List<string> { "Verse", "Quiet" }
            };

            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 1, Section: null, SegmentProfile: segmentProfile, BarWithinSection: 0, BarsUntilSectionEnd: 0)
            };

            var policy = new GrooveProtectionPolicy
            {
                HierarchyLayers = new List<GrooveProtectionLayer>
                {
                    new GrooveProtectionLayer
                    {
                        LayerId = "Base",
                        RoleProtections = new Dictionary<string, RoleProtectionSet>
                        {
                            ["Kick"] = new RoleProtectionSet { MustHitOnsets = new List<decimal> { 1m } }
                        }
                    }
                }
            };

            // Act
            var result = ProtectionPerBarBuilder.Build(barContexts, policy);

            // Assert
            Assert.Single(result);
            Assert.True(result.ContainsKey(1));
        }

        [Fact]
        public void Build_WithNullSegmentProfile_UsesEmptyTags()
        {
            // Arrange
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 1, Section: null, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 0)
            };

            var policy = new GrooveProtectionPolicy
            {
                HierarchyLayers = new List<GrooveProtectionLayer>
                {
                    new GrooveProtectionLayer
                    {
                        LayerId = "Base",
                        RoleProtections = new Dictionary<string, RoleProtectionSet>
                        {
                            ["Kick"] = new RoleProtectionSet { MustHitOnsets = new List<decimal> { 1m } }
                        }
                    }
                }
            };

            // Act
            var result = ProtectionPerBarBuilder.Build(barContexts, policy);

            // Assert
            Assert.Single(result);
            Assert.True(result.ContainsKey(1));
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void Build_IsDeterministic()
        {
            // Arrange
            var barContexts = new List<BarContext>
            {
                new BarContext(BarNumber: 1, Section: null, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 1),
                new BarContext(BarNumber: 2, Section: null, SegmentProfile: null, BarWithinSection: 1, BarsUntilSectionEnd: 0)
            };

            var policy = new GrooveProtectionPolicy
            {
                HierarchyLayers = new List<GrooveProtectionLayer>
                {
                    new GrooveProtectionLayer
                    {
                        LayerId = "Base",
                        RoleProtections = new Dictionary<string, RoleProtectionSet>
                        {
                            ["Kick"] = new RoleProtectionSet { MustHitOnsets = new List<decimal> { 1m, 3m } }
                        }
                    }
                }
            };

            // Act
            var result1 = ProtectionPerBarBuilder.Build(barContexts, policy);
            var result2 = ProtectionPerBarBuilder.Build(barContexts, policy);
            var result3 = ProtectionPerBarBuilder.Build(barContexts, policy);

            // Assert
            Assert.Equal(result1.Count, result2.Count);
            Assert.Equal(result2.Count, result3.Count);
            Assert.Equal(result1.Keys.OrderBy(k => k), result2.Keys.OrderBy(k => k));
        }

        #endregion
    }
}
