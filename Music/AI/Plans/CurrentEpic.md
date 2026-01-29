# Epic: Groove System Domain Refactoring

**Scope:** Refactor the Groove system to its proper domain (rhythm/onset patterns only) and move part-generation concerns (candidates, velocity, policies) to the Drum Generator space.

**Objective:** Create a clean separation where Groove provides rhythm patterns and Part Generators make musical decisions.

**Principle:** REUSE existing classes. Do not create new files when existing files can be modified. Move files to new locations when domain ownership changes. Delete files when no longer needed.

---

## Problem Statement

The current Groove system has overstepped its domain. It handles:
- ❌ Velocity determination (should be Part Generator's job)
- ❌ Candidate selection and weighted picking (should be Part Generator's job)
- ❌ Section-aware behavior (Groove should be constant per groove event)
- ❌ Variation constraints (Part Generator handles via style configuration)
- ❌ Policy decisions (Part Generator's domain)

## Existing Classes to Reuse

The following classes ALREADY EXIST and should be reused (not recreated):

| Existing Class | Purpose | Reuse Plan |
|----------------|---------|------------|
| `GrooveInstanceLayer` | Holds onset lists per role (Kick, Snare, Hat, etc.) | **USE AS-IS** — this IS the groove instance |
| `GroovePresetDefinition` | Contains AnchorLayer + policies | Simplify to just AnchorLayer + Identity |
| `GroovePresetIdentity` | Name, BeatsPerBar, StyleFamily, Tags | **USE AS-IS** |
| `GrooveRoles` | Role name constants | **USE AS-IS** |
| `GrooveSetupFactory.BuildPopRockBasicAnchorLayer()` | Hardcoded PopRock anchor | Extract to standalone method |
| `GroovePresetLibrary` | Preset storage by name | Simplify — remove segment complexity |

---

## Target Architecture

### Groove System (Simplified Domain)
**Input:**
1. Genre (e.g., "PopRock")
2. Random seed

**Output:**
- `GrooveInstanceLayer` — onset positions per role (EXISTING CLASS)
- Query via role name → list of beat positions

**NOT Responsible For:**
- Velocities (Part Generator)
- Candidates (Part Generator)
- Section awareness (Part Generator)
- Policies (Part Generator)

### Part Generator (Drum Generator)
- Uses `GrooveInstanceLayer` for timing reference
- Generates candidates via operators
- Applies velocity shaping
- Applies section/energy awareness
- Handles all musical intelligence

---

## Milestone Deliverable

**Goal:** Create a Generator method that:
1. Accepts a random seed and genre
2. Generates a `GrooveInstanceLayer` (using existing class)
3. Converts to drum set PartTrack
4. Loads into song grid for audition

**Success Criteria:** Enter different random seeds and hear the groove instance (anchor + variation), unadulterated by part generator logic.

---

## Story Breakdown

### Phase 1: Simplify Groove Generation (Reuse Existing)  (COMPLETED)

#### Story 1.1 — Add Query Methods to GrooveInstanceLayer (COMPLETED)

**As a** developer  
**I want** helper methods on the existing `GrooveInstanceLayer`  
**So that** part generators can query onset data easily

**Acceptance Criteria:**
- [ ] Add to existing `GrooveInstanceLayer.cs`:
  ```csharp
  public IReadOnlyList<decimal> GetOnsets(string role)
  public IReadOnlySet<string> GetActiveRoles()
  public bool HasRole(string role)
  ```
- [ ] Implementation maps role string to the appropriate List property
- [ ] Returns empty list for unknown roles (no exception)
- [ ] Deterministic, null-safe
- [ ] Unit tests: query methods work correctly

**Files to Modify:**
- `Generator/Groove/GrooveInstanceLayer.cs`

**Files to Create:**
- `Music.Tests/Generator/Groove/GrooveInstanceLayerQueryTests.cs`

**Notes:**
- NO new class — extend existing `GrooveInstanceLayer`
- Keep existing properties for backward compatibility

---

#### Story 1.2 — Extract Anchor Patterns to GrooveAnchorFactory (COMPLETED)

**As a** developer  
**I want** a simple factory that returns anchor patterns by genre  
**So that** anchor data is centralized and easily accessible

**Acceptance Criteria:**
- [ ] Modify existing `GrooveTestSetup.cs` → rename to `GrooveAnchorFactory.cs`
- [ ] Simplify to expose only:
  ```csharp
  public static class GrooveAnchorFactory
  {
      public static GrooveInstanceLayer GetAnchor(string genre)
      public static IReadOnlyList<string> GetAvailableGenres()
  }
  ```
- [ ] Move `BuildPopRockBasicAnchorLayer()` logic into `GetAnchor("PopRock")`
- [ ] Remove all policy/segment building (move to Drum Generator if needed)
- [ ] Throw `ArgumentException` for unknown genre
- [ ] Unit tests: PopRock anchor retrieval works

**Files to Modify:**
- `Generator/Groove/GrooveTestSetup.cs` → **Rename to** `GrooveAnchorFactory.cs`

**Files to Delete:**
- (Original `GrooveTestSetup.cs` after rename)

**Notes:**
- Keep anchor data as-is: Kick [1, 3], Snare [2, 4], Hat [1, 1.5, 2, ...]
- Policy building code will be deleted or moved in later stories

---

#### Story 1.3 — Add Variation Logic to GrooveInstanceLayer  (COMPLETED)

**As a** developer  
**I want** seed-based variation applied to a `GrooveInstanceLayer`  
**So that** different seeds produce different grooves

**Acceptance Criteria:**
- [ ] Add static method to `GrooveInstanceLayer.cs`:
  ```csharp
  public static GrooveInstanceLayer CreateVariation(
      GrooveInstanceLayer anchor, 
      int seed)
  ```
- [ ] Variation operations (deterministic from seed):
  - [ ] **Kick doubles**: 50% chance add kick at 1.5 or 3.5
  - [ ] **Hat subdivision**: 30% chance upgrade 8ths to 16ths
  - [ ] **Syncopation**: 20% chance add anticipation (e.g., 0.75, 2.75)
- [ ] Use existing `Rng` class with purpose-specific streams
- [ ] Snare backbeat stays stable (2, 4 never modified)
- [ ] **Deterministic**: Same seed + anchor → identical output
- [ ] Unit tests:
  - [ ] Same seed → same output
  - [ ] Different seeds → different outputs  
  - [ ] Backbeat preserved

**Files to Modify:**
- `Generator/Groove/GrooveInstanceLayer.cs`

**Notes:**
- MVP: Keep variations simple
- Use existing `Rng.NextDouble(RandomPurpose.GrooveVariation)` pattern

---

#### Story 1.4 — Create GrooveInstanceGenerator Facade Method  (COMPLETED)

**As a** developer  
**I want** a single entry point: genre + seed → GrooveInstanceLayer  
**So that** callers don't need to know internal details

**Acceptance Criteria:**
- [ ] Add static method (can be in `GrooveAnchorFactory` or new small file):
  ```csharp
  public static GrooveInstanceLayer Generate(string genre, int seed)
  ```
- [ ] Implementation:
  1. Get anchor from `GrooveAnchorFactory.GetAnchor(genre)`
  2. Apply variation via `GrooveInstanceLayer.CreateVariation(anchor, seed)`
  3. Return result
- [ ] Deterministic: Same genre + seed → identical output
- [ ] Unit tests: generation works, determinism verified

**Files to Modify:**
- `Generator/Groove/GrooveAnchorFactory.cs` (add Generate method)

**Notes:**
- Single file preferred — avoid creating separate GrooveInstanceGenerator.cs

---

### Phase 2: Groove to PartTrack Conversion (COMPLETED)

#### Story 2.1 — Add ToPartTrack Method to GrooveInstanceLayer  (COMPLETED)

**As a** developer  
**I want** to convert a GrooveInstanceLayer to a playable drum PartTrack  
**So that** I can audition the groove directly

**Acceptance Criteria:**
- [ ] Add method to `GrooveInstanceLayer.cs`:
  ```csharp
  public PartTrack ToPartTrack(BarTrack barTrack, int totalBars, int velocity = 100)
  ```
- [ ] Maps roles to GM MIDI drum notes (use existing constants from `DrumTrackGenerator`):
  - [ ] "Kick" → 36
  - [ ] "Snare" → 38
  - [ ] "ClosedHat" → 42
  - [ ] "OpenHat" → 46
- [ ] For each bar 1..totalBars, for each role, for each onset beat:
  - [ ] Calculate `AbsoluteTimeTicks` from bar + beat using `barTrack`
  - [ ] Create `PartTrackEvent` with fixed velocity
- [ ] Events sorted by `AbsoluteTimeTicks`
- [ ] Unit tests: conversion produces correct MIDI events

**Files to Modify:**
- `Generator/Groove/GrooveInstanceLayer.cs`

**Notes:**
- Reuse MIDI note constants from existing `DrumTrackGenerator.cs`
- All onsets get same velocity (no shaping — that's Drum Generator's job)

---

#### Story 2.2 — Add GenerateGroovePreview to Generator.cs (COMPLETED)

**As a** developer  
**I want** a single method to generate a groove preview from seed + genre  
**So that** I can audition different seeds quickly

**Acceptance Criteria:**
- [ ] Add to existing `Generator.cs`:
  ```csharp
  public static PartTrack GenerateGroovePreview(
      int seed,
      string genre,
      BarTrack barTrack,
      int totalBars,
      int velocity = 100)
  ```
- [ ] Implementation:
  1. Call `GrooveAnchorFactory.Generate(genre, seed)`
  2. Call `result.ToPartTrack(barTrack, totalBars, velocity)`
  3. Return PartTrack
- [ ] Unit tests: generates valid PartTrack, determinism verified

**Files to Modify:**
- `Generator/Core/Generator.cs`

---

### Phase 3: UI Integration for Audition (COMPLETED)

#### Story 3.1 — Add Groove Preview Command to WriterForm (COMPLETED)

**As a** user  
**I want** to enter a seed and hear the generated groove  
**So that** I can audition different groove variations

**Acceptance Criteria:**
- [ ] Add menu item: "Generate → Groove Preview" (or keyboard shortcut Ctrl+G)
- [ ] Show input dialog requesting:
  - [ ] Seed (integer, default: random from DateTime)
  - [ ] Genre (dropdown: "PopRock", default)
  - [ ] Bars (default: 8)
- [ ] On confirm:
  1. Call `Generator.GenerateGroovePreview(seed, genre, barTrack, bars)`
  2. Load resulting PartTrack into song grid (replace existing drum track or add new)
  3. Auto-play if playback is easy to trigger
- [ ] Display seed used in status bar or message (for reproduction)

**Files to Modify:**
- `Writer/WriterForm/WriterForm.cs` (add menu item)
- `Writer/WriterForm/WriterFormEventHandlers.cs` (add handler)

**Notes:**
- Use existing dialog patterns from project
- Keep it simple — MVP is seed input + button

---

### Phase 4: Move Part-Generation Code to Drum Generator

#### Story 4.1 — Move Candidate Types to Drum Generator (COMPLETED)

**As a** developer  
**I want** candidate selection logic in the Drum Generator namespace  
**So that** Groove system is free of part-generation concerns

**Acceptance Criteria:**
- [ ] Move `GrooveCandidateGroup.cs` → `Generator/Agents/Drums/DrumCandidateGroup.cs`
  - [ ] Rename class to `DrumCandidateGroup`
  - [ ] Update namespace to `Music.Generator.Agents.Drums`
- [ ] Move `GrooveOnsetCandidate.cs` → `Generator/Agents/Drums/DrumOnsetCandidate.cs`
  - [ ] Rename class to `DrumOnsetCandidate`
  - [ ] Update namespace
- [ ] Move `GrooveSelectionEngine.cs` → `Generator/Agents/Drums/DrumSelectionEngine.cs`
  - [ ] Rename class to `DrumSelectionEngine`
  - [ ] Update namespace and references
- [ ] Move `GrooveWeightedCandidateSelector.cs` → `Generator/Agents/Drums/DrumWeightedCandidateSelector.cs`
  - [ ] Rename class
  - [ ] Update namespace
- [ ] Update all references in Drum Generator code

**Files to Move (with rename):**
- `Generator/Groove/GrooveCandidateGroup.cs` → `Generator/Agents/Drums/DrumCandidateGroup.cs`
- `Generator/Groove/GrooveOnsetCandidate.cs` → `Generator/Agents/Drums/DrumOnsetCandidate.cs`
- `Generator/Groove/GrooveSelectionEngine.cs` → `Generator/Agents/Drums/DrumSelectionEngine.cs`
- `Generator/Groove/GrooveWeightedCandidateSelector.cs` → `Generator/Agents/Drums/DrumWeightedCandidateSelector.cs`

**Files to Delete (after move):**
- Original files in `Generator/Groove/`

**Files to Modify:**
- All files referencing moved types (update namespaces/using statements)

---

#### Story 4.2 — Move Policy Interfaces to Drum Generator  (COMPLETED)

**As a** developer  
**I want** policy provider interfaces in the Drum Generator  
**So that** Groove is not defining part-generation contracts

**Acceptance Criteria:**
- [ ] Move `IGroovePolicyProvider.cs` → `Generator/Agents/Drums/IDrumPolicyProvider.cs`
  - [ ] Rename interface to `IDrumPolicyProvider`
- [ ] Move `IGrooveCandidateSource.cs` → `Generator/Agents/Drums/IDrumCandidateSource.cs`
  - [ ] Rename interface to `IDrumCandidateSource`
- [ ] Move `GroovePolicyDecision.cs` → `Generator/Agents/Drums/DrumPolicyDecision.cs`
  - [ ] Rename class
- [ ] Update `DrummerPolicyProvider` and `DrummerCandidateSource` to use new interfaces
- [ ] Update `GrooveBasedDrumGenerator` references

**Files to Move (with rename):**
- `Generator/Groove/IGroovePolicyProvider.cs` → `Generator/Agents/Drums/IDrumPolicyProvider.cs`
- `Generator/Groove/IGrooveCandidateSource.cs` → `Generator/Agents/Drums/IDrumCandidateSource.cs`
- `Generator/Groove/GroovePolicyDecision.cs` → `Generator/Agents/Drums/DrumPolicyDecision.cs`

**Files to Delete (after move):**
- Original files in `Generator/Groove/`

---

#### Story 4.3 — Move Density/Caps Logic to Drum Generator (COMPLETED)

**As a** developer  
**I want** density and caps logic in the Drum Generator  
**So that** Groove doesn't manage part complexity

**Acceptance Criteria:**
- [ ] Move `GrooveDensityCalculator.cs` → `Generator/Agents/Drums/DrumDensityCalculator.cs`
- [ ] Move `RoleDensityTarget.cs` → `Generator/Agents/Drums/RoleDensityTarget.cs`
- [ ] Move `GrooveCapsEnforcer.cs` → `Generator/Agents/Drums/DrumCapsEnforcer.cs`
- [ ] Update all references

**Files to Move (with rename):**
- `Generator/Groove/GrooveDensityCalculator.cs` → `Generator/Agents/Drums/DrumDensityCalculator.cs`
- `Generator/Groove/RoleDensityTarget.cs` → `Generator/Agents/Drums/RoleDensityTarget.cs`
- `Generator/Groove/GrooveCapsEnforcer.cs` → `Generator/Agents/Drums/DrumCapsEnforcer.cs`

**Files to Delete (after move):**
- Original files in `Generator/Groove/`

---

### Phase 5: Delete Unused Groove Files

#### Story 5.1 — Remove Velocity from Groove Types

**As a** developer  
**I want** velocity removed from Groove types  
**So that** Groove never influences velocity (Part Generator's job)

**Acceptance Criteria:**
- [ ] Remove `Velocity` property from `GrooveOnset.cs` (or keep for MIDI conversion only)
- [ ] Ensure `GrooveInstanceLayer.ToPartTrack()` uses passed-in velocity only
- [ ] Verify all velocity shaping happens in `DrummerVelocityShaper`
- [ ] Update tests

**Files to Modify:**
- `Generator/Groove/GrooveOnset.cs`

---

#### Story 5.2 — Delete Section-Aware Groove Files

**As a** developer  
**I want** to remove section-awareness from Groove  
**So that** Groove is constant and section handling is Part Generator's job

**Acceptance Criteria:**
- [ ] Delete or simplify files no longer needed:
  - [ ] `SegmentGrooveProfile.cs` — DELETE (section awareness is Part Generator's job)
  - [ ] `BarContext.cs` — MOVE to Drums if still used, else DELETE
  - [ ] `GrooveBarContext.cs` — MOVE to Drums if still used, else DELETE
  - [ ] `BarContextBuilder.cs` — MOVE to Drums if still used, else DELETE
- [ ] Update `SongContext` to remove `SegmentGrooveProfiles` if no longer needed
- [ ] Ensure `DrummerPolicyProvider` handles section-aware density (already does)

**Files to Delete:**
- `Generator/Groove/SegmentGrooveProfile.cs`

**Files to Move (if still used by Drums):**
- `Generator/Groove/BarContext.cs` → `Generator/Agents/Drums/BarContext.cs`
- `Generator/Groove/GrooveBarContext.cs` → `Generator/Agents/Drums/DrumBarContext.cs`
- `Generator/Groove/BarContextBuilder.cs` → `Generator/Agents/Drums/DrumBarContextBuilder.cs`

---

#### Story 5.3 — Delete Unused Protection/Policy Files

**As a** developer  
**I want** to delete protection/policy files that are now Drum Generator's domain  
**So that** Groove namespace is clean

**Acceptance Criteria:**
- [ ] Review and delete or move:
  - [ ] `GrooveProtectionPolicy.cs` — DELETE or MOVE to Drums
  - [ ] `GrooveProtectionLayer.cs` — DELETE or MOVE to Drums
  - [ ] `RoleProtectionSet.cs` — DELETE or MOVE to Drums
  - [ ] `ProtectionApplier.cs` — DELETE or MOVE to Drums
  - [ ] `ProtectionPerBarBuilder.cs` — DELETE or MOVE to Drums
  - [ ] `ProtectionPolicyMerger.cs` — DELETE or MOVE to Drums
  - [ ] `PhraseHookProtectionAugmenter.cs` — DELETE or MOVE to Drums
  - [ ] `GroovePhraseHookPolicy.cs` — DELETE or MOVE to Drums
  - [ ] `GrooveOrchestrationPolicy.cs` — DELETE or MOVE to Drums
  - [ ] `GrooveRoleConstraintPolicy.cs` — DELETE or MOVE to Drums
  - [ ] `GrooveSubdivisionPolicy.cs` — DELETE or MOVE to Drums
  - [ ] `GrooveTimingPolicy.cs` — DELETE or MOVE to Drums
  - [ ] `GrooveOverrideMergePolicy.cs` — DELETE or MOVE to Drums
  - [ ] `OverrideMergePolicyEnforcer.cs` — DELETE or MOVE to Drums
- [ ] Update any remaining references

**Files to Delete (if not used):**
- Many files in `Generator/Groove/` related to protection/policy

**Files to Move (if still used by Drums):**
- Move to `Generator/Agents/Drums/` with appropriate renaming

---

#### Story 5.4 — Delete Remaining Unused Groove Files

**As a** developer  
**I want** to delete remaining files that are no longer needed  
**So that** Groove namespace is minimal

**Acceptance Criteria:**
- [ ] Review and delete:
  - [ ] `DefaultGroovePolicyProvider.cs` — DELETE (replaced by DrummerPolicyProvider)
  - [ ] `CatalogGrooveCandidateSource.cs` — DELETE (replaced by DrummerCandidateSource)
  - [ ] `GrooveVariationCatalog.cs` — DELETE (Part Generator handles variation)
  - [ ] `GrooveVariationLayer.cs` — DELETE (Part Generator handles variation)
  - [ ] `GrooveVariationLayerMerger.cs` — DELETE
  - [ ] `GrooveCandidateFilter.cs` — DELETE or MOVE to Drums
  - [ ] `GrooveBarPlan.cs` — DELETE or MOVE to Drums
  - [ ] `GrooveBarDiagnostics.cs` — DELETE or MOVE to Drums
  - [ ] `GrooveDiagnosticsCollector.cs` — DELETE or MOVE to Drums
  - [ ] `GroovePresetLibrary.cs` — Simplify or DELETE (replaced by GrooveAnchorFactory)
  - [ ] `GroovePresetDefinition.cs` — Simplify or DELETE
- [ ] Verify all Drum Generator code compiles after deletions
- [ ] Run all tests

**Files to Delete:**
- List above based on usage analysis

---

#### Story 5.5 — Update Documentation

**As a** developer  
**I want** documentation updated to reflect new architecture  
**So that** future development follows correct patterns

**Acceptance Criteria:**
- [ ] Update `ProjectArchitecture.md`:
  - [ ] Simplified Groove System section
  - [ ] Clear boundary: Groove = rhythm onsets, Part Generator = musical intelligence
  - [ ] Updated file organization
- [ ] Update `CurrentEpic.md` to reflect completed refactoring
- [ ] Add AI comments to modified files

**Files to Modify:**
- `AI/Plans/ProjectArchitecture.md`
- `AI/Plans/CurrentEpic.md`

---

## Appendix A: File Disposition Summary

### Files to KEEP in Groove (Simplified)
```
Generator/Groove/
  ├── GrooveInstanceLayer.cs      # MODIFY: add query methods, variation, ToPartTrack
  ├── GrooveAnchorFactory.cs      # RENAME from GrooveTestSetup.cs, simplify
  ├── GroovePresetIdentity.cs     # KEEP AS-IS (may be used for genre metadata)
  ├── GrooveRoles.cs              # KEEP AS-IS
  ├── GrooveFeel.cs               # KEEP (Straight/Swing enum)
  ├── AllowedSubdivision.cs       # KEEP (8th/16th flags)
  ├── OnsetStrength.cs            # KEEP (Downbeat/Backbeat/etc enum)
  ├── GrooveOnset.cs              # KEEP (simplified, no velocity)
```

### Files to MOVE to Drum Generator
```
Generator/Groove/ → Generator/Agents/Drums/
  ├── GrooveCandidateGroup.cs        → DrumCandidateGroup.cs
  ├── GrooveOnsetCandidate.cs        → DrumOnsetCandidate.cs
  ├── GrooveSelectionEngine.cs       → DrumSelectionEngine.cs
  ├── GrooveWeightedCandidateSelector.cs → DrumWeightedCandidateSelector.cs
  ├── IGroovePolicyProvider.cs       → IDrumPolicyProvider.cs
  ├── IGrooveCandidateSource.cs      → IDrumCandidateSource.cs
  ├── GroovePolicyDecision.cs        → DrumPolicyDecision.cs
  ├── GrooveDensityCalculator.cs     → DrumDensityCalculator.cs
  ├── RoleDensityTarget.cs           → (keep name)
  ├── GrooveCapsEnforcer.cs          → DrumCapsEnforcer.cs
  ├── BarContext.cs                  → (keep name or merge)
  ├── GrooveBarContext.cs            → DrumBarContext.cs
  └── [protection/policy files as needed]
```

### Files to DELETE
```
Generator/Groove/
  ├── SegmentGrooveProfile.cs        # Section awareness → Drums
  ├── DefaultGroovePolicyProvider.cs # Replaced by DrummerPolicyProvider
  ├── CatalogGrooveCandidateSource.cs # Replaced by DrummerCandidateSource
  ├── GrooveVariationCatalog.cs      # Part Generator handles
  ├── GrooveVariationLayer.cs        # Part Generator handles
  ├── GrooveVariationLayerMerger.cs  # Part Generator handles
  ├── GroovePresetLibrary.cs         # Replaced by GrooveAnchorFactory
  ├── GroovePresetDefinition.cs      # Simplified away
  └── [unused protection/policy files]
```

---

## Appendix B: Dependency Changes

### Before (Current)
```
SongContext → GroovePresetDefinition → GrooveProtectionPolicy → ...many policies
DrumGenerator → IGroovePolicyProvider → GroovePolicyDecision
DrumGenerator → IGrooveCandidateSource → GrooveCandidateGroup
```

### After (Target)
```
SongContext → GrooveInstanceLayer (simple onset lists)
DrumGenerator → GrooveInstanceLayer (for timing reference)
DrumGenerator → IDrumPolicyProvider (internal to Drums)
DrumGenerator → DrumCandidateGroup (internal to Drums)
```

---

## Estimated Effort

| Phase | Stories | Complexity | Points |
|-------|---------|------------|--------|
| 1 - Simplify Groove | 4 | Small | 6 |
| 2 - PartTrack Conversion | 2 | Small | 3 |
| 3 - UI Integration | 1 | Small | 2 |
| 4 - Move to Drums | 3 | Medium | 8 |
| 5 - Delete/Cleanup | 5 | Medium | 8 |
| **Total** | **15** | | **27** |

---

## Definition of Done (Epic Level)

- [ ] `GrooveInstanceLayer` is the primary groove output (no new GrooveInstance class)
- [ ] `GrooveAnchorFactory.Generate(genre, seed)` creates groove from genre + seed
- [ ] `Generator.GenerateGroovePreview()` creates auditionable PartTrack
- [ ] UI allows entering seed → hearing result (milestone)
- [ ] Candidate selection logic moved to `Generator/Agents/Drums/`
- [ ] Policy interfaces moved to `Generator/Agents/Drums/`
- [ ] Velocity logic exclusively in Drum Generator
- [ ] Unused Groove files deleted
- [ ] Documentation updated
- [ ] All tests passing
- [ ] Manual audition test: different seeds → audibly different grooves

---

*Created:* Response to domain refactoring request  
*Principle:* Reuse existing classes, move to correct domain, delete unused  
*Milestone:* Audition groove instances directly from seed + genre
