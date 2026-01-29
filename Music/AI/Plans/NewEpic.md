# Epic: Groove System Domain Refactoring

**Scope:** Refactor the Groove system to its proper domain (rhythm/onset patterns only) and move part-generation concerns (candidates, velocity, policies) to the Drum Generator space.

**Objective:** Create a clean separation where Groove provides rhythm patterns and Part Generators make musical decisions.

---

## Problem Statement

The current Groove system has overstepped its domain. It handles:
- ❌ Velocity determination (should be Part Generator's job)
- ❌ Candidate selection and weighted picking (should be Part Generator's job)
- ❌ Section-aware behavior (Groove should be constant per groove event)
- ❌ Variation constraints (Part Generator handles via style configuration)
- ❌ Policy decisions (Part Generator's domain)

## Target Architecture

### Groove System (New Domain)
**Input:**
1. Genre (e.g., "PopRock")
2. Anchor groove settings (hardcoded per genre)
3. Random seed

**Output:**
- `GrooveInstance` — a deterministic variation of the anchor pattern
- Query API for part generators to get onset data

**NOT Responsible For:**
- Velocities (always owned by Part Generator)
- Candidates (Part Generator generates candidates)
- Section awareness (constant groove per song/groove event)
- Variation constraints (Part Generator via style)

### Part Generator (Drum Generator)
- Uses Groove Instance for timing reference
- Generates candidates via operators
- Applies velocity shaping
- Applies section/energy awareness
- Handles all musical intelligence

---

## Milestone Deliverable

**Goal:** Create a Generator method that:
1. Accepts a random seed and genre
2. Generates a GrooveInstance 
3. Converts GrooveInstance to drum set PartTrack
4. Loads into song grid for audition

**Success Criteria:** Enter different random seeds and hear the groove instance (anchor + variation), unadulterated by part generator logic.

---

## Story Breakdown

### Phase 1: Core Groove Simplification

#### Story 1.1 — Define Simplified GrooveInstance

**As a** developer  
**I want** a clean GrooveInstance that only contains onset positions per role  
**So that** the Groove domain is strictly about rhythm patterns

**Acceptance Criteria:**
- [ ] Create `GrooveInstance` record in `Generator/Groove/`:
  ```csharp
  public sealed record GrooveInstance
  {
      public required string PresetName { get; init; }
      public required int BeatsPerBar { get; init; }
      public required IReadOnlyDictionary<string, IReadOnlyList<decimal>> RoleOnsets { get; init; }
      // Key = role name (e.g., "Kick", "Snare", "ClosedHat")
      // Value = list of beat positions (1-based, fractional OK)
  }
  ```
- [ ] Include helper method: `GetOnsets(string role) → IReadOnlyList<decimal>`
- [ ] Include helper method: `GetAllRoles() → IReadOnlySet<string>`
- [ ] Include helper method: `HasRole(string role) → bool`
- [ ] Immutable by design (all init-only properties)
- [ ] Unit tests: construction, query methods

**Files to Create:**
- `Generator/Groove/GrooveInstance.cs`
- `Music.Tests/Generator/Groove/GrooveInstanceTests.cs`

**Notes:**
- NO velocity, NO timing offset, NO protection flags — pure rhythm positions only
- This is the output of groove generation, consumed by part generators

---

#### Story 1.2 — Create GrooveAnchorLibrary (Genre Presets)

**As a** developer  
**I want** hardcoded anchor patterns per genre  
**So that** each genre has a characteristic base groove

**Acceptance Criteria:**
- [ ] Create `GrooveAnchor` record:
  ```csharp
  public sealed record GrooveAnchor
  {
      public required string Genre { get; init; }
      public required int BeatsPerBar { get; init; }
      public required IReadOnlyDictionary<string, IReadOnlyList<decimal>> RoleOnsets { get; init; }
  }
  ```
- [ ] Create `GrooveAnchorLibrary` static class with:
  - [ ] `GetAnchor(string genre) → GrooveAnchor`
  - [ ] `GetAvailableGenres() → IReadOnlyList<string>`
- [ ] Implement PopRock anchor (4/4):
  - [ ] Kick: [1.0, 3.0] (standard 1 and 3)
  - [ ] Snare: [2.0, 4.0] (backbeat on 2 and 4)
  - [ ] ClosedHat: [1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5] (eighth notes)
- [ ] Throw `ArgumentException` for unknown genre
- [ ] Unit tests: PopRock anchor retrieval, unknown genre handling

**Files to Create:**
- `Generator/Groove/GrooveAnchor.cs`
- `Generator/Groove/GrooveAnchorLibrary.cs`
- `Music.Tests/Generator/Groove/GrooveAnchorLibraryTests.cs`

**Notes:**
- Future: Jazz, Metal, EDM can be added later
- Anchors are read-only reference data, not modified at runtime

---

#### Story 1.3 — Create GrooveVariator (Seed-Based Variation)

**As a** developer  
**I want** a deterministic variator that creates groove variations from anchors  
**So that** different seeds produce different but musically valid rhythms

**Acceptance Criteria:**
- [ ] Create `GrooveVariator` class:
  ```csharp
  public static class GrooveVariator
  {
      public static GrooveInstance CreateVariation(
          GrooveAnchor anchor,
          int seed,
          GrooveVariationSettings? settings = null) → GrooveInstance
  }
  ```
- [ ] Create `GrooveVariationSettings` record:
  ```csharp
  public sealed record GrooveVariationSettings
  {
      public double KickVariationAmount { get; init; } = 0.2; // 0.0-1.0
      public double SnareVariationAmount { get; init; } = 0.1; // Lower = more stable
      public double HatVariationAmount { get; init; } = 0.3;
      public bool AllowKickDoubles { get; init; } = true;
      public bool AllowSnareGhosts { get; init; } = false; // MVP: keep snare simple
      public bool AllowHatOpens { get; init; } = true;
  }
  ```
- [ ] Variation operations (deterministic from seed):
  - [ ] **Kick doubles**: Optionally add kick at 1.5 or 3.5 (based on seed + settings)
  - [ ] **Hat opens**: Optionally replace a closed hat with open hat position marker
  - [ ] **Subdivision changes**: 8ths vs 16ths for hats based on seed
  - [ ] **Syncopation**: Add anticipation (e.g., 3.75 → 4.0) based on seed
- [ ] Use `Rng.Initialize(seed)` then purpose-specific streams
- [ ] **Deterministic**: Same seed + anchor + settings → identical GrooveInstance
- [ ] Unit tests:
  - [ ] Same seed → same output
  - [ ] Different seeds → different outputs
  - [ ] Variation amount affects output
  - [ ] Zero variation amount → unchanged anchor

**Files to Create:**
- `Generator/Groove/GrooveVariator.cs`
- `Generator/Groove/GrooveVariationSettings.cs`
- `Music.Tests/Generator/Groove/GrooveVariatorTests.cs`

**Notes:**
- MVP: Keep variations simple (kick doubles, hat opens)
- This is the creative randomization layer
- Snare kept stable — backbeat is sacred in pop/rock

---

#### Story 1.4 — Create GrooveInstanceGenerator (Facade)

**As a** developer  
**I want** a single entry point to generate a GrooveInstance from genre + seed  
**So that** callers don't need to know internal details

**Acceptance Criteria:**
- [ ] Create `GrooveInstanceGenerator` static class:
  ```csharp
  public static class GrooveInstanceGenerator
  {
      public static GrooveInstance Generate(
          string genre,
          int seed,
          GrooveVariationSettings? settings = null) → GrooveInstance
  }
  ```
- [ ] Implementation:
  1. Get anchor from `GrooveAnchorLibrary.GetAnchor(genre)`
  2. Apply variation via `GrooveVariator.CreateVariation(anchor, seed, settings)`
  3. Return `GrooveInstance`
- [ ] Validate genre parameter (throw if unknown)
- [ ] Deterministic: Same genre + seed + settings → identical output
- [ ] Unit tests:
  - [ ] PopRock generation works
  - [ ] Unknown genre throws
  - [ ] Determinism verified

**Files to Create:**
- `Generator/Groove/GrooveInstanceGenerator.cs`
- `Music.Tests/Generator/Groove/GrooveInstanceGeneratorTests.cs`

---

### Phase 2: Groove Instance to PartTrack Conversion

#### Story 2.1 — Create GrooveInstanceToPartTrack Converter

**As a** developer  
**I want** to convert a GrooveInstance to a playable drum PartTrack  
**So that** I can audition the groove directly

**Acceptance Criteria:**
- [ ] Create `GrooveInstanceConverter` class:
  ```csharp
  public static class GrooveInstanceConverter
  {
      public static PartTrack ToPartTrack(
          GrooveInstance instance,
          BarTrack barTrack,
          int totalBars,
          int defaultVelocity = 100) → PartTrack
  }
  ```
- [ ] Maps roles to GM MIDI drum notes:
  - [ ] "Kick" → 36
  - [ ] "Snare" → 38
  - [ ] "ClosedHat" → 42
  - [ ] "OpenHat" → 46
  - [ ] (extensible for future roles)
- [ ] For each bar 1..totalBars:
  - [ ] For each role in GrooveInstance:
    - [ ] For each onset beat:
      - [ ] Calculate `AbsoluteTimeTicks` from bar + beat using `barTrack`
      - [ ] Create `PartTrackEvent` with:
        - [ ] `Type = NoteOn`
        - [ ] `NoteNumber = mapped MIDI note`
        - [ ] `NoteOnVelocity = defaultVelocity`
        - [ ] `NoteDurationTicks = barTrack.TicksPerBeat / 2` (eighth note duration)
- [ ] Events sorted by `AbsoluteTimeTicks`
- [ ] Unit tests:
  - [ ] PopRock groove converts correctly
  - [ ] Events sorted
  - [ ] Correct MIDI notes for each role

**Files to Create:**
- `Generator/Groove/GrooveInstanceConverter.cs`
- `Music.Tests/Generator/Groove/GrooveInstanceConverterTests.cs`

**Notes:**
- This is a pure data conversion — no musical intelligence
- All onsets get same velocity (defaultVelocity parameter)
- Drum generator will add velocity shaping later

---

#### Story 2.2 — Create GenerateGroovePreview Method

**As a** developer  
**I want** a single method to generate a groove preview from seed + genre  
**So that** I can audition different seeds quickly

**Acceptance Criteria:**
- [ ] Add to `Generator.cs`:
  ```csharp
  public static PartTrack GenerateGroovePreview(
      int seed,
      string genre,
      BarTrack barTrack,
      int totalBars,
      int defaultVelocity = 100) → PartTrack
  ```
- [ ] Implementation:
  1. Call `GrooveInstanceGenerator.Generate(genre, seed)`
  2. Call `GrooveInstanceConverter.ToPartTrack(instance, barTrack, totalBars, defaultVelocity)`
  3. Return PartTrack
- [ ] Unit tests:
  - [ ] Generates valid PartTrack
  - [ ] Different seeds produce different tracks
  - [ ] Same seed produces identical tracks

**Files to Modify:**
- `Generator/Core/Generator.cs`

**Files to Create:**
- `Music.Tests/Generator/Core/GeneratorGroovePreviewTests.cs`

---

### Phase 3: UI Integration for Audition

#### Story 3.1 — Add Groove Preview Command to WriterForm

**As a** user  
**I want** to enter a seed and hear the generated groove  
**So that** I can audition different groove variations

**Acceptance Criteria:**
- [ ] Add menu item or button: "Generate Groove Preview" (or keyboard shortcut)
- [ ] Show simple dialog requesting:
  - [ ] Seed (integer input, default: random)
  - [ ] Genre (dropdown, default: "PopRock")
  - [ ] Bars to generate (default: 8)
- [ ] On confirm:
  1. Call `Generator.GenerateGroovePreview(seed, genre, barTrack, bars)`
  2. Load resulting PartTrack into song grid
  3. Optionally auto-play
- [ ] Display seed used in status bar (for reproduction)
- [ ] Unit tests (if applicable) or manual test steps documented

**Files to Modify:**
- `Writer/WriterForm/WriterForm.cs`
- `Writer/WriterForm/WriterFormEventHandlers.cs` (or appropriate handler file)

**Optional Files to Create:**
- `Writer/EditorForms/GroovePreviewDialog.cs` (if using dialog)

**Notes:**
- MVP: Can be simple input box or toolbar controls
- Key is fast iteration: enter seed → hear result → repeat

---

### Phase 4: Migrate Candidates to Drum Generator Space

#### Story 4.1 — Create DrumCandidateGroup in Drum Generator

**As a** developer  
**I want** candidate concepts moved to the Drum Generator namespace  
**So that** Groove system is free of part-generation concerns

**Acceptance Criteria:**
- [ ] Create `DrumCandidateGroup` in `Generator/Agents/Drums/`:
  ```csharp
  public sealed class DrumCandidateGroup
  {
      public string GroupId { get; set; } = "";
      public List<string> GroupTags { get; set; } = new();
      public int MaxAddsPerBar { get; set; }
      public double BaseProbabilityBias { get; set; }
      public List<DrumCandidate> Candidates { get; set; } = new();
  }
  ```
- [ ] Migrate selection logic from `GrooveWeightedCandidateSelector` to new `DrumCandidateSelector`
- [ ] Update `DrummerCandidateSource` to use new types
- [ ] Unit tests: selection works with new types

**Files to Create:**
- `Generator/Agents/Drums/DrumCandidateGroup.cs`
- `Generator/Agents/Drums/DrumCandidateSelector.cs`

**Files to Modify:**
- `Generator/Agents/Drums/DrummerCandidateSource.cs`

---

#### Story 4.2 — Create DrumPolicyProvider (Move from Groove)

**As a** developer  
**I want** policy decisions to be in the Drum Generator  
**So that** Groove is not making part-generation decisions

**Acceptance Criteria:**
- [ ] Verify `DrummerPolicyProvider` already exists and handles policy
- [ ] Remove `IGroovePolicyProvider` dependency from Groove system core
- [ ] `DrummerPolicyProvider` should directly provide:
  - [ ] Density targets (based on section/energy)
  - [ ] Enabled operators (based on context)
  - [ ] Caps per role
- [ ] Update `GrooveBasedDrumGenerator` to use drummer policies directly
- [ ] Unit tests: policy decisions still work

**Files to Modify:**
- `Generator/Agents/Drums/DrummerPolicyProvider.cs`
- `Generator/Agents/Drums/GrooveBasedDrumGenerator.cs`

---

#### Story 4.3 — Move Velocity Shaping to Drum Generator Only

**As a** developer  
**I want** all velocity logic in the Drum Generator  
**So that** Groove never sets velocities

**Acceptance Criteria:**
- [ ] Remove `Velocity` property from `GrooveOnset` (or make always null)
- [ ] Remove `VelocityHint` from `GrooveOnsetCandidate`
- [ ] Velocity is set ONLY in:
  - [ ] `DrummerVelocityShaper` (performance layer)
  - [ ] Final MIDI conversion step
- [ ] `GrooveInstanceConverter` uses passed-in defaultVelocity (no groove influence)
- [ ] Unit tests: velocities come from drummer, not groove

**Files to Modify:**
- `Generator/Groove/GrooveOnset.cs`
- `Generator/Groove/GrooveOnsetCandidate.cs`
- `Generator/Agents/Drums/Performance/DrummerVelocityShaper.cs`

---

### Phase 5: Simplify Groove System (Remove Overstepping)

#### Story 5.1 — Remove Section Awareness from Groove

**As a** developer  
**I want** Groove to be constant (not section-aware)  
**So that** section handling is the Part Generator's job

**Acceptance Criteria:**
- [ ] Remove `SegmentGrooveProfile` references from core Groove generation
- [ ] Groove instance is same for entire song (or per explicit groove event)
- [ ] Section-aware density changes moved to `DrummerPolicyProvider`
- [ ] Unit tests: groove output is constant regardless of section

**Files to Modify:**
- Multiple Groove files (remove section dependencies)
- `Generator/Agents/Drums/DrummerPolicyProvider.cs` (absorb section logic)

**Notes:**
- `SegmentGrooveProfile` may remain for explicit groove changes (different preset per section)
- But variation/density is Part Generator's domain

---

#### Story 5.2 — Deprecate Groove Candidate System

**As a** developer  
**I want** to mark old groove candidate types as obsolete  
**So that** the codebase migrates to drum-specific types

**Acceptance Criteria:**
- [ ] Add `[Obsolete("Use DrumCandidateGroup instead")]` to:
  - [ ] `GrooveCandidateGroup`
  - [ ] `GrooveOnsetCandidate`
  - [ ] `IGrooveCandidateSource`
  - [ ] `GrooveSelectionEngine`
  - [ ] `GrooveWeightedCandidateSelector`
- [ ] Update all drum code to use new drum-specific types
- [ ] Plan removal in future cleanup pass
- [ ] Document migration path

**Files to Modify:**
- All files with obsolete types

---

#### Story 5.3 — Document New Groove Architecture

**As a** developer  
**I want** clear documentation of the new Groove domain  
**So that** future development follows the correct architecture

**Acceptance Criteria:**
- [ ] Update `ProjectArchitecture.md`:
  - [ ] New Groove System section describing simplified domain
  - [ ] Clear boundary: Groove = rhythm patterns, Part Generator = musical intelligence
  - [ ] Query API documentation
- [ ] Update `CurrentEpic.md` to reflect completed refactoring
- [ ] Add inline comments to new classes

**Files to Modify:**
- `AI/Plans/ProjectArchitecture.md`
- `AI/Plans/CurrentEpic.md`

---

## Appendix A: File Organization After Refactoring

### Groove System (Simplified)
```
Generator/Groove/
  ├── GrooveAnchor.cs                    # Genre anchor pattern record
  ├── GrooveAnchorLibrary.cs             # Static library of genre anchors
  ├── GrooveInstance.cs                  # Generated groove instance (output)
  ├── GrooveInstanceConverter.cs         # Convert to PartTrack
  ├── GrooveInstanceGenerator.cs         # Facade for generation
  ├── GrooveVariator.cs                  # Seed-based variation logic
  ├── GrooveVariationSettings.cs         # Variation configuration
  ├── GrooveRoles.cs                     # Role name constants (Kick, Snare, etc.)
  └── [Legacy files marked obsolete]
```

### Drum Generator (Enhanced)
```
Generator/Agents/Drums/
  ├── DrumCandidateGroup.cs              # Moved from Groove
  ├── DrumCandidateSelector.cs           # Moved from Groove
  ├── DrummerAgent.cs                    # Main facade
  ├── DrummerPolicyProvider.cs           # All policy logic
  ├── DrummerCandidateSource.cs          # Candidate generation
  ├── GrooveBasedDrumGenerator.cs        # Pipeline orchestrator
  ├── Operators/                         # Musical operators
  ├── Performance/                       # Velocity/timing
  └── Physicality/                       # Playability constraints
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
SongContext → GrooveInstance (simple onset map)
DrumGenerator → GrooveInstance (for timing reference)
DrumGenerator → DrummerPolicyProvider (internal)
DrumGenerator → DrumCandidateGroup (internal)
```

---

## Appendix C: Migration Strategy

1. **Phase 1-2**: Create new simplified types alongside existing
2. **Phase 3**: UI integration uses new path
3. **Phase 4**: Migrate drum code to new types
4. **Phase 5**: Deprecate old types, update docs

This allows incremental migration without breaking existing functionality.

---

## Estimated Effort

| Phase | Stories | Complexity | Points |
|-------|---------|------------|--------|
| 1 - Core Simplification | 4 | Medium | 10 |
| 2 - PartTrack Conversion | 2 | Small | 4 |
| 3 - UI Integration | 1 | Small | 2 |
| 4 - Migrate Candidates | 3 | Medium | 9 |
| 5 - Simplify/Deprecate | 3 | Medium | 6 |
| **Total** | **13** | | **31** |

---

## Definition of Done (Epic Level)

- [ ] `GrooveInstance` contains only onset positions (no velocity, no protection)
- [ ] `GrooveInstanceGenerator` creates groove from genre + seed
- [ ] Generator.GenerateGroovePreview creates auditionable PartTrack
- [ ] UI allows entering seed → hearing result
- [ ] Candidate selection logic moved to Drum Generator
- [ ] Velocity shaping exclusively in Drum Generator
- [ ] Old groove types marked obsolete
- [ ] Documentation updated
- [ ] All tests passing
- [ ] Manual audition test: different seeds → audibly different grooves

---

*Created:* Response to domain refactoring request
*Milestone:* Audition groove instances directly from seed + genre
