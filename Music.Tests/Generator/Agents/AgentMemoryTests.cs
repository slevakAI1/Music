// AI: purpose=Unit tests for Story 1.2 AgentMemory (anti-repetition memory system).
// AI: deps=xunit for test framework; Music.Generator.Agents.Common for types under test.
// AI: change=Story 1.2 acceptance criteria: memory tracks usage, penalties increase with repetition, decay works, determinism.

using Xunit;
using Music.Generator.Agents.Common;
using Music;

namespace Music.Generator.Agents.Common.Tests
{
    /// <summary>
    /// Story 1.2: Tests for AgentMemory anti-repetition system.
    /// Verifies operator tracking, repetition penalties, decay curves, and determinism.
    /// </summary>
    [Collection("RngDependentTests")]
    public class AgentMemoryTests
    {
        public AgentMemoryTests()
        {
            Rng.Initialize(42);
        }

        #region Constructor Tests

        [Fact]
        public void AgentMemory_DefaultConstructor_CreatesValidInstance()
        {
            // Act
            var memory = new AgentMemory();

            // Assert
            Assert.Equal(0, memory.CurrentBarNumber);
            Assert.Null(memory.GetLastFillShape());
        }

        [Fact]
        public void AgentMemory_CustomWindowSize_Accepted()
        {
            // Act
            var memory = new AgentMemory(windowSize: 4);

            // Assert - no exception, valid instance
            Assert.NotNull(memory);
        }

        [Fact]
        public void AgentMemory_InvalidWindowSize_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new AgentMemory(windowSize: 0));
        }

        [Fact]
        public void AgentMemory_InvalidDecayFactor_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new AgentMemory(decayFactor: 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AgentMemory(decayFactor: 1.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AgentMemory(decayFactor: -0.5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AgentMemory(decayFactor: 1.5));
        }

        #endregion

        #region RecordDecision Tests

        [Fact]
        public void RecordDecision_UpdatesCurrentBarNumber()
        {
            // Arrange
            var memory = new AgentMemory();

            // Act
            memory.RecordDecision(5, "GhostNote", "ghost-1.75");

            // Assert
            Assert.Equal(5, memory.CurrentBarNumber);
        }

        [Fact]
        public void RecordDecision_MultipleDecisions_TracksAll()
        {
            // Arrange
            var memory = new AgentMemory();

            // Act
            memory.RecordDecision(1, "GhostNote", "ghost-1.75");
            memory.RecordDecision(1, "GhostNote", "ghost-3.75");
            memory.RecordDecision(2, "Fill", "fill-basic");

            // Assert
            var usage = memory.GetRecentOperatorUsage(10);
            Assert.Equal(2, usage["GhostNote"]);
            Assert.Equal(1, usage["Fill"]);
        }

        [Fact]
        public void RecordDecision_InvalidBarNumber_Throws()
        {
            // Arrange
            var memory = new AgentMemory();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => memory.RecordDecision(0, "Op", "c"));
        }

        [Fact]
        public void RecordDecision_NullOperatorId_Throws()
        {
            // Arrange
            var memory = new AgentMemory();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => memory.RecordDecision(1, null!, "c"));
        }

        #endregion

        #region GetRecentOperatorUsage Tests

        [Fact]
        public void GetRecentOperatorUsage_EmptyMemory_ReturnsEmpty()
        {
            // Arrange
            var memory = new AgentMemory();

            // Act
            var usage = memory.GetRecentOperatorUsage(8);

            // Assert
            Assert.Empty(usage);
        }

        [Fact]
        public void GetRecentOperatorUsage_RespectsWindowSize()
        {
            // Arrange
            var memory = new AgentMemory(windowSize: 4);
            memory.RecordDecision(1, "Op1", "c1");
            memory.RecordDecision(2, "Op2", "c2");
            memory.RecordDecision(3, "Op3", "c3");
            memory.RecordDecision(4, "Op4", "c4");
            memory.RecordDecision(5, "Op5", "c5");

            // Act - get usage from last 3 bars (bars 3, 4, 5)
            var usage = memory.GetRecentOperatorUsage(3);

            // Assert
            Assert.DoesNotContain("Op1", usage.Keys);
            Assert.DoesNotContain("Op2", usage.Keys);
            Assert.Contains("Op3", usage.Keys);
            Assert.Contains("Op4", usage.Keys);
            Assert.Contains("Op5", usage.Keys);
        }

        [Fact]
        public void GetRecentOperatorUsage_ReturnsSortedKeys()
        {
            // Arrange
            var memory = new AgentMemory();
            memory.RecordDecision(1, "Zebra", "c1");
            memory.RecordDecision(1, "Alpha", "c2");
            memory.RecordDecision(1, "Middle", "c3");

            // Act
            var usage = memory.GetRecentOperatorUsage(8);
            var keys = usage.Keys.ToList();

            // Assert - deterministic sorted order
            Assert.Equal("Alpha", keys[0]);
            Assert.Equal("Middle", keys[1]);
            Assert.Equal("Zebra", keys[2]);
        }

        #endregion

        #region GetRepetitionPenalty Tests

        [Fact]
        public void GetRepetitionPenalty_NeverUsed_ReturnsZero()
        {
            // Arrange
            var memory = new AgentMemory();
            memory.RecordDecision(1, "OtherOp", "c1");

            // Act
            double penalty = memory.GetRepetitionPenalty("GhostNote");

            // Assert
            Assert.Equal(0.0, penalty);
        }

        [Fact]
        public void GetRepetitionPenalty_RecentUse_ReturnsPositive()
        {
            // Arrange
            var memory = new AgentMemory(windowSize: 8);
            memory.RecordDecision(1, "GhostNote", "ghost-1.75");

            // Act
            double penalty = memory.GetRepetitionPenalty("GhostNote");

            // Assert
            Assert.True(penalty > 0.0);
        }

        [Fact]
        public void GetRepetitionPenalty_MoreUsage_HigherPenalty()
        {
            // Arrange
            var memory1 = new AgentMemory(windowSize: 8);
            memory1.RecordDecision(1, "GhostNote", "g1");

            var memory2 = new AgentMemory(windowSize: 8);
            memory2.RecordDecision(1, "GhostNote", "g1");
            memory2.RecordDecision(2, "GhostNote", "g2");
            memory2.RecordDecision(3, "GhostNote", "g3");

            // Act
            double penalty1 = memory1.GetRepetitionPenalty("GhostNote");
            double penalty2 = memory2.GetRepetitionPenalty("GhostNote");

            // Assert - more usage = higher penalty
            Assert.True(penalty2 > penalty1);
        }

        [Fact]
        public void GetRepetitionPenalty_RecentUse_HigherThanOldUse_Exponential()
        {
            // Arrange - exponential decay penalizes recent use more
            var memory = new AgentMemory(windowSize: 8, decayCurve: DecayCurve.Exponential);
            memory.RecordDecision(1, "OldOp", "c1");
            memory.RecordDecision(8, "RecentOp", "c2"); // Current bar

            // Act
            double oldPenalty = memory.GetRepetitionPenalty("OldOp");
            double recentPenalty = memory.GetRepetitionPenalty("RecentOp");

            // Assert - recent use has higher penalty
            Assert.True(recentPenalty > oldPenalty);
        }

        [Fact]
        public void GetRepetitionPenalty_LinearDecay_UniformDecay()
        {
            // Arrange
            var memory = new AgentMemory(windowSize: 4, decayCurve: DecayCurve.Linear);
            memory.RecordDecision(1, "Op", "c1");
            memory.RecordDecision(4, "Dummy", "d1"); // Move current bar to 4

            // Act - Op was used at bar 1, now at bar 4 (age = 3)
            double penalty = memory.GetRepetitionPenalty("Op");

            // Assert - linear decay: (4 - 3) / 4 = 0.25 normalized
            Assert.True(penalty > 0.0 && penalty < 0.5);
        }

        [Fact]
        public void GetRepetitionPenalty_MaxPenalty_ClampedToOne()
        {
            // Arrange - saturate with many uses
            var memory = new AgentMemory(windowSize: 4);
            for (int bar = 1; bar <= 10; bar++)
            {
                memory.RecordDecision(bar, "OverusedOp", $"c{bar}");
                memory.RecordDecision(bar, "OverusedOp", $"c{bar}b");
            }

            // Act
            double penalty = memory.GetRepetitionPenalty("OverusedOp");

            // Assert - clamped to 1.0
            Assert.True(penalty <= 1.0);
        }

        #endregion

        #region Fill Shape Tests

        [Fact]
        public void RecordFillShape_StoresShape()
        {
            // Arrange
            var memory = new AgentMemory();
            var fillShape = new FillShape(
                BarPosition: 8,
                RolesInvolved: new List<string> { "Snare", "Toms" },
                DensityLevel: 0.8,
                DurationBars: 1.0m,
                FillTag: "BigFill");

            // Act
            memory.RecordFillShape(fillShape);

            // Assert
            var retrieved = memory.GetLastFillShape();
            Assert.NotNull(retrieved);
            Assert.Equal(8, retrieved.BarPosition);
            Assert.Equal("BigFill", retrieved.FillTag);
        }

        [Fact]
        public void RecordFillShape_OverwritesPrevious()
        {
            // Arrange
            var memory = new AgentMemory();
            var fill1 = new FillShape(4, new[] { "Snare" }, 0.5, 1.0m, "Small");
            var fill2 = new FillShape(8, new[] { "Snare", "Toms" }, 0.9, 1.0m, "Big");

            // Act
            memory.RecordFillShape(fill1);
            memory.RecordFillShape(fill2);

            // Assert - only last fill stored
            var retrieved = memory.GetLastFillShape();
            Assert.Equal("Big", retrieved?.FillTag);
        }

        #endregion

        #region Section Signature Tests

        [Fact]
        public void RecordSectionSignature_StoresSignature()
        {
            // Arrange
            var memory = new AgentMemory();

            // Act
            memory.RecordSectionSignature(MusicConstants.eSectionType.Chorus, "PowerBackbeat");
            memory.RecordSectionSignature(MusicConstants.eSectionType.Chorus, "OpenHatEmphasis");

            // Assert
            var signature = memory.GetSectionSignature(MusicConstants.eSectionType.Chorus);
            Assert.Equal(2, signature.Count);
            Assert.Contains("PowerBackbeat", signature);
            Assert.Contains("OpenHatEmphasis", signature);
        }

        [Fact]
        public void RecordSectionSignature_DuplicatesIgnored()
        {
            // Arrange
            var memory = new AgentMemory();

            // Act
            memory.RecordSectionSignature(MusicConstants.eSectionType.Verse, "Op1");
            memory.RecordSectionSignature(MusicConstants.eSectionType.Verse, "Op1");
            memory.RecordSectionSignature(MusicConstants.eSectionType.Verse, "Op1");

            // Assert - only one entry
            var signature = memory.GetSectionSignature(MusicConstants.eSectionType.Verse);
            Assert.Single(signature);
        }

        [Fact]
        public void GetSectionSignature_ReturnsSortedOrder()
        {
            // Arrange
            var memory = new AgentMemory();
            memory.RecordSectionSignature(MusicConstants.eSectionType.Chorus, "Zebra");
            memory.RecordSectionSignature(MusicConstants.eSectionType.Chorus, "Alpha");
            memory.RecordSectionSignature(MusicConstants.eSectionType.Chorus, "Middle");

            // Act
            var signature = memory.GetSectionSignature(MusicConstants.eSectionType.Chorus);

            // Assert - deterministic sorted order
            Assert.Equal("Alpha", signature[0]);
            Assert.Equal("Middle", signature[1]);
            Assert.Equal("Zebra", signature[2]);
        }

        [Fact]
        public void GetSectionSignature_UnknownSection_ReturnsEmpty()
        {
            // Arrange
            var memory = new AgentMemory();
            memory.RecordSectionSignature(MusicConstants.eSectionType.Chorus, "Op1");

            // Act
            var signature = memory.GetSectionSignature(MusicConstants.eSectionType.Bridge);

            // Assert
            Assert.Empty(signature);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_ResetsAllState()
        {
            // Arrange
            var memory = new AgentMemory();
            memory.RecordDecision(1, "Op1", "c1");
            memory.RecordDecision(2, "Op2", "c2");
            memory.RecordFillShape(new FillShape(1, new[] { "Snare" }, 0.5, 1.0m));
            memory.RecordSectionSignature(MusicConstants.eSectionType.Chorus, "Sig1");

            // Act
            memory.Clear();

            // Assert
            Assert.Equal(0, memory.CurrentBarNumber);
            Assert.Empty(memory.GetRecentOperatorUsage(10));
            Assert.Null(memory.GetLastFillShape());
            Assert.Empty(memory.GetSectionSignature(MusicConstants.eSectionType.Chorus));
        }

        #endregion

        #region Window Pruning Tests

        [Fact]
        public void RecordDecision_PrunesOldEntries()
        {
            // Arrange
            var memory = new AgentMemory(windowSize: 4);
            memory.RecordDecision(1, "OldOp", "c1");
            memory.RecordDecision(2, "Op2", "c2");
            memory.RecordDecision(3, "Op3", "c3");
            memory.RecordDecision(4, "Op4", "c4");

            // Act - add decision at bar 10, should prune bars 1-6
            memory.RecordDecision(10, "NewOp", "c5");
            var usage = memory.GetRecentOperatorUsage(10);

            // Assert - old entries pruned, only bar 7-10 in window
            Assert.DoesNotContain("OldOp", usage.Keys);
            Assert.DoesNotContain("Op2", usage.Keys);
            Assert.DoesNotContain("Op3", usage.Keys);
            Assert.DoesNotContain("Op4", usage.Keys);
            Assert.Contains("NewOp", usage.Keys);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void AgentMemory_SameSequence_SameState()
        {
            // Arrange
            var memory1 = new AgentMemory(windowSize: 8, decayCurve: DecayCurve.Exponential);
            var memory2 = new AgentMemory(windowSize: 8, decayCurve: DecayCurve.Exponential);

            // Act - same sequence of operations
            var operations = new[]
            {
                (1, "GhostNote", "g1"),
                (2, "Fill", "f1"),
                (3, "GhostNote", "g2"),
                (3, "Accent", "a1"),
                (4, "GhostNote", "g3")
            };

            foreach (var (bar, op, cand) in operations)
            {
                memory1.RecordDecision(bar, op, cand);
                memory2.RecordDecision(bar, op, cand);
            }

            // Assert - identical state
            Assert.Equal(memory1.CurrentBarNumber, memory2.CurrentBarNumber);

            var usage1 = memory1.GetRecentOperatorUsage(8);
            var usage2 = memory2.GetRecentOperatorUsage(8);
            Assert.Equal(usage1.Count, usage2.Count);
            foreach (var key in usage1.Keys)
            {
                Assert.Equal(usage1[key], usage2[key]);
            }

            // Same penalties
            Assert.Equal(
                memory1.GetRepetitionPenalty("GhostNote"),
                memory2.GetRepetitionPenalty("GhostNote"));
            Assert.Equal(
                memory1.GetRepetitionPenalty("Fill"),
                memory2.GetRepetitionPenalty("Fill"));
        }

        [Fact]
        public void GetRecentOperatorUsage_DeterministicIteration()
        {
            // Arrange
            var memory = new AgentMemory();

            // Add in non-sorted order
            memory.RecordDecision(1, "Charlie", "c1");
            memory.RecordDecision(1, "Alpha", "c2");
            memory.RecordDecision(1, "Bravo", "c3");

            // Act - call multiple times
            var usage1 = memory.GetRecentOperatorUsage(8).Keys.ToList();
            var usage2 = memory.GetRecentOperatorUsage(8).Keys.ToList();

            // Assert - same order every time
            Assert.Equal(usage1, usage2);
            Assert.Equal("Alpha", usage1[0]);
            Assert.Equal("Bravo", usage1[1]);
            Assert.Equal("Charlie", usage1[2]);
        }

        #endregion
    }
}
