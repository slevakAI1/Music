// AI: purpose=Unit tests for Story 6.1 DrummerVelocityShaper.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums.Performance for types under test.
// AI: change=Story 6.1 acceptance criteria: determinism, style targets, minimal adjustment, fill ramps.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Performance;
using Music.Generator.Agents.Common;
using Music.Generator.Groove;

namespace Music.Tests.Generator.Agents.Drums.Performance
{
    /// <summary>
    /// Story 6.1: Tests for DrummerVelocityShaper.
    /// Verifies velocity hint application, determinism, and style-aware behavior.
    /// </summary>
    public class DrummerVelocityShaperTests
    {
        #region ApplyHints Basic Tests

        [Fact]
        public void ApplyHints_NullCandidates_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DrummerVelocityShaper.ApplyHints(null!, StyleConfigurationLibrary.PopRock));
        }

        [Fact]
        public void ApplyHints_EmptyCandidates_ReturnsEmpty()
        {
            var result = DrummerVelocityShaper.ApplyHints(
                Array.Empty<DrumCandidate>(),
                StyleConfigurationLibrary.PopRock);

            Assert.Empty(result);
        }

        [Fact]
        public void ApplyHints_NullStyleConfig_UsesConservativeDefaults()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Ghost);
            var candidates = new[] { candidate };

            var result = DrummerVelocityShaper.ApplyHints(candidates, null);

            Assert.Single(result);
            Assert.True(result[0].VelocityHint.HasValue);
            // Conservative ghost default is 35
            Assert.InRange(result[0].VelocityHint!.Value, 30, 40);
        }

        [Fact]
        public void ApplyHints_SingleCandidate_ReturnsHintedCandidate()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Backbeat);
            var candidates = new[] { candidate };

            var result = DrummerVelocityShaper.ApplyHints(
                candidates,
                StyleConfigurationLibrary.PopRock);

            Assert.Single(result);
            Assert.True(result[0].VelocityHint.HasValue);
        }

        #endregion

        #region Dynamic Intent Classification Tests

        [Fact]
        public void ClassifyDynamicIntent_GhostStrength_ReturnsLow()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Ghost);

            var intent = DrummerVelocityShaper.ClassifyDynamicIntent(candidate);

            Assert.Equal(DynamicIntent.Low, intent);
        }

        [Fact]
        public void ClassifyDynamicIntent_BackbeatStrength_ReturnsStrongAccent()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Backbeat);

            var intent = DrummerVelocityShaper.ClassifyDynamicIntent(candidate);

            Assert.Equal(DynamicIntent.StrongAccent, intent);
        }

        [Fact]
        public void ClassifyDynamicIntent_DownbeatStrength_ReturnsStrongAccent()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Downbeat);

            var intent = DrummerVelocityShaper.ClassifyDynamicIntent(candidate);

            Assert.Equal(DynamicIntent.StrongAccent, intent);
        }

        [Fact]
        public void ClassifyDynamicIntent_OffbeatStrength_ReturnsMediumLow()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Offbeat);

            var intent = DrummerVelocityShaper.ClassifyDynamicIntent(candidate);

            Assert.Equal(DynamicIntent.MediumLow, intent);
        }

        [Fact]
        public void ClassifyDynamicIntent_StrongStrength_ReturnsMedium()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Strong);

            var intent = DrummerVelocityShaper.ClassifyDynamicIntent(candidate);

            Assert.Equal(DynamicIntent.Medium, intent);
        }

        [Fact]
        public void ClassifyDynamicIntent_CrashRole_ReturnsPeakAccent()
        {
            var candidate = CreateCandidateWithRole(GrooveRoles.Crash, OnsetStrength.Strong);

            var intent = DrummerVelocityShaper.ClassifyDynamicIntent(candidate);

            Assert.Equal(DynamicIntent.PeakAccent, intent);
        }

        [Theory]
        [InlineData(FillRole.FillStart, DynamicIntent.FillRampStart)]
        [InlineData(FillRole.FillBody, DynamicIntent.FillRampBody)]
        [InlineData(FillRole.FillEnd, DynamicIntent.FillRampEnd)]
        [InlineData(FillRole.Setup, DynamicIntent.StrongAccent)]
        public void ClassifyDynamicIntent_FillRoles_ReturnsExpectedIntent(FillRole fillRole, DynamicIntent expectedIntent)
        {
            var candidate = CreateCandidateWithFillRole(fillRole);

            var intent = DrummerVelocityShaper.ClassifyDynamicIntent(candidate);

            Assert.Equal(expectedIntent, intent);
        }

        #endregion

        #region Style Target Tests

        [Fact]
        public void ApplyHints_PopRockGhost_UsesPopRockGhostTarget()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Ghost);
            var candidates = new[] { candidate };

            var result = DrummerVelocityShaper.ApplyHints(
                candidates,
                StyleConfigurationLibrary.PopRock,
                energyLevel: 0.5); // Neutral energy

            Assert.Single(result);
            // PopRock ghost target is 35
            Assert.InRange(result[0].VelocityHint!.Value, 33, 37);
        }

        [Fact]
        public void ApplyHints_JazzGhost_UsesJazzGhostTarget()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Ghost);
            var candidates = new[] { candidate };

            var result = DrummerVelocityShaper.ApplyHints(
                candidates,
                StyleConfigurationLibrary.Jazz,
                energyLevel: 0.5);

            Assert.Single(result);
            // Jazz ghost target is 30
            Assert.InRange(result[0].VelocityHint!.Value, 28, 32);
        }

        [Fact]
        public void ApplyHints_MetalBackbeat_UsesMetalBackbeatTarget()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Backbeat);
            var candidates = new[] { candidate };

            var result = DrummerVelocityShaper.ApplyHints(
                candidates,
                StyleConfigurationLibrary.Metal,
                energyLevel: 0.5);

            Assert.Single(result);
            // Metal backbeat target is 115
            Assert.InRange(result[0].VelocityHint!.Value, 110, 120);
        }

        #endregion

        #region Minimal Adjustment Tests

        [Fact]
        public void ApplyHints_ExistingHint_AdjustsMinimally()
        {
            // Create candidate with existing hint far from target
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Backbeat)
                with { VelocityHint = 80 };
            var candidates = new[] { candidate };

            // PopRock backbeat target is ~105
            var result = DrummerVelocityShaper.ApplyHints(
                candidates,
                StyleConfigurationLibrary.PopRock,
                energyLevel: 0.5);

            Assert.Single(result);
            // Should adjust by at most MaxAdjustmentDelta (10)
            Assert.InRange(result[0].VelocityHint!.Value, 88, 92);
        }

        [Fact]
        public void ApplyHints_ExistingHintCloseToTarget_MovesToTarget()
        {
            // Create candidate with hint close to target
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Ghost)
                with { VelocityHint = 38 };
            var candidates = new[] { candidate };

            // PopRock ghost target is 35
            var result = DrummerVelocityShaper.ApplyHints(
                candidates,
                StyleConfigurationLibrary.PopRock,
                energyLevel: 0.5);

            Assert.Single(result);
            // Within MaxAdjustmentDelta, should move to target
            Assert.InRange(result[0].VelocityHint!.Value, 33, 37);
        }

        [Fact]
        public void ApplyHints_ExistingHintAtTarget_NoChange()
        {
            var settings = DrummerVelocityHintSettings.ConservativeDefaults;
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Ghost)
                with { VelocityHint = settings.GhostVelocityTarget };

            var (hintedCandidate, diagnostics) = DrummerVelocityShaper.ApplyHintWithDiagnostics(
                candidate,
                settings,
                energyLevel: 0.5);

            // Should be at or very close to target
            Assert.InRange(hintedCandidate.VelocityHint!.Value, 
                settings.GhostVelocityTarget - 2, 
                settings.GhostVelocityTarget + 2);
        }

        #endregion

        #region Energy Level Tests

        [Fact]
        public void ApplyHints_HighEnergy_IncreasesVelocity()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Backbeat);
            var candidates = new[] { candidate };

            var lowEnergyResult = DrummerVelocityShaper.ApplyHints(
                candidates,
                StyleConfigurationLibrary.PopRock,
                energyLevel: 0.0);

            var highEnergyResult = DrummerVelocityShaper.ApplyHints(
                candidates,
                StyleConfigurationLibrary.PopRock,
                energyLevel: 1.0);

            Assert.True(highEnergyResult[0].VelocityHint > lowEnergyResult[0].VelocityHint);
        }

        [Fact]
        public void ApplyHints_NeutralEnergy_NoSignificantChange()
        {
            var settings = DrummerVelocityHintSettings.PopRockDefaults;
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Backbeat);

            var (hintedCandidate, _) = DrummerVelocityShaper.ApplyHintWithDiagnostics(
                candidate,
                settings,
                energyLevel: 0.5);

            // At neutral energy, velocity should be close to base target
            Assert.InRange(hintedCandidate.VelocityHint!.Value,
                settings.BackbeatVelocityTarget - 5,
                settings.BackbeatVelocityTarget + 5);
        }

        #endregion

        #region Fill Ramp Tests

        [Fact]
        public void ApplyHints_AscendingFillRamp_VelocityIncreases()
        {
            var settings = new DrummerVelocityHintSettings
            {
                FillVelocityMin = 60,
                FillVelocityMax = 100,
                FillRampDirection = FillRampDirection.Ascending
            };

            var startCandidate = CreateCandidateWithFillRole(FillRole.FillStart);
            var endCandidate = CreateCandidateWithFillRole(FillRole.FillEnd);

            var (startHinted, _) = DrummerVelocityShaper.ApplyHintWithDiagnostics(startCandidate, settings, 0.5);
            var (endHinted, _) = DrummerVelocityShaper.ApplyHintWithDiagnostics(endCandidate, settings, 0.5);

            Assert.True(endHinted.VelocityHint > startHinted.VelocityHint,
                $"Ascending fill should increase: start={startHinted.VelocityHint}, end={endHinted.VelocityHint}");
        }

        [Fact]
        public void ApplyHints_DescendingFillRamp_VelocityDecreases()
        {
            var settings = new DrummerVelocityHintSettings
            {
                FillVelocityMin = 60,
                FillVelocityMax = 100,
                FillRampDirection = FillRampDirection.Descending
            };

            var startCandidate = CreateCandidateWithFillRole(FillRole.FillStart);
            var endCandidate = CreateCandidateWithFillRole(FillRole.FillEnd);

            var (startHinted, _) = DrummerVelocityShaper.ApplyHintWithDiagnostics(startCandidate, settings, 0.5);
            var (endHinted, _) = DrummerVelocityShaper.ApplyHintWithDiagnostics(endCandidate, settings, 0.5);

            Assert.True(startHinted.VelocityHint > endHinted.VelocityHint,
                $"Descending fill should decrease: start={startHinted.VelocityHint}, end={endHinted.VelocityHint}");
        }

        [Fact]
        public void ApplyHints_FlatFillRamp_VelocityConstant()
        {
            var settings = new DrummerVelocityHintSettings
            {
                FillVelocityMin = 60,
                FillVelocityMax = 100,
                FillRampDirection = FillRampDirection.Flat
            };

            var startCandidate = CreateCandidateWithFillRole(FillRole.FillStart);
            var bodyCandidate = CreateCandidateWithFillRole(FillRole.FillBody);
            var endCandidate = CreateCandidateWithFillRole(FillRole.FillEnd);

            var (startHinted, _) = DrummerVelocityShaper.ApplyHintWithDiagnostics(startCandidate, settings, 0.5);
            var (bodyHinted, _) = DrummerVelocityShaper.ApplyHintWithDiagnostics(bodyCandidate, settings, 0.5);
            var (endHinted, _) = DrummerVelocityShaper.ApplyHintWithDiagnostics(endCandidate, settings, 0.5);

            // All should be at midpoint (80) ± energy adjustment
            Assert.InRange(startHinted.VelocityHint!.Value, 75, 85);
            Assert.InRange(bodyHinted.VelocityHint!.Value, 75, 85);
            Assert.InRange(endHinted.VelocityHint!.Value, 75, 85);
        }

        #endregion

        #region Clamping Tests

        [Fact]
        public void ApplyHints_ExtremeHighValue_ClampsTo127()
        {
            var settings = new DrummerVelocityHintSettings
            {
                CrashVelocityTarget = 200 // Invalid, but should be clamped
            };

            var candidate = CreateCandidateWithRole(GrooveRoles.Crash, OnsetStrength.Strong);

            var (hinted, _) = DrummerVelocityShaper.ApplyHintWithDiagnostics(candidate, settings, 1.0);

            Assert.InRange(hinted.VelocityHint!.Value, 1, 127);
        }

        [Fact]
        public void ApplyHints_ExistingHintOutOfRange_ClampsToValid()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Ghost)
                with { VelocityHint = 150 }; // Invalid

            var hinted = DrummerVelocityShaper.ApplyHintToCandidate(
                candidate,
                DrummerVelocityHintSettings.ConservativeDefaults);

            Assert.InRange(hinted.VelocityHint!.Value, 1, 127);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void ApplyHints_SameInputs_ProducesSameOutputs()
        {
            var candidates = new[]
            {
                DrumCandidate.CreateMinimal(strength: OnsetStrength.Ghost),
                DrumCandidate.CreateMinimal(strength: OnsetStrength.Backbeat, beat: 2.0m),
                DrumCandidate.CreateMinimal(strength: OnsetStrength.Strong, beat: 3.0m)
            };

            var result1 = DrummerVelocityShaper.ApplyHints(
                candidates, StyleConfigurationLibrary.PopRock, 0.5);
            var result2 = DrummerVelocityShaper.ApplyHints(
                candidates, StyleConfigurationLibrary.PopRock, 0.5);

            Assert.Equal(result1.Count, result2.Count);
            for (int i = 0; i < result1.Count; i++)
            {
                Assert.Equal(result1[i].VelocityHint, result2[i].VelocityHint);
            }
        }

        [Fact]
        public void ApplyHints_MultipleCalls_Deterministic()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Backbeat);
            var settings = DrummerVelocityHintSettings.PopRockDefaults;

            int? firstHint = null;
            for (int i = 0; i < 10; i++)
            {
                var (hinted, _) = DrummerVelocityShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

                if (firstHint == null)
                    firstHint = hinted.VelocityHint;
                else
                    Assert.Equal(firstHint, hinted.VelocityHint);
            }
        }

        #endregion

        #region Diagnostics Tests

        [Fact]
        public void ApplyHintWithDiagnostics_ReturnsCompleteDiagnostics()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Backbeat)
                with { VelocityHint = 90 };
            var settings = DrummerVelocityHintSettings.PopRockDefaults;

            var (_, diagnostics) = DrummerVelocityShaper.ApplyHintWithDiagnostics(candidate, settings, 0.7);

            Assert.Equal(candidate.CandidateId, diagnostics.CandidateId);
            Assert.Equal(candidate.Role, diagnostics.Role);
            Assert.Equal(candidate.Strength, diagnostics.Strength);
            Assert.Equal(DynamicIntent.StrongAccent, diagnostics.ClassifiedIntent);
            Assert.Equal(90, diagnostics.OriginalHint);
            Assert.True(diagnostics.FinalHint > 0 && diagnostics.FinalHint <= 127);
        }

        [Fact]
        public void ApplyHintWithDiagnostics_NoOriginalHint_OriginalHintIsNull()
        {
            var candidate = DrumCandidate.CreateMinimal(strength: OnsetStrength.Ghost);
            var settings = DrummerVelocityHintSettings.ConservativeDefaults;

            var (_, diagnostics) = DrummerVelocityShaper.ApplyHintWithDiagnostics(candidate, settings);

            Assert.Null(diagnostics.OriginalHint);
        }

        #endregion

        #region Settings Validation Tests

        [Fact]
        public void DrummerVelocityHintSettings_IsValid_ValidSettings_ReturnsTrue()
        {
            var settings = DrummerVelocityHintSettings.PopRockDefaults;

            Assert.True(settings.IsValid(out string? error));
            Assert.Null(error);
        }

        [Fact]
        public void DrummerVelocityHintSettings_IsValid_InvalidGhostTarget_ReturnsFalse()
        {
            var settings = new DrummerVelocityHintSettings { GhostVelocityTarget = 0 };

            Assert.False(settings.IsValid(out string? error));
            Assert.Contains("GhostVelocityTarget", error);
        }

        [Fact]
        public void DrummerVelocityHintSettings_IsValid_FillMinGreaterThanMax_ReturnsFalse()
        {
            var settings = new DrummerVelocityHintSettings
            {
                FillVelocityMin = 100,
                FillVelocityMax = 50
            };

            Assert.False(settings.IsValid(out string? error));
            Assert.Contains("FillVelocityMin", error);
        }

        #endregion

        #region Helper Methods

        private static DrumCandidate CreateCandidateWithRole(string role, OnsetStrength strength)
        {
            return new DrumCandidate
            {
                CandidateId = $"Test_{role}_1_1.0",
                OperatorId = "TestOperator",
                Role = role,
                BarNumber = 1,
                Beat = 1.0m,
                Strength = strength,
                FillRole = FillRole.None,
                Score = 0.5
            };
        }

        private static DrumCandidate CreateCandidateWithFillRole(FillRole fillRole)
        {
            return new DrumCandidate
            {
                CandidateId = $"Test_Fill_{fillRole}_1_3.0",
                OperatorId = "FillOperator",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 3.0m,
                Strength = OnsetStrength.Strong,
                FillRole = fillRole,
                Score = 0.5
            };
        }

        #endregion
    }
}

