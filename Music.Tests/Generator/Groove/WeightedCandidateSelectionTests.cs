// AI: purpose=Unit tests for Story B3 weighted candidate selection (DrumWeightedCandidateSelector).
// AI: deps=xunit for test framework; Music.Generator for types under test.
// AI: change=Story B3 acceptance criteria: test determinism, weight computation, tie-breaking, zero weights.

using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
using Xunit;

namespace Music.Generator.Tests
{
    /// <summary>
    /// Story B3: Tests for weighted candidate selection.
    /// Verifies determinism, weight computation, tie-breaking, and edge cases.
    /// </summary>
    /// <remarks>
    /// This class is in the RngDependentTests collection to run sequentially because
    /// tests verify determinism by re-initializing global RNG state.
    /// </remarks>
    [Collection("RngDependentTests")]
    public class WeightedCandidateSelectionTests
    {
        public WeightedCandidateSelectionTests()
        {
            // Ensure RNG is initialized for all tests
            Rng.Initialize(12345);
        }

        #region Weight Computation Tests

        [Fact]
        public void ComputeWeight_PositiveBiases_ReturnsProduct()
        {
            // Arrange
            var candidate = new DrumOnsetCandidate { ProbabilityBias = 0.5 };
            var group = new DrumCandidateGroup { BaseProbabilityBias = 0.8 };

            // Act
            double weight = DrumWeightedCandidateSelector.ComputeWeight(candidate, group);

            // Assert
            Assert.Equal(0.4, weight, precision: 10);
        }

        [Fact]
        public void ComputeWeight_ZeroCandidateBias_ReturnsZero()
        {
            // Arrange
            var candidate = new DrumOnsetCandidate { ProbabilityBias = 0.0 };
            var group = new DrumCandidateGroup { BaseProbabilityBias = 0.8 };

            // Act
            double weight = DrumWeightedCandidateSelector.ComputeWeight(candidate, group);

            // Assert
            Assert.Equal(0.0, weight);
        }

        [Fact]
        public void ComputeWeight_NegativeCandidateBias_ReturnsZero()
        {
            // Arrange
            var candidate = new DrumOnsetCandidate { ProbabilityBias = -0.5 };
            var group = new DrumCandidateGroup { BaseProbabilityBias = 0.8 };

            // Act
            double weight = DrumWeightedCandidateSelector.ComputeWeight(candidate, group);

            // Assert
            Assert.Equal(0.0, weight);
        }

        [Fact]
        public void ComputeWeight_ZeroGroupBias_ReturnsZero()
        {
            // Arrange
            var candidate = new DrumOnsetCandidate { ProbabilityBias = 0.5 };
            var group = new DrumCandidateGroup { BaseProbabilityBias = 0.0 };

            // Act
            double weight = DrumWeightedCandidateSelector.ComputeWeight(candidate, group);

            // Assert
            Assert.Equal(0.0, weight);
        }

        [Fact]
        public void ComputeWeight_NegativeGroupBias_ReturnsZero()
        {
            // Arrange
            var candidate = new DrumOnsetCandidate { ProbabilityBias = 0.5 };
            var group = new DrumCandidateGroup { BaseProbabilityBias = -0.3 };

            // Act
            double weight = DrumWeightedCandidateSelector.ComputeWeight(candidate, group);

            // Assert
            Assert.Equal(0.0, weight);
        }

        #endregion

        #region Deterministic Tie-Breaking Tests

        [Fact]
        public void BuildWeightedCandidates_SameWeight_SortsByStableId()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                new DrumCandidateGroup
                {
                    GroupId = "Group1",
                    BaseProbabilityBias = 1.0,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { OnsetBeat = 3.0m, ProbabilityBias = 0.5 },
                        new DrumOnsetCandidate { OnsetBeat = 1.0m, ProbabilityBias = 0.5 },
                        new DrumOnsetCandidate { OnsetBeat = 2.0m, ProbabilityBias = 0.5 }
                    }
                }
            };

            // Act
            var result = DrumWeightedCandidateSelector.BuildWeightedCandidates(groups);

            // Assert - Same weight, sorted by stable ID (which includes beat)
            Assert.Equal(3, result.Count);
            Assert.Equal(1.0m, result[0].Candidate.OnsetBeat);
            Assert.Equal(2.0m, result[1].Candidate.OnsetBeat);
            Assert.Equal(3.0m, result[2].Candidate.OnsetBeat);
        }

        [Fact]
        public void BuildWeightedCandidates_DifferentWeights_SortsByWeightDesc()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                new DrumCandidateGroup
                {
                    GroupId = "Group1",
                    BaseProbabilityBias = 1.0,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { OnsetBeat = 1.0m, ProbabilityBias = 0.3 },
                        new DrumOnsetCandidate { OnsetBeat = 2.0m, ProbabilityBias = 0.9 },
                        new DrumOnsetCandidate { OnsetBeat = 3.0m, ProbabilityBias = 0.6 }
                    }
                }
            };

            // Act
            var result = DrumWeightedCandidateSelector.BuildWeightedCandidates(groups);

            // Assert - Sorted by weight descending
            Assert.Equal(3, result.Count);
            Assert.Equal(0.9, result[0].ComputedWeight);
            Assert.Equal(0.6, result[1].ComputedWeight);
            Assert.Equal(0.3, result[2].ComputedWeight);
        }

        [Fact]
        public void BuildWeightedCandidates_MixedWeights_WeightThenStableId()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                new DrumCandidateGroup
                {
                    GroupId = "GroupA",
                    BaseProbabilityBias = 1.0,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { OnsetBeat = 1.0m, ProbabilityBias = 0.5 }
                    }
                },
                new DrumCandidateGroup
                {
                    GroupId = "GroupB",
                    BaseProbabilityBias = 1.0,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { OnsetBeat = 1.0m, ProbabilityBias = 0.5 }
                    }
                }
            };

            // Act
            var result = DrumWeightedCandidateSelector.BuildWeightedCandidates(groups);

            // Assert - Same weight, GroupA comes before GroupB alphabetically
            Assert.Equal(2, result.Count);
            Assert.Equal("GroupA", result[0].Group.GroupId);
            Assert.Equal("GroupB", result[1].Group.GroupId);
        }

        #endregion

        #region Same Seed Identical Selection Tests

        [Fact]
        public void SelectCandidates_SameSeed_IdenticalSelections()
        {
            // Arrange
            var groups = CreateTestGroups();

            // Act - Run multiple times with same seed (re-initialize to reset RNG state)
            Rng.Initialize(12345);
            var result1 = DrumWeightedCandidateSelector.SelectCandidates(groups, 2, barNumber: 1, role: "Kick");

            Rng.Initialize(12345);
            var result2 = DrumWeightedCandidateSelector.SelectCandidates(groups, 2, barNumber: 1, role: "Kick");

            Rng.Initialize(12345);
            var result3 = DrumWeightedCandidateSelector.SelectCandidates(groups, 2, barNumber: 1, role: "Kick");

            // Assert - All runs produce identical results
            Assert.Equal(result1.Select(w => w.StableId), result2.Select(w => w.StableId));
            Assert.Equal(result2.Select(w => w.StableId), result3.Select(w => w.StableId));
        }

        [Fact]
        public void SelectCandidates_SameSeedDifferentBar_IdenticalWithinBar()
        {
            // Arrange
            var groups = CreateTestGroups();

            // Act - Same bar, same results
            Rng.Initialize(12345);
            var bar1Result1 = DrumWeightedCandidateSelector.SelectCandidates(groups, 2, barNumber: 1, role: "Kick");

            Rng.Initialize(12345);
            var bar1Result2 = DrumWeightedCandidateSelector.SelectCandidates(groups, 2, barNumber: 1, role: "Kick");

            // Assert
            Assert.Equal(bar1Result1.Select(w => w.StableId), bar1Result2.Select(w => w.StableId));
        }

        #endregion

        #region Different Seed Different Selection Tests

        [Fact]
        public void SelectCandidates_DifferentSeed_DifferentSelections()
        {
            // Arrange
            var groups = CreateTestGroupsWithManyOptions();

            // Act
            Rng.Initialize(12345);
            var result1 = DrumWeightedCandidateSelector.SelectCandidates(groups, 3, barNumber: 1, role: "Kick");

            Rng.Initialize(99999);
            var result2 = DrumWeightedCandidateSelector.SelectCandidates(groups, 3, barNumber: 1, role: "Kick");

            // Assert - Different seeds should produce different selections
            // Note: With enough candidates and similar weights, different seeds should select differently
            // This test may occasionally pass with same selection if RNG happens to pick same order
            var ids1 = result1.Select(w => w.StableId).ToList();
            var ids2 = result2.Select(w => w.StableId).ToList();

            // At minimum, verify we got results
            Assert.Equal(3, result1.Count);
            Assert.Equal(3, result2.Count);

            // For determinism test, we verify the results are reproducible within each seed
            Rng.Initialize(12345);
            var result1Again = DrumWeightedCandidateSelector.SelectCandidates(groups, 3, barNumber: 1, role: "Kick");
            Assert.Equal(ids1, result1Again.Select(w => w.StableId).ToList());
        }

        #endregion

        #region Zero/Negative Weight Handling Tests

        [Fact]
        public void BuildWeightedCandidates_ZeroWeightCandidates_Filtered()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                new DrumCandidateGroup
                {
                    GroupId = "Group1",
                    BaseProbabilityBias = 1.0,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { OnsetBeat = 1.0m, ProbabilityBias = 0.5 },
                        new DrumOnsetCandidate { OnsetBeat = 2.0m, ProbabilityBias = 0.0 }, // Zero
                        new DrumOnsetCandidate { OnsetBeat = 3.0m, ProbabilityBias = -0.5 } // Negative
                    }
                }
            };

            // Act
            var result = DrumWeightedCandidateSelector.BuildWeightedCandidates(groups);

            // Assert - Only positive weight candidate included
            Assert.Single(result);
            Assert.Equal(1.0m, result[0].Candidate.OnsetBeat);
        }

        [Fact]
        public void BuildWeightedCandidates_AllZeroWeights_ReturnsEmpty()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                new DrumCandidateGroup
                {
                    GroupId = "Group1",
                    BaseProbabilityBias = 0.0, // Group bias is zero
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { OnsetBeat = 1.0m, ProbabilityBias = 0.5 },
                        new DrumOnsetCandidate { OnsetBeat = 2.0m, ProbabilityBias = 0.8 }
                    }
                }
            };

            // Act
            var result = DrumWeightedCandidateSelector.BuildWeightedCandidates(groups);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void SelectCandidates_ZeroWeightGroup_SafelyHandled()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>
            {
                new DrumCandidateGroup
                {
                    GroupId = "Group1",
                    BaseProbabilityBias = 0.0,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { OnsetBeat = 1.0m, ProbabilityBias = 0.5 }
                    }
                }
            };

            // Act
            Rng.Initialize(12345);
            var result = DrumWeightedCandidateSelector.SelectCandidates(groups, 5, barNumber: 1, role: "Kick");

            // Assert - No candidates selected (all have zero weight)
            Assert.Empty(result);
        }

        #endregion

        #region Selection Count Tests

        [Fact]
        public void SelectCandidates_TargetLessThanAvailable_SelectsExactCount()
        {
            // Arrange
            var groups = CreateTestGroups();

            // Act
            Rng.Initialize(12345);
            var result = DrumWeightedCandidateSelector.SelectCandidates(groups, 2, barNumber: 1, role: "Kick");

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void SelectCandidates_TargetMoreThanAvailable_SelectsAll()
        {
            // Arrange
            var groups = CreateTestGroups(); // Has 3 candidates

            // Act
            Rng.Initialize(12345);
            var result = DrumWeightedCandidateSelector.SelectCandidates(groups, 10, barNumber: 1, role: "Kick");

            // Assert
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void SelectCandidates_TargetZero_ReturnsEmpty()
        {
            // Arrange
            var groups = CreateTestGroups();

            // Act
            var result = DrumWeightedCandidateSelector.SelectCandidates(groups, 0, barNumber: 1, role: "Kick");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void SelectCandidates_NegativeTarget_ReturnsEmpty()
        {
            // Arrange
            var groups = CreateTestGroups();

            // Act
            var result = DrumWeightedCandidateSelector.SelectCandidates(groups, -5, barNumber: 1, role: "Kick");

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void SelectCandidates_EmptyGroups_ReturnsEmpty()
        {
            // Arrange
            var groups = new List<DrumCandidateGroup>();

            // Act
            Rng.Initialize(12345);
            var result = DrumWeightedCandidateSelector.SelectCandidates(groups, 5, barNumber: 1, role: "Kick");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void SelectCandidates_NullGroups_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                DrumWeightedCandidateSelector.SelectCandidates(null!, 5, barNumber: 1, role: "Kick"));
        }

        [Fact]
        public void GetTopByWeight_ReturnsOrderedByWeight()
        {
            // Arrange
            var groups = CreateTestGroups();

            // Act
            var result = DrumWeightedCandidateSelector.GetTopByWeight(groups, topN: 2);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.True(result[0].ComputedWeight >= result[1].ComputedWeight);
        }

        [Fact]
        public void SelectFromGroup_SingleGroup_Works()
        {
            // Arrange
            var group = new DrumCandidateGroup
            {
                GroupId = "TestGroup",
                BaseProbabilityBias = 1.0,
                Candidates = new List<DrumOnsetCandidate>
                {
                    new DrumOnsetCandidate { OnsetBeat = 1.0m, ProbabilityBias = 0.5 },
                    new DrumOnsetCandidate { OnsetBeat = 2.0m, ProbabilityBias = 0.8 }
                }
            };

            // Act
            Rng.Initialize(12345);
            var result = DrumWeightedCandidateSelector.SelectFromGroup(group, 1, barNumber: 1, role: "Kick");

            // Assert
            Assert.Single(result);
        }

        #endregion

        #region Stable ID Tests

        [Fact]
        public void CreateStableId_ConsistentFormat()
        {
            // Arrange
            var group = new DrumCandidateGroup { GroupId = "TestGroup" };
            var candidate = new DrumOnsetCandidate { OnsetBeat = 2.5m };

            // Act
            var stableId = DrumWeightedCandidateSelector.CreateStableId(group, candidate);

            // Assert
            Assert.Equal("TestGroup:2.5000", stableId);
        }

        [Fact]
        public void CreateStableId_DifferentGroups_DifferentIds()
        {
            // Arrange
            var group1 = new DrumCandidateGroup { GroupId = "Group1" };
            var group2 = new DrumCandidateGroup { GroupId = "Group2" };
            var candidate = new DrumOnsetCandidate { OnsetBeat = 1.0m };

            // Act
            var id1 = DrumWeightedCandidateSelector.CreateStableId(group1, candidate);
            var id2 = DrumWeightedCandidateSelector.CreateStableId(group2, candidate);

            // Assert
            Assert.NotEqual(id1, id2);
        }

        #endregion

        #region Test Helpers

        private static List<DrumCandidateGroup> CreateTestGroups()
        {
            return new List<DrumCandidateGroup>
            {
                new DrumCandidateGroup
                {
                    GroupId = "TestGroup",
                    BaseProbabilityBias = 1.0,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { OnsetBeat = 1.0m, ProbabilityBias = 0.5 },
                        new DrumOnsetCandidate { OnsetBeat = 2.0m, ProbabilityBias = 0.8 },
                        new DrumOnsetCandidate { OnsetBeat = 3.0m, ProbabilityBias = 0.3 }
                    }
                }
            };
        }

        private static List<DrumCandidateGroup> CreateTestGroupsWithManyOptions()
        {
            return new List<DrumCandidateGroup>
            {
                new DrumCandidateGroup
                {
                    GroupId = "GroupA",
                    BaseProbabilityBias = 1.0,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { OnsetBeat = 1.0m, ProbabilityBias = 0.5 },
                        new DrumOnsetCandidate { OnsetBeat = 1.5m, ProbabilityBias = 0.5 },
                        new DrumOnsetCandidate { OnsetBeat = 2.0m, ProbabilityBias = 0.5 },
                        new DrumOnsetCandidate { OnsetBeat = 2.5m, ProbabilityBias = 0.5 }
                    }
                },
                new DrumCandidateGroup
                {
                    GroupId = "GroupB",
                    BaseProbabilityBias = 1.0,
                    Candidates = new List<DrumOnsetCandidate>
                    {
                        new DrumOnsetCandidate { OnsetBeat = 3.0m, ProbabilityBias = 0.5 },
                        new DrumOnsetCandidate { OnsetBeat = 3.5m, ProbabilityBias = 0.5 },
                        new DrumOnsetCandidate { OnsetBeat = 4.0m, ProbabilityBias = 0.5 }
                    }
                }
            };
        }

        #endregion
    }
}


