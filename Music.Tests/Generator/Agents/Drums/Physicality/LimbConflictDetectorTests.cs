// AI: purpose=Unit tests for Story 4.1 LimbConflictDetector and LimbConflict types.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums.Physicality for types under test.
// AI: change=Story 4.1 acceptance criteria: detect conflicts, allow different limbs on same beat.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Physicality;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Physicality.Tests
{
    /// <summary>
    /// Story 4.1: Tests for LimbConflictDetector.
    /// Verifies conflict detection for simultaneous limb usage.
    /// </summary>
    public class LimbConflictDetectorTests
    {
        private readonly LimbConflictDetector _detector = LimbConflictDetector.Default;
        private readonly LimbModel _model = LimbModel.Default;

        #region Empty and Single Assignment Tests

        [Fact]
        public void DetectConflicts_EmptyList_ReturnsNoConflicts()
        {
            var assignments = new List<LimbAssignment>();

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Empty(conflicts);
        }

        [Fact]
        public void DetectConflicts_SingleAssignment_ReturnsNoConflicts()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Empty(conflicts);
        }

        [Fact]
        public void DetectConflicts_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _detector.DetectConflicts((IReadOnlyList<LimbAssignment>)null!));
        }

        #endregion

        #region Same Limb Same Position (Conflict) Tests

        [Fact]
        public void DetectConflicts_SameLimb_SameTick_DetectsConflict()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.Tom1, Limb.LeftHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Single(conflicts);
            Assert.Equal(Limb.LeftHand, conflicts[0].Limb);
            Assert.Equal(1, conflicts[0].BarNumber);
            Assert.Equal(2.0m, conflicts[0].Beat);
            Assert.Equal(2, conflicts[0].ConflictCount);
        }

        [Fact]
        public void DetectConflicts_ThreeAssignments_SameLimb_SamePosition_ReturnsOneConflict()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.Tom1, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.Tom2, Limb.LeftHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Single(conflicts);
            Assert.Equal(3, conflicts[0].ConflictCount);
            Assert.Contains(GrooveRoles.Snare, conflicts[0].ConflictingRoles);
            Assert.Contains(GrooveRoles.Tom1, conflicts[0].ConflictingRoles);
            Assert.Contains(GrooveRoles.Tom2, conflicts[0].ConflictingRoles);
        }

        [Fact]
        public void DetectConflicts_TwoConflicts_DifferentPositions_ReturnsBothConflicts()
        {
            var assignments = new List<LimbAssignment>
            {
                // Conflict 1: bar 1, beat 2
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.Tom1, Limb.LeftHand),
                // Conflict 2: bar 1, beat 4
                new(1, 4.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 4.0m, GrooveRoles.FloorTom, Limb.LeftHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Equal(2, conflicts.Count);
            Assert.Equal(2.0m, conflicts[0].Beat);
            Assert.Equal(4.0m, conflicts[1].Beat);
        }

        #endregion

        #region Different Limbs Same Position (No Conflict) Tests

        [Fact]
        public void DetectConflicts_DifferentLimbs_SameTick_NoConflict()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.ClosedHat, Limb.RightHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Empty(conflicts);
        }

        [Fact]
        public void DetectConflicts_AllFourLimbs_SamePosition_NoConflict()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 1.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 1.0m, GrooveRoles.ClosedHat, Limb.RightHand),
                new(1, 1.0m, GrooveRoles.Kick, Limb.RightFoot),
                new(1, 1.0m, "HiHatPedal", Limb.LeftFoot)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Empty(conflicts);
        }

        [Fact]
        public void DetectConflicts_HatAndSnare_SameBeat_NoConflict_DifferentLimbs()
        {
            // This is a common drum pattern: snare + hat on backbeats
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.ClosedHat, Limb.RightHand),
                new(1, 4.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 4.0m, GrooveRoles.ClosedHat, Limb.RightHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Empty(conflicts);
        }

        #endregion

        #region Same Limb Different Position (No Conflict) Tests

        [Fact]
        public void DetectConflicts_SameLimb_DifferentBeat_NoConflict()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 4.0m, GrooveRoles.Snare, Limb.LeftHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Empty(conflicts);
        }

        [Fact]
        public void DetectConflicts_SameLimb_DifferentBar_NoConflict()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(2, 2.0m, GrooveRoles.Snare, Limb.LeftHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Empty(conflicts);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void DetectConflicts_SameInputDifferentOrder_ReturnsSameConflicts()
        {
            var assignments1 = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.Tom1, Limb.LeftHand),
                new(1, 4.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 4.0m, GrooveRoles.FloorTom, Limb.LeftHand)
            };

            var assignments2 = new List<LimbAssignment>
            {
                new(1, 4.0m, GrooveRoles.FloorTom, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.Tom1, Limb.LeftHand),
                new(1, 4.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand)
            };

            var conflicts1 = _detector.DetectConflicts(assignments1);
            var conflicts2 = _detector.DetectConflicts(assignments2);

            Assert.Equal(conflicts1.Count, conflicts2.Count);
            for (int i = 0; i < conflicts1.Count; i++)
            {
                Assert.Equal(conflicts1[i].BarNumber, conflicts2[i].BarNumber);
                Assert.Equal(conflicts1[i].Beat, conflicts2[i].Beat);
                Assert.Equal(conflicts1[i].Limb, conflicts2[i].Limb);
            }
        }

        [Fact]
        public void DetectConflicts_MultipleRuns_ReturnsSameResults()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.Tom1, Limb.LeftHand)
            };

            var conflicts1 = _detector.DetectConflicts(assignments);
            var conflicts2 = _detector.DetectConflicts(assignments);

            Assert.Equal(conflicts1.Count, conflicts2.Count);
            Assert.Equal(conflicts1[0].BarNumber, conflicts2[0].BarNumber);
            Assert.Equal(conflicts1[0].Beat, conflicts2[0].Beat);
        }

        #endregion

        #region DrumCandidate Overload Tests

        [Fact]
        public void DetectConflicts_FromCandidates_DetectsConflict()
        {
            var candidates = new List<DrumCandidate>
            {
                DrumCandidate.CreateMinimal(role: GrooveRoles.Snare, barNumber: 1, beat: 2.0m),
                DrumCandidate.CreateMinimal(role: GrooveRoles.Tom1, barNumber: 1, beat: 2.0m)
            };

            var conflicts = _detector.DetectConflicts(candidates, _model);

            Assert.Single(conflicts);
            Assert.Equal(Limb.LeftHand, conflicts[0].Limb);
        }

        [Fact]
        public void DetectConflicts_FromCandidates_UnknownRole_SkipsAssignment()
        {
            var candidates = new List<DrumCandidate>
            {
                DrumCandidate.CreateMinimal(role: GrooveRoles.Snare, barNumber: 1, beat: 2.0m),
                DrumCandidate.CreateMinimal(role: "UnknownRole", barNumber: 1, beat: 2.0m)
            };

            var conflicts = _detector.DetectConflicts(candidates, _model);

            Assert.Empty(conflicts);
        }

        [Fact]
        public void DetectConflicts_FromCandidates_NullCandidates_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _detector.DetectConflicts((IReadOnlyList<DrumCandidate>)null!, _model));
        }

        [Fact]
        public void DetectConflicts_FromCandidates_NullModel_ThrowsArgumentNullException()
        {
            var candidates = new List<DrumCandidate>();
            Assert.Throws<ArgumentNullException>(() =>
                _detector.DetectConflicts(candidates, null!));
        }

        #endregion

        #region HasConflicts Tests

        [Fact]
        public void HasConflicts_WithConflict_ReturnsTrue()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.Tom1, Limb.LeftHand)
            };

            Assert.True(_detector.HasConflicts(assignments));
        }

        [Fact]
        public void HasConflicts_WithoutConflict_ReturnsFalse()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.ClosedHat, Limb.RightHand)
            };

            Assert.False(_detector.HasConflicts(assignments));
        }

        [Fact]
        public void HasConflicts_EmptyList_ReturnsFalse()
        {
            Assert.False(_detector.HasConflicts(new List<LimbAssignment>()));
        }

        [Fact]
        public void HasConflicts_SingleAssignment_ReturnsFalse()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand)
            };

            Assert.False(_detector.HasConflicts(assignments));
        }

        #endregion

        #region LimbConflict Record Tests

        [Fact]
        public void LimbConflict_ConflictingRoles_ReturnsAllRoles()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.Tom1, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.Tom2, Limb.LeftHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            var roles = conflicts[0].ConflictingRoles.ToList();
            Assert.Equal(3, roles.Count);
            Assert.Contains(GrooveRoles.Snare, roles);
            Assert.Contains(GrooveRoles.Tom1, roles);
            Assert.Contains(GrooveRoles.Tom2, roles);
        }

        #endregion

        #region Mixed Scenario Tests

        [Fact]
        public void DetectConflicts_RealisticDrumPattern_DetectsOnlyActualConflicts()
        {
            // Realistic pattern: backbeats with hat + snare, some fills
            var assignments = new List<LimbAssignment>
            {
                // Beat 1: Kick + hat (OK - different limbs)
                new(1, 1.0m, GrooveRoles.Kick, Limb.RightFoot),
                new(1, 1.0m, GrooveRoles.ClosedHat, Limb.RightHand),
                // Beat 2: Snare + hat (OK - different limbs)
                new(1, 2.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.0m, GrooveRoles.ClosedHat, Limb.RightHand),
                // Beat 3: Kick + hat (OK)
                new(1, 3.0m, GrooveRoles.Kick, Limb.RightFoot),
                new(1, 3.0m, GrooveRoles.ClosedHat, Limb.RightHand),
                // Beat 4: Snare + Tom1 (CONFLICT - both left hand)
                new(1, 4.0m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 4.0m, GrooveRoles.Tom1, Limb.LeftHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Single(conflicts);
            Assert.Equal(4.0m, conflicts[0].Beat);
            Assert.Equal(Limb.LeftHand, conflicts[0].Limb);
        }

        [Fact]
        public void DetectConflicts_OpenAndClosedHat_SameBeat_IsConflict()
        {
            // Both OpenHat and ClosedHat map to RightHand by default
            var assignments = new List<LimbAssignment>
            {
                new(1, 1.5m, GrooveRoles.ClosedHat, Limb.RightHand),
                new(1, 1.5m, GrooveRoles.OpenHat, Limb.RightHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Single(conflicts);
        }

        #endregion

        #region Fractional Beat Precision Tests

        [Fact]
        public void DetectConflicts_FractionalBeats_SamePosition_DetectsConflict()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.75m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.75m, GrooveRoles.Tom1, Limb.LeftHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Single(conflicts);
            Assert.Equal(2.75m, conflicts[0].Beat);
        }

        [Fact]
        public void DetectConflicts_SlightlyDifferentFractionalBeats_NoConflict()
        {
            var assignments = new List<LimbAssignment>
            {
                new(1, 2.750m, GrooveRoles.Snare, Limb.LeftHand),
                new(1, 2.751m, GrooveRoles.Tom1, Limb.LeftHand)
            };

            var conflicts = _detector.DetectConflicts(assignments);

            Assert.Empty(conflicts); // Different positions
        }

        #endregion
    }
}
