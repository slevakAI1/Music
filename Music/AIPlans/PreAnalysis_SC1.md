# PreAnalysis_SC1 - Part Track Bar Coverage Analyzer

## 1. Story Intent Summary
- What: Introduce a pure utility to compute per-part per-bar coverage (Empty / HasContent / Locked) so generators can decide whether to fill or skip bars.
- Why: Enables incremental/manual workflows and generation policies that respect existing content; avoids premature coupling of generator logic to mutable state.
- Who: Generator authors and tooling (developers), generation pipeline (generator), and end-users who expect non-destructive fills.

## 2. Acceptance Criteria Checklist
1. Add a pure analysis utility `PartTrackBarCoverageAnalyzer` (proposed) that computes coverage for a PartTrack.
2. Inputs: `PartTrack`, `BarTrack`, `totalBars`.
3. Output: per-bar status map (e.g., `Dictionary<int, BarFillState>`).
4. `BarFillState` MVP values: `Empty`, `HasContent`, `Locked` (Locked is optional user-set flag).
5. Bar inclusion rule: a note "belongs to" a bar if its `[startTick, endTick)` overlaps the bar's `[StartTick, EndTick)`.
6. Analyzer must be pure and side-effect-free (do not store state on domain models).
7. Unit tests covering cross-bar notes, overlapping durations, and non-contiguous fills must be added before enabling generators to honor coverage.

Notes: acceptance criteria are written as a proposal in the story; `Locked` semantics and how/where the flag is provided are flagged as optional / deferred.

## 3. Dependencies & Integration Points
- Depends on: later material/placement stories (motif placement, comping, bass patterns, variation) as noted in the story.
- Likely story IDs: Material/Motif work (mentioned Phase 8+9), and generator stories that will honor coverage (future change requests in generator pipeline).
- Interacts with existing types: `PartTrack`, `PartTrackEvent` (AbsoluteTimeTicks, NoteDurationTicks), `BarTrack` (Bar boundaries, `TryGetBar`, `ToTick` helpers), and generator entry points (e.g., `DrumTrackGenerator` or Groove selection engines when they query coverage).
- Provides for future stories: a canonical source of truth for which bars are empty vs filled; used to gate generation (fill-only-empty bars) or surface UI status to users.

## 4. Inputs & Outputs
- Inputs:
  - `PartTrack` (contains `PartTrackEvent` list with `AbsoluteTimeTicks` and durations)
  - `BarTrack` (bar boundaries / tick conversion)
  - `totalBars` (int) or inferred from `BarTrack` length
  - Optional: `Locked` metadata (where does this come from? track meta, per-bar flags, or external user settings)
- Outputs:
  - `IReadOnlyDictionary<int, BarFillState>` mapping 1-based bar number -> state
  - Optionally: helper queries (e.g., `IsBarEmpty(int bar)`)
- Configuration/settings read:
  - Tick resolution `MusicConstants.TicksPerQuarterNote` indirectly (used by inputs)
  - No global mutable configuration expected; analyzer should be deterministic and environment-free

## 5. Constraints & Invariants
- Bars are 1-based and must map to `BarTrack` entries.
- Inclusion rule invariant: a note belongs to a bar iff `[startTick, endTick)` intersects bar `[startTick, endTick)` (inclusive start, exclusive end).
- Analyzer must be pure (no side-effects) and idempotent: same inputs → same outputs.
- Must not mutate `PartTrack` or `PartTrackEvent` objects.
- `Locked` state, if present, must be honored as a higher-precedence status (i.e., a locked bar should not be reported as `Empty` even if no events exist) — specification for how `Locked` supplied is required.
- The analyzer must handle notes with zero or negative durations defensively (clarify expected handling).

## 6. Edge Cases to Test
- Empty `PartTrack` → all bars `Empty` (unless `Locked`).
- Single note that starts at bar start and ends at bar end boundary — ensure inclusion/exclusion semantics (end exclusive) are correct.
- Note that spans multiple bars → all overlapped bars marked `HasContent`.
- Note with duration 0 → does it count as content? (clarify)
- Notes with negative duration or invalid `AbsoluteTimeTicks` → analyzer should not throw; decide whether to skip or treat as content.
- Overlapping notes that create contiguous/non-contiguous fills.
- PartTrack with events outside song range (before bar 1 or after last bar) → ensure they are ignored or clamped per decision.
- `Locked` bars mixed with `HasContent` bars; validate precedence.
- Large `totalBars` with sparse events (performance and correctness checks).

## 7. Clarifying Questions
1. What is the authoritative source for a bar `Locked` state? (track metadata, external map, UI flag?)
2. Should zero-length notes (duration == 0) count as `HasContent`?
3. How to treat notes that start exactly at a bar's `EndTick` (end exclusive rule asks that they belong to the next bar) — confirm desired behavior.
4. How should events outside the `[bar1Start..lastBarEnd)` range be treated (ignore, clamp, or error)?
5. If `ruleMin > ruleMax` style issues are present in other policies, should the analyzer attempt to normalize or simply report coverage?
6. Is `PartTrackEvent` AbsoluteTimeTicks guaranteed to be non-negative and sorted? If not, should the analyzer sort/validate or assume upstream guarantees?
7. Do generators prefer a dense map (every bar entry) or a sparse list of non-empty bar indices?
8. Should the analyzer expose counts (number of events / total occupied ticks) or only categorical states?

## 8. Test Scenario Ideas
- `Analyzer_When_PartTrackEmpty_ReturnsAllBarsEmptyUnlessLocked`
  - Input: empty PartTrack, BarTrack with N bars, optional locked list
  - Expect: every bar -> `Empty` or `Locked` where specified

- `Analyzer_NoteSpanningBars_MarksAllIntersectedBarsHasContent`
  - Input: one long note spanning bars 2..4
  - Expect: bars 2,3,4 -> `HasContent`

- `Analyzer_NoteOnBoundary_StartInclusiveEndExclusive_BelongsToCorrectBar`
  - Input: note with start == bar2.StartTick and end == bar2.EndTick
  - Expect: belongs to bar2 only (end exclusive semantics)

- `Analyzer_ZeroDurationNote_CountAsContent_WhenConfigured`
  - Input: zero-length event; Expect: configurable behavior (requires clarification)

- `Analyzer_OverlappingNotes_NonContiguousFillHandledCorrectly`
  - Input: notes in bars 1 and 3 only
  - Expect: bar 2 -> `Empty`, others `HasContent`

- Determinism test: run analyzer twice with same inputs → identical outputs.

---

// Notes: This document focuses on analysis/questions only; no implementation guidance is provided.
