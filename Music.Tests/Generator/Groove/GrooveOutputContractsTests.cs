// AI: purpose=Unit tests for Story A1 groove output contracts (GrooveBarContext, GrooveOnset, GrooveBarPlan).
// AI: deps=xunit for test framework; Music.Generator for types under test.
// AI: change=Story A1 acceptance criteria: verify stable types, conversion methods, and immutability.

using Xunit;

namespace Music.Generator.Tests
{
    /// <summary>
    /// Story A1: Tests for stable groove output contracts.
    /// Verifies GrooveBarContext, GrooveOnset, and GrooveBarPlan types.
    /// </summary>
    public class GrooveOutputContractsTests
    {
        #region GrooveBarContext Tests

        [Fact]
        public void GrooveBarContext_CanCreate_WithValidParameters()
        {
            // Arrange & Act
            var context = new GrooveBarContext(
                BarNumber: 5,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 2,
                BarsUntilSectionEnd: 3);

            // Assert
            Assert.Equal(5, context.BarNumber);
            Assert.Null(context.Section);
            Assert.Null(context.SegmentProfile);
            Assert.Equal(2, context.BarWithinSection);
            Assert.Equal(3, context.BarsUntilSectionEnd);
        }

        [Fact]
        public void GrooveBarContext_FromBarContext_ConvertsCorrectly()
        {
            // Arrange
            var barContext = new BarContext(
                BarNumber: 10,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 5,
                BarsUntilSectionEnd: 7);

            // Act
            var grooveContext = GrooveBarContext.FromBarContext(barContext);

            // Assert
            Assert.Equal(10, grooveContext.BarNumber);
            Assert.Equal(5, grooveContext.BarWithinSection);
            Assert.Equal(7, grooveContext.BarsUntilSectionEnd);
        }

        [Fact]
        public void GrooveBarContext_ToBarContext_ConvertsCorrectly()
        {
            // Arrange
            var grooveContext = new GrooveBarContext(
                BarNumber: 8,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 3,
                BarsUntilSectionEnd: 4);

            // Act
            var barContext = grooveContext.ToBarContext();

            // Assert
            Assert.Equal(8, barContext.BarNumber);
            Assert.Equal(3, barContext.BarWithinSection);
            Assert.Equal(4, barContext.BarsUntilSectionEnd);
        }

        [Fact]
        public void GrooveBarContext_RoundTrip_PreservesData()
        {
            // Arrange
            var original = new BarContext(
                BarNumber: 15,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 8,
                BarsUntilSectionEnd: 6);

            // Act
            var grooveContext = GrooveBarContext.FromBarContext(original);
            var roundTrip = grooveContext.ToBarContext();

            // Assert
            Assert.Equal(original.BarNumber, roundTrip.BarNumber);
            Assert.Equal(original.BarWithinSection, roundTrip.BarWithinSection);
            Assert.Equal(original.BarsUntilSectionEnd, roundTrip.BarsUntilSectionEnd);
        }

        #endregion

        #region GrooveOnset Tests

        [Fact]
        public void GrooveOnset_CanCreate_WithRequiredFields()
        {
            // Arrange & Act
            var onset = new GrooveOnset
            {
                Role = "Kick",
                BarNumber = 1,
                Beat = 1.0m
            };

            // Assert
            Assert.Equal("Kick", onset.Role);
            Assert.Equal(1, onset.BarNumber);
            Assert.Equal(1.0m, onset.Beat);
            Assert.Null(onset.Strength);
            Assert.Null(onset.Velocity);
            Assert.Null(onset.TimingOffsetTicks);
            Assert.Null(onset.Provenance);
            Assert.False(onset.IsMustHit);
            Assert.False(onset.IsNeverRemove);
            Assert.False(onset.IsProtected);
        }

        [Fact]
        public void GrooveOnset_CanCreate_WithAllFields()
        {
            // Arrange & Act
            var onset = new GrooveOnset
            {
                Role = "Snare",
                BarNumber = 2,
                Beat = 2.5m,
                Strength = OnsetStrength.Backbeat,
                Velocity = 100,
                TimingOffsetTicks = -5,
                Provenance = null, // Would be MaterialProvenance in real use
                IsMustHit = true,
                IsNeverRemove = true,
                IsProtected = false
            };

            // Assert
            Assert.Equal("Snare", onset.Role);
            Assert.Equal(2, onset.BarNumber);
            Assert.Equal(2.5m, onset.Beat);
            Assert.Equal(OnsetStrength.Backbeat, onset.Strength);
            Assert.Equal(100, onset.Velocity);
            Assert.Equal(-5, onset.TimingOffsetTicks);
            Assert.True(onset.IsMustHit);
            Assert.True(onset.IsNeverRemove);
            Assert.False(onset.IsProtected);
        }

        [Fact]
        public void GrooveOnset_SupportsFractionalBeats()
        {
            // Arrange & Act - test eighth note offbeat
            var onset = new GrooveOnset
            {
                Role = "Hat",
                BarNumber = 1,
                Beat = 1.5m
            };

            // Assert
            Assert.Equal(1.5m, onset.Beat);
        }

        [Fact]
        public void GrooveOnset_SupportsAllStrengthValues()
        {
            // Arrange & Act - verify all OnsetStrength enum values work
            var strengths = new[]
            {
                OnsetStrength.Downbeat,
                OnsetStrength.Backbeat,
                OnsetStrength.Strong,
                OnsetStrength.Offbeat,
                OnsetStrength.Pickup,
                OnsetStrength.Ghost
            };

            // Assert - all values should be assignable
            foreach (var strength in strengths)
            {
                var onset = new GrooveOnset
                {
                    Role = "Test",
                    BarNumber = 1,
                    Beat = 1.0m,
                    Strength = strength
                };
                Assert.Equal(strength, onset.Strength);
            }
        }

        [Fact]
        public void GrooveOnset_IsImmutable()
        {
            // Arrange
            var onset = new GrooveOnset
            {
                Role = "Kick",
                BarNumber = 1,
                Beat = 1.0m,
                Velocity = 100
            };

            // Act & Assert - verify record is immutable via with-expression
            var modified = onset with { Velocity = 80 };
            Assert.Equal(100, onset.Velocity);
            Assert.Equal(80, modified.Velocity);
        }

        #endregion

        #region GrooveBarPlan Tests

        [Fact]
        public void GrooveBarPlan_CanCreate_WithEmptyOnsetLists()
        {
            // Arrange & Act
            var plan = new GrooveBarPlan
            {
                BarNumber = 1,
                BaseOnsets = Array.Empty<GrooveOnset>(),
                SelectedVariationOnsets = Array.Empty<GrooveOnset>(),
                FinalOnsets = Array.Empty<GrooveOnset>()
            };

            // Assert
            Assert.Equal(1, plan.BarNumber);
            Assert.Empty(plan.BaseOnsets);
            Assert.Empty(plan.SelectedVariationOnsets);
            Assert.Empty(plan.FinalOnsets);
            Assert.Null(plan.Diagnostics);
        }

        [Fact]
        public void GrooveBarPlan_CanCreate_WithOnsets()
        {
            // Arrange
            var baseOnset = new GrooveOnset
            {
                Role = "Kick",
                BarNumber = 1,
                Beat = 1.0m
            };
            var variationOnset = new GrooveOnset
            {
                Role = "Snare",
                BarNumber = 1,
                Beat = 2.0m
            };
            var finalOnset1 = new GrooveOnset
            {
                Role = "Kick",
                BarNumber = 1,
                Beat = 1.0m
            };
            var finalOnset2 = new GrooveOnset
            {
                Role = "Snare",
                BarNumber = 1,
                Beat = 2.0m
            };

            // Act
            var plan = new GrooveBarPlan
            {
                BarNumber = 1,
                BaseOnsets = new[] { baseOnset },
                SelectedVariationOnsets = new[] { variationOnset },
                FinalOnsets = new[] { finalOnset1, finalOnset2 }
            };

            // Assert
            Assert.Equal(1, plan.BarNumber);
            Assert.Single(plan.BaseOnsets);
            Assert.Single(plan.SelectedVariationOnsets);
            Assert.Equal(2, plan.FinalOnsets.Count);
            Assert.Equal("Kick", plan.BaseOnsets[0].Role);
            Assert.Equal("Snare", plan.SelectedVariationOnsets[0].Role);
        }

        [Fact]
        public void GrooveBarPlan_SupportsOptionalDiagnostics()
        {
            // Arrange
            var diagnostics = new { Message = "Test diagnostic data" };

            // Act
            var plan = new GrooveBarPlan
            {
                BarNumber = 1,
                BaseOnsets = Array.Empty<GrooveOnset>(),
                SelectedVariationOnsets = Array.Empty<GrooveOnset>(),
                FinalOnsets = Array.Empty<GrooveOnset>(),
                Diagnostics = diagnostics
            };

            // Assert
            Assert.NotNull(plan.Diagnostics);
            Assert.Equal("Test diagnostic data", ((dynamic)plan.Diagnostics!).Message);
        }

        [Fact]
        public void GrooveBarPlan_IsImmutable()
        {
            // Arrange
            var plan = new GrooveBarPlan
            {
                BarNumber = 1,
                BaseOnsets = Array.Empty<GrooveOnset>(),
                SelectedVariationOnsets = Array.Empty<GrooveOnset>(),
                FinalOnsets = Array.Empty<GrooveOnset>()
            };

            // Act & Assert - verify record is immutable via with-expression
            var modified = plan with { BarNumber = 2 };
            Assert.Equal(1, plan.BarNumber);
            Assert.Equal(2, modified.BarNumber);
        }

        [Fact]
        public void GrooveBarPlan_OnsetLists_AreReadOnly()
        {
            // Arrange
            var onset = new GrooveOnset
            {
                Role = "Kick",
                BarNumber = 1,
                Beat = 1.0m
            };

            var plan = new GrooveBarPlan
            {
                BarNumber = 1,
                BaseOnsets = new[] { onset },
                SelectedVariationOnsets = Array.Empty<GrooveOnset>(),
                FinalOnsets = new[] { onset }
            };

            // Assert - verify lists are IReadOnlyList
            Assert.IsAssignableFrom<IReadOnlyList<GrooveOnset>>(plan.BaseOnsets);
            Assert.IsAssignableFrom<IReadOnlyList<GrooveOnset>>(plan.SelectedVariationOnsets);
            Assert.IsAssignableFrom<IReadOnlyList<GrooveOnset>>(plan.FinalOnsets);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Story_A1_IntegrationTest_CompleteGroovePipeline()
        {
            // Arrange - simulate a complete groove pipeline for one bar
            var barContext = new GrooveBarContext(
                BarNumber: 1,
                Section: null,
                SegmentProfile: null,
                BarWithinSection: 0,
                BarsUntilSectionEnd: 7);

            // Phase 2: Anchor generation
            var baseOnsets = new[]
            {
                new GrooveOnset
                {
                    Role = "Kick",
                    BarNumber = 1,
                    Beat = 1.0m,
                    IsMustHit = true
                },
                new GrooveOnset
                {
                    Role = "Snare",
                    BarNumber = 1,
                    Beat = 3.0m,
                    IsMustHit = true
                }
            };

            // Phase 3: Variation selection
            var variationOnsets = new[]
            {
                new GrooveOnset
                {
                    Role = "Hat",
                    BarNumber = 1,
                    Beat = 1.5m
                },
                new GrooveOnset
                {
                    Role = "Hat",
                    BarNumber = 1,
                    Beat = 2.5m
                }
            };

            // Phase 4: Final onsets (anchors + variations)
            var finalOnsets = baseOnsets.Concat(variationOnsets).ToArray();

            // Act
            var plan = new GrooveBarPlan
            {
                BarNumber = 1,
                BaseOnsets = baseOnsets,
                SelectedVariationOnsets = variationOnsets,
                FinalOnsets = finalOnsets
            };

            // Assert
            Assert.Equal(1, plan.BarNumber);
            Assert.Equal(2, plan.BaseOnsets.Count);
            Assert.Equal(2, plan.SelectedVariationOnsets.Count);
            Assert.Equal(4, plan.FinalOnsets.Count);
            Assert.All(plan.BaseOnsets, o => Assert.True(o.IsMustHit));
            Assert.All(plan.SelectedVariationOnsets, o => Assert.False(o.IsMustHit));
        }

        #endregion
    }
}
