// AI: purpose=Unit tests for Story 4.1 LimbModel, Limb enum, and LimbAssignment types.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums.Physicality for types under test.
// AI: change=Story 4.1 acceptance criteria: model has correct defaults, GetRequiredLimb works.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Physicality;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Physicality.Tests
{
    /// <summary>
    /// Story 4.1: Tests for LimbModel and LimbAssignment types.
    /// Verifies role→limb mapping, default configurations, and assignment creation.
    /// </summary>
    public class LimbModelTests
    {
        #region Limb Enum Tests

        [Fact]
        public void Limb_HasFourValues()
        {
            var values = Enum.GetValues<Limb>();
            Assert.Equal(4, values.Length);
        }

        [Theory]
        [InlineData(Limb.RightHand, 0)]
        [InlineData(Limb.LeftHand, 1)]
        [InlineData(Limb.RightFoot, 2)]
        [InlineData(Limb.LeftFoot, 3)]
        public void Limb_HasCorrectValues(Limb limb, int expectedValue)
        {
            Assert.Equal(expectedValue, (int)limb);
        }

        #endregion

        #region LimbModel Default Mapping Tests

        [Fact]
        public void LimbModel_Default_HasExpectedMappings()
        {
            var model = LimbModel.Default;

            // Right hand plays cymbals
            Assert.Equal(Limb.RightHand, model.GetRequiredLimb(GrooveRoles.ClosedHat));
            Assert.Equal(Limb.RightHand, model.GetRequiredLimb(GrooveRoles.OpenHat));
            Assert.Equal(Limb.RightHand, model.GetRequiredLimb(GrooveRoles.Ride));
            Assert.Equal(Limb.RightHand, model.GetRequiredLimb(GrooveRoles.Crash));

            // Left hand plays snare and toms
            Assert.Equal(Limb.LeftHand, model.GetRequiredLimb(GrooveRoles.Snare));
            Assert.Equal(Limb.LeftHand, model.GetRequiredLimb(GrooveRoles.Tom1));
            Assert.Equal(Limb.LeftHand, model.GetRequiredLimb(GrooveRoles.Tom2));
            Assert.Equal(Limb.LeftHand, model.GetRequiredLimb(GrooveRoles.FloorTom));

            // Right foot plays kick
            Assert.Equal(Limb.RightFoot, model.GetRequiredLimb(GrooveRoles.Kick));
        }

        [Fact]
        public void LimbModel_Default_RoleLimbMapping_ContainsAllDrumRoles()
        {
            var model = LimbModel.Default;
            var mapping = model.RoleLimbMapping;

            Assert.True(mapping.ContainsKey(GrooveRoles.Kick));
            Assert.True(mapping.ContainsKey(GrooveRoles.Snare));
            Assert.True(mapping.ContainsKey(GrooveRoles.ClosedHat));
            Assert.True(mapping.ContainsKey(GrooveRoles.OpenHat));
            Assert.True(mapping.ContainsKey(GrooveRoles.Crash));
            Assert.True(mapping.ContainsKey(GrooveRoles.Ride));
            Assert.True(mapping.ContainsKey(GrooveRoles.Tom1));
            Assert.True(mapping.ContainsKey(GrooveRoles.Tom2));
            Assert.True(mapping.ContainsKey(GrooveRoles.FloorTom));
        }

        #endregion

        #region GetRequiredLimb Tests

        [Fact]
        public void GetRequiredLimb_UnknownRole_ReturnsNull()
        {
            var model = LimbModel.Default;

            Assert.Null(model.GetRequiredLimb("UnknownRole"));
            Assert.Null(model.GetRequiredLimb(""));
            Assert.Null(model.GetRequiredLimb(null!));
        }

        [Theory]
        [InlineData(GrooveRoles.Kick, Limb.RightFoot)]
        [InlineData(GrooveRoles.Snare, Limb.LeftHand)]
        [InlineData(GrooveRoles.ClosedHat, Limb.RightHand)]
        [InlineData(GrooveRoles.FloorTom, Limb.LeftHand)]
        public void GetRequiredLimb_KnownRole_ReturnsCorrectLimb(string role, Limb expectedLimb)
        {
            var model = LimbModel.Default;
            Assert.Equal(expectedLimb, model.GetRequiredLimb(role));
        }

        #endregion

        #region WithRoleMapping Tests

        [Fact]
        public void WithRoleMapping_AddsNewMapping()
        {
            var model = LimbModel.Default;
            var newModel = model.WithRoleMapping("HiHatPedal", Limb.LeftFoot);

            Assert.Equal(Limb.LeftFoot, newModel.GetRequiredLimb("HiHatPedal"));
            Assert.Null(model.GetRequiredLimb("HiHatPedal")); // Original unchanged
        }

        [Fact]
        public void WithRoleMapping_OverridesExistingMapping()
        {
            var model = LimbModel.Default;
            var newModel = model.WithRoleMapping(GrooveRoles.Snare, Limb.RightHand);

            Assert.Equal(Limb.RightHand, newModel.GetRequiredLimb(GrooveRoles.Snare));
            Assert.Equal(Limb.LeftHand, model.GetRequiredLimb(GrooveRoles.Snare)); // Original unchanged
        }

        [Fact]
        public void WithRoleMapping_NullRole_ThrowsArgumentNullException()
        {
            var model = LimbModel.Default;
            Assert.Throws<ArgumentNullException>(() => model.WithRoleMapping(null!, Limb.RightHand));
        }

        #endregion

        #region LeftHanded Model Tests

        [Fact]
        public void LimbModel_LeftHanded_HasReversedHandMappings()
        {
            var model = LimbModel.LeftHanded;

            // Left hand plays cymbals (reversed)
            Assert.Equal(Limb.LeftHand, model.GetRequiredLimb(GrooveRoles.ClosedHat));
            Assert.Equal(Limb.LeftHand, model.GetRequiredLimb(GrooveRoles.OpenHat));
            Assert.Equal(Limb.LeftHand, model.GetRequiredLimb(GrooveRoles.Ride));
            Assert.Equal(Limb.LeftHand, model.GetRequiredLimb(GrooveRoles.Crash));

            // Right hand plays snare and toms (reversed)
            Assert.Equal(Limb.RightHand, model.GetRequiredLimb(GrooveRoles.Snare));
            Assert.Equal(Limb.RightHand, model.GetRequiredLimb(GrooveRoles.Tom1));
            Assert.Equal(Limb.RightHand, model.GetRequiredLimb(GrooveRoles.Tom2));
            Assert.Equal(Limb.RightHand, model.GetRequiredLimb(GrooveRoles.FloorTom));

            // Right foot unchanged
            Assert.Equal(Limb.RightFoot, model.GetRequiredLimb(GrooveRoles.Kick));
        }

        #endregion

        #region LimbAssignment Tests

        [Fact]
        public void LimbAssignment_IsSamePosition_SameBarAndBeat_ReturnsTrue()
        {
            var a1 = new LimbAssignment(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand);
            var a2 = new LimbAssignment(1, 2.0m, GrooveRoles.ClosedHat, Limb.RightHand);

            Assert.True(a1.IsSamePosition(a2));
        }

        [Fact]
        public void LimbAssignment_IsSamePosition_DifferentBar_ReturnsFalse()
        {
            var a1 = new LimbAssignment(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand);
            var a2 = new LimbAssignment(2, 2.0m, GrooveRoles.Snare, Limb.LeftHand);

            Assert.False(a1.IsSamePosition(a2));
        }

        [Fact]
        public void LimbAssignment_IsSamePosition_DifferentBeat_ReturnsFalse()
        {
            var a1 = new LimbAssignment(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand);
            var a2 = new LimbAssignment(1, 2.5m, GrooveRoles.Snare, Limb.LeftHand);

            Assert.False(a1.IsSamePosition(a2));
        }

        [Fact]
        public void LimbAssignment_ConflictsWith_SameLimbSamePosition_ReturnsTrue()
        {
            var a1 = new LimbAssignment(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand);
            var a2 = new LimbAssignment(1, 2.0m, GrooveRoles.Tom1, Limb.LeftHand);

            Assert.True(a1.ConflictsWith(a2));
        }

        [Fact]
        public void LimbAssignment_ConflictsWith_DifferentLimbsSamePosition_ReturnsFalse()
        {
            var a1 = new LimbAssignment(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand);
            var a2 = new LimbAssignment(1, 2.0m, GrooveRoles.ClosedHat, Limb.RightHand);

            Assert.False(a1.ConflictsWith(a2));
        }

        [Fact]
        public void LimbAssignment_ConflictsWith_SameLimbDifferentPosition_ReturnsFalse()
        {
            var a1 = new LimbAssignment(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand);
            var a2 = new LimbAssignment(1, 4.0m, GrooveRoles.Snare, Limb.LeftHand);

            Assert.False(a1.ConflictsWith(a2));
        }

        #endregion

        #region LimbAssignment.FromCandidate Tests

        [Fact]
        public void LimbAssignment_FromCandidate_KnownRole_ReturnsAssignment()
        {
            var candidate = DrumCandidate.CreateMinimal(
                role: GrooveRoles.Snare,
                barNumber: 3,
                beat: 2.5m);
            var model = LimbModel.Default;

            var result = LimbAssignment.FromCandidate(candidate, model);

            Assert.NotNull(result);
            Assert.Equal(3, result.Value.BarNumber);
            Assert.Equal(2.5m, result.Value.Beat);
            Assert.Equal(GrooveRoles.Snare, result.Value.Role);
            Assert.Equal(Limb.LeftHand, result.Value.Limb);
        }

        [Fact]
        public void LimbAssignment_FromCandidate_UnknownRole_ReturnsNull()
        {
            var candidate = DrumCandidate.CreateMinimal(role: "UnknownRole");
            var model = LimbModel.Default;

            var result = LimbAssignment.FromCandidate(candidate, model);

            Assert.Null(result);
        }

        [Fact]
        public void LimbAssignment_FromCandidate_NullCandidate_ThrowsArgumentNullException()
        {
            var model = LimbModel.Default;
            Assert.Throws<ArgumentNullException>(() => LimbAssignment.FromCandidate(null!, model));
        }

        [Fact]
        public void LimbAssignment_FromCandidate_NullModel_ThrowsArgumentNullException()
        {
            var candidate = DrumCandidate.CreateMinimal();
            Assert.Throws<ArgumentNullException>(() => LimbAssignment.FromCandidate(candidate, null!));
        }

        #endregion

        #region Custom Mapping Tests

        [Fact]
        public void LimbModel_CustomMapping_OverridesDefaults()
        {
            var customMapping = new Dictionary<string, Limb>
            {
                [GrooveRoles.Kick] = Limb.LeftFoot,  // Double pedal: left foot for kick
                [GrooveRoles.Snare] = Limb.LeftHand
            };

            var model = new LimbModel(customMapping);

            Assert.Equal(Limb.LeftFoot, model.GetRequiredLimb(GrooveRoles.Kick));
            Assert.Null(model.GetRequiredLimb(GrooveRoles.ClosedHat)); // Not in custom mapping
        }

        [Fact]
        public void LimbModel_DefaultConstructor_UsesDefaultMapping()
        {
            var model = new LimbModel();
            Assert.Equal(Limb.RightFoot, model.GetRequiredLimb(GrooveRoles.Kick));
        }

        #endregion
    }
}

