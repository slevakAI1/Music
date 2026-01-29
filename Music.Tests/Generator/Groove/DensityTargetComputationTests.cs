////// AI: purpose=Unit tests for Story C1 density target computation (DrumDensityCalculator).
////// AI: deps=xunit for test framework; Music.Generator for types under test.
////// AI: change=Story C1, 4.2: test rounding, clamping, multipliers, overrides; updated to use Drum types.

////using Music.Generator.Agents.Drums;
////using Music.Generator.Groove;
////using Xunit;

////namespace Music.Generator.Tests
////{
////    /// <summary>
////    /// Story C1: Tests for density target computation.
////    /// Verifies rounding, clamping, multipliers, and policy overrides.
////    /// </summary>
////    public class DensityTargetComputationTests
////    {
////        #region Basic Rounding Tests

////        [Fact]
////        public void ComputeDensityTarget_BasicRounding_CorrectResult()
////        {
////            // Arrange - density=0.5, maxEvents=5 => 2.5 rounds to 3
////            var barContext = CreateBarContext(density: 0.5, maxEvents: 5);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert
////            Assert.Equal(3, result.TargetCount);
////            Assert.Equal(0.5, result.Density01Used);
////            Assert.Equal(5, result.MaxEventsPerBarUsed);
////        }

////        [Fact]
////        public void ComputeDensityTarget_RoundingTieAwayFromZero_RoundsUp()
////        {
////            // Arrange - density=0.25, maxEvents=2 => 0.5 rounds away from zero to 1
////            var barContext = CreateBarContext(density: 0.25, maxEvents: 2);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert
////            Assert.Equal(1, result.TargetCount);
////        }

////        [Fact]
////        public void ComputeDensityTarget_RoundingDown_CorrectResult()
////        {
////            // Arrange - density=0.3, maxEvents=10 => 3.0 (exact, no rounding)
////            var barContext = CreateBarContext(density: 0.3, maxEvents: 10);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert
////            Assert.Equal(3, result.TargetCount);
////        }

////        [Fact]
////        public void ComputeDensityTarget_RoundingUp_CorrectResult()
////        {
////            // Arrange - density=0.7, maxEvents=10 => 7.0 (exact)
////            var barContext = CreateBarContext(density: 0.7, maxEvents: 10);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert
////            Assert.Equal(7, result.TargetCount);
////        }

////        #endregion

////        #region Multiplier Impact Tests

////        [Fact]
////        public void ComputeDensityTarget_WithMultiplier_AppliesCorrectly()
////        {
////            // Arrange - density=0.5, maxEvents=4, multiplier=1.5 => densityAfter=0.75 => target=3
////            var barContext = CreateBarContextWithSection(density: 0.5, maxEvents: 4, sectionType: "Chorus");
////            var orchestrationPolicy = CreateOrchestrationPolicy("Chorus", "Kick", multiplier: 1.5);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick", orchestrationPolicy: orchestrationPolicy);

////            // Assert
////            Assert.Equal(3, result.TargetCount);
////            Assert.Equal(0.75, result.Density01Used);
////        }

////        [Fact]
////        public void ComputeDensityTarget_MultiplierGreaterThanOne_ClampsTo1()
////        {
////            // Arrange - density=0.8, multiplier=2.0 => densityAfter would be 1.6, clamped to 1.0
////            var barContext = CreateBarContextWithSection(density: 0.8, maxEvents: 10, sectionType: "Chorus");
////            var orchestrationPolicy = CreateOrchestrationPolicy("Chorus", "Kick", multiplier: 2.0);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick", orchestrationPolicy: orchestrationPolicy);

////            // Assert
////            Assert.Equal(10, result.TargetCount); // 1.0 * 10 = 10
////            Assert.Equal(1.0, result.Density01Used);
////        }

////        [Fact]
////        public void ComputeDensityTarget_NegativeMultiplier_TreatedAsZero()
////        {
////            // Arrange - negative multiplier clamped to 0
////            var barContext = CreateBarContextWithSection(density: 0.5, maxEvents: 10, sectionType: "Verse");
////            var orchestrationPolicy = CreateOrchestrationPolicy("Verse", "Kick", multiplier: -0.5);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick", orchestrationPolicy: orchestrationPolicy);

////            // Assert
////            Assert.Equal(0, result.TargetCount);
////            Assert.Equal(0.0, result.Density01Used);
////        }

////        [Fact]
////        public void ComputeDensityTarget_NoMatchingSection_UsesMultiplier1()
////        {
////            // Arrange - orchestration has Chorus, but bar is in Verse
////            var barContext = CreateBarContextWithSection(density: 0.5, maxEvents: 10, sectionType: "Verse");
////            var orchestrationPolicy = CreateOrchestrationPolicy("Chorus", "Kick", multiplier: 2.0);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick", orchestrationPolicy: orchestrationPolicy);

////            // Assert - No multiplier applied, so density=0.5, target=5
////            Assert.Equal(5, result.TargetCount);
////            Assert.Equal(0.5, result.Density01Used);
////        }

////        #endregion

////        #region Policy Override Tests

////        [Fact]
////        public void ComputeDensityTarget_DensityOverride_TakesPrecedence()
////        {
////            // Arrange - base density 0.2, multiplier 2.0 => would be 0.4, but override 0.9
////            var barContext = CreateBarContextWithSection(density: 0.2, maxEvents: 10, sectionType: "Chorus");
////            var orchestrationPolicy = CreateOrchestrationPolicy("Chorus", "Kick", multiplier: 2.0);
////            var policyDecision = new DrumPolicyDecision { Density01Override = 0.9 };

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick", orchestrationPolicy: orchestrationPolicy, policyDecision: policyDecision);

////            // Assert - Override used: 0.9 * 10 = 9
////            Assert.Equal(9, result.TargetCount);
////            Assert.Equal(0.9, result.Density01Used);
////        }

////        [Fact]
////        public void ComputeDensityTarget_MaxEventsOverride_TakesPrecedence()
////        {
////            // Arrange - base max 10, override 3
////            var barContext = CreateBarContext(density: 0.5, maxEvents: 10);
////            var policyDecision = new DrumPolicyDecision { MaxEventsPerBarOverride = 3 };

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick", policyDecision: policyDecision);

////            // Assert - 0.5 * 3 = 1.5 rounds to 2
////            Assert.Equal(2, result.TargetCount);
////            Assert.Equal(3, result.MaxEventsPerBarUsed);
////        }

////        [Fact]
////        public void ComputeDensityTarget_BothOverrides_BothApplied()
////        {
////            // Arrange
////            var barContext = CreateBarContext(density: 0.5, maxEvents: 10);
////            var policyDecision = new DrumPolicyDecision
////            {
////                Density01Override = 0.8,
////                MaxEventsPerBarOverride = 5
////            };

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick", policyDecision: policyDecision);

////            // Assert - 0.8 * 5 = 4
////            Assert.Equal(4, result.TargetCount);
////            Assert.Equal(0.8, result.Density01Used);
////            Assert.Equal(5, result.MaxEventsPerBarUsed);
////        }

////        #endregion

////        #region Clamping Tests

////        [Fact]
////        public void ComputeDensityTarget_DensityGreaterThan1_ClampsTo1()
////        {
////            // Arrange - density > 1.0
////            var barContext = CreateBarContext(density: 1.5, maxEvents: 10);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert - Clamped to 1.0: 1.0 * 10 = 10
////            Assert.Equal(10, result.TargetCount);
////            Assert.Equal(1.0, result.Density01Used);
////        }

////        [Fact]
////        public void ComputeDensityTarget_DensityLessThan0_ClampsTo0()
////        {
////            // Arrange - negative density
////            var barContext = CreateBarContext(density: -0.5, maxEvents: 10);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert
////            Assert.Equal(0, result.TargetCount);
////            Assert.Equal(0.0, result.Density01Used);
////        }

////        [Fact]
////        public void ComputeDensityTarget_NegativeMaxEvents_ClampsTo0()
////        {
////            // Arrange - negative maxEvents
////            var barContext = CreateBarContext(density: 0.5, maxEvents: -5);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert
////            Assert.Equal(0, result.TargetCount);
////            Assert.Equal(0, result.MaxEventsPerBarUsed);
////        }

////        [Fact]
////        public void ComputeDensityTarget_TargetExceedsMax_ClampsToMax()
////        {
////            // Arrange - This shouldn't normally happen, but test the clamp
////            var barContext = CreateBarContext(density: 1.0, maxEvents: 5);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert - 1.0 * 5 = 5, clamped to 5
////            Assert.Equal(5, result.TargetCount);
////            Assert.True(result.TargetCount <= result.MaxEventsPerBarUsed);
////        }

////        #endregion

////        #region Fallback Tests

////        [Fact]
////        public void ComputeDensityTarget_MissingRoleDensityTarget_UsesFallback()
////        {
////            // Arrange - No RoleDensityTarget, use roleConstraintPolicy fallback
////            var barContext = new DrumBarContext(
////                BarNumber: 1,
////                Section: null,
////                SegmentProfile: null,
////                BarWithinSection: 1,
////                BarsUntilSectionEnd: 4);

////            var roleConstraintPolicy = new GrooveRoleConstraintPolicy
////            {
////                RoleMaxDensityPerBar = new Dictionary<string, int>
////                {
////                    ["Kick"] = 8
////                }
////            };

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick", roleConstraintPolicy: roleConstraintPolicy);

////            // Assert - density=0.0 (fallback), maxEvents=8 (fallback) => target=0
////            Assert.Equal(0, result.TargetCount);
////            Assert.Equal(0.0, result.Density01Used);
////            Assert.Equal(8, result.MaxEventsPerBarUsed);
////        }

////        [Fact]
////        public void ComputeDensityTarget_NoInputsAvailable_ReturnsZero()
////        {
////            // Arrange - No segment profile, no constraint policy
////            var barContext = new DrumBarContext(
////                BarNumber: 1,
////                Section: null,
////                SegmentProfile: null,
////                BarWithinSection: 1,
////                BarsUntilSectionEnd: 4);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert
////            Assert.Equal(0, result.TargetCount);
////            Assert.Equal(0.0, result.Density01Used);
////            Assert.Equal(0, result.MaxEventsPerBarUsed);
////        }

////        [Fact]
////        public void ComputeDensityTarget_FallbackToVocabulary_Works()
////        {
////            // Arrange - No RoleMaxDensityPerBar, but has RoleVocabulary
////            var barContext = new DrumBarContext(
////                BarNumber: 1,
////                Section: null,
////                SegmentProfile: null,
////                BarWithinSection: 1,
////                BarsUntilSectionEnd: 4);

////            var roleConstraintPolicy = new GrooveRoleConstraintPolicy
////            {
////                RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
////                {
////                    ["Kick"] = new RoleRhythmVocabulary { MaxHitsPerBar = 12 }
////                }
////            };

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick", roleConstraintPolicy: roleConstraintPolicy);

////            // Assert
////            Assert.Equal(0, result.TargetCount); // density=0
////            Assert.Equal(12, result.MaxEventsPerBarUsed);
////        }

////        #endregion

////        #region Determinism Tests

////        [Fact]
////        public void ComputeDensityTarget_SameInputs_SameOutput()
////        {
////            // Arrange
////            var barContext = CreateBarContext(density: 0.6, maxEvents: 7);

////            // Act - Run multiple times
////            var result1 = DrumDensityCalculator.ComputeDensityTarget(barContext, "Kick");
////            var result2 = DrumDensityCalculator.ComputeDensityTarget(barContext, "Kick");
////            var result3 = DrumDensityCalculator.ComputeDensityTarget(barContext, "Kick");

////            // Assert - All identical
////            Assert.Equal(result1.TargetCount, result2.TargetCount);
////            Assert.Equal(result2.TargetCount, result3.TargetCount);
////            Assert.Equal(result1.Density01Used, result2.Density01Used);
////            Assert.Equal(result1.MaxEventsPerBarUsed, result2.MaxEventsPerBarUsed);
////        }

////        #endregion

////        #region Edge Cases

////        [Fact]
////        public void ComputeDensityTarget_ZeroDensity_ReturnsZero()
////        {
////            // Arrange
////            var barContext = CreateBarContext(density: 0.0, maxEvents: 10);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert
////            Assert.Equal(0, result.TargetCount);
////        }

////        [Fact]
////        public void ComputeDensityTarget_ZeroMaxEvents_ReturnsZero()
////        {
////            // Arrange
////            var barContext = CreateBarContext(density: 0.5, maxEvents: 0);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert
////            Assert.Equal(0, result.TargetCount);
////        }

////        [Fact]
////        public void ComputeDensityTarget_NullBarContext_ThrowsArgumentNullException()
////        {
////            // Act & Assert
////            Assert.Throws<ArgumentNullException>(() =>
////                DrumDensityCalculator.ComputeDensityTarget(null!, "Kick"));
////        }

////        [Fact]
////        public void ComputeDensityTarget_NullRole_ThrowsArgumentNullException()
////        {
////            // Arrange
////            var barContext = CreateBarContext(density: 0.5, maxEvents: 10);

////            // Act & Assert
////            Assert.Throws<ArgumentNullException>(() =>
////                DrumDensityCalculator.ComputeDensityTarget(barContext, null!));
////        }

////        [Fact]
////        public void ComputeDensityTarget_EmptyRole_ThrowsArgumentException()
////        {
////            // Arrange
////            var barContext = CreateBarContext(density: 0.5, maxEvents: 10);

////            // Act & Assert
////            Assert.Throws<ArgumentException>(() =>
////                DrumDensityCalculator.ComputeDensityTarget(barContext, ""));
////        }

////        [Fact]
////        public void ComputeDensityTarget_ExplanationIncludesProvenance()
////        {
////            // Arrange
////            var barContext = CreateBarContext(density: 0.5, maxEvents: 10);

////            // Act
////            var result = DrumDensityCalculator.ComputeDensityTarget(
////                barContext, "Kick");

////            // Assert - Explanation should contain key information
////            Assert.NotEmpty(result.Explanation);
////            Assert.Contains("densityBase", result.Explanation);
////            Assert.Contains("maxEventsBase", result.Explanation);
////            Assert.Contains("target", result.Explanation);
////        }

////        #endregion

////        #region Test Helpers

////        private static DrumBarContext CreateBarContext(double density, int maxEvents)
////        {
////            var segmentProfile = new object
////            {
////                DensityTargets = new List<RoleDensityTarget>
////                {
////                    new RoleDensityTarget
////                    {
////                        Role = "Kick",
////                        Density01 = density,
////                        MaxEventsPerBar = maxEvents
////                    }
////                }
////            };

////            return new DrumBarContext(
////                BarNumber: 1,
////                Section: null,
////                SegmentProfile: segmentProfile,
////                BarWithinSection: 1,
////                BarsUntilSectionEnd: 4);
////        }

////        private static DrumBarContext CreateBarContextWithSection(double density, int maxEvents, string sectionType)
////        {
////            var segmentProfile = new object
////            {
////                DensityTargets = new List<RoleDensityTarget>
////                {
////                    new RoleDensityTarget
////                    {
////                        Role = "Kick",
////                        Density01 = density,
////                        MaxEventsPerBar = maxEvents
////                    }
////                }
////            };

////            // Parse section type string to enum
////            var sectionTypeEnum = Enum.TryParse<MusicConstants.eSectionType>(sectionType, ignoreCase: true, out var parsed)
////                ? parsed
////                : MusicConstants.eSectionType.Custom;

////            var section = new Section
////            {
////                SectionType = sectionTypeEnum
////            };

////            return new DrumBarContext(
////                BarNumber: 1,
////                Section: section,
////                SegmentProfile: segmentProfile,
////                BarWithinSection: 1,
////                BarsUntilSectionEnd: 4);
////        }

////        private static GrooveOrchestrationPolicy CreateOrchestrationPolicy(string sectionType, string role, double multiplier)
////        {
////            return new GrooveOrchestrationPolicy
////            {
////                DefaultsBySectionType = new List<SectionRolePresenceDefaults>
////                {
////                    new SectionRolePresenceDefaults
////                    {
////                        SectionType = sectionType,
////                        RoleDensityMultiplier = new Dictionary<string, double>
////                        {
////                            [role] = multiplier
////                        }
////                    }
////                }
////            };
////        }

////        #endregion
////    }
////}


