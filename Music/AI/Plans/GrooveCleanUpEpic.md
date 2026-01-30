# Groove Cleanup Epic: Remove Variation Infrastructure

**Purpose:** Remove groove-level variation logic and consolidate all variation responsibility in the Drummer Agent. This simplifies the architecture, makes debugging easier, and aligns with the "human drummer mental model" where all decisions are context-aware.

**Architectural Decision:** Groove system becomes a constraint definition (valid positions), not a decision maker. All variation lives in the Part Generator (Drummer Agent).

---

## Variation Case Coverage Verification

The `GrooveInstanceLayer.CreateVariation()` method handled these cases:

| Variation | Position | Probability | Drummer Agent Operator |
|-----------|----------|-------------|------------------------|
| Kick doubles | 1.5, 3.5 | 50% each | ✅ `KickDoubleOperator` (same positions) |
| Hat 16th subdivision | 1.25, 2.25, 3.25, 4.25 | 30% | ✅ `HatLiftOperator` (full 16th pattern) |
| Kick syncopation | 1.75, 3.75 | 20% | ✅ `KickDoubleOperator` (16th variants at high energy) |

**All variation cases are already covered by existing operators.** No new operators needed.

---

## Story GC-1: Delete Groove Variation Catalog Infrastructure

**Size:** Small (4 files, ~200 lines)

**Goal:** Remove the catalog-based variation system that is no longer needed.

### Files to Delete
1. `Music\Generator\Groove\GrooveVariationCatalog.cs`
2. `Music\Generator\Groove\GrooveVariationLayer.cs`
3. `Music\Generator\Groove\GrooveVariationLayerMerger.cs`

### Files to Delete (Recently Created Generic Types)
4. `Music\Generator\Groove\OnsetCandidate.cs`
5. `Music\Generator\Groove\CandidateGroup.cs`

### Implementation Steps
1. Delete all 5 files listed above
2. Run build to identify any remaining references
3. If references exist in test files, delete those test files (they test deleted functionality)

### Expected Test File Deletions
- `Music.Tests\Generator\Groove\VariationLayerMergeTests.cs` - tests deleted merger

### Acceptance Criteria
- [x] All 5 source files deleted
- [x] `VariationLayerMergeTests.cs` deleted
- [x] Build identifies remaining references (expected - to be fixed in GC-3, GC-4, GC-5)
- [x] Remaining references: `GrooveOnsetFactory.FromVariation`, `DrumOnsetCandidate` conversion methods, `DrumCandidateGroup` conversion methods

### Notes
- Do NOT write new unit tests for this story
- This is deletion only - no new code

---

## Story GC-2: Remove CreateVariation Method from GrooveInstanceLayer

**Size:** Small (1 file, ~100 lines removed)

**Goal:** Remove the variation generation logic from `GrooveInstanceLayer` since variation is now handled by Drummer Agent operators.

### File to Modify
`Music\Generator\Groove\GrooveInstanceLayer.cs`

### Methods to Delete
```csharp
// Delete this method and its helpers (lines ~75-153):
public static GrooveInstanceLayer CreateVariation(GrooveInstanceLayer anchor, int seed)
private static void ApplyKickDoubles(GrooveInstanceLayer variation)
private static void ApplyHatSubdivision(GrooveInstanceLayer variation)
private static void ApplySyncopation(GrooveInstanceLayer variation)
```

### Implementation Steps
1. Open `Music\Generator\Groove\GrooveInstanceLayer.cs`
2. Delete the `CreateVariation` method (public static)
3. Delete the `ApplyKickDoubles` method (private static)
4. Delete the `ApplyHatSubdivision` method (private static)
5. Delete the `ApplySyncopation` method (private static)
6. Remove any using directives that become unused after deletion
7. Run build to identify any callers of `CreateVariation`
8. Delete or update test files that call `CreateVariation`

### Expected Test File Updates
- `Music.Tests\Generator\Groove\GrooveInstanceLayerVariationTests.cs` - DELETE this file (tests deleted functionality)

### Acceptance Criteria
- [x] `CreateVariation` and all 3 helper methods deleted from `GrooveInstanceLayer.cs`
- [x] `GrooveInstanceLayerVariationTests.cs` deleted
- [x] Build shows no new errors (same expected errors from GC-1 remain)
- [x] No calls to `CreateVariation` remain in codebase

### Notes
- Do NOT write new unit tests for this story
- Keep `ToPartTrack` method - it's still useful for audition/preview
- Keep all onset query methods (`GetOnsets`, `GetActiveRoles`, `HasRole`)

---

## Story GC-3: Simplify GrooveOnsetFactory

**Size:** Small (1 file, ~30 lines changed)

**Goal:** Remove the `FromVariation` method that takes `OnsetCandidate`/`CandidateGroup` since these types are being deleted. Keep only `FromAnchor` and `WithUpdatedProperties`.

### File to Modify
`Music\Generator\Groove\GrooveOnsetFactory.cs`

### Current State (after recent refactoring)
```csharp
public static class GrooveOnsetFactory
{
    public static GrooveOnset FromAnchor(...) // KEEP
    public static GrooveOnset FromVariation(OnsetCandidate, CandidateGroup, ...) // DELETE
    public static GrooveOnset WithUpdatedProperties(...) // KEEP
}
```

### Implementation Steps
1. Open `Music\Generator\Groove\GrooveOnsetFactory.cs`
2. Delete the `FromVariation` method entirely
3. Update AI comments to reflect new purpose
4. Run build to identify callers
5. Update or delete test files that call `FromVariation` with generic types

### Final State
```csharp
// AI: purpose=Factory for creating GrooveOnset from anchors; variation handled by DrumGrooveOnsetFactory.
public static class GrooveOnsetFactory
{
    public static GrooveOnset FromAnchor(...)
    public static GrooveOnset WithUpdatedProperties(...)
}
```

### Expected Test File Updates
- `Music.Tests\Generator\Groove\GrooveOnsetProvenanceTests.cs` - Remove tests that use `GrooveOnsetFactory.FromVariation`. Keep tests for `FromAnchor` and `WithUpdatedProperties`.

### Acceptance Criteria
- [x] `FromVariation` method deleted from `GrooveOnsetFactory.cs`
- [x] AI comments updated to reflect new purpose
- [x] Build shows only expected errors from GC-1 (DrumOnsetCandidate and DrumCandidateGroup conversion methods)
- [x] Provenance tests require no changes (all use `DrumGrooveOnsetFactory.FromVariation`)

### Notes
- Do NOT write new unit tests for this story
- `DrumGrooveOnsetFactory` in Drums namespace still has its own `FromVariation` for drum-specific use

---

## Story GC-4: Clean Up Drum Type Conversion Methods

**Size:** Small (2 files, ~60 lines changed)

**Goal:** Remove the `ToOnsetCandidate`/`ToCandidateGroup` conversion methods from drum types since the generic types are being deleted. Keep `FromOnsetCandidate`/`FromCandidateGroup` static methods if any code uses them, otherwise delete those too.

### Files to Modify
1. `Music\Generator\Agents\Drums\DrumOnsetCandidate.cs`
2. `Music\Generator\Agents\Drums\DrumCandidateGroup.cs`

### Methods to Evaluate for Deletion

In `DrumOnsetCandidate.cs`:
```csharp
public static DrumOnsetCandidate FromOnsetCandidate(OnsetCandidate candidate) // DELETE - generic type gone
public OnsetCandidate ToOnsetCandidate() // DELETE - generic type gone
```

In `DrumCandidateGroup.cs`:
```csharp
public static DrumCandidateGroup FromCandidateGroup(CandidateGroup group) // DELETE - generic type gone
public CandidateGroup ToCandidateGroup() // DELETE - generic type gone
```

### Implementation Steps
1. Delete all 4 conversion methods listed above
2. Remove `using Music.Generator.Groove;` if no longer needed
3. Update AI comments to reflect simplified purpose
4. Run build to identify any callers
5. Fix any build errors (likely in `DrumGrooveOnsetFactory`)

### Acceptance Criteria
- [x] All 4 conversion methods deleted from both files
- [x] Build shows only expected errors in `DrumGrooveOnsetFactory` (to be fixed in GC-5)
- [x] AI comments updated to reflect simplified purpose
- [x] Additional cleanup: `GrooveAnchorFactory.Generate()` removed, `Generator.cs` updated

### Notes
- Do NOT write new unit tests for this story
- Story GC-5 will fix `DrumGrooveOnsetFactory` which calls these methods

---

## Story GC-5: Update DrumGrooveOnsetFactory to Not Use Generic Types

**Size:** Small (1 file, ~40 lines changed)

**Goal:** Update `DrumGrooveOnsetFactory.FromVariation` to create `GrooveOnset` directly instead of converting to generic types and calling `GrooveOnsetFactory.FromVariation`.

### File to Modify
`Music\Generator\Agents\Drums\DrumGrooveOnsetFactory.cs`

### Current Implementation (broken after GC-4)
```csharp
public static GrooveOnset FromVariation(DrumOnsetCandidate candidate, DrumCandidateGroup group, ...)
{
    return GrooveOnsetFactory.FromVariation(
        candidate.ToOnsetCandidate(),  // ERROR: method deleted
        group.ToCandidateGroup(),       // ERROR: method deleted
        barNumber,
        enabledTags);
}
```

### New Implementation
```csharp
// AI: purpose=Creates GrooveOnset from drum variation candidate with provenance tracking.
public static GrooveOnset FromVariation(
    DrumOnsetCandidate candidate,
    DrumCandidateGroup group,
    int barNumber,
    IReadOnlyList<string>? enabledTags = null)
{
    ArgumentNullException.ThrowIfNull(candidate);
    ArgumentNullException.ThrowIfNull(group);

    string candidateId = GrooveOnsetProvenance.MakeCandidateId(group.GroupId, candidate.OnsetBeat);

    return new GrooveOnset
    {
        Role = candidate.Role,
        BarNumber = barNumber,
        Beat = candidate.OnsetBeat,
        Strength = candidate.Strength,
        Velocity = candidate.VelocityHint,
        TimingOffsetTicks = candidate.TimingHint,
        Provenance = GrooveOnsetProvenance.ForVariation(group.GroupId, candidateId, enabledTags)
    };
}

// FromWeightedCandidate stays the same - just calls FromVariation
```

### Implementation Steps
1. Open `Music\Generator\Agents\Drums\DrumGrooveOnsetFactory.cs`
2. Replace `FromVariation` implementation to create `GrooveOnset` directly
3. Add `using Music.Generator.Groove;` if not present (for `GrooveOnsetProvenance`)
4. Update AI comments
5. Run build to verify

### Acceptance Criteria
- [x] `FromVariation` creates `GrooveOnset` directly without calling generic factory
- [x] No references to deleted generic types
- [x] Build succeeds with no errors
- [x] Test file `GrooveInstanceLayerToPartTrackTests.cs` updated to use `GetAnchor()` instead of `Generate()`

### Notes
- Do NOT write new unit tests for this story
- This completes the decoupling from groove generic types

---

## Story GC-6: Delete Provenance Tests for Deleted Functionality

**Size:** Small (1 file, selective deletion)

**Goal:** Clean up `GrooveOnsetProvenanceTests.cs` by removing tests that use `DrumGrooveOnsetFactory.FromVariation` if the provenance tracking for variations is no longer testable at this level, OR keep them if they still work with the updated implementation.

### File to Evaluate
`Music.Tests\Generator\Groove\GrooveOnsetProvenanceTests.cs`

### Decision Logic
After GC-5 is complete:
1. Run `dotnet test --filter GrooveOnsetProvenance` 
2. If tests pass → KEEP them (the `DrumGrooveOnsetFactory` still creates provenance correctly)
3. If tests fail → Analyze failures and fix or delete individual tests

### Tests to Review
- Tests calling `DrumGrooveOnsetFactory.FromVariation` - should still work
- Tests calling `DrumGrooveOnsetFactory.FromWeightedCandidate` - should still work
- Tests calling `GrooveOnsetFactory.FromVariation` with generic types - DELETE (types gone)

### Implementation Steps
1. Run the provenance tests after GC-1 through GC-5 are complete
2. Fix or delete failing tests based on analysis
3. Ensure remaining tests pass

### Acceptance Criteria
- [x] All remaining provenance tests pass (26/26 tests passing)
- [x] No tests for deleted functionality found (all tests already using correct APIs)
- [x] Tests for `FromAnchor` and `WithUpdatedProperties` preserved

### Notes
- Do NOT write new unit tests for this story
- This is cleanup only

---

## Story GC-7: Remove Unused RandomPurpose Value

**Size:** Tiny (1 file, 1 line)

**Goal:** Remove `GrooveVariationGroupPick` from `RandomPurpose` enum if no longer used.

### File to Modify
`Music\Generator\Core\Randomization\RandomPurpose.cs` (or wherever the enum is defined)

### Implementation Steps
1. Search codebase for `GrooveVariationGroupPick`
2. If no usages remain after previous stories, delete the enum value
3. If usages remain in other code (unlikely), keep it

### Acceptance Criteria
- [x] `GrooveVariationGroupPick` removed if unused
- [x] Build succeeds
- [x] All tests pass (1091/1091)
- [x] Also removed `VariationGroupPick` from `GrooveRngStreamKey` and related mappings

### Notes
- Do NOT write new unit tests for this story
- Skip this story if the enum value is still used

---

## Execution Order

Execute stories in order: GC-1 → GC-2 → GC-3 → GC-4 → GC-5 → GC-6 → GC-7

Each story should compile successfully before proceeding to the next.

---

## Post-Epic State

### Groove Namespace Contains (Simplified)
- `GrooveInstanceLayer.cs` - Anchor onset lists only (no variation)
- `GroovePresetDefinition.cs` - Identity + anchor layer
- `GrooveOnset.cs` - Onset record type
- `GrooveOnsetFactory.cs` - `FromAnchor` and `WithUpdatedProperties` only
- `GrooveOnsetProvenance.cs` - Provenance tracking
- `GrooveRoles.cs` - Role constants
- `OnsetGrid.cs` / `OnsetGridBuilder.cs` - Valid position grid
- `OnsetStrength.cs` / `OnsetStrengthClassifier.cs` - Beat hierarchy
- `FeelTimingEngine.cs` / `RoleTimingEngine.cs` - Micro-timing
- `AllowedSubdivision.cs` - Subdivision policy

### Drums Namespace Contains (Variation Owners)
- `DrumGrooveOnsetFactory.cs` - Creates `GrooveOnset` from drum candidates
- `DrumOnsetCandidate.cs` - Drum candidate (no generic conversion)
- `DrumCandidateGroup.cs` - Drum candidate group (no generic conversion)
- All 28 operators handling all variation decisions
- `DrummerContext`, `DrummerMemory`, etc.

### Deleted Files
- `GrooveVariationCatalog.cs`
- `GrooveVariationLayer.cs`
- `GrooveVariationLayerMerger.cs`
- `OnsetCandidate.cs`
- `CandidateGroup.cs`
- `VariationLayerMergeTests.cs`
- `GrooveInstanceLayerVariationTests.cs`

---

## Future Epic (Not in Scope)

**Wiring Drummer Agent Back In**: A separate epic will handle:
- Integrating drummer agent with the generation pipeline
- Connecting operators to produce final output
- End-to-end testing with audio verification

This cleanup epic focuses solely on removing the groove variation infrastructure.
