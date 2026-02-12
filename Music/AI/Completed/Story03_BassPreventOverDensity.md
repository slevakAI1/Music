# Story 3: BassPreventOverDensityOperator

## Summary
- Added `BassPreventOverDensityOperator` as a deterministic bass cleanup/removal operator.
- Registered it in `BassOperatorRegistryBuilder` under the cleanup/constraints group.

## Implementation Notes
- File: `Music/Generator/Bass/Operators/CleanupAndConstraints/BassPreventOverDensityOperator.cs`
- OperatorId: `BassPreventOverDensity`
- Family: `OperatorFamily.NoteRemoval`
- Uses `BassOperatorHelper.GetBassAnchorBeats(songContext, barNumber)` per bar.
- If bass anchors exceed `maxOnsetsPerBar` (default 10), emits `OperatorCandidateRemoval` for selected beats.
- Protects beat 1 by skipping it when any other beat remains removable.
- Deterministic ordering: removal candidates are scored (weak beat + non-beat1) and then tie-broken stably.

## Build
- Solution builds clean.
