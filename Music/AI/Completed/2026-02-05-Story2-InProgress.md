# Story 2: Remove energy level and redundant DrummerContext fields (In Progress)

## Summary
- Removed all redundant properties from `DrummerContext` (EnergyLevel, MotifPresenceMap, ActiveRoles, CurrentHatMode, HatSubdivision, IsFillWindow, IsAtSectionBoundary, BackbeatBeats, BeatsPerBar).
- Simplified `DrummerContextBuilder` to only populate cross-bar state (LastKickBeat, LastSnareBeat).
- Removed `MinEnergyThreshold`/`MaxEnergyThreshold` from `DrumOperatorBase`.
- Created `DrummerContextHelpers` with utilities for bar-derived properties.
- Removed all energy threshold overrides from operators (28 files).

## Remaining Work
- **161+ compilation errors** in drum operators accessing removed properties.
- Need to update all operators to:
  - Replace `context.EnergyLevel` usage (remove or replace with section-based logic)
  - Replace `context.ActiveRoles` with groove preset lookup
  - Replace `context.BackbeatBeats` with `DrummerContextHelpers.ComputeBackbeatBeats(context.Bar.BeatsPerBar)`
  - Replace `context.BeatsPerBar` with `context.Bar.BeatsPerBar`
  - Replace `context.IsFillWindow` with `DrummerContextHelpers.IsFillWindow(context.Bar)`
  - Replace `context.IsAtSectionBoundary` with `DrummerContextHelpers.IsAtSectionBoundary(context.Bar)`
  - Replace `context.CurrentHatMode` / `context.HatSubdivision` with memory lookup or removal
  - Remove `context.MotifPresenceMap` usage

## Files Modified
- Music/Generator/Agents/Drums/Context/DrummerContext.cs
- Music/Generator/Agents/Drums/Context/DrummerContextBuilder.cs
- Music/Generator/Agents/Drums/Context/DrummerContextHelpers.cs (new)
- Music/Generator/Agents/Drums/Operators/Base/DrumOperatorBase.cs
- Music/Generator/Agents/Drums/Selection/Candidates/DrummerCandidateSource.cs
- Music/Generator/Agents/Drums/Operators/SubdivisionTransform/HatDropOperator.cs (partial)
- All operator files (energy thresholds removed via script)

## Next Steps
Systematically update remaining operators to fix compilation errors.
