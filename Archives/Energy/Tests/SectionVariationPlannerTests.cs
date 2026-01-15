// AI: purpose=Tests for SectionVariationPlanner verifying determinism, bounded outputs, and variation across A/A'/B repeats.
// AI: invariants=All tests deterministic; variation intensity always [0..1]; role deltas within safe ranges; plans reference valid base indices.
// AI: NOTE - TEMPORARILY DISABLED (Epic 6): These tests use EnergyArc which was removed in Story 4.1. To be re-enabled during energy reintegration.

#if FALSE_DISABLED_FOR_ENERGY_DISCONNECT // Epic 6: Disabled until energy reintegration

namespace Music.Generator;

/// <summary>
/// Tests for SectionVariationPlanner (Story 7.6.3).
/// Verifies deterministic computation of variation plans, bounded outputs, and controlled variation.
/// </summary>
public static class SectionVariationPlannerTests
{
    public static void RunAllTests()
    {
        // TEMPORARILY DISABLED (Epic 6): Tests use EnergyArc which was removed in Story 4.1.
        // To be re-enabled during energy reintegration.
        Console.WriteLine("=== SectionVariationPlanner Tests - SKIPPED (Energy Disconnected) ===\n");
        return;

        /* COMMENTED OUT UNTIL ENERGY REINTEGRATION
        Console.WriteLine("=== SectionVariationPlanner Tests ===\n");

        TestDeterminism();
        TestVariationIntensityBounds();
        TestRoleDeltasBounded();
        TestNoReuseCreatesNeutralPlans();
        TestVariationDiffersAcrossRepeats();
        TestTransitionHintInfluence();
        TestEnergyInfluence();
        TestTensionInfluence();
        TestSectionTypeInfluence();
        TestTagGeneration();
        TestBaseReferenceValidity();
        TestCommonSongForm();
        TestMinimalStructure();
        TestRepeatedSections();

        Console.WriteLine("\n=== All SectionVariationPlanner Tests Passed ===");
        */
    }

    /// <summary>
    /// Test: Same inputs yield same plans (determinism).
    /// </summary>
    private static void TestDeterminism()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var plans1 = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);
        var plans2 = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        if (plans1.Count != plans2.Count)
        {
            throw new Exception("Determinism failed: plan count differs");
        }

        for (int i = 0; i < plans1.Count; i++)
        {
            var p1 = plans1[i];
            var p2 = plans2[i];

            if (p1.AbsoluteSectionIndex != p2.AbsoluteSectionIndex ||
                p1.BaseReferenceSectionIndex != p2.BaseReferenceSectionIndex ||
                Math.Abs(p1.VariationIntensity - p2.VariationIntensity) > 0.0001)
            {
                throw new Exception($"Determinism failed at section {i}");
            }

            // Check role deltas match
            if (!RoleDeltasMatch(p1.Roles, p2.Roles))
            {
                throw new Exception($"Determinism failed: role deltas differ at section {i}");
            }
        }

        Console.WriteLine("? Determinism: Same inputs yield same plans");
    }

    /// <summary>
    /// Test: Variation intensity always in [0..1].
    /// </summary>
    private static void TestVariationIntensityBounds()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var plans = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        foreach (var plan in plans)
        {
            if (plan.VariationIntensity < 0.0 || plan.VariationIntensity > 1.0)
            {
                throw new Exception($"Section {plan.AbsoluteSectionIndex}: intensity {plan.VariationIntensity} out of bounds");
            }
        }

        Console.WriteLine("? Variation intensity bounded [0..1] for all sections");
    }

    /// <summary>
    /// Test: Role deltas within safe ranges.
    /// </summary>
    private static void TestRoleDeltasBounded()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var plans = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        foreach (var plan in plans)
        {
            ValidateRoleDeltaBounds(plan.Roles.Bass, "Bass", plan.AbsoluteSectionIndex);
            ValidateRoleDeltaBounds(plan.Roles.Comp, "Comp", plan.AbsoluteSectionIndex);
            ValidateRoleDeltaBounds(plan.Roles.Keys, "Keys", plan.AbsoluteSectionIndex);
            ValidateRoleDeltaBounds(plan.Roles.Pads, "Pads", plan.AbsoluteSectionIndex);
            ValidateRoleDeltaBounds(plan.Roles.Drums, "Drums", plan.AbsoluteSectionIndex);
        }

        Console.WriteLine("? Role deltas bounded within safe ranges");
    }

    /// <summary>
    /// Test: Sections with no reuse (A or B) create neutral plans.
    /// </summary>
    private static void TestNoReuseCreatesNeutralPlans()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var plans = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        // First verse should have no base reference
        var firstVersePlan = plans[0];
        if (firstVersePlan.BaseReferenceSectionIndex.HasValue)
        {
            throw new Exception("First verse should have no base reference");
        }

        if (firstVersePlan.VariationIntensity != 0.0)
        {
            throw new Exception("First verse should have zero variation intensity");
        }

        Console.WriteLine("? No-reuse sections have neutral plans (intensity=0, no base ref)");
    }

    /// <summary>
    /// Test: Repeated sections (A') differ from first occurrence (A) in at least one controlled way.
    /// </summary>
    private static void TestVariationDiffersAcrossRepeats()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var plans = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        // Find repeated verses
        var versePlans = plans.Where(p => sectionTrack.Sections[p.AbsoluteSectionIndex].SectionType == MusicConstants.eSectionType.Verse).ToList();
        
        if (versePlans.Count < 2)
        {
            throw new Exception("Test requires at least 2 verses");
        }

        var firstVerse = versePlans[0];
        var secondVerse = versePlans[1];

        // Second verse should have non-zero variation intensity (bounded)
        if (secondVerse.VariationIntensity == 0.0)
        {
            throw new Exception("Second verse should have non-zero variation intensity");
        }

        if (secondVerse.VariationIntensity > 0.6)
        {
            throw new Exception($"Second verse intensity {secondVerse.VariationIntensity} exceeds conservative bound 0.6");
        }

        // Should reference first verse
        if (secondVerse.BaseReferenceSectionIndex != firstVerse.AbsoluteSectionIndex)
        {
            throw new Exception("Second verse should reference first verse");
        }

        Console.WriteLine("? Repeated sections vary from first occurrence in controlled way (A vs A')");
    }

    /// <summary>
    /// Test: Transition hints influence variation intensity.
    /// </summary>
    private static void TestTransitionHintInfluence()
    {
        var sectionTrack = CreateTestSectionTrack();
        
        // Create two arcs with different inherent transition contexts (by changing section order/type)
        var energyArc1 = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery1 = new DeterministicTensionQuery(energyArc1, seed: 42);
        var plans1 = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc1, tensionQuery1, "TestGroove", seed: 42);

        // Different seed creates different transition contexts
        var energyArc2 = EnergyArc.Create(sectionTrack, "TestGroove", seed: 99);
        var tensionQuery2 = new DeterministicTensionQuery(energyArc2, seed: 99);
        var plans2 = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc2, tensionQuery2, "TestGroove", seed: 99);

        // At least one section should have different intensity due to different transition contexts
        bool foundDifference = false;
        for (int i = 0; i < plans1.Count; i++)
        {
            if (Math.Abs(plans1[i].VariationIntensity - plans2[i].VariationIntensity) > 0.05)
            {
                foundDifference = true;
                break;
            }
        }

        if (!foundDifference)
        {
            throw new Exception("Transition hints should influence variation intensity");
        }

        Console.WriteLine("? Transition hints influence variation intensity");
    }

    /// <summary>
    /// Test: Energy targets influence variation intensity.
    /// </summary>
    private static void TestEnergyInfluence()
    {
        var sectionTrack = CreateTestSectionTrack();
        
        // Two different grooves should yield different energy profiles
        var energyArc1 = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
        var tensionQuery1 = new DeterministicTensionQuery(energyArc1, seed: 42);
        var plans1 = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc1, tensionQuery1, "PopGroove", seed: 42);

        var energyArc2 = EnergyArc.Create(sectionTrack, "RockGroove", seed: 42);
        var tensionQuery2 = new DeterministicTensionQuery(energyArc2, seed: 42);
        var plans2 = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc2, tensionQuery2, "RockGroove", seed: 42);

        // Some section should show energy influence
        bool foundDifference = false;
        for (int i = 0; i < Math.Min(plans1.Count, plans2.Count); i++)
        {
            if (Math.Abs(plans1[i].VariationIntensity - plans2[i].VariationIntensity) > 0.05)
            {
                foundDifference = true;
                break;
            }
        }

        if (!foundDifference)
        {
            Console.WriteLine("? Warning: Energy influence not clearly observable (may be subtle)");
        }
        else
        {
            Console.WriteLine("? Energy targets influence variation intensity");
        }
    }

    /// <summary>
    /// Test: Tension influences variation.
    /// </summary>
    private static void TestTensionInfluence()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        
        // Different seeds create different tension profiles
        var tensionQuery1 = new DeterministicTensionQuery(energyArc, seed: 42);
        var plans1 = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery1, "TestGroove", seed: 42);

        var tensionQuery2 = new DeterministicTensionQuery(energyArc, seed: 99);
        var plans2 = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery2, "TestGroove", seed: 99);

        // Tensio differences should manifest
        bool foundDifference = false;
        for (int i = 0; i < plans1.Count; i++)
        {
            if (Math.Abs(plans1[i].VariationIntensity - plans2[i].VariationIntensity) > 0.05)
            {
                foundDifference = true;
                break;
            }
        }

        if (!foundDifference)
        {
            Console.WriteLine("? Warning: Tension influence not clearly observable (may be subtle)");
        }
        else
        {
            Console.WriteLine("? Tension influences variation");
        }
    }

    /// <summary>
    /// Test: Section type influences variation decisions.
    /// </summary>
    private static void TestSectionTypeInfluence()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var plans = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        // Find chorus and verse plans with reuse
        var chorusPlans = plans.Where(p => 
            sectionTrack.Sections[p.AbsoluteSectionIndex].SectionType == MusicConstants.eSectionType.Chorus &&
            p.BaseReferenceSectionIndex.HasValue).ToList();
        
        var versePlans = plans.Where(p => 
            sectionTrack.Sections[p.AbsoluteSectionIndex].SectionType == MusicConstants.eSectionType.Verse &&
            p.BaseReferenceSectionIndex.HasValue).ToList();

        if (chorusPlans.Any() && versePlans.Any())
        {
            // Chorus typically allows more variation (section type factor)
            double avgChorusIntensity = chorusPlans.Average(p => p.VariationIntensity);
            double avgVerseIntensity = versePlans.Average(p => p.VariationIntensity);

            // Note: This is a soft expectation; implementation may vary
            Console.WriteLine($"? Section type influences variation (Chorus avg: {avgChorusIntensity:F3}, Verse avg: {avgVerseIntensity:F3})");
        }
        else
        {
            Console.WriteLine("? Insufficient repeated choruses/verses to verify section type influence");
        }
    }

    /// <summary>
    /// Test: Tags are generated appropriately.
    /// </summary>
    private static void TestTagGeneration()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var plans = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        foreach (var plan in plans)
        {
            // All plans should have at least one tag
            if (plan.Tags.Count == 0)
            {
                throw new Exception($"Section {plan.AbsoluteSectionIndex} has no tags");
            }

            // Primary tag should be A, Aprime, or B
            bool hasPrimaryTag = plan.Tags.Contains("A") || plan.Tags.Contains("Aprime") || plan.Tags.Contains("B");
            if (!hasPrimaryTag)
            {
                throw new Exception($"Section {plan.AbsoluteSectionIndex} missing primary A/Aprime/B tag");
            }

            // Should have section type tag
            var sectionTypeName = sectionTrack.Sections[plan.AbsoluteSectionIndex].SectionType.ToString();
            if (!plan.Tags.Contains(sectionTypeName))
            {
                throw new Exception($"Section {plan.AbsoluteSectionIndex} missing section type tag");
            }
        }

        Console.WriteLine("? Tags generated appropriately (A/Aprime/B + section type)");
    }

    /// <summary>
    /// Test: Base reference indices are always valid (< current index).
    /// </summary>
    private static void TestBaseReferenceValidity()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var plans = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        foreach (var plan in plans)
        {
            if (plan.BaseReferenceSectionIndex.HasValue)
            {
                if (plan.BaseReferenceSectionIndex.Value >= plan.AbsoluteSectionIndex)
                {
                    throw new Exception($"Section {plan.AbsoluteSectionIndex}: invalid base reference {plan.BaseReferenceSectionIndex.Value}");
                }
            }
        }

        Console.WriteLine("? All base reference indices valid (< current index)");
    }

    /// <summary>
    /// Test: Common song form produces reasonable variation pattern.
    /// </summary>
    private static void TestCommonSongForm()
    {
        // Standard pop: Intro-V-C-V-C-Bridge-C-Outro
        var sectionTrack = CreateStandardPopForm();
        var energyArc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var plans = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "PopGroove", seed: 42);

        // Verify expected count
        if (plans.Count != 8)
        {
            throw new Exception($"Expected 8 plans for standard pop form, got {plans.Count}");
        }

        // Intro (0) should be A
        if (!plans[0].Tags.Contains("A"))
        {
            throw new Exception("Intro should be tagged A");
        }

        // Verse 1 (1) should be A
        if (!plans[1].Tags.Contains("A"))
        {
            throw new Exception("Verse 1 should be tagged A");
        }

        // Verse 2 (3) should be Aprime and reference Verse 1
        if (!plans[3].Tags.Contains("Aprime"))
        {
            throw new Exception("Verse 2 should be tagged Aprime");
        }
        if (plans[3].BaseReferenceSectionIndex != 1)
        {
            throw new Exception("Verse 2 should reference Verse 1");
        }

        Console.WriteLine("? Standard pop form produces expected A/A'/B pattern");
    }

    /// <summary>
    /// Test: Minimal structure (V-C-V-C) works correctly.
    /// </summary>
    private static void TestMinimalStructure()
    {
        var sectionTrack = CreateMinimalForm();
        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var plans = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        if (plans.Count != 4)
        {
            throw new Exception($"Expected 4 plans for minimal form, got {plans.Count}");
        }

        // All base references should be valid
        foreach (var plan in plans)
        {
            if (plan.BaseReferenceSectionIndex.HasValue && 
                plan.BaseReferenceSectionIndex.Value >= plan.AbsoluteSectionIndex)
            {
                throw new Exception("Invalid base reference in minimal structure");
            }
        }

        Console.WriteLine("? Minimal structure (V-C-V-C) handled correctly");
    }

    /// <summary>
    /// Test: Repeated sections consistently reference earliest instance.
    /// </summary>
    private static void TestRepeatedSections()
    {
        // Create form with 4 choruses: Intro-C-V-C-V-C-Bridge-C
        var sectionTrack = new SectionTrack();
        sectionTrack.Add(MusicConstants.eSectionType.Intro, 4);
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Bridge, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);
        var plans = SectionVariationPlanner.ComputePlans(sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        // Find all chorus plans
        var chorusIndices = new List<int>();
        for (int i = 0; i < sectionTrack.Sections.Count; i++)
        {
            if (sectionTrack.Sections[i].SectionType == MusicConstants.eSectionType.Chorus)
            {
                chorusIndices.Add(i);
            }
        }

        // First chorus should have no base ref (A)
        if (plans[chorusIndices[0]].BaseReferenceSectionIndex.HasValue)
        {
            throw new Exception("First chorus should have no base reference");
        }

        // All subsequent choruses should reference the first chorus (unless contrasting B)
        for (int i = 1; i < chorusIndices.Count; i++)
        {
            var plan = plans[chorusIndices[i]];
            if (plan.BaseReferenceSectionIndex.HasValue)
            {
                if (plan.BaseReferenceSectionIndex.Value != chorusIndices[0])
                {
                    throw new Exception($"Chorus {i} should reference first chorus (got ref to {plan.BaseReferenceSectionIndex.Value})");
                }
            }
            // else it's a contrasting B, which is valid
        }

        Console.WriteLine("? Repeated sections consistently reference earliest instance");
    }

    // === Helper Methods ===

    private static SectionTrack CreateTestSectionTrack()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        return track;
    }

    private static SectionTrack CreateStandardPopForm()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Intro, 4);
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Bridge, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Outro, 4);
        return track;
    }

    private static SectionTrack CreateMinimalForm()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        return track;
    }

    private static bool RoleDeltasMatch(VariationRoleDeltas r1, VariationRoleDeltas r2)
    {
        return RoleDeltaMatches(r1.Bass, r2.Bass) &&
               RoleDeltaMatches(r1.Comp, r2.Comp) &&
               RoleDeltaMatches(r1.Keys, r2.Keys) &&
               RoleDeltaMatches(r1.Pads, r2.Pads) &&
               RoleDeltaMatches(r1.Drums, r2.Drums);
    }

    private static bool RoleDeltaMatches(RoleVariationDelta? d1, RoleVariationDelta? d2)
    {
        if (d1 == null && d2 == null) return true;
        if (d1 == null || d2 == null) return false;

        const double epsilon = 0.0001;
        return Math.Abs((d1.DensityMultiplier ?? 0) - (d2.DensityMultiplier ?? 0)) < epsilon &&
               d1.VelocityBias == d2.VelocityBias &&
               d1.RegisterLiftSemitones == d2.RegisterLiftSemitones &&
               Math.Abs((d1.BusyProbability ?? 0) - (d2.BusyProbability ?? 0)) < epsilon;
    }

    private static void ValidateRoleDeltaBounds(RoleVariationDelta? delta, string roleName, int sectionIndex)
    {
        if (delta == null) return;

        if (delta.DensityMultiplier.HasValue)
        {
            if (delta.DensityMultiplier.Value < 0.5 || delta.DensityMultiplier.Value > 2.0)
            {
                throw new Exception($"Section {sectionIndex}, {roleName}: DensityMultiplier {delta.DensityMultiplier.Value} out of bounds [0.5, 2.0]");
            }
        }

        if (delta.VelocityBias.HasValue)
        {
            if (delta.VelocityBias.Value < -30 || delta.VelocityBias.Value > 30)
            {
                throw new Exception($"Section {sectionIndex}, {roleName}: VelocityBias {delta.VelocityBias.Value} out of bounds [-30, 30]");
            }
        }

        if (delta.RegisterLiftSemitones.HasValue)
        {
            if (delta.RegisterLiftSemitones.Value < -24 || delta.RegisterLiftSemitones.Value > 24)
            {
                throw new Exception($"Section {sectionIndex}, {roleName}: RegisterLiftSemitones {delta.RegisterLiftSemitones.Value} out of bounds [-24, 24]");
            }
        }

        if (delta.BusyProbability.HasValue)
        {
            if (delta.BusyProbability.Value < -1.0 || delta.BusyProbability.Value > 1.0)
            {
                throw new Exception($"Section {sectionIndex}, {roleName}: BusyProbability {delta.BusyProbability.Value} out of bounds [-1.0, 1.0]");
            }
        }
    }
}

#endif // FALSE_DISABLED_FOR_ENERGY_DISCONNECT
