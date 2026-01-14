# Story 8.0.1 Testing Guide

## Running the Tests

The test file `CompBehaviorTests.cs` follows the internal static test class pattern used in the codebase.

### Test Location
```
Generator\Guitar\CompBehaviorTests.cs
```

### Running Tests

#### Option 1: Call from existing test runner
If the project has a test runner that discovers and runs internal static test classes, add to the runner:

```csharp
CompBehaviorTests.RunAllTests();
```

#### Option 2: Manual invocation
Add to any test entry point or console application:

```csharp
using Music.Generator.Tests;

// Run all Story 8.0.1 tests
CompBehaviorTests.RunAllTests();
```

#### Option 3: Individual test execution
Run specific tests:

```csharp
CompBehaviorTests.Test_Determinism_SameInputs_SameBehavior();
CompBehaviorTests.Test_DifferentSections_ProduceDifferentBehaviors();
// ... etc
```

---

## Expected Test Output

```
Running Story 8.0.1 CompBehavior tests...
  ? Determinism: Same inputs ? SyncopatedChop
  ? Different sections: Verse=Anticipate, Chorus=SyncopatedChop
  ? Seed variation: Seed=10 ? Standard, Seed=50 ? Standard
  ? Energy affects behavior: 0.2 ? SparseAnchors, 0.9 ? SyncopatedChop
  ? BusyProb affects behavior: 0.1 ? Standard, 0.95 ? SyncopatedChop
  ? Verse mapping: Sparse/Std/Anticipate/Syncopated correct
  ? Chorus mapping: Std/Anticipate/Syncopated/Driving correct
  ? Variation timing: bar0=Standard, bar4=Standard, bar8=Standard
  ? Variation logic: boundaries behave correctly
  ? Edge case: Zero energy ? SparseAnchors
  ? Edge case: Max energy ? DrivingFull
  ? Edge case: First bar (bar=0) ? Standard (no variation)
? All Story 8.0.1 CompBehavior tests passed.
```

---

## Test Coverage Summary

| Test Category | Tests | Purpose |
|--------------|-------|---------|
| Determinism | 1 | Verify repeatability |
| Section Differentiation | 1 | Verify audible contrast |
| Seed Sensitivity | 1 | Verify variation within constraints |
| Energy Mapping | 1 | Verify correct behavior selection |
| BusyProb Mapping | 1 | Verify activity score influence |
| Verse Mapping | 1 | Verify section-specific thresholds |
| Chorus Mapping | 1 | Verify section-specific thresholds |
| Variation Timing | 1 | Verify every-4th-bar logic |
| Variation Logic | 1 | Verify upgrade/downgrade |
| Edge Cases | 3 | Verify boundaries and first bar |
| **Total** | **12** | **Comprehensive coverage** |

---

## Manual Verification Examples

### Example 1: Verse Energy Progression
```csharp
// Low energy verse ? SparseAnchors
var v1 = CompBehaviorSelector.SelectBehavior(
    MusicConstants.eSectionType.Verse, 0, 0, 
    energy: 0.2, busyProbability: 0.5, seed: 100);
Console.WriteLine($"Verse low energy: {v1}"); // ? SparseAnchors

// Mid energy verse ? Standard
var v2 = CompBehaviorSelector.SelectBehavior(
    MusicConstants.eSectionType.Verse, 0, 0, 
    energy: 0.5, busyProbability: 0.5, seed: 100);
Console.WriteLine($"Verse mid energy: {v2}"); // ? Standard

// High energy verse ? Anticipate or SyncopatedChop
var v3 = CompBehaviorSelector.SelectBehavior(
    MusicConstants.eSectionType.Verse, 0, 0, 
    energy: 0.9, busyProbability: 0.5, seed: 100);
Console.WriteLine($"Verse high energy: {v3}"); // ? SyncopatedChop
```

### Example 2: Chorus Energy Progression
```csharp
// Low energy chorus ? Standard
var c1 = CompBehaviorSelector.SelectBehavior(
    MusicConstants.eSectionType.Chorus, 0, 0, 
    energy: 0.2, busyProbability: 0.5, seed: 100);
Console.WriteLine($"Chorus low energy: {c1}"); // ? Standard

// High energy chorus ? DrivingFull
var c2 = CompBehaviorSelector.SelectBehavior(
    MusicConstants.eSectionType.Chorus, 0, 0, 
    energy: 0.95, busyProbability: 0.5, seed: 100);
Console.WriteLine($"Chorus high energy: {c2}"); // ? DrivingFull
```

### Example 3: Seed Variation
```csharp
// Different seeds at variation point (bar 4)
var seed1_bar4 = CompBehaviorSelector.SelectBehavior(
    MusicConstants.eSectionType.Verse, 0, 4, 
    energy: 0.5, busyProbability: 0.5, seed: 10);

var seed2_bar4 = CompBehaviorSelector.SelectBehavior(
    MusicConstants.eSectionType.Verse, 0, 4, 
    energy: 0.5, busyProbability: 0.5, seed: 999);

Console.WriteLine($"Seed 10 bar 4: {seed1_bar4}");
Console.WriteLine($"Seed 999 bar 4: {seed2_bar4}");
// May be same or different due to 30% variation chance
```

### Example 4: Per-Bar Variation Timing
```csharp
const int seed = 42;
const double energy = 0.5;
const double busyProb = 0.5;

for (int bar = 0; bar < 12; bar++)
{
    var behavior = CompBehaviorSelector.SelectBehavior(
        MusicConstants.eSectionType.Verse, 0, bar, 
        energy, busyProb, seed);
    
    string variationFlag = (bar > 0 && bar % 4 == 0) ? "*" : " ";
    Console.WriteLine($"Bar {bar,2}: {variationFlag} {behavior}");
}
// * marks bars eligible for variation (4, 8, etc.)
```

---

## Integration Test (Simulated Generation)

```csharp
// Simulate a song with multiple sections
var sections = new[]
{
    (Type: MusicConstants.eSectionType.Intro, Bars: 4, Energy: 0.3, Busy: 0.4),
    (Type: MusicConstants.eSectionType.Verse, Bars: 8, Energy: 0.4, Busy: 0.5),
    (Type: MusicConstants.eSectionType.Chorus, Bars: 8, Energy: 0.8, Busy: 0.7),
    (Type: MusicConstants.eSectionType.Verse, Bars: 8, Energy: 0.5, Busy: 0.6),
    (Type: MusicConstants.eSectionType.Chorus, Bars: 8, Energy: 0.85, Busy: 0.75),
    (Type: MusicConstants.eSectionType.Bridge, Bars: 8, Energy: 0.7, Busy: 0.65),
    (Type: MusicConstants.eSectionType.Chorus, Bars: 8, Energy: 0.9, Busy: 0.8),
    (Type: MusicConstants.eSectionType.Outro, Bars: 4, Energy: 0.3, Busy: 0.4)
};

const int seed = 123;
int absoluteSectionIndex = 0;

foreach (var section in sections)
{
    Console.WriteLine($"\n{section.Type} (Energy: {section.Energy}):");
    
    for (int barInSection = 0; barInSection < section.Bars; barInSection++)
    {
        var behavior = CompBehaviorSelector.SelectBehavior(
            section.Type, 
            absoluteSectionIndex, 
            barInSection, 
            section.Energy, 
            section.Busy, 
            seed);
        
        Console.WriteLine($"  Bar {barInSection}: {behavior}");
    }
    
    absoluteSectionIndex++;
}
```

Expected output:
```
Intro (Energy: 0.3):
  Bar 0: SparseAnchors
  Bar 1: SparseAnchors
  Bar 2: SparseAnchors
  Bar 3: SparseAnchors

Verse (Energy: 0.4):
  Bar 0: Standard
  Bar 1: Standard
  Bar 2: Standard
  Bar 3: Standard
  Bar 4: Standard (or Anticipate if variation triggers)
  Bar 5: Standard
  Bar 6: Standard
  Bar 7: Standard

Chorus (Energy: 0.8):
  Bar 0: SyncopatedChop
  Bar 1: SyncopatedChop
  ...
```

---

## Debugging Tips

### If test fails with determinism violation:
1. Check for any `Random` usage (there should be none)
2. Verify all hash computations use the same seed
3. Ensure no external state affects selection

### If section differentiation fails:
1. Check activity score calculation
2. Verify section-specific thresholds
3. Ensure switch statement covers all section types

### If variation doesn't occur:
1. Verify bar index is > 0 and divisible by 4
2. Check hash computation includes all required inputs
3. Remember: 30% chance, so some attempts won't vary

---

## Performance Considerations

All operations are O(1) time complexity:
- No loops or recursion
- Simple arithmetic and switch expressions
- Single hash computation per call

Memory: Minimal allocation (enum return, value types for parameters)

Thread safety: All methods are pure functions (no shared state)

---

## Next Steps for Integration

After Story 8.0.1 is verified:
1. Story 8.0.2 will create `CompBehaviorRealizer`
2. Story 8.0.3 will wire into `GuitarTrackGenerator`
3. Integration tests will verify audible differences

These tests provide the foundation for verifying the entire chain works correctly.
