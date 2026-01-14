# Story 8.5 Testing Guide

## Overview

Story 8.5 provides comprehensive integration tests for the entire motif data layer (Stories 8.1-8.4), ensuring everything works together correctly before Stage 9 placement work begins.

## Running the Tests

Story 8.5 tests can be executed directly:

```csharp
// Comprehensive data layer tests only:
Music.Song.Material.Tests.MotifDefinitionsTests.RunAll();

// Or run all Stage 8 tests (includes 8.5):
Music.Song.Material.Tests.Stage8TestRunner.RunAllStage8Tests();
```

## Test Coverage

### 1. MotifSpec Creation and Immutability (Story 8.1)
- **TestMotifSpecCreation**: Verifies MotifSpec.Create() produces valid motif with all fields set
- **TestMotifSpecImmutability**: Confirms MotifSpec is properly immutable (record type)
- **TestMotifSpecFactoryMethodClamping**: Validates that Create() clamps invalid values to safe ranges

### 2. MotifSpec ↔ PartTrack Conversion (Story 8.2)
- **TestMotifSpecToPartTrackConversion**: Verifies ToPartTrack() sets correct Meta fields and domain
- **TestPartTrackToMotifSpecConversion**: Confirms FromPartTrack() reconstructs MotifSpec correctly
- **TestMotifSpecRoundTripPreservesData**: Ensures full round-trip preserves all data
- **TestMotifConversionHandlesInvalidDomain**: Validates rejection of tracks with wrong domain

### 3. MaterialBank Storage and Retrieval (Story 8.2)
- **TestMaterialBankStoresMotif**: Confirms motifs can be added to MaterialBank
- **TestMaterialBankRetrievesMotif**: Verifies TryGet() retrieves correct motif by ID
- **TestGetMotifsByRoleFiltersCorrectly**: Tests role-based filtering
- **TestGetMotifsByKindFiltersCorrectly**: Tests MaterialKind-based filtering
- **TestGetMotifByNameFindsCorrectMotif**: Tests name-based lookup

### 4. Hardcoded Test Motifs (Story 8.3)
- **TestAllHardcodedMotifsAreValid**: All motifs from MotifLibrary pass validation
- **TestHardcodedMotifsDeterministic**: Same factory method produces consistent results
- **TestHardcodedMotifsHaveUniqueIds**: Different motifs get different IDs
- **TestHardcodedMotifsCanBeStored**: All motifs can be stored in MaterialBank

### 5. Validation Error Detection (Story 8.4)
- **TestValidationCatchesEmptyName**: Detects empty name field
- **TestValidationCatchesEmptyRole**: Detects empty role field
- **TestValidationCatchesInvalidRhythm**: Detects negative rhythm ticks
- **TestValidationCatchesInvalidRegister**: Detects out-of-range MIDI notes
- **TestValidationCatchesInvalidTonePolicy**: Detects invalid chord tone bias

### 6. Overall System Determinism
- **TestEntireMotifSystemIsDeterministic**: Verifies entire workflow is deterministic (same inputs → same outputs except generated IDs)

## Expected Output

```
=== Story 8.5: Comprehensive Motif Data Layer Tests ===

  ✓ MotifSpec creation works correctly
  ✓ MotifSpec is properly immutable
  ✓ MotifSpec.Create clamps invalid values
  ✓ MotifSpec → PartTrack conversion works
  ✓ PartTrack → MotifSpec conversion works
  ✓ MotifSpec → PartTrack → MotifSpec round-trip preserves data
  ✓ Conversion rejects invalid domain
  ✓ MaterialBank stores motifs
  ✓ MaterialBank retrieves motifs
  ✓ GetMotifsByRole filters correctly
  ✓ GetMotifsByKind filters correctly
  ✓ GetMotifByName finds correct motif
  ✓ All 4 hardcoded motifs are valid
  ✓ Hardcoded motifs are deterministic
  ✓ Hardcoded motifs have unique IDs
  ✓ All 4 hardcoded motifs can be stored
  ✓ Validation catches empty name
  ✓ Validation catches empty role
  ✓ Validation catches invalid rhythm
  ✓ Validation catches invalid register
  ✓ Validation catches invalid tone policy
  ✓ Entire motif system is deterministic

✓✓✓ All Story 8.5 comprehensive tests passed! ✓✓✓
Motif data layer is solid and ready for Stage 9.
```

## What This Validates

### Data Integrity
- MotifSpec fields are properly initialized and immutable
- Conversion between MotifSpec and PartTrack preserves all data
- MaterialBank correctly stores and retrieves motifs

### Musical Correctness
- All hardcoded test motifs have valid structure
- Validation catches common musical errors
- Register, rhythm, and tone policy constraints are enforced

### Determinism
- Same factory methods produce consistent results (except unique IDs)
- Validation results are deterministic
- Round-trip conversions are deterministic

### Readiness for Stage 9
- Motifs can be stored in MaterialBank
- Motifs can be queried by role, kind, and name
- All test fixtures are valid and ready for placement/rendering

## Acceptance Criteria Met

### Story 8.5 Requirements ✓
- [x] Created `Song/Material/Tests/MotifDefinitionsTests.cs`
- [x] Parallel structure to `MaterialDefinitionsTests.cs`
- [x] Test coverage:
  - [x] MotifSpec creation and immutability (3 tests)
  - [x] MotifSpec ↔ PartTrack round-trip (4 tests)
  - [x] MaterialBank storage and retrieval (5 tests)
  - [x] Hardcoded test motifs validity and determinism (4 tests)
  - [x] Validation catches common errors (5 tests)
- [x] All tests pass
- [x] All tests verify determinism where applicable (1 comprehensive test + determinism checks in others)

## Integration with Existing Tests

Story 8.5 consolidates and validates the work from:
- **Story 8.1**: MotifSpec model
- **Story 8.2**: Conversion and storage (MotifStorageTests)
- **Story 8.3**: Hardcoded motifs (MotifLibraryTests)
- **Story 8.4**: Validation helpers (MotifValidationTests)

While those individual story tests remain useful for focused testing, Story 8.5 provides comprehensive integration validation ensuring the entire system works together correctly.

## Next Step

With Story 8.5 complete, **Stage 8 (Material motifs data layer) is complete**. The motif data layer is solid, tested, and ready for **Stage 9 (Motif placement and rendering)**.

Stage 9 will add:
- **Story 9.1**: MotifPlacementPlanner (where motifs appear)
- **Story 9.2**: MotifRenderer (notes from motif spec + harmony)
- **Story 9.3**: Motif integration with accompaniment (ducking)
- **Story 9.4**: Motif diagnostics
