# Story G1 Implementation Summary

**Story:** Add Groove Decision Trace (Opt-in, No Behavior Change)  
**Status:** COMPLETE

## Files Created

### 1. `Music\Generator\Groove\GrooveBarDiagnostics.cs`
Structured diagnostics records for groove decision tracing:
- `GrooveBarDiagnostics` - Main record capturing all decision data per bar/role
- `FilterDecision` - Records candidate filtering with reason
- `DensityTargetDiagnostics` - Records density computation inputs and result
- `SelectionDecision` - Records candidate selection with weight and RNG stream
- `PruneDecision` - Records prune events with protection status
- `OnsetListSummary` - Records onset counts at pipeline stages

### 2. `Music\Generator\Groove\GrooveDiagnosticsCollector.cs`
Mutable collector for building diagnostics during pipeline execution:
- `RecordEnabledTags()` - Capture tag resolution
- `RecordCandidatePool()` - Capture candidate counts
- `RecordFilter()` - Capture filter decisions
- `RecordDensityTarget()` - Capture density computation
- `RecordSelection()` - Capture candidate selections with RNG stream
- `RecordPrune()` - Capture prune events with protection status
- `RecordOnsetCounts()` - Capture final onset summary
- `Build()` - Create immutable `GrooveBarDiagnostics`
- Static helpers: `MakeCandidateId()`, `MakeOnsetId()` for stable IDs

### 3. `Music.Tests\Generator\Groove\GrooveBarDiagnosticsTests.cs`
Comprehensive unit tests (25 tests):
- Record creation and field validation
- Collector data recording and building
- Static helper ID generation
- GrooveBarPlan integration (null and populated diagnostics)
- Determinism verification
- RNG stream name recording

## Files Modified

### 1. `Music\Generator\Groove\GrooveBarPlan.cs`
- Changed `Diagnostics` property from `string?` to `GrooveBarDiagnostics?`
- Updated AI comments for Story G1

### 2. `Music\Generator\Groove\GrooveSelectionEngine.cs`
- Added optional `GrooveDiagnosticsCollector? diagnostics` parameter to `SelectUntilTargetReached()`
- Records candidate pool statistics via `RecordCandidatePool()`
- Records filter decisions for anchor conflicts via `RecordFilter()`
- Records selection decisions with weights and RNG stream via `RecordSelection()`
- Updated AI comments for Story G1

### 3. `Music\Generator\Groove\GrooveCapsEnforcer.cs`
- Replaced `List<string>? diagnostics` with `GrooveDiagnosticsCollector? collector` throughout
- Creates per-role collectors when diagnostics enabled
- Records prune decisions with reasons and protection status
- Records onset counts for final summary
- Builds structured `GrooveBarDiagnostics` and attaches to returned `GrooveBarPlan`
- Updated AI comments for Story G1

### 4. `Music.Tests\Generator\Groove\GrooveOutputContractsTests.cs`
- Updated `GrooveBarPlan_SupportsOptionalDiagnostics` test to use structured type

### 5. `Music.Tests\Generator\Groove\CapEnforcementTests.cs`
- Updated `EnforceHardCaps_DiagnosticsEnabled_ProducesDiagnostics` test to verify behavior

## Acceptance Criteria Status

| AC | Status | Notes |
|----|--------|-------|
| 1. Opt-in diagnostics flag | ✅ | `diagnosticsEnabled` parameter in pipeline methods |
| 2.1. Enabled tags | ✅ | `RecordEnabledTags()` captures tags |
| 2.2. Candidate counts | ✅ | `RecordCandidatePool()` in SelectionEngine |
| 2.3. Filters with reasons | ✅ | `RecordFilter()` for anchor conflicts and tag mismatches |
| 2.4. Density target | ✅ | `RecordDensityTarget()` with all inputs |
| 2.5. Selection with weights/RNG | ✅ | `RecordSelection()` with RandomPurpose.ToString() |
| 2.6. Prune events | ✅ | `RecordPrune()` in CapsEnforcer with WasProtected flag |
| 2.7. Final onset summary | ✅ | `RecordOnsetCounts()` + `OnsetListSummary` |
| 3. Zero-cost when disabled | ✅ | Collector is null; Diagnostics is null |
| 4. Diagnostics on/off same output | ✅ | `GrooveBarPlan_WithDiagnostics_DoesNotAffectOnsets` test |

## Test Results
- **New tests:** 25 passing (GrooveBarDiagnosticsTests)
- **Affected tests:** 65 passing (GrooveBarDiagnosticsTests + CapEnforcementTests + SelectionUntilTargetTests)
- **Build:** Successful ✅

## Integration Points

### GrooveSelectionEngine
```csharp
public static IReadOnlyList<GrooveOnsetCandidate> SelectUntilTargetReached(
    GrooveBarContext barContext,
    string role,
    IReadOnlyList<GrooveCandidateGroup> groups,
    int targetCount,
    IReadOnlyList<GrooveOnset> existingAnchors,
    GrooveDiagnosticsCollector? diagnostics = null)  // NEW: Story G1
```

### GrooveCapsEnforcer
```csharp
public GrooveBarPlan EnforceHardCaps(
    GrooveBarPlan barPlan,
    GroovePresetDefinition preset,
    SegmentGrooveProfile? segmentProfile,
    GrooveVariationCatalog? variationCatalog,
    int rngSeed,
    GrooveOverrideMergePolicy? mergePolicy = null,
    bool diagnosticsEnabled = false)  // Returns GrooveBarPlan with Diagnostics populated
```
