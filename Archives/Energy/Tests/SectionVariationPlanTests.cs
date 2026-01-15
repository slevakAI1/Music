// AI: purpose=Unit tests verifying SectionVariationPlan model invariants, clamping, immutability, and factory methods.
// AI: deps=Tests Story 7.6.1 acceptance criteria; must verify clamping/range enforcement, immutability, determinism.

namespace Music.Generator;

/// <summary>
/// Unit tests for SectionVariationPlan model (Story 7.6.1).
/// Verifies acceptance criteria:
/// - Clamping/range enforcement
/// - Immutability
/// - Factory method correctness
/// - Validation rules (BaseReferenceSectionIndex must be &lt; AbsoluteSectionIndex)
/// </summary>
public static class SectionVariationPlanTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("=== SectionVariationPlan Tests ===");

        // Basic model tests
        TestNoReuseFactoryMethod();
        TestWithReuseFactoryMethod();
        TestVariationIntensityClamping();
        TestBaseReferenceValidation();

        // Role delta tests
        TestRoleVariationDeltaClamping();
        TestRoleVariationDeltaFactoryMethods();
        TestVariationRoleDeltasFactoryMethods();

        // Immutability tests
        TestPlanImmutability();
        TestRoleDeltaImmutability();
        TestTagImmutability();

        // With* modifier tests
        TestWithVariationIntensityModifier();
        TestWithRoleDeltasModifier();
        TestWithTagModifier();

        // Tag tests
        TestDefaultTags();
        TestCustomTags();

        // Determinism tests
        TestFactoryMethodDeterminism();

        Console.WriteLine("=== All SectionVariationPlan Tests Passed ===");
    }

    private static void TestNoReuseFactoryMethod()
    {
        var plan = SectionVariationPlan.NoReuse(5, "A");

        AssertEqual(5, plan.AbsoluteSectionIndex, "AbsoluteSectionIndex");
        AssertNull(plan.BaseReferenceSectionIndex, "BaseReferenceSectionIndex should be null for NoReuse");
        AssertEqual(0.0, plan.VariationIntensity, "VariationIntensity should be 0.0 for NoReuse");
        AssertTrue(plan.Tags.Contains("A"), "Tags should contain 'A'");
        AssertNotNull(plan.Roles, "Roles should be non-null");

        Console.WriteLine("? NoReuse factory method");
    }

    private static void TestWithReuseFactoryMethod()
    {
        var roleDeltas = new VariationRoleDeltas
        {
            Bass = RoleVariationDelta.Create(densityMultiplier: 1.2)
        };

        var plan = SectionVariationPlan.WithReuse(
            absoluteSectionIndex: 5,
            baseReferenceSectionIndex: 2,
            variationIntensity: 0.6,
            roleDeltas: roleDeltas,
            tags: new[] { "Aprime", "Lift" });

        AssertEqual(5, plan.AbsoluteSectionIndex, "AbsoluteSectionIndex");
        AssertEqual(2, plan.BaseReferenceSectionIndex!.Value, "BaseReferenceSectionIndex");
        AssertEqual(0.6, plan.VariationIntensity, "VariationIntensity");
        AssertTrue(plan.Tags.Contains("Aprime"), "Tags should contain 'Aprime'");
        AssertTrue(plan.Tags.Contains("Lift"), "Tags should contain 'Lift'");
        AssertNotNull(plan.Roles.Bass, "Bass delta should be set");

        Console.WriteLine("? WithReuse factory method");
    }

    private static void TestVariationIntensityClamping()
    {
        // Test upper clamp
        var plan1 = SectionVariationPlan.WithReuse(5, 2, 1.5);
        AssertEqual(1.0, plan1.VariationIntensity, "VariationIntensity should clamp to 1.0");

        // Test lower clamp
        var plan2 = SectionVariationPlan.WithReuse(5, 2, -0.5);
        AssertEqual(0.0, plan2.VariationIntensity, "VariationIntensity should clamp to 0.0");

        // Test valid range
        var plan3 = SectionVariationPlan.WithReuse(5, 2, 0.5);
        AssertEqual(0.5, plan3.VariationIntensity, "VariationIntensity should preserve valid value");

        Console.WriteLine("? VariationIntensity clamping");
    }

    private static void TestBaseReferenceValidation()
    {
        // Valid: base < absolute
        var planValid = SectionVariationPlan.WithReuse(5, 2, 0.5);
        AssertEqual(2, planValid.BaseReferenceSectionIndex!.Value, "Valid base reference");

        // Invalid: base >= absolute (should throw)
        try
        {
            var planInvalid = SectionVariationPlan.WithReuse(5, 5, 0.5);
            throw new Exception("Should have thrown ArgumentException for base >= absolute");
        }
        catch (ArgumentException)
        {
            // Expected
        }

        try
        {
            var planInvalid = SectionVariationPlan.WithReuse(5, 6, 0.5);
            throw new Exception("Should have thrown ArgumentException for base > absolute");
        }
        catch (ArgumentException)
        {
            // Expected
        }

        Console.WriteLine("? BaseReferenceValidation");
    }

    private static void TestRoleVariationDeltaClamping()
    {
        // Test DensityMultiplier clamping
        var delta1 = RoleVariationDelta.Create(densityMultiplier: 3.0);
        AssertEqual(2.0, delta1.DensityMultiplier!.Value, "DensityMultiplier upper clamp");

        var delta2 = RoleVariationDelta.Create(densityMultiplier: 0.2);
        AssertEqual(0.5, delta2.DensityMultiplier!.Value, "DensityMultiplier lower clamp");

        // Test VelocityBias clamping
        var delta3 = RoleVariationDelta.Create(velocityBias: 50);
        AssertEqual(30, delta3.VelocityBias!.Value, "VelocityBias upper clamp");

        var delta4 = RoleVariationDelta.Create(velocityBias: -50);
        AssertEqual(-30, delta4.VelocityBias!.Value, "VelocityBias lower clamp");

        // Test RegisterLiftSemitones clamping
        var delta5 = RoleVariationDelta.Create(registerLiftSemitones: 36);
        AssertEqual(24, delta5.RegisterLiftSemitones!.Value, "RegisterLiftSemitones upper clamp");

        var delta6 = RoleVariationDelta.Create(registerLiftSemitones: -36);
        AssertEqual(-24, delta6.RegisterLiftSemitones!.Value, "RegisterLiftSemitones lower clamp");

        // Test BusyProbability clamping
        var delta7 = RoleVariationDelta.Create(busyProbability: 1.5);
        AssertEqual(1.0, delta7.BusyProbability!.Value, "BusyProbability upper clamp");

        var delta8 = RoleVariationDelta.Create(busyProbability: -1.5);
        AssertEqual(-1.0, delta8.BusyProbability!.Value, "BusyProbability lower clamp");

        Console.WriteLine("? RoleVariationDelta clamping");
    }

    private static void TestRoleVariationDeltaFactoryMethods()
    {
        // Test Neutral
        var neutral = RoleVariationDelta.Neutral();
        AssertNull(neutral.DensityMultiplier, "Neutral DensityMultiplier");
        AssertNull(neutral.VelocityBias, "Neutral VelocityBias");
        AssertNull(neutral.RegisterLiftSemitones, "Neutral RegisterLiftSemitones");
        AssertNull(neutral.BusyProbability, "Neutral BusyProbability");

        // Test Lift (all values should be positive/increase energy)
        var lift = RoleVariationDelta.Lift();
        AssertTrue(lift.DensityMultiplier!.Value > 1.0, "Lift DensityMultiplier > 1.0");
        AssertTrue(lift.VelocityBias!.Value > 0, "Lift VelocityBias > 0");
        AssertTrue(lift.RegisterLiftSemitones!.Value > 0, "Lift RegisterLiftSemitones > 0");
        AssertTrue(lift.BusyProbability!.Value > 0, "Lift BusyProbability > 0");

        // Test Thin (density/velocity should decrease)
        var thin = RoleVariationDelta.Thin();
        AssertTrue(thin.DensityMultiplier!.Value < 1.0, "Thin DensityMultiplier < 1.0");
        AssertTrue(thin.VelocityBias!.Value < 0, "Thin VelocityBias < 0");
        AssertTrue(thin.BusyProbability!.Value < 0, "Thin BusyProbability < 0");

        Console.WriteLine("? RoleVariationDelta factory methods");
    }

    private static void TestVariationRoleDeltasFactoryMethods()
    {
        // Test Neutral
        var neutral = VariationRoleDeltas.Neutral();
        AssertNull(neutral.Bass, "Neutral Bass");
        AssertNull(neutral.Comp, "Neutral Comp");
        AssertNull(neutral.Keys, "Neutral Keys");
        AssertNull(neutral.Pads, "Neutral Pads");
        AssertNull(neutral.Drums, "Neutral Drums");

        // Test AllRoles
        var delta = RoleVariationDelta.Lift();
        var allRoles = VariationRoleDeltas.AllRoles(delta);
        AssertNotNull(allRoles.Bass, "AllRoles Bass");
        AssertNotNull(allRoles.Comp, "AllRoles Comp");
        AssertNotNull(allRoles.Keys, "AllRoles Keys");
        AssertNotNull(allRoles.Pads, "AllRoles Pads");
        AssertNotNull(allRoles.Drums, "AllRoles Drums");

        // Verify they all reference the same delta instance
        AssertEqual(delta, allRoles.Bass, "AllRoles uses same delta instance");

        Console.WriteLine("? VariationRoleDeltas factory methods");
    }

    private static void TestPlanImmutability()
    {
        var plan1 = SectionVariationPlan.NoReuse(5);
        var plan2 = plan1 with { VariationIntensity = 0.7 };

        // Original should be unchanged
        AssertEqual(0.0, plan1.VariationIntensity, "Original plan unchanged");
        AssertEqual(0.7, plan2.VariationIntensity, "New plan has modified value");

        // They should be different instances
        AssertTrue(!ReferenceEquals(plan1, plan2), "Plans are different instances");

        Console.WriteLine("? Plan immutability");
    }

    private static void TestRoleDeltaImmutability()
    {
        var delta1 = RoleVariationDelta.Create(densityMultiplier: 1.2);
        var delta2 = delta1 with { VelocityBias = 5 };

        // Original should be unchanged
        AssertNull(delta1.VelocityBias, "Original delta unchanged");
        AssertEqual(5, delta2.VelocityBias!.Value, "New delta has modified value");
        AssertEqual(1.2, delta2.DensityMultiplier!.Value, "New delta preserves original values");

        Console.WriteLine("? RoleDelta immutability");
    }

    private static void TestTagImmutability()
    {
        var plan = SectionVariationPlan.NoReuse(5, "A");
        var originalTagCount = plan.Tags.Count;

        // Attempt to modify tags (should fail if truly immutable)
        try
        {
            var tags = plan.Tags as HashSet<string>;
            if (tags != null)
            {
                tags.Add("B");
                throw new Exception("Tags should be read-only");
            }
        }
        catch (InvalidCastException)
        {
            // Expected - Tags is IReadOnlySet, not HashSet
        }

        // Verify tag count unchanged
        AssertEqual(originalTagCount, plan.Tags.Count, "Tag count unchanged");

        Console.WriteLine("? Tag immutability");
    }

    private static void TestWithVariationIntensityModifier()
    {
        var plan1 = SectionVariationPlan.WithReuse(5, 2, 0.3);
        var plan2 = plan1.WithVariationIntensity(0.8);

        AssertEqual(0.3, plan1.VariationIntensity, "Original plan unchanged");
        AssertEqual(0.8, plan2.VariationIntensity, "Modified plan has new intensity");

        // Test clamping in modifier
        var plan3 = plan1.WithVariationIntensity(1.5);
        AssertEqual(1.0, plan3.VariationIntensity, "WithVariationIntensity clamps upper");

        var plan4 = plan1.WithVariationIntensity(-0.5);
        AssertEqual(0.0, plan4.VariationIntensity, "WithVariationIntensity clamps lower");

        Console.WriteLine("? WithVariationIntensity modifier");
    }

    private static void TestWithRoleDeltasModifier()
    {
        var plan1 = SectionVariationPlan.WithReuse(5, 2, 0.5);
        var newDeltas = new VariationRoleDeltas
        {
            Bass = RoleVariationDelta.Lift()
        };
        var plan2 = plan1.WithRoleDeltas(newDeltas);

        // Original plan roles unchanged (should be neutral)
        AssertNull(plan1.Roles.Bass, "Original plan roles unchanged");

        // New plan has updated roles
        AssertNotNull(plan2.Roles.Bass, "Modified plan has new roles");

        Console.WriteLine("? WithRoleDeltas modifier");
    }

    private static void TestWithTagModifier()
    {
        var plan1 = SectionVariationPlan.NoReuse(5, "A");
        var plan2 = plan1.WithTag("Lift");

        AssertEqual(1, plan1.Tags.Count, "Original plan tag count");
        AssertEqual(2, plan2.Tags.Count, "Modified plan tag count");
        AssertTrue(plan2.Tags.Contains("A"), "Modified plan preserves original tag");
        AssertTrue(plan2.Tags.Contains("Lift"), "Modified plan has new tag");

        Console.WriteLine("? WithTag modifier");
    }

    private static void TestDefaultTags()
    {
        var planNoReuse = SectionVariationPlan.NoReuse(5);
        AssertTrue(planNoReuse.Tags.Contains("A"), "NoReuse default tag is 'A'");

        var planWithReuse = SectionVariationPlan.WithReuse(5, 2, 0.5);
        AssertTrue(planWithReuse.Tags.Contains("Aprime"), "WithReuse default tag is 'Aprime'");

        Console.WriteLine("? Default tags");
    }

    private static void TestCustomTags()
    {
        var planNoReuse = SectionVariationPlan.NoReuse(5, "B");
        AssertTrue(planNoReuse.Tags.Contains("B"), "NoReuse accepts custom tag");

        var planWithReuse = SectionVariationPlan.WithReuse(
            5, 2, 0.5, tags: new[] { "Aprime", "Lift", "Peak" });
        AssertEqual(3, planWithReuse.Tags.Count, "WithReuse accepts multiple custom tags");
        AssertTrue(planWithReuse.Tags.Contains("Aprime"), "Contains Aprime");
        AssertTrue(planWithReuse.Tags.Contains("Lift"), "Contains Lift");
        AssertTrue(planWithReuse.Tags.Contains("Peak"), "Contains Peak");

        Console.WriteLine("? Custom tags");
    }

    private static void TestFactoryMethodDeterminism()
    {
        // Same inputs should produce equal plans
        var plan1 = SectionVariationPlan.NoReuse(5, "A");
        var plan2 = SectionVariationPlan.NoReuse(5, "A");

        AssertEqual(plan1.AbsoluteSectionIndex, plan2.AbsoluteSectionIndex, "Deterministic AbsoluteSectionIndex");
        AssertEqual(plan1.VariationIntensity, plan2.VariationIntensity, "Deterministic VariationIntensity");
        AssertEqual(plan1.Tags.Count, plan2.Tags.Count, "Deterministic tag count");

        // WithReuse determinism
        var roleDeltas = new VariationRoleDeltas
        {
            Bass = RoleVariationDelta.Create(densityMultiplier: 1.2)
        };

        var plan3 = SectionVariationPlan.WithReuse(5, 2, 0.6, roleDeltas);
        var plan4 = SectionVariationPlan.WithReuse(5, 2, 0.6, roleDeltas);

        AssertEqual(plan3.AbsoluteSectionIndex, plan4.AbsoluteSectionIndex, "Deterministic with reuse");
        AssertEqual(plan3.BaseReferenceSectionIndex, plan4.BaseReferenceSectionIndex, "Deterministic base ref");
        AssertEqual(plan3.VariationIntensity, plan4.VariationIntensity, "Deterministic intensity");

        Console.WriteLine("? Factory method determinism");
    }

    // Helper assertion methods
    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"FAIL: {message}. Expected: {expected}, Actual: {actual}");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"FAIL: {message}");
        }
    }

    private static void AssertNull(object? value, string message)
    {
        if (value != null)
        {
            throw new Exception($"FAIL: {message}. Expected null but got: {value}");
        }
    }

    private static void AssertNotNull<T>(T? value, string message) where T : class
    {
        if (value == null)
        {
            throw new Exception($"FAIL: {message}. Expected non-null value");
        }
    }
}
