# Disconnect Energy from Note Generation

## Overview

**Goal**: Remove all energy-related code from `Generator.cs` and its dependent generators. Energy components remain intact but unreferenced. Generators function as if energy was never implemented.

### Guiding Principles

1. **Delete, don't abstract** - Remove energy code, don't neutralize it
2. **No new code** - Only delete and fix broken references minimally
3. **Bottom-up** - Start with leaf components, work up to `Generator.cs`
4. **Preserve energy module** - Energy classes remain intact, just unused
5. **Non-breaking stories** - Each story compiles and tests pass

### What Gets Removed

- `EnergyArc` creation and usage in `Generator.cs`
- `EnergySectionProfile` parameter from all track generators
- `energyProfile` lookups and usage in track generators
- Energy-based velocity calculations
- Energy-based orchestration presence checks
- `energy` parameter from behavior selectors (or remove callers' energy arguments)
- `ISongIntentQuery` energy-dependent logic in motif components

### What Remains Unchanged

- All classes starting with `Energy*` (EnergyArc, EnergyProfileBuilder, EnergySectionProfile, etc.)
- `SectionEnergyMicroArc`, `PhrasePosition` (Song\Energy folder)
- Energy module unit tests (`EnergyConstraintApplicationTests`, `EnergyArcTests`, etc.)

---

## Epic 1: Remove Energy from Behavior Selectors

### Story 1.1: Remove `energy` Parameter from CompBehaviorSelector ✓ COMPLETE

**File**: `Music\Generator\Guitar\CompBehavior.cs`

**Changes completed**:
- Removed `energy` parameter from `SelectBehavior()` method signature
- Removed `energy = Math.Clamp(energy, 0.0, 1.0);` line
- Changed `activityScore` calculation to use only `busyProbability`
- Updated all callers to not pass energy argument

**Files updated**:
- `Music\Generator\Guitar\CompBehavior.cs` - method signature and logic
- `Music\Generator\Guitar\GuitarTrackGenerator.cs` - caller
- `Music\Generator\Guitar\CompBehaviorTests.cs` - removed energy tests
- `Music\Generator\Guitar\CompBehaviorIntegrationTests.cs` - updated calls
- `Music\Generator\Tests\SeedSensitivityTests.cs` - updated calls

---

### Story 1.2: Remove `energy` Parameter from KeysRoleModeSelector

**File**: `Music\Generator\Keys\KeysRoleMode.cs`

**Changes**:
- Remove `energy` parameter from `SelectMode()` method signature
- Remove `energy = Math.Clamp(energy, 0.0, 1.0);` line
- Change `activityScore` calculation to use only `busyProbability`
- Update callers to not pass energy argument

**Callers to update**: `KeysTrackGenerator.cs`, `KeysRoleModeTests.cs`, `SeedSensitivityTests.cs`

---

### Story 1.3: Remove Energy Logic from DrumVariationEngine

**File**: `Music\Generator\Drums\DrumVariationEngine.cs`

**Changes**:
- `IsHighEnergySection()` - delete method or make it return `false`
- `ShouldUseRide()` - simplify to not depend on section type energy mapping
- `GetOpenHatProbability()` - return fixed value (e.g., 0.1)
- Remove "energy" references from comments

---

### Story 1.4: Remove Energy Logic from DrumVelocityShaper

**File**: `Music\Generator\Drums\DrumVelocityShaper.cs`

**Changes**:
- `ApplySectionEnergyBoost()` - return input velocity unchanged (or inline deletion)
- Remove call to `ApplySectionEnergyBoost()` from `CalculateVelocity()`
- Remove "energy" references from comments

---

## Epic 2: Remove Energy from Track Generators

### Story 2.1: Remove Energy from GuitarTrackGenerator

**File**: `Music\Generator\Guitar\GuitarTrackGenerator.cs`

**Changes**:
- Remove `Dictionary<int, EnergySectionProfile> sectionProfiles` parameter from `Generate()`
- Delete `EnergySectionProfile? energyProfile = null;` and profile lookup
- Delete orchestration presence check (`energyProfile?.Orchestration`)
- Delete `energyProfile` parameter from internal method calls
- Delete velocity energy bias calculation (`energyProfile?.Global.Energy`)
- Update `CompBehaviorSelector.SelectBehavior()` call to not pass energy
- Remove AI comments referencing "Story 7.3" and energy

**Callers to update**: `Generator.cs`

---

### Story 2.2: Remove Energy from KeysTrackGenerator

**File**: `Music\Generator\Keys\KeysTrackGenerator.cs`

**Changes**:
- Remove `Dictionary<int, EnergySectionProfile> sectionProfiles` parameter from `Generate()`
- Delete `EnergySectionProfile? energyProfile = null;` and profile lookup
- Delete orchestration presence check
- Delete `UpdateSectionProfileWithEnergy()` method or its call
- Delete velocity energy bias calculation
- Update `KeysRoleModeSelector.SelectMode()` call to not pass energy
- Remove AI comments referencing energy

**Callers to update**: `Generator.cs`

---

### Story 2.3: Remove Energy from BassTrackGenerator

**File**: `Music\Generator\Bass\BassTrackGenerator.cs`

**Changes**:
- Remove `Dictionary<int, EnergySectionProfile> sectionProfiles` parameter from `Generate()`
- Delete `EnergySectionProfile? energyProfile = null;` and profile lookup
- Delete orchestration presence check
- Delete `energyProfile` parameter from internal method calls
- Delete velocity energy bias calculation
- Remove AI comments referencing energy

**Callers to update**: `Generator.cs`

---

### Story 2.4: Remove Energy from DrumTrackGenerator

**File**: `Music\Generator\Drums\DrumTrackGenerator.cs`

**Changes**:
- Remove `Dictionary<int, EnergySectionProfile> sectionProfiles` parameter from `Generate()`
- Delete `EnergySectionProfile? energyProfile = null;` and profile lookup
- Delete orchestration presence check
- Delete `BuildDrumParameters()` energy profile merging (use defaults)
- Delete `GenerateCymbalHitsWithEnergyProfile()` energy hints (simplify to basic call)
- Delete phrase-end dropout energy threshold check
- Remove AI comments referencing energy

**Callers to update**: `Generator.cs`

---

## Epic 3: Remove Energy from Motif Components

### Story 3.1: Remove Energy from MotifPlacementPlanner

**File**: `Music\Song\Material\MotifPlacementPlanner.cs`

**Changes**:
- `ShouldPlaceMotif()` - remove `intent.Energy` checks, use section type only
- `GetPreferredMaterialKind()` - remove `intent.Energy` checks
- `DetermineMotifScope()` - remove `intent.Energy > 0.7` check
- Remove energy trace logging

---

### Story 3.2: Remove Energy from MotifRenderer

**File**: `Music\Song\Material\MotifRenderer.cs`

**Changes**:
- `Render()` overload with `energy` parameter - remove parameter or use fixed value
- `CalculateVelocity()` - remove `barIntent.EffectiveEnergy` usage, use fixed base velocity
- Remove energy references from comments

---

## Epic 4: Remove Energy from Core Generator

### Story 4.1: Remove Energy Arc and Profiles from Generator.cs

**File**: `Music\Generator\Core\Generator.cs`

**Changes**:
- Delete `EnergyArc.Create()` call and `energyArc` variable
- Delete `BuildSectionProfiles()` call and `sectionProfiles` variable
- Delete `BuildSectionProfiles()` method entirely
- Remove `sectionProfiles` argument from all track generator calls
- Remove `energyArc` argument from `DeterministicTensionQuery` constructor
- Remove `energyArc` argument from `DeterministicVariationQuery` constructor
- Remove `energyArc` argument from `CreateMotifPlacementPlan()` call
- Update AI comments to remove energy references

**Dependent changes**: `DeterministicTensionQuery`, `DeterministicVariationQuery` constructors may need parameter removal

---

### Story 4.2: Remove Energy from DeterministicTensionQuery

**File**: `Music\Generator\Energy\DeterministicTensionQuery.cs`

**Changes**:
- Remove `EnergyArc` parameter from constructor
- Remove energy-based tension calculations
- Return fixed/simplified tension values based on section type only

---

### Story 4.3: Remove Energy from DeterministicVariationQuery

**File**: `Music\Generator\Energy\DeterministicVariationQuery.cs`

**Changes**:
- Remove `EnergyArc` parameter from constructor
- Remove energy-based variation calculations

---

### Story 4.4: Remove Energy from DeterministicSongIntentQuery

**File**: `Music\Generator\Energy\DeterministicSongIntentQuery.cs`

**Changes**:
- Remove `EnergySectionProfile` dictionary from constructor
- `GetSectionIntent()` - return fixed `Energy = 0.5` or remove Energy property usage
- `GetBarIntent()` - return fixed `EnergyDelta = 0.0`

---

## Epic 5: Update Tests

### Story 5.1: Update CompBehaviorTests and CompBehaviorIntegrationTests

**Files**: 
- `Music\Generator\Guitar\CompBehaviorTests.cs`
- `Music\Generator\Guitar\CompBehaviorIntegrationTests.cs`

**Changes**:
- Remove `energy` argument from all `SelectBehavior()` calls
- Delete tests that specifically test energy affects behavior (`Test_Energy_AffectsBehaviorSelection`, `Test_EdgeCase_ZeroEnergy`, `Test_EdgeCase_MaxEnergy`)
- Update remaining tests to work without energy parameter

---

### Story 5.2: Update KeysRoleModeTests

**File**: `Music\Generator\Keys\KeysRoleModeTests.cs`

**Changes**:
- Remove `energy` argument from all `SelectMode()` calls
- Delete tests that specifically test energy affects mode selection
- Update remaining tests to work without energy parameter

---

### Story 5.3: Update SeedSensitivityTests

**File**: `Music\Generator\Tests\SeedSensitivityTests.cs`

**Changes**:
- Remove all `energy` variable declarations and usages
- Remove `energy` arguments from behavior selector calls
- Update assertions that depend on energy

---

### Story 5.4: Update TensionHooksIntegrationTests

**Files**:
- `Music\Generator\Bass\BassTensionHooksIntegrationTests.cs`
- `Music\Generator\Drums\DrumTensionHooksIntegrationTests.cs`
- `Music\Generator\Guitar\CompTensionHooksIntegrationTests.cs`
- `Music\Generator\Keys\KeysTensionHooksIntegrationTests.cs`

**Changes**:
- Remove `sectionEnergy` parameter from test method calls
- Remove `energyBias` calculations
- Update velocity assertions to not expect energy-driven differences

---

### Story 5.5: Update MotifPlacementPlannerTests

**File**: `Music\Song\Material\Tests\MotifPlacementPlannerTests.cs`

**Changes**:
- Remove `introEnergy`, `verseEnergy`, `chorusEnergy` parameters from `CreateTestIntentQuery()`
- Delete or update tests: `TestVerseHighEnergyGetsMotif()`, `TestVerseLowEnergyRarelyGetsMotif()`, `TestIntroLowEnergyGetsOptionalMotif()`
- Update `TestSongIntentQuery` to not use energy

---

### Story 5.6: Update MotifRendererTests

**File**: `Music\Song\Material\Tests\MotifRendererTests.cs`

**Changes**:
- Remove `energy` parameter from `CreateTestIntentQuery()`
- Delete or update `TestVelocityFromEnergyTension()` test
- Update `TestSongIntentQuery` to return fixed energy values

---

## Epic 6: Preserve Energy Module (No Changes)

These files remain **unchanged** - they test energy components in isolation:

- `Music.Tests\Generator\Energy\EnergyConstraintApplicationTests.cs`
- `Music.Tests\Generator\Energy\DeterministicTensionQueryTests.cs`
- `Music\Generator\Energy\EnergyArcTests.cs`
- `Music\Song\Energy\SectionEnergyMicroArcTests.cs`
- `Music\Generator\Energy\Tests\SongIntentQueryTests.cs`

**Note**: After Epic 4, these tests may fail because their dependencies changed. If so, they should be **skipped/ignored** (not deleted) until energy reintegration.

---

## Summary: Execution Order

| Order | Story | Files | Type |
|-------|-------|-------|------|
| 1 | 1.1 | CompBehavior.cs | Remove parameter |
| 2 | 1.2 | KeysRoleMode.cs | Remove parameter |
| 3 | 1.3 | DrumVariationEngine.cs | Remove logic |
| 4 | 1.4 | DrumVelocityShaper.cs | Remove logic |
| 5 | 2.1 | GuitarTrackGenerator.cs | Remove parameter + logic |
| 6 | 2.2 | KeysTrackGenerator.cs | Remove parameter + logic |
| 7 | 2.3 | BassTrackGenerator.cs | Remove parameter + logic |
| 8 | 2.4 | DrumTrackGenerator.cs | Remove parameter + logic |
| 9 | 3.1 | MotifPlacementPlanner.cs | Remove logic |
| 10 | 3.2 | MotifRenderer.cs | Remove logic |
| 11 | 4.1 | Generator.cs | Remove energy arc + profiles |
| 12 | 4.2 | DeterministicTensionQuery.cs | Remove parameter |
| 13 | 4.3 | DeterministicVariationQuery.cs | Remove parameter |
| 14 | 4.4 | DeterministicSongIntentQuery.cs | Remove parameter |
| 15 | 5.1 | CompBehaviorTests.cs | Remove tests + update calls |
| 16 | 5.2 | KeysRoleModeTests.cs | Remove tests + update calls |
| 17 | 5.3 | SeedSensitivityTests.cs | Update calls |
| 18 | 5.4 | TensionHooksIntegrationTests (4 files) | Update calls |
| 19 | 5.5 | MotifPlacementPlannerTests.cs | Remove tests + update fixture |
| 20 | 5.6 | MotifRendererTests.cs | Remove tests + update fixture |

---

## Verification

After completion:
1. `Generator.cs` has no references to `EnergyArc`, `EnergySectionProfile`, or `EnergyProfileBuilder`
2. Track generators have no `sectionProfiles` parameter
3. Behavior selectors have no `energy` parameter
4. All tests compile and pass (energy module tests may be skipped)
5. Search for "energy" in generator files returns only comments (if any)
