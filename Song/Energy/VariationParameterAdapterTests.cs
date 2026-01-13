// AI: purpose=Tests for Story 7.6.5 variation parameter adapters ensuring bounded application with guardrails.
// AI: invariants=Adapters are pure functions; outputs always within safe ranges; determinism preserved; no side effects.
// AI: deps=Tests VariationParameterAdapter and VariationPlanDiagnostics; verifies adapters don't violate guardrails.

namespace Music.Generator;

/// <summary>
/// Tests for Story 7.6.5: Role-parameter application adapters + minimal diagnostics.
/// Verifies that variation deltas are applied correctly with guardrails enforced.
/// </summary>
public static class VariationParameterAdapterTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("=== Running Story 7.6.5 Tests: Variation Parameter Adapters ===\n");

        // Core adapter functionality
        TestApplyVariation_WithNullDelta_ReturnsBaseUnchanged();
        TestApplyVariation_WithDensityDelta_AppliesMultiplicatively();
        TestApplyVariation_WithVelocityDelta_AppliesAdditively();
        TestApplyVariation_WithRegisterDelta_AppliesAdditively();
        TestApplyVariation_WithBusyProbabilityDelta_AppliesAdditively();

        // Guardrail enforcement
        TestApplyVariation_DensityMultiplier_ClampedToSafeRange();
        TestApplyVariation_VelocityBias_ClampedToMidiRange();
        TestApplyVariation_RegisterLift_ClampedToSafeRange();
        TestApplyVariation_BusyProbability_ClampedTo0And1();

        // Drums special handling
        TestApplyVariationToDrums_AppliesCorrectly();
        TestApplyVariationToDrums_PreservesFillParameters();
        TestApplyVariationToDrums_WithNullDelta_ReturnsBaseUnchanged();

        // Diagnostics
        TestGetVariationDiagnostic_WithNullDelta_ReturnsNull();
        TestGetVariationDiagnostic_WithDeltas_ReturnsFormattedString();
        TestDiagnostics_DoNotAffectGeneration();
        TestDiagnostics_AreDeterministic();

        // Variation plan diagnostics
        TestVariationPlanDiagnostics_CompactReport();
        TestVariationPlanDiagnostics_DetailedReport();
        TestVariationPlanDiagnostics_Summary();

        Console.WriteLine("\n=== All Story 7.6.5 Tests Passed ===");
    }

    private static void TestApplyVariation_WithNullDelta_ReturnsBaseUnchanged()
    {
        var baseProfile = new EnergyRoleProfile
        {
            DensityMultiplier = 1.2,
            VelocityBias = 5,
            RegisterLiftSemitones = 12,
            BusyProbability = 0.6
        };

        var result = VariationParameterAdapter.ApplyVariation(baseProfile, null);

        Assert(result == baseProfile, "Null delta should return base unchanged");
        Console.WriteLine("? ApplyVariation with null delta returns base unchanged");
    }

    private static void TestApplyVariation_WithDensityDelta_AppliesMultiplicatively()
    {
        var baseProfile = new EnergyRoleProfile { DensityMultiplier = 1.0 };
        var delta = new RoleVariationDelta { DensityMultiplier = 1.2 };

        var result = VariationParameterAdapter.ApplyVariation(baseProfile, delta);

        Assert(Math.Abs(result.DensityMultiplier - 1.2) < 0.001, $"Expected 1.2, got {result.DensityMultiplier}");
        Console.WriteLine("? Density delta applied multiplicatively");
    }

    private static void TestApplyVariation_WithVelocityDelta_AppliesAdditively()
    {
        var baseProfile = new EnergyRoleProfile { VelocityBias = 5 };
        var delta = new RoleVariationDelta { VelocityBias = 10 };

        var result = VariationParameterAdapter.ApplyVariation(baseProfile, delta);

        Assert(result.VelocityBias == 15, $"Expected 15, got {result.VelocityBias}");
        Console.WriteLine("? Velocity delta applied additively");
    }

    private static void TestApplyVariation_WithRegisterDelta_AppliesAdditively()
    {
        var baseProfile = new EnergyRoleProfile { RegisterLiftSemitones = 0 };
        var delta = new RoleVariationDelta { RegisterLiftSemitones = 12 };

        var result = VariationParameterAdapter.ApplyVariation(baseProfile, delta);

        Assert(result.RegisterLiftSemitones == 12, $"Expected 12, got {result.RegisterLiftSemitones}");
        Console.WriteLine("? Register delta applied additively");
    }

    private static void TestApplyVariation_WithBusyProbabilityDelta_AppliesAdditively()
    {
        var baseProfile = new EnergyRoleProfile { BusyProbability = 0.5 };
        var delta = new RoleVariationDelta { BusyProbability = 0.2 };

        var result = VariationParameterAdapter.ApplyVariation(baseProfile, delta);

        Assert(Math.Abs(result.BusyProbability - 0.7) < 0.001, $"Expected 0.7, got {result.BusyProbability}");
        Console.WriteLine("? Busy probability delta applied additively");
    }

    private static void TestApplyVariation_DensityMultiplier_ClampedToSafeRange()
    {
        var baseProfile = new EnergyRoleProfile { DensityMultiplier = 1.0 };
        
        // Test upper bound
        var deltaHigh = new RoleVariationDelta { DensityMultiplier = 5.0 };
        var resultHigh = VariationParameterAdapter.ApplyVariation(baseProfile, deltaHigh);
        Assert(resultHigh.DensityMultiplier <= 2.0, $"Density should be clamped to max 2.0, got {resultHigh.DensityMultiplier}");

        // Test lower bound
        var deltaLow = new RoleVariationDelta { DensityMultiplier = 0.1 };
        var resultLow = VariationParameterAdapter.ApplyVariation(baseProfile, deltaLow);
        Assert(resultLow.DensityMultiplier >= 0.5, $"Density should be clamped to min 0.5, got {resultLow.DensityMultiplier}");

        Console.WriteLine("? Density multiplier clamped to safe range [0.5, 2.0]");
    }

    private static void TestApplyVariation_VelocityBias_ClampedToMidiRange()
    {
        var baseProfile = new EnergyRoleProfile { VelocityBias = 100 };
        
        // Test upper bound
        var deltaHigh = new RoleVariationDelta { VelocityBias = 100 };
        var resultHigh = VariationParameterAdapter.ApplyVariation(baseProfile, deltaHigh);
        Assert(resultHigh.VelocityBias <= 127, $"Velocity should be clamped to max 127, got {resultHigh.VelocityBias}");

        // Test lower bound
        var baseLow = new EnergyRoleProfile { VelocityBias = -100 };
        var deltaLow = new RoleVariationDelta { VelocityBias = -100 };
        var resultLow = VariationParameterAdapter.ApplyVariation(baseLow, deltaLow);
        Assert(resultLow.VelocityBias >= -127, $"Velocity should be clamped to min -127, got {resultLow.VelocityBias}");

        Console.WriteLine("? Velocity bias clamped to MIDI range [-127, 127]");
    }

    private static void TestApplyVariation_RegisterLift_ClampedToSafeRange()
    {
        var baseProfile = new EnergyRoleProfile { RegisterLiftSemitones = 24 };
        
        // Test upper bound
        var deltaHigh = new RoleVariationDelta { RegisterLiftSemitones = 36 };
        var resultHigh = VariationParameterAdapter.ApplyVariation(baseProfile, deltaHigh);
        Assert(resultHigh.RegisterLiftSemitones <= 48, $"Register should be clamped to max 48, got {resultHigh.RegisterLiftSemitones}");

        // Test lower bound
        var baseLow = new EnergyRoleProfile { RegisterLiftSemitones = -24 };
        var deltaLow = new RoleVariationDelta { RegisterLiftSemitones = -36 };
        var resultLow = VariationParameterAdapter.ApplyVariation(baseLow, deltaLow);
        Assert(resultLow.RegisterLiftSemitones >= -48, $"Register should be clamped to min -48, got {resultLow.RegisterLiftSemitones}");

        Console.WriteLine("? Register lift clamped to safe range [-48, 48]");
    }

    private static void TestApplyVariation_BusyProbability_ClampedTo0And1()
    {
        var baseProfile = new EnergyRoleProfile { BusyProbability = 0.8 };
        
        // Test upper bound
        var deltaHigh = new RoleVariationDelta { BusyProbability = 0.5 };
        var resultHigh = VariationParameterAdapter.ApplyVariation(baseProfile, deltaHigh);
        Assert(resultHigh.BusyProbability <= 1.0, $"Busy probability should be clamped to max 1.0, got {resultHigh.BusyProbability}");

        // Test lower bound
        var baseLow = new EnergyRoleProfile { BusyProbability = 0.1 };
        var deltaLow = new RoleVariationDelta { BusyProbability = -0.5 };
        var resultLow = VariationParameterAdapter.ApplyVariation(baseLow, deltaLow);
        Assert(resultLow.BusyProbability >= 0.0, $"Busy probability should be clamped to min 0.0, got {resultLow.BusyProbability}");

        Console.WriteLine("? Busy probability clamped to [0.0, 1.0]");
    }

    private static void TestApplyVariationToDrums_AppliesCorrectly()
    {
        var baseParams = new DrumRoleParameters
        {
            DensityMultiplier = 1.0,
            VelocityBias = 0.0,
            BusyProbability = 0.5,
            FillProbability = 0.1,
            FillComplexityMultiplier = 1.0
        };

        var delta = new RoleVariationDelta
        {
            DensityMultiplier = 1.2,
            VelocityBias = 5,
            BusyProbability = 0.1
        };

        var result = VariationParameterAdapter.ApplyVariationToDrums(baseParams, delta);

        Assert(Math.Abs(result.DensityMultiplier - 1.2) < 0.001, $"Density expected 1.2, got {result.DensityMultiplier}");
        Assert(Math.Abs(result.VelocityBias - 5.0) < 0.001, $"Velocity expected 5, got {result.VelocityBias}");
        Assert(Math.Abs(result.BusyProbability - 0.6) < 0.001, $"Busy expected 0.6, got {result.BusyProbability}");
        
        Console.WriteLine("? Drums variation applied correctly");
    }

    private static void TestApplyVariationToDrums_PreservesFillParameters()
    {
        var baseParams = new DrumRoleParameters
        {
            FillProbability = 0.15,
            FillComplexityMultiplier = 1.5
        };

        var delta = new RoleVariationDelta { DensityMultiplier = 1.1 };
        var result = VariationParameterAdapter.ApplyVariationToDrums(baseParams, delta);

        Assert(Math.Abs(result.FillProbability - 0.15) < 0.001, "Fill probability should be preserved");
        Assert(Math.Abs(result.FillComplexityMultiplier - 1.5) < 0.001, "Fill complexity should be preserved");
        
        Console.WriteLine("? Drums fill parameters preserved");
    }

    private static void TestApplyVariationToDrums_WithNullDelta_ReturnsBaseUnchanged()
    {
        var baseParams = new DrumRoleParameters { DensityMultiplier = 1.3 };
        var result = VariationParameterAdapter.ApplyVariationToDrums(baseParams, null);

        Assert(result == baseParams, "Null delta should return base unchanged");
        Console.WriteLine("? Drums with null delta returns base unchanged");
    }

    private static void TestGetVariationDiagnostic_WithNullDelta_ReturnsNull()
    {
        var baseProfile = new EnergyRoleProfile();
        var result = VariationParameterAdapter.GetVariationDiagnostic("Bass", baseProfile, null);

        Assert(result == null, "Diagnostic should return null for null delta");
        Console.WriteLine("? Diagnostic returns null for null delta");
    }

    private static void TestGetVariationDiagnostic_WithDeltas_ReturnsFormattedString()
    {
        var baseProfile = new EnergyRoleProfile
        {
            DensityMultiplier = 1.0,
            VelocityBias = 0
        };

        var delta = new RoleVariationDelta
        {
            DensityMultiplier = 1.2,
            VelocityBias = 5
        };

        var result = VariationParameterAdapter.GetVariationDiagnostic("Bass", baseProfile, delta);

        Assert(result != null, "Diagnostic should not be null");
        Assert(result!.Contains("Bass"), "Diagnostic should contain role name");
        Assert(result.Contains("Density"), "Diagnostic should mention density");
        Assert(result.Contains("Vel"), "Diagnostic should mention velocity");
        
        Console.WriteLine("? Diagnostic returns formatted string");
    }

    private static void TestDiagnostics_DoNotAffectGeneration()
    {
        var baseProfile = new EnergyRoleProfile { DensityMultiplier = 1.0 };
        var delta = new RoleVariationDelta { DensityMultiplier = 1.5 };

        // Apply variation
        var result1 = VariationParameterAdapter.ApplyVariation(baseProfile, delta);

        // Generate diagnostic (should not affect anything)
        var diagnostic = VariationParameterAdapter.GetVariationDiagnostic("Test", baseProfile, delta);

        // Apply again - should get same result
        var result2 = VariationParameterAdapter.ApplyVariation(baseProfile, delta);

        Assert(result1.DensityMultiplier == result2.DensityMultiplier, "Diagnostics should not affect generation");
        
        Console.WriteLine("? Diagnostics do not affect generation");
    }

    private static void TestDiagnostics_AreDeterministic()
    {
        var baseProfile = new EnergyRoleProfile { VelocityBias = 5 };
        var delta = new RoleVariationDelta { VelocityBias = 10 };

        var diagnostic1 = VariationParameterAdapter.GetVariationDiagnostic("Bass", baseProfile, delta);
        var diagnostic2 = VariationParameterAdapter.GetVariationDiagnostic("Bass", baseProfile, delta);

        Assert(diagnostic1 == diagnostic2, "Diagnostics should be deterministic");
        
        Console.WriteLine("? Diagnostics are deterministic");
    }

    private static void TestVariationPlanDiagnostics_CompactReport()
    {
        var query = CreateTestVariationQuery();
        var report = VariationPlanDiagnostics.GenerateCompactReport(query);

        Assert(!string.IsNullOrEmpty(report), "Compact report should not be empty");
        Assert(report.Contains("Section Variation Plan Report"), "Report should have header");
        Assert(report.Contains("Idx"), "Report should have column headers");
        
        Console.WriteLine("? Compact report generated");
    }

    private static void TestVariationPlanDiagnostics_DetailedReport()
    {
        var query = CreateTestVariationQuery();
        var report = VariationPlanDiagnostics.GenerateDetailedReport(query);

        Assert(!string.IsNullOrEmpty(report), "Detailed report should not be empty");
        Assert(report.Contains("Detailed Section Variation Report"), "Report should have header");
        Assert(report.Contains("Section 0"), "Report should list sections");
        
        Console.WriteLine("? Detailed report generated");
    }

    private static void TestVariationPlanDiagnostics_Summary()
    {
        var query = CreateTestVariationQuery();
        var summary = VariationPlanDiagnostics.GenerateSummary(query);

        Assert(!string.IsNullOrEmpty(summary), "Summary should not be empty");
        Assert(summary.Contains("Variation Plan Summary"), "Summary should have header");
        Assert(summary.Contains("Total Sections"), "Summary should show section count");
        
        Console.WriteLine("? Summary generated");
    }

    private static IVariationQuery CreateTestVariationQuery()
    {
        // Create simple test variation query with a few sections
        var sectionTrack = new SectionTrack();
        sectionTrack.Add(MusicConstants.eSectionType.Intro, 4);
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        return new DeterministicVariationQuery(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Test assertion failed: {message}");
        }
    }
}
