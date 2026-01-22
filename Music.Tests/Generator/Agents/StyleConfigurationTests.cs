// AI: purpose=Unit tests for Story 1.4 StyleConfiguration and StyleConfigurationLibrary.
// AI: deps=xunit for test framework; Music.Generator.Agents.Common for types under test.
// AI: change=Story 1.4 acceptance criteria: configurations load; defaults apply; style lookup works.

using Xunit;
using Music.Generator.Agents.Common;
using Music.Generator;

namespace Music.Generator.Agents.Common.Tests
{
    /// <summary>
    /// Story 1.4: Tests for StyleConfiguration and StyleConfigurationLibrary.
    /// Verifies configuration loading, default values, and style lookup.
    /// </summary>
    [Collection("RngDependentTests")]
    public class StyleConfigurationTests
    {
        public StyleConfigurationTests()
        {
            Rng.Initialize(42);
        }

        #region FeelRules Tests

        [Fact]
        public void FeelRules_Straight_HasCorrectDefaults()
        {
            // Act
            var rules = FeelRules.Straight;

            // Assert
            Assert.Equal(GrooveFeel.Straight, rules.DefaultFeel);
            Assert.Equal(0.0, rules.SwingAmount);
            Assert.True(rules.AllowFeelOverrides);
        }

        [Fact]
        public void FeelRules_Swing_HasCorrectDefaults()
        {
            // Act
            var rules = FeelRules.Swing();

            // Assert
            Assert.Equal(GrooveFeel.Swing, rules.DefaultFeel);
            Assert.Equal(0.5, rules.SwingAmount);
        }

        [Fact]
        public void FeelRules_Swing_CustomAmount_Clamped()
        {
            // Act
            var rulesLow = FeelRules.Swing(-0.5);
            var rulesHigh = FeelRules.Swing(1.5);

            // Assert - clamped to [0.0, 1.0]
            Assert.Equal(0.0, rulesLow.SwingAmount);
            Assert.Equal(1.0, rulesHigh.SwingAmount);
        }

        #endregion

        #region GridRules Tests

        [Fact]
        public void GridRules_SixteenthGrid_HasCorrectSubdivisions()
        {
            // Act
            var rules = GridRules.SixteenthGrid;

            // Assert
            Assert.True(rules.AllowedSubdivisions.HasFlag(AllowedSubdivision.Quarter));
            Assert.True(rules.AllowedSubdivisions.HasFlag(AllowedSubdivision.Eighth));
            Assert.True(rules.AllowedSubdivisions.HasFlag(AllowedSubdivision.Sixteenth));
            Assert.False(rules.AllowTriplets);
        }

        [Fact]
        public void GridRules_EighthWithTriplets_HasCorrectSubdivisions()
        {
            // Act
            var rules = GridRules.EighthWithTriplets;

            // Assert
            Assert.True(rules.AllowedSubdivisions.HasFlag(AllowedSubdivision.Quarter));
            Assert.True(rules.AllowedSubdivisions.HasFlag(AllowedSubdivision.Eighth));
            Assert.True(rules.AllowedSubdivisions.HasFlag(AllowedSubdivision.EighthTriplet));
            Assert.True(rules.AllowTriplets);
        }

        [Fact]
        public void GridRules_AllowTriplets_DetectsBothTripletTypes()
        {
            // Arrange
            var withEighthTriplet = new GridRules
            {
                AllowedSubdivisions = AllowedSubdivision.EighthTriplet
            };
            var withSixteenthTriplet = new GridRules
            {
                AllowedSubdivisions = AllowedSubdivision.SixteenthTriplet
            };
            var withBothTriplets = new GridRules
            {
                AllowedSubdivisions = AllowedSubdivision.EighthTriplet | AllowedSubdivision.SixteenthTriplet
            };

            // Assert
            Assert.True(withEighthTriplet.AllowTriplets);
            Assert.True(withSixteenthTriplet.AllowTriplets);
            Assert.True(withBothTriplets.AllowTriplets);
        }

        #endregion

        #region StyleConfiguration Tests

        [Fact]
        public void StyleConfiguration_GetOperatorWeight_ReturnsConfiguredValue()
        {
            // Arrange
            var config = CreateTestConfiguration(new Dictionary<string, double>
            {
                { "Op1", 0.8 },
                { "Op2", 0.3 }
            });

            // Act & Assert
            Assert.Equal(0.8, config.GetOperatorWeight("Op1"));
            Assert.Equal(0.3, config.GetOperatorWeight("Op2"));
        }

        [Fact]
        public void StyleConfiguration_GetOperatorWeight_ReturnsDefaultForMissing()
        {
            // Arrange
            var config = CreateTestConfiguration(new Dictionary<string, double> { { "Op1", 0.8 } });

            // Act
            double weight = config.GetOperatorWeight("UnknownOp");

            // Assert - default is 0.5
            Assert.Equal(StyleConfiguration.DefaultOperatorWeight, weight);
            Assert.Equal(0.5, weight);
        }

        [Fact]
        public void StyleConfiguration_GetRoleDensity_ReturnsConfiguredValue()
        {
            // Arrange
            var config = CreateTestConfiguration(roleDensities: new Dictionary<string, double>
            {
                { GrooveRoles.Kick, 0.7 }
            });

            // Act
            double density = config.GetRoleDensity(GrooveRoles.Kick);

            // Assert
            Assert.Equal(0.7, density);
        }

        [Fact]
        public void StyleConfiguration_GetRoleDensity_ReturnsDefaultForMissing()
        {
            // Arrange
            var config = CreateTestConfiguration();

            // Act
            double density = config.GetRoleDensity("UnknownRole");

            // Assert - default is 0.5
            Assert.Equal(StyleConfiguration.DefaultRoleDensity, density);
        }

        [Fact]
        public void StyleConfiguration_GetRoleCap_ReturnsConfiguredValue()
        {
            // Arrange
            var config = CreateTestConfiguration(roleCaps: new Dictionary<string, int>
            {
                { GrooveRoles.Kick, 8 }
            });

            // Act
            int cap = config.GetRoleCap(GrooveRoles.Kick);

            // Assert
            Assert.Equal(8, cap);
        }

        [Fact]
        public void StyleConfiguration_GetRoleCap_ReturnsMaxValueForMissing()
        {
            // Arrange
            var config = CreateTestConfiguration();

            // Act
            int cap = config.GetRoleCap("UnknownRole");

            // Assert - no cap
            Assert.Equal(int.MaxValue, cap);
        }

        [Fact]
        public void StyleConfiguration_IsOperatorAllowed_EmptyList_AllowsAll()
        {
            // Arrange
            var config = CreateTestConfiguration(allowedOperators: Array.Empty<string>());

            // Act & Assert
            Assert.True(config.IsOperatorAllowed("AnyOperator"));
            Assert.True(config.IsOperatorAllowed("AnotherOperator"));
        }

        [Fact]
        public void StyleConfiguration_IsOperatorAllowed_WithList_FiltersCorrectly()
        {
            // Arrange
            var config = CreateTestConfiguration(allowedOperators: new[] { "Op1", "Op2" });

            // Act & Assert
            Assert.True(config.IsOperatorAllowed("Op1"));
            Assert.True(config.IsOperatorAllowed("Op2"));
            Assert.False(config.IsOperatorAllowed("Op3"));
        }

        #endregion

        #region StyleConfigurationLibrary Tests

        [Fact]
        public void StyleConfigurationLibrary_GetStyle_PopRock_LoadsCorrectly()
        {
            // Act
            var style = StyleConfigurationLibrary.GetStyle("PopRock");

            // Assert
            Assert.NotNull(style);
            Assert.Equal("PopRock", style.StyleId);
            Assert.Equal("Pop/Rock", style.DisplayName);
            Assert.Equal(GrooveFeel.Straight, style.FeelRules.DefaultFeel);
        }

        [Fact]
        public void StyleConfigurationLibrary_GetStyle_Jazz_LoadsCorrectly()
        {
            // Act
            var style = StyleConfigurationLibrary.GetStyle("Jazz");

            // Assert
            Assert.NotNull(style);
            Assert.Equal("Jazz", style.StyleId);
            Assert.Equal(GrooveFeel.Swing, style.FeelRules.DefaultFeel);
            Assert.True(style.FeelRules.SwingAmount > 0.0);
            Assert.True(style.GridRules.AllowTriplets);
        }

        [Fact]
        public void StyleConfigurationLibrary_GetStyle_Metal_LoadsCorrectly()
        {
            // Act
            var style = StyleConfigurationLibrary.GetStyle("Metal");

            // Assert
            Assert.NotNull(style);
            Assert.Equal("Metal", style.StyleId);
            Assert.Equal(GrooveFeel.Straight, style.FeelRules.DefaultFeel);
            // Metal allows dense kick patterns
            Assert.Equal(16, style.GetRoleCap(GrooveRoles.Kick));
        }

        [Fact]
        public void StyleConfigurationLibrary_GetStyle_CaseInsensitive()
        {
            // Act
            var style1 = StyleConfigurationLibrary.GetStyle("poprock");
            var style2 = StyleConfigurationLibrary.GetStyle("POPROCK");
            var style3 = StyleConfigurationLibrary.GetStyle("PopRock");

            // Assert
            Assert.NotNull(style1);
            Assert.NotNull(style2);
            Assert.NotNull(style3);
            Assert.Equal(style1.StyleId, style2.StyleId);
            Assert.Equal(style2.StyleId, style3.StyleId);
        }

        [Fact]
        public void StyleConfigurationLibrary_GetStyle_UnknownStyle_ReturnsNull()
        {
            // Act
            var style = StyleConfigurationLibrary.GetStyle("UnknownStyle");

            // Assert
            Assert.Null(style);
        }

        [Fact]
        public void StyleConfigurationLibrary_StyleExists_ReturnsCorrectly()
        {
            // Assert
            Assert.True(StyleConfigurationLibrary.StyleExists("PopRock"));
            Assert.True(StyleConfigurationLibrary.StyleExists("Jazz"));
            Assert.True(StyleConfigurationLibrary.StyleExists("Metal"));
            Assert.False(StyleConfigurationLibrary.StyleExists("Country"));
        }

        [Fact]
        public void StyleConfigurationLibrary_AvailableStyleIds_ContainsExpectedStyles()
        {
            // Act
            var ids = StyleConfigurationLibrary.AvailableStyleIds;

            // Assert
            Assert.Contains("PopRock", ids);
            Assert.Contains("Jazz", ids);
            Assert.Contains("Metal", ids);
            Assert.Equal(3, ids.Count);
        }

        [Fact]
        public void StyleConfigurationLibrary_PopRock_HasExpectedRoleDensities()
        {
            // Act
            var style = StyleConfigurationLibrary.PopRock;

            // Assert
            Assert.Equal(0.6, style.GetRoleDensity(GrooveRoles.Kick));
            Assert.Equal(0.5, style.GetRoleDensity(GrooveRoles.Snare));
            Assert.Equal(0.7, style.GetRoleDensity(GrooveRoles.ClosedHat));
        }

        [Fact]
        public void StyleConfigurationLibrary_PopRock_HasExpectedRoleCaps()
        {
            // Act
            var style = StyleConfigurationLibrary.PopRock;

            // Assert
            Assert.Equal(8, style.GetRoleCap(GrooveRoles.Kick));
            Assert.Equal(6, style.GetRoleCap(GrooveRoles.Snare));
            Assert.Equal(16, style.GetRoleCap(GrooveRoles.ClosedHat));
        }

        #endregion

        #region Immutability Tests

        [Fact]
        public void StyleConfiguration_IsImmutable()
        {
            // Arrange
            var original = StyleConfigurationLibrary.PopRock;

            // Act - create modified copy
            var modified = original with { StyleId = "Modified" };

            // Assert - original unchanged
            Assert.Equal("PopRock", original.StyleId);
            Assert.Equal("Modified", modified.StyleId);
        }

        [Fact]
        public void FeelRules_IsImmutable()
        {
            // Arrange
            var original = FeelRules.Straight;

            // Act
            var modified = original with { SwingAmount = 0.5 };

            // Assert
            Assert.Equal(0.0, original.SwingAmount);
            Assert.Equal(0.5, modified.SwingAmount);
        }

        #endregion

        #region Test Helpers

        private static StyleConfiguration CreateTestConfiguration(
            Dictionary<string, double>? operatorWeights = null,
            Dictionary<string, double>? roleDensities = null,
            Dictionary<string, int>? roleCaps = null,
            IReadOnlyList<string>? allowedOperators = null)
        {
            return new StyleConfiguration
            {
                StyleId = "Test",
                DisplayName = "Test Style",
                AllowedOperatorIds = allowedOperators ?? Array.Empty<string>(),
                OperatorWeights = operatorWeights ?? new Dictionary<string, double>(),
                RoleDensityDefaults = roleDensities ?? new Dictionary<string, double>(),
                RoleCaps = roleCaps ?? new Dictionary<string, int>(),
                FeelRules = FeelRules.Straight,
                GridRules = GridRules.SixteenthGrid
            };
        }

        #endregion
    }
}
