# Story RF-2 Complete: Create GrooveBasedDrumGenerator Pipeline

**Date**: 2025-01-27  
**Status**: ✅ COMPLETE

## Summary

Successfully created `GrooveBasedDrumGenerator` - the pipeline orchestrator that properly uses `IGroovePolicyProvider` + `IGrooveCandidateSource` with `GrooveSelectionEngine` for weighted selection and density enforcement.

## Files Created

### `Music/Generator/Agents/Drums/GrooveBasedDrumGenerator.cs` (378 lines)

#### Settings Record ✅
- `GrooveBasedDrumGeneratorSettings` record
- Properties: `EnableDiagnostics`, `ActiveRoles`, `DefaultVelocity`
- Static `Default` property
- `GetActiveRoles()` helper for defaults

#### Pipeline Class ✅
- `public sealed class GrooveBasedDrumGenerator`
- Constructor validates parameters (throws `ArgumentNullException`)
- Stores policy provider + candidate source + settings

#### Main Generate Method ✅
- `public PartTrack Generate(SongContext songContext)`
- Validates song context (required tracks)
- Extracts: BarTrack, SectionTrack, SegmentGrooveProfiles, GroovePresetDefinition
- Calls helper methods in proper sequence
- Returns `PartTrack` with sorted events

#### Helper Methods Implemented ✅

1. **ExtractAnchorOnsets** (2.4)
   - Loops through all bars
   - Extracts kick/snare/hat onsets from groove preset anchor layer
   - Creates `GrooveOnset` with `IsMustHit = true`
   - Returns sorted list by (bar, beat)

2. **GenerateOperatorOnsets** (2.5)
   - Builds per-bar contexts using `BarContextBuilder.Build()`
   - For each bar+role:
     - Calls `_policyProvider.GetPolicy()` ✅
     - Calculates target count from density ✅
     - Gets candidate groups from `_candidateSource` ✅
     - Filters anchors for this bar+role ✅
     - **Calls `GrooveSelectionEngine.SelectUntilTargetReached()`** ✅
     - Converts selected candidates to `GrooveOnset`
   - Returns combined list

3. **CalculateTargetCount** (2.6)
   - Returns 0 if no policy or no density override (anchor-only)
   - Gets `density01` from policy
   - Base count = 4 (assumes 4/4)
   - Scales by density: `targetCount = (int)(baseCount * density01 * 2.0)`
   - Clamps to [0, maxEventsOverride ?? 16]

4. **CombineOnsets** (2.7)
   - Dictionary keyed by (bar, beat, role)
   - Adds anchors first (they win conflicts)
   - Adds operators, skipping conflicts
   - Returns sorted by bar, then beat

5. **ConvertToPartTrack** (2.8)
   - Converts `GrooveOnset` list to `PartTrack`
   - Gets absolute tick from `BarTrack.ToTick()`
   - Applies timing offset if present
   - Maps role to MIDI note via `MapRoleToMidiNote()`
   - Creates `PartTrackEvent` with proper fields
   - **CRITICAL: Sorts events by AbsoluteTimeTicks** ✅
   - Sets MidiProgramNumber

6. **GetDrumProgramNumber** (2.9)
   - Looks up "DrumKit" voice from VoiceSet
   - Returns MIDI program number or 255 default

7. **MapRoleToMidiNote** (2.9)
   - Switch on role: Kick→36, Snare→38, ClosedHat→42, etc.
   - Defaults to 38 (snare) for unknown roles

8. **ValidateSongContext**
   - Validates BarTrack, SectionTrack, GroovePresetDefinition
   - Throws `ArgumentException` for missing required data

## Key Design Decisions

### Proper Groove System Integration ✅
- Uses `GrooveSelectionEngine.SelectUntilTargetReached()` for candidate selection
- Passes `IReadOnlyList<GrooveOnset>` anchors (not HashSet<decimal>)
- Respects density targets from policy
- Enforces caps via GrooveSelectionEngine

### Anchor Preservation ✅
- Anchors marked with `IsMustHit = true`
- Anchors added to dictionary first in `CombineOnsets()`
- Operators cannot overwrite anchor positions

### Velocity and Timing Flow ✅
- Uses `candidate.VelocityHint` → `GrooveOnset.Velocity`
- Uses `candidate.TimingHint` → `GrooveOnset.TimingOffsetTicks`
- Falls back to `DefaultVelocity` (100) if hints not set

### MIDI Note Mapping ✅
- Centralized in `MapRoleToMidiNote()` static method
- Uses standard GM2 drum map
- Safe fallback to snare (38) for unknown roles

## Build Status

### Successful Compilation ✅
`GrooveBasedDrumGenerator.cs` compiles without errors.

### Expected Build Errors (To be fixed in RF-3 and RF-4)
Two files still have errors referencing the removed `DrummerAgent.Generate()`:
1. `Generator.cs` (line 51) - **Fix in RF-3**
2. `DrumTrackGenerator.cs` (line 75) - **Fix in RF-4**

These errors are correct and expected - they'll be fixed by wiring up the new pipeline.

## Acceptance Criteria Status

| Section | Criterion | Status |
|---------|-----------|--------|
| 2.1 | Create pipeline class | ✅ DONE |
| 2.1 | Constructor validates parameters | ✅ DONE |
| 2.2 | Settings record defined | ✅ DONE |
| 2.3 | Generate method signature | ✅ DONE |
| 2.4 | Anchor extraction | ✅ DONE |
| 2.5 | Operator selection pipeline | ✅ DONE |
| 2.5 | Uses GrooveSelectionEngine | ✅ DONE |
| 2.6 | Target count calculation | ✅ DONE |
| 2.7 | Onset combination | ✅ DONE |
| 2.8 | MIDI conversion | ✅ DONE |
| 2.8 | Events sorted by AbsoluteTimeTicks | ✅ DONE |
| 2.9 | Helper methods | ✅ DONE |

## Architecture Correctness

### Before (Wrong) ❌
```
DrummerAgent
  ├── Generate() [FULL PIPELINE]
  ├── No GrooveSelectionEngine
  ├── Returns ALL candidates
  └── No density enforcement
```

### After (Correct) ✅
```
GrooveBasedDrumGenerator
  ├── Uses IGroovePolicyProvider (DrummerAgent)
  ├── Uses IGrooveCandidateSource (DrummerAgent)
  ├── Calls GrooveSelectionEngine.SelectUntilTargetReached()
  ├── Enforces density targets from policy
  ├── Respects operator caps
  └── Weighted selection via groove system
```

## Next Steps

**Ready for Story RF-3**: Update Generator.cs Integration

The new pipeline will be wired into the top-level Generator in RF-3, fixing the first build error.

---

**Estimated Effort**: 2 hours (actual: 1.5 hours)  
**Critical Path**: ✅ Unblocks RF-3  
**Lines of Code**: 378 lines
