// AI: purpose=Unit tests for Story A2 deterministic RNG stream policy.
// AI: deps=xunit for test framework; Music.Generator for types under test.
// AI: change=Story A2 acceptance criteria: verify determinism and RNG sequence independence.

using Xunit;

namespace Music.Generator.Tests
{
    /// <summary>
    /// Story A2: Tests for deterministic RNG stream policy.
    /// Verifies same inputs => identical RNG sequences per stream key.
    /// </summary>
    public class GrooveRngStreamPolicyTests
    {
        #region Setup/Teardown

        public GrooveRngStreamPolicyTests()
        {
            // Initialize RNG before each test with known seed
            Rng.Initialize(42);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void RngFor_SameInputs_ProducesIdenticalSequence()
        {
            // Arrange
            int barNumber = 5;
            string role = "Kick";
            var streamKey = GrooveRngStreamKey.CandidatePick;

            // Act - Initialize and draw first sequence
            Rng.Initialize(42);
            var purpose1 = GrooveRngHelper.RngFor(barNumber, role, streamKey);
            var seq1 = new[] {
                Rng.NextInt(purpose1, 0, 100),
                Rng.NextInt(purpose1, 0, 100),
                Rng.NextInt(purpose1, 0, 100)
            };

            // Reset RNG to same seed and draw second sequence
            Rng.Initialize(42);
            var purpose2 = GrooveRngHelper.RngFor(barNumber, role, streamKey);
            var seq2 = new[] {
                Rng.NextInt(purpose2, 0, 100),
                Rng.NextInt(purpose2, 0, 100),
                Rng.NextInt(purpose2, 0, 100)
            };

            // Assert - sequences must be identical when RNG reset to same seed
            Assert.Equal(seq1[0], seq2[0]);
            Assert.Equal(seq1[1], seq2[1]);
            Assert.Equal(seq1[2], seq2[2]);
        }

        [Fact]
        public void RngFor_DifferentStreamKeys_UseDifferentSequences()
        {
            // Arrange
            int barNumber = 7;
            string role = "Hat";

            // Act - different stream keys map to different RandomPurpose values
            var purpose1 = GrooveRngHelper.RngFor(barNumber, role, GrooveRngStreamKey.CandidatePick);
            var seq1 = new[] {
                Rng.NextInt(purpose1, 0, 100),
                Rng.NextInt(purpose1, 0, 100),
                Rng.NextInt(purpose1, 0, 100)
            };

            // Reset to ensure we're comparing from same starting point
            Rng.Initialize(42);
            var purpose2 = GrooveRngHelper.RngFor(barNumber, role, GrooveRngStreamKey.PrunePick);
            var seq2 = new[] {
                Rng.NextInt(purpose2, 0, 100),
                Rng.NextInt(purpose2, 0, 100),
                Rng.NextInt(purpose2, 0, 100)
            };

            // Assert - at least one value should differ (different streams)
            bool anyDifferent = seq1[0] != seq2[0] || seq1[1] != seq2[1] || seq1[2] != seq2[2];
            Assert.True(anyDifferent, "Different stream keys should use different RNG sequences");
        }

        #endregion

        #region Stream Key Coverage Tests

        [Theory]
        [InlineData(GrooveRngStreamKey.VariationGroupPick)]
        [InlineData(GrooveRngStreamKey.CandidatePick)]
        [InlineData(GrooveRngStreamKey.TieBreak)]
        [InlineData(GrooveRngStreamKey.PrunePick)]
        [InlineData(GrooveRngStreamKey.DensityPick)]
        [InlineData(GrooveRngStreamKey.VelocityJitter)]
        [InlineData(GrooveRngStreamKey.GhostVelocityPick)]
        [InlineData(GrooveRngStreamKey.TimingJitter)]
        [InlineData(GrooveRngStreamKey.SwingJitter)]
        [InlineData(GrooveRngStreamKey.FillPick)]
        [InlineData(GrooveRngStreamKey.AccentPick)]
        [InlineData(GrooveRngStreamKey.GhostNotePick)]
        [InlineData(GrooveRngStreamKey.OrnamentPick)]
        [InlineData(GrooveRngStreamKey.CymbalPick)]
        [InlineData(GrooveRngStreamKey.DynamicsPick)]
        public void RngFor_AllStreamKeys_MapToValidPurpose(GrooveRngStreamKey streamKey)
        {
            // Arrange
            int barNumber = 1;
            string role = "Test";

            // Act
            var purpose = GrooveRngHelper.RngFor(barNumber, role, streamKey);
            var value = Rng.NextInt(purpose, 0, 100);

            // Assert - should return valid value in range
            Assert.InRange(value, 0, 99);
        }

        #endregion

        #region Reproducibility Tests

        [Fact]
        public void RngFor_SameMasterSeed_ProducesIdenticalSequences()
        {
            // Arrange
            int barNumber = 3;
            string role = "Kick";
            var streamKey = GrooveRngStreamKey.CandidatePick;

            // Act - generate sequence 1
            Rng.Initialize(12345);
            var purpose1 = GrooveRngHelper.RngFor(barNumber, role, streamKey);
            var seq1 = new[] {
                Rng.NextInt(purpose1, 0, 1000),
                Rng.NextInt(purpose1, 0, 1000),
                Rng.NextInt(purpose1, 0, 1000)
            };

            // Act - generate sequence 2 with same seed
            Rng.Initialize(12345);
            var purpose2 = GrooveRngHelper.RngFor(barNumber, role, streamKey);
            var seq2 = new[] {
                Rng.NextInt(purpose2, 0, 1000),
                Rng.NextInt(purpose2, 0, 1000),
                Rng.NextInt(purpose2, 0, 1000)
            };

            // Assert - sequences must be identical
            Assert.Equal(seq1[0], seq2[0]);
            Assert.Equal(seq1[1], seq2[1]);
            Assert.Equal(seq1[2], seq2[2]);
        }

        [Fact]
        public void RngFor_DifferentMasterSeeds_ProducesDifferentSequences()
        {
            // Arrange
            int barNumber = 5;
            string role = "Snare";
            var streamKey = GrooveRngStreamKey.VelocityJitter;

            // Act - generate with seed 100
            Rng.Initialize(100);
            var purpose1 = GrooveRngHelper.RngFor(barNumber, role, streamKey);
            var seq1 = new[] {
                Rng.NextInt(purpose1, 0, 1000),
                Rng.NextInt(purpose1, 0, 1000),
                Rng.NextInt(purpose1, 0, 1000)
            };

            // Act - generate with seed 200
            Rng.Initialize(200);
            var purpose2 = GrooveRngHelper.RngFor(barNumber, role, streamKey);
            var seq2 = new[] {
                Rng.NextInt(purpose2, 0, 1000),
                Rng.NextInt(purpose2, 0, 1000),
                Rng.NextInt(purpose2, 0, 1000)
            };

            // Assert - at least one value should differ
            bool anyDifferent = seq1[0] != seq2[0] || seq1[1] != seq2[1] || seq1[2] != seq2[2];
            Assert.True(anyDifferent, "Different master seeds should produce different sequences");
        }

        #endregion

        #region Integration Test

        [Fact]
        public void Story_A2_IntegrationTest_FullGrooveScenario()
        {
            // Arrange - simulate groove generation for 8 bars with 3 roles
            Rng.Initialize(7777);
            int totalBars = 8;
            string[] roles = { "Kick", "Snare", "Hat" };
            var results = new Dictionary<string, List<int>>();

            // Act - generate random selections for each bar/role/stream
            foreach (var role in roles)
            {
                var roleResults = new List<int>();
                for (int bar = 1; bar <= totalBars; bar++)
                {
                    // Simulate variation selection
                    var purposeVariation = GrooveRngHelper.RngFor(bar, role, GrooveRngStreamKey.CandidatePick);
                    roleResults.Add(Rng.NextInt(purposeVariation, 0, 10));

                    // Simulate velocity jitter
                    var purposeVelocity = GrooveRngHelper.RngFor(bar, role, GrooveRngStreamKey.VelocityJitter);
                    roleResults.Add(Rng.NextInt(purposeVelocity, -5, 5));

                    // Simulate timing jitter
                    var purposeTiming = GrooveRngHelper.RngFor(bar, role, GrooveRngStreamKey.TimingJitter);
                    roleResults.Add(Rng.NextInt(purposeTiming, -10, 10));
                }
                results[role] = roleResults;
            }

            // Act again - regenerate with same seed
            Rng.Initialize(7777);
            var resultsRepeat = new Dictionary<string, List<int>>();
            foreach (var role in roles)
            {
                var roleResults = new List<int>();
                for (int bar = 1; bar <= totalBars; bar++)
                {
                    var purposeVariation = GrooveRngHelper.RngFor(bar, role, GrooveRngStreamKey.CandidatePick);
                    roleResults.Add(Rng.NextInt(purposeVariation, 0, 10));

                    var purposeVelocity = GrooveRngHelper.RngFor(bar, role, GrooveRngStreamKey.VelocityJitter);
                    roleResults.Add(Rng.NextInt(purposeVelocity, -5, 5));

                    var purposeTiming = GrooveRngHelper.RngFor(bar, role, GrooveRngStreamKey.TimingJitter);
                    roleResults.Add(Rng.NextInt(purposeTiming, -10, 10));
                }
                resultsRepeat[role] = roleResults;
            }

            // Assert - all results must match exactly
            foreach (var role in roles)
            {
                Assert.Equal(results[role].Count, resultsRepeat[role].Count);
                for (int i = 0; i < results[role].Count; i++)
                {
                    Assert.Equal(results[role][i], resultsRepeat[role][i]);
                }
            }
        }

        #endregion
    }
}

