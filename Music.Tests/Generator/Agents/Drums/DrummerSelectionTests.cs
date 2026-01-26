// AI: purpose=Unit tests for Story 10.8.2 AC2, AC3, AC5: Selection engine, operator weights, memory penalties, density targets.
// AI: deps=xUnit, DrummerPolicyProvider, AgentMemory, StyleConfiguration.
// AI: change=Story 10.8.2: verify weighted selection, memory effects, density enforcement (unit-level focus).

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Common;
using Music.Generator.Groove;
using Music;

namespace Music.Generator.Agents.Drums.Tests
{
    /// <summary>
    /// Story 10.8.2 AC2, AC3, AC5: Unit-level tests for selection, weighting, memory, and density.
    /// Note: Comprehensive selection tests exist in OperatorSelectionEngineTests.cs.
    /// These tests verify high-level acceptance criteria integration.
    /// </summary>
    public class DrummerSelectionTests
    {
        public DrummerSelectionTests()
        {
            Rng.Initialize(6001);
        }

        #region AC2: Operator Weights Affect Selection Frequency

        [Fact]
        public void Selection_StyleConfiguration_HasOperatorWeights()
        {
            // Arrange & Act
            var popRockStyle = StyleConfigurationLibrary.PopRock;

            // Assert: PopRock has operator weight configuration
            Assert.NotNull(popRockStyle.OperatorWeights);
        }

        [Fact]
        public void Selection_PolicyProvider_UsesDensityTargets()
        {
            // Arrange
            var policyProvider = new DrummerPolicyProvider(StyleConfigurationLibrary.PopRock);
            var context = CreateBarContext(MusicConstants.eSectionType.Verse);

            // Act: Get policy with density override
            var policy = policyProvider.GetPolicy(context, GrooveRoles.Snare);

            // Assert: Policy provides density override
            Assert.NotNull(policy.Density01Override);
            Assert.True(policy.Density01Override.Value > 0.0);
        }

        #endregion

        #region AC3: Memory Penalty Affects Repetition

        [Fact]
        public void Memory_TracksRecentOperatorUsage()
        {
            // Arrange
            var memory = new AgentMemory();

            // Act: Record several decisions
            memory.RecordDecision(1, "OpA", "candidate1");
            memory.RecordDecision(2, "OpA", "candidate2");
            memory.RecordDecision(3, "OpB", "candidate3");

            var recentUsage = memory.GetRecentOperatorUsage(lastNBars: 3);

            // Assert: Memory tracks operator usage
            Assert.True(recentUsage.ContainsKey("OpA"));
            Assert.Equal(2, recentUsage["OpA"]);
            Assert.True(recentUsage.ContainsKey("OpB"));
            Assert.Equal(1, recentUsage["OpB"]);
        }

        [Fact]
        public void Memory_GetRepetitionPenalty_IncreasesWithUsage()
        {
            // Arrange
            var memory = new AgentMemory();

            // Record frequent usage of OpA
            for (int i = 1; i <= 5; i++)
            {
                memory.RecordDecision(i, "OpA", $"candidate{i}");
            }

            // Act: Get penalty for frequently used operator
            var penaltyForFrequentOp = memory.GetRepetitionPenalty("OpA");
            var penaltyForUnusedOp = memory.GetRepetitionPenalty("OpB");

            // Assert: Frequently used operator has higher penalty
            Assert.True(penaltyForFrequentOp > penaltyForUnusedOp,
                $"Expected penalty for OpA ({penaltyForFrequentOp}) > OpB ({penaltyForUnusedOp})");
        }

        [Fact]
        public void Memory_SectionSignature_Tracked()
        {
            // Arrange
            var memory = new AgentMemory();

            // Act: Record decisions
            memory.RecordDecision(1, "CrashOp", "crash1");
            var signature = memory.GetSectionSignature(MusicConstants.eSectionType.Chorus);

            // Assert: Section signature can be retrieved
            Assert.NotNull(signature);
        }

        #endregion

        #region AC5: Density Targets Respected

        [Fact]
        public void Density_RoleCaps_DefinedInStyleConfiguration()
        {
            // Arrange & Act
            var popRockStyle = StyleConfigurationLibrary.PopRock;

            // Assert: Style has role caps defined
            Assert.NotNull(popRockStyle.RoleCaps);
            Assert.True(popRockStyle.RoleCaps.Count > 0);
        }

        [Fact]
        public void Density_PolicyProvider_ReturnsMaxEventsOverride()
        {
            // Arrange
            var policyProvider = new DrummerPolicyProvider(StyleConfigurationLibrary.PopRock);
            var context = CreateBarContext(MusicConstants.eSectionType.Chorus);

            // Act
            var policy = policyProvider.GetPolicy(context, GrooveRoles.Snare);

            // Assert: Policy provides max events cap
            Assert.NotNull(policy.MaxEventsPerBarOverride);
        }

        [Fact]
        public void Density_DifferentSections_DifferentTargets()
        {
            // Arrange
            var policyProvider = new DrummerPolicyProvider(StyleConfigurationLibrary.PopRock);
            
            var verseContext = CreateBarContext(MusicConstants.eSectionType.Verse);
            var chorusContext = CreateBarContext(MusicConstants.eSectionType.Chorus);

            // Act
            var versePolicy = policyProvider.GetPolicy(verseContext, GrooveRoles.Snare);
            var chorusPolicy = policyProvider.GetPolicy(chorusContext, GrooveRoles.Snare);

            // Assert: Different sections have different density targets
            Assert.NotEqual(versePolicy.Density01Override, chorusPolicy.Density01Override);
        }

        #endregion

        #region Helper Methods

        private static GrooveBarContext CreateBarContext(MusicConstants.eSectionType sectionType)
        {
            var section = new Music.Generator.Section
            {
                SectionType = sectionType,
                StartBar = 1,
                BarCount = 8
            };

            return new GrooveBarContext(
                BarNumber: 1,
                Section: section,
                SegmentProfile: null,
                BarWithinSection: 1,
                BarsUntilSectionEnd: 7);
        }

        #endregion
    }
}

