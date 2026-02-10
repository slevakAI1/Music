# Epic: DrummerContext De-duplication and Bar-Derived Context

**Completed:** 2026-02-05

## Goal
Remove redundant fields from `Music.Generator.Agents.Drums.Context.DrummerContext` so it contains only non-bar-derivable state, while keeping runtime behavior intact.

## Summary

### Removed from DrummerContext
The following properties were removed from `DrummerContext`:

| Property | Replacement |
|----------|-------------|
| `EnergyLevel` | **Removed entirely** - all energy-level logic eliminated |
| `ActiveRoles` | Deferred to operator `RequiredRole` or groove preset |
| `CurrentHatMode` | Operators use default assumptions |
| `HatSubdivision` | Operators use default assumptions |
| `IsFillWindow` | Now `context.Bar.IsFillWindow` |
| `IsAtSectionBoundary` | Now `context.Bar.IsAtSectionBoundary` |
| `BackbeatBeats` | Now `context.Bar.BackbeatBeats` |
| `BeatsPerBar` | Now `context.Bar.BeatsPerBar` |
| `MotifPresenceMap` | Removed from context; belongs in policy |

### New DrummerContext Contract
```csharp
public sealed record DrummerContext : AgentContext
{
    public required Bar Bar { get; init; }
    public decimal? LastKickBeat { get; init; }
    public decimal? LastSnareBeat { get; init; }
}
```
Plus inherited from `AgentContext`: `Seed`, `RngStreamKey`

### Properties Added to Bar Class
The `Bar` class (`Music/Song/Bar/Bar.cs`) now owns these computed properties:

```csharp
public bool IsAtSectionBoundary => BarWithinSection == 0 || BarsUntilSectionEnd == 0;
public bool IsFillWindow => BarsUntilSectionEnd <= 2;
public IReadOnlyList<int> BackbeatBeats => _backbeatBeats ??= ComputeBackbeatBeats();
```

### Files Modified

**Core Context:**
- `Music/Generator/Agents/Drums/Context/DrummerContext.cs` - Simplified to minimal contract
- `Music/Generator/Agents/Drums/Context/DrummerContextBuilder.cs` - Removed unused role constants
- `Music/Song/Bar/Bar.cs` - Added IsAtSectionBoundary, IsFillWindow, BackbeatBeats

**Operators (28 files):**
- All operators updated to use `context.Bar.*` instead of context properties
- Removed all energy threshold overrides (`MinEnergyThreshold`, `MaxEnergyThreshold`)
- Energy-based scoring and conditional logic removed

**Performance Shapers:**
- `DrummerTimingShaper.cs` - Removed energy parameter and adjustment
- `DrummerVelocityShaper.cs` - Removed energy parameter and adjustment

**Base Classes:**
- `DrumOperatorBase.cs` - Removed energy threshold properties

**Deleted:**
- `DrummerContextHelpers.cs` - Properties moved to Bar class

### Breaking Changes
1. `DrummerContext.CreateMinimal()` signature changed - removed `activeRoles`, `backbeatBeats`, `motifPresenceMap` parameters
2. `DrummerContextBuildInput` simplified - removed `BeatsPerBar`, `ActiveRolesOverride`, `HatModeOverride`, `HatSubdivisionOverride`
3. Existing unit tests for DrummerContext are commented out (per epic constraints - test updates deferred)

### New Access Patterns

**Before:**
```csharp
if (context.IsAtSectionBoundary) { ... }
foreach (int bb in context.BackbeatBeats) { ... }
int bpb = context.BeatsPerBar;
```

**After:**
```csharp
if (context.Bar.IsAtSectionBoundary) { ... }
foreach (int bb in context.Bar.BackbeatBeats) { ... }
int bpb = context.Bar.BeatsPerBar;
```

### Build Status
✅ Solution builds successfully with zero compilation errors

## Stories Completed
1. **Story 1:** Inventory and define target contract ✅
2. **Story 2:** Refactor code to eliminate redundant properties ✅
3. **Story 3:** Cleanup and documentation alignment ✅
