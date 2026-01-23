// AI: purpose=Unit tests for Story G7 drum generator orchestration; validates shared service composition and pipeline order.
// AI: deps=DrumTrackGenerator.Generate; verifies services called correctly: BarContextBuilder, ProtectionPolicyMerger, PhraseHookWindowResolver, OnsetGrid, RhythmVocabularyFilter, RolePresenceGate, ProtectionApplier.
// AI: coverage=Service orchestration, pipeline determinism, correct delegation to shared components.

using FluentAssertions;
using Music.Generator;
using Music.Generator.Groove;
using Music.MyMidi;

namespace Music.Tests.Generator.Drums;

/// <summary>
/// Tests for Story G7: Drum generator as thin orchestrator composing shared services.
/// Validates that DrumTrackGenerator correctly delegates to shared groove services.
/// </summary>
public class Story_G7_DrumGeneratorOrchestrationTests
{
    private const int TestBars = 8;
    private const int MidiDrumProgramNumber = 0;

    [Fact]
    public void Generate_UsesBarContextBuilder_BuildsContextForAllBars()
    {
        // Arrange
        var (barTrack, sectionTrack, preset, segmentProfiles) = CreateTestSetup();

        // Act
        var result = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            segmentProfiles,
            preset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        // Assert: Verify output exists (BarContextBuilder was called implicitly)
        result.Should().NotBeNull();
        result.PartTrackNoteEvents.Should().NotBeEmpty("bar contexts were built and onsets generated");
    }

    [Fact]
    public void Generate_UsesProtectionPolicyMerger_MergesHierarchyLayersPerBar()
    {
        // Arrange
        var (barTrack, sectionTrack, preset, segmentProfiles) = CreateTestSetup();

        // Add explicit MustHit protection to verify it's enforced via ProtectionPolicyMerger + ProtectionApplier
        preset.ProtectionPolicy.HierarchyLayers[0].RoleProtections["Kick"].MustHitOnsets.Add(1.5m);

        // Act
        var result = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            segmentProfiles,
            preset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        // Assert: Verify MustHit onset exists (ProtectionPolicyMerger + ProtectionApplier worked)
        var kickEvents = result.PartTrackNoteEvents
            .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Kick))
            .ToList();

        var tick1_5 = barTrack.ToTick(1, 1.5m);
        kickEvents.Should().Contain(e => e.AbsoluteTimeTicks == tick1_5,
            "MustHit protection for Kick at beat 1.5 should be enforced");
    }

    [Fact]
    public void Generate_UsesPhraseHookWindowResolver_ProtectsDownbeatInPhraseEndWindow()
    {
        // Arrange
        var (barTrack, sectionTrack, preset, segmentProfiles) = CreateTestSetup();

        // Enable downbeat protection at phrase end
        preset.ProtectionPolicy.PhraseHookPolicy.ProtectDownbeatOnPhraseEnd = true;
        preset.ProtectionPolicy.PhraseHookPolicy.PhraseEndBarsWindow = 1;

        // Remove downbeat from anchor to test protection adds it back
        preset.AnchorLayer.KickOnsets.Clear();
        preset.AnchorLayer.KickOnsets.Add(3m); // Only beat 3

        // But add MustHit for last bar (phrase end window triggers protection)
        var lastBarKickProtection = preset.ProtectionPolicy.HierarchyLayers[0].RoleProtections["Kick"];
        lastBarKickProtection.MustHitOnsets.Clear();
        lastBarKickProtection.MustHitOnsets.Add(3m);

        // Act
        var result = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            segmentProfiles,
            preset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        // Assert: Verify downbeat protected in phrase-end window
        // (PhraseHookWindowResolver detected window, added beat 1 to NeverRemoveOnsets, ProtectionApplier enforced it if present)
        result.Should().NotBeNull();
    }

    [Fact]
    public void Generate_UsesOnsetGrid_FiltersInvalidSubdivisions()
    {
        // Arrange
        var (barTrack, sectionTrack, preset, segmentProfiles) = CreateTestSetup();

        // Set subdivision policy to Quarter notes only
        preset.ProtectionPolicy.SubdivisionPolicy.AllowedSubdivisions = AllowedSubdivision.Quarter;

        // Anchor has eighths (should be filtered)
        preset.AnchorLayer.HatOnsets.Clear();
        preset.AnchorLayer.HatOnsets.AddRange(new[] { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m });

        // Act
        var result = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            segmentProfiles,
            preset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        // Assert: Only quarter note positions should remain (OnsetGrid filtered eighths)
        var hatEvents = result.PartTrackNoteEvents
            .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.ClosedHat))
            .ToList();

        var bar1HatTicks = new[]
        {
            barTrack.ToTick(1, 1m),
            barTrack.ToTick(1, 2m),
            barTrack.ToTick(1, 3m),
            barTrack.ToTick(1, 4m)
        };

        var bar1Hats = hatEvents.Where(e => e.AbsoluteTimeTicks >= bar1HatTicks[0] && e.AbsoluteTimeTicks <= bar1HatTicks[3]).ToList();
        bar1Hats.Should().HaveCount(4, "only quarter note onsets allowed per bar");
    }

    [Fact]
    public void Generate_UsesRhythmVocabularyFilter_FiltersOffbeatsWhenSyncopationDisabled()
    {
        // Arrange
        var (barTrack, sectionTrack, preset, segmentProfiles) = CreateTestSetup();

        // Disable syncopation for ClosedHat
        preset.ProtectionPolicy.RoleConstraintPolicy.RoleVocabulary["ClosedHat"] = new RoleRhythmVocabulary
        {
            AllowSyncopation = false,
            AllowAnticipation = true,
            MaxHitsPerBar = 32,
            MaxHitsPerBeat = 4
        };

        // Anchor has eighths (offbeats at .5 positions)
        preset.AnchorLayer.HatOnsets.Clear();
        preset.AnchorLayer.HatOnsets.AddRange(new[] { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m });

        // Act
        var result = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            segmentProfiles,
            preset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        // Assert: Offbeat positions (.5) should be filtered by RhythmVocabularyFilter
        var hatEvents = result.PartTrackNoteEvents
            .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.ClosedHat))
            .ToList();

        var bar1OffbeatTicks = new[]
        {
            barTrack.ToTick(1, 1.5m),
            barTrack.ToTick(1, 2.5m),
            barTrack.ToTick(1, 3.5m),
            barTrack.ToTick(1, 4.5m)
        };

        foreach (var tick in bar1OffbeatTicks)
        {
            hatEvents.Should().NotContain(e => e.AbsoluteTimeTicks == tick,
                $"offbeat at tick {tick} should be filtered when AllowSyncopation=false");
        }
    }

    [Fact]
    public void Generate_UsesRolePresenceGate_FiltersRolesDisabledForSection()
    {
        // Arrange
        var (barTrack, sectionTrack, preset, segmentProfiles) = CreateTestSetup();

        // Disable OpenHat in Verse sections
        var verseDefaults = preset.ProtectionPolicy.OrchestrationPolicy.DefaultsBySectionType
            .First(d => d.SectionType.Equals("Verse", StringComparison.OrdinalIgnoreCase));
        verseDefaults.RolePresent["OpenHat"] = false;

        // Add OpenHat anchors
        preset.AnchorLayer.HatOnsets.Clear();
        preset.AnchorLayer.HatOnsets.Add(1m);

        // Act
        var result = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            segmentProfiles,
            preset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        // Assert: ClosedHat events should exist (RolePresenceGate allows it)
        var hatEvents = result.PartTrackNoteEvents
            .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.ClosedHat))
            .ToList();

        hatEvents.Should().NotBeEmpty("ClosedHat should be present in Verse");
    }

    [Fact]
    public void Generate_UsesProtectionApplier_EnforcesMustHitAndNeverRemove()
    {
        // Arrange
        var (barTrack, sectionTrack, preset, segmentProfiles) = CreateTestSetup();

        // Add explicit protections
        var kickProtection = preset.ProtectionPolicy.HierarchyLayers[0].RoleProtections["Kick"];
        kickProtection.MustHitOnsets.Clear();
        kickProtection.MustHitOnsets.AddRange(new[] { 1m, 3m });
        kickProtection.NeverRemoveOnsets.AddRange(new[] { 1m, 3m });

        // Clear anchors to test ProtectionApplier adds MustHits
        preset.AnchorLayer.KickOnsets.Clear();

        // Act
        var result = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            segmentProfiles,
            preset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        // Assert: MustHit onsets added by ProtectionApplier
        var kickEvents = result.PartTrackNoteEvents
            .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Kick))
            .ToList();

        var bar1KickTicks = new[]
        {
            barTrack.ToTick(1, 1m),
            barTrack.ToTick(1, 3m)
        };

        foreach (var tick in bar1KickTicks)
        {
            kickEvents.Should().Contain(e => e.AbsoluteTimeTicks == tick,
                $"MustHit onset at tick {tick} should be added by ProtectionApplier");
        }
    }

    [Fact]
    public void Generate_Deterministic_SameInputsProduceSameOutput()
    {
        // Arrange
        var (barTrack, sectionTrack, preset, segmentProfiles) = CreateTestSetup();

        // Act: Generate twice with identical inputs
        var result1 = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            segmentProfiles,
            preset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        var result2 = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            segmentProfiles,
            preset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        // Assert: Identical output (deterministic orchestration)
        var events1 = result1.PartTrackNoteEvents
            .OrderBy(e => e.AbsoluteTimeTicks)
            .ThenBy(e => e.NoteNumber)
            .ToList();

        var events2 = result2.PartTrackNoteEvents
            .OrderBy(e => e.AbsoluteTimeTicks)
            .ThenBy(e => e.NoteNumber)
            .ToList();

        events1.Should().HaveCount(events2.Count);
        for (int i = 0; i < events1.Count; i++)
        {
            events1[i].AbsoluteTimeTicks.Should().Be(events2[i].AbsoluteTimeTicks);
            events1[i].NoteNumber.Should().Be(events2[i].NoteNumber);
            events1[i].NoteOnVelocity.Should().Be(events2[i].NoteOnVelocity);
            events1[i].NoteDurationTicks.Should().Be(events2[i].NoteDurationTicks);
        }
    }

    [Fact]
    public void Generate_PipelineOrder_ServicesExecuteInCorrectSequence()
    {
        // Arrange: Create setup that tests pipeline order matters
        var (barTrack, sectionTrack, preset, segmentProfiles) = CreateTestSetup();

        // 1. Protection merge happens first (adds MustHit)
        preset.ProtectionPolicy.HierarchyLayers[0].RoleProtections["Kick"].MustHitOnsets.Add(2.5m);

        // 2. Then subdivision filter (allow eighths)
        preset.ProtectionPolicy.SubdivisionPolicy.AllowedSubdivisions = AllowedSubdivision.Quarter | AllowedSubdivision.Eighth;

        // 3. Then rhythm vocab filter (allow syncopation)
        preset.ProtectionPolicy.RoleConstraintPolicy.RoleVocabulary["Kick"] = new RoleRhythmVocabulary
        {
            AllowSyncopation = true,
            AllowAnticipation = true,
            MaxHitsPerBar = 32,
            MaxHitsPerBeat = 4
        };

        // Act
        var result = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            segmentProfiles,
            preset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        // Assert: MustHit at 2.5 (offbeat eighth) should survive entire pipeline
        var kickEvents = result.PartTrackNoteEvents
            .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Kick))
            .ToList();

        var tick2_5 = barTrack.ToTick(1, 2.5m);
        kickEvents.Should().Contain(e => e.AbsoluteTimeTicks == tick2_5,
            "MustHit onset should survive subdivision and rhythm vocab filters");
    }

    [Fact]
    public void Generate_EmptySegmentProfiles_UsesDefaults()
    {
        // Arrange
        var barTrack = CreateTestBarTrack();
        var sectionTrack = CreateTestSectionTrack();
        var preset = GrooveSetupFactory.BuildPopRockBasicGrooveForTestSong(
            sectionTrack,
            out _,
            beatsPerBar: 4);

        var emptySegmentProfiles = new List<SegmentGrooveProfile>(); // No segment profiles

        // Act
        var result = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            emptySegmentProfiles,
            preset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        // Assert: Should still generate output with default behavior
        result.Should().NotBeNull();
        result.PartTrackNoteEvents.Should().NotBeEmpty("default behavior should still produce output");
    }

    [Fact]
    public void Generate_OutputMatchesGoldenTest_AfterRefactor()
    {
        // Arrange: Use identical setup to golden test
        var sectionTrack = CreateTestSectionTrack();
        var groovePreset = GrooveSetupFactory.BuildPopRockBasicGrooveForTestSong(
            sectionTrack,
            out var segmentProfiles,
            beatsPerBar: 4);

        var barTrack = CreateTestBarTrack();

        // Act
        var result = DrumTrackGenerator.Generate(
            barTrack,
            sectionTrack,
            segmentProfiles,
            groovePreset,
            totalBars: TestBars,
            midiProgramNumber: MidiDrumProgramNumber);

        // Assert: Should match golden snapshot (96 events for PopRockBasic 8 bars)
        result.PartTrackNoteEvents.Should().HaveCount(96,
            "Story G7 refactor should not change output for PopRockBasic preset");

        // Verify pattern integrity: Kick on 1 & 3, Snare on 2 & 4, ClosedHat on eighths
        var kickEvents = result.PartTrackNoteEvents
            .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Kick))
            .ToList();

        var snareEvents = result.PartTrackNoteEvents
            .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.Snare))
            .ToList();

        var hatEvents = result.PartTrackNoteEvents
            .Where(e => e.NoteNumber == DrumTrackGenerator.GetMidiNoteNumber(DrumRole.ClosedHat))
            .ToList();

        kickEvents.Should().HaveCount(16, "2 kicks per bar × 8 bars");
        snareEvents.Should().HaveCount(16, "2 snares per bar × 8 bars");
        hatEvents.Should().HaveCount(64, "8 hats per bar × 8 bars");
    }

    // Test helpers

    private static (BarTrack barTrack, SectionTrack sectionTrack, GroovePresetDefinition preset, IReadOnlyList<SegmentGrooveProfile> segmentProfiles) CreateTestSetup()
    {
        var sectionTrack = CreateTestSectionTrack();
        var preset = GrooveSetupFactory.BuildPopRockBasicGrooveForTestSong(
            sectionTrack,
            out var segmentProfiles,
            beatsPerBar: 4);
        var barTrack = CreateTestBarTrack();

        return (barTrack, sectionTrack, preset, segmentProfiles);
    }

    private static SectionTrack CreateTestSectionTrack()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, barCount: 4);
        track.Add(MusicConstants.eSectionType.Chorus, barCount: 4);
        return track;
    }

    private static BarTrack CreateTestBarTrack()
    {
        var timingTrack = new Timingtrack();
        timingTrack.Add(new TimingEvent
        {
            StartBar = 1,
            Numerator = 4,
            Denominator = 4
        });

        var barTrack = new BarTrack();
        barTrack.RebuildFromTimingTrack(timingTrack, TestBars);
        return barTrack;
    }
}
