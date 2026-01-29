// AI: purpose=Unit tests for Story 2.2 DrumCandidate, FillRole, and DrumArticulation types.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums for types under test.
// AI: change=Story 2.2 acceptance criteria: candidates can be created and scored.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator;
using Music;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Tests
{
    /// <summary>
    /// Story 2.2: Tests for DrumCandidate, FillRole, and DrumArticulation types.
    /// Verifies creation, validation, scoring, and helper methods.
    /// </summary>
    [Collection("RngDependentTests")]
    public class DrumCandidateTests
    {
        public DrumCandidateTests()
        {
            Rng.Initialize(42);
        }

        #region DrumCandidate Creation Tests

        [Fact]
        public void DrumCandidate_CreateMinimal_ReturnsValidInstance()
        {
            // Act
            var candidate = DrumCandidate.CreateMinimal();

            // Assert
            Assert.NotNull(candidate);
            Assert.Equal("TestOperator", candidate.OperatorId);
            Assert.Equal(GrooveRoles.Snare, candidate.Role);
            Assert.Equal(1, candidate.BarNumber);
            Assert.Equal(2.0m, candidate.Beat);
            Assert.Equal(OnsetStrength.Backbeat, candidate.Strength);
            Assert.Equal(0.5, candidate.Score);
            Assert.Equal(FillRole.None, candidate.FillRole);
            Assert.Null(candidate.VelocityHint);
            Assert.Null(candidate.TimingHint);
            Assert.Null(candidate.ArticulationHint);
        }

        [Fact]
        public void DrumCandidate_CreateMinimal_WithCustomParameters()
        {
            // Act
            var candidate = DrumCandidate.CreateMinimal(
                operatorId: "GhostOperator",
                role: GrooveRoles.Kick,
                barNumber: 5,
                beat: 1.5m,
                strength: OnsetStrength.Ghost,
                score: 0.8);

            // Assert
            Assert.Equal("GhostOperator", candidate.OperatorId);
            Assert.Equal(GrooveRoles.Kick, candidate.Role);
            Assert.Equal(5, candidate.BarNumber);
            Assert.Equal(1.5m, candidate.Beat);
            Assert.Equal(OnsetStrength.Ghost, candidate.Strength);
            Assert.Equal(0.8, candidate.Score);
        }

        [Fact]
        public void DrumCandidate_FullConstruction_AllFieldsSet()
        {
            // Arrange & Act
            var candidate = new DrumCandidate
            {
                CandidateId = "FillOp_Snare_8_3",
                OperatorId = "FillOperator",
                Role = GrooveRoles.Snare,
                BarNumber = 8,
                Beat = 3.0m,
                Strength = OnsetStrength.Strong,
                VelocityHint = 100,
                TimingHint = -5,
                ArticulationHint = DrumArticulation.Rimshot,
                FillRole = FillRole.FillStart,
                Score = 0.9
            };

            // Assert
            Assert.Equal("FillOp_Snare_8_3", candidate.CandidateId);
            Assert.Equal("FillOperator", candidate.OperatorId);
            Assert.Equal(GrooveRoles.Snare, candidate.Role);
            Assert.Equal(8, candidate.BarNumber);
            Assert.Equal(3.0m, candidate.Beat);
            Assert.Equal(OnsetStrength.Strong, candidate.Strength);
            Assert.Equal(100, candidate.VelocityHint);
            Assert.Equal(-5, candidate.TimingHint);
            Assert.Equal(DrumArticulation.Rimshot, candidate.ArticulationHint);
            Assert.Equal(FillRole.FillStart, candidate.FillRole);
            Assert.Equal(0.9, candidate.Score);
        }

        [Fact]
        public void DrumCandidate_IsImmutableRecord()
        {
            // Arrange
            var candidate1 = DrumCandidate.CreateMinimal(barNumber: 1, beat: 2.0m);
            var candidate2 = DrumCandidate.CreateMinimal(barNumber: 1, beat: 2.0m);

            // Assert - records with same values are equal
            Assert.Equal(candidate1.CandidateId, candidate2.CandidateId);
            Assert.Equal(candidate1.OperatorId, candidate2.OperatorId);
            Assert.Equal(candidate1.Role, candidate2.Role);
            Assert.Equal(candidate1.BarNumber, candidate2.BarNumber);
            Assert.Equal(candidate1.Beat, candidate2.Beat);
            Assert.Equal(candidate1.Strength, candidate2.Strength);
            Assert.Equal(candidate1.Score, candidate2.Score);
        }

        #endregion

        #region CandidateId Generation Tests

        [Fact]
        public void GenerateCandidateId_BasicFormat_Correct()
        {
            // Act
            var id = DrumCandidate.GenerateCandidateId(
                operatorId: "GhostBefore",
                role: GrooveRoles.Snare,
                barNumber: 4,
                beat: 1.75m);

            // Assert
            Assert.Equal("GhostBefore_Snare_4_1.75", id);
        }

        [Fact]
        public void GenerateCandidateId_WithArticulation_IncludesArticulation()
        {
            // Act
            var id = DrumCandidate.GenerateCandidateId(
                operatorId: "BackbeatVariant",
                role: GrooveRoles.Snare,
                barNumber: 2,
                beat: 2.0m,
                articulation: DrumArticulation.Rimshot);

            // Assert
            Assert.Equal("BackbeatVariant_Snare_2_2.0_Rimshot", id);
        }

        [Fact]
        public void GenerateCandidateId_WithNoneArticulation_NoSuffix()
        {
            // Act
            var id = DrumCandidate.GenerateCandidateId(
                operatorId: "Standard",
                role: GrooveRoles.Kick,
                barNumber: 1,
                beat: 1.0m,
                articulation: DrumArticulation.None);

            // Assert
            Assert.Equal("Standard_Kick_1_1.0", id);
        }

        [Fact]
        public void GenerateCandidateId_Deterministic_SameInputsSameOutput()
        {
            // Act
            var id1 = DrumCandidate.GenerateCandidateId("Op1", GrooveRoles.Snare, 3, 2.5m);
            var id2 = DrumCandidate.GenerateCandidateId("Op1", GrooveRoles.Snare, 3, 2.5m);

            // Assert
            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GenerateCandidateId_DifferentInputs_DifferentOutput()
        {
            // Act
            var id1 = DrumCandidate.GenerateCandidateId("Op1", GrooveRoles.Snare, 3, 2.5m);
            var id2 = DrumCandidate.GenerateCandidateId("Op2", GrooveRoles.Snare, 3, 2.5m);
            var id3 = DrumCandidate.GenerateCandidateId("Op1", GrooveRoles.Kick, 3, 2.5m);

            // Assert
            Assert.NotEqual(id1, id2);
            Assert.NotEqual(id1, id3);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void TryValidate_ValidCandidate_ReturnsTrue()
        {
            // Arrange
            var candidate = DrumCandidate.CreateMinimal();

            // Act
            bool isValid = candidate.TryValidate(out string? error);

            // Assert
            Assert.True(isValid);
            Assert.Null(error);
        }

        [Fact]
        public void TryValidate_InvalidBarNumber_ReturnsFalse()
        {
            // Arrange
            var candidate = new DrumCandidate
            {
                CandidateId = "test_id",
                OperatorId = "TestOp",
                Role = GrooveRoles.Snare,
                BarNumber = 0, // Invalid: must be >= 1
                Beat = 2.0m,
                Strength = OnsetStrength.Backbeat,
                FillRole = FillRole.None,
                Score = 0.5
            };

            // Act
            bool isValid = candidate.TryValidate(out string? error);

            // Assert
            Assert.False(isValid);
            Assert.Contains("BarNumber", error);
        }

        [Fact]
        public void TryValidate_InvalidBeat_ReturnsFalse()
        {
            // Arrange
            var candidate = new DrumCandidate
            {
                CandidateId = "test_id",
                OperatorId = "TestOp",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 0.5m, // Invalid: must be >= 1.0
                Strength = OnsetStrength.Backbeat,
                FillRole = FillRole.None,
                Score = 0.5
            };

            // Act
            bool isValid = candidate.TryValidate(out string? error);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Beat", error);
        }

        [Fact]
        public void TryValidate_ScoreTooLow_ReturnsFalse()
        {
            // Arrange
            var candidate = new DrumCandidate
            {
                CandidateId = "test_id",
                OperatorId = "TestOp",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 2.0m,
                Strength = OnsetStrength.Backbeat,
                FillRole = FillRole.None,
                Score = -0.1 // Invalid: must be >= 0.0
            };

            // Act
            bool isValid = candidate.TryValidate(out string? error);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Score", error);
        }

        [Fact]
        public void TryValidate_ScoreTooHigh_ReturnsFalse()
        {
            // Arrange
            var candidate = new DrumCandidate
            {
                CandidateId = "test_id",
                OperatorId = "TestOp",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 2.0m,
                Strength = OnsetStrength.Backbeat,
                FillRole = FillRole.None,
                Score = 1.1 // Invalid: must be <= 1.0
            };

            // Act
            bool isValid = candidate.TryValidate(out string? error);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Score", error);
        }

        [Fact]
        public void TryValidate_VelocityHintTooLow_ReturnsFalse()
        {
            // Arrange
            var candidate = new DrumCandidate
            {
                CandidateId = "test_id",
                OperatorId = "TestOp",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 2.0m,
                Strength = OnsetStrength.Backbeat,
                VelocityHint = -1, // Invalid: must be >= 0
                FillRole = FillRole.None,
                Score = 0.5
            };

            // Act
            bool isValid = candidate.TryValidate(out string? error);

            // Assert
            Assert.False(isValid);
            Assert.Contains("VelocityHint", error);
        }

        [Fact]
        public void TryValidate_VelocityHintTooHigh_ReturnsFalse()
        {
            // Arrange
            var candidate = new DrumCandidate
            {
                CandidateId = "test_id",
                OperatorId = "TestOp",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 2.0m,
                Strength = OnsetStrength.Backbeat,
                VelocityHint = 128, // Invalid: must be <= 127
                FillRole = FillRole.None,
                Score = 0.5
            };

            // Act
            bool isValid = candidate.TryValidate(out string? error);

            // Assert
            Assert.False(isValid);
            Assert.Contains("VelocityHint", error);
        }

        [Fact]
        public void TryValidate_NullVelocityHint_IsValid()
        {
            // Arrange
            var candidate = DrumCandidate.CreateMinimal();

            // Act
            bool isValid = candidate.TryValidate(out string? error);

            // Assert
            Assert.True(isValid);
            Assert.Null(candidate.VelocityHint);
        }

        [Fact]
        public void TryValidate_EmptyOperatorId_ReturnsFalse()
        {
            // Arrange
            var candidate = new DrumCandidate
            {
                CandidateId = "test_id",
                OperatorId = "", // Invalid
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 2.0m,
                Strength = OnsetStrength.Backbeat,
                FillRole = FillRole.None,
                Score = 0.5
            };

            // Act
            bool isValid = candidate.TryValidate(out string? error);

            // Assert
            Assert.False(isValid);
            Assert.Contains("OperatorId", error);
        }

        #endregion

        #region FillRole Enum Tests

        [Fact]
        public void FillRole_None_IsDefaultValue()
        {
            // Assert
            Assert.Equal(0, (int)FillRole.None);
        }

        [Fact]
        public void FillRole_AllValuesDistinct()
        {
            // Arrange
            var values = Enum.GetValues<FillRole>();

            // Assert
            Assert.Equal(5, values.Length);
            Assert.Contains(FillRole.None, values);
            Assert.Contains(FillRole.Setup, values);
            Assert.Contains(FillRole.FillStart, values);
            Assert.Contains(FillRole.FillBody, values);
            Assert.Contains(FillRole.FillEnd, values);
        }

        [Fact]
        public void FillRole_ValuesAreOrdered()
        {
            // Assert - verify ordering for deterministic serialization
            Assert.True((int)FillRole.None < (int)FillRole.Setup);
            Assert.True((int)FillRole.Setup < (int)FillRole.FillStart);
            Assert.True((int)FillRole.FillStart < (int)FillRole.FillBody);
            Assert.True((int)FillRole.FillBody < (int)FillRole.FillEnd);
        }

        #endregion

        #region DrumArticulation Enum Tests

        [Fact]
        public void DrumArticulation_None_IsDefaultValue()
        {
            // Assert
            Assert.Equal(0, (int)DrumArticulation.None);
        }

        [Fact]
        public void DrumArticulation_AllValuesDistinct()
        {
            // Arrange
            var values = Enum.GetValues<DrumArticulation>();

            // Assert
            Assert.Equal(9, values.Length);
            Assert.Contains(DrumArticulation.None, values);
            Assert.Contains(DrumArticulation.Rimshot, values);
            Assert.Contains(DrumArticulation.SideStick, values);
            Assert.Contains(DrumArticulation.OpenHat, values);
            Assert.Contains(DrumArticulation.Crash, values);
            Assert.Contains(DrumArticulation.Ride, values);
            Assert.Contains(DrumArticulation.RideBell, values);
            Assert.Contains(DrumArticulation.CrashChoke, values);
            Assert.Contains(DrumArticulation.Flam, values);
        }

        #endregion

        #region Score Range Tests

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        public void DrumCandidate_ValidScoreRange_PassesValidation(double score)
        {
            // Arrange
            var candidate = DrumCandidate.CreateMinimal(score: score);

            // Act
            bool isValid = candidate.TryValidate(out string? error);

            // Assert
            Assert.True(isValid);
            Assert.Null(error);
        }

        [Theory]
        [InlineData(-0.001)]
        [InlineData(1.001)]
        [InlineData(-1.0)]
        [InlineData(2.0)]
        public void DrumCandidate_InvalidScoreRange_FailsValidation(double score)
        {
            // Arrange
            var candidate = new DrumCandidate
            {
                CandidateId = "test",
                OperatorId = "test",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 2.0m,
                Strength = OnsetStrength.Backbeat,
                FillRole = FillRole.None,
                Score = score
            };

            // Act
            bool isValid = candidate.TryValidate(out string? error);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Score", error);
        }

        #endregion

        #region OnsetStrength Usage Tests

        [Theory]
        [InlineData(OnsetStrength.Downbeat)]
        [InlineData(OnsetStrength.Backbeat)]
        [InlineData(OnsetStrength.Strong)]
        [InlineData(OnsetStrength.Offbeat)]
        [InlineData(OnsetStrength.Pickup)]
        [InlineData(OnsetStrength.Ghost)]
        public void DrumCandidate_AllOnsetStrengths_Accepted(OnsetStrength strength)
        {
            // Act
            var candidate = DrumCandidate.CreateMinimal(strength: strength);

            // Assert
            Assert.Equal(strength, candidate.Strength);
            Assert.True(candidate.TryValidate(out _));
        }

        #endregion

        #region Timing Hint Tests

        [Theory]
        [InlineData(-48)]
        [InlineData(-10)]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(48)]
        public void DrumCandidate_TimingHint_AcceptsValidRange(int timingHint)
        {
            // Arrange
            var candidate = new DrumCandidate
            {
                CandidateId = "test",
                OperatorId = "test",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 2.0m,
                Strength = OnsetStrength.Backbeat,
                TimingHint = timingHint,
                FillRole = FillRole.None,
                Score = 0.5
            };

            // Act
            bool isValid = candidate.TryValidate(out _);

            // Assert
            Assert.True(isValid);
            Assert.Equal(timingHint, candidate.TimingHint);
        }

        #endregion
    }
}

