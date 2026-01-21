// AI: purpose=Unit tests for Story 1.3 OperatorSelectionEngine (candidate selection with scoring).
// AI: deps=xunit for test framework; Music.Generator.Agents.Common for types under test.
// AI: change=Story 1.3 acceptance criteria: determinism, density targets, hard caps, tie-breaking.

using Xunit;
using Music.Generator.Agents.Common;
using Music.Generator;

namespace Music.Generator.Agents.Common.Tests
{
    /// <summary>
    /// Story 1.3: Tests for OperatorSelectionEngine.
    /// Verifies selection algorithm, determinism, density targets, hard caps, and tie-breaking.
    /// </summary>
    public class OperatorSelectionEngineTests
    {
        public OperatorSelectionEngineTests()
        {
            Rng.Initialize(42);
        }

        #region Test Helpers

        private static ScoredCandidate<string> CreateCandidate(
            string operatorId,
            string candidateId,
            double baseScore,
            double styleWeight = 1.0,
            double penalty = 0.0,
            double density = 1.0)
        {
            return OperatorSelectionEngine<string>.CreateScoredCandidate(
                candidate: $"{operatorId}_{candidateId}",
                operatorId: operatorId,
                candidateId: candidateId,
                baseScore: baseScore,
                styleWeight: styleWeight,
                densityContribution: density,
                repetitionPenalty: penalty);
        }

        #endregion

        #region ScoredCandidate Tests

        [Fact]
        public void ScoredCandidate_FinalScore_ComputesCorrectly()
        {
            // Arrange
            var candidate = CreateCandidate("Op1", "c1", baseScore: 0.8, styleWeight: 0.5, penalty: 0.0);

            // Assert: 0.8 * 0.5 * (1.0 - 0.0) = 0.4
            Assert.Equal(0.4, candidate.FinalScore, precision: 4);
        }

        [Fact]
        public void ScoredCandidate_FinalScore_AppliesPenalty()
        {
            // Arrange
            var candidate = CreateCandidate("Op1", "c1", baseScore: 1.0, styleWeight: 1.0, penalty: 0.5);

            // Assert: 1.0 * 1.0 * (1.0 - 0.5) = 0.5
            Assert.Equal(0.5, candidate.FinalScore, precision: 4);
        }

        [Fact]
        public void ScoredCandidate_FinalScore_ClampedToZero()
        {
            // Arrange - penalty > 1.0 would make score negative, but clamped
            var candidate = CreateCandidate("Op1", "c1", baseScore: 0.5, styleWeight: 1.0, penalty: 1.5);

            // Assert: clamped to 0.0
            Assert.Equal(0.0, candidate.FinalScore, precision: 4);
        }

        [Fact]
        public void ScoredCandidate_FinalScore_ClampedToOne()
        {
            // Arrange - high values would exceed 1.0
            var candidate = CreateCandidate("Op1", "c1", baseScore: 2.0, styleWeight: 2.0, penalty: 0.0);

            // Assert: clamped to 1.0
            Assert.Equal(1.0, candidate.FinalScore, precision: 4);
        }

        #endregion

        #region Basic Selection Tests

        [Fact]
        public void Select_EmptyCandidates_ReturnsEmpty()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = Array.Empty<ScoredCandidate<string>>();

            // Act
            var result = engine.Select(candidates, densityTarget: 5.0, hardCap: 10);

            // Assert
            Assert.Empty(result.Selected);
            Assert.Equal(0.0, result.TotalDensity);
            Assert.False(result.DensityTargetReached);
            Assert.False(result.HardCapReached);
        }

        [Fact]
        public void Select_SingleCandidate_SelectsIt()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[] { CreateCandidate("Op1", "c1", baseScore: 0.8) };

            // Act
            var result = engine.Select(candidates, densityTarget: 5.0, hardCap: 10);

            // Assert
            Assert.Single(result.Selected);
            Assert.Equal("Op1", result.Selected[0].OperatorId);
        }

        [Fact]
        public void Select_MultipleCandidates_SelectsInScoreOrder()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.5),
                CreateCandidate("Op2", "c1", baseScore: 0.9),
                CreateCandidate("Op3", "c1", baseScore: 0.7)
            };

            // Act
            var result = engine.Select(candidates, densityTarget: 5.0, hardCap: 10);

            // Assert - highest score first
            Assert.Equal(3, result.Selected.Count);
            Assert.Equal("Op2", result.Selected[0].OperatorId); // 0.9
            Assert.Equal("Op3", result.Selected[1].OperatorId); // 0.7
            Assert.Equal("Op1", result.Selected[2].OperatorId); // 0.5
        }

        #endregion

        #region Density Target Tests

        [Fact]
        public void Select_StopsAtDensityTarget()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.9, density: 2.0),
                CreateCandidate("Op2", "c1", baseScore: 0.8, density: 2.0),
                CreateCandidate("Op3", "c1", baseScore: 0.7, density: 2.0),
                CreateCandidate("Op4", "c1", baseScore: 0.6, density: 2.0)
            };

            // Act - target is 5.0, each candidate contributes 2.0
            var result = engine.Select(candidates, densityTarget: 5.0, hardCap: 10);

            // Assert - should select 3 (total 6.0 >= 5.0 after third)
            Assert.Equal(3, result.Selected.Count);
            Assert.Equal(6.0, result.TotalDensity);
            Assert.True(result.DensityTargetReached);
            Assert.False(result.HardCapReached);
        }

        [Fact]
        public void Select_ExactDensityTarget()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.9, density: 2.5),
                CreateCandidate("Op2", "c1", baseScore: 0.8, density: 2.5),
                CreateCandidate("Op3", "c1", baseScore: 0.7, density: 2.5)
            };

            // Act - target is 5.0
            var result = engine.Select(candidates, densityTarget: 5.0, hardCap: 10);

            // Assert - should select 2 (total 5.0 = 5.0)
            Assert.Equal(2, result.Selected.Count);
            Assert.Equal(5.0, result.TotalDensity);
            Assert.True(result.DensityTargetReached);
        }

        [Fact]
        public void Select_InsufficientDensity_SelectsAll()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.9, density: 1.0),
                CreateCandidate("Op2", "c1", baseScore: 0.8, density: 1.0)
            };

            // Act - target is 10.0 but only 2.0 available
            var result = engine.Select(candidates, densityTarget: 10.0, hardCap: 10);

            // Assert
            Assert.Equal(2, result.Selected.Count);
            Assert.Equal(2.0, result.TotalDensity);
            Assert.False(result.DensityTargetReached);
        }

        #endregion

        #region Hard Cap Tests

        [Fact]
        public void Select_StopsAtHardCap()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.9),
                CreateCandidate("Op2", "c1", baseScore: 0.8),
                CreateCandidate("Op3", "c1", baseScore: 0.7),
                CreateCandidate("Op4", "c1", baseScore: 0.6),
                CreateCandidate("Op5", "c1", baseScore: 0.5)
            };

            // Act - cap is 3
            var result = engine.Select(candidates, densityTarget: 100.0, hardCap: 3);

            // Assert
            Assert.Equal(3, result.Selected.Count);
            Assert.True(result.HardCapReached);
            Assert.False(result.DensityTargetReached);
        }

        [Fact]
        public void Select_HardCapZero_SelectsNothing()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.9)
            };

            // Act
            var result = engine.Select(candidates, densityTarget: 100.0, hardCap: 0);

            // Assert
            Assert.Empty(result.Selected);
            Assert.True(result.HardCapReached);
        }

        [Fact]
        public void Select_HardCapReachedBeforeDensityTarget()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.9, density: 1.0),
                CreateCandidate("Op2", "c1", baseScore: 0.8, density: 1.0),
                CreateCandidate("Op3", "c1", baseScore: 0.7, density: 1.0),
                CreateCandidate("Op4", "c1", baseScore: 0.6, density: 1.0)
            };

            // Act - cap is 2, target is 10.0
            var result = engine.Select(candidates, densityTarget: 10.0, hardCap: 2);

            // Assert - cap takes precedence
            Assert.Equal(2, result.Selected.Count);
            Assert.Equal(2.0, result.TotalDensity);
            Assert.True(result.HardCapReached);
            Assert.False(result.DensityTargetReached);
        }

        #endregion

        #region Tie-Breaking Tests

        [Fact]
        public void Select_EqualScores_BreaksByOperatorId()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[]
            {
                CreateCandidate("Zebra", "c1", baseScore: 0.8),
                CreateCandidate("Alpha", "c1", baseScore: 0.8),
                CreateCandidate("Middle", "c1", baseScore: 0.8)
            };

            // Act
            var result = engine.Select(candidates, densityTarget: 10.0, hardCap: 10);

            // Assert - alphabetical by operatorId
            Assert.Equal("Alpha", result.Selected[0].OperatorId);
            Assert.Equal("Middle", result.Selected[1].OperatorId);
            Assert.Equal("Zebra", result.Selected[2].OperatorId);
        }

        [Fact]
        public void Select_EqualScoresAndOperatorId_BreaksByCandidateId()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[]
            {
                CreateCandidate("Op1", "z-candidate", baseScore: 0.8),
                CreateCandidate("Op1", "a-candidate", baseScore: 0.8),
                CreateCandidate("Op1", "m-candidate", baseScore: 0.8)
            };

            // Act
            var result = engine.Select(candidates, densityTarget: 10.0, hardCap: 10);

            // Assert - alphabetical by candidateId
            Assert.Equal("a-candidate", result.Selected[0].CandidateId);
            Assert.Equal("m-candidate", result.Selected[1].CandidateId);
            Assert.Equal("z-candidate", result.Selected[2].CandidateId);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void Select_SameSeed_IdenticalOutput()
        {
            // Arrange
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.9),
                CreateCandidate("Op2", "c1", baseScore: 0.8),
                CreateCandidate("Op3", "c1", baseScore: 0.7),
                CreateCandidate("Op1", "c2", baseScore: 0.85)
            };

            // Act - run twice
            var engine1 = new OperatorSelectionEngine<string>();
            var result1 = engine1.Select(candidates, densityTarget: 3.0, hardCap: 10);

            var engine2 = new OperatorSelectionEngine<string>();
            var result2 = engine2.Select(candidates, densityTarget: 3.0, hardCap: 10);

            // Assert - identical
            Assert.Equal(result1.Selected.Count, result2.Selected.Count);
            for (int i = 0; i < result1.Selected.Count; i++)
            {
                Assert.Equal(result1.Selected[i].OperatorId, result2.Selected[i].OperatorId);
                Assert.Equal(result1.Selected[i].CandidateId, result2.Selected[i].CandidateId);
            }
        }

        [Fact]
        public void Select_DifferentInputOrder_SameOutput()
        {
            // Arrange - same candidates, different input order
            var candidates1 = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.9),
                CreateCandidate("Op2", "c1", baseScore: 0.8),
                CreateCandidate("Op3", "c1", baseScore: 0.7)
            };

            var candidates2 = new[]
            {
                CreateCandidate("Op3", "c1", baseScore: 0.7),
                CreateCandidate("Op1", "c1", baseScore: 0.9),
                CreateCandidate("Op2", "c1", baseScore: 0.8)
            };

            // Act
            var engine = new OperatorSelectionEngine<string>();
            var result1 = engine.Select(candidates1, densityTarget: 10.0, hardCap: 10);
            var result2 = engine.Select(candidates2, densityTarget: 10.0, hardCap: 10);

            // Assert - same order (sorted by score)
            Assert.Equal(result1.Selected.Count, result2.Selected.Count);
            for (int i = 0; i < result1.Selected.Count; i++)
            {
                Assert.Equal(result1.Selected[i].OperatorId, result2.Selected[i].OperatorId);
            }
        }

        #endregion

        #region Random Tie-Break Tests

        [Fact]
        public void SelectWithRandomTieBreak_SameSeed_IdenticalOutput()
        {
            // Arrange - candidates with same score
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.8),
                CreateCandidate("Op2", "c1", baseScore: 0.8),
                CreateCandidate("Op3", "c1", baseScore: 0.8)
            };

            // Act - run twice with same seed
            Rng.Initialize(42);
            var engine1 = new OperatorSelectionEngine<string>();
            var result1 = engine1.SelectWithRandomTieBreak(
                candidates, densityTarget: 10.0, hardCap: 10, RandomPurpose.GrooveTieBreak);

            Rng.Initialize(42);
            var engine2 = new OperatorSelectionEngine<string>();
            var result2 = engine2.SelectWithRandomTieBreak(
                candidates, densityTarget: 10.0, hardCap: 10, RandomPurpose.GrooveTieBreak);

            // Assert - identical order
            Assert.Equal(result1.Selected.Count, result2.Selected.Count);
            for (int i = 0; i < result1.Selected.Count; i++)
            {
                Assert.Equal(result1.Selected[i].OperatorId, result2.Selected[i].OperatorId);
            }
        }

        [Fact]
        public void SelectWithRandomTieBreak_DifferentSeed_DifferentOrder()
        {
            // Arrange - candidates with same score
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.8),
                CreateCandidate("Op2", "c1", baseScore: 0.8),
                CreateCandidate("Op3", "c1", baseScore: 0.8),
                CreateCandidate("Op4", "c1", baseScore: 0.8),
                CreateCandidate("Op5", "c1", baseScore: 0.8)
            };

            // Act - run with different seeds
            Rng.Initialize(42);
            var engine1 = new OperatorSelectionEngine<string>();
            var result1 = engine1.SelectWithRandomTieBreak(
                candidates, densityTarget: 10.0, hardCap: 10, RandomPurpose.GrooveTieBreak);

            Rng.Initialize(999);
            var engine2 = new OperatorSelectionEngine<string>();
            var result2 = engine2.SelectWithRandomTieBreak(
                candidates, densityTarget: 10.0, hardCap: 10, RandomPurpose.GrooveTieBreak);

            // Assert - likely different order (with 5 candidates, extremely unlikely to be same)
            var order1 = string.Join(",", result1.Selected.Select(c => c.OperatorId));
            var order2 = string.Join(",", result2.Selected.Select(c => c.OperatorId));

            // Note: This could theoretically fail with probability 1/120, but practically never will
            Assert.NotEqual(order1, order2);
        }

        #endregion

        #region Score Computation Integration Tests

        [Fact]
        public void Select_HighPenalty_LowersSelection()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.9, penalty: 0.0), // Final: 0.9
                CreateCandidate("Op2", "c1", baseScore: 0.9, penalty: 0.8), // Final: 0.18
                CreateCandidate("Op3", "c1", baseScore: 0.5, penalty: 0.0)  // Final: 0.5
            };

            // Act
            var result = engine.Select(candidates, densityTarget: 10.0, hardCap: 10);

            // Assert - Op2 should be last despite same base score as Op1
            Assert.Equal("Op1", result.Selected[0].OperatorId);
            Assert.Equal("Op3", result.Selected[1].OperatorId);
            Assert.Equal("Op2", result.Selected[2].OperatorId);
        }

        [Fact]
        public void Select_LowStyleWeight_LowersSelection()
        {
            // Arrange
            var engine = new OperatorSelectionEngine<string>();
            var candidates = new[]
            {
                CreateCandidate("Op1", "c1", baseScore: 0.8, styleWeight: 1.0), // Final: 0.8
                CreateCandidate("Op2", "c1", baseScore: 0.8, styleWeight: 0.25), // Final: 0.2
                CreateCandidate("Op3", "c1", baseScore: 0.5, styleWeight: 1.0)   // Final: 0.5
            };

            // Act
            var result = engine.Select(candidates, densityTarget: 10.0, hardCap: 10);

            // Assert - Op2 should be last despite same base score as Op1
            Assert.Equal("Op1", result.Selected[0].OperatorId);
            Assert.Equal("Op3", result.Selected[1].OperatorId);
            Assert.Equal("Op2", result.Selected[2].OperatorId);
        }

        #endregion

        #region CreateScoredCandidate Factory Tests

        [Fact]
        public void CreateScoredCandidate_WithMemory_GetsRepetitionPenalty()
        {
            // Arrange
            var memory = new AgentMemory(windowSize: 8);
            memory.RecordDecision(1, "Op1", "c1");
            memory.RecordDecision(2, "Op1", "c2");

            // Act
            var scored = OperatorSelectionEngine<string>.CreateScoredCandidate(
                candidate: "test",
                operatorId: "Op1",
                candidateId: "c3",
                baseScore: 1.0,
                styleWeight: 1.0,
                densityContribution: 1.0,
                memory: memory);

            // Assert - should have penalty > 0 from memory
            Assert.True(scored.RepetitionPenalty > 0.0);
            Assert.True(scored.FinalScore < 1.0);
        }

        [Fact]
        public void CreateScoredCandidate_ExplicitPenalty_UsesProvidedValue()
        {
            // Act
            var scored = OperatorSelectionEngine<string>.CreateScoredCandidate(
                candidate: "test",
                operatorId: "Op1",
                candidateId: "c1",
                baseScore: 1.0,
                styleWeight: 1.0,
                densityContribution: 1.0,
                repetitionPenalty: 0.5);

            // Assert
            Assert.Equal(0.5, scored.RepetitionPenalty);
            Assert.Equal(0.5, scored.FinalScore, precision: 4);
        }

        #endregion
    }
}
