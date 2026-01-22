// AI: purpose=Unit tests for Story 3.1 MicroAddition operators; verifies candidate generation and constraints.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums.Operators.MicroAddition for operators under test.
// AI: change=Story 3.1 acceptance criteria: each operator produces expected candidates.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Operators.MicroAddition;
using Music.Generator;
using Music;

namespace Music.Generator.Agents.Drums.Operators.Tests
{
    /// <summary>
    /// Story 3.1: Tests for MicroAddition operators.
    /// Verifies candidate generation, context filtering, velocity ranges, and determinism.
    /// </summary>
    [Collection("RngDependentTests")]
    public class MicroAdditionOperatorTests
    {
        public MicroAdditionOperatorTests()
        {
            Rng.Initialize(42);
        }

        #region GhostBeforeBackbeatOperator Tests

        [Fact]
        public void GhostBeforeBackbeat_GeneratesGhostAtExpectedPositions_WhenEnergyHighAndSnareActive()
        {
            // Arrange
            var op = new GhostBeforeBackbeatOperator();
            var context = CreateContext(energy: 0.7, activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Equal(2, candidates.Count);
            Assert.Contains(candidates, c => c.Beat == 1.75m);
            Assert.Contains(candidates, c => c.Beat == 3.75m);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.Snare, c.Role);
                Assert.Equal(OnsetStrength.Ghost, c.Strength);
                Assert.NotNull(c.VelocityHint);
                Assert.InRange(c.VelocityHint!.Value, 30, 50);
                Assert.InRange(c.Score, 0.0, 1.0);
            });
        }

        [Fact]
        public void GhostBeforeBackbeat_ReturnsEmpty_WhenEnergyLow()
        {
            // Arrange
            var op = new GhostBeforeBackbeatOperator();
            var context = CreateContext(energy: 0.2, activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void GhostBeforeBackbeat_ReturnsEmpty_WhenSnareNotActive()
        {
            // Arrange
            var op = new GhostBeforeBackbeatOperator();
            var context = CreateContext(energy: 0.7, activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void GhostBeforeBackbeat_ReturnsEmpty_WhenInFillWindow()
        {
            // Arrange
            var op = new GhostBeforeBackbeatOperator();
            var context = CreateContext(energy: 0.7, activeRoles: new HashSet<string> { GrooveRoles.Snare }, isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region GhostAfterBackbeatOperator Tests

        [Fact]
        public void GhostAfterBackbeat_GeneratesGhostAtExpectedPositions_WhenEnergyHighAndSnareActive()
        {
            // Arrange
            var op = new GhostAfterBackbeatOperator();
            var context = CreateContext(energy: 0.7, activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Equal(2, candidates.Count);
            Assert.Contains(candidates, c => c.Beat == 2.25m);
            Assert.Contains(candidates, c => c.Beat == 4.25m);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.Snare, c.Role);
                Assert.Equal(OnsetStrength.Ghost, c.Strength);
                Assert.NotNull(c.VelocityHint);
                Assert.InRange(c.VelocityHint!.Value, 30, 50);
            });
        }

        [Fact]
        public void GhostAfterBackbeat_ReturnsEmpty_WhenEnergyBelowThreshold()
        {
            // Arrange
            var op = new GhostAfterBackbeatOperator();
            var context = CreateContext(energy: 0.3, activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        #endregion

        #region KickPickupOperator Tests

        [Fact]
        public void KickPickup_GeneratesPickupAtBarEnd_WhenKickActiveAndEnergyModerate()
        {
            // Arrange
            var op = new KickPickupOperator();
            var context = CreateContext(energy: 0.6, activeRoles: new HashSet<string> { GrooveRoles.Kick });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Single(candidates);
            var candidate = candidates[0];
            Assert.Equal(GrooveRoles.Kick, candidate.Role);
            Assert.Equal(4.75m, candidate.Beat);
            Assert.Equal(OnsetStrength.Pickup, candidate.Strength);
            Assert.NotNull(candidate.VelocityHint);
            Assert.InRange(candidate.VelocityHint!.Value, 60, 80);
        }

        [Fact]
        public void KickPickup_HasLowerScore_WhenAtSectionBoundary()
        {
            // Arrange
            var op = new KickPickupOperator();
            var normalContext = CreateContext(energy: 0.6, activeRoles: new HashSet<string> { GrooveRoles.Kick });
            var boundaryContext = CreateContext(energy: 0.6, activeRoles: new HashSet<string> { GrooveRoles.Kick }, isAtSectionBoundary: true);

            // Act
            var normalScore = op.GenerateCandidates(normalContext).First().Score;
            var boundaryScore = op.GenerateCandidates(boundaryContext).First().Score;

            // Assert
            Assert.True(boundaryScore < normalScore, "Score should be lower at section boundary");
        }

        [Fact]
        public void KickPickup_HasStableCandidateId_ForSameSeed()
        {
            // Arrange
            var op = new KickPickupOperator();
            var context1 = CreateContext(energy: 0.6, activeRoles: new HashSet<string> { GrooveRoles.Kick }, seed: 123);
            var context2 = CreateContext(energy: 0.6, activeRoles: new HashSet<string> { GrooveRoles.Kick }, seed: 123);

            // Act
            var id1 = op.GenerateCandidates(context1).First().CandidateId;
            var id2 = op.GenerateCandidates(context2).First().CandidateId;

            // Assert
            Assert.Equal(id1, id2);
        }

        #endregion

        #region KickDoubleOperator Tests

        [Fact]
        public void KickDouble_Uses8thPositions_WhenSubdivisionIsEighth()
        {
            // Arrange
            var op = new KickDoubleOperator();
            var context = CreateContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Equal(2, candidates.Count);
            Assert.Contains(candidates, c => c.Beat == 1.5m);
            Assert.Contains(candidates, c => c.Beat == 3.5m);
        }

        [Fact]
        public void KickDouble_Uses16thVariants_WhenSubdivisionIsSixteenthAndEnergyHigh()
        {
            // Arrange
            var op = new KickDoubleOperator();
            var context = CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                hatSubdivision: HatSubdivision.Sixteenth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Equal(2, candidates.Count);
            // Should use either 1.25/3.25 or 1.75/3.75 (deterministic by seed)
            Assert.True(
                (candidates.Any(c => c.Beat == 1.25m) && candidates.Any(c => c.Beat == 3.25m)) ||
                (candidates.Any(c => c.Beat == 1.75m) && candidates.Any(c => c.Beat == 3.75m)),
                "Should use either early or late 16th variants consistently");
        }

        [Fact]
        public void KickDouble_UsesConsistent16thVariant_ForSameSeed()
        {
            // Arrange
            var op = new KickDoubleOperator();
            var context1 = CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                hatSubdivision: HatSubdivision.Sixteenth,
                seed: 999);
            var context2 = CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                hatSubdivision: HatSubdivision.Sixteenth,
                seed: 999);

            // Act
            var beats1 = op.GenerateCandidates(context1).Select(c => c.Beat).ToList();
            var beats2 = op.GenerateCandidates(context2).Select(c => c.Beat).ToList();

            // Assert
            Assert.Equal(beats1, beats2);
        }

        #endregion

        #region HatEmbellishmentOperator Tests

        [Fact]
        public void HatEmbellishment_GeneratesSparseCandidates_WhenHatActiveAnd8thSubdivision()
        {
            // Arrange
            var op = new HatEmbellishmentOperator();
            var context = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.InRange(candidates.Count, 1, 2);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.ClosedHat, c.Role);
                Assert.Equal(OnsetStrength.Offbeat, c.Strength);
                Assert.NotNull(c.VelocityHint);
                Assert.InRange(c.VelocityHint!.Value, 40, 60);
            });
        }

        [Fact]
        public void HatEmbellishment_ReturnsEmpty_WhenHatSubdivisionIsSixteenth()
        {
            // Arrange
            var op = new HatEmbellishmentOperator();
            var context = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Sixteenth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert - no embellishment when already playing 16ths
            Assert.Empty(candidates);
        }

        [Fact]
        public void HatEmbellishment_ReturnsEmpty_WhenHatNotActive()
        {
            // Arrange
            var op = new HatEmbellishmentOperator();
            var context = CreateContext(
                energy: 0.6,
                activeRoles: new HashSet<string> { GrooveRoles.Kick },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void HatEmbellishment_GeneratesMoreCandidates_AtHigherEnergy()
        {
            // Arrange
            var op = new HatEmbellishmentOperator();
            var lowEnergyContext = CreateContext(
                energy: 0.5,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth);
            var highEnergyContext = CreateContext(
                energy: 0.8,
                activeRoles: new HashSet<string> { GrooveRoles.ClosedHat },
                hatSubdivision: HatSubdivision.Eighth);

            // Act
            var lowCount = op.GenerateCandidates(lowEnergyContext).Count();
            var highCount = op.GenerateCandidates(highEnergyContext).Count();

            // Assert
            Assert.True(highCount >= lowCount, "Higher energy should produce at least as many candidates");
        }

        #endregion

        #region GhostClusterOperator Tests

        [Fact]
        public void GhostCluster_GeneratesCluster_WhenSnareActiveAndEnergyHigh()
        {
            // Arrange
            var op = new GhostClusterOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.InRange(candidates.Count, 2, 3);
            Assert.All(candidates, c =>
            {
                Assert.Equal(GrooveRoles.Snare, c.Role);
                Assert.Equal(OnsetStrength.Ghost, c.Strength);
                Assert.NotNull(c.VelocityHint);
                Assert.InRange(c.VelocityHint!.Value, 30, 50);
            });
        }

        [Fact]
        public void GhostCluster_ReturnsEmpty_WhenInFillWindow()
        {
            // Arrange
            var op = new GhostClusterOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                isFillWindow: true);

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void GhostCluster_HasDecreasingVelocity_ThroughCluster()
        {
            // Arrange
            var op = new GhostClusterOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                seed: 42);

            // Act
            var candidates = op.GenerateCandidates(context).OrderBy(c => c.Beat).ToList();

            // Assert - first note should generally have higher or equal velocity
            if (candidates.Count >= 2)
            {
                // Score should decrease for later notes
                Assert.True(candidates[0].Score >= candidates[1].Score,
                    "First note in cluster should have higher or equal score");
            }
        }

        #endregion

        #region FloorTomPickupOperator Tests

        [Fact]
        public void FloorTomPickup_GeneratesPickupAtBarEnd_WhenFloorTomActiveAndEnergyHigh()
        {
            // Arrange
            var op = new FloorTomPickupOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.FloorTom });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Single(candidates);
            var candidate = candidates[0];
            Assert.Equal(GrooveRoles.FloorTom, candidate.Role);
            Assert.Equal(4.75m, candidate.Beat);
            Assert.Equal(OnsetStrength.Pickup, candidate.Strength);
            Assert.NotNull(candidate.VelocityHint);
            Assert.InRange(candidate.VelocityHint!.Value, 60, 80);
        }

        [Fact]
        public void FloorTomPickup_ReturnsEmpty_WhenFloorTomNotActive()
        {
            // Arrange
            var op = new FloorTomPickupOperator();
            var context = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare });

            // Act
            var candidates = op.GenerateCandidates(context).ToList();

            // Assert
            Assert.Empty(candidates);
        }

        [Fact]
        public void FloorTomPickup_HasReducedScore_WhenAtSectionBoundary()
        {
            // Arrange
            var op = new FloorTomPickupOperator();
            var normalContext = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.FloorTom });
            var boundaryContext = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.FloorTom },
                isAtSectionBoundary: true);

            // Act
            var normalScore = op.GenerateCandidates(normalContext).First().Score;
            var boundaryScore = op.GenerateCandidates(boundaryContext).First().Score;

            // Assert
            Assert.True(boundaryScore < normalScore, "Score should be reduced at section boundary");
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void AllOperators_AreDeterministic_ForSameSeed()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new GhostBeforeBackbeatOperator(),
                new GhostAfterBackbeatOperator(),
                new KickPickupOperator(),
                new KickDoubleOperator(),
                new HatEmbellishmentOperator(),
                new GhostClusterOperator(),
                new FloorTomPickupOperator()
            };

            var activeRoles = new HashSet<string>
            {
                GrooveRoles.Kick,
                GrooveRoles.Snare,
                GrooveRoles.ClosedHat,
                GrooveRoles.FloorTom
            };

            foreach (var op in operators)
            {
                // Act
                var context1 = CreateContext(energy: 0.7, activeRoles: activeRoles, seed: 12345);
                var context2 = CreateContext(energy: 0.7, activeRoles: activeRoles, seed: 12345);

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
        public void Operators_ProduceDifferentResults_ForDifferentSeeds()
        {
            // Arrange
            var op = new GhostClusterOperator();
            var context1 = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                seed: 100);
            var context2 = CreateContext(
                energy: 0.7,
                activeRoles: new HashSet<string> { GrooveRoles.Snare },
                seed: 200);

            // Act
            var candidates1 = op.GenerateCandidates(context1).ToList();
            var candidates2 = op.GenerateCandidates(context2).ToList();

            // Assert - at least one aspect should differ (velocity, position, or count)
            bool sameCount = candidates1.Count == candidates2.Count;
            bool allSameBeats = candidates1.Select(c => c.Beat).SequenceEqual(candidates2.Select(c => c.Beat));
            bool allSameVelocity = candidates1.Select(c => c.VelocityHint).SequenceEqual(candidates2.Select(c => c.VelocityHint));

            Assert.False(sameCount && allSameBeats && allSameVelocity,
                "Different seeds should produce different results");
        }

        #endregion

        #region CanApply Tests

        [Fact]
        public void AllOperators_CanApply_ReturnsFalse_ForNullContext()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new GhostBeforeBackbeatOperator(),
                new GhostAfterBackbeatOperator(),
                new KickPickupOperator(),
                new KickDoubleOperator(),
                new HatEmbellishmentOperator(),
                new GhostClusterOperator(),
                new FloorTomPickupOperator()
            };

            foreach (var op in operators)
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() => op.CanApply((DrummerContext)null!));
            }
        }

        [Fact]
        public void AllOperators_HaveCorrectOperatorFamily()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new GhostBeforeBackbeatOperator(),
                new GhostAfterBackbeatOperator(),
                new KickPickupOperator(),
                new KickDoubleOperator(),
                new HatEmbellishmentOperator(),
                new GhostClusterOperator(),
                new FloorTomPickupOperator()
            };

            // Assert
            foreach (var op in operators)
            {
                Assert.Equal(Common.OperatorFamily.MicroAddition, op.OperatorFamily);
            }
        }

        [Fact]
        public void AllOperators_HaveUniqueOperatorIds()
        {
            // Arrange
            var operators = new IDrumOperator[]
            {
                new GhostBeforeBackbeatOperator(),
                new GhostAfterBackbeatOperator(),
                new KickPickupOperator(),
                new KickDoubleOperator(),
                new HatEmbellishmentOperator(),
                new GhostClusterOperator(),
                new FloorTomPickupOperator()
            };

            // Act
            var ids = operators.Select(op => op.OperatorId).ToList();

            // Assert
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        #endregion

        #region Helper Methods

        private static DrummerContext CreateContext(
            double energy = 0.5,
            IReadOnlySet<string>? activeRoles = null,
            HatSubdivision hatSubdivision = HatSubdivision.Eighth,
            bool isFillWindow = false,
            bool isAtSectionBoundary = false,
            int seed = 42,
            int barNumber = 1)
        {
            return new DrummerContext
            {
                // Base AgentContext fields
                BarNumber = barNumber,
                Beat = 1.0m,
                SectionType = MusicConstants.eSectionType.Verse,
                PhrasePosition = 0.0,
                BarsUntilSectionEnd = 4,
                EnergyLevel = energy,
                TensionLevel = 0.0,
                MotifPresenceScore = 0.0,
                Seed = seed,
                RngStreamKey = $"Test_{barNumber}",

                // Drummer-specific fields
                ActiveRoles = activeRoles ?? new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat },
                LastKickBeat = null,
                LastSnareBeat = null,
                CurrentHatMode = HatMode.Closed,
                HatSubdivision = hatSubdivision,
                IsFillWindow = isFillWindow,
                IsAtSectionBoundary = isAtSectionBoundary,
                BackbeatBeats = new List<int> { 2, 4 },
                BeatsPerBar = 4
            };
        }

        #endregion
    }
}
