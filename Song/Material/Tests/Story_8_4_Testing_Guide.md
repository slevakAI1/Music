# Story 8.4 Testing Guide

## Running the Tests

Story 8.4 tests are self-contained and can be executed directly:

```csharp
// In any test harness or main entry point:
Music.Song.Material.Tests.MotifValidationTests.RunAll();
```

## What is Tested

### Valid Motif Tests
- Valid motif with all correct fields passes validation (returns empty list)
- All test motifs from MotifLibrary pass validation

### Invalid Name/Role Tests
- Empty name produces validation issue mentioning "Name"
- Empty role produces validation issue mentioning "IntendedRole"

### Invalid Rhythm Tests
- Empty rhythm shape produces validation issue
- Negative rhythm ticks produce validation issue mentioning "negative"
- Ticks beyond reasonable bar length (> 8 bars at 480 PPQN) produce validation issue

### Invalid Register Tests
- Center MIDI below 21 (A0) produces validation issue
- Center MIDI above 108 (C8) produces validation issue
- Negative range semitones produce validation issue mentioning "positive"
- Zero range semitones produce validation issue mentioning "positive"
- Range semitones > 24 (2 octaves) produce validation issue mentioning "reasonable"

### Invalid Tone Policy Tests
- Chord tone bias < 0.0 produces validation issue
- Chord tone bias > 1.0 produces validation issue

### Determinism Test
- Same invalid motif produces identical validation issues on multiple calls

## Expected Output

```
=== Story 8.4: MotifValidation Tests ===

  ✓ Valid motif passes validation
  ✓ All 4 test motifs from MotifLibrary pass validation
  ✓ Empty name fails validation
  ✓ Empty role fails validation
  ✓ Empty rhythm shape fails validation
  ✓ Negative rhythm ticks fail validation
  ✓ Too large rhythm ticks fail validation
  ✓ Center MIDI too low fails validation
  ✓ Center MIDI too high fails validation
  ✓ Negative range semitones fail validation
  ✓ Zero range semitones fail validation
  ✓ Too large range semitones fail validation
  ✓ Chord tone bias too low fails validation
  ✓ Chord tone bias too high fails validation
  ✓ Validation is deterministic

✓ All Story 8.4 MotifValidation tests passed!
```

## Validation Rules

### MotifValidation.ValidateMotif(MotifSpec)

Returns `IReadOnlyList<string>` of validation issues (empty if valid).

**Field Constraints:**

| Field | Constraint | Error Message Includes |
|-------|-----------|------------------------|
| `Name` | Must not be empty | "Name" |
| `IntendedRole` | Must not be empty | "IntendedRole" |
| `RhythmShape` | Must have at least one onset | "RhythmShape" |
| `RhythmShape` ticks | Must be >= 0 | "negative" |
| `RhythmShape` ticks | Must be <= 3840 (8 bars at 480 PPQN) | "bar length" |
| `Register.CenterMidiNote` | Must be in [21..108] | "CenterMidiNote", "MIDI range" |
| `Register.RangeSemitones` | Must be > 0 | "RangeSemitones", "positive" |
| `Register.RangeSemitones` | Must be <= 24 | "RangeSemitones", "reasonable" |
| `TonePolicy.ChordToneBias` | Must be in [0.0..1.0] | "ChordToneBias" |

## Usage Example

```csharp
var motif = MotifSpec.Create(
    name: "My Motif",
    intendedRole: "Lead",
    kind: MaterialKind.Hook,
    rhythmShape: new List<int> { 0, 240, 480 },
    contour: ContourIntent.Arch,
    centerMidiNote: 60,
    rangeSemitones: 12,
    chordToneBias: 0.8,
    allowPassingTones: true);

var issues = MotifValidation.ValidateMotif(motif);

if (issues.Count > 0)
{
    Console.WriteLine("Validation failed:");
    foreach (var issue in issues)
    {
        Console.WriteLine($"  - {issue}");
    }
}
else
{
    Console.WriteLine("Motif is valid!");
}
```

## Design Notes

- **Non-throwing**: Returns issues list instead of throwing exceptions
- **Parallel to PartTrackMaterialValidation**: Same pattern as Story M1 validation
- **Deterministic**: Same motif → same validation results
- **Read-only**: Validation never modifies the motif
- **Clear messages**: Each issue describes what's wrong and which field

## Integration with MotifSpec.Create()

Note that `MotifSpec.Create()` already clamps values to safe ranges (e.g., negative ticks become 0, out-of-range MIDI notes clamped to [21..108]). Therefore:

- If you construct a motif via `MotifSpec.Create()`, most validation issues are prevented by clamping
- If you construct a motif via the record constructor (bypassing Create), validation may catch issues
- Validation is still useful for:
  - Motifs loaded from external sources (files, network)
  - Detecting when clamping occurred (e.g., did user intend negative ticks?)
  - Verifying motifs before Stage 9 placement/rendering

## Next Step

Story 8.5 will create comprehensive tests for the entire motif data layer (MotifSpec, conversion, MaterialBank, validation).
