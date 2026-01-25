// AI: purpose=Unit tests for Story 6.2 DrummerTimingShaper.
// AI: deps=xunit for test framework; Music.Generator.Agents.Drums.Performance for types under test.
// AI: change=Story 6.2 acceptance criteria: determinism, style targets, minimal adjustment, fill timing, role defaults.

using Xunit;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Performance;
using Music.Generator.Agents.Common;
using Music.Generator.Groove;

namespace Music.Tests.Generator.Agents.Drums.Performance
{
    /// <summary>
    /// Story 6.2: Tests for DrummerTimingShaper.
    /// Verifies timing hint application, determinism, and style-aware behavior.
    /// </summary>
    public class DrummerTimingShaperTests
    {
        #region ApplyHints Basic Tests

        [Fact]
        public void ApplyHints_NullCandidates_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DrummerTimingShaper.ApplyHints(null!, StyleConfigurationLibrary.PopRock));
        }

        [Fact]
        public void ApplyHints_EmptyCandidates_ReturnsEmpty()
        {
            var result = DrummerTimingShaper.ApplyHints(
                Array.Empty<DrumCandidate>(),
                StyleConfigurationLibrary.PopRock);

            Assert.Empty(result);
        }

        [Fact]
        public void ApplyHints_NullStyleConfig_UsesConservativeDefaults()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var candidates = new[] { candidate };

            var result = DrummerTimingShaper.ApplyHints(candidates, null);

            Assert.Single(result);
            Assert.True(result[0].TimingHint.HasValue);
            // Conservative snare default is +3 (SlightlyBehind with conservative settings)
            Assert.InRange(result[0].TimingHint!.Value, -5, 10);
        }

        [Fact]
        public void ApplyHints_SingleCandidate_ReturnsHintedCandidate()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var candidates = new[] { candidate };

            var result = DrummerTimingShaper.ApplyHints(
                candidates,
                StyleConfigurationLibrary.PopRock);

            Assert.Single(result);
            Assert.True(result[0].TimingHint.HasValue);
        }

        [Fact]
        public void ApplyHints_MultipleCandidates_AllGetHints()
        {
            var candidates = new[]
            {
                DrumCandidate.CreateMinimal(role: GrooveRoles.Kick, beat: 1.0m),
                DrumCandidate.CreateMinimal(role: GrooveRoles.Snare, beat: 2.0m),
                DrumCandidate.CreateMinimal(role: GrooveRoles.ClosedHat, beat: 1.5m)
            };

            var result = DrummerTimingShaper.ApplyHints(candidates, StyleConfigurationLibrary.PopRock);

            Assert.Equal(3, result.Count);
            Assert.All(result, c => Assert.True(c.TimingHint.HasValue));
        }

        #endregion

        #region Timing Intent Classification - Role-Based Tests

        [Fact]
        public void ClassifyTimingIntent_Snare_DefaultsToSlightlyBehind()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var intent = DrummerTimingShaper.ClassifyTimingIntent(candidate, settings);

            Assert.Equal(TimingIntent.SlightlyBehind, intent);
        }

        [Fact]
        public void ClassifyTimingIntent_Kick_DefaultsToOnTop()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Kick);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var intent = DrummerTimingShaper.ClassifyTimingIntent(candidate, settings);

            Assert.Equal(TimingIntent.OnTop, intent);
        }

        [Fact]
        public void ClassifyTimingIntent_ClosedHat_DefaultsToOnTop()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.ClosedHat);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var intent = DrummerTimingShaper.ClassifyTimingIntent(candidate, settings);

            Assert.Equal(TimingIntent.OnTop, intent);
        }

        [Fact]
        public void ClassifyTimingIntent_Crash_DefaultsToOnTop()
        {
            var candidate = CreateCandidateWithRole(GrooveRoles.Crash);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var intent = DrummerTimingShaper.ClassifyTimingIntent(candidate, settings);

            Assert.Equal(TimingIntent.OnTop, intent);
        }

        [Fact]
        public void ClassifyTimingIntent_UnknownRole_FallsBackToOnTop()
        {
            var candidate = CreateCandidateWithRole("CustomDrum");
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var intent = DrummerTimingShaper.ClassifyTimingIntent(candidate, settings);

            Assert.Equal(TimingIntent.OnTop, intent);
        }

        #endregion

        #region Timing Intent Classification - Fill Role Tests

        [Theory]
        [InlineData(FillRole.FillStart, TimingIntent.SlightlyAhead)]
        [InlineData(FillRole.FillBody, TimingIntent.Rushed)]
        [InlineData(FillRole.FillEnd, TimingIntent.OnTop)]
        [InlineData(FillRole.Setup, TimingIntent.OnTop)]
        public void ClassifyTimingIntent_FillRoles_ReturnsExpectedIntent(FillRole fillRole, TimingIntent expectedIntent)
        {
            var candidate = CreateCandidateWithFillRole(fillRole);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var intent = DrummerTimingShaper.ClassifyTimingIntent(candidate, settings);

            Assert.Equal(expectedIntent, intent);
        }

        [Fact]
        public void ClassifyTimingIntent_FillRoleTakesPriorityOverRole()
        {
            // Snare normally gets SlightlyBehind, but in FillBody should get Rushed
            var candidate = new DrumCandidate
            {
                CandidateId = "Test_Snare_1_3",
                OperatorId = "Test",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 3.0m,
                Strength = OnsetStrength.Strong,
                FillRole = FillRole.FillBody,
                Score = 0.5
            };

            var intent = DrummerTimingShaper.ClassifyTimingIntent(candidate);

            Assert.Equal(TimingIntent.Rushed, intent);
        }

        [Fact]
        public void ClassifyTimingIntent_NonFillCandidate_UsesRoleDefault()
        {
            var candidate = new DrumCandidate
            {
                CandidateId = "Test_Snare_1_2",
                OperatorId = "Test",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 2.0m,
                Strength = OnsetStrength.Backbeat,
                FillRole = FillRole.None,
                Score = 0.5
            };

            var intent = DrummerTimingShaper.ClassifyTimingIntent(candidate);

            Assert.Equal(TimingIntent.SlightlyBehind, intent);
        }

        #endregion

        #region Style Target Tests

        [Fact]
        public void ApplyHints_PopRockSnare_UsesSlightlyBehindOffset()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

            // PopRock SlightlyBehind = +5 ticks (before jitter/energy)
            Assert.Equal(TimingIntent.SlightlyBehind, diag.ClassifiedIntent);
            Assert.Equal(5, diag.BaseTargetOffset);
        }

        [Fact]
        public void ApplyHints_PopRockKick_UsesOnTopOffset()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Kick);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

            Assert.Equal(TimingIntent.OnTop, diag.ClassifiedIntent);
            Assert.Equal(0, diag.BaseTargetOffset);
        }

        [Fact]
        public void ApplyHints_JazzSnare_UsesJazzOffset()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var settings = DrummerTimingHintSettings.JazzDefaults;

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

            // Jazz SlightlyBehind = +8 ticks
            Assert.Equal(TimingIntent.SlightlyBehind, diag.ClassifiedIntent);
            Assert.Equal(8, diag.BaseTargetOffset);
        }

        [Fact]
        public void ApplyHints_JazzKick_UsesJazzRoleDefault()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Kick);
            var settings = DrummerTimingHintSettings.JazzDefaults;

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

            // Jazz kick is SlightlyBehind (+8 from SlightlyBehindTicks... wait, Jazz kick should use +3)
            // Actually Jazz kick is SlightlyBehind so uses SlightlyBehindTicks = 8
            Assert.Equal(TimingIntent.SlightlyBehind, diag.ClassifiedIntent);
        }

        [Fact]
        public void ApplyHints_MetalSnare_UsesTightOnTopOffset()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var settings = DrummerTimingHintSettings.MetalDefaults;

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

            // Metal snare is OnTop (tight)
            Assert.Equal(TimingIntent.OnTop, diag.ClassifiedIntent);
            Assert.Equal(0, diag.BaseTargetOffset);
        }

        #endregion

        #region Minimal Adjustment Tests

        [Fact]
        public void MinimalAdjustment_ExistingHint_AdjustsTowardTarget()
        {
            // Create candidate with existing hint of -10 (very rushed)
            var candidate = new DrumCandidate
            {
                CandidateId = "Test_Snare_1_2",
                OperatorId = "Test",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 2.0m,
                Strength = OnsetStrength.Backbeat,
                TimingHint = -10,  // Existing rushed hint
                FillRole = FillRole.None,
                Score = 0.5
            };

            var settings = DrummerTimingHintSettings.PopRockDefaults; // MaxAdjustmentDelta = 5, target = +5

            var result = DrummerTimingShaper.ApplyHintToCandidate(candidate, settings, 0.5);

            // Should adjust from -10 toward +5, but only by MaxAdjustmentDelta (5)
            // So -10 + 5 = -5 (before jitter)
            Assert.True(result.TimingHint > -10);
        }

        [Fact]
        public void MinimalAdjustment_HintWithinDelta_MovesToTarget()
        {
            var candidate = new DrumCandidate
            {
                CandidateId = "Test_Snare_1_2",
                OperatorId = "Test",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = 2.0m,
                Strength = OnsetStrength.Backbeat,
                TimingHint = 3,  // Close to target of +5
                FillRole = FillRole.None,
                Score = 0.5
            };

            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

            // Hint of 3 is within MaxAdjustmentDelta (5) of target (5), so moves to target
            Assert.Equal(3, diag.OriginalHint);
            Assert.Equal(5, diag.HintBeforeJitter); // Moved to target (energy neutral at 0.5)
        }

        [Fact]
        public void MinimalAdjustment_NoExistingHint_UsesTargetDirectly()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

            Assert.Null(diag.OriginalHint);
            Assert.Equal(5, diag.HintBeforeJitter); // Target directly (PopRock snare = +5)
        }

        #endregion

        #region Energy Adjustment Tests

        [Fact]
        public void EnergyAdjustment_HighEnergy_NudgesEarlier()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 1.0);

            // High energy (1.0) should nudge -2 ticks
            // Base = 5, energy adjustment = (0.5 - 1.0) * 4 = -2
            Assert.Equal(3, diag.EnergyAdjustedTarget); // 5 - 2 = 3
        }

        [Fact]
        public void EnergyAdjustment_LowEnergy_NudgesLater()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.0);

            // Low energy (0.0) should nudge +2 ticks
            // Base = 5, energy adjustment = (0.5 - 0.0) * 4 = +2
            Assert.Equal(7, diag.EnergyAdjustedTarget); // 5 + 2 = 7
        }

        [Fact]
        public void EnergyAdjustment_NeutralEnergy_NoChange()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

            // Neutral energy (0.5) should have no adjustment
            Assert.Equal(5, diag.EnergyAdjustedTarget); // Same as base
        }

        #endregion

        #region Jitter Tests

        [Fact]
        public void Jitter_IsDeterministic_SameInputsSameJitter()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var (result1, diag1) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);
            var (result2, diag2) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

            Assert.Equal(diag1.Jitter, diag2.Jitter);
            Assert.Equal(result1.TimingHint, result2.TimingHint);
        }

        [Fact]
        public void Jitter_IsWithinBounds()
        {
            var settings = DrummerTimingHintSettings.PopRockDefaults; // MaxTimingJitter = 3

            // Test multiple candidates
            for (int bar = 1; bar <= 10; bar++)
            {
                for (decimal beat = 1.0m; beat <= 4.0m; beat += 0.5m)
                {
                    var candidate = DrumCandidate.CreateMinimal(
                        role: GrooveRoles.Snare,
                        barNumber: bar,
                        beat: beat);

                    var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

                    Assert.InRange(diag.Jitter, -3, 3);
                }
            }
        }

        [Fact]
        public void Jitter_ZeroMaxJitter_NoJitter()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var settings = new DrummerTimingHintSettings { MaxTimingJitter = 0 };

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(candidate, settings, 0.5);

            Assert.Equal(0, diag.Jitter);
        }

        #endregion

        #region Clamping Tests

        [Fact]
        public void Clamping_ExtremeOffset_ClampedToMax()
        {
            // Create settings with extreme values
            var settings = new DrummerTimingHintSettings
            {
                LaidBackTicks = 50,  // Exceeds MaxAbsoluteOffset
                MaxAbsoluteOffset = 20
            };

            // Note: Settings.IsValid() would fail, but we test clamping behavior
            var candidate = CreateCandidateWithFillRole(FillRole.None); // Uses role default
            
            // Use a role that maps to LaidBack (none do by default, so we need a custom approach)
            // Actually, let's test with a fill that has extreme settings
            var rushSettings = new DrummerTimingHintSettings
            {
                RushedTicks = -50,
                MaxAbsoluteOffset = 20,
                MaxTimingJitter = 0
            };

            var fillCandidate = CreateCandidateWithFillRole(FillRole.FillBody);
            var result = DrummerTimingShaper.ApplyHintToCandidate(fillCandidate, rushSettings, 0.5);

            // Should be clamped to -20
            Assert.InRange(result.TimingHint!.Value, -20, 20);
        }

        [Fact]
        public void Clamping_JitterOverflow_StaysWithinBounds()
        {
            var settings = new DrummerTimingHintSettings
            {
                SlightlyBehindTicks = 18,  // Close to max
                MaxTimingJitter = 5,
                MaxAbsoluteOffset = 20
            };

            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var result = DrummerTimingShaper.ApplyHintToCandidate(candidate, settings, 0.5);

            // Even with jitter, should stay within [-20, 20]
            Assert.InRange(result.TimingHint!.Value, -20, 20);
        }

        #endregion

        #region Fill Timing Progression Tests

        [Fact]
        public void FillTiming_Progression_RushToOnTop()
        {
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            var fillStart = CreateCandidateWithFillRole(FillRole.FillStart);
            var fillBody = CreateCandidateWithFillRole(FillRole.FillBody);
            var fillEnd = CreateCandidateWithFillRole(FillRole.FillEnd);

            var (startResult, startDiag) = DrummerTimingShaper.ApplyHintWithDiagnostics(fillStart, settings, 0.5);
            var (bodyResult, bodyDiag) = DrummerTimingShaper.ApplyHintWithDiagnostics(fillBody, settings, 0.5);
            var (endResult, endDiag) = DrummerTimingShaper.ApplyHintWithDiagnostics(fillEnd, settings, 0.5);

            // FillStart = SlightlyAhead (-5)
            Assert.Equal(-5, startDiag.BaseTargetOffset);
            
            // FillBody = Rushed (-10)
            Assert.Equal(-10, bodyDiag.BaseTargetOffset);
            
            // FillEnd = OnTop (0)
            Assert.Equal(0, endDiag.BaseTargetOffset);
        }

        [Fact]
        public void FillTiming_SetupHit_IsOnTop()
        {
            var settings = DrummerTimingHintSettings.PopRockDefaults;
            var setup = CreateCandidateWithFillRole(FillRole.Setup);

            var (result, diag) = DrummerTimingShaper.ApplyHintWithDiagnostics(setup, settings, 0.5);

            Assert.Equal(TimingIntent.OnTop, diag.ClassifiedIntent);
            Assert.Equal(0, diag.BaseTargetOffset);
        }

        #endregion

        #region Determinism Tests

        [Fact]
        public void Determinism_SameInputs_IdenticalOutput()
        {
            var candidates = new[]
            {
                DrumCandidate.CreateMinimal(role: GrooveRoles.Kick, beat: 1.0m),
                DrumCandidate.CreateMinimal(role: GrooveRoles.Snare, beat: 2.0m),
                CreateCandidateWithFillRole(FillRole.FillBody)
            };

            var result1 = DrummerTimingShaper.ApplyHints(candidates, StyleConfigurationLibrary.PopRock, 0.6);
            var result2 = DrummerTimingShaper.ApplyHints(candidates, StyleConfigurationLibrary.PopRock, 0.6);

            Assert.Equal(result1.Count, result2.Count);
            for (int i = 0; i < result1.Count; i++)
            {
                Assert.Equal(result1[i].TimingHint, result2[i].TimingHint);
            }
        }

        [Fact]
        public void Determinism_DifferentEnergy_DifferentOutput()
        {
            var candidate = DrumCandidate.CreateMinimal(role: GrooveRoles.Snare);
            var candidates = new[] { candidate };

            var result1 = DrummerTimingShaper.ApplyHints(candidates, StyleConfigurationLibrary.PopRock, 0.0);
            var result2 = DrummerTimingShaper.ApplyHints(candidates, StyleConfigurationLibrary.PopRock, 1.0);

            Assert.NotEqual(result1[0].TimingHint, result2[0].TimingHint);
        }

        #endregion

        #region Settings Validation Tests

        [Fact]
        public void Settings_ValidSettings_PassesValidation()
        {
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            Assert.True(settings.IsValid(out var error));
            Assert.Null(error);
        }

        [Fact]
        public void Settings_NegativeJitter_FailsValidation()
        {
            var settings = new DrummerTimingHintSettings { MaxTimingJitter = -1 };

            Assert.False(settings.IsValid(out var error));
            Assert.Contains("MaxTimingJitter", error);
        }

        [Fact]
        public void Settings_OffsetExceedsMax_FailsValidation()
        {
            var settings = new DrummerTimingHintSettings
            {
                RushedTicks = -30,
                MaxAbsoluteOffset = 20
            };

            Assert.False(settings.IsValid(out var error));
            Assert.Contains("RushedTicks", error);
        }

        [Fact]
        public void Settings_GetTickOffset_ReturnsCorrectValues()
        {
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            Assert.Equal(0, settings.GetTickOffset(TimingIntent.OnTop));
            Assert.Equal(-5, settings.GetTickOffset(TimingIntent.SlightlyAhead));
            Assert.Equal(5, settings.GetTickOffset(TimingIntent.SlightlyBehind));
            Assert.Equal(-10, settings.GetTickOffset(TimingIntent.Rushed));
            Assert.Equal(10, settings.GetTickOffset(TimingIntent.LaidBack));
        }

        [Fact]
        public void Settings_GetRoleDefaultIntent_ReturnsCorrectDefaults()
        {
            var settings = DrummerTimingHintSettings.PopRockDefaults;

            Assert.Equal(TimingIntent.SlightlyBehind, settings.GetRoleDefaultIntent(GrooveRoles.Snare));
            Assert.Equal(TimingIntent.OnTop, settings.GetRoleDefaultIntent(GrooveRoles.Kick));
            Assert.Equal(TimingIntent.OnTop, settings.GetRoleDefaultIntent(GrooveRoles.ClosedHat));
            Assert.Equal(TimingIntent.OnTop, settings.GetRoleDefaultIntent("UnknownRole"));
        }

        #endregion

        #region Style Integration Tests

        [Fact]
        public void StyleIntegration_PopRock_HasTimingHints()
        {
            var style = StyleConfigurationLibrary.PopRock;

            Assert.NotNull(style.DrummerTimingHints);
            var hints = style.GetDrummerTimingHints();
            Assert.True(hints.IsValid(out _));
        }

        [Fact]
        public void StyleIntegration_Jazz_HasTimingHints()
        {
            var style = StyleConfigurationLibrary.Jazz;

            Assert.NotNull(style.DrummerTimingHints);
            var hints = style.GetDrummerTimingHints();
            Assert.True(hints.IsValid(out _));
        }

        [Fact]
        public void StyleIntegration_Metal_HasTimingHints()
        {
            var style = StyleConfigurationLibrary.Metal;

            Assert.NotNull(style.DrummerTimingHints);
            var hints = style.GetDrummerTimingHints();
            Assert.True(hints.IsValid(out _));
        }

        [Fact]
        public void StyleIntegration_MissingStyle_UsesConservativeDefaults()
        {
            // Create a minimal style without timing hints
            var style = new StyleConfiguration
            {
                StyleId = "Test",
                DisplayName = "Test",
                AllowedOperatorIds = Array.Empty<string>(),
                OperatorWeights = new Dictionary<string, double>(),
                RoleDensityDefaults = new Dictionary<string, double>(),
                RoleCaps = new Dictionary<string, int>(),
                FeelRules = FeelRules.Straight,
                GridRules = GridRules.SixteenthGrid,
                DrummerTimingHints = null
            };

            var hints = style.GetDrummerTimingHints();

            Assert.NotNull(hints);
            Assert.Equal(DrummerTimingHintSettings.ConservativeDefaults.SlightlyAheadTicks, hints.SlightlyAheadTicks);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void EdgeCase_FillInBar1_WorksCorrectly()
        {
            var candidate = new DrumCandidate
            {
                CandidateId = "Fill_Snare_1_3",
                OperatorId = "Fill",
                Role = GrooveRoles.Snare,
                BarNumber = 1,  // First bar
                Beat = 3.0m,
                Strength = OnsetStrength.Strong,
                FillRole = FillRole.FillStart,
                Score = 0.7
            };

            var result = DrummerTimingShaper.ApplyHintToCandidate(
                candidate,
                DrummerTimingHintSettings.PopRockDefaults,
                0.5);

            Assert.True(result.TimingHint.HasValue);
            Assert.True(result.TimingHint < 0); // Should be ahead (negative)
        }

        [Fact]
        public void EdgeCase_MixedFillAndNonFill_SameBar()
        {
            var candidates = new[]
            {
                // Non-fill kick on beat 1
                new DrumCandidate
                {
                    CandidateId = "Groove_Kick_1_1",
                    OperatorId = "Groove",
                    Role = GrooveRoles.Kick,
                    BarNumber = 1,
                    Beat = 1.0m,
                    Strength = OnsetStrength.Downbeat,
                    FillRole = FillRole.None,
                    Score = 0.8
                },
                // Fill body on beat 3
                new DrumCandidate
                {
                    CandidateId = "Fill_Snare_1_3",
                    OperatorId = "Fill",
                    Role = GrooveRoles.Snare,
                    BarNumber = 1,
                    Beat = 3.0m,
                    Strength = OnsetStrength.Strong,
                    FillRole = FillRole.FillBody,
                    Score = 0.7
                }
            };

            var result = DrummerTimingShaper.ApplyHints(
                candidates,
                StyleConfigurationLibrary.PopRock,
                0.5);

            // Kick should have OnTop-ish timing
            var kickIntent = DrummerTimingShaper.ClassifyTimingIntent(candidates[0]);
            Assert.Equal(TimingIntent.OnTop, kickIntent);

            // Fill snare should have Rushed timing
            var fillIntent = DrummerTimingShaper.ClassifyTimingIntent(candidates[1]);
            Assert.Equal(TimingIntent.Rushed, fillIntent);
        }

        [Fact]
        public void EdgeCase_AllCandidatesAreFills()
        {
            var candidates = new[]
            {
                CreateCandidateWithFillRole(FillRole.FillStart, beat: 3.0m),
                CreateCandidateWithFillRole(FillRole.FillBody, beat: 3.5m),
                CreateCandidateWithFillRole(FillRole.FillBody, beat: 4.0m),
                CreateCandidateWithFillRole(FillRole.FillEnd, beat: 4.5m)
            };

            var result = DrummerTimingShaper.ApplyHints(
                candidates,
                StyleConfigurationLibrary.PopRock,
                0.5);

            Assert.All(result, c => Assert.True(c.TimingHint.HasValue));
        }

        #endregion

        #region Helper Methods

        private static DrumCandidate CreateCandidateWithRole(string role)
        {
            return new DrumCandidate
            {
                CandidateId = $"Test_{role}_1_1",
                OperatorId = "Test",
                Role = role,
                BarNumber = 1,
                Beat = 1.0m,
                Strength = OnsetStrength.Strong,
                FillRole = FillRole.None,
                Score = 0.5
            };
        }

        private static DrumCandidate CreateCandidateWithFillRole(FillRole fillRole, decimal beat = 3.0m)
        {
            return new DrumCandidate
            {
                CandidateId = $"Fill_Snare_1_{beat}_{fillRole}",
                OperatorId = "Fill",
                Role = GrooveRoles.Snare,
                BarNumber = 1,
                Beat = beat,
                Strength = OnsetStrength.Strong,
                FillRole = fillRole,
                Score = 0.7
            };
        }

        #endregion
    }
}
