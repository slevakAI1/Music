# Backlog

## Story 1.1b — Track per-part bar coverage (progress) independently of song length — DEFERRED

Status: DEFERRED
Reason: The finer details of note generation and generator policies (when to prefer filling `Empty` vs augmenting `HasContent`, how to treat non-contiguous fills, and the semantics of a `Locked` state) are not yet settled. This story depends on later work (comping, bass patterns, variation, motif placement) and is postponed to avoid premature coupling.

Intent: support incremental/manual workflows where parts are filled non-uniformly and non-contiguously; generation needs to know what is empty vs already has content.

Acceptance criteria (proposed):
- Introduce a computed analysis utility (not stored state) such as `PartTrackBarCoverageAnalyzer`:
  - Inputs: `PartTrack`, `BarTrack` (the ruler), and `totalBars`.
  - Output: per-bar status map, e.g. `Dictionary<int, BarFillState>`.
- Define `BarFillState` (proposed MVP):
  - `Empty`: no events intersect this bar
  - `HasContent`: at least one event intersects this bar
  - `Locked`: optional user-set flag meaning “do not modify” (may be added later)
- Provide bar intersection rules (MVP):
  - A note “belongs to” a bar if its `[startTick, endTick)` overlaps the bar’s `[StartTick, EndTick)`.
- Update generators to optionally:
  - fill only `Empty` bars
  - leave `HasContent` bars untouched (or add only if policy allows)

Notes for future implementation:
- Implement `PartTrackBarCoverageAnalyzer` as a pure, side-effect-free utility returning coverage data; do not store per-part coverage state in domain models to avoid staleness.
- Add unit tests covering cross-bar notes, overlapping durations, and non-contiguous fills before enabling generator behavior that honors coverage.
- Consider adding a `GenerationPolicy` parameter to generators to control whether they respect coverage or always generate full parts.
