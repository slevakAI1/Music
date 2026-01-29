// AI: purpose=Unit tests for Story 2.3 DrummerPolicyProvider.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums for types under test.
// AI: change=Story 2.3 acceptance criteria: determinism, density overrides, fill window gating.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Common;
using Music.Generator;
using Music;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Tests
{
    /// <summary>
    /// Story 2.3: Tests for DrummerPolicyProvider.
    /// Verifies policy decision computation, determinism, and override behavior.
    /// </summary>
    [Collection("RngDependentTests")]
    public class DrummerPolicyProviderTests
    {
        public DrummerPolicyProviderTests()
        {
            Rng.Initialize(42);
        }

        #region Construction Tests

        [Fact]
        public void Constructor_NullStyleConfig_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DrummerPolicyProvider(null!));
        }

        [Fact]
        public void Constructor_ValidStyleConfig_Succeeds()
        {
            // Arrange
            var style = StyleConfigurationLibrary.PopRock;

            // Act
            var provider = new DrummerPolicyProvider(style);

            // Assert
            Assert.NotNull(provider);
        }

        [Fact]
        public void Constructor_WithMemory_Succeeds()
        {
            // Arrange
            var style = StyleConfigurationLibrary.PopRock;
            var memory = new AgentMemory();

            // Act
            var provider = new DrummerPolicyProvider(style, memory);

            // Assert
            Assert.NotNull(provider);
        }

        #endregion

        #region GetPolicy Basic Tests

        [Fact]
        public void GetPolicy_NullBarContext_Throws()
        {
            // Arrange
            var provider = CreateProvider();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => provider.GetPolicy(null!, GrooveRoles.Kick));
        }

        [Fact]
        public void GetPolicy_NullRole_Throws()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => provider.GetPolicy(barContext, null!));
        }

        [Fact]
        public void GetPolicy_ValidInputs_ReturnsNonNullDecision()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext();

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Kick);

            // Assert
            Assert.NotNull(decision);
        }

        [Fact]
        public void GetPolicy_UnknownRole_ReturnsNoOverrides()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext();

            // Act
            var decision = provider.GetPolicy(barContext, "UnknownRole");

            // Assert
            Assert.NotNull(decision);
            Assert.False(decision.HasAnyOverrides);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void GetPolicy_SameBarContext_ReturnsDeterministicPolicy()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext(barNumber: 5, sectionType: MusicConstants.eSectionType.Chorus);

            // Act
            var decision1 = provider.GetPolicy(barContext, GrooveRoles.Snare);
            var decision2 = provider.GetPolicy(barContext, GrooveRoles.Snare);

            // Assert
            Assert.Equal(decision1!.Density01Override, decision2!.Density01Override);
            Assert.Equal(decision1.MaxEventsPerBarOverride, decision2.MaxEventsPerBarOverride);
            Assert.Equal(decision1.RoleTimingFeelOverride, decision2.RoleTimingFeelOverride);
            Assert.Equal(decision1.VelocityBiasOverride, decision2.VelocityBiasOverride);
        }

        [Fact]
        public void GetPolicy_DifferentBars_ProducesDifferentDecisions()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext1 = CreateBarContext(barNumber: 1, sectionType: MusicConstants.eSectionType.Intro);
            var barContext2 = CreateBarContext(barNumber: 10, sectionType: MusicConstants.eSectionType.Chorus);

            // Act
            var decision1 = provider.GetPolicy(barContext1, GrooveRoles.Kick);
            var decision2 = provider.GetPolicy(barContext2, GrooveRoles.Kick);

            // Assert - different sections should produce different density
            Assert.NotEqual(decision1!.Density01Override, decision2!.Density01Override);
        }

        #endregion

        #region Density Override Tests

        [Fact]
        public void GetPolicy_ChorusSection_HigherDensity()
        {
            // Arrange
            var provider = CreateProvider();
            var verseContext = CreateBarContext(sectionType: MusicConstants.eSectionType.Verse);
            var chorusContext = CreateBarContext(sectionType: MusicConstants.eSectionType.Chorus);

            // Act
            var verseDecision = provider.GetPolicy(verseContext, GrooveRoles.Kick);
            var chorusDecision = provider.GetPolicy(chorusContext, GrooveRoles.Kick);

            // Assert - chorus should have higher density
            Assert.True(chorusDecision!.Density01Override > verseDecision!.Density01Override);
        }

        [Fact]
        public void GetPolicy_BridgeSection_LowerDensity()
        {
            // Arrange
            var provider = CreateProvider();
            var verseContext = CreateBarContext(sectionType: MusicConstants.eSectionType.Verse);
            var bridgeContext = CreateBarContext(sectionType: MusicConstants.eSectionType.Bridge);

            // Act
            var verseDecision = provider.GetPolicy(verseContext, GrooveRoles.Kick);
            var bridgeDecision = provider.GetPolicy(bridgeContext, GrooveRoles.Kick);

            // Assert - bridge should have lower density
            Assert.True(bridgeDecision!.Density01Override < verseDecision!.Density01Override);
        }

        [Fact]
        public void GetPolicy_DensityClampedToValidRange()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext(sectionType: MusicConstants.eSectionType.Chorus);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Kick);

            // Assert - density must be in [0.0, 1.0]
            Assert.True(decision!.Density01Override >= 0.0);
            Assert.True(decision.Density01Override <= 1.0);
        }

        #endregion

        #region MaxEvents Override Tests

        [Fact]
        public void GetPolicy_KickRole_RespectsStyleCap()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext();

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Kick);

            // Assert - PopRock has Kick cap of 8
            Assert.Equal(8, decision!.MaxEventsPerBarOverride);
        }

        [Fact]
        public void GetPolicy_HatRole_RespectsStyleCap()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext();

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.ClosedHat);

            // Assert - PopRock has ClosedHat cap of 16
            Assert.Equal(16, decision!.MaxEventsPerBarOverride);
        }

        #endregion

        #region Fill Window Tests

        [Fact]
        public void GetPolicy_FillWindow_EnablesFillTags()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext(barNumber: 8, barsUntilSectionEnd: 0);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Snare);

            // Assert - should have fill-related variation tags
            Assert.NotNull(decision!.EnabledVariationTagsOverride);
            Assert.Contains("Fill", decision.EnabledVariationTagsOverride);
        }

        [Fact]
        public void GetPolicy_NotInFillWindow_NoFillTags()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext(barNumber: 2, barsUntilSectionEnd: 6);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Snare);

            // Assert - should not have fill tags when not in fill window
            if (decision!.EnabledVariationTagsOverride != null)
            {
                Assert.DoesNotContain("Fill", decision.EnabledVariationTagsOverride);
            }
        }

        [Fact]
        public void GetPolicy_SectionStart_EnablesCrashTag()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext(barNumber: 1, barWithinSection: 0);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Snare);

            // Assert - section start should enable crash tag
            Assert.NotNull(decision!.EnabledVariationTagsOverride);
            Assert.Contains("SectionStart", decision.EnabledVariationTagsOverride);
        }

        #endregion

        #region Memory Integration Tests

        [Fact]
        public void GetPolicy_RecentFillInMemory_DisablesFills()
        {
            // Arrange
            var memory = new AgentMemory();
            var recentFill = new FillShape(
                BarPosition: 5,
                RolesInvolved: new List<string> { GrooveRoles.Snare },
                DensityLevel: 0.7,
                DurationBars: 1,
                FillTag: "TurnaroundFillShort");
            memory.RecordFillShape(recentFill);

            var settings = new DrummerPolicySettings
            {
                AllowConsecutiveFills = false,
                MinBarsBetweenFills = 4
            };
            var provider = new DrummerPolicyProvider(StyleConfigurationLibrary.PopRock, memory, settings);

            // Bar 6 is only 1 bar after last fill (should be blocked)
            var barContext = CreateBarContext(barNumber: 6, barsUntilSectionEnd: 0);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Snare);

            // Assert - fill tags should not be present due to memory
            if (decision!.EnabledVariationTagsOverride != null)
            {
                Assert.DoesNotContain("Fill", decision.EnabledVariationTagsOverride);
            }
        }

        [Fact]
        public void GetPolicy_NoRecentFill_AllowsFills()
        {
            // Arrange
            var memory = new AgentMemory(); // Empty memory
            var provider = new DrummerPolicyProvider(StyleConfigurationLibrary.PopRock, memory);
            var barContext = CreateBarContext(barNumber: 8, barsUntilSectionEnd: 0);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Snare);

            // Assert - fill tags should be present (in fill window, no recent fill)
            Assert.NotNull(decision!.EnabledVariationTagsOverride);
            Assert.Contains("Fill", decision.EnabledVariationTagsOverride);
        }

        #endregion

        #region Timing Feel Override Tests

        [Fact]
        public void GetPolicy_SnareRole_PopRock_ReturnsBehindTiming()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext();

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Snare);

            // Assert - PopRock snare should have Behind timing feel
            Assert.Equal(TimingFeel.Behind, decision!.RoleTimingFeelOverride);
        }

        [Fact]
        public void GetPolicy_KickRole_PopRock_NoTimingOverride()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext();

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Kick);

            // Assert - Kick should not have timing override
            Assert.Null(decision!.RoleTimingFeelOverride);
        }

        #endregion

        #region Velocity Bias Override Tests

        [Fact]
        public void GetPolicy_ChorusSection_PositiveVelocityBias()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext(sectionType: MusicConstants.eSectionType.Chorus);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Kick);

            // Assert - chorus should have positive velocity bias (louder)
            Assert.NotNull(decision!.VelocityBiasOverride);
            Assert.True(decision.VelocityBiasOverride > 0);
        }

        [Fact]
        public void GetPolicy_BridgeSection_NegativeVelocityBias()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext(sectionType: MusicConstants.eSectionType.Bridge);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Kick);

            // Assert - bridge should have negative velocity bias (softer)
            Assert.NotNull(decision!.VelocityBiasOverride);
            Assert.True(decision.VelocityBiasOverride < 0);
        }

        [Fact]
        public void GetPolicy_VerseSection_NoVelocityBias()
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext(sectionType: MusicConstants.eSectionType.Verse);

            // Act
            var decision = provider.GetPolicy(barContext, GrooveRoles.Kick);

            // Assert - verse should have no velocity bias
            Assert.Null(decision!.VelocityBiasOverride);
        }

        #endregion

        #region All Drum Roles Tests

        [Theory]
        [InlineData("Kick")]
        [InlineData("Snare")]
        [InlineData("ClosedHat")]
        [InlineData("OpenHat")]
        [InlineData("Crash")]
        [InlineData("Ride")]
        [InlineData("Tom1")]
        [InlineData("Tom2")]
        [InlineData("FloorTom")]
        public void GetPolicy_AllDrumRoles_ReturnValidDecision(string role)
        {
            // Arrange
            var provider = CreateProvider();
            var barContext = CreateBarContext();

            // Act
            var decision = provider.GetPolicy(barContext, role);

            // Assert
            Assert.NotNull(decision);
            Assert.True(decision.HasAnyOverrides || decision == DrumPolicyDecision.NoOverrides);
        }

        #endregion

        #region Helper Methods

        private static DrummerPolicyProvider CreateProvider()
        {
            return new DrummerPolicyProvider(StyleConfigurationLibrary.PopRock);
        }

        private static GrooveBarContext CreateBarContext(
            int barNumber = 1,
            MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse,
            int barWithinSection = 0,
            int barsUntilSectionEnd = 4)
        {
            var section = new Section
            {
                SectionId = 1,
                SectionType = sectionType,
                StartBar = 1,
                BarCount = 8
            };

            return new GrooveBarContext(
                BarNumber: barNumber,
                Section: section,
                SegmentProfile: null,
                BarWithinSection: barWithinSection,
                BarsUntilSectionEnd: barsUntilSectionEnd);
        }

        #endregion
    }
}


