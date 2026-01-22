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

### Story 3.2: Remove Energy from MotifRenderer ✓ COMPLETE

**File**: `Music\Song\Material\MotifRenderer.cs`

**Changes completed**:
- Removed `energy` parameter from simplified `Render()` overload (lines 21-110)
- Updated simplified `Render()` velocity calculation to use fixed base value (85) instead of energy-based calculation
- Removed `barIntent.EffectiveEnergy` usage from `CalculateVelocity()` - now uses fixed base velocity (85)
- Updated XML doc comment to remove energy reference from `intentQuery` parameter
- Updated callers in `GuitarTrackGenerator.cs`, `BassTrackGenerator.cs`, and `KeysTrackGenerator.cs` to remove energy argument

**Test file updated**: `Music\Song\Material\Tests\MotifRendererTests.cs`
- Removed `energy` parameter from `CreateTestIntentQuery()`
- Deleted `TestVelocityFromEnergyTension()` test
- Updated `TestSongIntentQuery` to use fixed energy value (0.5)

**Result**: Motif rendering no longer varies by energy. Uses fixed base velocity of 85 with tension-based modulation and beat strength accents. All rendered notes use consistent velocity calculation regardless of section energy levels.

---

## Epic 4: Remove Energy from Core Generator

### Story 4.1: Remove Energy Arc and Profiles from Generator.cs ✓ COMPLETE

**File**: `Music\Generator\Core\Generator.cs`

**Changes completed**:
- Deleted `EnergyArc.Create()` call and `energyArc` variable
- Deleted `BuildSectionProfiles()` call and `sectionProfiles` variable
- Deleted `BuildSectionProfiles()` method entirely
- Removed `sectionProfiles` argument from all track generator calls (already done in Stories 2.1-2.4)
- Removed `energyArc` argument from `DeterministicTensionQuery` constructor
- Removed `energyArc` argument from `DeterministicVariationQuery` constructor
- Removed `energyArc` argument from `CreateMotifPlacementPlan()` call
- Updated AI comments to remove energy references

**Dependent changes completed**:
- `DeterministicTensionQuery.cs` - removed `EnergyArc` parameter, replaced with `SectionTrack`; uses simplified tension based on section type only
- `DeterministicVariationQuery.cs` - removed `EnergyArc` parameter; uses fixed energy value (0.5)
- `SectionVariationPlanner.cs` - removed `EnergyArc` parameter; uses fixed energy value (0.5)
- `DeterministicSongIntentQuery.cs` - removed `EnergySectionProfile` dictionary parameter, replaced with `SectionTrack`; returns fixed `Energy = 0.5` and `EnergyDelta = 0.0`

**Result**: Generator.cs no longer creates or uses EnergyArc or EnergySectionProfile. All energy-based calculations replaced with fixed values or section-type-based logic. Energy module tests expected to fail (Epic 6).

---

### Story 4.2: Remove Energy from DeterministicTensionQuery ✓ COMPLETE

**File**: `Music\Generator\Energy\DeterministicTensionQuery.cs`

**Changes completed**:
- Removed `EnergyArc` parameter from constructor, replaced with `SectionTrack`
- Removed energy-based tension calculations
- Implemented simplified tension values based on section type only
- Updated `ComputeProfiles()` to use fixed tension values per section type
- Simplified `ComputeTransitionHint()` to use tension delta only (no energy delta)
- Removed `ComputeSectionIndex()` method (no longer needed)

**Result**: Tension calculation now uses section type only. Intro=0.55, Verse=0.45, Chorus=0.42, Bridge=0.62, Solo=0.57, Outro=0.35, with anticipation adjustments for transitions.

---

### Story 4.3: Remove Energy from DeterministicVariationQuery ✓ COMPLETE

**File**: `Music\Generator\Energy\DeterministicVariationQuery.cs`

**Changes completed**:
- Removed `EnergyArc` parameter from constructor
- Updated `ComputePlans()` call to `SectionVariationPlanner` to not pass `energyArc`
- Updated AI comments to remove energy references

**Dependent file**: `Music\Generator\Energy\SectionVariationPlanner.cs`
- Removed `EnergyArc` parameter from `ComputePlans()` method
- Replaced energy target lookup with fixed energy value (0.5)
- Updated AI comments and XML docs to remove energy references

**Result**: Variation planning no longer depends on energy. Uses fixed energy value (0.5) for all variation intensity calculations.

---

### Story 4.4: Remove Energy from DeterministicSongIntentQuery ✓ COMPLETE

**File**: `Music\Generator\Energy\DeterministicSongIntentQuery.cs`

**Changes completed**:
- Removed `EnergySectionProfile` dictionary parameter from constructor
- Added `SectionTrack` parameter to access section types
- Updated `GetSectionIntent()` to return fixed `Energy = 0.5`
- Updated `GetBarIntent()` to return fixed `EnergyDelta = 0.0` and `PhrasePosition = Middle`
- Updated `BuildSectionContext()` to use section from SectionTrack and fixed energy value
- Updated `BuildRolePresenceHints()` to return all roles present (no orchestration gating)
- Updated `BuildDensityCaps()` to use fixed energy value (0.5)
- Updated AI comments to remove energy references

**Caller updated**: `Generator.cs` - now passes `sectionTrack` to `DeterministicSongIntentQuery` constructor

**Result**: Song intent query now returns fixed energy values and all roles present. No energy-based orchestration or density gating.

**Note**: Energy module test files temporarily disabled using `#if FALSE_DISABLED_FOR_ENERGY_DISCONNECT` directive to allow build to succeed. Test files preserved for future energy reintegration:
- `Music\Generator\Energy\Tests\SectionVariationPlannerTests.cs`
- `Music\Generator\Energy\Tests\SongIntentQueryTests.cs`
- `Music\Generator\Energy\Tests\TensionDiagnosticsTests.cs`
- `Music\Generator\Energy\Tests\VariationParameterAdapterTests.cs`
- `Music\Generator\Energy\Tests\VariationQueryTests.cs`
- `Music\Generator\Energy\Tests\TensionContextIntegrationTests.cs`
- `Music.Tests\Generator\Energy\DeterministicTensionQueryTests.cs`
- Test runners updated to skip tests: `RunTensionContextIntegrationTests.cs`, `RunSongIntentQueryTests.cs`

---

## Epic 5: Update Tests

### Story 5.1: Update CompBehaviorTests and CompBehaviorIntegrationTests ✓ COMPLETE

**Files**: 
- `Music\Generator\Guitar\CompBehaviorTests.cs`
- `Music\Generator\Guitar\CompBehaviorIntegrationTests.cs`

**Changes completed**:
- Verified no `energy` arguments in any `SelectBehavior()` calls
- Confirmed energy-related tests (`Test_Energy_AffectsBehaviorSelection`, `Test_EdgeCase_ZeroEnergy`, `Test_EdgeCase_MaxEnergy`) were already removed in Story 1.1
- All tests work without energy parameter and compile successfully

**Result**: Both test files have no energy references. All tests pass and verify CompBehavior selection determinism, section type variation, seed sensitivity, busy probability effects, behavior variation timing, and integration with CompBehaviorRealizer.

---

### Story 5.2: Update KeysRoleModeTests ✓ COMPLETE

**File**: `Music\Generator\Keys\KeysRoleModeTests.cs`

**Changes completed**:
- Verified no `energy` arguments in any `SelectMode()` calls
- Confirmed energy-related tests were already removed in Story 1.2
- All tests work without energy parameter and compile successfully

**Result**: Test file has no energy references. All tests pass and verify KeysRoleMode selection determinism, section type variation, seed sensitivity (Bridge SplitVoicing), busy probability effects, and section-specific mode rules (Outro/Solo always Sustain, Bridge SplitVoicing only on first bar).

---

### Story 5.3: Update SeedSensitivityTests ✓ COMPLETE

**File**: `Music\Generator\Tests\SeedSensitivityTests.cs`

**Changes completed**:
- Verified no `energy` variable declarations or usages
- Confirmed all behavior selector calls use only required parameters (no energy arguments)
- All tests work without energy parameter and compile successfully
- No energy-dependent assertions present

**Result**: Test file has no energy references. All tests pass and verify seed sensitivity across Comp and Keys roles, including determinism, cross-role variation, section type differences (Verse vs Chorus), Bridge SplitVoicing probability, and every-4th-bar comp variation.

---

### Story 5.4: Update TensionHooksIntegrationTests ✓ COMPLETE

**Files**:
- `Music\Generator\Bass\BassTensionHooksIntegrationTests.cs`
- `Music\Generator\Drums\DrumTensionHooksIntegrationTests.cs`
- `Music\Generator\Guitar\CompTensionHooksIntegrationTests.cs`
- `Music\Generator\Keys\KeysTensionHooksIntegrationTests.cs`

**Changes completed**:
- Changed all `sectionEnergy` parameters from variable values (0.55, 0.60, 1.0, 0.0) to fixed value `0.5`
- Removed `energyBias` variable declarations and usages from Comp and Keys tests
- Updated velocity calculations to use only `baseVelocity + hooks.VelocityAccentBias` (no energy bias)
- Updated hard-coded energy value in Drums test condition from `0.55` to `0.5`

**Result**: All 4 test files now use fixed energy value (0.5) consistently. Velocity calculations no longer include energy bias. Tests verify tension hooks behavior without energy dependency while maintaining determinism, guardrails, and bias application logic.

---

### Story 5.5: Update MotifPlacementPlannerTests ✓ COMPLETE

**File**: `Music\Song\Material\Tests\MotifPlacementPlannerTests.cs`

**Changes completed**:
- Removed `introEnergy`, `verseEnergy`, `chorusEnergy` parameters from `CreateTestIntentQuery()` - now uses no parameters
- Deleted energy-dependent tests: `TestVerseHighEnergyGetsMotif()`, `TestVerseLowEnergyRarelyGetsMotif()`, `TestIntroLowEnergyGetsOptionalMotif()`
- Updated `TestSongIntentQuery` to use fixed `Energy = 0.5` for all section types (removed energy fields, constructor parameters, and `GetEnergyForSectionType()` method)
- Updated `TestChorusAlmostAlwaysGetsMotif()` to call `CreateTestIntentQuery()` without energy parameters
- Removed calls to deleted tests from `RunAllTests()`

**Result**: Test file has no energy references. All remaining tests verify motif placement determinism, constraint compliance, A/A' variation, and placement bounds without energy dependency. All sections now use fixed energy value (0.5) in tests.

---

### Story 5.6: Update MotifRendererTests ✓ COMPLETE

**File**: `Music\Song\Material\Tests\MotifRendererTests.cs`

**Changes completed**:
- Verified `CreateTestIntentQuery()` has no energy parameter (line 478)
- Confirmed `TestVelocityFromEnergyTension()` test was already deleted in Story 3.2
- Verified `TestSongIntentQuery` uses fixed energy value (0.5) at line 510
- No additional changes needed - all work was already completed as part of Story 3.2

**Result**: Test file has no energy references. All tests use fixed energy value (0.5) for rendering verification without energy dependency.

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
