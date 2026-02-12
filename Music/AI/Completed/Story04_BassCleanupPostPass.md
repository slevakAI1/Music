# Story 4: Apply Cleanup Operators as Post-Pass

## Summary
- Implemented an explicit cleanup post-pass in `BassOperatorApplicator` that always runs after the normal random operator loop.
- Cleanup operators run in a fixed deterministic order:
  1) `BassSnapBeatsToSubdivision`
  2) `BassResolveOverlapsAndOrder`
  3) `BassPreventOverDensity`
- Cleanup operators are excluded from the random selection pool.

## Implementation
- Modified: `Music/Generator/Bass/Operators/BassOperatorApplicator.cs`
  - Added `CleanupOperatorIds` list.
  - Random operator pool now filters out cleanup operator IDs.
  - Added `ApplyCleanupPostPass(...)` which:
    - Skips only when there are no bass onsets in the current result.
    - Applies cleanup operators by `OperatorId` via `BassOperatorRegistry.GetOperatorById`.
    - Runs both additions and removals per bar using existing `ApplyAdditions`/`ApplyRemovals`.

## Build
- Solution builds clean.
