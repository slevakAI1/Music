# Story 8.2 Testing Guide

## Running the Tests

Story 8.2 tests are self-contained and can be executed directly:

```csharp
// In any test harness or main entry point:
Music.Tests.Material.MotifStorageTests.RunAllTests();
```

## What is Tested

### Round-trip Conversion Tests
- `MotifSpec` → `PartTrack` → `MotifSpec` preserves all data
- Invalid domain rejection (SongAbsolute instead of MaterialLocal)
- Invalid kind rejection (RoleTrack instead of MaterialFragment)
- Field preservation (name, role, rhythm, tags, etc.)

### MaterialBank Query Tests
- Store and retrieve motifs by PartTrackId
- `GetMotifsByRole()` - filter by intended role (case-insensitive)
- `GetMotifsByMaterialKind()` - filter by material kind (Hook, Riff, etc.)
- `GetMotifByName()` - find by name (case-insensitive)
- Multiple motifs with different roles/kinds return correct subsets

### Validation Tests
- Valid motif tracks pass validation (MaterialLocal + valid MaterialKind)
- Reject wrong domain (must be MaterialLocal)
- Reject invalid MaterialKind (must be one of: Riff, Hook, MelodyPhrase, DrumFill, BassFill, CompPattern, KeysPattern)
- Reject negative ticks (MaterialLocal requires ticks >= 0)

## Expected Output

```
=== Story 8.2: Motif Storage and MaterialBank Tests ===

✓ MotifSpec.ToPartTrack basic conversion
✓ MotifSpec.ToPartTrack preserves all fields
✓ MotifSpec → PartTrack → MotifSpec round-trip preserves data
✓ FromPartTrack rejects invalid domain
✓ FromPartTrack rejects invalid kind
✓ MaterialBank stores and retrieves motifs
✓ GetMotifsByRole returns correct subsets
✓ GetMotifsByMaterialKind returns correct subsets
✓ GetMotifByName finds motifs correctly
✓ All query methods return correct subsets
✓ Validation accepts valid motif track
✓ Validation rejects wrong domain
✓ Validation rejects invalid MaterialKind
✓ Validation rejects negative ticks

✓ All Story 8.2 tests passed!
```

## Test Coverage

All Story 8.2 acceptance criteria are covered:

1. ✓ MaterialBank can store and retrieve motifs
2. ✓ Query methods: GetMotifsByRole, GetMotifsByMaterialKind, GetMotifByName
3. ✓ Conversion helpers: ToPartTrack and FromPartTrack
4. ✓ Validation: domain, kind, and tick constraints
5. ✓ Round-trip preservation: MotifSpec → PartTrack → MotifSpec

## Integration with Existing Tests

To run all material-related tests:

```csharp
// Story M1 tests
Music.Song.Material.Tests.MaterialDefinitionsTests.RunAll();

// Story 8.2 tests
Music.Tests.Material.MotifStorageTests.RunAllTests();
```
