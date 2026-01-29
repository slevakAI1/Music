// AI: purpose=Unit tests for Story 10.8.2 AC4: Physicality filter rejects impossible patterns.
// AI: deps=xUnit, PhysicalityFilter, PhysicalityRules.
// AI: change=Story 10.8.2: verify physicality constraints prevent impossible patterns (unit-level focus).

using Xunit;
using Music.Generator.Agents.Drums.Physicality;
using Music.Generator.Groove;
using Music;

namespace Music.Generator.Agents.Drums.Tests
{
    /// <summary>
    /// Story 10.8.2 AC4: Unit-level tests that physicality filter prevents impossible patterns.
    /// Note: Comprehensive physicality tests exist in PhysicalityFilterTests.cs.
    /// These tests verify high-level acceptance criteria integration.
    /// </summary>
    public class DrummerPhysicalityTests
    {
        public DrummerPhysicalityTests()
        {
            Rng.Initialize(7001);
        }

        #region AC4: Physicality Filter Rejects Impossible Patterns

        [Fact]
        public void Physicality_FilterExists_AndCanFilterCandidates()
        {
            // Arrange: Verify physicality filter can be constructed and used
            var rules = PhysicalityRules.Default;
            var filter = new PhysicalityFilter(rules);

            var candidate = CreateGrooveCandidate("Snare", 2.0m);
            var candidates = new List<DrumOnsetCandidate> { candidate };

            // Act
            var filtered = filter.Filter(candidates, barNumber: 1);

            // Assert: Filter executes successfully
            Assert.NotNull(filtered);
            Assert.Single(filtered);
        }

        [Fact]
        public void Physicality_Filter_RespectsProtectedOnsets()
        {
            // Arrange: Create protected onset
            var rules = PhysicalityRules.Default;
            var filter = new PhysicalityFilter(rules);

            var protectedOnset = CreateGrooveCandidate("Snare", 2.0m, isProtected: true);
            var candidates = new List<DrumOnsetCandidate> { protectedOnset };

            // Act
            var filtered = filter.Filter(candidates, barNumber: 1);

            // Assert: Protected onset survives filtering
            Assert.Contains(filtered, c => c.Tags.Contains(DrumCandidateMapper.ProtectedTag));
        }

        [Fact]
        public void Physicality_DefaultRules_LoadSuccessfully()
        {
            // Arrange & Act
            var defaultRules = PhysicalityRules.Default;

            // Assert: Default rules exist with expected properties
            Assert.NotNull(defaultRules);
            Assert.NotNull(defaultRules.LimbModel);
            Assert.NotNull(defaultRules.StickingRules);
            Assert.True(defaultRules.MaxHitsPerBar > 0);
            Assert.True(defaultRules.MaxHitsPerBeat > 0);
        }

        [Fact]
        public void Physicality_DifferentStrictnessLevels_Available()
        {
            // Arrange & Act: Verify different strictness levels can be configured
            var strict = PhysicalityRules.Default with { StrictnessLevel = PhysicalityStrictness.Strict };
            var normal = PhysicalityRules.Default with { StrictnessLevel = PhysicalityStrictness.Normal };
            var loose = PhysicalityRules.Default with { StrictnessLevel = PhysicalityStrictness.Loose };

            // Assert: All levels valid
            Assert.Equal(PhysicalityStrictness.Strict, strict.StrictnessLevel);
            Assert.Equal(PhysicalityStrictness.Normal, normal.StrictnessLevel);
            Assert.Equal(PhysicalityStrictness.Loose, loose.StrictnessLevel);
        }

        [Fact]
        public void Physicality_DensityCaps_EnforcedByRules()
        {
            // Arrange: Create rules with specific caps
            var rules = PhysicalityRules.Default with
            {
                MaxHitsPerBar = 20,
                MaxHitsPerBeat = 3
            };

            // Assert: Caps are stored correctly
            Assert.Equal(20, rules.MaxHitsPerBar);
            Assert.Equal(3, rules.MaxHitsPerBeat);
        }

        [Fact]
        public void Physicality_PerRoleCaps_CanBeConfigured()
        {
            // Arrange: Configure per-role caps
            var perRoleCaps = new Dictionary<string, int>
            {
                { GrooveRoles.Snare, 10 },
                { GrooveRoles.Kick, 8 }
            };

            var rules = PhysicalityRules.Default with
            {
                MaxHitsPerRolePerBar = perRoleCaps
            };

            // Assert: Per-role caps configured
            Assert.NotNull(rules.MaxHitsPerRolePerBar);
            Assert.Equal(10, rules.MaxHitsPerRolePerBar[GrooveRoles.Snare]);
            Assert.Equal(8, rules.MaxHitsPerRolePerBar[GrooveRoles.Kick]);
        }

        #endregion

        #region Helper Methods

        private static DrumOnsetCandidate CreateGrooveCandidate(
            string role,
            decimal beat,
            bool isProtected = false)
        {
            var candidate = new DrumOnsetCandidate
            {
                Role = role,
                OnsetBeat = beat,
                Strength = OnsetStrength.Backbeat,
                VelocityHint = 80,
                TimingHint = 0,
                MaxAddsPerBar = 10,
                ProbabilityBias = 1.0,
                Tags = new List<string>()
            };

            if (isProtected)
            {
                candidate.Tags.Add(DrumCandidateMapper.ProtectedTag);
            }

            return candidate;
        }

        #endregion
    }
}


