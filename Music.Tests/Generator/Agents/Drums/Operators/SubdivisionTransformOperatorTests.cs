// AI: purpose=Unit tests for Story 3.2 SubdivisionTransform operators; verifies candidate generation and constraints.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums.Operators.SubdivisionTransform for operators under test.
// AI: change=Story 3.2 acceptance criteria: each operator produces expected patterns based on context.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Operators.SubdivisionTransform;
using Music.Generator;
using Music;

namespace Music.Generator.Agents.Drums.Operators.Tests
{
    /// <summary>
    /// Story 3.2: Tests for SubdivisionTransform operators.
    /// Verifies pattern generation, context filtering, subdivision checks, and determinism.
    /// </summary>
    [Collection("RngDependentTests")]
    public class SubdivisionTransformOperatorTests
    {
        public SubdivisionTransformOperatorTests()
        {
            Rng.Initialize(42);
        }

        #region HatLiftOperator Tests

        [Fact]
        public void HatLift_WhenHatSubdivisionIsEighthAndEnergyHigh_GeneratesSixteenthPatternForFullBar()
        {
            // Arrange
            var op = new HatLiftOperator();
            var context = CreateContext(
                energy: 0.9,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - 16 positions (4 beats × 4 16ths per beat)
            Assert.Equal(16, candidates.Count);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.ClosedHat, c.Role);
                Assert.NotNull(c.VelocityHint);
                Assert.InRange(c.Score, 0.0, 1.0);
            });

            // Verify 16th grid positions
            var expectedPositions = new[] { 1.0m, 1.25m, 1.5m, 1.75m, 2.0m, 2.25m, 2.5m, 2.75m, 3.0m, 3.25m, 3.5m, 3.75m, 4.0m, 4.25m, 4.5m, 4.75m };
            Assert.Equal(expectedPositions, candidates.Select(c => c.Beat).OrderBy(b => b));
        }

        [Fact]
        public void HatLift_ReturnsEmpty_WhenHatSubdivisionIsSixteenth()
        {
            // Arrange
            var op = new HatLiftOperator();
            var context = CreateContext(
                energy: 0.9,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Sixteenth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - cannot lift from 16ths
            Assert.Empty(candidates);
        }

        [Fact]
        public void HatLift_ReturnsEmpty_WhenEnergyLow()
        {
            // Arrange
            var op = new HatLiftOperator();
            var context = CreateContext(
                energy: 0.4,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void HatLift_ReturnsEmpty_WhenHatNotActive()
        {
            // Arrange
            var op = new HatLiftOperator();
            var context = CreateContext(
                energy: 0.9,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void HatLift_ReturnsEmpty_WhenInFillWindow()
        {
            // Arrange
            var op = new HatLiftOperator();
            var context = CreateContext(
                energy: 0.9,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth,
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void HatLift_ReturnsEmpty_WhenCurrentModeIsRide()
        {
            // Arrange
            var op = new HatLiftOperator();
            var context = CreateContext(
                energy: 0.9,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth,
                hatMode: HatMode.Ride);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void HatLift_HasHigherScore_AtSectionBoundary()
        {
            // Arrange
            var op = new HatLiftOperator();
            var normalContext = CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth);
            var boundaryContext = CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth,
                isAtSectionBoundary: true);

            // Act
            var normalAvgScore = op.GenerateCandidates(normalContext).Average(c => c.Score);
            var boundaryAvgScore = op.GenerateCandidates(boundaryContext).Average(c => c.Score);

            // Assert
            Assert.True(boundaryAvgScore > normalAvgScore, "Score should be higher at section boundary");
        }

        #endregion

        #region HatDropOperator Tests

        [Fact]
        public void HatDrop_WhenHatSubdivisionIsSixteenthAndEnergyLow_GeneratesEighthPattern()
        {
            // Arrange
            var op = new HatDropOperator();
            var context = CreateContext(
                energy: 0.2,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Sixteenth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - 8 positions (4 beats × 2 8ths per beat)
            Assert.Equal(8, candidates.Count);
            Assert.All(candidates, c => Assert.Equal(GrooveRoles.ClosedHat, c.Role));

            // Verify 8th grid positions
            var expectedPositions = new[] { 1.0m, 1.5m, 2.0m, 2.5m, 3.0m, 3.5m, 4.0m, 4.5m };
            Assert.Equal(expectedPositions, candidates.Select(c => c.Beat).OrderBy(b => b));
        }

        [Fact]
        public void HatDrop_ReturnsEmpty_WhenHatSubdivisionIsEighth()
        {
            // Arrange
            var op = new HatDropOperator();
            var context = CreateContext(
                energy: 0.2,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - cannot drop from 8ths
            Assert.Empty(candidates);
        }

        [Fact]
        public void HatDrop_ReturnsEmpty_WhenEnergyHigh()
        {
            // Arrange
            var op = new HatDropOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Sixteenth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void HatDrop_ReturnsEmpty_WhenHatNotActive()
        {
            // Arrange
            var op = new HatDropOperator();
            var context = CreateContext(
                energy: 0.2,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                hatSubdivision: HatSubdivision.Sixteenth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region RideSwapOperator Tests

        [Fact]
        public void RideSwap_WhenHatModeIsClosedAndRideActive_GeneratesRidePattern()
        {
            // Arrange
            var op = new RideSwapOperator();
            var context = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Ride, GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth,
                hatMode: HatMode.Closed);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - 8 positions (8th pattern on ride)
            Assert.Equal(8, candidates.Count);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.Ride, c.Role);
                Assert.True(c.ArticulationHint == DrumArticulation.Ride || c.ArticulationHint == DrumArticulation.RideBell);
            });
        }

        [Fact]
        public void RideSwap_UsesRideBell_OnBeatOneDownbeat()
        {
            // Arrange
            var op = new RideSwapOperator();
            var context = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Ride },
                hatSubdivision: HatSubdivision.Eighth,
                hatMode: HatMode.Closed);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();
            var beatOneCandidate = candidates.FirstOrDefault(c => c.Beat == 1.0m);

            // Assert
            Assert.NotNull(beatOneCandidate);
            Assert.Equal(DrumArticulation.RideBell, beatOneCandidate.ArticulationHint);
        }

        [Fact]
        public void RideSwap_ReturnsEmpty_WhenAlreadyOnRide()
        {
            // Arrange
            var op = new RideSwapOperator();
            var context = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Ride },
                hatSubdivision: HatSubdivision.Eighth,
                hatMode: HatMode.Ride);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void RideSwap_ReturnsEmpty_WhenRideNotActive()
        {
            // Arrange
            var op = new RideSwapOperator();
            var context = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth,
                hatMode: HatMode.Closed);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void RideSwap_Uses16thPattern_WhenSubdivisionIsSixteenth()
        {
            // Arrange
            var op = new RideSwapOperator();
            var context = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Ride },
                hatSubdivision: HatSubdivision.Sixteenth,
                hatMode: HatMode.Closed);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - 16 positions for 16th pattern
            Assert.Equal(16, candidates.Count);
        }

        [Fact]
        public void RideSwap_HasHigherScore_InChorus()
        {
            // Arrange
            var op = new RideSwapOperator();
            var verseContext = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Ride },
                hatSubdivision: HatSubdivision.Eighth,
                hatMode: HatMode.Closed,
                sectionType: MusicConstants.eSectionType.Verse);
            var chorusContext = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Ride },
                hatSubdivision: HatSubdivision.Eighth,
                hatMode: HatMode.Closed,
                sectionType: MusicConstants.eSectionType.Chorus);

            // Act
            var verseAvgScore = op.GenerateCandidates(verseContext).Average(c => c.Score);
            var chorusAvgScore = op.GenerateCandidates(chorusContext).Average(c => c.Score);

            // Assert
            Assert.True(chorusAvgScore > verseAvgScore, "Score should be higher in chorus");
        }

        #endregion

        #region PartialLiftOperator Tests

        [Fact]
        public void PartialLift_Generates16thsOnSecondHalf_And8thsOnFirstHalf()
        {
            // Arrange
            var op = new PartialLiftOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - 4 (8ths on beats 1-2) + 8 (16ths on beats 3-4) = 12 positions
            Assert.Equal(12, candidates.Count);

            // Verify first half has 8th positions only
            var firstHalfBeats = candidates.Where(c => c.Beat < 3.0m).Select(c => c.Beat).OrderBy(b => b).ToList();
            var expectedFirstHalf = new[] { 1.0m, 1.5m, 2.0m, 2.5m };
            Assert.Equal(expectedFirstHalf, firstHalfBeats);

            // Verify second half has 16th positions
            var secondHalfBeats = candidates.Where(c => c.Beat >= 3.0m).Select(c => c.Beat).OrderBy(b => b).ToList();
            var expectedSecondHalf = new[] { 3.0m, 3.25m, 3.5m, 3.75m, 4.0m, 4.25m, 4.5m, 4.75m };
            Assert.Equal(expectedSecondHalf, secondHalfBeats);
        }

        [Fact]
        public void PartialLift_ReturnsEmpty_WhenHatSubdivisionIsSixteenth()
        {
            // Arrange
            var op = new PartialLiftOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Sixteenth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void PartialLift_ReturnsEmpty_WhenEnergyBelowThreshold()
        {
            // Arrange
            var op = new PartialLiftOperator();
            var context = CreateContext(
                energy: 0.3,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void PartialLift_ReturnsEmpty_WhenBarTooShort()
        {
            // Arrange
            var op = new PartialLiftOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth,
                beatsPerBar: 3);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - needs at least 4 beats
            Assert.Empty(candidates);
        }

        [Fact]
        public void PartialLift_IsDeterministic_ForSameSeed()
        {
            // Arrange
            var op = new PartialLiftOperator();
            var context1 = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth,
                seed: 123);
            var context2 = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth,
                seed: 123);

            // Act
            var candidates1 = op.GenerateCandidates(context1).ToList();
            var candidates2 = op.GenerateCandidates(context2).ToList();

            // Assert
            Assert.Equal(candidates1.Count, candidates2.Count);
            for (int i = 0; i < candidates1.Count; i++)
            {
                Assert.Equal(candidates1[i].CandidateId, candidates2[i].CandidateId);
                Assert.Equal(candidates1[i].VelocityHint, candidates2[i].VelocityHint);
            }
        }

        #endregion

        #region OpenHatAccentOperator Tests

        [Fact]
        public void OpenHatAccent_GeneratesOpenHatOnBeat1And3_WhenEnergyHighAndRolePresent()
        {
            // Arrange
            var op = new OpenHatAccentOperator();
            var context = CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.OpenHat, GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - two accents at high energy
            Assert.Equal(2, candidates.Count);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.OpenHat, c.Role);
                Assert.Equal(DrumArticulation.OpenHat, c.ArticulationHint);
                Assert.Equal(OnsetStrength.Offbeat, c.Strength);
                Assert.NotNull(c.VelocityHint);
                Assert.InRange(c.VelocityHint!.Value, 85, 105);
            });

            // Verify positions are on expected offbeats
            var beats = candidates.Select(c => c.Beat).OrderBy(b => b).ToList();
            Assert.Contains(1.5m, beats);
            Assert.Contains(3.5m, beats);
        }

        [Fact]
        public void OpenHatAccent_GeneratesOneAccent_WhenEnergyModerate()
        {
            // Arrange
            var op = new OpenHatAccentOperator();
            var context = CreateContext(
                energy: 0.55,
                activeRoles: new HashSet<string> { GrooveRoles.OpenHat },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - one accent at moderate energy
            Assert.Single(candidates);
        }

        [Fact]
        public void OpenHatAccent_ReturnsEmpty_WhenOpenHatNotActive()
        {
            // Arrange
            var op = new OpenHatAccentOperator();
            var context = CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void OpenHatAccent_ReturnsEmpty_WhenEnergyLow()
        {
            // Arrange
            var op = new OpenHatAccentOperator();
            var context = CreateContext(
                energy: 0.3,
                activeRoles: new HashSet<string> { GrooveRoles.OpenHat },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void OpenHatAccent_ReturnsEmpty_WhenCurrentModeIsRide()
        {
            // Arrange
            var op = new OpenHatAccentOperator();
            var context = CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.OpenHat },
                hatSubdivision: HatSubdivision.Eighth,
                hatMode: HatMode.Ride);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void OpenHatAccent_ReturnsEmpty_WhenInFillWindow()
        {
            // Arrange
            var op = new OpenHatAccentOperator();
            var context = CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.OpenHat },
                hatSubdivision: HatSubdivision.Eighth,
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region Cross-Operator Tests

        [Fact]
        public void AllOperators_SkipsWhenActiveRolesDoNotContainTargetRole()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new HatLiftOperator(),
                new HatDropOperator(),
                new RideSwapOperator(),
                new PartialLiftOperator(),
                new OpenHatAccentOperator()
            };

            // Active roles missing all hat/ride roles
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare },
                hatSubdivision: HatSubdivision.Eighth);

            // Act & Assert
            foreach (var op in operators)
            {
                var candidates = op.GenerateCandidates(context).ToList();
                Assert.Empty(candidates);
            }
        }

        [Fact]
        public void AllOperators_DoNotProduceScoresOutsideZeroToOne()
        {
            // Arrange
            var operators = new (IDrumOperator Op, HatSubdivision Sub, double Energy, HashSet<string> Roles)[]
            {
                (new HatLiftOperator(), HatSubdivision.Eighth, 0.9, new HashSet<string> { GrooveRoles.ClosedHat }),
                (new HatDropOperator(), HatSubdivision.Sixteenth, 0.2, new HashSet<string> { GrooveRoles.ClosedHat }),
                (new RideSwapOperator(), HatSubdivision.Eighth, 0.6, new HashSet<string> { GrooveRoles.Ride }),
                (new PartialLiftOperator(), HatSubdivision.Eighth, 0.7, new HashSet<string> { GrooveRoles.ClosedHat }),
                (new OpenHatAccentOperator(), HatSubdivision.Eighth, 0.8, new HashSet<string> { GrooveRoles.OpenHat })
            };

            foreach (var (op, sub, energy, roles) in operators)
            {
                var context = CreateContext(energy: energy, activeRoles: roles, hatSubdivision: sub);

                // Act
                var candidates = op.GenerateCandidates(context).ToList();

                // Assert
                Assert.All(candidates, c => Assert.InRange(c.Score, 0.0, 1.0));
            }
        }

        [Fact]
        public void AllOperators_HaveCorrectOperatorFamily()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new HatLiftOperator(),
                new HatDropOperator(),
                new RideSwapOperator(),
                new PartialLiftOperator(),
                new OpenHatAccentOperator()
            };

            // Assert
            foreach (var op in operators)
            {
                Assert.Equal(Common.OperatorFamily.SubdivisionTransform, op.OperatorFamily);
            }
        }

        [Fact]
        public void AllOperators_HaveUniqueOperatorIds()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new HatLiftOperator(),
                new HatDropOperator(),
                new RideSwapOperator(),
                new PartialLiftOperator(),
                new OpenHatAccentOperator()
            };

            // Act
            var ids = operators.Select(op => op.OperatorId).ToList();

            // Assert
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void AllOperators_AreDeterministic_ForSameSeed()
        {
            // Arrange
            var testCases = new (IDrumOperator Op, HatSubdivision Sub, double Energy, HashSet<string> Roles)[]
            {
                (new HatLiftOperator(), HatSubdivision.Eighth, 0.9, new HashSet<string> { GrooveRoles.ClosedHat }),
                (new HatDropOperator(), HatSubdivision.Sixteenth, 0.2, new HashSet<string> { GrooveRoles.ClosedHat }),
                (new RideSwapOperator(), HatSubdivision.Eighth, 0.6, new HashSet<string> { GrooveRoles.Ride }),
                (new PartialLiftOperator(), HatSubdivision.Eighth, 0.7, new HashSet<string> { GrooveRoles.ClosedHat }),
                (new OpenHatAccentOperator(), HatSubdivision.Eighth, 0.8, new HashSet<string> { GrooveRoles.OpenHat })
            };

            foreach (var (op, sub, energy, roles) in testCases)
            {
                var context1 = CreateContext(energy: energy, activeRoles: roles, hatSubdivision: sub, seed: 12345);
                var context2 = CreateContext(energy: energy, activeRoles: roles, hatSubdivision: sub, seed: 12345);

                // Act
                var candidates1 = op.GenerateCandidates(context1).ToList();
                var candidates2 = op.GenerateCandidates(context2).ToList();

                // Assert
                Assert.Equal(candidates1.Count, candidates2.Count);
                for (int i = 0; i < candidates1.Count; i++)
                {
                    Assert.Equal(candidates1[i].CandidateId, candidates2[i].CandidateId);
                    Assert.Equal(candidates1[i].Beat, candidates2[i].Beat);
                    Assert.Equal(candidates1[i].VelocityHint, candidates2[i].VelocityHint);
                }
            }
        }

        [Fact]
        public void AllOperators_CanApply_ThrowsForNullContext()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new HatLiftOperator(),
                new HatDropOperator(),
                new RideSwapOperator(),
                new PartialLiftOperator(),
                new OpenHatAccentOperator()
            };

            foreach (var op in operators)
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() => op.CanApply((DrummerContext)null!));
            }
        }

        #endregion

        #region Odd Meter Tests

        [Fact]
        public void Operators_RespectBarLength_InOddMeter_3Over4()
        {
            // Arrange - 3/4 time
            var op = new HatLiftOperator();
            var context = CreateContext(
                energy: 0.9,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth,
                beatsPerBar: 3);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - 12 positions (3 beats × 4 16ths per beat)
            Assert.Equal(12, candidates.Count);
            Assert.All(candidates, c => Assert.True(c.Beat <= 3.75m, "All beats should be within 3/4 bar"));
        }

        [Fact]
        public void Operators_RespectBarLength_InOddMeter_5Over4()
        {
            // Arrange - 5/4 time
            var op = new HatLiftOperator();
            var context = CreateContext(
                energy: 0.9,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth,
                beatsPerBar: 5);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - 20 positions (5 beats × 4 16ths per beat)
            Assert.Equal(20, candidates.Count);
            Assert.All(candidates, c => Assert.True(c.Beat <= 5.75m, "All beats should be within 5/4 bar"));
        }

        [Fact]
        public void PartialLift_HandlesOddMeter_6Over4()
        {
            // Arrange - 6/4 time (halfway = beat 4)
            var op = new PartialLiftOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth,
                beatsPerBar: 6);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - 6 (8ths on beats 1-3) + 12 (16ths on beats 4-6) = 18 positions
            Assert.Equal(18, candidates.Count);

            // First half (beats 1-3) should be 8ths
            var firstHalfCount = candidates.Count(c => c.Beat < 4.0m);
            Assert.Equal(6, firstHalfCount); // 3 beats × 2 8ths
        }

        #endregion

        #region Helper Methods

        private static DrummerContext CreateContext(
            double energy = 0.5,
            IReadOnlySet<string>? activeRoles = null,
            HatSubdivision hatSubdivision = HatSubdivision.Eighth,
            HatMode hatMode = HatMode.Closed,
            bool isFillWindow = false,
            bool isAtSectionBoundary = false,
            int seed = 42,
            int barNumber = 1,
            int beatsPerBar = 4,
            int barsUntilSectionEnd = 4,
            MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse)
        {
            return new DrummerContext
            {
                // Base AgentContext fields
                BarNumber = barNumber,
                Beat = 1.0m,
                SectionType = sectionType,
                PhrasePosition = 0.0,
                BarsUntilSectionEnd = barsUntilSectionEnd,
                EnergyLevel = energy,
                TensionLevel = 0.0,
                MotifPresenceScore = 0.0,
                Seed = seed,
                RngStreamKey = $"Test_{barNumber}",

                // Drummer-specific fields
                ActiveRoles = activeRoles ?? new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat },
                LastKickBeat = null,
                LastSnareBeat = null,
                CurrentHatMode = hatMode,
                HatSubdivision = hatSubdivision,
                IsFillWindow = isFillWindow,
                IsAtSectionBoundary = isAtSectionBoundary,
                BackbeatBeats = beatsPerBar >= 4 ? new List<int> { 2, 4 } : new List<int> { 2 },
                BeatsPerBar = beatsPerBar
            };
        }

        #endregion
    }
}
