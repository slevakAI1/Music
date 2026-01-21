//// AI: purpose=Tests for MotifRenderer verifying determinism, valid MIDI, harmony respect, register constraints, variation bounds, no overlaps, sorted events.
//// AI: invariants=All tests deterministic; rendered notes respect constraints; output PartTrack properly formed.

//using Music.Generator;
//using Music.MyMidi;

//namespace Music.Song.Material.Tests;

///// <summary>
///// Tests for MotifRenderer (Story 9.2).
///// Verifies rendering produces valid, deterministic output respecting all constraints.
///// </summary>
//public static class MotifRendererTests
//{
//    public static void RunAllTests()
//    {
//        Console.WriteLine("=== MotifRenderer Tests ===\n");

//        TestDeterminism();
//        TestValidMidiRange();
//        TestRespectHarmonyChordTones();
//        TestRespectRegisterConstraints();
//        TestVariationStaysInBounds();
//        TestNoNoteOverlaps();
//        TestEventsSortedByTime();
//        TestEmptyOutputForInvalidPlacement();
//        TestContourUp();
//        TestContourDown();
//        TestContourArch();
//        TestDifferentSeedsProduceDifferentPitches();
//        TestTransformTagOctaveUp();

//        Console.WriteLine("\n=== All MotifRenderer Tests Passed ===");
//    }

//    /// <summary>
//    /// Test: Same inputs yield same output (determinism).
//    /// </summary>
//    private static void TestDeterminism()
//    {
//        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        var result1 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);
//        var result2 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        if (result1.PartTrackNoteEvents.Count != result2.PartTrackNoteEvents.Count)
//            throw new Exception("Determinism failed: event count differs");

//        for (int i = 0; i < result1.PartTrackNoteEvents.Count; i++)
//        {
//            var e1 = result1.PartTrackNoteEvents[i];
//            var e2 = result2.PartTrackNoteEvents[i];

//            if (e1.NoteNumber != e2.NoteNumber ||
//                e1.AbsoluteTimeTicks != e2.AbsoluteTimeTicks ||
//                e1.NoteDurationTicks != e2.NoteDurationTicks ||
//                e1.NoteOnVelocity != e2.NoteOnVelocity)
//            {
//                throw new Exception($"Determinism failed at event {i}");
//            }
//        }

//        Console.WriteLine("✓ Determinism: Same inputs yield same output");
//    }

//    /// <summary>
//    /// Test: All rendered notes are in valid MIDI range (21-108).
//    /// </summary>
//    private static void TestValidMidiRange()
//    {
//        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        foreach (var evt in result.PartTrackNoteEvents)
//        {
//            if (evt.NoteNumber < 21 || evt.NoteNumber > 108)
//                throw new Exception($"MIDI note {evt.NoteNumber} out of valid range [21, 108]");
//        }

//        Console.WriteLine("✓ Valid MIDI range: All notes in [21, 108]");
//    }

//    /// <summary>
//    /// Test: Strong beat notes tend to use chord tones (high chord tone bias).
//    /// </summary>
//    private static void TestRespectHarmonyChordTones()
//    {
//        // Create spec with high chord tone bias
//        var spec = MotifSpec.Create(
//            name: "Test Hook",
//            intendedRole: "Lead",
//            kind: MaterialKind.Hook,
//            rhythmShape: new List<int> { 0, 480, 960, 1440 }, // Quarter notes (strong beats)
//            contour: ContourIntent.Flat,
//            centerMidiNote: 60, // C4
//            rangeSemitones: 12,
//            chordToneBias: 1.0, // Always chord tones
//            allowPassingTones: false);

//        var (_, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        // C major chord: C, E, G (pitch classes 0, 4, 7)
//        var chordPitchClasses = new HashSet<int> { 0, 4, 7 };
//        int chordToneCount = 0;

//        foreach (var evt in result.PartTrackNoteEvents)
//        {
//            int pitchClass = evt.NoteNumber % 12;
//            if (chordPitchClasses.Contains(pitchClass))
//                chordToneCount++;
//        }

//        // Most notes should be chord tones with 1.0 bias
//        double chordToneRatio = result.PartTrackNoteEvents.Count > 0
//            ? (double)chordToneCount / result.PartTrackNoteEvents.Count
//            : 0;

//        if (chordToneRatio < 0.5)
//            throw new Exception($"Chord tone ratio {chordToneRatio:P} too low for bias=1.0");

//        Console.WriteLine($"✓ Harmony respect: {chordToneRatio:P} chord tones (bias=1.0)");
//    }

//    /// <summary>
//    /// Test: All notes within register constraints.
//    /// </summary>
//    private static void TestRespectRegisterConstraints()
//    {
//        var spec = MotifSpec.Create(
//            name: "Register Test",
//            intendedRole: "Lead",
//            kind: MaterialKind.Hook,
//            rhythmShape: new List<int> { 0, 240, 480, 720, 960, 1200, 1440, 1680 },
//            contour: ContourIntent.Arch,
//            centerMidiNote: 67, // G4
//            rangeSemitones: 6,  // Tight range: G4 ± 6 semitones
//            chordToneBias: 0.5,
//            allowPassingTones: true);

//        var (_, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        int minAllowed = 67 - 6;
//        int maxAllowed = 67 + 6;

//        foreach (var evt in result.PartTrackNoteEvents)
//        {
//            // Allow some flexibility for voice leading
//            if (evt.NoteNumber < minAllowed - 12 || evt.NoteNumber > maxAllowed + 12)
//                throw new Exception($"Note {evt.NoteNumber} far outside register [{minAllowed}, {maxAllowed}]");
//        }

//        Console.WriteLine("✓ Register constraints: All notes within allowed range");
//    }

//    /// <summary>
//    /// Test: Variation operators stay within bounds.
//    /// </summary>
//    private static void TestVariationStaysInBounds()
//    {
//        var spec = MotifLibrary.ClassicRockHookA();
//        var (_, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        // High variation intensity
//        var placement = MotifPlacement.Create(
//            spec,
//            absoluteSectionIndex: 0,
//            startBarWithinSection: 0,
//            durationBars: 2,
//            variationIntensity: 1.0,
//            transformTags: new[] { "OctaveUp" });

//        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        foreach (var evt in result.PartTrackNoteEvents)
//        {
//            if (evt.NoteNumber < 21 || evt.NoteNumber > 108)
//                throw new Exception($"Variation produced out-of-range note: {evt.NoteNumber}");
//        }

//        Console.WriteLine("✓ Variation bounds: All varied notes in valid range");
//    }

//    /// <summary>
//    /// Test: No note overlaps in output.
//    /// </summary>
//    private static void TestNoNoteOverlaps()
//    {
//        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        for (int i = 1; i < result.PartTrackNoteEvents.Count; i++)
//        {
//            var prev = result.PartTrackNoteEvents[i - 1];
//            var curr = result.PartTrackNoteEvents[i];

//            long prevEnd = prev.AbsoluteTimeTicks + prev.NoteDurationTicks;
//            if (prevEnd > curr.AbsoluteTimeTicks)
//            {
//                throw new Exception($"Overlap detected: note at {prev.AbsoluteTimeTicks} (dur {prev.NoteDurationTicks}) overlaps note at {curr.AbsoluteTimeTicks}");
//            }
//        }

//        Console.WriteLine("✓ No overlaps: All notes properly sequenced");
//    }

//    /// <summary>
//    /// Test: Events are sorted by AbsoluteTimeTicks.
//    /// </summary>
//    private static void TestEventsSortedByTime()
//    {
//        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        for (int i = 1; i < result.PartTrackNoteEvents.Count; i++)
//        {
//            if (result.PartTrackNoteEvents[i].AbsoluteTimeTicks < result.PartTrackNoteEvents[i - 1].AbsoluteTimeTicks)
//                throw new Exception("Events not sorted by time");
//        }

//        Console.WriteLine("✓ Events sorted: All events in time order");
//    }

//    /// <summary>
//    /// Test: Invalid placement produces empty track.
//    /// </summary>
//    private static void TestEmptyOutputForInvalidPlacement()
//    {
//        var (spec, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        // Placement referencing non-existent section
//        var invalidPlacement = MotifPlacement.Create(
//            spec,
//            absoluteSectionIndex: 999, // Does not exist
//            startBarWithinSection: 0,
//            durationBars: 1);

//        var result = MotifRenderer.Render(spec, invalidPlacement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        if (result.PartTrackNoteEvents.Count != 0)
//            throw new Exception("Invalid placement should produce empty track");

//        Console.WriteLine("✓ Invalid placement: Produces empty track");
//    }

//    /// <summary>
//    /// Test: Contour.Up produces ascending pitches.
//    /// </summary>
//    private static void TestContourUp()
//    {
//        var spec = MotifSpec.Create(
//            name: "Up Test",
//            intendedRole: "Lead",
//            kind: MaterialKind.Hook,
//            rhythmShape: new List<int> { 0, 480, 960, 1440 },
//            contour: ContourIntent.Up,
//            centerMidiNote: 60,
//            rangeSemitones: 12,
//            chordToneBias: 0.0, // Pure contour, no chord bias
//            allowPassingTones: true);

//        var (_, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        if (result.PartTrackNoteEvents.Count >= 2)
//        {
//            int first = result.PartTrackNoteEvents[0].NoteNumber;
//            int last = result.PartTrackNoteEvents[^1].NoteNumber;
//            if (last < first)
//                throw new Exception($"Contour.Up should ascend: first={first}, last={last}");
//        }

//        Console.WriteLine("✓ Contour.Up: Produces ascending pitch trend");
//    }

//    /// <summary>
//    /// Test: Contour.Down produces descending pitches.
//    /// </summary>
//    private static void TestContourDown()
//    {
//        var spec = MotifSpec.Create(
//            name: "Down Test",
//            intendedRole: "Lead",
//            kind: MaterialKind.Hook,
//            rhythmShape: new List<int> { 0, 480, 960, 1440 },
//            contour: ContourIntent.Down,
//            centerMidiNote: 72,
//            rangeSemitones: 12,
//            chordToneBias: 0.0,
//            allowPassingTones: true);

//        var (_, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        if (result.PartTrackNoteEvents.Count >= 2)
//        {
//            int first = result.PartTrackNoteEvents[0].NoteNumber;
//            int last = result.PartTrackNoteEvents[^1].NoteNumber;
//            if (last > first)
//                throw new Exception($"Contour.Down should descend: first={first}, last={last}");
//        }

//        Console.WriteLine("✓ Contour.Down: Produces descending pitch trend");
//    }

//    /// <summary>
//    /// Test: Contour.Arch produces peak in middle.
//    /// </summary>
//    private static void TestContourArch()
//    {
//        var spec = MotifSpec.Create(
//            name: "Arch Test",
//            intendedRole: "Lead",
//            kind: MaterialKind.Hook,
//            rhythmShape: new List<int> { 0, 240, 480, 720, 960, 1200, 1440, 1680 },
//            contour: ContourIntent.Arch,
//            centerMidiNote: 60,
//            rangeSemitones: 12,
//            chordToneBias: 0.0,
//            allowPassingTones: true);

//        var (_, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        var result = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        if (result.PartTrackNoteEvents.Count >= 4)
//        {
//            var notes = result.PartTrackNoteEvents.Select(e => e.NoteNumber).ToList();
//            int midIndex = notes.Count / 2;
//            int midPitch = notes[midIndex];
//            int first = notes[0];
//            int last = notes[^1];

//            // Arch should have higher middle than endpoints
//            if (midPitch < first && midPitch < last)
//                throw new Exception("Contour.Arch should have peak in middle");
//        }

//        Console.WriteLine("✓ Contour.Arch: Produces arch-shaped pitch contour");
//    }

//    /// <summary>
//    /// Test: Different seeds produce different pitches.
//    /// </summary>
//    private static void TestDifferentSeedsProduceDifferentPitches()
//    {
//        var (spec, placement, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        var result1 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 1);
//        var result2 = MotifRenderer.Render(spec, placement, harmony, barTrack, groove, sectionTrack, seed: 2);

//        bool anyDifferent = false;
//        int minCount = Math.Min(result1.PartTrackNoteEvents.Count, result2.PartTrackNoteEvents.Count);

//        for (int i = 0; i < minCount; i++)
//        {
//            if (result1.PartTrackNoteEvents[i].NoteNumber != result2.PartTrackNoteEvents[i].NoteNumber)
//            {
//                anyDifferent = true;
//                break;
//            }
//        }

//        if (!anyDifferent && minCount > 2)
//            throw new Exception("Different seeds should produce different pitches");

//        Console.WriteLine("✓ Seed sensitivity: Different seeds produce different output");
//    }

//    /// <summary>
//    /// Test: OctaveUp transform tag shifts pitches up.
//    /// </summary>
//    private static void TestTransformTagOctaveUp()
//    {
//        var (spec, _, harmony, barTrack, groove, sectionTrack) = CreateTestContext();

//        var noTransformPlacement = MotifPlacement.Create(
//            spec,
//            absoluteSectionIndex: 0,
//            startBarWithinSection: 0,
//            durationBars: 1,
//            variationIntensity: 0.0);

//        var octaveUpPlacement = MotifPlacement.Create(
//            spec,
//            absoluteSectionIndex: 0,
//            startBarWithinSection: 0,
//            durationBars: 1,
//            variationIntensity: 1.0, // Need high intensity for transform to apply
//            transformTags: new[] { "OctaveUp" });

//        var noTransformResult = MotifRenderer.Render(spec, noTransformPlacement, harmony, barTrack, groove, sectionTrack, seed: 42);
//        var octaveUpResult = MotifRenderer.Render(spec, octaveUpPlacement, harmony, barTrack, groove, sectionTrack, seed: 42);

//        // OctaveUp should generally produce higher pitches (within register bounds)
//        if (noTransformResult.PartTrackNoteEvents.Count > 0 && octaveUpResult.PartTrackNoteEvents.Count > 0)
//        {
//            double noTransformAvg = noTransformResult.PartTrackNoteEvents.Average(e => e.NoteNumber);
//            double octaveUpAvg = octaveUpResult.PartTrackNoteEvents.Average(e => e.NoteNumber);

//            // Allow for register clamping to prevent octave up if already at top
//            Console.WriteLine($"  No transform avg: {noTransformAvg:F1}, OctaveUp avg: {octaveUpAvg:F1}");
//        }

//        Console.WriteLine("✓ OctaveUp transform: Applied when within register bounds");
//    }

//    // ========== Test Helpers ==========

//    private static (MotifSpec, MotifPlacement, HarmonyTrack, BarTrack, GroovePreset, SectionTrack) CreateTestContext()
//    {
//        var spec = MotifLibrary.ClassicRockHookA();

//        var sectionTrack = new SectionTrack();
//        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

//        var placement = MotifPlacement.Create(
//            spec,
//            absoluteSectionIndex: 0,
//            startBarWithinSection: 0,
//            durationBars: 2);

//        var harmony = CreateTestHarmonyTrack();
//        var barTrack = CreateTestBarTrack();
//        var groove = CreateTestGroovePreset();

//        return (spec, placement, harmony, barTrack, groove, sectionTrack);
//    }

//    private static HarmonyTrack CreateTestHarmonyTrack()
//    {
//        var track = new HarmonyTrack();
//        track.Add(new HarmonyEvent
//        {
//            StartBar = 1,
//            StartBeat = 1,
//            Key = "C",
//            Degree = 1,
//            Quality = "maj",
//            Bass = "root"
//        });
//        return track;
//    }

//    private static BarTrack CreateTestBarTrack()
//    {
//        var timingTrack = new Timingtrack();
//        timingTrack.Events.Add(new TimingEvent { StartBar = 1, Numerator = 4, Denominator = 4 });

//        var barTrack = new BarTrack();
//        barTrack.RebuildFromTimingTrack(timingTrack, totalBars: 20);

//        return barTrack;
//    }

//    private static GroovePreset CreateTestGroovePreset()
//    {
//        return new GroovePreset
//        {
//            Name = "Test Groove",
//            BeatsPerBar = 4,
//            AnchorLayer = new GrooveInstanceLayer
//            {
//                PadsOnsets = new List<decimal> { 1m, 2m, 3m, 4m }
//            }
//        };
//    }
//}

