// AI: purpose=Tests for PartTrackBarCoverageAnalyzer covering all SC1 acceptance criteria and edge cases.
// AI: invariants=Tests are deterministic; no external dependencies; covers empty, locked, spanning, edge cases.

using Music.Generator;
using Music.MyMidi;
using Xunit;

namespace Music.Tests.Generator.Groove;

public class PartTrackBarCoverageAnalyzerTests
{
    // ========================================================================
    // Test Fixtures
    // ========================================================================

    private static BarTrack CreateBarTrack(int totalBars = 10, int numerator = 4, int denominator = 4)
    {
        var timingTrack = new Timingtrack();
        timingTrack.Add(new TimingEvent { StartBar = 1, Numerator = numerator, Denominator = denominator });

        var barTrack = new BarTrack();
        barTrack.RebuildFromTimingTrack(timingTrack, totalBars);
        return barTrack;
    }

    private static PartTrack CreatePartTrack(params (long startTick, int durationTicks)[] events)
    {
        var noteEvents = events.Select(e => new PartTrackEvent
        {
            AbsoluteTimeTicks = e.startTick,
            NoteDurationTicks = e.durationTicks,
            NoteNumber = 60,
            NoteOnVelocity = 100
        }).ToList();

        return new PartTrack(noteEvents);
    }

    // 4/4 at 480 TPQ = 1920 ticks per bar
    private const int TicksPerBar = 1920;

    // ========================================================================
    // Core Functionality Tests
    // ========================================================================

    [Fact]
    public void Analyzer_EmptyPartTrack_ReturnsAllBarsEmpty()
    {
        var barTrack = CreateBarTrack(totalBars: 10);
        var partTrack = CreatePartTrack(); // Empty

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        Assert.Equal(10, result.Count);
        Assert.All(result.Values, state => Assert.Equal(BarFillState.Empty, state));
    }

    [Fact]
    public void Analyzer_LockedBarsOverrideContent()
    {
        var barTrack = CreateBarTrack(totalBars: 10);
        // Events in bars 3, 5, 7
        var partTrack = CreatePartTrack(
            (2 * TicksPerBar + 100, 100),  // Bar 3
            (4 * TicksPerBar + 100, 100),  // Bar 5
            (6 * TicksPerBar + 100, 100)   // Bar 7
        );
        var lockedBars = new HashSet<int> { 3, 7 };

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack, lockedBars);

        Assert.Equal(BarFillState.Locked, result[3]);      // Locked overrides content
        Assert.Equal(BarFillState.HasContent, result[5]);  // Not locked, has content
        Assert.Equal(BarFillState.Locked, result[7]);      // Locked overrides content
        Assert.Equal(BarFillState.Empty, result[1]);       // No content, not locked
    }

    [Fact]
    public void Analyzer_SingleEventSpanningMultipleBars_MarksAllIntersectedBars()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Event starting at tick 800 (bar 1), duration 1200 spans into bar 2 (ends at 2000)
        var partTrack = CreatePartTrack((800, 1200));

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        Assert.Equal(BarFillState.HasContent, result[1]); // Event starts here
        Assert.Equal(BarFillState.HasContent, result[2]); // Event spans into bar 2
        Assert.Equal(BarFillState.Empty, result[3]);      // No intersection
    }

    [Fact]
    public void Analyzer_EventSpanningThreeBars_MarksAllThree()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Event: tick 800, duration 2200 (ends at 3000, spanning bars 1, 2, and part of 3)
        // Bar 1: [0, 1920), Bar 2: [1920, 3840), Bar 3: [3840, 5760)
        // Event: [800, 3000) overlaps bar 1 and bar 2
        var partTrack = CreatePartTrack((800, 2200));

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        Assert.Equal(BarFillState.HasContent, result[1]);
        Assert.Equal(BarFillState.HasContent, result[2]);
        Assert.Equal(BarFillState.Empty, result[3]); // Event ends at 3000, bar 3 starts at 3840
    }

    // ========================================================================
    // Zero-Duration Event Tests
    // ========================================================================

    [Fact]
    public void Analyzer_ZeroDurationEventAtBarBoundary_BelongsToCorrectBar()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Zero-duration event at bar 2 start (tick 1920)
        var partTrack = CreatePartTrack((TicksPerBar, 0)); // tick 1920, duration 0

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        Assert.Equal(BarFillState.Empty, result[1]);       // Event not in bar 1
        Assert.Equal(BarFillState.HasContent, result[2]);  // Event at bar 2 start
    }

    [Fact]
    public void Analyzer_ZeroDurationEventInsideBar_CountsAsContent()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Zero-duration event at tick 500 (inside bar 1)
        var partTrack = CreatePartTrack((500, 0));

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        Assert.Equal(BarFillState.HasContent, result[1]);
    }

    // ========================================================================
    // Boundary Tests (Half-Open Interval Semantics)
    // ========================================================================

    [Fact]
    public void Analyzer_EventEndingExactlyAtBarStart_DoesNotBelongToBar()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Event ending exactly at bar 2 start (tick 1920)
        // Event: [0, 1920) - ends exactly where bar 2 starts
        var partTrack = CreatePartTrack((0, TicksPerBar));

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        Assert.Equal(BarFillState.HasContent, result[1]); // Event in bar 1
        Assert.Equal(BarFillState.Empty, result[2]);      // Event does NOT extend into bar 2
    }

    [Fact]
    public void Analyzer_EventStartingAtBarEnd_BelongsToNextBar()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Event starting at bar 1 end (which is bar 2 start, tick 1920)
        var partTrack = CreatePartTrack((TicksPerBar, 100));

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        Assert.Equal(BarFillState.Empty, result[1]);      // Bar 1 ends at tick 1920, event starts there
        Assert.Equal(BarFillState.HasContent, result[2]); // Event belongs to bar 2
    }

    // ========================================================================
    // Negative Duration (Degenerate) Event Tests
    // ========================================================================

    [Fact]
    public void Analyzer_NegativeDurationEvent_HandledWithoutException()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Degenerate event: start=1000, duration=-50 => interval [950, 1000)
        var partTrack = CreatePartTrack((1000, -50));

        var report = PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(partTrack, barTrack);

        Assert.Equal(1, report.DegenerateEventCount);
        Assert.Equal(BarFillState.HasContent, report.Coverage[1]); // Still counts as content
    }

    [Fact]
    public void Analyzer_NegativeDurationEvent_CountedInDiagnostics()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        var partTrack = CreatePartTrack((1000, -50), (2000, -100), (3000, 100));

        var report = PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(partTrack, barTrack);

        Assert.Equal(2, report.DegenerateEventCount);
    }

    // ========================================================================
    // Out-of-Range Event Tests
    // ========================================================================

    [Fact]
    public void Analyzer_EventsOutsideBarRange_IgnoredAndCounted()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Bar range: [0, 9600) for 5 bars at 1920 ticks each
        // Events before bar 1 and after last bar
        var partTrack = CreatePartTrack(
            (-100, 50),      // Entirely before bar 1
            (10000, 100)     // Entirely after last bar
        );

        var report = PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(partTrack, barTrack);

        Assert.Equal(2, report.OutOfRangeEventCount);
        Assert.All(report.Coverage.Values, state => Assert.Equal(BarFillState.Empty, state));
    }

    [Fact]
    public void Analyzer_EventEndingBeforeFirstBar_IgnoredAsOutOfRange()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Event ending exactly at tick 0 (first bar start)
        var partTrack = CreatePartTrack((-100, 100)); // [-100, 0) - ends exactly at bar 1 start

        var report = PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(partTrack, barTrack);

        Assert.Equal(1, report.OutOfRangeEventCount);
        Assert.Equal(1, report.NegativeTickEventCount);
    }

    // ========================================================================
    // Unsorted Input Tests
    // ========================================================================

    [Fact]
    public void Analyzer_UnsortedEventList_ProducesDeterministicResult()
    {
        var barTrack = CreateBarTrack(totalBars: 5);

        // Same events in different orders
        var eventsOrder1 = new List<(long, int)> { (100, 50), (3000, 100), (1500, 200) };
        var eventsOrder2 = new List<(long, int)> { (3000, 100), (100, 50), (1500, 200) };
        var eventsOrder3 = new List<(long, int)> { (1500, 200), (100, 50), (3000, 100) };

        var result1 = PartTrackBarCoverageAnalyzer.Analyze(CreatePartTrack(eventsOrder1.ToArray()), barTrack);
        var result2 = PartTrackBarCoverageAnalyzer.Analyze(CreatePartTrack(eventsOrder2.ToArray()), barTrack);
        var result3 = PartTrackBarCoverageAnalyzer.Analyze(CreatePartTrack(eventsOrder3.ToArray()), barTrack);

        // All should produce identical results
        foreach (var barNum in result1.Keys)
        {
            Assert.Equal(result1[barNum], result2[barNum]);
            Assert.Equal(result2[barNum], result3[barNum]);
        }
    }

    // ========================================================================
    // Overlapping Events Tests
    // ========================================================================

    [Fact]
    public void Analyzer_OverlappingEvents_DoNotDoubleCountContent()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Two overlapping events in bar 1
        var partTrack = CreatePartTrack((100, 500), (200, 500));

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        Assert.Equal(BarFillState.HasContent, result[1]); // Single HasContent, not "double content"
    }

    [Fact]
    public void Analyzer_OverlappingEvents_EventCountReflectsActualCount()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        var partTrack = CreatePartTrack((100, 500), (200, 500), (300, 500));

        var report = PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(partTrack, barTrack);

        Assert.Equal(3, report.EventCountPerBar[1]);
    }

    [Fact]
    public void Analyzer_OverlappingEvents_OccupiedTicksAreDeduplicated()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Two overlapping events: [100, 600) and [200, 700)
        // Union is [100, 700) = 600 ticks
        var partTrack = CreatePartTrack((100, 500), (200, 500));

        var report = PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(partTrack, barTrack);

        Assert.Equal(600, report.OccupiedTicksPerBar[1]);
    }

    // ========================================================================
    // Empty BarTrack Tests
    // ========================================================================

    [Fact]
    public void Analyzer_EmptyBarTrack_ReturnsEmptyCoverageMap()
    {
        var barTrack = new BarTrack(); // Empty, no rebuild
        var partTrack = CreatePartTrack((100, 500));

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        Assert.Empty(result);
    }

    [Fact]
    public void Analyzer_EmptyBarTrack_AllEventsCountedAsOutOfRange()
    {
        var barTrack = new BarTrack();
        var partTrack = CreatePartTrack((100, 500), (200, 300));

        var report = PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(partTrack, barTrack);

        Assert.Empty(report.Coverage);
        Assert.Equal(2, report.OutOfRangeEventCount);
    }

    // ========================================================================
    // Locked Bars Edge Cases
    // ========================================================================

    [Fact]
    public void Analyzer_LockedBarWithNoContent_RemainsLocked()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        var partTrack = CreatePartTrack(); // No events
        var lockedBars = new HashSet<int> { 3 };

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack, lockedBars);

        Assert.Equal(BarFillState.Locked, result[3]);
    }

    [Fact]
    public void Analyzer_LockedBarNotInBarTrack_Ignored()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        var partTrack = CreatePartTrack();
        var lockedBars = new HashSet<int> { 99 }; // Bar 99 doesn't exist

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack, lockedBars);

        Assert.False(result.ContainsKey(99)); // Not added to result
        Assert.Equal(5, result.Count);
    }

    // ========================================================================
    // Diagnostics Tests
    // ========================================================================

    [Fact]
    public void AnalyzeWithDiagnostics_CapturesTotalOccupiedTicks()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Two non-overlapping events in bar 1: [100, 200) and [300, 500)
        // Total occupied = 100 + 200 = 300 ticks
        var partTrack = CreatePartTrack((100, 100), (300, 200));

        var report = PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(partTrack, barTrack);

        Assert.Equal(300, report.OccupiedTicksPerBar[1]);
    }

    [Fact]
    public void AnalyzeWithDiagnostics_CountsEventsPerBar()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // 3 events in bar 1, 2 events in bar 2
        var partTrack = CreatePartTrack(
            (100, 50), (200, 50), (300, 50),   // Bar 1
            (TicksPerBar + 100, 50), (TicksPerBar + 200, 50) // Bar 2
        );

        var report = PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(partTrack, barTrack);

        Assert.Equal(3, report.EventCountPerBar[1]);
        Assert.Equal(2, report.EventCountPerBar[2]);
        Assert.Equal(0, report.EventCountPerBar[3]);
    }

    [Fact]
    public void AnalyzeWithDiagnostics_TracksNegativeTickEvents()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        var noteEvents = new List<PartTrackEvent>
        {
            new PartTrackEvent { AbsoluteTimeTicks = -50, NoteDurationTicks = 100, NoteNumber = 60 },
            new PartTrackEvent { AbsoluteTimeTicks = -100, NoteDurationTicks = 50, NoteNumber = 60 }
        };
        var partTrack = new PartTrack(noteEvents);

        var report = PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(partTrack, barTrack);

        Assert.Equal(2, report.NegativeTickEventCount);
    }

    // ========================================================================
    // Sparse Events Tests
    // ========================================================================

    [Fact]
    public void Analyzer_SparseEvents_HandlesNonContiguousFills()
    {
        var barTrack = CreateBarTrack(totalBars: 10);
        // Events only in bars 1, 5, 10
        var partTrack = CreatePartTrack(
            (100, 50),                    // Bar 1
            (4 * TicksPerBar + 100, 50),  // Bar 5
            (9 * TicksPerBar + 100, 50)   // Bar 10
        );

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        Assert.Equal(BarFillState.HasContent, result[1]);
        Assert.Equal(BarFillState.Empty, result[2]);
        Assert.Equal(BarFillState.Empty, result[3]);
        Assert.Equal(BarFillState.Empty, result[4]);
        Assert.Equal(BarFillState.HasContent, result[5]);
        Assert.Equal(BarFillState.Empty, result[6]);
        Assert.Equal(BarFillState.Empty, result[7]);
        Assert.Equal(BarFillState.Empty, result[8]);
        Assert.Equal(BarFillState.Empty, result[9]);
        Assert.Equal(BarFillState.HasContent, result[10]);
    }

    // ========================================================================
    // Argument Validation Tests
    // ========================================================================

    [Fact]
    public void Analyze_NullPartTrack_ThrowsArgumentNullException()
    {
        var barTrack = CreateBarTrack(totalBars: 5);

        Assert.Throws<ArgumentNullException>(() =>
            PartTrackBarCoverageAnalyzer.Analyze(null!, barTrack));
    }

    [Fact]
    public void Analyze_NullBarTrack_ThrowsArgumentNullException()
    {
        var partTrack = CreatePartTrack();

        Assert.Throws<ArgumentNullException>(() =>
            PartTrackBarCoverageAnalyzer.Analyze(partTrack, null!));
    }

    [Fact]
    public void AnalyzeWithDiagnostics_NullPartTrack_ThrowsArgumentNullException()
    {
        var barTrack = CreateBarTrack(totalBars: 5);

        Assert.Throws<ArgumentNullException>(() =>
            PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(null!, barTrack));
    }

    // ========================================================================
    // Dense Coverage Map Tests
    // ========================================================================

    [Fact]
    public void Analyzer_ProducesDenseCoverageMap_AllBarsPresent()
    {
        var barTrack = CreateBarTrack(totalBars: 7);
        var partTrack = CreatePartTrack((100, 50)); // Only bar 1 has content

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        // All 7 bars should be present in the map
        for (int i = 1; i <= 7; i++)
        {
            Assert.True(result.ContainsKey(i), $"Bar {i} should be present in coverage map");
        }
    }

    // ========================================================================
    // Complex Multi-Bar Spanning Tests
    // ========================================================================

    [Fact]
    public void Analyzer_EventSpanningAllBars_MarksAllBarsAsHasContent()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        // Event spanning all 5 bars
        var partTrack = CreatePartTrack((0, 5 * TicksPerBar));

        var result = PartTrackBarCoverageAnalyzer.Analyze(partTrack, barTrack);

        for (int i = 1; i <= 5; i++)
        {
            Assert.Equal(BarFillState.HasContent, result[i]);
        }
    }

    [Fact]
    public void Analyzer_EventSpanningAllBars_OccupiedTicksCorrect()
    {
        var barTrack = CreateBarTrack(totalBars: 5);
        var partTrack = CreatePartTrack((0, 5 * TicksPerBar));

        var report = PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics(partTrack, barTrack);

        // Each bar should have TicksPerBar occupied ticks
        for (int i = 1; i <= 5; i++)
        {
            Assert.Equal(TicksPerBar, report.OccupiedTicksPerBar[i]);
        }
    }
}
