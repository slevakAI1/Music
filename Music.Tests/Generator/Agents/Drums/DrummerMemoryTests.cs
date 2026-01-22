// AI: purpose=Unit tests for Story 2.5 DrummerMemory (drummer-specific anti-repetition memory).
// AI: deps=xUnit for test framework; DrummerMemory, FillShape, HatMode, HatSubdivision for types under test.
// AI: change=Story 2.5 acceptance criteria: fill tracking, anti-repetition, crash patterns, hat history, ghost frequency.

using Xunit;
using Music.Generator.Agents.Common;
using Music.Generator.Agents.Drums;
using Music;

namespace Music.Generator.Agents.Drums.Tests
{
    /// <summary>
    /// Story 2.5: Tests for DrummerMemory drummer-specific anti-repetition system.
    /// Verifies fill tracking, section-based anti-repetition, crash patterns, hat mode history, and ghost frequency.
    /// </summary>
    [Collection("RngDependentTests")]
    public class DrummerMemoryTests
    {
        public DrummerMemoryTests()
        {
            Rng.Initialize(42);
        }

        #region Constructor Tests

        [Fact]
        public void DrummerMemory_DefaultConstructor_CreatesValidInstance()
        {
            // Act
            var memory = new DrummerMemory();

            // Assert
            Assert.Equal(0, memory.LastFillBar);
            Assert.Null(memory.PreviousSectionFillShape);
            Assert.Empty(memory.ChorusCrashPattern);
            Assert.False(memory.IsChorusCrashPatternEstablished);
            Assert.Empty(memory.HatModeHistory);
            Assert.Equal(0.0, memory.GhostNoteFrequency);
        }

        [Fact]
        public void DrummerMemory_CustomSettings_Accepted()
        {
            // Act
            var memory = new DrummerMemory(
                operatorWindowSize: 4,
                fillLookbackBars: 16,
                ghostWindowSize: 12,
                decayFactor: 0.6,
                fillShapeTolerance: 0.15);

            // Assert - no exception, valid instance
            Assert.NotNull(memory);
        }

        [Fact]
        public void DrummerMemory_InvalidFillLookbackBars_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DrummerMemory(fillLookbackBars: 0));
        }

        [Fact]
        public void DrummerMemory_InvalidGhostWindowSize_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DrummerMemory(ghostWindowSize: 0));
        }

        [Fact]
        public void DrummerMemory_InvalidFillShapeTolerance_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DrummerMemory(fillShapeTolerance: -0.1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DrummerMemory(fillShapeTolerance: 1.5));
        }

        #endregion

        #region Fill Recording Tests

        [Fact]
        public void RecordFill_UpdatesLastFillBar()
        {
            // Arrange
            var memory = new DrummerMemory();
            var fillShape = CreateTestFillShape(bar: 8);

            // Act
            memory.RecordFill(8, fillShape, MusicConstants.eSectionType.Verse);

            // Assert
            Assert.Equal(8, memory.LastFillBar);
        }

        [Fact]
        public void RecordFill_UpdatesLastFillShape()
        {
            // Arrange
            var memory = new DrummerMemory();
            var fillShape = CreateTestFillShape(bar: 8, roles: new[] { "Snare", "Kick" });

            // Act
            memory.RecordFill(8, fillShape, MusicConstants.eSectionType.Verse);

            // Assert
            var lastFill = memory.GetLastFillShape();
            Assert.NotNull(lastFill);
            Assert.Equal(8, lastFill.BarPosition);
            Assert.Contains("Snare", lastFill.RolesInvolved);
            Assert.Contains("Kick", lastFill.RolesInvolved);
        }

        [Fact]
        public void RecordFill_MultipleFillsSameBar_RecordsLast()
        {
            // Arrange
            var memory = new DrummerMemory();
            var fill1 = CreateTestFillShape(bar: 8, tag: "SnareRoll");
            var fill2 = CreateTestFillShape(bar: 8, tag: "TomPattern");

            // Act
            memory.RecordFill(8, fill1, MusicConstants.eSectionType.Verse);
            memory.RecordFill(8, fill2, MusicConstants.eSectionType.Verse);

            // Assert - last fill wins
            var lastFill = memory.GetLastFillShape();
            Assert.NotNull(lastFill);
            Assert.Equal("TomPattern", lastFill.FillTag);
        }

        [Fact]
        public void RecordFill_InvalidBarNumber_Throws()
        {
            // Arrange
            var memory = new DrummerMemory();
            var fillShape = CreateTestFillShape(bar: 1);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                memory.RecordFill(0, fillShape, MusicConstants.eSectionType.Verse));
        }

        [Fact]
        public void RecordFill_NullFillShape_Throws()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                memory.RecordFill(8, null!, MusicConstants.eSectionType.Verse));
        }

        #endregion

        #region Anti-Repetition Tests

        [Fact]
        public void WouldRepeatPreviousSectionFill_NoPreviousFill_ReturnsFalse()
        {
            // Arrange
            var memory = new DrummerMemory();
            var proposedFill = CreateTestFillShape(bar: 16);

            // Act
            bool wouldRepeat = memory.WouldRepeatPreviousSectionFill(proposedFill);

            // Assert
            Assert.False(wouldRepeat);
        }

        [Fact]
        public void WouldRepeatPreviousSectionFill_SameSection_ReturnsFalse()
        {
            // Arrange
            var memory = new DrummerMemory();
            var fill1 = CreateTestFillShape(bar: 8, roles: new[] { "Snare", "Kick" });
            var fill2 = CreateTestFillShape(bar: 16, roles: new[] { "Snare", "Kick" });

            // Both fills in same section (Verse)
            memory.RecordFill(8, fill1, MusicConstants.eSectionType.Verse);

            // Act - still in same section, no previous section fill yet
            bool wouldRepeat = memory.WouldRepeatPreviousSectionFill(fill2);

            // Assert
            Assert.False(wouldRepeat);
        }

        [Fact]
        public void WouldRepeatPreviousSectionFill_DifferentSection_SameFill_ReturnsTrue()
        {
            // Arrange
            var memory = new DrummerMemory();
            var verseFill = CreateTestFillShape(bar: 8, roles: new[] { "Snare", "Kick" }, density: 0.6);

            // Record fill in verse
            memory.RecordFill(8, verseFill, MusicConstants.eSectionType.Verse);

            // Move to chorus - this saves verse fill as previous section fill
            var chorusFill = CreateTestFillShape(bar: 16, roles: new[] { "Snare", "Kick" }, density: 0.6);
            memory.RecordFill(16, chorusFill, MusicConstants.eSectionType.Chorus);

            // Now proposing same fill for next section
            var proposedFill = CreateTestFillShape(bar: 24, roles: new[] { "Snare", "Kick" }, density: 0.6);

            // Move to bridge - chorusFill becomes previous
            var bridgeFill = CreateTestFillShape(bar: 32, roles: new[] { "Toms" }, density: 0.4);
            memory.RecordFill(32, bridgeFill, MusicConstants.eSectionType.Bridge);

            // Act - check if same as the previous (chorus) fill
            bool wouldRepeat = memory.WouldRepeatPreviousSectionFill(chorusFill);

            // Assert
            Assert.True(wouldRepeat);
        }

        [Fact]
        public void WouldRepeatPreviousSectionFill_DifferentRoles_ReturnsFalse()
        {
            // Arrange
            var memory = new DrummerMemory();
            var verseFill = CreateTestFillShape(bar: 8, roles: new[] { "Snare", "Kick" });
            memory.RecordFill(8, verseFill, MusicConstants.eSectionType.Verse);

            // Move to chorus
            var chorusFill = CreateTestFillShape(bar: 16, roles: new[] { "Toms", "Kick" }); // Different roles
            memory.RecordFill(16, chorusFill, MusicConstants.eSectionType.Chorus);

            // Act - check if toms fill would repeat snare fill
            var proposedFill = CreateTestFillShape(bar: 24, roles: new[] { "Toms" });

            // Assert - different roles = not a repeat
            Assert.False(memory.WouldRepeatPreviousSectionFill(proposedFill));
        }

        [Fact]
        public void WouldRepeatPreviousSectionFill_DensityWithinTolerance_ReturnsTrue()
        {
            // Arrange
            var memory = new DrummerMemory(fillShapeTolerance: 0.1);
            var verseFill = CreateTestFillShape(bar: 8, roles: new[] { "Snare" }, density: 0.5);
            memory.RecordFill(8, verseFill, MusicConstants.eSectionType.Verse);

            var chorusFill = CreateTestFillShape(bar: 16, roles: new[] { "Toms" }, density: 0.3);
            memory.RecordFill(16, chorusFill, MusicConstants.eSectionType.Chorus);

            // Act - propose fill with density 0.55 (within 0.1 of 0.5)
            var proposedFill = CreateTestFillShape(bar: 24, roles: new[] { "Snare" }, density: 0.55);

            // Assert
            Assert.True(memory.WouldRepeatPreviousSectionFill(proposedFill));
        }

        [Fact]
        public void WouldRepeatPreviousSectionFill_DensityOutsideTolerance_ReturnsFalse()
        {
            // Arrange
            var memory = new DrummerMemory(fillShapeTolerance: 0.1);
            var verseFill = CreateTestFillShape(bar: 8, roles: new[] { "Snare" }, density: 0.5);
            memory.RecordFill(8, verseFill, MusicConstants.eSectionType.Verse);

            var chorusFill = CreateTestFillShape(bar: 16, roles: new[] { "Toms" }, density: 0.3);
            memory.RecordFill(16, chorusFill, MusicConstants.eSectionType.Chorus);

            // Act - propose fill with density 0.7 (outside 0.1 of 0.5)
            var proposedFill = CreateTestFillShape(bar: 24, roles: new[] { "Snare" }, density: 0.7);

            // Assert
            Assert.False(memory.WouldRepeatPreviousSectionFill(proposedFill));
        }

        [Fact]
        public void GetFillRepetitionPenalty_NoRepeat_ReturnsZero()
        {
            // Arrange
            var memory = new DrummerMemory();
            var proposedFill = CreateTestFillShape(bar: 8);

            // Act
            double penalty = memory.GetFillRepetitionPenalty(proposedFill);

            // Assert
            Assert.Equal(0.0, penalty);
        }

        [Fact]
        public void GetFillRepetitionPenalty_WouldRepeat_ReturnsHighPenalty()
        {
            // Arrange
            var memory = new DrummerMemory();
            var verseFill = CreateTestFillShape(bar: 8, roles: new[] { "Snare" }, density: 0.5);
            memory.RecordFill(8, verseFill, MusicConstants.eSectionType.Verse);

            var chorusFill = CreateTestFillShape(bar: 16, roles: new[] { "Toms" });
            memory.RecordFill(16, chorusFill, MusicConstants.eSectionType.Chorus);

            // Act - same shape as verse fill
            var proposedFill = CreateTestFillShape(bar: 24, roles: new[] { "Snare" }, density: 0.5);
            double penalty = memory.GetFillRepetitionPenalty(proposedFill);

            // Assert
            Assert.Equal(0.8, penalty);
        }

        #endregion

        #region Crash Pattern Tests

        [Fact]
        public void RecordCrashHit_NonChorus_DoesNotRecord()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act - record crash in verse (not chorus)
            memory.RecordCrashHit(1.0m, MusicConstants.eSectionType.Verse);

            // Assert
            Assert.Empty(memory.ChorusCrashPattern);
            Assert.False(memory.IsChorusCrashPatternEstablished);
        }

        [Fact]
        public void RecordCrashHit_Chorus_RecordsPattern()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act
            memory.RecordCrashHit(1.0m, MusicConstants.eSectionType.Chorus);

            // Assert
            Assert.Single(memory.ChorusCrashPattern);
            Assert.Contains(1.0m, memory.ChorusCrashPattern);
        }

        [Fact]
        public void RecordCrashHit_MultipleCrashes_EstablishesPattern()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act - record 2+ crashes to establish pattern
            memory.RecordCrashHit(1.0m, MusicConstants.eSectionType.Chorus);
            memory.RecordCrashHit(3.0m, MusicConstants.eSectionType.Chorus);

            // Assert
            Assert.True(memory.IsChorusCrashPatternEstablished);
            Assert.Equal(2, memory.ChorusCrashPattern.Count);
            Assert.Contains(1.0m, memory.ChorusCrashPattern);
            Assert.Contains(3.0m, memory.ChorusCrashPattern);
        }

        [Fact]
        public void RecordCrashHit_SortedByBeat()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act - record out of order
            memory.RecordCrashHit(3.0m, MusicConstants.eSectionType.Chorus);
            memory.RecordCrashHit(1.0m, MusicConstants.eSectionType.Chorus);
            memory.RecordCrashHit(2.0m, MusicConstants.eSectionType.Chorus);

            // Assert - sorted
            var pattern = memory.ChorusCrashPattern.ToList();
            Assert.Equal(1.0m, pattern[0]);
            Assert.Equal(2.0m, pattern[1]);
            Assert.Equal(3.0m, pattern[2]);
        }

        [Fact]
        public void RecordCrashHit_InvalidBeat_Throws()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                memory.RecordCrashHit(0.5m, MusicConstants.eSectionType.Chorus));
        }

        [Fact]
        public void IsCrashBeatInPattern_NoPatternEstablished_ReturnsTrue()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act - no pattern yet, any beat should be acceptable
            bool result = memory.IsCrashBeatInPattern(2.5m);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCrashBeatInPattern_BeatInPattern_ReturnsTrue()
        {
            // Arrange
            var memory = new DrummerMemory();
            memory.RecordCrashHit(1.0m, MusicConstants.eSectionType.Chorus);
            memory.RecordCrashHit(3.0m, MusicConstants.eSectionType.Chorus);

            // Act
            bool result = memory.IsCrashBeatInPattern(1.0m);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCrashBeatInPattern_BeatNotInPattern_ReturnsFalse()
        {
            // Arrange
            var memory = new DrummerMemory();
            memory.RecordCrashHit(1.0m, MusicConstants.eSectionType.Chorus);
            memory.RecordCrashHit(3.0m, MusicConstants.eSectionType.Chorus);

            // Act
            bool result = memory.IsCrashBeatInPattern(2.0m);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Hat Mode History Tests

        [Fact]
        public void RecordHatModeChange_AddsToHistory()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act
            memory.RecordHatModeChange(1, HatMode.Closed, HatSubdivision.Eighth);

            // Assert
            Assert.Single(memory.HatModeHistory);
            Assert.Equal(1, memory.HatModeHistory[0].BarNumber);
            Assert.Equal(HatMode.Closed, memory.HatModeHistory[0].Mode);
            Assert.Equal(HatSubdivision.Eighth, memory.HatModeHistory[0].Subdivision);
        }

        [Fact]
        public void RecordHatModeChange_SameMode_DoesNotDuplicate()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act - record same mode twice
            memory.RecordHatModeChange(1, HatMode.Closed, HatSubdivision.Eighth);
            memory.RecordHatModeChange(2, HatMode.Closed, HatSubdivision.Eighth);

            // Assert - only one entry
            Assert.Single(memory.HatModeHistory);
        }

        [Fact]
        public void RecordHatModeChange_DifferentMode_RecordsBoth()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act
            memory.RecordHatModeChange(1, HatMode.Closed, HatSubdivision.Eighth);
            memory.RecordHatModeChange(9, HatMode.Ride, HatSubdivision.Eighth);

            // Assert
            Assert.Equal(2, memory.HatModeHistory.Count);
            Assert.Equal(HatMode.Closed, memory.HatModeHistory[0].Mode);
            Assert.Equal(HatMode.Ride, memory.HatModeHistory[1].Mode);
        }

        [Fact]
        public void RecordHatModeChange_DifferentSubdivision_RecordsBoth()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act - same mode, different subdivision
            memory.RecordHatModeChange(1, HatMode.Closed, HatSubdivision.Eighth);
            memory.RecordHatModeChange(9, HatMode.Closed, HatSubdivision.Sixteenth);

            // Assert
            Assert.Equal(2, memory.HatModeHistory.Count);
        }

        [Fact]
        public void RecordHatModeChange_InvalidBarNumber_Throws()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                memory.RecordHatModeChange(0, HatMode.Closed, HatSubdivision.Eighth));
        }

        [Fact]
        public void GetHatModeAt_NoHistory_ReturnsNull()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act
            var result = memory.GetHatModeAt(5);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetHatModeAt_ExactMatch_ReturnsEntry()
        {
            // Arrange
            var memory = new DrummerMemory();
            memory.RecordHatModeChange(4, HatMode.Ride, HatSubdivision.Eighth);

            // Act
            var result = memory.GetHatModeAt(4);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(HatMode.Ride, result.Mode);
        }

        [Fact]
        public void GetHatModeAt_BeforeAnyEntry_ReturnsNull()
        {
            // Arrange
            var memory = new DrummerMemory();
            memory.RecordHatModeChange(5, HatMode.Ride, HatSubdivision.Eighth);

            // Act
            var result = memory.GetHatModeAt(3);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetHatModeAt_BetweenEntries_ReturnsPrevious()
        {
            // Arrange
            var memory = new DrummerMemory();
            memory.RecordHatModeChange(1, HatMode.Closed, HatSubdivision.Eighth);
            memory.RecordHatModeChange(9, HatMode.Ride, HatSubdivision.Eighth);

            // Act - bar 5 is between the two changes
            var result = memory.GetHatModeAt(5);

            // Assert - should get the first entry (bar 1)
            Assert.NotNull(result);
            Assert.Equal(HatMode.Closed, result.Mode);
            Assert.Equal(1, result.BarNumber);
        }

        #endregion

        #region Ghost Note Frequency Tests

        [Fact]
        public void GhostNoteFrequency_NoRecords_ReturnsZero()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act & Assert
            Assert.Equal(0.0, memory.GhostNoteFrequency);
        }

        [Fact]
        public void RecordGhostNotes_UpdatesFrequency()
        {
            // Arrange
            var memory = new DrummerMemory(ghostWindowSize: 4);

            // Act - record some ghost notes
            memory.RecordDecision(1, "GhostOp", "c1"); // Need to set CurrentBarNumber
            memory.RecordGhostNotes(1, 2);

            // Assert
            Assert.Equal(2.0, memory.GhostNoteFrequency);
        }

        [Fact]
        public void GhostNoteFrequency_ComputesAverage()
        {
            // Arrange
            var memory = new DrummerMemory(ghostWindowSize: 4);

            // Record decisions to set CurrentBarNumber
            memory.RecordDecision(1, "Op", "c");
            memory.RecordDecision(2, "Op", "c");
            memory.RecordDecision(3, "Op", "c");
            memory.RecordDecision(4, "Op", "c");

            // Act - record varying ghost counts
            memory.RecordGhostNotes(1, 2);
            memory.RecordGhostNotes(2, 4);
            memory.RecordGhostNotes(3, 6);
            memory.RecordGhostNotes(4, 8);

            // Assert - average of 2,4,6,8 = 5.0
            Assert.Equal(5.0, memory.GhostNoteFrequency);
        }

        [Fact]
        public void GhostNoteFrequency_RespectsWindowSize()
        {
            // Arrange
            var memory = new DrummerMemory(ghostWindowSize: 2);

            // Record decisions through bar 4
            memory.RecordDecision(1, "Op", "c");
            memory.RecordDecision(2, "Op", "c");
            memory.RecordDecision(3, "Op", "c");
            memory.RecordDecision(4, "Op", "c");

            // Record ghost notes for all bars
            memory.RecordGhostNotes(1, 10);
            memory.RecordGhostNotes(2, 20);
            memory.RecordGhostNotes(3, 2);
            memory.RecordGhostNotes(4, 4);

            // Assert - only bars 3,4 in window (size=2), average = (2+4)/2 = 3.0
            Assert.Equal(3.0, memory.GhostNoteFrequency);
        }

        [Fact]
        public void RecordGhostNotes_InvalidBarNumber_Throws()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                memory.RecordGhostNotes(0, 2));
        }

        [Fact]
        public void RecordGhostNotes_NegativeCount_Throws()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                memory.RecordGhostNotes(1, -1));
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_ResetsAllDrummerFields()
        {
            // Arrange
            var memory = new DrummerMemory();
            var fillShape = CreateTestFillShape(bar: 8);
            memory.RecordFill(8, fillShape, MusicConstants.eSectionType.Verse);
            memory.RecordCrashHit(1.0m, MusicConstants.eSectionType.Chorus);
            memory.RecordCrashHit(3.0m, MusicConstants.eSectionType.Chorus);
            memory.RecordHatModeChange(1, HatMode.Ride, HatSubdivision.Sixteenth);
            memory.RecordGhostNotes(8, 4);

            // Act
            memory.Clear();

            // Assert
            Assert.Equal(0, memory.LastFillBar);
            Assert.Null(memory.PreviousSectionFillShape);
            Assert.Null(memory.GetLastFillShape());
            Assert.Empty(memory.ChorusCrashPattern);
            Assert.False(memory.IsChorusCrashPatternEstablished);
            Assert.Empty(memory.HatModeHistory);
            Assert.Equal(0.0, memory.GhostNoteFrequency);
            Assert.Equal(0, memory.CurrentBarNumber);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void Determinism_SameSequence_ProducesSameState()
        {
            // Arrange
            var memory1 = new DrummerMemory();
            var memory2 = new DrummerMemory();

            // Act - apply identical sequences
            ApplyTestSequence(memory1);
            ApplyTestSequence(memory2);

            // Assert - same state
            Assert.Equal(memory1.LastFillBar, memory2.LastFillBar);
            Assert.Equal(memory1.ChorusCrashPattern.Count, memory2.ChorusCrashPattern.Count);
            Assert.Equal(memory1.HatModeHistory.Count, memory2.HatModeHistory.Count);
            Assert.Equal(memory1.GhostNoteFrequency, memory2.GhostNoteFrequency);
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void InheritsFromAgentMemory_RecordDecision_Works()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act - use inherited RecordDecision
            memory.RecordDecision(1, "GhostOperator", "ghost-1.75");
            memory.RecordDecision(1, "GhostOperator", "ghost-3.75");

            // Assert
            var usage = memory.GetRecentOperatorUsage(8);
            Assert.Equal(2, usage["GhostOperator"]);
        }

        [Fact]
        public void InheritsFromAgentMemory_GetRepetitionPenalty_Works()
        {
            // Arrange
            var memory = new DrummerMemory();
            memory.RecordDecision(1, "GhostOperator", "ghost-1");
            memory.RecordDecision(2, "GhostOperator", "ghost-2");
            memory.RecordDecision(3, "GhostOperator", "ghost-3");

            // Act
            double penalty = memory.GetRepetitionPenalty("GhostOperator");

            // Assert - should be positive due to recent usage
            Assert.True(penalty > 0.0);
        }

        [Fact]
        public void InheritsFromAgentMemory_SectionSignature_Works()
        {
            // Arrange
            var memory = new DrummerMemory();

            // Act
            memory.RecordSectionSignature(MusicConstants.eSectionType.Chorus, "CrashOnOne");
            memory.RecordSectionSignature(MusicConstants.eSectionType.Chorus, "HatLift");

            // Assert
            var signature = memory.GetSectionSignature(MusicConstants.eSectionType.Chorus);
            Assert.Equal(2, signature.Count);
            Assert.Contains("CrashOnOne", signature);
            Assert.Contains("HatLift", signature);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void EmptyFillShape_NotConsideredRepetition()
        {
            // Arrange
            var memory = new DrummerMemory();
            memory.RecordFill(8, FillShape.Empty, MusicConstants.eSectionType.Verse);
            memory.RecordFill(16, CreateTestFillShape(bar: 16), MusicConstants.eSectionType.Chorus);

            // Act
            bool wouldRepeat = memory.WouldRepeatPreviousSectionFill(FillShape.Empty);

            // Assert - empty fill has no content, can't repeat
            Assert.False(wouldRepeat);
        }

        [Fact]
        public void FewerSamplesThanWindow_StillComputesAverage()
        {
            // Arrange
            var memory = new DrummerMemory(ghostWindowSize: 8);
            memory.RecordDecision(1, "Op", "c");
            memory.RecordDecision(2, "Op", "c");
            memory.RecordGhostNotes(1, 4);
            memory.RecordGhostNotes(2, 8);

            // Act - only 2 samples but window is 8
            double frequency = memory.GhostNoteFrequency;

            // Assert - average of available samples = (4+8)/2 = 6.0
            Assert.Equal(6.0, frequency);
        }

        #endregion

        #region Helper Methods

        private static FillShape CreateTestFillShape(
            int bar = 1,
            string[]? roles = null,
            double density = 0.5,
            decimal duration = 1.0m,
            string? tag = null)
        {
            return new FillShape(
                BarPosition: bar,
                RolesInvolved: roles ?? new[] { "Snare", "Kick" },
                DensityLevel: density,
                DurationBars: duration,
                FillTag: tag);
        }

        private static void ApplyTestSequence(DrummerMemory memory)
        {
            memory.RecordDecision(1, "Op1", "c1");
            memory.RecordDecision(2, "Op2", "c2");
            memory.RecordFill(4, CreateTestFillShape(bar: 4), MusicConstants.eSectionType.Verse);
            memory.RecordCrashHit(1.0m, MusicConstants.eSectionType.Chorus);
            memory.RecordCrashHit(3.0m, MusicConstants.eSectionType.Chorus);
            memory.RecordHatModeChange(1, HatMode.Closed, HatSubdivision.Eighth);
            memory.RecordHatModeChange(5, HatMode.Ride, HatSubdivision.Eighth);
            memory.RecordGhostNotes(1, 2);
            memory.RecordGhostNotes(2, 4);
        }

        #endregion
    }
}
