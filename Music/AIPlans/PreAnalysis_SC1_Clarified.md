# PreAnalysis_SC1_Clarified - Part Track Bar Coverage Analyzer

This document resolves the clarifying questions from `PreAnalysis_SC1.md` into explicit, implementation-agnostic rules the analyzer must follow. These rules are intended to remove ambiguity so that the analyzer can be implemented and tested deterministically.

## Codebase Context

The analyzer will work with these existing types:
- **`PartTrack`**: Contains `List<PartTrackEvent> PartTrackNoteEvents` (events with absolute tick timing)
- **`PartTrackEvent`**: Has `long AbsoluteTimeTicks`, `int NoteDurationTicks` (note: field is `int` but tick calculations use `long`)
- **`BarTrack`**: Provides `IReadOnlyList<Bar> Bars` and `bool TryGetBar(int barNumber, out Bar bar)` method
- **`Bar`**: Has `int BarNumber` (1-based), `long StartTick`, `long EndTick`, `int Numerator`, `int Denominator`
- **Tick resolution**: All calculations use `MusicConstants.TicksPerQuarterNote = 480`

## Explicit Rules (answers to clarifying questions)

### 1. Locked bar source and precedence
   - The analyzer accepts an **optional external locked-bar set** (e.g., `IReadOnlySet<int>?` or equivalent) as the authoritative source for the `Locked` state.
   - If no locked-bar input is provided (`null` or empty), no bars are considered `Locked`.
   - **Locked precedence rule**: when a bar is marked `Locked`, its reported state MUST be `Locked` regardless of whether events exist in that bar.
   - Rationale: User-locked bars represent "do not modify" intent and override content analysis.

### 2. Inclusion semantics for notes and bar intervals
   - **Use half-open intervals** for all membership and overlap tests:
     - Note interval: `[noteStartTick, noteEndTick)` where `noteEndTick = AbsoluteTimeTicks + NoteDurationTicks`
     - Bar interval: `[bar.StartTick, bar.EndTick)`
   - **Overlap rule**: A note "belongs to" a bar iff the intersection of these intervals is non-empty.
   - **Consequences**:
     - A note that starts exactly at `bar.EndTick` does NOT belong to that bar
     - A note that ends exactly at `bar.StartTick` does NOT belong to that bar
     - A note with `AbsoluteTimeTicks == bar.StartTick` and zero duration DOES belong to that bar
   - **Implementation note**: Use `long` arithmetic throughout to match `AbsoluteTimeTicks` and `StartTick`/`EndTick` types.

### 3. Zero-duration and degenerate events
   - **Zero-duration notes** (where `NoteDurationTicks == 0`) COUNT as content if `AbsoluteTimeTicks` is within the bar's `[StartTick, EndTick)` interval.
     - Example: A drum hit with zero duration at tick 960 belongs to bar 2 if bar 2's interval is [960, 1920).
   - **Negative durations**: Events with `NoteDurationTicks < 0` are degenerate; they do NOT throw but are handled as follows:
     - Compute `noteEndTick = AbsoluteTimeTicks + NoteDurationTicks` (may be before start)
     - Apply half-open interval overlap rule as normal
     - If the degenerate interval `[endTick, startTick)` (reversed) intersects the bar, treat as content
     - SHOULD surface a non-fatal diagnostic marker for degenerate events
   - **Tests must assert**: degenerate events do not cause exceptions; behavior is predictable and deterministic.

### 4. Events outside the song range
   - **Ignore events** that lie entirely outside the valid bar range:
     - Before bar 1: `noteEndTick <= firstBar.StartTick`
     - After last bar: `noteStartTick >= lastBar.EndTick`
   - The analyzer SHOULD optionally expose a diagnostic count of out-of-range events (e.g., `OutOfRangeEventCount` property).
   - Out-of-range events MUST NOT throw exceptions; they are silently skipped in coverage computation.

### 5. Sorting, ordering and input assumptions
   - **Do NOT assume** callers supply sorted `PartTrackEvent` lists.
   - The analyzer MUST produce the same result regardless of event ordering in the input list.
   - The analyzer MUST NOT mutate `PartTrack`, `PartTrackEvent`, or `BarTrack` instances.
   - Implementation may perform internal buffering or per-bar accumulation but must return an **immutable snapshot** mapping.
   - **Performance note**: For large track lists, consider building a per-bar event index, but maintain determinism.

### 6. Output shape and density
   - **Primary output**: A dense mapping for every bar in the range `1..totalBars`: `IReadOnlyDictionary<int, BarFillState>` (or equivalent).
   - Every bar in `BarTrack.Bars` MUST have an entry in the output dictionary with one of these states:
     - `BarFillState.Locked`: Bar is user-locked (highest precedence)
     - `BarFillState.HasContent`: At least one event intersects this bar
     - `BarFillState.Empty`: No events intersect and not locked
   - **Optional extended metrics** (recommended for diagnostics/UI):
     - `EventCount` (int): number of events that intersect the bar
     - `OccupiedTicks` (long): total distinct tick coverage within the bar (deduplicated for overlapping notes)
     - `OutOfRangeEventCount` (int, global): count of events ignored due to being outside bar range
   - **Consumer expectations**: Generators and UI tools will rely on the dense map for gating decisions; extended metrics are diagnostic-only.

### 7. Determinism and purity
   - The analyzer SHALL be **pure and deterministic**: same inputs (same events, same BarTrack, same locked set) → same outputs.
   - The analyzer SHALL NOT:
     - Modify external state
     - Store results on domain objects (`PartTrack`, `Bar`, etc.)
     - Depend on system time, random numbers, or environment state
   - The analyzer MAY be called multiple times without side effects.
   - **Thread-safety**: Not required (single-threaded analysis is acceptable); if multi-threaded, ensure no shared mutable state.

### 8. Diagnostics and validation behavior
   - The analyzer MUST NOT throw for malformed events:
     - Negative `AbsoluteTimeTicks`
     - Negative `NoteDurationTicks`
     - Events with `AbsoluteTimeTicks` beyond reasonable bounds
   - Instead, the analyzer MUST record **non-fatal diagnostics** that tests can assert against.
   - **Diagnostic fields** (optional but recommended):
     - `DegenerateEventCount`: count of events with negative duration
     - `OutOfRangeEventCount`: count of events outside bar range
     - `NegativeTickEventCount`: count of events with negative AbsoluteTimeTicks
   - **Default behavior**: When diagnostics are not requested, validation issues are silently handled; coverage is computed with best-effort rules.

### 9. Input validation and totalBars handling
   - **totalBars parameter**: If provided, it represents the maximum bar number to analyze; bars beyond this are ignored.
   - **BarTrack.Bars**: The authoritative source of bar boundaries; analyzer iterates this collection.
   - **Empty BarTrack**: If `BarTrack.Bars` is empty, the analyzer returns an empty coverage map (no bars to analyze).
   - **Invalid bar numbers**: If locked set contains bar numbers not present in BarTrack, those entries are ignored (optional diagnostic warning).

### 10. BarFillState enum definition (proposed)
```csharp
public enum BarFillState
{
    Empty = 0,       // No events intersect this bar
    HasContent = 1,  // At least one event intersects this bar
    Locked = 2       // User-set "do not modify" flag (highest precedence)
}
```
- **Precedence**: `Locked` > `HasContent` > `Empty`
- **Future extension**: Additional states (e.g., `Partial`, `Full`) may be added later without breaking existing code if enum is designed for extension.

## Implementation Guidance (Non-Normative)

### Suggested API Shape
```csharp
public class PartTrackBarCoverageAnalyzer
{
    /// <summary>
    /// Analyzes per-bar coverage state for a PartTrack.
    /// </summary>
    /// <param name="partTrack">The track to analyze</param>
    /// <param name="barTrack">Bar boundaries (ruler)</param>
    /// <param name="lockedBars">Optional set of user-locked bar numbers (1-based)</param>
    /// <returns>Dense map: bar number -> fill state</returns>
    public static IReadOnlyDictionary<int, BarFillState> Analyze(
        PartTrack partTrack,
        BarTrack barTrack,
        IReadOnlySet<int>? lockedBars = null);
    
    /// <summary>
    /// Analyzes with extended diagnostics.
    /// </summary>
    public static BarCoverageReport AnalyzeWithDiagnostics(
        PartTrack partTrack,
        BarTrack barTrack,
        IReadOnlySet<int>? lockedBars = null);
}

public record BarCoverageReport(
    IReadOnlyDictionary<int, BarFillState> Coverage,
    IReadOnlyDictionary<int, int> EventCountPerBar,
    IReadOnlyDictionary<int, long> OccupiedTicksPerBar,
    int OutOfRangeEventCount,
    int DegenerateEventCount,
    int NegativeTickEventCount);
```

### Algorithm Sketch (for clarity, not prescription)
```
1. Initialize coverage map: for each bar in BarTrack.Bars, set state = Empty
2. For each bar in lockedBars, override state = Locked
3. For each event in partTrack.PartTrackNoteEvents:
     a. Compute noteStart = event.AbsoluteTimeTicks
     b. Compute noteEnd = noteStart + event.NoteDurationTicks
     c. For each bar where [bar.StartTick, bar.EndTick) intersects [noteStart, noteEnd):
          - If state != Locked, set state = HasContent
          - Increment diagnostic counters if tracking enabled
4. Return immutable coverage map
```

### Edge Case Handling Examples

**Example 1: Zero-duration event at bar boundary**
- Event: `AbsoluteTimeTicks = 960`, `NoteDurationTicks = 0`
- Bar 2: `[960, 1920)`
- Result: Event belongs to bar 2 (960 is within [960, 1920))

**Example 2: Note spanning multiple bars**
- Event: `AbsoluteTimeTicks = 800`, `NoteDurationTicks = 1200`
- Note interval: `[800, 2000)`
- Bar 1: `[0, 960)` → intersects → `HasContent`
- Bar 2: `[960, 1920)` → intersects → `HasContent`
- Bar 3: `[1920, 2880)` → intersects → `HasContent`

**Example 3: Negative duration (degenerate)**
- Event: `AbsoluteTimeTicks = 1000`, `NoteDurationTicks = -50`
- Note interval: `[950, 1000)` (reversed semantically but computed as half-open)
- Bar with `[900, 1800)` → intersects → `HasContent` (with diagnostic warning)

**Example 4: Locked bar with content**
- Bar 5 is in `lockedBars` set
- Bar 5 has 3 events
- Result: state = `Locked` (not `HasContent`), precedence rule applies
1. Locked source: external locked-bar set parameter; no bars locked if omitted.
2. Zero-length notes: count as `HasContent` if start tick in bar interval.
3. Start exactly at bar end: does not belong to that bar (half-open interval semantics).
4. Events outside range: ignore (optionally report count in diagnostics).
5. Normalization of unrelated policy bounds: analyzer does not normalize other policy data; it only reports coverage.
6. Sorting: do not assume sorted input; handle unsorted safely without mutating input.
7. Dense vs sparse: produce dense per-bar map covering `1..totalBars`.
8. Counts/metrics: optional extended metrics allowed and recommended for diagnostics, but primary contract is categorical per-bar state.

## Summary: Quick Reference for Original Questions

1. **Locked source**: External `IReadOnlySet<int>?` parameter; `null`/empty = no locked bars
2. **Zero-length notes**: Count as `HasContent` if `AbsoluteTimeTicks` in bar's `[StartTick, EndTick)` interval
3. **Start exactly at bar end**: Does NOT belong to that bar (half-open interval: `[start, end)`)
4. **Events outside range**: Ignore; optionally report count in diagnostics; no exceptions
5. **Sorting**: Do NOT assume sorted input; handle unsorted safely; no mutation
6. **Dense vs sparse**: Dense map covering all bars in `BarTrack.Bars` (1-based numbering)
7. **Counts/metrics**: Optional extended metrics recommended for diagnostics; primary contract is categorical state
8. **Degenerate events**: Non-fatal; handle with best-effort rules; surface diagnostic count

## Test Scenarios (Expanded from PreAnalysis_SC1.md)

### Core Functionality Tests
1. **`Analyzer_EmptyPartTrack_ReturnsAllBarsEmpty`**
   - Input: Empty `PartTrackNoteEvents` list, 10 bars in BarTrack, no locked bars
   - Expected: All 10 bars → `BarFillState.Empty`

2. **`Analyzer_LockedBarsOverrideContent`**
   - Input: 10 bars, bars 3 and 7 in locked set, events in bars 3, 5, 7
   - Expected: Bar 3 → `Locked`, Bar 5 → `HasContent`, Bar 7 → `Locked`

3. **`Analyzer_SingleEventSpanningMultipleBars_MarksAllIntersectedBars`**
   - Input: One event at tick 800, duration 1200 (spans bars 1-3)
   - Expected: Bars 1, 2, 3 → `HasContent`

4. **`Analyzer_ZeroDurationEventAtBarBoundary_BelongsToCorrectBar`**
   - Input: Event at tick 960 (bar 2 start), duration 0
   - Expected: Bar 2 → `HasContent`, Bar 1 → `Empty`

5. **`Analyzer_EventEndingExactlyAtBarStart_DoesNotBelongToBar`**
   - Input: Event ending at tick 960 (bar 2 start)
   - Expected: Bar 2 → `Empty` (assuming no other events)

### Edge Case Tests
6. **`Analyzer_NegativeDurationEvent_HandledWithoutException`**
   - Input: Event with `NoteDurationTicks = -50`
   - Expected: No exception; diagnostic count incremented; coverage computed per rule

7. **`Analyzer_EventsOutsideBarRange_IgnoredAndCounted`**
   - Input: Events before bar 1 and after last bar
   - Expected: Coverage ignores them; `OutOfRangeEventCount` reflects count

8. **`Analyzer_UnsortedEventList_ProducesDeterministicResult`**
   - Input: Same events in different orders across multiple runs
   - Expected: Identical coverage map each time

9. **`Analyzer_OverlappingEvents_DoNotDoubleCountContent`**
   - Input: Two overlapping events in same bar
   - Expected: Bar → `HasContent` (not "double content"); metrics may show event count = 2

10. **`Analyzer_EmptyBarTrack_ReturnsEmptyCoverageMap`**
    - Input: `BarTrack.Bars` is empty
    - Expected: Empty coverage map (no bars to analyze)

### Diagnostics Tests
11. **`AnalyzeWithDiagnostics_CapturesTotalOccupiedTicks`**
    - Input: Bar with two events totaling 400 ticks of coverage
    - Expected: `OccupiedTicksPerBar[bar]` reflects deduplicated tick count

12. **`AnalyzeWithDiagnostics_CountsEventsPerBar`**
    - Input: Bar 2 has 3 events
    - Expected: `EventCountPerBar[2] == 3`

### Performance/Stress Tests (optional but recommended)
13. **`Analyzer_LargeTrack_PerformsReasonably`**
    - Input: 10,000 events across 100 bars
    - Expected: Completes in reasonable time; deterministic result

14. **`Analyzer_SparseEvents_HandlesNonContiguousFills`**
    - Input: Events in bars 1, 5, 10, 50 only
    - Expected: Only those bars → `HasContent`; others → `Empty`

---

## Integration with Generation Pipeline (Future Work)

Once this analyzer is implemented and tested, generators can use it to:
1. **Fill-only-empty policy**: Generate only in bars where `state == BarFillState.Empty`
2. **Respect locked bars**: Never modify bars where `state == BarFillState.Locked`
3. **Incremental generation**: User can generate verse drums, lock them, then generate chorus without overwriting verse
4. **UI feedback**: Display per-bar coverage state in song grid (color coding)
5. **Validation**: Detect unintended gaps or accidental overwrites

**Note**: The analyzer itself does NOT enforce these policies; it only provides coverage data. Generation policy enforcement is a separate concern.

---

// End of clarified rules for SC1. These rules are intentionally prescriptive about behavior, error-handling, and testing, while remaining flexible about specific API design and internal algorithms.
