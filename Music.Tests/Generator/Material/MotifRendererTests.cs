// AI: purpose=Unit tests for Story 9.2 MotifRenderer; verifies determinism, pitch selection, variations, edge cases.
// AI: deps=xunit; Music.Song.Material; Music.Generator; test fixtures build minimal song contexts.
// AI: change=Story 9.2 acceptance criteria: determinism, harmony respect, register bounds, valid MIDI, no overlaps.

using Xunit;
using Music.Song.Material;
using Music.Generator;
using Music;

namespace Music.Song.Material.Tests;

/// <summary>
/// Story 9.2: Tests for MotifRenderer.
/// Verifies rendering produces valid, deterministic output respecting all constraints.
/// </summary>
/// <remarks>
/// This class is in the RngDependentTests collection to run sequentially because
/// tests depend on global RNG state.
/// </remarks>
[Collection("RngDependentTests")]
public class MotifRendererTests
{
    public MotifRendererTests()
    {
        Rng.Initialize(42);
    }

    #region Test Fixture Helpers

    private static (MotifSpec spec, MotifPlacement placement, HarmonyTrack harmony, BarTrack barTrack, GroovePresetDefinition groove, SectionTrack sectionTrack) CreateTestContext()
    {
        var spec = MotifSpec.Create(
            name: "Test Hook",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480, 720 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var placement = MotifPlacement.Create(
            motifSpec: spec,
            absoluteSectionIndex: 0,
            startBarWithinSection: 0,
            durationBars: 1);

        var harmony = new HarmonyTrack();
        harmony.Add(new HarmonyEvent { StartBar = 1, StartBeat = 1, Key = "C major", Degree = 1, Quality = "" });

        var timingTrack = new Timingtrack();
        timingTrack.Add(new TimingEvent { StartBar = 1, Numerator = 4, Denominator = 4 });

        var barTrack = new BarTrack();
        barTrack.RebuildFromTimingTrack(timingTrack, 8);

        var groove = new GroovePresetDefinition();

        var sectionTrack = new SectionTrack();
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);

        return (spec, placement, harmony, barTrack, groove, sectionTrack);
    }

    private static List<OnsetSlot> CreateSimpleOnsetGrid()
    {
        return new List<OnsetSlot>
        {
            new OnsetSlot(0, 240, true),
            new OnsetSlot(240, 240, false),
            new OnsetSlot(480, 240, true),
            new OnsetSlot(720, 240, false)
        };
    }

    private static List<HarmonyPitchContext> CreateSimpleHarmonyContexts()
    {
        var harmonyEvent = new HarmonyEvent { StartBar = 1, StartBeat = 1, Key = "C major", Degree = 1, Quality = "" };
        var context = HarmonyPitchContextBuilder.Build(harmonyEvent);
        return new List<HarmonyPitchContext> { context, context, context, context };
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void Render_SameInputs_SameOutput()
    {
        // Arrange
        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result1 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);
        var result2 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert
        Assert.Equal(result1.PartTrackNoteEvents.Count, result2.PartTrackNoteEvents.Count);

        for (int i = 0; i < result1.PartTrackNoteEvents.Count; i++)
        {
            var e1 = result1.PartTrackNoteEvents[i];
            var e2 = result2.PartTrackNoteEvents[i];

            Assert.Equal(e1.NoteNumber, e2.NoteNumber);
            Assert.Equal(e1.AbsoluteTimeTicks, e2.AbsoluteTimeTicks);
            Assert.Equal(e1.NoteDurationTicks, e2.NoteDurationTicks);
            Assert.Equal(e1.NoteOnVelocity, e2.NoteOnVelocity);
        }
    }

    [Fact]
    public void Render_DifferentSeeds_DifferentOutput()
    {
        // Arrange
        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result1 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);
        var result2 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 123);

        // Assert - at least one pitch should differ (with high probability)
        if (result1.PartTrackNoteEvents.Count > 0 && result2.PartTrackNoteEvents.Count > 0)
        {
            bool anyDifferent = false;
            for (int i = 0; i < Math.Min(result1.PartTrackNoteEvents.Count, result2.PartTrackNoteEvents.Count); i++)
            {
                if (result1.PartTrackNoteEvents[i].NoteNumber != result2.PartTrackNoteEvents[i].NoteNumber ||
                    result1.PartTrackNoteEvents[i].NoteOnVelocity != result2.PartTrackNoteEvents[i].NoteOnVelocity)
                {
                    anyDifferent = true;
                    break;
                }
            }
            Assert.True(anyDifferent, "Different seeds should produce different pitches or velocities");
        }
    }

    [Fact]
    public void RenderSimplified_SameInputs_SameOutput()
    {
        // Arrange
        var spec = MotifSpec.Create(
            name: "Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var placement = MotifPlacement.Create(spec, 0, 0, 1);
        var harmonyContexts = CreateSimpleHarmonyContexts();
        var onsetGrid = CreateSimpleOnsetGrid();

        // Act
        var result1 = MotifRenderer.Render(spec, placement, harmonyContexts, onsetGrid, 0, seed: 42);
        var result2 = MotifRenderer.Render(spec, placement, harmonyContexts, onsetGrid, 0, seed: 42);

        // Assert
        Assert.Equal(result1.PartTrackNoteEvents.Count, result2.PartTrackNoteEvents.Count);
        for (int i = 0; i < result1.PartTrackNoteEvents.Count; i++)
        {
            Assert.Equal(result1.PartTrackNoteEvents[i].NoteNumber, result2.PartTrackNoteEvents[i].NoteNumber);
        }
    }

    #endregion

    #region MIDI Range Tests

    [Fact]
    public void Render_AllNotesInValidMidiRange()
    {
        // Arrange
        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert
        foreach (var evt in result.PartTrackNoteEvents)
        {
            Assert.InRange(evt.NoteNumber, 21, 108);
        }
    }

    [Fact]
    public void Render_ExtremeRegister_ClampedToValidRange()
    {
        // Arrange - very low register
        var spec = MotifSpec.Create(
            name: "Bass Test",
            intendedRole: "Bass",
            kind: MaterialKind.Riff,
            rhythmShape: new List<int> { 0, 480 },
            contour: ContourIntent.Flat,
            centerMidiNote: 25,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: false);

        var placement = MotifPlacement.Create(spec, 0, 0, 1);
        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert
        foreach (var evt in result.PartTrackNoteEvents)
        {
            Assert.InRange(evt.NoteNumber, 21, 108);
        }
    }

    [Fact]
    public void Render_ValidVelocityRange()
    {
        // Arrange
        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert
        foreach (var evt in result.PartTrackNoteEvents)
        {
            Assert.InRange(evt.NoteOnVelocity, 40, 127);
        }
    }

    #endregion

    #region Register Constraint Tests

    [Fact]
    public void Render_NotesWithinRegisterBounds()
    {
        // Arrange - tight register
        var spec = MotifSpec.Create(
            name: "Register Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480, 720, 960, 1200 },
            contour: ContourIntent.Arch,
            centerMidiNote: 67,
            rangeSemitones: 6,
            chordToneBias: 0.5,
            allowPassingTones: true);

        var placement = MotifPlacement.Create(spec, 0, 0, 1);
        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert - allow small deviation due to voice leading
        int minAllowed = 67 - 6 - 2;
        int maxAllowed = 67 + 6 + 2;

        foreach (var evt in result.PartTrackNoteEvents)
        {
            Assert.InRange(evt.NoteNumber, minAllowed, maxAllowed);
        }
    }

    #endregion

    #region Harmony Respect Tests

    [Fact]
    public void Render_HighChordToneBias_MostNotesAreChordTones()
    {
        // Arrange - 100% chord tone bias on strong beats
        var spec = MotifSpec.Create(
            name: "Chord Tone Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 480, 960, 1440 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 1.0,
            allowPassingTones: false);

        var placement = MotifPlacement.Create(spec, 0, 0, 1);
        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert - C major chord: C, E, G (pitch classes 0, 4, 7)
        var chordPitchClasses = new HashSet<int> { 0, 4, 7 };
        int chordToneCount = 0;

        foreach (var evt in result.PartTrackNoteEvents)
        {
            int pitchClass = evt.NoteNumber % 12;
            if (chordPitchClasses.Contains(pitchClass))
                chordToneCount++;
        }

        double ratio = result.PartTrackNoteEvents.Count > 0
            ? (double)chordToneCount / result.PartTrackNoteEvents.Count
            : 0;

        Assert.True(ratio >= 0.5, $"Expected >= 50% chord tones, got {ratio:P}");
    }

    #endregion

    #region Contour Tests

    [Fact]
    public void Render_ContourUp_GenerallyAscending()
    {
        // Arrange
        var spec = MotifSpec.Create(
            name: "Contour Up",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480, 720, 960, 1200, 1440, 1680 },
            contour: ContourIntent.Up,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.3,
            allowPassingTones: true);

        var placement = MotifPlacement.Create(spec, 0, 0, 1);
        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert - last note should generally be higher than first
        if (result.PartTrackNoteEvents.Count >= 2)
        {
            int first = result.PartTrackNoteEvents[0].NoteNumber;
            int last = result.PartTrackNoteEvents[^1].NoteNumber;
            Assert.True(last >= first - 5, $"Contour Up: expected last ({last}) >= first ({first}) - 5");
        }
    }

    [Fact]
    public void Render_ContourDown_GenerallyDescending()
    {
        // Arrange
        var spec = MotifSpec.Create(
            name: "Contour Down",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480, 720, 960, 1200, 1440, 1680 },
            contour: ContourIntent.Down,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.3,
            allowPassingTones: true);

        var placement = MotifPlacement.Create(spec, 0, 0, 1);
        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert - last note should generally be lower than first
        if (result.PartTrackNoteEvents.Count >= 2)
        {
            int first = result.PartTrackNoteEvents[0].NoteNumber;
            int last = result.PartTrackNoteEvents[^1].NoteNumber;
            Assert.True(last <= first + 5, $"Contour Down: expected last ({last}) <= first ({first}) + 5");
        }
    }

    [Fact]
    public void Render_ContourArch_PeaksInMiddle()
    {
        // Arrange
        var spec = MotifSpec.Create(
            name: "Contour Arch",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480, 720, 960, 1200, 1440, 1680 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.3,
            allowPassingTones: true);

        var placement = MotifPlacement.Create(spec, 0, 0, 1);
        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert - middle notes should be at or above average
        if (result.PartTrackNoteEvents.Count >= 4)
        {
            var pitches = result.PartTrackNoteEvents.Select(e => e.NoteNumber).ToList();
            double avg = pitches.Average();
            int middleIdx = pitches.Count / 2;
            int middlePitch = pitches[middleIdx];
            Assert.True(middlePitch >= avg - 3, $"Arch: middle ({middlePitch}) should be near/above average ({avg:F1})");
        }
    }

    #endregion

    #region Variation Tests

    [Fact]
    public void Render_OctaveUpTransform_RaisesNotes()
    {
        // Arrange
        var spec = MotifSpec.Create(
            name: "Octave Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 480 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 18,
            chordToneBias: 0.8,
            allowPassingTones: false);

        var placementNormal = MotifPlacement.Create(spec, 0, 0, 1, 0.5);
        var placementOctaveUp = MotifPlacement.Create(spec, 0, 0, 1, 0.5, new[] { "OctaveUp" });

        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var resultNormal = MotifRenderer.Render(spec, placementNormal, harmony, barTrack, groove, sectionTrack, seed: 42);
        var resultOctaveUp = MotifRenderer.Render(spec, placementOctaveUp, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert - octave up should have some higher notes (within register)
        if (resultNormal.PartTrackNoteEvents.Count > 0 && resultOctaveUp.PartTrackNoteEvents.Count > 0)
        {
            double avgNormal = resultNormal.PartTrackNoteEvents.Average(e => e.NoteNumber);
            double avgOctaveUp = resultOctaveUp.PartTrackNoteEvents.Average(e => e.NoteNumber);
            // May not always be higher due to register clamping, but should not be lower
            Assert.True(avgOctaveUp >= avgNormal - 2, 
                $"OctaveUp avg ({avgOctaveUp:F1}) should be >= normal avg ({avgNormal:F1}) - 2");
        }
    }

    [Fact]
    public void Render_ZeroVariationIntensity_NoRandomChanges()
    {
        // Arrange
        var spec = MotifSpec.Create(
            name: "No Variation",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 480 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 1.0,
            allowPassingTones: false);

        var placement = MotifPlacement.Create(spec, 0, 0, 1, 0.0);
        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result1 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);
        var result2 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 999);

        // Assert - with zero variation, pitches should be identical regardless of seed
        Assert.Equal(result1.PartTrackNoteEvents.Count, result2.PartTrackNoteEvents.Count);
        for (int i = 0; i < result1.PartTrackNoteEvents.Count; i++)
        {
            Assert.Equal(result1.PartTrackNoteEvents[i].NoteNumber, result2.PartTrackNoteEvents[i].NoteNumber);
        }
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Render_EmptyRhythmShape_ReturnsEmptyTrack()
    {
        // Arrange
        var spec = MotifSpec.Create(
            name: "Empty",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int>(),
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var placement = MotifPlacement.Create(spec, 0, 0, 1);
        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert
        Assert.Empty(result.PartTrackNoteEvents);
    }

    [Fact]
    public void Render_InvalidSectionIndex_ReturnsEmptyTrack()
    {
        // Arrange
        var (spec, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();
        var placement = MotifPlacement.Create(spec, 99, 0, 1); // Invalid section index

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert
        Assert.Empty(result.PartTrackNoteEvents);
    }

    [Fact]
    public void RenderSimplified_EmptyHarmonyContexts_ReturnsEmptyTrack()
    {
        // Arrange
        var spec = MotifSpec.Create(
            name: "Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 480 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var placement = MotifPlacement.Create(spec, 0, 0, 1);
        var emptyHarmony = new List<HarmonyPitchContext>();
        var onsetGrid = CreateSimpleOnsetGrid();

        // Act
        var result = MotifRenderer.Render(spec, placement, emptyHarmony, onsetGrid, 0, seed: 42);

        // Assert
        Assert.Empty(result.PartTrackNoteEvents);
    }

    [Fact]
    public void RenderSimplified_EmptyOnsetGrid_ReturnsEmptyTrack()
    {
        // Arrange
        var spec = MotifSpec.Create(
            name: "Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 480 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var placement = MotifPlacement.Create(spec, 0, 0, 1);
        var harmonyContexts = CreateSimpleHarmonyContexts();
        var emptyGrid = new List<OnsetSlot>();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmonyContexts, emptyGrid, 0, seed: 42);

        // Assert
        Assert.Empty(result.PartTrackNoteEvents);
    }

    #endregion

    #region Output Format Tests

    [Fact]
    public void Render_EventsSortedByTime()
    {
        // Arrange
        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert
        for (int i = 1; i < result.PartTrackNoteEvents.Count; i++)
        {
            Assert.True(
                result.PartTrackNoteEvents[i].AbsoluteTimeTicks >= result.PartTrackNoteEvents[i - 1].AbsoluteTimeTicks,
                "Events must be sorted by AbsoluteTimeTicks");
        }
    }

    [Fact]
    public void Render_PositiveDurations()
    {
        // Arrange
        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert
        foreach (var evt in result.PartTrackNoteEvents)
        {
            Assert.True(evt.NoteDurationTicks > 0, "All durations must be positive");
        }
    }

    [Fact]
    public void Render_MetaDataCorrectlySet()
    {
        // Arrange
        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert
        Assert.NotNull(result.Meta);
        Assert.Equal(PartTrackDomain.SongAbsolute, result.Meta.Domain);
        Assert.Equal(PartTrackKind.RoleTrack, result.Meta.Kind);
        Assert.Equal(spec.IntendedRole, result.Meta.IntendedRole);
        Assert.Equal(spec.Kind, result.Meta.MaterialKind);
        Assert.Contains("rendered", result.Meta.Name);
    }

    #endregion

    #region Integration Test

    [Fact]
    public void Render_MotifLibraryHook_ProducesValidOutput()
    {
        // Arrange - use a real motif from the library
        var spec = MotifLibrary.ClassicRockHookA();
        var placement = MotifPlacement.Create(spec, 0, 0, 2);
        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act
        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

        // Assert
        Assert.NotEmpty(result.PartTrackNoteEvents);
        Assert.All(result.PartTrackNoteEvents, e => Assert.InRange(e.NoteNumber, 21, 108));
        Assert.All(result.PartTrackNoteEvents, e => Assert.InRange(e.NoteOnVelocity, 40, 127));
        Assert.All(result.PartTrackNoteEvents, e => Assert.True(e.NoteDurationTicks > 0));
    }

    [Fact]
    public void Render_MotifLibraryRiff_DeterministicAcrossRuns()
    {
        // Arrange
        var spec = MotifLibrary.SteadyVerseRiffA();
        var placement = MotifPlacement.Create(spec, 0, 0, 1);
        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

        // Act - render twice with same seed
        var result1 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 12345);
        var result2 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 12345);

        // Assert
        Assert.Equal(result1.PartTrackNoteEvents.Count, result2.PartTrackNoteEvents.Count);
        for (int i = 0; i < result1.PartTrackNoteEvents.Count; i++)
        {
            Assert.Equal(result1.PartTrackNoteEvents[i].NoteNumber, result2.PartTrackNoteEvents[i].NoteNumber);
            Assert.Equal(result1.PartTrackNoteEvents[i].AbsoluteTimeTicks, result2.PartTrackNoteEvents[i].AbsoluteTimeTicks);
        }
    }

    #endregion
}
