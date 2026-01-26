// AI: purpose=Unit tests for Story 10.8.2 AC1: All 28 operators generate valid candidates.
// AI: deps=xUnit, Music.Generator.Agents.Drums operators, DrumOperatorRegistry, DrummerContext.
// AI: change=Story 10.8.2: verify all operators generate valid DrumCandidate instances.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Common;
using Music.Generator.Groove;
using Music;

namespace Music.Generator.Agents.Drums.Tests
{
    /// <summary>
    /// Story 10.8.2 AC1: Tests that all 28 operators generate valid candidates.
    /// Validates candidate structure, field ranges, and deterministic generation.
    /// </summary>
    public class DrummerOperatorTests
    {
        public DrummerOperatorTests()
        {
            Rng.Initialize(5001);
        }

        #region AC1: All 28 Operators Generate Valid Candidates

        [Fact]
        public void Operators_AllOperatorsProduceValidCandidates()
        {
            // Arrange: Get all 28 registered operators
            var registry = DrumOperatorRegistryBuilder.BuildComplete();
            var allOperators = registry.GetAllOperators();
            
            Assert.Equal(28, allOperators.Count);

            // Arrange: Create a permissive context that allows most operators
            var context = CreatePermissiveContext();

            var invalidCandidates = new List<string>();

            // Act: Test each operator
            foreach (var op in allOperators)
            {
                if (!op.CanApply(context))
                    continue;

                var candidates = op.GenerateCandidates(context).ToList();

                // Validate each candidate
                foreach (var candidate in candidates)
                {
                    var validation = ValidateCandidate(candidate);
                    if (!validation.IsValid)
                    {
                        invalidCandidates.Add($"{op.OperatorId}: {validation.ErrorMessage}");
                    }
                }
            }

            // Assert: All candidates are valid
            Assert.Empty(invalidCandidates);
        }

        [Fact]
        public void Operators_ByFamily_MicroAddition_GenerateValidCandidates()
        {
            // Arrange
            var registry = DrumOperatorRegistryBuilder.BuildComplete();
            var operators = registry.GetOperatorsByFamily(OperatorFamily.MicroAddition);
            
            Assert.Equal(7, operators.Count);

            var context = CreatePermissiveContext();

            // Act & Assert
            foreach (var op in operators)
            {
                if (!op.CanApply(context))
                    continue;

                var candidates = op.GenerateCandidates(context).ToList();
                Assert.All(candidates, c => Assert.True(ValidateCandidate(c).IsValid));
            }
        }

        [Fact]
        public void Operators_ByFamily_SubdivisionTransform_GenerateValidCandidates()
        {
            // Arrange
            var registry = DrumOperatorRegistryBuilder.BuildComplete();
            var operators = registry.GetOperatorsByFamily(OperatorFamily.SubdivisionTransform);
            
            Assert.Equal(5, operators.Count);

            var context = CreatePermissiveContext();

            // Act & Assert
            foreach (var op in operators)
            {
                if (!op.CanApply(context))
                    continue;

                var candidates = op.GenerateCandidates(context).ToList();
                Assert.All(candidates, c => Assert.True(ValidateCandidate(c).IsValid));
            }
        }

        [Fact]
        public void Operators_ByFamily_PhrasePunctuation_GenerateValidCandidates()
        {
            // Arrange
            var registry = DrumOperatorRegistryBuilder.BuildComplete();
            var operators = registry.GetOperatorsByFamily(OperatorFamily.PhrasePunctuation);
            
            Assert.Equal(7, operators.Count);

            var context = CreatePermissiveContext();

            // Act & Assert
            foreach (var op in operators)
            {
                if (!op.CanApply(context))
                    continue;

                var candidates = op.GenerateCandidates(context).ToList();
                Assert.All(candidates, c => Assert.True(ValidateCandidate(c).IsValid));
            }
        }

        [Fact]
        public void Operators_ByFamily_PatternSubstitution_GenerateValidCandidates()
        {
            // Arrange
            var registry = DrumOperatorRegistryBuilder.BuildComplete();
            var operators = registry.GetOperatorsByFamily(OperatorFamily.PatternSubstitution);
            
            Assert.Equal(4, operators.Count);

            var context = CreatePermissiveContext();

            // Act & Assert
            foreach (var op in operators)
            {
                if (!op.CanApply(context))
                    continue;

                var candidates = op.GenerateCandidates(context).ToList();
                Assert.All(candidates, c => Assert.True(ValidateCandidate(c).IsValid));
            }
        }

        [Fact]
        public void Operators_ByFamily_StyleIdiom_GenerateValidCandidates()
        {
            // Arrange
            var registry = DrumOperatorRegistryBuilder.BuildComplete();
            var operators = registry.GetOperatorsByFamily(OperatorFamily.StyleIdiom);
            
            Assert.Equal(5, operators.Count);

            var context = CreatePermissiveContext();

            // Act & Assert
            foreach (var op in operators)
            {
                if (!op.CanApply(context))
                    continue;

                var candidates = op.GenerateCandidates(context).ToList();
                Assert.All(candidates, c => Assert.True(ValidateCandidate(c).IsValid));
            }
        }

        [Fact]
        public void Operators_Deterministic_SameContext_SameCandidates()
        {
            // Arrange
            Rng.Initialize(5002);
            var registry = DrumOperatorRegistryBuilder.BuildComplete();
            var op = registry.GetAllOperators().First();
            var context = CreatePermissiveContext();

            // Act: Generate candidates twice with same seed/context
            Rng.Initialize(5002);
            var candidates1 = op.GenerateCandidates(context).ToList();
            
            Rng.Initialize(5002);
            var candidates2 = op.GenerateCandidates(context).ToList();

            // Assert: Identical results
            Assert.Equal(candidates1.Count, candidates2.Count);
            for (int i = 0; i < candidates1.Count; i++)
            {
                Assert.Equal(candidates1[i].CandidateId, candidates2[i].CandidateId);
                Assert.Equal(candidates1[i].BarNumber, candidates2[i].BarNumber);
                Assert.Equal(candidates1[i].Beat, candidates2[i].Beat);
                Assert.Equal(candidates1[i].Role, candidates2[i].Role);
            }
        }

        #endregion

        #region Helper Methods

        private static DrummerContext CreatePermissiveContext()
        {
            return new DrummerContext
            {
                BarNumber = 1,
                Beat = 1.0m,
                BeatsPerBar = 4,
                SectionType = MusicConstants.eSectionType.Chorus,
                PhrasePosition = 0.5,
                BarsUntilSectionEnd = 4,
                EnergyLevel = 0.7,
                TensionLevel = 0.5,
                MotifPresenceScore = 0.4,
                Seed = 5001,
                RngStreamKey = "test",
                ActiveRoles = new HashSet<string>
                {
                    GrooveRoles.Kick,
                    GrooveRoles.Snare,
                    GrooveRoles.ClosedHat,
                    GrooveRoles.OpenHat,
                    GrooveRoles.Crash,
                    GrooveRoles.Ride,
                    GrooveRoles.Tom1,
                    GrooveRoles.Tom2,
                    GrooveRoles.FloorTom
                },
                LastKickBeat = 1.0m,
                LastSnareBeat = 2.0m,
                CurrentHatMode = HatMode.Closed,
                HatSubdivision = HatSubdivision.Eighth,
                IsFillWindow = true,
                IsAtSectionBoundary = false,
                BackbeatBeats = new List<int> { 2, 4 }
            };
        }

        private static (bool IsValid, string ErrorMessage) ValidateCandidate(DrumCandidate candidate)
        {
            // Check required fields are non-null
            if (string.IsNullOrEmpty(candidate.CandidateId))
                return (false, "CandidateId is null or empty");
            
            if (string.IsNullOrEmpty(candidate.OperatorId))
                return (false, "OperatorId is null or empty");
            
            if (string.IsNullOrEmpty(candidate.Role))
                return (false, "Role is null or empty");

            // Check Role is valid
            var validRoles = new HashSet<string>
            {
                GrooveRoles.Kick,
                GrooveRoles.Snare,
                GrooveRoles.ClosedHat,
                GrooveRoles.OpenHat,
                GrooveRoles.Crash,
                GrooveRoles.Ride,
                GrooveRoles.Tom1,
                GrooveRoles.Tom2,
                GrooveRoles.FloorTom,
                GrooveRoles.DrumKit
            };

            if (!validRoles.Contains(candidate.Role))
                return (false, $"Invalid role: {candidate.Role}");

            // Check numeric ranges
            if (candidate.BarNumber < 1)
                return (false, $"BarNumber must be >= 1, got {candidate.BarNumber}");

            if (candidate.Beat < 1.0m)
                return (false, $"Beat must be >= 1.0, got {candidate.Beat}");

            if (candidate.Score < 0.0 || candidate.Score > 1.0)
                return (false, $"Score must be 0.0-1.0, got {candidate.Score}");

            // Check optional hint ranges
            if (candidate.VelocityHint.HasValue)
            {
                if (candidate.VelocityHint.Value < 0 || candidate.VelocityHint.Value > 127)
                    return (false, $"VelocityHint must be 0-127, got {candidate.VelocityHint.Value}");
            }

            return (true, string.Empty);
        }

        #endregion
    }
}
