# Epic: Human Drummer Agent (Pop Rock)

**Scope:** Build a Pop/Rock drummer agent that generates drum tracks modeled after a skilled human drummer, using the completed groove system hooks.

**Prerequisites:** Groove system completion (Stories A1-H2 complete). All hooks are ready: `IGroovePolicyProvider`, `IGrooveCandidateSource`, `GroovePolicyDecision`, deterministic RNG streams, diagnostics.

**North Star Alignment:** This epic delivers Stage 11 of the NorthStar plan—the first full instrument agent implementation that will inform Stages 12-15 (Guitar, Keys, Bass, Vocal agents).

---

## Guiding Principles

1. **Operator = Musical Move:** Not frozen patterns; procedural candidate generators parameterized by context.
2. **Policy = Timing/Frequency:** When and how often to apply operators (context-aware, style-gated).
3. **Constraints = The Craft:** Playability + idiom + mix-clarity define realism (limbs, kit pieces, density).
4. **Memory = Anti-Repetition + Identity:** Track recent decisions; avoid robotic loops.
5. **Style = Configuration:** Same operator interface; different weights/caps/idioms per genre.
6. **Deterministic:** Same seed + context → identical output.
7. **Shared Infrastructure:** Common agent patterns extracted for reuse by future instrument models.

---

## Definition of "Human Drummer Agent Done"

- Generates drum tracks that vary meaningfully with different seeds
- Implements at least 25 musical operators across 5 operator families
- Pop Rock genre configuration drives operator selection and constraints
- Implements `IGroovePolicyProvider` and `IGrooveCandidateSource` from groove module
- Physicality constraints prevent impossible patterns
- Memory system prevents robotic repetition
- Diagnostics explain all decisions (opt-in)
- Unit tests lock determinism and verify musical sensibility
- Performance rendering applies velocity/timing nuance

---

## Stage 1 — Shared Agent Infrastructure (Reusable Foundation)

**Goal:** Extract common patterns that all instrument agents (drums, guitar, keys, bass, vocal) will share.

---

### Story 1.1 — Define Common Agent Contracts (COMPLETED)

**As a** developer  
**I want** shared interfaces and base types for all instrument agents  
**So that** future agents follow the same architecture

**Acceptance Criteria:**
- [ ] Create `Music.Generator.Agents.Common` namespace
- [ ] Define `IMusicalOperator<TCandidate>` interface:
  - [ ] `OperatorId` (stable string identifier)
  - [ ] `OperatorFamily` (enum: MicroAddition, SubdivisionTransform, PhrasePunctuation, PatternSubstitution, StyleIdiom)
  - [ ] `CanApply(AgentContext context) → bool` (pre-filter check)
  - [ ] `GenerateCandidates(AgentContext context) → IEnumerable<TCandidate>` (procedural generation)
  - [ ] `Score(TCandidate candidate, AgentContext context) → double` (0.0-1.0 scoring)
- [ ] Define `AgentContext` base record:
  - [ ] `BarNumber`, `Beat`, `SectionType`, `PhrasePosition`, `BarsUntilSectionEnd`
  - [ ] `EnergyLevel` (0.0-1.0), `TensionLevel` (0.0-1.0)
  - [ ] `MotifPresenceScore` (0.0-1.0, how busy is the arrangement)
  - [ ] `Seed`, `RngStreamKey`
- [ ] Define `IAgentMemory` interface:
  - [ ] `RecordDecision(barNumber, operatorId, candidateId)`
  - [ ] `GetRecentOperatorUsage(lastNBars) → Dictionary<string, int>`
  - [ ] `GetLastFillShape() → FillShape?`
  - [ ] `GetSectionSignature(sectionType) → List<string>` (recurring choices)
- [ ] Define `OperatorFamily` enum: `MicroAddition`, `SubdivisionTransform`, `PhrasePunctuation`, `PatternSubstitution`, `StyleIdiom`
- [ ] Unit tests verify interfaces compile and can be mocked

**Files to Create:**
- `Generator/Agents/Common/IMusicalOperator.cs`
- `Generator/Agents/Common/AgentContext.cs`
- `Generator/Agents/Common/IAgentMemory.cs`
- `Generator/Agents/Common/OperatorFamily.cs`

---

### Story 1.2 — Implement Agent Memory (Anti-Repetition) (COMPLETED)

**As a** agent  
**I want** memory of recent decisions  
**So that** I avoid robotic repetition and maintain identity

**Acceptance Criteria:**
- [ ] Create `AgentMemory` class implementing `IAgentMemory`
- [ ] Store last N bars of operator usage (configurable, default 8)
- [ ] Store last fill shape (bar position, roles involved, density level)
- [ ] Store section signature choices (key decisions made for each section type)
- [ ] Implement `GetRepetitionPenalty(operatorId) → double` (0.0-1.0):
  - [ ] Returns higher penalty for operators used in recent bars
  - [ ] Configurable decay curve (linear, exponential)
- [ ] Memory is deterministic (same sequence of decisions → same memory state)
- [ ] Unit tests: verify repetition penalty increases with recent usage

**Files to Create:**
- `Generator/Agents/Common/AgentMemory.cs`
- `Generator/Agents/Common/FillShape.cs`

---

### Story 1.3 — Implement Operator Selection Engine (COMPLETED)

**As a** agent  
**I want** weighted selection from operator candidates  
**So that** selection is deterministic but configurable

**Acceptance Criteria:**
- [ ] Create `OperatorSelectionEngine<TCandidate>` class:
  - [ ] Input: list of candidates with scores + style weights + memory penalties
  - [ ] Output: selected candidates respecting density targets and caps
- [ ] Score computation: `finalScore = baseScore * styleWeight * (1.0 - repetitionPenalty)`
- [ ] Selection uses `Rng.RngFor(bar, role, streamKey)` for determinism
- [ ] Deterministic tie-breaking: score desc → operatorId asc → candidateId asc
- [ ] Respects density target (stop when reached)
- [ ] Respects hard caps (never exceed)
- [ ] Unit tests:
  - [ ] Same seed → identical selection
  - [ ] Different seed → different selection when multiple valid options
  - [ ] Caps enforced

**Files to Create:**
- `Generator/Agents/Common/OperatorSelectionEngine.cs`

---

### Story 1.4 — Implement Style Configuration Model (COMPLETED)

**As a** developer  
**I want** style configuration separate from operator logic  
**So that** the same operators work across genres with different weights

**Acceptance Criteria:**
- [ ] Create `StyleConfiguration` record:
  - [ ] `StyleId` (e.g., "PopRock", "Jazz", "Metal")
  - [ ] `AllowedOperatorIds` (list of enabled operators for this style)
  - [ ] `OperatorWeights` (Dictionary<operatorId, double>)
  - [ ] `RoleDensityDefaults` (Dictionary<role, double>)
  - [ ] `RoleCaps` (Dictionary<role, int>)
  - [ ] `FeelRules` (GrooveFeel, SwingAmount, etc.)
  - [ ] `GridRules` (AllowedSubdivision)
- [ ] Create `StyleConfigurationLibrary` with static method `GetStyle(styleId)`
- [ ] Implement "PopRock" as first configuration (detailed in Stage 3)
- [ ] Unit tests: verify PopRock configuration loads correctly

**Files to Create:**
- `Generator/Agents/Common/StyleConfiguration.cs`
- `Generator/Agents/Common/StyleConfigurationLibrary.cs`

---

## Stage 2 — Drummer Agent Core (Pop Rock)  (COMPELTED)

**Goal:** Implement the drummer agent framework with Pop Rock as the first genre.

---

### Story 2.1 — Define Drummer-Specific Context (COMPLETED)

**As a** drummer agent  
**I want** drum-specific context extending the common agent context  
**So that** operators have access to drum-relevant information

**Acceptance Criteria:**
- [ ] Create `DrummerContext` extending `AgentContext`:
  - [ ] `ActiveRoles` (which drum roles are enabled: Kick, Snare, ClosedHat, OpenHat, Crash, Ride, Toms)
  - [ ] `LastKickBeat` (for coordination with bass)
  - [ ] `LastSnareBeat` (for ghost note placement)
  - [ ] `CurrentHatMode` (Closed, Open, Ride)
  - [ ] `HatSubdivision` (Eighth, Sixteenth, None)
  - [ ] `IsFillWindow` (true if in phrase-end fill zone)
  - [ ] `IsAtSectionBoundary` (true if at section start/end)
  - [ ] `BackbeatBeats` (e.g., [2, 4] for 4/4)
- [ ] Create `DrummerContextBuilder` that builds from `GrooveBarContext` + policies
- [ ] Unit tests: context builds correctly from groove inputs

**Files to Create:**
- `Generator/Agents/Drums/DrummerContext.cs`
- `Generator/Agents/Drums/DrummerContextBuilder.cs`

---

### Story 2.2 — Define Drum Candidate Type (COMPLETED)

**As a** drummer agent  
**I want** drum-specific candidate type  
**So that** operators can generate rich drum events

**Acceptance Criteria:**
- [ ] Create `DrumCandidate` record:
  - [ ] `CandidateId` (stable identifier: operatorId + hash of params)
  - [ ] `OperatorId` (which operator generated this)
  - [ ] `Role` (Kick, Snare, ClosedHat, OpenHat, Crash, Ride, Tom1, Tom2, FloorTom)
  - [ ] `BarNumber`, `Beat` (position)
  - [ ] `Strength` (OnsetStrength: Downbeat, Backbeat, Strong, Offbeat, Pickup, Ghost)
  - [ ] `VelocityHint` (0-127, operator-suggested velocity)
  - [ ] `TimingHint` (tick offset, operator-suggested timing)
  - [ ] `ArticulationHint` (optional: Rimshot, SideStick, OpenHat, Crash, etc.)
  - [ ] `FillRole` (None, FillStart, FillBody, FillEnd, Setup)
  - [ ] `Score` (operator-assigned score before style weighting)
- [ ] Unit tests: candidates can be created and scored

**Files to Create:**
- `Generator/Agents/Drums/DrumCandidate.cs`
- `Generator/Agents/Drums/DrumArticulation.cs` (enum)
- `Generator/Agents/Drums/FillRole.cs` (enum)

---

### Story 2.3 — Implement Drummer Policy Provider (COMPLETED)

**As a** groove system  
**I want** drummer agent to implement `IGroovePolicyProvider`  
**So that** drummer decisions drive groove behavior

**Acceptance Criteria:**
- [ ] Create `DrummerPolicyProvider` implementing `IGroovePolicyProvider`
- [ ] `GetPolicy(barContext, role)` returns `GroovePolicyDecision` with:
  - [ ] `EnabledVariationTagsOverride` computed from style + context + memory
  - [ ] `Density01Override` computed from energy level + section type
  - [ ] `MaxEventsPerBarOverride` from style caps
  - [ ] `OperatorAllowList` (operators enabled for this bar based on context)
  - [ ] `RoleTimingFeelOverride` when style dictates
  - [ ] `VelocityBiasOverride` when energy dictates
- [ ] Policy decisions are deterministic for same inputs
- [ ] Unit tests:
  - [ ] Same bar context → same policy
  - [ ] Different energy levels → different density overrides
  - [ ] Fill windows → fill operators enabled

**Files to Create:**
- `Generator/Agents/Drums/DrummerPolicyProvider.cs`

---

### Story 2.4 — Implement Drummer Candidate Source (COMPLETED)

**As a** groove system  
**I want** drummer agent to implement `IGrooveCandidateSource`  
**So that** operator-generated candidates feed into groove selection

**Acceptance Criteria:**
- [ ] Create `DrummerCandidateSource` implementing `IGrooveCandidateSource`
- [ ] `GetCandidateGroups(barContext, role)` returns `IReadOnlyList<GrooveCandidateGroup>`:
  - [ ] Calls enabled operators to generate candidates
  - [ ] Converts `DrumCandidate` to `GrooveOnsetCandidate` with proper mapping
  - [ ] Groups candidates by operator family for structured selection
  - [ ] Applies physicality filter before returning
- [ ] Candidates are deterministic for same context + seed
- [ ] Unit tests:
  - [ ] Same context + seed → same candidates
  - [ ] Operators generate expected candidate types

**Files to Create:**
- `Generator/Agents/Drums/DrummerCandidateSource.cs`
- `Generator/Agents/Drums/DrumCandidateMapper.cs` (maps DrumCandidate → GrooveOnsetCandidate)

---

### Story 2.5 — Implement Drummer Memory (COMPLETED)

**As a** drummer agent  
**I want** drummer-specific memory extending base memory  
**So that** I can track drum-specific patterns

**Acceptance Criteria:**
- [ ] Create `DrummerMemory` extending `AgentMemory`:
  - [ ] `LastFillBar` (which bar had the last fill)
  - [ ] `LastFillShape` (FillShape: roles used, density, ending beat)
  - [ ] `ChorusCrashPattern` (consistent crash placement for this song's choruses)
  - [ ] `HatModeHistory` (track hat subdivision changes)
  - [ ] `GhostNoteFrequency` (rolling average of ghost usage)
- [ ] Anti-repetition for fills: don't repeat exact same fill shape in adjacent sections
- [ ] Section signature: remember what worked for each section type
- [ ] Unit tests: memory tracks and recalls correctly

**Files to Create:**
- `Generator/Agents/Drums/DrummerMemory.cs`

---

## Stage 3 — Drum Operators (25+ Musical Moves)

**Goal:** Implement operator families that generate drum candidates. Each operator is a "musical move" parameterized by context.

---

### Story 3.1 — Micro-Addition Operators (Ghost Notes & Embellishments) (COMPLETED)

**As a** drummer agent  
**I want** operators that add subtle single hits  
**So that** grooves have human-like micro-variation

**Acceptance Criteria:**
- [ ] Implement `IDrumOperator` interface specialization
- [ ] Create operator implementations (7 operators):
  1. [ ] `GhostBeforeBackbeatOperator` — ghost snare at 1.75→2, 3.75→4
  2. [ ] `GhostAfterBackbeatOperator` — ghost snare at 2.25, 4.25
  3. [ ] `KickPickupOperator` — kick at 4.75 leading into next bar
  4. [ ] `KickDoubleOperator` — add kick on 1.5, 3.5 (or 1.25/1.75 on 16th grid)
  5. [ ] `HatEmbellishmentOperator` — add sparse 16th hat notes for interest
  6. [ ] `GhostClusterOperator` — 2-3 ghost notes as a mini-fill
  7. [ ] `FloorTomPickupOperator` — floor tom anticipation on 4.75
- [ ] Each operator:
  - [ ] Implements `CanApply` based on context (energy, section, grid)
  - [ ] Generates candidates with appropriate strength (Ghost, Pickup)
  - [ ] Provides velocity hint (ghosts: 30-50, pickups: 60-80)
  - [ ] Scores based on musical relevance
- [ ] Unit tests: each operator generates expected candidates

**Files to Create:**
- `Generator/Agents/Drums/Operators/MicroAddition/GhostBeforeBackbeatOperator.cs`
- `Generator/Agents/Drums/Operators/MicroAddition/GhostAfterBackbeatOperator.cs`
- `Generator/Agents/Drums/Operators/MicroAddition/KickPickupOperator.cs`
- `Generator/Agents/Drums/Operators/MicroAddition/KickDoubleOperator.cs`
- `Generator/Agents/Drums/Operators/MicroAddition/HatEmbellishmentOperator.cs`
- `Generator/Agents/Drums/Operators/MicroAddition/GhostClusterOperator.cs`
- `Generator/Agents/Drums/Operators/MicroAddition/FloorTomPickupOperator.cs`

---

### Story 3.2 — Subdivision Transform Operators (Timekeeping Changes)  (COMPLETED)

**As a** drummer agent  
**I want** operators that change the timekeeping texture  
**So that** energy and section changes are reflected in the groove

**Acceptance Criteria:**
- [ ] Create operator implementations (5 operators):
  1. [ ] `HatLiftOperator` — switch hats from 8ths to 16ths (with caps)
  2. [ ] `HatDropOperator` — switch hats from 16ths to 8ths for lower energy
  3. [ ] `RideSwapOperator` — switch from hat to ride for different color
  4. [ ] `PartialLiftOperator` — 16ths only on beats 2-4 or last half of bar
  5. [ ] `OpenHatAccentOperator` — open hat on specific beats (1&, 3&) for emphasis
- [ ] Each operator:
  - [ ] Checks `HatSubdivision` in context before applying
  - [ ] Respects energy level (lift at higher energy, drop at lower)
  - [ ] Generates full bar's worth of changed hat pattern
  - [ ] Scores based on section transition relevance
- [ ] Unit tests: verify subdivision changes generate correct patterns

**Files to Create:**
- `Generator/Agents/Drums/Operators/SubdivisionTransform/HatLiftOperator.cs`
- `Generator/Agents/Drums/Operators/SubdivisionTransform/HatDropOperator.cs`
- `Generator/Agents/Drums/Operators/SubdivisionTransform/RideSwapOperator.cs`
- `Generator/Agents/Drums/Operators/SubdivisionTransform/PartialLiftOperator.cs`
- `Generator/Agents/Drums/Operators/SubdivisionTransform/OpenHatAccentOperator.cs`

---

### Story 3.3 — Phrase Punctuation Operators (Boundaries & Fills)

**As a** drummer agent  
**I want** operators that mark phrase and section boundaries  
**So that** the music has clear structure and momentum

**Acceptance Criteria:**
- [ ] Create operator implementations (7 operators):
  1. [ ] `CrashOnOneOperator` — crash cymbal on beat 1 at phrase/section start
  2. [ ] `TurnaroundFillShortOperator` — 2-beat fill at end of phrase (beats 3-4)
  3. [ ] `TurnaroundFillFullOperator` — 1-bar fill at end of section
  4. [ ] `SetupHitOperator` — kick/snare on 4& leading into next section
  5. [ ] `StopTimeOperator` — brief dropout then return (hats off for 2 beats)
  6. [ ] `BuildFillOperator` — ascending tom fill for tension
  7. [ ] `DropFillOperator` — descending tom fill for release
- [ ] Each operator:
  - [ ] Checks `IsFillWindow` and `IsAtSectionBoundary` in context
  - [ ] Generates appropriate fill density based on energy
  - [ ] Avoids overlapping with previous fill (checks memory)
  - [ ] Scores higher at actual phrase boundaries
- [ ] Fill operators use deterministic patterns with seed-based variation
- [ ] Unit tests: fills generate only in appropriate windows

**Files to Create:**
- `Generator/Agents/Drums/Operators/PhrasePunctuation/CrashOnOneOperator.cs`
- `Generator/Agents/Drums/Operators/PhrasePunctuation/TurnaroundFillShortOperator.cs`
- `Generator/Agents/Drums/Operators/PhrasePunctuation/TurnaroundFillFullOperator.cs`
- `Generator/Agents/Drums/Operators/PhrasePunctuation/SetupHitOperator.cs`
- `Generator/Agents/Drums/Operators/PhrasePunctuation/StopTimeOperator.cs`
- `Generator/Agents/Drums/Operators/PhrasePunctuation/BuildFillOperator.cs`
- `Generator/Agents/Drums/Operators/PhrasePunctuation/DropFillOperator.cs`

---

### Story 3.4 — Pattern Substitution Operators (Groove Swaps)  (COMPLETED)

**As a** drummer agent  
**I want** operators that swap entire groove patterns  
**So that** sections have distinct character

**Acceptance Criteria:**
- [ ] Create operator implementations (4 operators):
  1. [ ] `BackbeatVariantOperator` — flam, rimshot, or offset backbeat
  2. [ ] `KickPatternVariantOperator` — four-on-floor vs syncopated vs half-time
  3. [ ] `HalfTimeFeelOperator` — half-time snare pattern (2 and 4 become 3 only)
  4. [ ] `DoubleTimeFeelOperator` — double-time feel with double kicks
- [ ] Each operator:
  - [ ] Checks section type and energy before applying
  - [ ] Generates complete pattern replacement for the bar
  - [ ] Uses style configuration to determine allowed variants
  - [ ] Scores based on section-change relevance
- [ ] CAUTION: These operators should be used sparingly (high memory penalty)
- [ ] Unit tests: pattern variants generate correctly

**Files to Create:**
- `Generator/Agents/Drums/Operators/PatternSubstitution/BackbeatVariantOperator.cs`
- `Generator/Agents/Drums/Operators/PatternSubstitution/KickPatternVariantOperator.cs`
- `Generator/Agents/Drums/Operators/PatternSubstitution/HalfTimeFeelOperator.cs`
- `Generator/Agents/Drums/Operators/PatternSubstitution/DoubleTimeFeelOperator.cs`

---

### Story 3.5 — Style Idiom Operators (Pop Rock Specifics)  (COMPLETED)

**As a** drummer agent  
**I want** genre-specific operators  
**So that** the groove sounds authentically Pop Rock

**Acceptance Criteria:**
- [ ] Create operator implementations (5 operators):
  1. [ ] `PopRockBackbeatPushOperator` — snare slightly ahead for urgency
  2. [ ] `RockKickSyncopationOperator` — rock-style kick anticipations (4&→1)
  3. [ ] `PopChorusCrashPatternOperator` — consistent crash pattern for choruses
  4. [ ] `VerseSimplifyOperator` — thin out verse grooves for contrast
  5. [ ] `BridgeBreakdownOperator` — half-time or minimal pattern for bridges
- [ ] These operators are Pop Rock specific (style-gated)
- [ ] Each operator:
  - [ ] Only applies when `StyleId == "PopRock"`
  - [ ] Uses section type for relevance
  - [ ] Generates style-appropriate candidates
- [ ] Unit tests: verify style gating works

**Files to Create:**
- `Generator/Agents/Drums/Operators/StyleIdiom/PopRockBackbeatPushOperator.cs`
- `Generator/Agents/Drums/Operators/StyleIdiom/RockKickSyncopationOperator.cs`
- `Generator/Agents/Drums/Operators/StyleIdiom/PopChorusCrashPatternOperator.cs`
- `Generator/Agents/Drums/Operators/StyleIdiom/VerseSimplifyOperator.cs`
- `Generator/Agents/Drums/Operators/StyleIdiom/BridgeBreakdownOperator.cs`

---

### Story 3.6 — Operator Registry and Discovery

**As a** drummer agent  
**I want** a registry of all available operators  
**So that** operators can be discovered and enabled by configuration

**Acceptance Criteria:**
- [ ] Create `DrumOperatorRegistry` class:
  - [ ] `RegisterOperator(IDrumOperator operator)`
  - [ ] `GetOperatorsByFamily(OperatorFamily family) → IReadOnlyList<IDrumOperator>`
  - [ ] `GetOperatorById(operatorId) → IDrumOperator?`
  - [ ] `GetAllOperators() → IReadOnlyList<IDrumOperator>`
  - [ ] `GetEnabledOperators(StyleConfiguration style) → IReadOnlyList<IDrumOperator>`
- [ ] Create `DrumOperatorRegistryBuilder` that registers all 28 operators
- [ ] Total operator count: 7 + 5 + 7 + 4 + 5 = **28 operators**
- [ ] Unit tests: registry contains all operators, filtering works

**Files to Create:**
- `Generator/Agents/Drums/DrumOperatorRegistry.cs`
- `Generator/Agents/Drums/DrumOperatorRegistryBuilder.cs`

---

## Stage 4 — Physicality Constraints (Limb Feasibility)    (COMPLETED)

**Goal:** Ensure generated patterns are physically playable by a human drummer.

---

### Story 4.1 — Define Limb Model  (COMPLETED)

**As a** drummer agent  
**I want** a model of what a human drummer can physically play  
**So that** impossible patterns are filtered out

**Acceptance Criteria:**
- [ ] Create `LimbModel` class:
  - [ ] `Limbs` enum: `RightHand`, `LeftHand`, `RightFoot`, `LeftFoot`
  - [ ] `RoleLimbMapping`: which limb plays which role (configurable):
    - Default: RightHand→Hat/Ride, LeftHand→Snare, RightFoot→Kick, LeftFoot→HiHatPedal
  - [ ] `GetRequiredLimb(role) → Limb`
- [ ] Create `LimbAssignment` record: `(Beat, Role, Limb)`
- [ ] Create `LimbConflictDetector`:
  - [ ] `DetectConflicts(List<LimbAssignment>) → List<LimbConflict>`
  - [ ] Conflict = same limb required for overlapping events
- [ ] Unit tests: detect basic conflicts (hat + snare on same beat is OK, but two snares impossible)

**Files to Create:**
- `Generator/Agents/Drums/Physicality/LimbModel.cs`
- `Generator/Agents/Drums/Physicality/LimbAssignment.cs`
- `Generator/Agents/Drums/Physicality/LimbConflictDetector.cs`

---

### Story 4.2 — Implement Sticking Rules

**As a** drummer agent  
**I want** sticking rules enforced  
**So that** fast alternations are realistic

**Acceptance Criteria:**
- [ ] Create `StickingRules` class:
  - [ ] `MaxConsecutiveSameHand` (default: 4 for 16ths)
  - [ ] `MaxGhostsPerBar` (default: 4 for taste)
  - [ ] `MinGapBetweenFastHits` (minimum ticks between same-limb hits)
  - [ ] `ValidatePattern(List<DrumCandidate>) → StickingValidation`
- [ ] `StickingValidation` contains:
  - [ ] `IsValid` (bool)
  - [ ] `Violations` (list of specific rule breaks)
- [ ] Unit tests: sticking violations detected correctly

**Files to Create:**
- `Generator/Agents/Drums/Physicality/StickingRules.cs`
- `Generator/Agents/Drums/Physicality/StickingValidation.cs`

---

### Story 4.3 — Implement Physicality Filter (COMPLETED)

**As a** drummer agent  
**I want** candidates filtered by physicality constraints  
**So that** only playable patterns are selected

**Acceptance Criteria:**
- [ ] Create `PhysicalityFilter` class:
  - [ ] `Filter(List<DrumCandidate>, PhysicalityRules) → List<DrumCandidate>`
  - [ ] Removes candidates that cause limb conflicts
  - [ ] Removes candidates that violate sticking rules
  - [ ] Logs rejections to diagnostics (when enabled)
- [ ] Create `PhysicalityRules` configuration:
  - [ ] `LimbModel`
  - [ ] `StickingRules`
  - [ ] `AllowDoublePedal` (bool, for metal styles)
  - [ ] `StrictnessLevel` (Strict, Normal, Loose)
- [ ] Filter is called by `DrummerCandidateSource` before returning candidates
- [ ] Unit tests: impossible patterns rejected, valid patterns pass

**Files to Create:**
- `Generator/Agents/Drums/Physicality/PhysicalityFilter.cs`
- `Generator/Agents/Drums/Physicality/PhysicalityRules.cs`

---

### Story 4.4 — Add Overcrowding Prevention (COMPLETED)

**As a** drummer agent  
**I want** density caps enforced at physicality level  
**So that** busy passages don't become mush

**Acceptance Criteria:**
- [ ] Add to `PhysicalityRules`:
  - [ ] `MaxHitsPerBeat` (default: 3)
  - [ ] `MaxHitsPerBar` (default: 24 for 16th grid in 4/4)
  - [ ] `MaxHitsPerRolePerBar` (Dictionary<role, int>)
- [ ] Implement pruning in `PhysicalityFilter`:
  - [ ] When caps exceeded, prune lowest-scored candidates first
  - [ ] Never prune protected onsets
  - [ ] Use deterministic tie-break
- [ ] Unit tests: overcrowded bars are thinned correctly

**Files to Create:** (additions to existing files)
- Updates to `PhysicalityFilter.cs` and `PhysicalityRules.cs`


## Stage 5 — Pop Rock Style Configuration (CONSOLIDATED — NO NEW STORIES)

**Status:** Stories 5.1-5.3 have been REMOVED as redundant. The required functionality already exists in earlier stages.

**Rationale:** The following infrastructure already provides complete genre configuration:

| Requirement | Already Implemented In |
|-------------|------------------------|
| Operator weights | Story 1.4: `StyleConfiguration.OperatorWeights` + `StyleConfigurationLibrary.PopRock` |
| Role caps | Story 1.4: `StyleConfiguration.RoleCaps` |
| Feel/Grid rules | Story 1.4: `StyleConfiguration.FeelRules`, `GridRules` |
| Section-aware density | Story 2.3: `DrummerPolicyProvider` computes density from section type (Intro=0.4, Verse=0.5, Chorus=0.8, etc.) |
| Energy modifiers | Story 2.3: `DrummerPolicyProvider` applies energy-based density modifiers |
| Memory settings | Story 2.3: `DrummerPolicySettings` (MinBarsBetweenFills, AllowConsecutiveFills, FillWindowBars) |
| Allowed operators | Story 2.3: `DrummerPolicyProvider` gates operators via policy |
| Groove timing/feel | Groove system: `GroovePresetDefinition`, `GrooveProtectionPolicy`, ~50 groove classes |

**Why This Matters:**
- **No separate PopRockStyleConfiguration.cs file needed** — configuration is distributed across existing systems
- **Adding new genres** only requires: new `StyleConfiguration` preset in `StyleConfigurationLibrary` + optional groove preset
- **Tuning** is an iterative task (adjust existing values), not a story with acceptance criteria

**What Remains:** Fine-tuning the actual numeric values in `StyleConfigurationLibrary.PopRock` and `DrummerPolicySettings` through listening tests. This is ongoing tuning work, not a discrete story.

---


IMPORTANT NOTE: THIS STAGE REQUIRES VSTs THAT SUPPORT DETAILED ARTICULATIONS


## Stage 6 — Performance Rendering (Human Realism)

**Goal:** Apply velocity and timing nuance for realistic drum output.

---

### Story 6.1 — Implement Drummer Velocity Shaper

**As a** drummer agent  
**I want** drum-specific velocity shaping  
**So that** dynamics sound human

**Acceptance Criteria:**
- [ ] Create `DrummerVelocityShaper`:
  - [ ] Uses operator-provided `VelocityHint` as starting point
  - [ ] Applies accent patterns based on beat strength
  - [ ] Ghost notes: 30-50 range
  - [ ] Backbeats: 90-110 range
  - [ ] Fills: crescendo from 70 to 110 typically
  - [ ] Crash hits: 100-127 range
- [ ] Respects groove system's velocity shaping pipeline (integrates, doesn't replace)
- [ ] Unit tests: velocity shaping produces expected ranges

**Files to Create:**
- `Generator/Agents/Drums/Performance/DrummerVelocityShaper.cs`

---

### Story 6.2 — Implement Drummer Timing Nuance

**As a** drummer agent  
**I want** drum-specific timing adjustments  
**So that** pocket feels human

**Acceptance Criteria:**
- [ ] Create `DrummerTimingShaper`:
  - [ ] Snare slightly behind (default +5 ticks) for laid-back feel
  - [ ] Kick on-top or slightly ahead
  - [ ] Hats consistent (low jitter)
  - [ ] Fill notes: slight rush at climax, laid-back in groove
- [ ] Respects groove system's timing pipeline (integrates, doesn't replace)
- [ ] Configurable per style (Pop Rock: standard, Jazz: more behind, Metal: more ahead)
- [ ] Unit tests: timing adjustments in expected ranges

**Files to Create:**
- `Generator/Agents/Drums/Performance/DrummerTimingShaper.cs`

---

### Story 6.3 — Implement Articulation Mapping

IMPORTANT NOTE: This story requires VST voices that support these articulations.
If the target VST does not support them, the mapping should gracefully fall back to standard notes.

**As a** drummer agent  
**I want** articulation hints mapped to MIDI  
**So that** future audio rendering can use them

**Acceptance Criteria:**
- [ ] Create `DrumArticulationMapper`:
  - [ ] Maps `DrumArticulation` enum to MIDI note variations
  - [ ] Rimshot → specific MIDI note (if available in GM2)
  - [ ] SideStick → specific MIDI note
  - [ ] OpenHat → open hat MIDI note (46) vs closed (42)
  - [ ] Crash types → different crash MIDI notes
- [ ] Fallback to standard notes when articulation unavailable
- [ ] Unit tests: articulations map to correct MIDI notes

**Files to Create:**
- `Generator/Agents/Drums/Performance/DrumArticulationMapper.cs`

---

## Stage 7 — Diagnostics & Tuning

**Goal:** Make drummer decisions visible for debugging and future improvement.

---

### Story 7.1 — Implement Drummer Diagnostics Collector

**As a** developer  
**I want** per-bar trace of drummer decisions  
**So that** I can debug and tune the agent

**Acceptance Criteria:**
- [ ] Create `DrummerDiagnostics` record:
  - [ ] `BarNumber`, `Role`
  - [ ] `OperatorsConsidered` (list with scores)
  - [ ] `OperatorsSelected` (list with final scores)
  - [ ] `OperatorsRejected` (list with reasons: physicality, memory, cap)
  - [ ] `MemoryState` (recent operators, fill history)
  - [ ] `DensityTargetVsActual`
  - [ ] `PhysicalityViolationsFiltered`
- [ ] Create `DrummerDiagnosticsCollector`:
  - [ ] Collects diagnostics during generation (opt-in)
  - [ ] Zero-cost when disabled
  - [ ] Non-invasive (read-only)
- [ ] Integrates with groove system diagnostics (Story G1)
- [ ] Unit tests: diagnostics collection doesn't affect output

**Files to Create:**
- `Generator/Agents/Drums/Diagnostics/DrummerDiagnostics.cs`
- `Generator/Agents/Drums/Diagnostics/DrummerDiagnosticsCollector.cs`

---

### Story 7.2 — Implement Benchmark Feature Extraction

**As a** developer  
**I want** to extract analyzable features from generated drums  
**So that** I can compare against human reference tracks

**Acceptance Criteria:**
- [ ] Create `DrumFeatureExtractor`:
  - [ ] `DensityCurve` (hits per bar over song)
  - [ ] `SyncopationProfile` (offbeat hit ratio)
  - [ ] `PunctuationRate` (fills/crashes per section)
  - [ ] `VelocityDistribution` (by beat strength)
  - [ ] `TimingOffsetDistribution` (average offset by role)
  - [ ] `RepetitionScore` (how much adjacent bars differ)
  - [ ] `GhostNoteFrequency` (ghosts per bar average)
- [ ] Features are serializable for comparison
- [ ] Unit tests: features extract correctly from known patterns

**Files to Create:**
- `Generator/Agents/Drums/Diagnostics/DrumFeatureExtractor.cs`
- `Generator/Agents/Drums/Diagnostics/DrumFeatures.cs`

**Note:** This story is preparatory for future benchmark loop (Stage 21 in NorthStar). The comparison against human tracks is a future epic.

---

## Stage 8 — Integration & Testing

**Goal:** Wire everything together and verify end-to-end behavior.

---

### Story 8.1 — Wire Drummer Agent into Generator

**As a** developer  
**I want** drummer agent integrated into the generation pipeline  
**So that** it produces real drum tracks

**Acceptance Criteria:**
- [ ] Create `DrummerAgent` facade class:
  - [ ] Constructor takes `StyleConfiguration`
  - [ ] Implements `IGroovePolicyProvider` (delegates to `DrummerPolicyProvider`)
  - [ ] Implements `IGrooveCandidateSource` (delegates to `DrummerCandidateSource`)
  - [ ] Owns `DrummerMemory` instance
  - [ ] Owns `DrumOperatorRegistry` instance
  - [ ] `Generate(SongContext) → PartTrack` entry point
- [ ] Update `Generator.cs` to use `DrummerAgent` when available
- [ ] Fallback to existing groove-only generation when agent not configured
- [ ] Manual test: run generation with different seeds, verify variation

**Files to Create:**
- `Generator/Agents/Drums/DrummerAgent.cs`

---

### Story 8.2 — Implement Drummer Unit Tests (Core)

**As a** developer  
**I want** comprehensive unit tests  
**So that** behavior is verified and regressions caught

**Acceptance Criteria:**
- [ ] Test: all 28 operators generate valid candidates
- [ ] Test: operator weights affect selection frequency
- [ ] Test: memory penalty affects repetition
- [ ] Test: physicality filter rejects impossible patterns
- [ ] Test: density targets respected
- [ ] Test: section-aware behavior (chorus busier than verse)
- [ ] Test: fill windows respected
- [ ] Test: determinism (same seed → identical output)
- [ ] Test: different seeds → different output
- [ ] Test: Pop Rock configuration loads and applies correctly

**Files to Create:**
- `Music.Tests/Generator/Agents/Drums/DrummerOperatorTests.cs`
- `Music.Tests/Generator/Agents/Drums/DrummerSelectionTests.cs`
- `Music.Tests/Generator/Agents/Drums/DrummerPhysicalityTests.cs`
- `Music.Tests/Generator/Agents/Drums/DrummerDeterminismTests.cs`

---

### Story 8.3 — End-to-End Regression Snapshot (Golden Test)

**As a** developer  
**I want** a golden-file regression test  
**So that** improvements don't accidentally break behavior

**Acceptance Criteria:**
- [ ] Create deterministic test fixture with:
  - [ ] Known seed
  - [ ] Known section structure (Intro-Verse-Chorus-Verse-Chorus-Bridge-Chorus-Outro)
  - [ ] Pop Rock style configuration
- [ ] Generate drum track and serialize snapshot:
  - [ ] Per bar: onset positions, roles, velocities, timing offsets
  - [ ] Operators used per bar (for transparency)
- [ ] Assert snapshot matches expected output exactly
- [ ] Provide controlled way to update snapshot when behavior changes by design

**Files to Create:**
- `Music.Tests/Generator/Agents/Drums/DrummerGoldenTests.cs`
- `Music.Tests/Generator/Agents/Drums/Snapshots/PopRock_Standard.json`

---

## Appendix A: Operator Summary (28 Total)

| Family | Operator | Purpose |
|--------|----------|---------|
| MicroAddition | GhostBeforeBackbeat | Ghost snare before 2 and 4 |
| MicroAddition | GhostAfterBackbeat | Ghost snare after 2 and 4 |
| MicroAddition | KickPickup | Kick anticipation into next bar |
| MicroAddition | KickDouble | Extra kick on offbeats |
| MicroAddition | HatEmbellishment | Sparse 16th hat fills |
| MicroAddition | GhostCluster | Mini ghost-note fills |
| MicroAddition | FloorTomPickup | Tom anticipation |
| SubdivisionTransform | HatLift | 8ths → 16ths |
| SubdivisionTransform | HatDrop | 16ths → 8ths |
| SubdivisionTransform | RideSwap | Hat → Ride |
| SubdivisionTransform | PartialLift | Partial 16ths |
| SubdivisionTransform | OpenHatAccent | Open hat emphasis |
| PhrasePunctuation | CrashOnOne | Section start crash |
| PhrasePunctuation | TurnaroundFillShort | 2-beat fill |
| PhrasePunctuation | TurnaroundFillFull | Full-bar fill |
| PhrasePunctuation | SetupHit | Pre-section accent |
| PhrasePunctuation | StopTime | Brief dropout |
| PhrasePunctuation | BuildFill | Ascending tom fill |
| PhrasePunctuation | DropFill | Descending tom fill |
| PatternSubstitution | BackbeatVariant | Snare articulation swap |
| PatternSubstitution | KickPatternVariant | Kick pattern swap |
| PatternSubstitution | HalfTimeFeel | Half-time feel |
| PatternSubstitution | DoubleTimeFeel | Double-time feel |
| StyleIdiom | PopRockBackbeatPush | Urgent snare timing |
| StyleIdiom | RockKickSyncopation | Rock-style kick patterns |
| StyleIdiom | PopChorusCrashPattern | Chorus crash consistency |
| StyleIdiom | VerseSimplify | Verse thinning |
| StyleIdiom | BridgeBreakdown | Bridge simplification |

---

## Appendix B: File Organization

```
Generator/
  Agents/
    Common/                           # Stage 1 - Shared infrastructure
      IMusicalOperator.cs
      AgentContext.cs
      IAgentMemory.cs
      AgentMemory.cs
      OperatorFamily.cs
      OperatorSelectionEngine.cs
      StyleConfiguration.cs
      StyleConfigurationLibrary.cs
      FillShape.cs
    Drums/                            # Stage 2-8 - Drummer agent
      DrummerAgent.cs                 # Main facade
      DrummerContext.cs
      DrummerContextBuilder.cs
      DrumCandidate.cs
      DrumArticulation.cs
      FillRole.cs
      DrummerPolicyProvider.cs
      DrummerCandidateSource.cs
      DrumCandidateMapper.cs
      DrummerMemory.cs
      DrumOperatorRegistry.cs
      DrumOperatorRegistryBuilder.cs
      Operators/
        IDrumOperator.cs
        MicroAddition/
          GhostBeforeBackbeatOperator.cs
          GhostAfterBackbeatOperator.cs
          KickPickupOperator.cs
          KickDoubleOperator.cs
          HatEmbellishmentOperator.cs
          GhostClusterOperator.cs
          FloorTomPickupOperator.cs
        SubdivisionTransform/
          HatLiftOperator.cs
          HatDropOperator.cs
          RideSwapOperator.cs
          PartialLiftOperator.cs
          OpenHatAccentOperator.cs
        PhrasePunctuation/
          CrashOnOneOperator.cs
          TurnaroundFillShortOperator.cs
          TurnaroundFillFullOperator.cs
          SetupHitOperator.cs
          StopTimeOperator.cs
          BuildFillOperator.cs
          DropFillOperator.cs
        PatternSubstitution/
          BackbeatVariantOperator.cs
          KickPatternVariantOperator.cs
          HalfTimeFeelOperator.cs
          DoubleTimeFeelOperator.cs
        StyleIdiom/
          PopRockBackbeatPushOperator.cs
          RockKickSyncopationOperator.cs
          PopChorusCrashPatternOperator.cs
          VerseSimplifyOperator.cs
          BridgeBreakdownOperator.cs
      Physicality/
        LimbModel.cs
        LimbAssignment.cs
        LimbConflictDetector.cs
        StickingRules.cs
        StickingValidation.cs
        PhysicalityFilter.cs
        PhysicalityRules.cs
      Performance/
        DrummerVelocityShaper.cs
        DrummerTimingShaper.cs
        DrumArticulationMapper.cs
      Diagnostics/
        DrummerDiagnostics.cs
        DrummerDiagnosticsCollector.cs
        DrumFeatureExtractor.cs
        DrumFeatures.cs
```

---

## Appendix C: RNG Stream Keys (New)

Add to `RandomPurpose` enum:

```csharp
// Drummer Agent Stream Keys
DrummerOperatorSelection,
DrummerCandidatePick,
DrummerTieBreak,
DrummerMemoryDecay,
DrummerFillVariation,
DrummerVelocityJitter,
DrummerTimingJitter,
DrummerArticulationPick
```

---

## Appendix D: Story Dependencies

```
STAGE 1: SHARED INFRASTRUCTURE
────────────────────────────────
1.1 (Contracts) → 1.2 (Memory) → 1.3 (Selection) → 1.4 (Style Config)

STAGE 2: DRUMMER CORE
────────────────────────────────
1.1-1.4 → 2.1 (Context) → 2.2 (Candidate) → 2.3 (Policy) → 2.4 (Source) → 2.5 (Memory)

STAGE 3: OPERATORS (can parallelize)
────────────────────────────────
2.2 → 3.1 (Micro) ─┐
2.2 → 3.2 (Subdiv) │
2.2 → 3.3 (Phrase) ├→ 3.6 (Registry)
2.2 → 3.4 (Pattern)│
2.2 → 3.5 (Style) ─┘

STAGE 4: PHYSICALITY
────────────────────────────────
2.2 → 4.1 (Limb) → 4.2 (Sticking) → 4.3 (Filter) → 4.4 (Overcrowd)

STAGE 5: POP ROCK CONFIG (CONSOLIDATED)
────────────────────────────────
[No new stories - functionality in Stories 1.4, 2.3, and groove system]

STAGE 6: PERFORMANCE
────────────────────────────────
4.4 → 6.1 (Velocity) → 6.2 (Timing) → 6.3 (Articulation)

STAGE 7: DIAGNOSTICS
────────────────────────────────
6.3 → 7.1 (Collector) → 7.2 (Benchmark) [SPECULATIVE - needs real tracks]

STAGE 8: INTEGRATION
────────────────────────────────
6.3 → 8.1 (Wire) → 8.2 (Tests) → 8.3 (Golden)
```

---

## Appendix E: Speculative Stories

The following stories are marked as speculative because they depend on earlier implementations and may need refinement:

1. **Story 7.2 (Benchmark Feature Extraction):** Preparatory only. Actual comparison against human tracks is a future epic (Stage 21 in NorthStar).

2. **Value tuning (ongoing):** Operator weights, density curves, and memory settings in `StyleConfigurationLibrary.PopRock` and `DrummerPolicySettings` will need tuning based on listening tests. This is iterative work, not discrete stories.

3. **Articulation mapping (6.3):** Depends on available MIDI drum maps; may need adjustment for specific synths/samples.

4. **Future genre styles:** The architecture supports adding Jazz, Metal, EDM styles later by creating new `StyleConfiguration` implementations. Not part of this epic.

---

## Estimated Effort

| Stage | Stories | Complexity | Points |
|-------|---------|------------|--------|
| 1 - Shared Infrastructure | 4 | Medium | 12 |
| 2 - Drummer Core | 5 | Medium | 15 |
| 3 - Operators | 6 | Large | 24 |
| 4 - Physicality | 4 | Medium | 12 |
| 5 - Pop Rock Config | (consolidated) | — | 0 |
| 6 - Performance | 3 | Medium | 9 |
| 7 - Diagnostics | 2 | Medium | 6 |
| 8 - Integration | 3 | Medium | 9 |
| **Total** | **27** | | **87** |

---

## Definition of Done (Epic Level)

- [ ] All 27 stories completed and tested
- [ ] 28 operators implemented and registered
- [ ] Pop Rock style configuration via existing `StyleConfigurationLibrary.PopRock` + `DrummerPolicyProvider` (no separate PopRockStyleConfiguration.cs)
- [ ] Drummer agent generates varied output for different seeds
- [ ] Physicality constraints prevent impossible patterns (genre-agnostic)
- [ ] Memory system prevents robotic repetition
- [ ] Diagnostics capture all decisions (opt-in)
- [ ] Unit tests cover all components
- [ ] Golden test locks deterministic behavior
- [ ] Integration with existing groove system verified
- [ ] Manual listening test: output sounds musically appropriate for Pop Rock
