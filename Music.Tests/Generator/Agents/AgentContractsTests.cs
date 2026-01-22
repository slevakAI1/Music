// AI: purpose=Unit tests for Story 1.1 agent contracts (OperatorFamily, FillShape, AgentContext, IAgentMemory, IMusicalOperator).
// AI: deps=xunit for test framework; Music.Generator.Agents.Common for types under test.
// AI: change=Story 1.1 acceptance criteria: verify interfaces compile and can be mocked.

using Xunit;
using Music.Generator.Agents.Common;
using Music;

namespace Music.Generator.Agents.Common.Tests
{
    /// <summary>
    /// Story 1.1: Tests for common agent contracts.
    /// Verifies OperatorFamily, FillShape, AgentContext, IAgentMemory, and IMusicalOperator.
    /// </summary>
    [Collection("RngDependentTests")]
    public class AgentContractsTests
    {
        public AgentContractsTests()
        {
            Rng.Initialize(42);
        }

        #region OperatorFamily Tests

        [Fact]
        public void OperatorFamily_HasFiveValues()
        {
            // Assert
            var values = Enum.GetValues<OperatorFamily>();
            Assert.Equal(5, values.Length);
        }

        [Fact]
        public void OperatorFamily_ValuesAreStable()
        {
            // Verify explicit enum values for determinism
            Assert.Equal(0, (int)OperatorFamily.MicroAddition);
            Assert.Equal(1, (int)OperatorFamily.SubdivisionTransform);
            Assert.Equal(2, (int)OperatorFamily.PhrasePunctuation);
            Assert.Equal(3, (int)OperatorFamily.PatternSubstitution);
            Assert.Equal(4, (int)OperatorFamily.StyleIdiom);
        }

        [Theory]
        [InlineData(OperatorFamily.MicroAddition, "MicroAddition")]
        [InlineData(OperatorFamily.SubdivisionTransform, "SubdivisionTransform")]
        [InlineData(OperatorFamily.PhrasePunctuation, "PhrasePunctuation")]
        [InlineData(OperatorFamily.PatternSubstitution, "PatternSubstitution")]
        [InlineData(OperatorFamily.StyleIdiom, "StyleIdiom")]
        public void OperatorFamily_NamesMatchExpected(OperatorFamily family, string expectedName)
        {
            Assert.Equal(expectedName, family.ToString());
        }

        #endregion

        #region FillShape Tests

        [Fact]
        public void FillShape_CanCreate_WithValidParameters()
        {
            // Arrange & Act
            var fillShape = new FillShape(
                BarPosition: 5,
                RolesInvolved: new List<string> { "Snare", "Kick" },
                DensityLevel: 0.75,
                DurationBars: 1.0m,
                FillTag: "SnareRoll");

            // Assert
            Assert.Equal(5, fillShape.BarPosition);
            Assert.Equal(2, fillShape.RolesInvolved.Count);
            Assert.Contains("Snare", fillShape.RolesInvolved);
            Assert.Contains("Kick", fillShape.RolesInvolved);
            Assert.Equal(0.75, fillShape.DensityLevel);
            Assert.Equal(1.0m, fillShape.DurationBars);
            Assert.Equal("SnareRoll", fillShape.FillTag);
        }

        [Fact]
        public void FillShape_Empty_HasNoContent()
        {
            // Act
            var empty = FillShape.Empty;

            // Assert
            Assert.Equal(0, empty.BarPosition);
            Assert.Empty(empty.RolesInvolved);
            Assert.Equal(0.0, empty.DensityLevel);
            Assert.Equal(0m, empty.DurationBars);
            Assert.False(empty.HasContent);
        }

        [Fact]
        public void FillShape_HasContent_TrueWhenPopulated()
        {
            // Arrange
            var fillShape = new FillShape(
                BarPosition: 1,
                RolesInvolved: new List<string> { "Snare" },
                DensityLevel: 0.5,
                DurationBars: 1.0m);

            // Assert
            Assert.True(fillShape.HasContent);
        }

        [Fact]
        public void FillShape_HasContent_FalseWhenEmptyRoles()
        {
            // Arrange
            var fillShape = new FillShape(
                BarPosition: 1,
                RolesInvolved: Array.Empty<string>(),
                DensityLevel: 0.5,
                DurationBars: 1.0m);

            // Assert
            Assert.False(fillShape.HasContent);
        }

        [Fact]
        public void FillShape_IsImmutable()
        {
            // Verify record is immutable by checking it can't be modified after creation
            var fillShape = new FillShape(
                BarPosition: 5,
                RolesInvolved: new List<string> { "Snare" },
                DensityLevel: 0.75,
                DurationBars: 1.0m);

            // Records support with-expressions for creating modified copies
            var modified = fillShape with { BarPosition = 10 };

            // Original unchanged
            Assert.Equal(5, fillShape.BarPosition);
            Assert.Equal(10, modified.BarPosition);
        }

        #endregion

        #region AgentContext Tests

        [Fact]
        public void AgentContext_CanCreate_WithRequiredFields()
        {
            // Arrange & Act
            var context = new AgentContext
            {
                BarNumber = 5,
                Beat = 2.5m,
                SectionType = MusicConstants.eSectionType.Chorus,
                PhrasePosition = 0.5,
                BarsUntilSectionEnd = 3,
                EnergyLevel = 0.8,
                TensionLevel = 0.3,
                MotifPresenceScore = 0.6,
                Seed = 42,
                RngStreamKey = "Drum_Bar5"
            };

            // Assert
            Assert.Equal(5, context.BarNumber);
            Assert.Equal(2.5m, context.Beat);
            Assert.Equal(MusicConstants.eSectionType.Chorus, context.SectionType);
            Assert.Equal(0.5, context.PhrasePosition);
            Assert.Equal(3, context.BarsUntilSectionEnd);
            Assert.Equal(0.8, context.EnergyLevel);
            Assert.Equal(0.3, context.TensionLevel);
            Assert.Equal(0.6, context.MotifPresenceScore);
            Assert.Equal(42, context.Seed);
            Assert.Equal("Drum_Bar5", context.RngStreamKey);
        }

        [Fact]
        public void AgentContext_CreateMinimal_ReturnsValidContext()
        {
            // Act
            var context = AgentContext.CreateMinimal(barNumber: 10, sectionType: MusicConstants.eSectionType.Bridge, seed: 123);

            // Assert
            Assert.Equal(10, context.BarNumber);
            Assert.Equal(1.0m, context.Beat);
            Assert.Equal(MusicConstants.eSectionType.Bridge, context.SectionType);
            Assert.Equal(0.0, context.PhrasePosition);
            Assert.Equal(4, context.BarsUntilSectionEnd);
            Assert.Equal(0.5, context.EnergyLevel);
            Assert.Equal(0.0, context.TensionLevel);
            Assert.Equal(0.0, context.MotifPresenceScore);
            Assert.Equal(123, context.Seed);
            Assert.Equal("Test_10", context.RngStreamKey);
        }

        [Fact]
        public void AgentContext_IsImmutable()
        {
            // Arrange
            var original = AgentContext.CreateMinimal();

            // Act
            var modified = original with { BarNumber = 99 };

            // Assert - original unchanged
            Assert.Equal(1, original.BarNumber);
            Assert.Equal(99, modified.BarNumber);
        }

        [Theory]
        [InlineData(MusicConstants.eSectionType.Intro)]
        [InlineData(MusicConstants.eSectionType.Verse)]
        [InlineData(MusicConstants.eSectionType.Chorus)]
        [InlineData(MusicConstants.eSectionType.Solo)]
        [InlineData(MusicConstants.eSectionType.Bridge)]
        [InlineData(MusicConstants.eSectionType.Outro)]
        [InlineData(MusicConstants.eSectionType.Custom)]
        public void AgentContext_AllSectionTypes_Supported(MusicConstants.eSectionType sectionType)
        {
            // Act
            var context = AgentContext.CreateMinimal(sectionType: sectionType);

            // Assert
            Assert.Equal(sectionType, context.SectionType);
        }

        #endregion

        #region IAgentMemory Mock Tests

        [Fact]
        public void IAgentMemory_CanBeMocked()
        {
            // Arrange
            var mockMemory = new MockAgentMemory();

            // Act
            mockMemory.RecordDecision(1, "GhostNote", "ghost-1.75");
            var usage = mockMemory.GetRecentOperatorUsage(4);

            // Assert
            Assert.Single(usage);
            Assert.True(usage.ContainsKey("GhostNote"));
            Assert.Equal(1, usage["GhostNote"]);
        }

        [Fact]
        public void IAgentMemory_FillShape_CanBeRecordedAndRetrieved()
        {
            // Arrange
            var mockMemory = new MockAgentMemory();
            var fillShape = new FillShape(
                BarPosition: 8,
                RolesInvolved: new List<string> { "Snare", "Toms" },
                DensityLevel: 0.9,
                DurationBars: 1.0m,
                FillTag: "BigFill");

            // Act
            mockMemory.RecordFillShape(fillShape);
            var retrieved = mockMemory.GetLastFillShape();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(8, retrieved.BarPosition);
            Assert.Equal("BigFill", retrieved.FillTag);
        }

        [Fact]
        public void IAgentMemory_SectionSignature_CanBeRecordedAndRetrieved()
        {
            // Arrange
            var mockMemory = new MockAgentMemory();

            // Act
            mockMemory.RecordSectionSignature(MusicConstants.eSectionType.Chorus, "PowerfulBackbeat");
            mockMemory.RecordSectionSignature(MusicConstants.eSectionType.Chorus, "OpenHatEmphasis");
            var signature = mockMemory.GetSectionSignature(MusicConstants.eSectionType.Chorus);

            // Assert
            Assert.Equal(2, signature.Count);
            Assert.Contains("PowerfulBackbeat", signature);
            Assert.Contains("OpenHatEmphasis", signature);
        }

        [Fact]
        public void IAgentMemory_Clear_ResetsState()
        {
            // Arrange
            var mockMemory = new MockAgentMemory();
            mockMemory.RecordDecision(1, "Op1", "c1");
            mockMemory.RecordFillShape(new FillShape(1, new[] { "Snare" }, 0.5, 1.0m));

            // Act
            mockMemory.Clear();

            // Assert
            Assert.Empty(mockMemory.GetRecentOperatorUsage(10));
            Assert.Null(mockMemory.GetLastFillShape());
            Assert.Equal(0, mockMemory.CurrentBarNumber);
        }

        #endregion

        #region IMusicalOperator Mock Tests

        [Fact]
        public void IMusicalOperator_CanBeMocked()
        {
            // Arrange
            var mockOperator = new MockGhostNoteOperator();

            // Assert
            Assert.Equal("MockGhostNote", mockOperator.OperatorId);
            Assert.Equal(OperatorFamily.MicroAddition, mockOperator.OperatorFamily);
        }

        [Fact]
        public void IMusicalOperator_CanApply_ReturnsTrueWhenAppropriate()
        {
            // Arrange
            var mockOperator = new MockGhostNoteOperator();
            var context = AgentContext.CreateMinimal() with { EnergyLevel = 0.7, BarsUntilSectionEnd = 3 };

            // Act
            bool canApply = mockOperator.CanApply(context);

            // Assert
            Assert.True(canApply);
        }

        [Fact]
        public void IMusicalOperator_CanApply_ReturnsFalseNearSectionEnd()
        {
            // Arrange
            var mockOperator = new MockGhostNoteOperator();
            var context = AgentContext.CreateMinimal() with { EnergyLevel = 0.7, BarsUntilSectionEnd = 1 };

            // Act
            bool canApply = mockOperator.CanApply(context);

            // Assert - near fill window, ghost notes disabled
            Assert.False(canApply);
        }

        [Fact]
        public void IMusicalOperator_GenerateCandidates_ProducesCandidates()
        {
            // Arrange
            var mockOperator = new MockGhostNoteOperator();
            var context = AgentContext.CreateMinimal();

            // Act
            var candidates = mockOperator.GenerateCandidates(context).ToList();

            // Assert
            Assert.NotEmpty(candidates);
            Assert.All(candidates, c => Assert.StartsWith("ghost-", c.CandidateId));
        }

        [Fact]
        public void IMusicalOperator_Score_ReturnsValidRange()
        {
            // Arrange
            var mockOperator = new MockGhostNoteOperator();
            var context = AgentContext.CreateMinimal();
            var candidate = new MockDrumCandidate { CandidateId = "ghost-1.75", Beat = 1.75m, Role = "Snare" };

            // Act
            double score = mockOperator.Score(candidate, context);

            // Assert
            Assert.InRange(score, 0.0, 1.0);
        }

        #endregion
    }

    #region Mock Implementations

    /// <summary>
    /// Mock implementation of IAgentMemory for testing.
    /// </summary>
    internal class MockAgentMemory : IAgentMemory
    {
        private readonly Dictionary<int, List<(string OperatorId, string CandidateId)>> _decisions = new();
        private readonly Dictionary<MusicConstants.eSectionType, List<string>> _sectionSignatures = new();
        private FillShape? _lastFill;
        private int _currentBar;

        public int CurrentBarNumber => _currentBar;

        public void RecordDecision(int barNumber, string operatorId, string candidateId)
        {
            if (!_decisions.ContainsKey(barNumber))
                _decisions[barNumber] = new List<(string, string)>();
            _decisions[barNumber].Add((operatorId, candidateId));
            _currentBar = Math.Max(_currentBar, barNumber);
        }

        public IReadOnlyDictionary<string, int> GetRecentOperatorUsage(int lastNBars)
        {
            var result = new Dictionary<string, int>();
            int startBar = Math.Max(1, _currentBar - lastNBars + 1);

            foreach (var kvp in _decisions.Where(d => d.Key >= startBar))
            {
                foreach (var (opId, _) in kvp.Value)
                {
                    if (!result.ContainsKey(opId))
                        result[opId] = 0;
                    result[opId]++;
                }
            }

            return result;
        }

        public FillShape? GetLastFillShape() => _lastFill;

        public IReadOnlyList<string> GetSectionSignature(MusicConstants.eSectionType sectionType)
        {
            return _sectionSignatures.TryGetValue(sectionType, out var sig) ? sig : Array.Empty<string>();
        }

        public void RecordFillShape(FillShape fillShape) => _lastFill = fillShape;

        public void RecordSectionSignature(MusicConstants.eSectionType sectionType, string operatorId)
        {
            if (!_sectionSignatures.ContainsKey(sectionType))
                _sectionSignatures[sectionType] = new List<string>();
            if (!_sectionSignatures[sectionType].Contains(operatorId))
                _sectionSignatures[sectionType].Add(operatorId);
        }

        public void Clear()
        {
            _decisions.Clear();
            _sectionSignatures.Clear();
            _lastFill = null;
            _currentBar = 0;
        }
    }

    /// <summary>
    /// Mock drum candidate for testing IMusicalOperator.
    /// </summary>
    internal class MockDrumCandidate
    {
        public required string CandidateId { get; init; }
        public required decimal Beat { get; init; }
        public required string Role { get; init; }
        public double Score { get; init; } = 0.8;
    }

    /// <summary>
    /// Mock ghost note operator for testing IMusicalOperator interface.
    /// Demonstrates the operator pattern from Story 1.1 appendix.
    /// </summary>
    internal class MockGhostNoteOperator : IMusicalOperator<MockDrumCandidate>
    {
        public string OperatorId => "MockGhostNote";
        public OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        public bool CanApply(AgentContext context)
        {
            // Only apply if energy > 0.5 and not in fill window
            return context.EnergyLevel > 0.5 && context.BarsUntilSectionEnd > 1;
        }

        public IEnumerable<MockDrumCandidate> GenerateCandidates(AgentContext context)
        {
            // Generate ghost note candidates before backbeats
            yield return new MockDrumCandidate
            {
                CandidateId = "ghost-1.75",
                Beat = 1.75m,
                Role = "Snare",
                Score = 0.8
            };
            yield return new MockDrumCandidate
            {
                CandidateId = "ghost-3.75",
                Beat = 3.75m,
                Role = "Snare",
                Score = 0.7
            };
        }

        public double Score(MockDrumCandidate candidate, AgentContext context)
        {
            // Simple scoring based on energy
            return candidate.Score * context.EnergyLevel;
        }
    }

    #endregion
}
