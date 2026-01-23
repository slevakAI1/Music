// AI: purpose=Unit tests for MotifPresenceMap (Story 9.3); verifies absolute bar queries, role filtering, and density calculations.
// AI: deps=MotifPresenceMap, MotifPlacementPlan, SectionTrack, MotifSpec; xUnit + FluentAssertions.

using FluentAssertions;
using Music.Generator;
using Music.Generator.Material;
using Music.Song.Material;
using Xunit;

namespace Music.Tests.Generator.Material;

/// <summary>
/// Unit tests for MotifPresenceMap (Story 9.3).
/// Verifies motif presence queries by absolute bar number, role filtering, and density calculations.
/// </summary>
public class MotifPresenceMapTests
{
    #region Constructor and Empty Tests

    [Fact]
    public void Constructor_WithValidInputs_CreatesMap()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var plan = MotifPlacementPlan.Empty();

        // Act
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Assert
        map.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullPlan_ThrowsArgumentNullException()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();

        // Act
        var act = () => new MotifPresenceMap(null!, sectionTrack);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullSectionTrack_ThrowsArgumentNullException()
    {
        // Arrange
        var plan = MotifPlacementPlan.Empty();

        // Act
        var act = () => new MotifPresenceMap(plan, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Empty_ReturnsMapWithNoMotifs()
    {
        // Arrange & Act
        var map = MotifPresenceMap.Empty;

        // Assert
        map.IsMotifActive(1).Should().BeFalse();
        map.GetMotifDensity(1).Should().Be(0.0);
        map.GetActiveMotifs(1).Should().BeEmpty();
    }

    #endregion

    #region IsMotifActive Tests

    [Fact]
    public void IsMotifActive_EmptyPlan_ReturnsFalseForAllBars()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var plan = MotifPlacementPlan.Empty();
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        for (int bar = 1; bar <= 16; bar++)
        {
            map.IsMotifActive(bar).Should().BeFalse($"Bar {bar} should have no motif");
        }
    }

    [Fact]
    public void IsMotifActive_SingleMotifInSection_ReturnsTrueForCoveredBars()
    {
        // Arrange: Section 0 starts at bar 1, motif covers bars 0-1 within section (absolute bars 1-2)
        var sectionTrack = CreateTestSectionTrack();
        var motif = CreateTestMotif("Lead", "TestHook");
        var placement = MotifPlacement.Create(motif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 2);
        var plan = MotifPlacementPlan.Create(new[] { placement }, seed: 42);
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        map.IsMotifActive(1).Should().BeTrue("Bar 1 should have motif (section 0, bar 0)");
        map.IsMotifActive(2).Should().BeTrue("Bar 2 should have motif (section 0, bar 1)");
        map.IsMotifActive(3).Should().BeFalse("Bar 3 should not have motif");
    }

    [Fact]
    public void IsMotifActive_WithRoleFilter_OnlyMatchesMatchingRole()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var leadMotif = CreateTestMotif("Lead", "LeadHook");
        var placement = MotifPlacement.Create(leadMotif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 2);
        var plan = MotifPlacementPlan.Create(new[] { placement }, seed: 42);
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        map.IsMotifActive(1, "Lead").Should().BeTrue("Bar 1 has Lead motif");
        map.IsMotifActive(1, "Bass").Should().BeFalse("Bar 1 has no Bass motif");
        map.IsMotifActive(1, "Guitar").Should().BeFalse("Bar 1 has no Guitar motif");
        map.IsMotifActive(1, null).Should().BeTrue("Bar 1 has some motif (null = any role)");
    }

    [Fact]
    public void IsMotifActive_InvalidBarNumber_ReturnsFalse()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var plan = MotifPlacementPlan.Empty();
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        map.IsMotifActive(0).Should().BeFalse("Bar 0 is invalid");
        map.IsMotifActive(-1).Should().BeFalse("Negative bar is invalid");
    }

    [Fact]
    public void IsMotifActive_MotifInSecondSection_CorrectlyMapsAbsoluteBars()
    {
        // Arrange: Section 0 = bars 1-4, Section 1 = bars 5-8
        // Motif in section 1, bars 0-1 within section = absolute bars 5-6
        var sectionTrack = CreateTestSectionTrack();
        var motif = CreateTestMotif("Lead", "ChorusHook");
        var placement = MotifPlacement.Create(motif, absoluteSectionIndex: 1, startBarWithinSection: 0, durationBars: 2);
        var plan = MotifPlacementPlan.Create(new[] { placement }, seed: 42);
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        map.IsMotifActive(4).Should().BeFalse("Bar 4 is in section 0, no motif");
        map.IsMotifActive(5).Should().BeTrue("Bar 5 is section 1 bar 0, has motif");
        map.IsMotifActive(6).Should().BeTrue("Bar 6 is section 1 bar 1, has motif");
        map.IsMotifActive(7).Should().BeFalse("Bar 7 is section 1 bar 2, no motif");
    }

    #endregion

    #region GetMotifDensity Tests

    [Fact]
    public void GetMotifDensity_EmptyPlan_ReturnsZero()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var plan = MotifPlacementPlan.Empty();
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        map.GetMotifDensity(1).Should().Be(0.0);
        map.GetMotifDensity(5).Should().Be(0.0);
    }

    [Fact]
    public void GetMotifDensity_SingleMotif_ReturnsPointFive()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var motif = CreateTestMotif("Lead", "TestHook");
        var placement = MotifPlacement.Create(motif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var plan = MotifPlacementPlan.Create(new[] { placement }, seed: 42);
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        map.GetMotifDensity(1).Should().Be(0.5);
    }

    [Fact]
    public void GetMotifDensity_TwoMotifs_ReturnsOne()
    {
        // Arrange: Two motifs both covering bar 1
        var sectionTrack = CreateTestSectionTrack();
        var leadMotif = CreateTestMotif("Lead", "LeadHook");
        var bassMotif = CreateTestMotif("Bass", "BassRiff");
        var leadPlacement = MotifPlacement.Create(leadMotif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var bassPlacement = MotifPlacement.Create(bassMotif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var plan = MotifPlacementPlan.Create(new[] { leadPlacement, bassPlacement }, seed: 42);
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        map.GetMotifDensity(1).Should().Be(1.0); // Capped at 1.0
    }

    [Fact]
    public void GetMotifDensity_ThreeOrMoreMotifs_CappedAtOne()
    {
        // Arrange: Three motifs covering same bar
        var sectionTrack = CreateTestSectionTrack();
        var motif1 = CreateTestMotif("Lead", "Hook1");
        var motif2 = CreateTestMotif("Bass", "Hook2");
        var motif3 = CreateTestMotif("Guitar", "Hook3");
        var p1 = MotifPlacement.Create(motif1, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var p2 = MotifPlacement.Create(motif2, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var p3 = MotifPlacement.Create(motif3, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var plan = MotifPlacementPlan.Create(new[] { p1, p2, p3 }, seed: 42);
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        map.GetMotifDensity(1).Should().Be(1.0); // Capped at 1.0
    }

    [Fact]
    public void GetMotifDensity_WithRoleFilter_OnlyCountsMatchingRole()
    {
        // Arrange: Two motifs, one Lead, one Bass
        var sectionTrack = CreateTestSectionTrack();
        var leadMotif = CreateTestMotif("Lead", "LeadHook");
        var bassMotif = CreateTestMotif("Bass", "BassRiff");
        var leadPlacement = MotifPlacement.Create(leadMotif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var bassPlacement = MotifPlacement.Create(bassMotif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var plan = MotifPlacementPlan.Create(new[] { leadPlacement, bassPlacement }, seed: 42);
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        map.GetMotifDensity(1, "Lead").Should().Be(0.5); // Only Lead
        map.GetMotifDensity(1, "Bass").Should().Be(0.5); // Only Bass
        map.GetMotifDensity(1, "Guitar").Should().Be(0.0); // No Guitar
        map.GetMotifDensity(1, null).Should().Be(1.0); // All = 2 motifs
    }

    [Fact]
    public void GetMotifDensity_InvalidBarNumber_ReturnsZero()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var plan = MotifPlacementPlan.Empty();
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        map.GetMotifDensity(0).Should().Be(0.0);
        map.GetMotifDensity(-5).Should().Be(0.0);
    }

    #endregion

    #region GetActiveMotifs Tests

    [Fact]
    public void GetActiveMotifs_EmptyPlan_ReturnsEmptyList()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var plan = MotifPlacementPlan.Empty();
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act & Assert
        map.GetActiveMotifs(1).Should().BeEmpty();
    }

    [Fact]
    public void GetActiveMotifs_WithMotif_ReturnsPlacements()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var motif = CreateTestMotif("Lead", "TestHook");
        var placement = MotifPlacement.Create(motif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 2);
        var plan = MotifPlacementPlan.Create(new[] { placement }, seed: 42);
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act
        var activeMotifs = map.GetActiveMotifs(1);

        // Assert
        activeMotifs.Should().HaveCount(1);
        activeMotifs[0].MotifSpec.Name.Should().Be("TestHook");
    }

    [Fact]
    public void GetActiveMotifs_MultipleMotifs_ReturnsAll()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var motif1 = CreateTestMotif("Lead", "Hook1");
        var motif2 = CreateTestMotif("Bass", "Hook2");
        var p1 = MotifPlacement.Create(motif1, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var p2 = MotifPlacement.Create(motif2, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var plan = MotifPlacementPlan.Create(new[] { p1, p2 }, seed: 42);
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act
        var activeMotifs = map.GetActiveMotifs(1);

        // Assert
        activeMotifs.Should().HaveCount(2);
    }

    [Fact]
    public void GetActiveMotifsForRole_FiltersCorrectly()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var leadMotif = CreateTestMotif("Lead", "LeadHook");
        var bassMotif = CreateTestMotif("Bass", "BassRiff");
        var leadPlacement = MotifPlacement.Create(leadMotif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var bassPlacement = MotifPlacement.Create(bassMotif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 1);
        var plan = MotifPlacementPlan.Create(new[] { leadPlacement, bassPlacement }, seed: 42);
        var map = new MotifPresenceMap(plan, sectionTrack);

        // Act
        var leadMotifs = map.GetActiveMotifsForRole(1, "Lead");
        var bassMotifs = map.GetActiveMotifsForRole(1, "Bass");
        var guitarMotifs = map.GetActiveMotifsForRole(1, "Guitar");

        // Assert
        leadMotifs.Should().HaveCount(1);
        leadMotifs[0].MotifSpec.Name.Should().Be("LeadHook");
        bassMotifs.Should().HaveCount(1);
        bassMotifs[0].MotifSpec.Name.Should().Be("BassRiff");
        guitarMotifs.Should().BeEmpty();
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void Determinism_SameInputs_SameResults()
    {
        // Arrange
        var sectionTrack = CreateTestSectionTrack();
        var motif = CreateTestMotif("Lead", "TestHook");
        var placement = MotifPlacement.Create(motif, absoluteSectionIndex: 0, startBarWithinSection: 0, durationBars: 4);
        var plan = MotifPlacementPlan.Create(new[] { placement }, seed: 42);

        // Act
        var map1 = new MotifPresenceMap(plan, sectionTrack);
        var map2 = new MotifPresenceMap(plan, sectionTrack);

        // Assert: Same queries produce same results
        for (int bar = 1; bar <= 8; bar++)
        {
            map1.IsMotifActive(bar).Should().Be(map2.IsMotifActive(bar), $"Bar {bar} should be deterministic");
            map1.GetMotifDensity(bar).Should().Be(map2.GetMotifDensity(bar), $"Bar {bar} density should be deterministic");
            map1.GetActiveMotifs(bar).Count.Should().Be(map2.GetActiveMotifs(bar).Count, $"Bar {bar} motif count should be deterministic");
        }
    }

    #endregion

    #region Test Helpers

    private static SectionTrack CreateTestSectionTrack()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 4);   // Section 0: bars 1-4
        track.Add(MusicConstants.eSectionType.Chorus, 4);  // Section 1: bars 5-8
        track.Add(MusicConstants.eSectionType.Verse, 4);   // Section 2: bars 9-12
        track.Add(MusicConstants.eSectionType.Chorus, 4);  // Section 3: bars 13-16
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

    #endregion
}
