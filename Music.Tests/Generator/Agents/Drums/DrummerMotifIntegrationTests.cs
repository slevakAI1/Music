// AI: purpose=Integration tests for DrummerAgent motif integration (Story 9.3).
// AI: deps=DrummerPolicyProvider, MotifPresenceMap, MicroAddition operators; xUnit + FluentAssertions.

using FluentAssertions;
using Music.Generator;
using Music.Generator.Agents.Common;
using Music.Generator.Agents.Drums;
using Music.Generator.Agents.Drums.Operators.MicroAddition;
using Music.Generator.Groove;
using Music.Generator.Material;
using Music.Song.Material;
using Xunit;

namespace Music.Generator.Agents.Drums.Tests;

/// <summary>
/// Integration tests for DrummerAgent motif integration (Story 9.3).
/// Verifies that motif presence affects policy decisions and operator scoring.
/// </summary>
[Collection("RngDependentTests")]
public class DrummerMotifIntegrationTests
{
    public DrummerMotifIntegrationTests()
    {
        Rng.Initialize(42);
    }

    #region DrummerPolicyProvider Integration

    [Fact]
    public void DrummerPolicyProvider_WithMotifActive_ReducesDensity()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var motif = CreateTestMotif("Lead", "TestHook");
        var placement = MotifPlacement.Create(motif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 4);
        var plan = MotifPlacementPlan.Create(new[] { placement }, seed: 42);
        var motifMap = new MotifPresenceMap(plan, sectionTrack);

        var styleConfig = StyleConfigurationLibrary.GetStyle("PopRock");
        var providerWithMotif = new DrummerPolicyProvider(styleConfig, motifPresenceMap: motifMap);
        var providerWithoutMotif = new DrummerPolicyProvider(styleConfig);

        var barContext = CreateBarContext(barNumber: 1, sectionType: MusicConstants.eSectionType.Verse);

        // Act
        var policyWithMotif = providerWithMotif.GetPolicy(barContext, GrooveRoles.Kick);
        var policyWithoutMotif = providerWithoutMotif.GetPolicy(barContext, GrooveRoles.Kick);

        // Assert: Density with motif should be lower
        policyWithMotif!.Density01Override!.Value.Should().BeLessThan(policyWithoutMotif!.Density01Override!.Value);
    }

    [Fact]
    public void DrummerPolicyProvider_WithMotifActive_AddsMotifPresentTag()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var motif = CreateTestMotif("Lead", "TestHook");
        var placement = MotifPlacement.Create(motif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 4);
        var plan = MotifPlacementPlan.Create(new[] { placement }, seed: 42);
        var motifMap = new MotifPresenceMap(plan, sectionTrack);

        var styleConfig = StyleConfigurationLibrary.GetStyle("PopRock");
        var provider = new DrummerPolicyProvider(styleConfig, motifPresenceMap: motifMap);
        var barContext = CreateBarContext(barNumber: 1, sectionType: MusicConstants.eSectionType.Verse);

        // Act
        var policy = provider.GetPolicy(barContext, GrooveRoles.Kick);

        // Assert
        policy!.EnabledVariationTagsOverride.Should().Contain("MotifPresent");
    }

    [Fact]
    public void DrummerPolicyProvider_NoMotifInBar_NoMotifPresentTag()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var motif = CreateTestMotif("Lead", "TestHook");
        // Motif in bar 1-2 only
        var placement = MotifPlacement.Create(motif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 2);
        var plan = MotifPlacementPlan.Create(new[] { placement }, seed: 42);
        var motifMap = new MotifPresenceMap(plan, sectionTrack);

        var styleConfig = StyleConfigurationLibrary.GetStyle("PopRock");
        var provider = new DrummerPolicyProvider(styleConfig, motifPresenceMap: motifMap);
        var barContext = CreateBarContext(barNumber: 5, sectionType: MusicConstants.eSectionType.Chorus); // Bar 5 has no motif

        // Act
        var policy = provider.GetPolicy(barContext, GrooveRoles.Kick);

        // Assert
        if (policy?.EnabledVariationTagsOverride != null)
        {
            policy.EnabledVariationTagsOverride.Should().NotContain("MotifPresent");
        }
    }

    [Fact]
    public void DrummerPolicyProvider_DensityReduction_BoundedAtMax()
    {
        // Arrange: Multiple motifs to try to exceed 20% reduction
        var sectionTrack = CreateTestSectionTrack();
        var motif1 = CreateTestMotif("Lead", "Hook1");
        var motif2 = CreateTestMotif("Guitar", "Hook2");
        var motif3 = CreateTestMotif("Bass", "Hook3");
        var p1 = MotifPlacement.Create(motif1, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var p2 = MotifPlacement.Create(motif2, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var p3 = MotifPlacement.Create(motif3, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var plan = MotifPlacementPlan.Create(new[] { p1, p2, p3 }, seed: 42);
        var motifMap = new MotifPresenceMap(plan, sectionTrack);

        var styleConfig = StyleConfigurationLibrary.GetStyle("PopRock");
        var settings = new DrummerPolicySettings
        {
            MotifDensityReductionPercent = 0.15,
            MaxMotifDensityReduction = 0.20
        };
        var provider = new DrummerPolicyProvider(styleConfig, settings: settings, motifPresenceMap: motifMap);
        var providerNoMotif = new DrummerPolicyProvider(styleConfig);

        var barContext = CreateBarContext(barNumber: 1, sectionType: MusicConstants.eSectionType.Verse);

        // Act
        var policyWithMotif = provider.GetPolicy(barContext, GrooveRoles.Kick);
        var policyNoMotif = providerNoMotif.GetPolicy(barContext, GrooveRoles.Kick);

        // Assert: Reduction should be at most 20%
        double baseD = policyNoMotif!.Density01Override!.Value;
        double motifD = policyWithMotif!.Density01Override!.Value;
        double reductionPercent = 1.0 - (motifD / baseD);
        reductionPercent.Should().BeLessThanOrEqualTo(0.21, "Reduction should not exceed 20% (with small tolerance)");
    }

    #endregion

    #region Operator Score Reduction Tests

    [Fact]
    public void GhostClusterOperator_WithMotif_ReducesScoreBy50Percent()
    {
        // Arrange
        var contextWithoutMotif = CreateDrummerContext(barNumber: 1, energyLevel: 0.6, motifPresenceMap: null);
        var motifMap = CreateMotifMapForBar(1);
        var contextWithMotif = CreateDrummerContext(barNumber: 1, energyLevel: 0.6, motifPresenceMap: motifMap);

        var op = new GhostClusterOperator();

        // Act
        var candidatesWithout = op.GenerateCandidates(contextWithoutMotif).ToList();
        var candidatesWith = op.GenerateCandidates(contextWithMotif).ToList();

        // Assert: Both should generate candidates
        candidatesWithout.Should().NotBeEmpty("Operator should generate candidates without motif");
        candidatesWith.Should().NotBeEmpty("Operator should generate candidates with motif");

        // Scores with motif should be ~50% of scores without
        for (int i = 0; i < Math.Min(candidatesWithout.Count, candidatesWith.Count); i++)
        {
            double ratio = candidatesWith[i].Score / candidatesWithout[i].Score;
            ratio.Should().BeApproximately(0.5, 0.05, $"Candidate {i} should have ~50% score reduction");
        }
    }

    [Fact]
    public void HatEmbellishmentOperator_WithMotif_ReducesScoreBy30Percent()
    {
        // Arrange
        var contextWithoutMotif = CreateDrummerContext(
            barNumber: 1, 
            energyLevel: 0.6, 
            motifPresenceMap: null,
            hatSubdivision: HatSubdivision.Eighth,
            activeRoles: new HashSet<string> { GrooveRoles.ClosedHat, GrooveRoles.Kick, GrooveRoles.Snare });
        var motifMap = CreateMotifMapForBar(1);
        var contextWithMotif = CreateDrummerContext(
            barNumber: 1, 
            energyLevel: 0.6, 
            motifPresenceMap: motifMap,
            hatSubdivision: HatSubdivision.Eighth,
            activeRoles: new HashSet<string> { GrooveRoles.ClosedHat, GrooveRoles.Kick, GrooveRoles.Snare });

        var op = new HatEmbellishmentOperator();

        // Act
        var candidatesWithout = op.GenerateCandidates(contextWithoutMotif).ToList();
        var candidatesWith = op.GenerateCandidates(contextWithMotif).ToList();

        // Assert
        candidatesWithout.Should().NotBeEmpty("Operator should generate candidates without motif");
        candidatesWith.Should().NotBeEmpty("Operator should generate candidates with motif");

        // Scores with motif should be ~70% of scores without (30% reduction)
        for (int i = 0; i < Math.Min(candidatesWithout.Count, candidatesWith.Count); i++)
        {
            double ratio = candidatesWith[i].Score / candidatesWithout[i].Score;
            ratio.Should().BeApproximately(0.7, 0.05, $"Candidate {i} should have ~30% score reduction");
        }
    }

    [Fact]
    public void GhostBeforeBackbeatOperator_WithMotif_ReducesScoreBy20Percent()
    {
        // Arrange
        var contextWithoutMotif = CreateDrummerContext(barNumber: 1, energyLevel: 0.5, motifPresenceMap: null);
        var motifMap = CreateMotifMapForBar(1);
        var contextWithMotif = CreateDrummerContext(barNumber: 1, energyLevel: 0.5, motifPresenceMap: motifMap);

        var op = new GhostBeforeBackbeatOperator();

        // Act
        var candidatesWithout = op.GenerateCandidates(contextWithoutMotif).ToList();
        var candidatesWith = op.GenerateCandidates(contextWithMotif).ToList();

        // Assert
        candidatesWithout.Should().NotBeEmpty("Operator should generate candidates without motif");
        candidatesWith.Should().NotBeEmpty("Operator should generate candidates with motif");

        // Scores with motif should be ~80% of scores without (20% reduction)
        for (int i = 0; i < Math.Min(candidatesWithout.Count, candidatesWith.Count); i++)
        {
            double ratio = candidatesWith[i].Score / candidatesWithout[i].Score;
            ratio.Should().BeApproximately(0.8, 0.05, $"Candidate {i} should have ~20% score reduction");
        }
    }

    [Fact]
    public void GhostAfterBackbeatOperator_WithMotif_ReducesScoreBy20Percent()
    {
        // Arrange
        var contextWithoutMotif = CreateDrummerContext(barNumber: 1, energyLevel: 0.5, motifPresenceMap: null);
        var motifMap = CreateMotifMapForBar(1);
        var contextWithMotif = CreateDrummerContext(barNumber: 1, energyLevel: 0.5, motifPresenceMap: motifMap);

        var op = new GhostAfterBackbeatOperator();

        // Act
        var candidatesWithout = op.GenerateCandidates(contextWithoutMotif).ToList();
        var candidatesWith = op.GenerateCandidates(contextWithMotif).ToList();

        // Assert
        candidatesWithout.Should().NotBeEmpty("Operator should generate candidates without motif");
        candidatesWith.Should().NotBeEmpty("Operator should generate candidates with motif");

        // Scores with motif should be ~80% of scores without (20% reduction)
        for (int i = 0; i < Math.Min(candidatesWithout.Count, candidatesWith.Count); i++)
        {
            double ratio = candidatesWith[i].Score / candidatesWithout[i].Score;
            ratio.Should().BeApproximately(0.8, 0.05, $"Candidate {i} should have ~20% score reduction");
        }
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void Determinism_SameMotifPlacement_SamePolicyDecisions()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var motif = CreateTestMotif("Lead", "TestHook");
        var placement = MotifPlacement.Create(motif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 4);
        var plan = MotifPlacementPlan.Create(new[] { placement }, seed: 42);
        
        var motifMap1 = new MotifPresenceMap(plan, sectionTrack);
        var motifMap2 = new MotifPresenceMap(plan, sectionTrack);

        var styleConfig = StyleConfigurationLibrary.GetStyle("PopRock");
        var provider1 = new DrummerPolicyProvider(styleConfig, motifPresenceMap: motifMap1);
        var provider2 = new DrummerPolicyProvider(styleConfig, motifPresenceMap: motifMap2);

        // Act & Assert
        for (int bar = 1; bar <= 8; bar++)
        {
            var barContext = CreateBarContext(barNumber: bar, sectionType: MusicConstants.eSectionType.Verse);
            var policy1 = provider1.GetPolicy(barContext, GrooveRoles.Kick);
            var policy2 = provider2.GetPolicy(barContext, GrooveRoles.Kick);

            policy1!.Density01Override.Should().Be(policy2!.Density01Override, $"Bar {bar} density should be deterministic");
        }
    }

    #endregion

    #region Test Helpers

    private static SectionTrack CreateTestSectionTrack()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 4);   // Section 0: bars 1-4
        track.Add(MusicConstants.eSectionType.Chorus, 4);  // Section 1: bars 5-8
        return track;
    }

    private static MotifSpec CreateTestMotif(string role, string name)
    {
        return new MotifSpec(
            MotifId: new PartTrack.PartTrackId(Guid.NewGuid().ToString()),
            Name: name,
            IntendedRole: role,
            Kind: MaterialKind.Hook,
            RhythmShape: new List<int> { 0, 240, 480, 720 },
            Contour: ContourIntent.Arch,
            Register: new RegisterIntent(60, 12),
            TonePolicy: new TonePolicy(0.8, true),
            Tags: new HashSet<string>()
        );
    }

    private static GrooveBarContext CreateBarContext(int barNumber, MusicConstants.eSectionType sectionType)
    {
        var section = new Section { SectionType = sectionType, StartBar = ((barNumber - 1) / 4) * 4 + 1, BarCount = 4 };
        return new GrooveBarContext(
            BarNumber: barNumber,
            Section: section,
            SegmentProfile: null,
            BarWithinSection: (barNumber - 1) % 4,
            BarsUntilSectionEnd: 4 - ((barNumber - 1) % 4));
    }

    private static MotifPresenceMap CreateMotifMapForBar(int barNumber)
    {
        var sectionTrack = CreateTestSectionTrack();
        var motif = CreateTestMotif("Lead", "TestHook");
        int sectionIndex = (barNumber - 1) / 4;
        int barWithinSection = (barNumber - 1) % 4;
        var placement = MotifPlacement.Create(motif, absoluteSectionIndex: sectionIndex, startBarWithinSection: barWithinSection, durationBars: 1);
        var plan = MotifPlacementPlan.Create(new[] { placement }, seed: 42);
        return new MotifPresenceMap(plan, sectionTrack);
    }

    private static DrummerContext CreateDrummerContext(
        int barNumber,
        double energyLevel,
        MotifPresenceMap? motifPresenceMap,
        HatSubdivision hatSubdivision = HatSubdivision.Eighth,
        IReadOnlySet<string>? activeRoles = null)
    {
        return new DrummerContext
        {
            BarNumber = barNumber,
            Beat = 1.0m,
            SectionType = MusicConstants.eSectionType.Verse,
            PhrasePosition = 0.0,
            BarsUntilSectionEnd = 4,
            EnergyLevel = energyLevel,
            TensionLevel = 0.0,
            MotifPresenceScore = motifPresenceMap?.GetMotifDensity(barNumber) ?? 0.0,
            Seed = 42,
            RngStreamKey = $"Test_{barNumber}",
            MotifPresenceMap = motifPresenceMap,
            ActiveRoles = activeRoles ?? new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat },
            LastKickBeat = null,
            LastSnareBeat = null,
            CurrentHatMode = HatMode.Closed,
            HatSubdivision = hatSubdivision,
            IsFillWindow = false,
            IsAtSectionBoundary = false,
            BackbeatBeats = new List<int> { 2, 4 },
            BeatsPerBar = 4
        };
    }

    #endregion
}
