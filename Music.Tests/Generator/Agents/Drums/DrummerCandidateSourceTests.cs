// AI: purpose=Unit tests for Story 2.4 DrummerCandidateSource.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums for types under test.
// AI: change=Story 2.4 acceptance criteria: determinism, operator generation, mapping, grouping, filtering.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Physicality;
using Music.Generator.Agents.Common;
using Music.Generator;
using Music;

namespace Music.Generator.Agents.Drums.Tests
{
    /// <summary>
    /// Story 2.4: Tests for DrummerCandidateSource.
    /// Verifies candidate generation, mapping, grouping, and physicality filtering.
    /// </summary>
    [Collection("RngDependentTests")]
    public class DrummerCandidateSourceTests
    {
        public DrummerCandidateSourceTests()
        {
            Rng.Initialize(42);
        }

        #region Construction Tests

        [Fact]
        public void Constructor_NullRegistry_Throws()
        {
            // Arrange
            var style = StyleConfigurationLibrary.PopRock;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DrummerCandidateSource(null!, style));
        }

        [Fact]
        public void Constructor_NullStyleConfig_Throws()
        {
            // Arrange
            var registry = DrumOperatorRegistry.CreateEmpty();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DrummerCandidateSource(registry, null!));
        }

        [Fact]
        public void Constructor_ValidInputs_Succeeds()
        {
            // Arrange
            var registry = DrumOperatorRegistry.CreateEmpty();
            var style = StyleConfigurationLibrary.PopRock;

            // Act
            var source = new DrummerCandidateSource(registry, style);

            // Assert
            Assert.NotNull(source);
        }

        #endregion

        #region GetCandidateGroups Basic Tests

        [Fact]
        public void GetCandidateGroups_NullBarContext_Throws()
        {
            // Arrange
            var source = CreateSource();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                source.GetCandidateGroups(null!, GrooveRoles.Kick));
        }

        [Fact]
        public void GetCandidateGroups_NullRole_Throws()
        {
            // Arrange
            var source = CreateSource();
            var barContext = CreateBarContext();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                source.GetCandidateGroups(barContext, null!));
        }

        [Fact]
        public void GetCandidateGroups_EmptyRegistry_ReturnsEmptyList()
        {
            // Arrange
            var source = CreateSource();
            var barContext = CreateBarContext();

            // Act
            var groups = source.GetCandidateGroups(barContext, GrooveRoles.Kick);

            // Assert
            Assert.NotNull(groups);
            Assert.Empty(groups);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void GetCandidateGroups_SameSeedAndContext_ReturnsSameCandidates()
        {
            // Arrange
            var registry = CreateRegistryWithTestOperators();
            var source = new DrummerCandidateSource(registry, StyleConfigurationLibrary.PopRock);
            var barContext = CreateBarContext();

            // Act - Call twice with same context
            Rng.Initialize(42);
            var result1 = source.GetCandidateGroups(barContext, GrooveRoles.Snare);

            Rng.Initialize(42);
            var result2 = source.GetCandidateGroups(barContext, GrooveRoles.Snare);

            // Assert - Results should be identical
            Assert.Equal(result1.Count, result2.Count);
            for (int i = 0; i < result1.Count; i++)
            {
                Assert.Equal(result1[i].GroupId, result2[i].GroupId);
                Assert.Equal(result1[i].Candidates.Count, result2[i].Candidates.Count);
            }
        }

        [Fact]
        public void GetCandidateGroups_DifferentSeeds_MayProduceDifferentResults()
        {
            // Arrange
            var registry = CreateRegistryWithTestOperators();
            var source = new DrummerCandidateSource(registry, StyleConfigurationLibrary.PopRock);
            var barContext1 = CreateBarContext(barNumber: 1);
            var barContext2 = CreateBarContext(barNumber: 2);

            // Act
            var result1 = source.GetCandidateGroups(barContext1, GrooveRoles.Snare);
            var result2 = source.GetCandidateGroups(barContext2, GrooveRoles.Snare);

            // Assert - Results should exist (may or may not differ based on operator logic)
            Assert.NotNull(result1);
            Assert.NotNull(result2);
        }

        #endregion

        #region Operator Execution Tests

        [Fact]
        public void GetCandidateGroups_OperatorsGenerate_ExpectedCandidates()
        {
            // Arrange
            var registry = CreateRegistryWithTestOperators();
            var source = new DrummerCandidateSource(registry, StyleConfigurationLibrary.PopRock);
            var barContext = CreateBarContext();

            // Act
            var groups = source.GetCandidateGroups(barContext, GrooveRoles.Snare);

            // Assert
            Assert.NotEmpty(groups);

            // Should have candidates from test operator
            var allCandidates = groups.SelectMany(g => g.Candidates).ToList();
            Assert.NotEmpty(allCandidates);
        }

        [Fact]
        public void GetCandidateGroups_TracksDiagnostics()
        {
            // Arrange
            var registry = CreateRegistryWithTestOperators();
            var source = new DrummerCandidateSource(registry, StyleConfigurationLibrary.PopRock);
            var barContext = CreateBarContext();

            // Act
            source.GetCandidateGroups(barContext, GrooveRoles.Snare);

            // Assert
            var diagnostics = source.LastExecutionDiagnostics;
            Assert.NotNull(diagnostics);
            Assert.NotEmpty(diagnostics);
        }

        [Fact]
        public void GetCandidateGroups_OperatorThrows_ContinuesWithOthers()
        {
            // Arrange
            var registry = DrumOperatorRegistry.CreateEmpty();
            registry.RegisterOperator(new ThrowingTestOperator());
            registry.RegisterOperator(new SimpleTestOperator("TestOp2"));
            registry.Freeze();

            var source = new DrummerCandidateSource(
                registry,
                StyleConfigurationLibrary.PopRock,
                settings: new DrummerCandidateSourceSettings { ContinueOnOperatorError = true });
            var barContext = CreateBarContext();

            // Act - Should not throw
            var groups = source.GetCandidateGroups(barContext, GrooveRoles.Snare);

            // Assert - Should have diagnostics with error
            var diagnostics = source.LastExecutionDiagnostics;
            Assert.NotNull(diagnostics);
            Assert.Contains(diagnostics, d => d.ErrorMessage != null);
            Assert.Contains(diagnostics, d => d.CandidatesGenerated > 0);
        }

        #endregion

        #region Mapping Tests

        [Fact]
        public void GetCandidateGroups_MapsDrumCandidate_ToGrooveOnsetCandidate()
        {
            // Arrange
            var registry = CreateRegistryWithTestOperators();
            var source = new DrummerCandidateSource(registry, StyleConfigurationLibrary.PopRock);
            var barContext = CreateBarContext();

            // Act
            var groups = source.GetCandidateGroups(barContext, GrooveRoles.Snare);

            // Assert
            var candidates = groups.SelectMany(g => g.Candidates).ToList();
            Assert.NotEmpty(candidates);

            foreach (var candidate in candidates)
            {
                Assert.NotNull(candidate.Role);
                Assert.True(candidate.OnsetBeat >= 1.0m);
                Assert.NotNull(candidate.Tags);

                // Should have CandidateId tag for traceability
                Assert.Contains(candidate.Tags, t => t.StartsWith(DrumCandidateMapper.CandidateIdTagPrefix));
            }
        }

        #endregion

        #region Grouping Tests

        [Fact]
        public void GetCandidateGroups_GroupsByOperatorFamily()
        {
            // Arrange
            var registry = DrumOperatorRegistry.CreateEmpty();
            registry.RegisterOperator(new SimpleTestOperator("MicroOp", OperatorFamily.MicroAddition));
            registry.RegisterOperator(new SimpleTestOperator("PhraseOp", OperatorFamily.PhrasePunctuation));
            registry.Freeze();

            var source = new DrummerCandidateSource(registry, StyleConfigurationLibrary.PopRock);
            var barContext = CreateBarContext();

            // Act
            var groups = source.GetCandidateGroups(barContext, GrooveRoles.Snare);

            // Assert - Should have groups for different families
            Assert.True(groups.Count >= 2);
            Assert.Contains(groups, g => g.GroupId == OperatorFamily.MicroAddition.ToString());
            Assert.Contains(groups, g => g.GroupId == OperatorFamily.PhrasePunctuation.ToString());
        }

        #endregion

        #region Physicality Filter Tests

        [Fact]
        public void GetCandidateGroups_WithPhysicalityFilter_PrunesExcessCandidates()
        {
            // Arrange
            var registry = DrumOperatorRegistry.CreateEmpty();
            // Register operator that generates many candidates
            registry.RegisterOperator(new ManyHitsTestOperator(candidateCount: 30));
            registry.Freeze();

            var physicalityFilter = new PhysicalityFilter(new PhysicalityRules { MaxHitsPerBar = 10 });
            var source = new DrummerCandidateSource(
                registry,
                StyleConfigurationLibrary.PopRock,
                physicalityFilter: physicalityFilter);
            var barContext = CreateBarContext();

            // Act
            var groups = source.GetCandidateGroups(barContext, GrooveRoles.Snare);

            // Assert - Total candidates should be <= 10
            var totalCandidates = groups.Sum(g => g.Candidates.Count);
            Assert.True(totalCandidates <= 10);
        }

        #endregion

        #region Helper Methods

        private static DrummerCandidateSource CreateSource()
        {
            var registry = DrumOperatorRegistry.CreateEmpty();
            var style = StyleConfigurationLibrary.PopRock;
            return new DrummerCandidateSource(registry, style);
        }

        private static DrumOperatorRegistry CreateRegistryWithTestOperators()
        {
            var registry = DrumOperatorRegistry.CreateEmpty();
            registry.RegisterOperator(new SimpleTestOperator("TestOperator1"));
            registry.Freeze();
            return registry;
        }

        private static GrooveBarContext CreateBarContext(int barNumber = 1)
        {
            return new GrooveBarContext(
                BarNumber: barNumber,
                Section: new Section
                {
                    SectionId = 1,
                    SectionType = MusicConstants.eSectionType.Verse,
                    StartBar = 1,
                    BarCount = 8
                },
                SegmentProfile: null,
                BarWithinSection: 0,
                BarsUntilSectionEnd: 7);
        }

        #endregion

        #region Test Operators

        /// <summary>
        /// Simple test operator that generates a few deterministic candidates.
        /// </summary>
        private sealed class SimpleTestOperator : IDrumOperator
        {
            private readonly string _id;
            private readonly OperatorFamily _family;

            public SimpleTestOperator(string id, OperatorFamily family = OperatorFamily.MicroAddition)
            {
                _id = id;
                _family = family;
            }

            public string OperatorId => _id;
            public OperatorFamily OperatorFamily => _family;

            public bool CanApply(AgentContext context) => true;

            public IEnumerable<DrumCandidate> GenerateCandidates(AgentContext context)
            {
                yield return DrumCandidate.CreateMinimal(
                    operatorId: _id,
                    role: GrooveRoles.Snare,
                    barNumber: context.BarNumber,
                    beat: 2.0m,
                    strength: OnsetStrength.Backbeat,
                    score: 0.8);

                yield return DrumCandidate.CreateMinimal(
                    operatorId: _id,
                    role: GrooveRoles.Snare,
                    barNumber: context.BarNumber,
                    beat: 4.0m,
                    strength: OnsetStrength.Backbeat,
                    score: 0.8);
            }

            public double Score(DrumCandidate candidate, AgentContext context) => candidate.Score;
        }

        /// <summary>
        /// Test operator that throws during generation.
        /// </summary>
        private sealed class ThrowingTestOperator : IDrumOperator
        {
            public string OperatorId => "ThrowingOp";
            public OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

            public bool CanApply(AgentContext context) => true;

            public IEnumerable<DrumCandidate> GenerateCandidates(AgentContext context)
            {
                throw new InvalidOperationException("Test exception");
            }

            public double Score(DrumCandidate candidate, AgentContext context) => 0.5;
        }

        /// <summary>
        /// Test operator that generates many candidates for overcrowding tests.
        /// </summary>
        private sealed class ManyHitsTestOperator : IDrumOperator
        {
            private readonly int _count;

            public ManyHitsTestOperator(int candidateCount)
            {
                _count = candidateCount;
            }

            public string OperatorId => "ManyHitsOp";
            public OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

            public bool CanApply(AgentContext context) => true;

            public IEnumerable<DrumCandidate> GenerateCandidates(AgentContext context)
            {
                for (int i = 0; i < _count; i++)
                {
                    decimal beat = 1.0m + (i * 0.25m);
                    yield return DrumCandidate.CreateMinimal(
                        operatorId: OperatorId,
                        role: GrooveRoles.Snare,
                        barNumber: context.BarNumber,
                        beat: beat,
                        strength: OnsetStrength.Ghost,
                        score: 0.3 + (i * 0.01));
                }
            }

            public double Score(DrumCandidate candidate, AgentContext context) => candidate.Score;
        }

        #endregion
    }
}
