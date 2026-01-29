// AI: purpose=Unit tests for Story 2.1 DrummerContext and DrummerContextBuilder.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums for types under test.
// AI: change=Story 2.1 acceptance criteria: context builds correctly from groove inputs.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator;
using Music;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Tests
{
    /// <summary>
    /// Story 2.1: Tests for DrummerContext and DrummerContextBuilder.
    /// Verifies context construction, field population, and edge case handling.
    /// </summary>
    [Collection("RngDependentTests")]
    public class DrummerContextTests
    {
        public DrummerContextTests()
        {
            Rng.Initialize(42);
        }

        #region DrummerContext Tests

        [Fact]
        public void DrummerContext_CreateMinimal_ReturnsValidInstance()
        {
            // Act
            var context = DrummerContext.CreateMinimal();

            // Assert
            Assert.Equal(1, context.BarNumber);
            Assert.Equal(1.0m, context.Beat);
            Assert.Equal(MusicConstants.eSectionType.Verse, context.SectionType);
            Assert.Equal(0.5, context.EnergyLevel);
            Assert.Contains(GrooveRoles.Kick, context.ActiveRoles);
            Assert.Contains(GrooveRoles.Snare, context.ActiveRoles);
            Assert.Contains(GrooveRoles.ClosedHat, context.ActiveRoles);
            Assert.Equal(HatMode.Closed, context.CurrentHatMode);
            Assert.Equal(HatSubdivision.Eighth, context.HatSubdivision);
            Assert.False(context.IsFillWindow);
            Assert.False(context.IsAtSectionBoundary);
            Assert.Equal(new List<int> { 2, 4 }, context.BackbeatBeats);
            Assert.Equal(4, context.BeatsPerBar);
        }

        [Fact]
        public void DrummerContext_CreateMinimal_WithCustomParameters()
        {
            // Act
            var customRoles = new HashSet<string> { GrooveRoles.Kick, "Crash" };
            var customBackbeats = new List<int> { 2 };
            var context = DrummerContext.CreateMinimal(
                barNumber: 5,
                sectionType: MusicConstants.eSectionType.Chorus,
                seed: 123,
                activeRoles: customRoles,
                backbeatBeats: customBackbeats);

            // Assert
            Assert.Equal(5, context.BarNumber);
            Assert.Equal(MusicConstants.eSectionType.Chorus, context.SectionType);
            Assert.Equal(123, context.Seed);
            Assert.Equal(customRoles, context.ActiveRoles);
            Assert.Equal(customBackbeats, context.BackbeatBeats);
        }

        [Fact]
        public void DrummerContext_IsImmutableRecord()
        {
            // Arrange
            var context1 = DrummerContext.CreateMinimal(barNumber: 1);
            var context2 = DrummerContext.CreateMinimal(barNumber: 1);

            // Assert - verify all scalar fields match (collections have reference equality in records)
            Assert.Equal(context1.BarNumber, context2.BarNumber);
            Assert.Equal(context1.Beat, context2.Beat);
            Assert.Equal(context1.SectionType, context2.SectionType);
            Assert.Equal(context1.PhrasePosition, context2.PhrasePosition);
            Assert.Equal(context1.EnergyLevel, context2.EnergyLevel);
            Assert.Equal(context1.CurrentHatMode, context2.CurrentHatMode);
            Assert.Equal(context1.HatSubdivision, context2.HatSubdivision);
            Assert.Equal(context1.IsFillWindow, context2.IsFillWindow);
            Assert.Equal(context1.IsAtSectionBoundary, context2.IsAtSectionBoundary);
            Assert.Equal(context1.BeatsPerBar, context2.BeatsPerBar);
        }

        [Fact]
        public void DrummerContext_NullableFieldsDefaultToNull()
        {
            // Act
            var context = DrummerContext.CreateMinimal();

            // Assert
            Assert.Null(context.LastKickBeat);
            Assert.Null(context.LastSnareBeat);
        }

        #endregion

        #region DrummerContextBuilder Basic Tests

        [Fact]
        public void DrummerContextBuilder_Build_WithMinimalInput_ReturnsValidContext()
        {
            // Arrange
            var section = new Section { SectionId = 1, SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 8 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 7);
            var input = new DrummerContextBuildInput { BarContext = barContext };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.NotNull(context);
            Assert.Equal(1, context.BarNumber);
            Assert.Equal(MusicConstants.eSectionType.Verse, context.SectionType);
            Assert.NotEmpty(context.ActiveRoles);
            Assert.NotEmpty(context.BackbeatBeats);
        }

        [Fact]
        public void DrummerContextBuilder_Build_NullInput_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DrummerContextBuilder.Build(null!));
        }

        [Fact]
        public void DrummerContextBuilder_Build_NullBarContext_Throws()
        {
            // Arrange
            var input = new DrummerContextBuildInput { BarContext = null! };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DrummerContextBuilder.Build(input));
        }

        #endregion

        #region SectionType and PhrasePosition Tests

        [Fact]
        public void DrummerContextBuilder_Build_ExtractsSectionTypeFromBarContext()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Chorus, StartBar = 1, BarCount = 4 };
            var barContext = new GrooveBarContext(BarNumber: 2, Section: section, SegmentProfile: null, BarWithinSection: 1, BarsUntilSectionEnd: 2);
            var input = new DrummerContextBuildInput { BarContext = barContext };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.Equal(MusicConstants.eSectionType.Chorus, context.SectionType);
        }

        [Fact]
        public void DrummerContextBuilder_Build_ComputesPhrasePositionStart()
        {
            // Arrange - first bar of 8-bar section
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 8 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 7);
            var input = new DrummerContextBuildInput { BarContext = barContext };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert - phrase position should be 0.0 at start
            Assert.Equal(0.0, context.PhrasePosition, 3);
        }

        [Fact]
        public void DrummerContextBuilder_Build_ComputesPhrasePositionEnd()
        {
            // Arrange - last bar of 8-bar section
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 8 };
            var barContext = new GrooveBarContext(BarNumber: 8, Section: section, SegmentProfile: null, BarWithinSection: 7, BarsUntilSectionEnd: 0);
            var input = new DrummerContextBuildInput { BarContext = barContext };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert - phrase position should be 1.0 at end
            Assert.Equal(1.0, context.PhrasePosition, 3);
        }

        [Fact]
        public void DrummerContextBuilder_Build_ComputesPhrasePositionMiddle()
        {
            // Arrange - bar 4 of 8-bar section (0-indexed = 3)
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 8 };
            var barContext = new GrooveBarContext(BarNumber: 4, Section: section, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 4);
            var input = new DrummerContextBuildInput { BarContext = barContext };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert - phrase position should be ~0.43 (3/7)
            Assert.True(context.PhrasePosition > 0.4 && context.PhrasePosition < 0.5);
        }

        [Fact]
        public void DrummerContextBuilder_Build_HandlesNullSection()
        {
            // Arrange
            var barContext = new GrooveBarContext(BarNumber: 1, Section: null, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 0);
            var input = new DrummerContextBuildInput { BarContext = barContext };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert - defaults to Verse and 0.0 phrase position
            Assert.Equal(MusicConstants.eSectionType.Verse, context.SectionType);
            Assert.Equal(0.0, context.PhrasePosition);
        }

        #endregion

        #region Backbeat Computation Tests

        [Theory]
        [InlineData(2, new int[] { 2 })]           // 2/4
        [InlineData(3, new int[] { 2 })]           // 3/4
        [InlineData(4, new int[] { 2, 4 })]        // 4/4
        [InlineData(5, new int[] { 3, 5 })]        // 5/4
        [InlineData(6, new int[] { 4 })]           // 6/8
        [InlineData(7, new int[] { 3, 5, 7 })]     // 7/8
        public void DrummerContextBuilder_Build_ComputesBackbeatBeatsForTimeSignature(int beatsPerBar, int[] expectedBackbeats)
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 3);
            var input = new DrummerContextBuildInput { BarContext = barContext, BeatsPerBar = beatsPerBar };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.Equal(expectedBackbeats.ToList(), context.BackbeatBeats);
            Assert.Equal(beatsPerBar, context.BeatsPerBar);
        }

        #endregion

        #region Fill Window and Section Boundary Tests

        [Fact]
        public void DrummerContextBuilder_Build_DetectsSectionBoundaryAtStart()
        {
            // Arrange - first bar of section
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 8 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 7);
            var input = new DrummerContextBuildInput { BarContext = barContext };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.True(context.IsAtSectionBoundary);
        }

        [Fact]
        public void DrummerContextBuilder_Build_DetectsSectionBoundaryAtEnd()
        {
            // Arrange - last bar of section
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 8 };
            var barContext = new GrooveBarContext(BarNumber: 8, Section: section, SegmentProfile: null, BarWithinSection: 7, BarsUntilSectionEnd: 0);
            var input = new DrummerContextBuildInput { BarContext = barContext };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.True(context.IsAtSectionBoundary);
        }

        [Fact]
        public void DrummerContextBuilder_Build_NoSectionBoundaryInMiddle()
        {
            // Arrange - middle bar of section
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 8 };
            var barContext = new GrooveBarContext(BarNumber: 4, Section: section, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 4);
            var input = new DrummerContextBuildInput { BarContext = barContext };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.False(context.IsAtSectionBoundary);
        }

        [Fact]
        public void DrummerContextBuilder_Build_FillWindowWithPhraseHookPolicy()
        {
            // Arrange - last bar in phrase window
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 8 };
            var barContext = new GrooveBarContext(BarNumber: 8, Section: section, SegmentProfile: null, BarWithinSection: 7, BarsUntilSectionEnd: 0);
            var phraseHookPolicy = new GroovePhraseHookPolicy
            {
                AllowFillsAtPhraseEnd = false,
                PhraseEndBarsWindow = 2,
                AllowFillsAtSectionEnd = false,
                SectionEndBarsWindow = 1
            };
            var protectionPolicy = new GrooveProtectionPolicy { PhraseHookPolicy = phraseHookPolicy };
            var input = new DrummerContextBuildInput { BarContext = barContext, ProtectionPolicy = protectionPolicy };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert - should be in fill window
            Assert.True(context.IsFillWindow);
        }

        [Fact]
        public void DrummerContextBuilder_Build_NoFillWindowWithoutPolicy()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 8 };
            var barContext = new GrooveBarContext(BarNumber: 8, Section: section, SegmentProfile: null, BarWithinSection: 7, BarsUntilSectionEnd: 0);
            var input = new DrummerContextBuildInput { BarContext = barContext, ProtectionPolicy = null };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert - no policy means no fill window detection
            Assert.False(context.IsFillWindow);
        }

        #endregion

        #region Energy-Based Hat Mode and Subdivision Tests

        [Fact]
        public void DrummerContextBuilder_Build_LowEnergyUsesNoSubdivision()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 3);
            var input = new DrummerContextBuildInput { BarContext = barContext, EnergyLevel = 0.2 };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.Equal(HatSubdivision.None, context.HatSubdivision);
            Assert.Equal(HatMode.Closed, context.CurrentHatMode);
        }

        [Fact]
        public void DrummerContextBuilder_Build_MediumEnergyUsesEighthSubdivision()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 3);
            var input = new DrummerContextBuildInput { BarContext = barContext, EnergyLevel = 0.5 };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.Equal(HatSubdivision.Eighth, context.HatSubdivision);
            Assert.Equal(HatMode.Closed, context.CurrentHatMode);
        }

        [Fact]
        public void DrummerContextBuilder_Build_HighEnergyUsesSixteenthSubdivision()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 3);
            var input = new DrummerContextBuildInput { BarContext = barContext, EnergyLevel = 0.8 };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.Equal(HatSubdivision.Sixteenth, context.HatSubdivision);
            Assert.Equal(HatMode.Ride, context.CurrentHatMode);
        }

        [Fact]
        public void DrummerContextBuilder_Build_OverrideHatModeRespected()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 3);
            var input = new DrummerContextBuildInput
            {
                BarContext = barContext,
                EnergyLevel = 0.2,
                HatModeOverride = HatMode.Ride,
                HatSubdivisionOverride = HatSubdivision.Sixteenth
            };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert - overrides take precedence over energy-based defaults
            Assert.Equal(HatMode.Ride, context.CurrentHatMode);
            Assert.Equal(HatSubdivision.Sixteenth, context.HatSubdivision);
        }

        #endregion

        #region Active Roles Tests

        [Fact]
        public void DrummerContextBuilder_Build_DefaultActiveRolesWithoutPolicy()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 3);
            var input = new DrummerContextBuildInput { BarContext = barContext };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert - default roles: Kick, Snare, ClosedHat
            Assert.Contains(GrooveRoles.Kick, context.ActiveRoles);
            Assert.Contains(GrooveRoles.Snare, context.ActiveRoles);
            Assert.Contains(GrooveRoles.ClosedHat, context.ActiveRoles);
            Assert.Equal(3, context.ActiveRoles.Count);
        }

        [Fact]
        public void DrummerContextBuilder_Build_ActiveRolesOverrideRespected()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 3);
            var customRoles = new HashSet<string> { GrooveRoles.Kick, "Crash", "Ride" };
            var input = new DrummerContextBuildInput { BarContext = barContext, ActiveRolesOverride = customRoles };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.Contains(GrooveRoles.Kick, context.ActiveRoles);
            Assert.Contains("Crash", context.ActiveRoles);
            Assert.Contains("Ride", context.ActiveRoles);
        }

        [Fact]
        public void DrummerContextBuilder_Build_InvalidRolesInOverrideFiltered()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 3);
            var customRoles = new HashSet<string> { "InvalidRole", "AnotherInvalid" };
            var input = new DrummerContextBuildInput { BarContext = barContext, ActiveRolesOverride = customRoles };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert - falls back to defaults when all custom roles are invalid
            Assert.Contains(GrooveRoles.Kick, context.ActiveRoles);
            Assert.Contains(GrooveRoles.Snare, context.ActiveRoles);
            Assert.Contains(GrooveRoles.ClosedHat, context.ActiveRoles);
        }

        #endregion

        #region LastKickBeat and LastSnareBeat Tests

        [Fact]
        public void DrummerContextBuilder_Build_PassesThroughLastKickBeat()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 };
            var barContext = new GrooveBarContext(BarNumber: 2, Section: section, SegmentProfile: null, BarWithinSection: 1, BarsUntilSectionEnd: 2);
            var input = new DrummerContextBuildInput { BarContext = barContext, LastKickBeat = 3.5m };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.Equal(3.5m, context.LastKickBeat);
        }

        [Fact]
        public void DrummerContextBuilder_Build_PassesThroughLastSnareBeat()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 4 };
            var barContext = new GrooveBarContext(BarNumber: 2, Section: section, SegmentProfile: null, BarWithinSection: 1, BarsUntilSectionEnd: 2);
            var input = new DrummerContextBuildInput { BarContext = barContext, LastSnareBeat = 2.0m };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.Equal(2.0m, context.LastSnareBeat);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void DrummerContextBuilder_Build_Deterministic_SameInputsSameResult()
        {
            // Arrange
            var section = new Section { SectionType = MusicConstants.eSectionType.Verse, StartBar = 1, BarCount = 8 };
            var barContext = new GrooveBarContext(BarNumber: 4, Section: section, SegmentProfile: null, BarWithinSection: 3, BarsUntilSectionEnd: 4);
            var input = new DrummerContextBuildInput
            {
                BarContext = barContext,
                Seed = 42,
                EnergyLevel = 0.6,
                TensionLevel = 0.3,
                BeatsPerBar = 4
            };

            // Act
            var context1 = DrummerContextBuilder.Build(input);
            var context2 = DrummerContextBuilder.Build(input);

            // Assert - verify all scalar fields match (collections have reference equality in records)
            Assert.Equal(context1.BarNumber, context2.BarNumber);
            Assert.Equal(context1.Beat, context2.Beat);
            Assert.Equal(context1.SectionType, context2.SectionType);
            Assert.Equal(context1.PhrasePosition, context2.PhrasePosition, 10);
            Assert.Equal(context1.BarsUntilSectionEnd, context2.BarsUntilSectionEnd);
            Assert.Equal(context1.EnergyLevel, context2.EnergyLevel);
            Assert.Equal(context1.TensionLevel, context2.TensionLevel);
            Assert.Equal(context1.Seed, context2.Seed);
            Assert.Equal(context1.RngStreamKey, context2.RngStreamKey);
            Assert.Equal(context1.CurrentHatMode, context2.CurrentHatMode);
            Assert.Equal(context1.HatSubdivision, context2.HatSubdivision);
            Assert.Equal(context1.IsFillWindow, context2.IsFillWindow);
            Assert.Equal(context1.IsAtSectionBoundary, context2.IsAtSectionBoundary);
            Assert.Equal(context1.BeatsPerBar, context2.BeatsPerBar);
            Assert.True(context1.ActiveRoles.SetEquals(context2.ActiveRoles));
            Assert.Equal(context1.BackbeatBeats, context2.BackbeatBeats);
        }

        #endregion

        #region Single-Bar Section Edge Case

        [Fact]
        public void DrummerContextBuilder_Build_SingleBarSection()
        {
            // Arrange - 1-bar section
            var section = new Section { SectionType = MusicConstants.eSectionType.Intro, StartBar = 1, BarCount = 1 };
            var barContext = new GrooveBarContext(BarNumber: 1, Section: section, SegmentProfile: null, BarWithinSection: 0, BarsUntilSectionEnd: 0);
            var input = new DrummerContextBuildInput { BarContext = barContext };

            // Act
            var context = DrummerContextBuilder.Build(input);

            // Assert
            Assert.Equal(0.0, context.PhrasePosition);  // Single bar section defaults to 0.0
            Assert.True(context.IsAtSectionBoundary);   // Both start and end
        }

        #endregion
    }
}

