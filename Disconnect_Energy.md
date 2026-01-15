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

### Story 1.2: Remove `energy` Parameter from KeysRoleModeSelector ✓ COMPLETE

**File**: `Music\Generator\Keys\KeysRoleMode.cs`

**Changes completed**:
- Removed `energy` parameter from `SelectMode()` method signature
- Removed `energy = Math.Clamp(energy, 0.0, 1.0);` line
- Changed `activityScore` calculation to use only `busyProbability`
- Updated all callers to not pass energy argument

**Files updated**:
- `Music\Generator\Keys\KeysRoleMode.cs` - method signature and logic
- `Music\Generator\Keys\KeysTrackGenerator.cs` - caller
- `Music\Generator\Keys\KeysRoleModeTests.cs` - removed energy tests
- `Music\Generator\Tests\SeedSensitivityTests.cs` - updated calls

---

### Story 1.3: Remove Energy Logic from DrumVariationEngine ✓ COMPLETE

**File**: `Music\Generator\Drums\DrumVariationEngine.cs`

**Changes completed**:
- `IsHighEnergySection()` - returns `false` (disables flams)
- `ShouldUseRide()` - simplified to return fixed probability (0.15) regardless of section type
- `OpenHatProbabilityForSection()` - returns fixed value (0.1) regardless of section type
- Removed "energy" references from AI comments

**Result**: Drum variation no longer varies by section energy - all sections use consistent probabilities for ride usage, open hats, and flams (disabled).

---

### Story 1.4: Remove Energy Logic from DrumVelocityShaper ✓ COMPLETE

**File**: `Music\Generator\Drums\DrumVelocityShaper.cs`

**Changes completed**:
- `ApplySectionEnergyBoost()` - now returns input velocity unchanged (section-based boost disabled)
- Removed call to `ApplySectionEnergyBoost()` from `ShapeVelocity()` to eliminate dead code path
- Updated AI comments to remove "energy context" references
- Updated inline comments to remove energy descriptions

**Result**: Drum velocities no longer vary by section type. All sections use the same base velocities with only hand pattern accents and humanization applied. Chorus (kick +10, snare +12, hat +8), Bridge (kick +8, snare +10), Solo (kick +7, snare +9), Intro (-5), and Outro (-8) boosts are now disabled.

---

## Epic 2: Remove Energy from Track Generators

### Story 2.1: Remove Energy from GuitarTrackGenerator ✓ COMPLETE

**File**: `Music\Generator\Guitar\GuitarTrackGenerator.cs`

**Changes completed**:
- Removed `Dictionary<int, EnergySectionProfile> sectionProfiles` parameter from `Generate()`
- Deleted `EnergySectionProfile? energyProfile = null;` and profile lookup
- Deleted orchestration presence check (`energyProfile?.Orchestration.CompPresent`)
- Deleted `energyProfile` parameter from `RenderMotifForBar()` and `TensionHooksBuilder.Create()` calls
- Removed compProfile retrieval and variation query logic that depended on energy
- Changed behavior/realization calls to use fixed values (0.5 for busyProbability, 1.0 for densityMultiplier)
- Removed register lift (`ApplyRegisterWithGuardrail`) and velocity bias (`ApplyVelocityBias`) applications
- Deleted `ApplyVelocityBias()` method
- Updated `MotifRenderer.Render()` call to use fixed energy value (0.5)
- Updated AI comments to remove "Story 7.3" and energy references

**Caller updated**: `Generator.cs` - removed `sectionProfiles` argument from `GuitarTrackGenerator.Generate()` call

**Result**: Guitar/comp track generation no longer varies by energy. Uses fixed busyProbability (0.5), densityMultiplier (1.0), no register lift, and base velocity of 85 with only tension-based accent bias applied.

---

### Story 2.2: Remove Energy from KeysTrackGenerator ✓ COMPLETE

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

### Story 2.3: Remove Energy from BassTrackGenerator ✓ COMPLETE

**File**: `Music\Generator\Bass\BassTrackGenerator.cs`

**Changes completed**:
- Removed `Dictionary<int, EnergySectionProfile> sectionProfiles` parameter from `Generate()`
- Deleted `EnergySectionProfile? energyProfile = null;` and profile lookup
- Deleted orchestration presence check (`energyProfile?.Orchestration.BassPresent`)
- Deleted `energyProfile` parameter from `RenderMotifForBar()` and `TensionHooksBuilder.Create()` calls (passed `null` instead)
- Removed bassProfile retrieval and variation query logic that depended on energy
- Changed approach probability to use fixed busyProbability (0.5)
- Removed velocity bias calculation (`ApplyVelocityBias()`) and deleted the method
- Updated velocity to use fixed base value (95) with clamp
- Removed register lift/guardrail application (kept guardrail logic but removed energy comments)
- Updated `MotifRenderer.Render()` call to use fixed energy value (0.5)
- Updated AI comments to remove "Story 7.3" and energy references

**Caller updated**: `Generator.cs` - removed `sectionProfiles` argument from `BassTrackGenerator.Generate()` call

**Result**: Bass track generation no longer varies by energy. Uses fixed busyProbability (0.5), no velocity bias, and base velocity of 95 with only range guardrail applied.

---

### Story 2.4: Remove Energy from DrumTrackGenerator ✓ COMPLETE

**File**: `Music\Generator\Drums\DrumTrackGenerator.cs`

**Changes completed**:
- Removed `Dictionary<int, EnergySectionProfile> sectionProfiles` parameter from `Generate()`
- Deleted `EnergySectionProfile? energyProfile = null;` and profile lookup
- Deleted orchestration presence check (`energyProfile?.Orchestration.DrumsPresent`)
- Deleted `BuildDrumParameters()` method - now uses default `settings.DrumParameters`
- Replaced `GenerateCymbalHitsWithEnergyProfile()` with direct call to `CymbalOrchestrationEngine.GenerateCymbalHits()`
- Deleted both energy-specific methods entirely
- Removed energy threshold check from phrase-end dropout logic (simplified condition from `energyProfile?.Global.Energy is < 0.92` to always allow)
- Updated AI comments to remove "Story 7.3" and energy references

**Caller updated**: `Generator.cs` - removed `sectionProfiles` argument from `DrumTrackGenerator.Generate()` call

**Result**: Drum track generation no longer varies by energy. Uses default drum parameters from settings, no orchestration gating, and simplified cymbal generation without energy hints. Phrase-end dropout now only depends on tension hooks, not energy levels.

---

## Epic 3: Remove Energy from Motif Components

### Story 3.1: Remove Energy from MotifPlacementPlanner ✓ COMPLETE

**File**: `Music\Song\Material\MotifPlacementPlanner.cs`

**Changes completed**:
- `ShouldPlaceMotif()` - removed `intent.Energy` checks, now uses section type only
- `GetPreferredMaterialKind()` - removed `intent.Energy > 0.6` check for Verse section (always uses MelodyPhrase)
- `DetermineMotifScope()` - removed `intent.Energy > 0.7` check (Chorus only now determines full section)
- Removed energy trace logging (`Section {sectionIndex} ({section.SectionType}): Energy={intent.Energy:F2}`)
- Updated AI comments and XML docs to remove energy references

**Result**: Motif placement now uses section type only for decisions. Chorus gets hooks with high probability (0.8), Intro gets teasers (0.5), Verse gets riffs/phrases (0.7), Bridge gets contrast (0.7), Solo gets featured motifs (0.8), Outro gets fills (0.7). Verse always uses MelodyPhrase material kind. Duration determined by Chorus section type only, not energy level.

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
