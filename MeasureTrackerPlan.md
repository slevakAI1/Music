# Measure Tracking (Green Highlight) – Agile Plan

## Goal
Add measure tracking to MIDI playback so that the `dgSong` grid highlights the currently-playing measure column in green, and restores the previous measure’s background when the next measure begins. The feature must be non-breaking and implemented with the minimum number of changes related to this goal.

The user interaction starting point is a `dgSong` double-click on the row:
- `SongGridManager.FIXED_ROW_SEPARATOR_2` (`public const int FIXED_ROW_SEPARATOR_2 = 7;`)

## Key Constraints / Non-breaking Requirements
- Keep existing playback behavior intact.
- Do not rename grid column names (especially `colData`, measure columns, etc.).
- Keep fixed row indices stable.
- UI updates must occur on the UI thread.
- Polling should not block the UI thread.

## Assumptions / Dependencies
- `MidiPlaybackService` is the playback engine used by `Player.PlayMidiFromSongTracksAsync(_midiPlaybackService, midiDoc)`.
- We can derive “current measure” from current tick + the song’s time signature track.
- `SongContext.BarTrack` exists and can map ticks to bar/measure index, OR we can compute it via time signature events.

If BarTrack does *not* provide tick-to-measure mapping, add a small internal helper (non-breaking) that computes it.

---

# Story 1 — Expose Current Playback Tick from `MidiPlaybackService`

Status: Done

## Why
The UI needs to know which measure is currently playing. The most robust low-level signal is the current playback tick.

## Scope
Expose one or more non-breaking read-only properties (or method) in `MidiPlaybackService` that allow a consumer to poll the current playback position in ticks.

## Expected Code Touchpoints
- `Midi/MidiPlaybackService.cs`
- Possibly `Midi/Player.cs` if `Player` is wrapping playback state and can provide it without invasive changes.

## Technical Approach
1. Identify where the playback library tracks “current time” during playback.
2. Add a minimal API surface to `MidiPlaybackService`:
   - Example: `public long CurrentTick { get; }` (or `TryGetCurrentTick(out long tick)` if state is not always available).
   - Example: `public bool IsPlaying { get; }` already exists? If not, consider adding a read-only `PlaybackState` enum.
3. Ensure thread-safety:
   - If the playback callback updates a tick value, store it in a `long` and write with `Interlocked.Exchange`.
   - Read via `Interlocked.Read`.
4. Ensure the property behaves safely when not playing:
   - Return `0` (or `-1`) consistently.

## Acceptance Criteria
- A consumer can poll the playback position in ticks while playback is active.
- Polling does not throw and does not require UI-thread affinity.
- Existing playback APIs/signatures remain unchanged (non-breaking).

## Instructions
1. Use the minimum changes necessary and only changes related to this goal.
2. When done, re-check the acceptance criteria against the implemented code changes to ensure completeness.

---

# Story 2 — Implement Measure Polling + Measure Change Events (Background Worker)

Status: Done

## Why
We need a recurring polling mechanism that converts current tick ? current measure and emits measure-change notifications.

## Scope
Create a small component that:
- Polls `MidiPlaybackService.CurrentTick` on an interval.
- Converts tick to measure index (1-based).
- Detects measure changes.
- Raises an event on the UI thread so the grid can be updated safely.

## Expected Code Touchpoints
- New file: `Writer/SongGrid/PlaybackProgressTracker.cs` (or similar), if not already present.
  - Note: if a tracker already exists in the repo, extend it minimally rather than creating another.
- `Song/Bar/BarTrack.cs` and/or time-signature mapping helpers **only if** tick?measure mapping is missing.

## Technical Approach
1. Choose a polling interval (e.g., 50ms-100ms) balancing responsiveness and UI load.
2. Use `System.Threading.Timer` or a `Task` loop:
   - Prefer `PeriodicTimer` in .NET 6+ (available) for simple async polling.
   - Add cancellation support.
3. Ensure synchronization:
   - Capture `SynchronizationContext.Current` from the UI thread (when constructed from UI).
   - Post measure-changed events via that context.
4. Tick ? measure mapping:
   - Preferred: use `BarTrack` if it can map ticks to bar index.
   - Alternative: compute measure boundaries via time signature track:
     - `ticksPerMeasure = (TicksPerQuarterNote * 4 * numerator) / denominator`
     - Walk measures until tick < boundary.
     - Consider time signature changes at bar boundaries.
5. Expose event:
   - `event EventHandler<MeasureChangedEventArgs> MeasureChanged;`
   - Include `PreviousMeasure`, `CurrentMeasure`.
6. Provide Start/Stop/Dispose.

## Acceptance Criteria
- Component polls ticks without blocking the UI thread.
- It raises measure change events only when the measure changes.
- Events are dispatched on UI thread (no cross-thread exceptions when subscribers update controls).
- It stops promptly on playback stop/end.

## Instructions
1. Use the minimum changes necessary and only changes related to this goal.
2. When done, re-check the acceptance criteria against the implemented code changes to ensure completeness.

---

# Story 3 — Add Grid Highlight Helpers in `SongGridManager`

Status: Done

## Why
Centralize UI logic for highlighting measure columns and restoring prior background to ensure consistent styling.

## Scope
Add minimal helper methods to `SongGridManager` to:
- Highlight a given measure column across relevant rows.
- Restore previous measure’s column background.

## Expected Code Touchpoints
- `Writer/SongGrid/SongGridManager.cs`

## Technical Approach
1. Implement methods with minimal surface area:
   - `HighlightCurrentMeasure(DataGridView dgSong, int measureNumber)`
   - `ClearMeasureHighlight(DataGridView dgSong, int measureNumber)`
   - `ClearAllMeasureHighlights(DataGridView dgSong)`
2. Apply highlight only to measure columns (starting at `MEASURE_START_COLUMN_INDEX`).
3. Decide which rows are affected:
   - At minimum: highlight measure cells for actual part track rows (rows >= `FIXED_ROWS_COUNT`).
   - Optionally: highlight fixed rows too if they show measure-related data.
4. Preserve original cell background:
   - Minimal approach: restore to `DataGridView.DefaultCellStyle.BackColor` or per-row default.
   - If you need perfect restoration (custom per-cell styles), store prior colors in a dictionary keyed by (row, col). Only add this complexity if required.

## Acceptance Criteria
- When asked to highlight measure N, measure column N becomes green.
- When clearing measure N, the prior background is restored correctly (or reverts to default row/grid style).
- No changes to existing column naming or fixed row ordering.

## Instructions
1. Use the minimum changes necessary and only changes related to this goal.
2. When done, re-check the acceptance criteria against the implemented code changes to ensure completeness.

---

# Story 4 — Wire Measure Tracking to Play button (`btnPlayTracks`)

Status: Done

## Why
Measure tracking should activate during normal playback initiated by the WriterForm Play button.

## Scope
On Play click:
- Start playback via the existing playback code path.
- Start the polling tracker.
- Update the grid’s measure highlight in response to tracker events.
- Stop and clear highlights when playback finishes or is stopped.

## Expected Code Touchpoints
- `Writer/WriterForm/WriterForm.cs`

## Technical Approach
1. Update `btnPlay_Click`:
   - Call a wrapper that starts the tracker and reuses the existing play routine.
2. Ensure cleanup:
   - Stop tracker and clear highlights in a `finally`.
3. Ensure stop flows:
   - `btnStop_Click` stops tracker and clears highlights.

## Acceptance Criteria
- Clicking Play triggers playback with measure highlighting.
- While playing, the currently playing measure column is green.
- When measures advance, the previous measure returns to normal.
- When playback ends or is stopped, all highlights are cleared.

## Instructions
1. Use the minimum changes necessary and only changes related to this goal.
2. When done, re-check the acceptance criteria against the implemented code changes to ensure completeness.

---

# Story 5 — Edge Cases & Regression Checks

## Why
Measure tracking is timing-sensitive and may fail under common situations (no time signature, empty bars, stop/pause).

## Scope
Add minimal guards and ensure feature doesn’t break playback.

## Technical Approach
- If `TimeSignatureTrack` is missing, do not start tracking, and do not crash.
- If tick?measure mapping returns invalid values, do not highlight.
- Ensure highlight methods tolerate measure numbers beyond current column count.
- Verify stop/pause behavior:
  - Stop clears highlights.
  - Pause either keeps current highlight or stops updates (choose simplest consistent behavior).

## Acceptance Criteria
- Feature does not throw when there is no time signature track.
- Grid does not throw on highlight calls.
- Existing playback/import/export flows still work.

## Instructions
1. Use the minimum changes necessary and only changes related to this goal.
2. When done, re-check the acceptance criteria against the implemented code changes to ensure completeness.
