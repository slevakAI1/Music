// AI: purpose=Unit tests for energy constraint rules and policy framework (Story 7.4.1).
// AI: invariants=Tests must be deterministic; verify all rule behaviors and edge cases.
// AI: deps=Tests EnergyConstraintRule implementations and EnergyConstraintPolicy.

using Music.Generator.EnergyConstraints;

namespace Music.Generator
{
    /// <summary>
    /// Tests for Story 7.4.1: Energy constraint model and rules framework.
    /// Validates constraint rule behavior, policy evaluation, and determinism.
    /// </summary>
    public static class EnergyConstraintTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Energy Constraint Tests ===");

            // SameTypeSectionsMonotonicRule tests
            SameTypeSectionsMonotonicRule_FirstSection_NoOpinion();
            SameTypeSectionsMonotonicRule_SecondSectionHigher_Accept();
            SameTypeSectionsMonotonicRule_SecondSectionLower_Adjust();
            SameTypeSectionsMonotonicRule_WithMinIncrement_EnforcesGrowth();

            // PostChorusDropRule tests
            PostChorusDropRule_NotAfterChorus_NoOpinion();
            PostChorusDropRule_ChorusToChorus_Accept();
            PostChorusDropRule_AfterChorus_EnergyTooHigh_Adjust();
            PostChorusDropRule_AfterChorus_EnergyAlreadyLow_Accept();

            // FinalChorusPeakRule tests
            FinalChorusPeakRule_NotChorus_NoOpinion();
            FinalChorusPeakRule_NotLastChorus_NoOpinion();
            FinalChorusPeakRule_LastChorus_EnergyTooLow_Adjust();
            FinalChorusPeakRule_LastChorus_EnergyAtPeak_Accept();

            // BridgeContrastRule tests
            BridgeContrastRule_NotBridge_NoOpinion();
            BridgeContrastRule_NoPreviousChorus_Accept();
            BridgeContrastRule_SufficientContrast_Accept();
            BridgeContrastRule_InsufficientContrast_Adjust();

            // EnergyConstraintPolicy tests
            EnergyConstraintPolicy_EmptyPolicy_NoAdjustment();
            EnergyConstraintPolicy_MultipleRules_BlendsAdjustments();
            EnergyConstraintPolicy_Deterministic();

            Console.WriteLine("All Energy Constraint tests passed.");
        }

        #region SameTypeSectionsMonotonicRule Tests

        private static void SameTypeSectionsMonotonicRule_FirstSection_NoOpinion()
        {
            // Arrange
            var rule = new SameTypeSectionsMonotonicRule();
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Verse,
                sectionIndex: 0,
                proposedEnergy: 0.5,
                previousSameTypeEnergy: null);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (result.HasAdjustment)
                throw new Exception("Expected no adjustment for first section");
        }

        private static void SameTypeSectionsMonotonicRule_SecondSectionHigher_Accept()
        {
            // Arrange
            var rule = new SameTypeSectionsMonotonicRule();
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Verse,
                sectionIndex: 1,
                proposedEnergy: 0.6,
                previousSameTypeEnergy: 0.5);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (result.HasAdjustment)
                throw new Exception("Expected no adjustment when energy already higher");
            if (!result.DiagnosticMessage?.Contains("no adjustment needed") ?? true)
                throw new Exception("Expected diagnostic about no adjustment");
        }

        private static void SameTypeSectionsMonotonicRule_SecondSectionLower_Adjust()
        {
            // Arrange
            var rule = new SameTypeSectionsMonotonicRule();
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Chorus,
                sectionIndex: 1,
                proposedEnergy: 0.6,
                previousSameTypeEnergy: 0.7);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (!result.HasAdjustment)
                throw new Exception("Expected adjustment when energy lower than previous");
            if (Math.Abs(result.AdjustedEnergy!.Value - 0.7) > 0.001)
                throw new Exception($"Expected energy adjusted to 0.7, got {result.AdjustedEnergy}");
            if (!result.DiagnosticMessage?.Contains("Chorus 2") ?? true)
                throw new Exception("Expected section name in diagnostic");
        }

        private static void SameTypeSectionsMonotonicRule_WithMinIncrement_EnforcesGrowth()
        {
            // Arrange
            var rule = new SameTypeSectionsMonotonicRule(strength: 1.0, minIncrement: 0.05);
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Verse,
                sectionIndex: 1,
                proposedEnergy: 0.52,
                previousSameTypeEnergy: 0.50);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (!result.HasAdjustment)
                throw new Exception("Expected adjustment to enforce min increment");
            if (Math.Abs(result.AdjustedEnergy!.Value - 0.55) > 0.001)
                throw new Exception($"Expected energy adjusted to 0.55, got {result.AdjustedEnergy}");
        }

        #endregion

        #region PostChorusDropRule Tests

        private static void PostChorusDropRule_NotAfterChorus_NoOpinion()
        {
            // Arrange
            var rule = new PostChorusDropRule();
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Verse,
                sectionIndex: 1,
                proposedEnergy: 0.6,
                previousSectionType: MusicConstants.eSectionType.Verse);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (result.HasAdjustment)
                throw new Exception("Expected no adjustment when not after chorus");
        }

        private static void PostChorusDropRule_ChorusToChorus_Accept()
        {
            // Arrange
            var rule = new PostChorusDropRule();
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Chorus,
                sectionIndex: 1,
                proposedEnergy: 0.8,
                previousSectionType: MusicConstants.eSectionType.Chorus,
                previousAnySectionEnergy: 0.75);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (result.HasAdjustment)
                throw new Exception("Expected no adjustment for chorus-to-chorus");
            if (!result.DiagnosticMessage?.Contains("also chorus") ?? true)
                throw new Exception("Expected diagnostic about chorus-to-chorus");
        }

        private static void PostChorusDropRule_AfterChorus_EnergyTooHigh_Adjust()
        {
            // Arrange
            var rule = new PostChorusDropRule(maxEnergyAfterChorus: 0.55, typicalDropAmount: 0.20);
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Verse,
                sectionIndex: 1,
                proposedEnergy: 0.70,
                previousSectionType: MusicConstants.eSectionType.Chorus,
                previousAnySectionEnergy: 0.80);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (!result.HasAdjustment)
                throw new Exception("Expected adjustment for high energy after chorus");
            if (result.AdjustedEnergy > 0.55)
                throw new Exception($"Expected energy <= 0.55, got {result.AdjustedEnergy}");
            if (!result.DiagnosticMessage?.Contains("post-chorus drop") ?? true)
                throw new Exception("Expected diagnostic about post-chorus drop");
        }

        private static void PostChorusDropRule_AfterChorus_EnergyAlreadyLow_Accept()
        {
            // Arrange
            var rule = new PostChorusDropRule(maxEnergyAfterChorus: 0.55);
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Verse,
                sectionIndex: 1,
                proposedEnergy: 0.45,
                previousSectionType: MusicConstants.eSectionType.Chorus,
                previousAnySectionEnergy: 0.80);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (result.HasAdjustment)
                throw new Exception("Expected no adjustment when energy already low");
            if (!result.DiagnosticMessage?.Contains("already below max") ?? true)
                throw new Exception("Expected diagnostic about energy already low");
        }

        #endregion

        #region FinalChorusPeakRule Tests

        private static void FinalChorusPeakRule_NotChorus_NoOpinion()
        {
            // Arrange
            var rule = new FinalChorusPeakRule();
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Verse,
                sectionIndex: 0,
                proposedEnergy: 0.5,
                isLastOfType: true);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (result.HasAdjustment)
                throw new Exception("Expected no adjustment for non-chorus section");
        }

        private static void FinalChorusPeakRule_NotLastChorus_NoOpinion()
        {
            // Arrange
            var rule = new FinalChorusPeakRule();
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Chorus,
                sectionIndex: 0,
                proposedEnergy: 0.7,
                isLastOfType: false);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (result.HasAdjustment)
                throw new Exception("Expected no adjustment for non-last chorus");
        }

        private static void FinalChorusPeakRule_LastChorus_EnergyTooLow_Adjust()
        {
            // Arrange
            var rule = new FinalChorusPeakRule(minPeakEnergy: 0.80);
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Chorus,
                sectionIndex: 2,
                proposedEnergy: 0.65,
                isLastOfType: true,
                finalizedEnergies: new Dictionary<int, double> { { 0, 0.5 }, { 1, 0.7 }, { 2, 0.75 } });

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (!result.HasAdjustment)
                throw new Exception("Expected adjustment for low final chorus energy");
            if (result.AdjustedEnergy < 0.80)
                throw new Exception($"Expected energy >= 0.80, got {result.AdjustedEnergy}");
            if (!result.DiagnosticMessage?.Contains("final chorus peak") ?? true)
                throw new Exception("Expected diagnostic about final chorus peak");
        }

        private static void FinalChorusPeakRule_LastChorus_EnergyAtPeak_Accept()
        {
            // Arrange
            var rule = new FinalChorusPeakRule(minPeakEnergy: 0.80);
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Chorus,
                sectionIndex: 2,
                proposedEnergy: 0.85,
                isLastOfType: true,
                finalizedEnergies: new Dictionary<int, double> { { 0, 0.5 }, { 1, 0.7 }, { 2, 0.75 } });

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (result.HasAdjustment)
                throw new Exception("Expected no adjustment when energy already at peak");
            if (!result.DiagnosticMessage?.Contains("final chorus energy") ?? true)
                throw new Exception("Expected diagnostic about final chorus energy");
        }

        #endregion

        #region BridgeContrastRule Tests

        private static void BridgeContrastRule_NotBridge_NoOpinion()
        {
            // Arrange
            var rule = new BridgeContrastRule();
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Verse,
                sectionIndex: 0,
                proposedEnergy: 0.5);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (result.HasAdjustment)
                throw new Exception("Expected no adjustment for non-bridge section");
        }

        private static void BridgeContrastRule_NoPreviousChorus_Accept()
        {
            // Arrange
            var rule = new BridgeContrastRule();
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Bridge,
                sectionIndex: 0,
                proposedEnergy: 0.6,
                previousSectionType: MusicConstants.eSectionType.Verse);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (result.HasAdjustment)
                throw new Exception("Expected no adjustment when no previous chorus");
            if (!result.DiagnosticMessage?.Contains("no previous chorus") ?? true)
                throw new Exception("Expected diagnostic about no previous chorus");
        }

        private static void BridgeContrastRule_SufficientContrast_Accept()
        {
            // Arrange
            var rule = new BridgeContrastRule(minContrastAmount: 0.15);
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Bridge,
                sectionIndex: 0,
                proposedEnergy: 0.85,
                previousSectionType: MusicConstants.eSectionType.Chorus,
                previousAnySectionEnergy: 0.65);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (result.HasAdjustment)
                throw new Exception("Expected no adjustment when contrast sufficient");
            if (!result.DiagnosticMessage?.Contains("climactic") ?? true)
                throw new Exception("Expected diagnostic about climactic contrast");
        }

        private static void BridgeContrastRule_InsufficientContrast_Adjust()
        {
            // Arrange
            var rule = new BridgeContrastRule(minContrastAmount: 0.15);
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Bridge,
                sectionIndex: 0,
                proposedEnergy: 0.68,
                previousSectionType: MusicConstants.eSectionType.Chorus,
                previousAnySectionEnergy: 0.65);

            // Act
            var result = rule.Evaluate(context);

            // Assert
            if (!result.HasAdjustment)
                throw new Exception("Expected adjustment for insufficient contrast");
            double energyDiff = Math.Abs(result.AdjustedEnergy!.Value - 0.65);
            if (energyDiff < 0.15)
                throw new Exception($"Expected energy diff >= 0.15, got {energyDiff}");
        }

        #endregion

        #region EnergyConstraintPolicy Tests

        private static void EnergyConstraintPolicy_EmptyPolicy_NoAdjustment()
        {
            // Arrange
            var policy = EnergyConstraintPolicy.Empty();
            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Verse,
                sectionIndex: 0,
                proposedEnergy: 0.5);

            // Act
            var (adjustedEnergy, diagnostics) = policy.Apply(context);

            // Assert
            if (Math.Abs(adjustedEnergy - 0.5) > 0.001)
                throw new Exception($"Expected no adjustment, got {adjustedEnergy}");
            if (diagnostics.Count > 0)
                throw new Exception("Expected no diagnostics for empty policy");
        }

        private static void EnergyConstraintPolicy_MultipleRules_BlendsAdjustments()
        {
            // Arrange
            var rules = new List<EnergyConstraintRule>
            {
                new SameTypeSectionsMonotonicRule(strength: 1.0),
                new FinalChorusPeakRule(strength: 1.5, minPeakEnergy: 0.85)
            };

            var policy = new EnergyConstraintPolicy
            {
                PolicyName = "Test",
                Rules = rules,
                IsEnabled = true
            };

            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Chorus,
                sectionIndex: 1,
                proposedEnergy: 0.65,
                previousSameTypeEnergy: 0.70,
                isLastOfType: true,
                finalizedEnergies: new Dictionary<int, double> { { 0, 0.70 } });

            // Act
            var (adjustedEnergy, diagnostics) = policy.Apply(context);

            // Assert
            // Should blend: (0.70 * 1.0 + 0.85 * 1.5) / (1.0 + 1.5) = 1.975 / 2.5 = 0.79
            if (adjustedEnergy <= 0.70)
                throw new Exception($"Expected energy > 0.70, got {adjustedEnergy}");
            if (adjustedEnergy >= 0.85)
                throw new Exception($"Expected energy < 0.85, got {adjustedEnergy}");
            if (Math.Abs(adjustedEnergy - 0.79) > 0.02)
                throw new Exception($"Expected energy ? 0.79, got {adjustedEnergy}");
            if (diagnostics.Count == 0)
                throw new Exception("Expected diagnostics from rules");
        }

        private static void EnergyConstraintPolicy_Deterministic()
        {
            // Arrange
            var policy = new EnergyConstraintPolicy
            {
                PolicyName = "Test",
                Rules = new List<EnergyConstraintRule>
                {
                    new SameTypeSectionsMonotonicRule(),
                    new PostChorusDropRule()
                },
                IsEnabled = true
            };

            var context = CreateContext(
                sectionType: MusicConstants.eSectionType.Verse,
                sectionIndex: 1,
                proposedEnergy: 0.7,
                previousSameTypeEnergy: 0.45,
                previousSectionType: MusicConstants.eSectionType.Chorus,
                previousAnySectionEnergy: 0.80);

            // Act
            var (energy1, _) = policy.Apply(context);
            var (energy2, _) = policy.Apply(context);

            // Assert
            if (Math.Abs(energy1 - energy2) > 0.0001)
                throw new Exception($"Policy not deterministic: {energy1} != {energy2}");
        }

        #endregion

        #region Helper Methods

        private static EnergyConstraintContext CreateContext(
            MusicConstants.eSectionType sectionType,
            int sectionIndex,
            double proposedEnergy,
            double? previousSameTypeEnergy = null,
            double? previousAnySectionEnergy = null,
            MusicConstants.eSectionType? previousSectionType = null,
            double? nextSectionEnergy = null,
            bool isLastOfType = false,
            bool isLastSection = false,
            Dictionary<int, double>? finalizedEnergies = null,
            int totalSectionsOfType = 3,
            int totalSections = 8,
            int absoluteSectionIndex = 0)
        {
            return new EnergyConstraintContext
            {
                SectionType = sectionType,
                SectionIndex = sectionIndex,
                AbsoluteSectionIndex = absoluteSectionIndex,
                ProposedEnergy = proposedEnergy,
                PreviousSameTypeEnergy = previousSameTypeEnergy,
                PreviousAnySectionEnergy = previousAnySectionEnergy,
                PreviousSectionType = previousSectionType,
                NextSectionEnergy = nextSectionEnergy,
                IsLastOfType = isLastOfType,
                IsLastSection = isLastSection,
                FinalizedEnergies = finalizedEnergies ?? new Dictionary<int, double>(),
                TotalSectionsOfType = totalSectionsOfType,
                TotalSections = totalSections
            };
        }

        #endregion
    }
}
