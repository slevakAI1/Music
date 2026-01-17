# Epic: Extract Shared Groove Services from DrumTrackGeneratorNew

## Epic Goal
Move groove-related logic out of `DrumTrackGeneratorNew` into reusable, generator-agnostic components so that drums, motifs, comp, melody, and later roles all consume the same timing/grid/phrase/orchestration/protection behavior.

## Guardrails
- **No audible output change** after each story (unless the story explicitly changes behavior).
- **Determinism preserved** (same inputs + seed => same output).
- **Keep APIs small**: builders return immutable “context objects” rather than mutating lists in-place.

## Definition of Done (Epic)
- `DrumTrackGeneratorNew` contains only:
  - drum-specific anchor extraction
  - drum MIDI mapping and emission
  - orchestration of shared services
- Shared services exist for:
  - bar/section/segment context (“intent context”)
  - onset grid / slot legality
  - rhythm slot classification helpers (temporary or grid-driven)
  - role presence gate (orchestration)
  - phrase hook window resolver
  - protection application (at least partly generic)
- Unit tests validate:
  - same drum output before/after refactor
  - services behave deterministically
- Remaining Groove Plan stories 12–22 will be implemented **against the shared services**, not directly in drums.

---

## Phase 0: Baseline Lock (must-do, quick)   (COMPLETED)

### Story G0: Freeze current drum output with a “golden test”
**As a** developer  
**I want** a snapshot test for current drum output  
**So that** refactors don’t accidentally change behavior

**Acceptance Criteria**
- Create `DrumTrackGeneratorGoldenTests.cs`
- Use existing test groove preset + 4–8 bars
- Assert the complete sorted list of emitted events equals expected:
  - `(absoluteTick, noteNumber, velocity, duration)`
- Test passes on current main branch before refactor starts

**Notes**
- This is your safety net. Don’t skip it.

---

## Phase 1: Extract Shared Bar Context

### Story G1: Create generator-agnostic `BarContext` + builder
**As a** developer  
**I want** bar/section/segment context in a shared class  
**So that** all generators can query the same song-structure context

**Acceptance Criteria**
- Create `BarContext` record (rename from `DrumBarContext`)
  - `BarNumber`
  - `Section?`
  - `SegmentGrooveProfile?`
  - `BarWithinSection`
  - `BarsUntilSectionEnd`
- Create `BarContextBuilder.Build(...)`
  - inputs: `SectionTrack`, `IReadOnlyList<SegmentGrooveProfile>`, `totalBars`
  - returns: `IReadOnlyList<BarContext>`
- Drum generator updated to use `BarContextBuilder`
- Golden test still passes (no output change)

---

## Phase 2: Extract Onset Grid / Subdivision Logic     (COMPLETED)

### Story G2: Create `OnsetGrid` and `OnsetGridBuilder`
**As a** developer  
**I want** a reusable onset grid object  
**So that** all generators share the same slot legality rules

**Acceptance Criteria**
- Create `OnsetGrid`:
  - constructed from `beatsPerBar` + `AllowedSubdivision`
  - exposes `bool IsAllowed(decimal beat)` using your epsilon logic
  - (optional) `IReadOnlyList<decimal> Slots` or internal representation
- Create `OnsetGridBuilder.Build(beatsPerBar, allowedSubdivisions)`
- Replace `ApplySubdivisionFilter` usage:
  - drum generator builds grid and filters via `grid.IsAllowed(onset.Beat)`
- Keep old method temporarily as wrapper (optional) but mark `[Obsolete]`
- Golden test still passes

**Note**
- This is the first big “avoid rewrites later” win.

---

## Phase 3: Extract Rhythm Vocabulary Classification (syncopation/anticipation)      (COMPLETED)

### Story G3: Create `RhythmVocabularyFilter` (grid-aware or v1)
**As a** developer  
**I want** syncopation/anticipation detection in a shared service  
**So that** melody/motifs/comp can reuse rhythmic vocabulary concepts

**Acceptance Criteria**
- Create `RhythmVocabularyFilter` with a role-agnostic API:
  - `bool IsAllowed(string roleName, decimal beat, int beatsPerBar, GrooveRoleConstraintPolicy policy)`
  - or `Filter<T>(events, roleSelector, beatSelector, policy)`
- Internally, move:
  - `IsOffbeatPosition`
  - `IsPickupPosition`
- Drum generator calls the new filter
- Golden test still passes

**Implementation advice**
- Keep current detection logic as “v1 straight-grid heuristics”.
- Later, when you implement Story 18 (strength) + Story 20 (feel), you’ll upgrade classification to use slot indices/strength.

---

## Phase 4: Extract Role Presence Gate

### Story G4: Create `RolePresenceGate`
**As a** developer  
**I want** orchestration role presence checks in a shared helper  
**So that** every generator uses identical role gating behavior

**Acceptance Criteria**
- Create `RolePresenceGate.IsRolePresent(sectionType, roleName, orchestrationPolicy)`
- Drum generator replaces `ApplyRolePresenceFilter` with:
  - a simple loop checking `IsRolePresent`
- Golden test still passes

**Note**
- This will be used everywhere: drums, comp, bass, melody, motifs.

---

## Phase 5: Extract Phrase Hook Window Resolution (Story 12 foundation)

### Story G5: Create `PhraseHookWindowResolver` (no behavior change yet)
**As a** developer  
**I want** phrase/section end window calculation in a shared resolver  
**So that** fills/cadences can be gated consistently across generators

**Acceptance Criteria**
- Create a struct/record: `PhraseHookWindowInfo`
  - `bool InPhraseEndWindow`
  - `bool InSectionEndWindow`
  - `IReadOnlyList<string> EnabledFillTags` (may be empty)
- Create resolver:
  - `Resolve(BarContext ctx, GroovePhraseHookPolicy policy)`
- Drum generator uses resolver only to compute info (no new filtering behavior required yet)
- Golden test still passes

**Why “no behavior change yet”?**
- Because your current implementation is partially protection-only and doesn’t yet implement fill tag filtering. This story is extraction + groundwork.

---

## Phase 6: Make Protection Application Less Drum-Specific (recommended)

### Story G6: Create generic `ProtectionApplier` (events with role+beat)
**As a** developer  
**I want** protection enforcement to be role-agnostic  
**So that** other generators can reuse must-hit/never-add/never-remove logic

**Acceptance Criteria**
- Create `ProtectionApplier` that works on any event type:
  - inputs:
    - list of events
    - merged protections per bar: `Dictionary<int, Dictionary<string, RoleProtectionSet>>`
    - delegates:
      - `getBar`, `getRoleName`, `getBeat`
      - `setFlags(...)` (or return updated copy)
      - `createEvent(bar, roleName, beat)`
  - outputs: deduped list
- Drum generator plugs in DrumOnset adapters
- Output unchanged (golden test passes)

**Note**
- If you don’t do this now, you’ll do it later when melody/comp wants the same “must-hit” and “never-add” semantics.

---

## Phase 7: Consolidate Drum Generator into a Thin Orchestrator

### Story G7: Simplify `DrumTrackGeneratorNew` to “compose services”
**As a** developer  
**I want** DrumTrackGeneratorNew to be a thin pipeline orchestrator  
**So that** groove logic doesn’t creep back into drum code

**Acceptance Criteria**
- Drum generator contains only:
  - anchor extraction
  - calling shared services in pipeline order
  - MIDI emission
- All helper methods moved or deleted
- Golden test passes

---

## Phase 8: New tests for shared services (fast wins)

### Story G8: Unit tests for extracted services
**As a** developer  
**I want** tests on shared groove services  
**So that** later changes don’t break cross-generator rhythm consistency

**Acceptance Criteria**
- Tests for:
  - `OnsetGridBuilder` legality for each AllowedSubdivision flag
  - `RolePresenceGate` defaulting behavior
  - `PhraseHookWindowResolver` window correctness
  - `RhythmVocabularyFilter` offbeat/pickup detection behavior
- All deterministic

---

## After this Epic: Resume remaining Groove Stories 12–22 “agnostically”
Once this cleanup epic is done:
- Story 12 becomes: implement *behavior* on top of `PhraseHookWindowResolver`:
  - fill tag enable/disable + protection augmentation as separate outputs
- Story 18 becomes: `OnsetGrid` gains strength labels (Downbeat/Backbeat/etc.)
- Story 20–21 become: `FeelTimingMapper` built as another shared service

---

## Suggested order (min risk, fastest value)
1. **G0** golden test  
2. **G1** BarContext extraction  
3. **G2** OnsetGrid extraction  
4. **G4** RolePresenceGate extraction  
5. **G3** RhythmVocabularyFilter extraction  
6. **G5** PhraseHookWindowResolver extraction  
7. **G6** ProtectionApplier genericization  
8. **G7** drum generator cleanup  
9. **G8** shared tests  
