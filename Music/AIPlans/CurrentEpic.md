# Epic: Shared Agent Infrastructure (Phase 1)

**Epic ID:** MUSIC-PHASE1  
**Status:** Not Started  
**Target Start:** TBD  
**Estimated Duration:** 1-2 weeks  
**Stage:** 11 (Human Drummer Agent - Foundation)  
**Phase:** 1 of 8

---

## Epic Overview

Build reusable infrastructure components that ALL future instrument agents (Drums, Guitar, Keys, Bass, Vocals) will depend on. This epic establishes the core abstractions, patterns, and systems for the "Expert Musician Agent" architecture described in the NorthStar plan.

**Key Principle:** Agents = Decision Makers, Not Pattern Libraries. Operators generate candidates procedurally; policies decide when to apply them; constraints filter impossible patterns; memory prevents repetition; selection picks from valid options.

All new agent classes will reside in the Music/Generator/Agent folder and `Music.Generator` namespace.
After each story is completed, update `ProjectArchitecture.md` to reflect new files and namespaces.
Do not attempt to update this document. The user will manage it.
---

## Business Value

### Why This Matters

1. **Enables all Stage 11-15 work:** Without these foundations, no instrument agent can be built
2. **Prevents rework:** Shared patterns means future agents don't reinvent these systems
3. **Enforces architectural consistency:** All agents follow the same structural approach
4. **Supports North Star goals:** Determinism, human realism, variation, memory, style-awareness

### What We Get

- Shared agent contracts (`IMusicalOperator`, `AgentContext`, `IAgentMemory`)
- Anti-repetition memory system (prevents robotic loops)
- Deterministic operator selection engine (density targets, caps, scoring)
- Style configuration system (genre-aware behavior without code changes)

### What We DON'T Get (Yet)

- Audible output (infrastructure only—no drum/instrument generation yet)
- Drummer-specific operators (those come in Phase 3-4)
- Performance rendering (Phase 7)
- Integration with generation pipeline (Phase 8)

---

## Goals and Success Criteria

### Goals

1. **Define stable agent contracts** that work across all instrument types
2. **Implement memory system** that tracks recent decisions and prevents repetition
3. **Build selection engine** that respects density targets and caps deterministically
4. **Create style configuration model** that parameterizes behavior by genre

### Success Criteria

| Criterion | Measurement |
|-----------|-------------|
| **Reusability** | All 4 stories produce artifacts usable by Drums, Guitar, Keys, Bass, Vocals |
| **Determinism** | Same inputs + seed → identical memory state and selection results |
| **Testability** | Each component has unit tests verifying contract compliance |
| **Zero breaking changes** | Existing groove system and material system unaffected |
| **Documentation** | Each interface/class has AI-facing comments per coding standards |

---

## Story Breakdown

### Story 1.1 — Define Common Agent Contracts

**Priority:** Critical (blocks all other stories)  
**Estimate:** 2-3 days

#### Intent

Define the shared interfaces and base types that ALL instrument agents will implement. These contracts must be generic enough to work for drums (limb-based), guitar (fretboard-based), keys (hand-span-based), bass (register-based), and vocals (tessitura-based).

#### Acceptance Criteria

- [ ] Create `Music.Generator.Agents.Common` namespace
- [ ] Define `IMusicalOperator<TCandidate>` interface:
  - [ ] `OperatorId` property (stable string identifier)
  - [ ] `OperatorFamily` property (enum: MicroAddition, SubdivisionTransform, PhrasePunctuation, PatternSubstitution, StyleIdiom)
  - [ ] `CanApply(AgentContext context) → bool` method (pre-filter check)
  - [ ] `GenerateCandidates(AgentContext context) → IEnumerable<TCandidate>` method (procedural generation)
  - [ ] `Score(TCandidate candidate, AgentContext context) → double` method (0.0-1.0 scoring)
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
- [ ] Define `OperatorFamily` enum with 5 values
- [ ] Unit tests verify interfaces compile and can be mocked

#### Files to Create

```
Generator/Agents/Common/
  ├── IMusicalOperator.cs          (generic operator interface)
  ├── AgentContext.cs               (shared context base record)
  ├── IAgentMemory.cs               (memory interface)
  ├── OperatorFamily.cs             (operator classification enum)
  └── FillShape.cs                  (data structure for fill memory)
```

#### Technical Notes

- `AgentContext` is a **record** (immutable) to ensure determinism
- `IMusicalOperator<TCandidate>` is generic—drums use `DrumCandidate`, guitar uses `GuitarCandidate`, etc.
- `OperatorFamily` enum values must remain stable (no reordering) for determinism
- `Score()` returns 0.0-1.0 range; final selection uses `score * styleWeight * (1.0 - memoryPenalty)`

#### AI Coding Guidance

```csharp
// AI: purpose=Generic operator interface; all instrument agents implement specialized versions.
// AI: invariants=OperatorId stable across runs; Score returns [0.0..1.0]; CanApply is fast pre-filter.
// AI: deps=Generic over TCandidate; agent-specific contexts extend AgentContext.
public interface IMusicalOperator<TCandidate>
{
    string OperatorId { get; }
    OperatorFamily OperatorFamily { get; }
    bool CanApply(AgentContext context);
    IEnumerable<TCandidate> GenerateCandidates(AgentContext context);
    double Score(TCandidate candidate, AgentContext context);
}
```

---

### Story 1.2 — Implement Agent Memory (Anti-Repetition)

**Priority:** High  
**Estimate:** 2-3 days  
**Depends On:** Story 1.1

#### Intent

Human musicians don't repeat the exact same pattern 8 times. Memory tracks recent decisions and penalizes repetition, creating variation while maintaining identity.

#### Acceptance Criteria

- [ ] Create `AgentMemory` class implementing `IAgentMemory`
- [ ] Store last N bars of operator usage (configurable, default 8)
- [ ] Store last fill shape (bar position, roles involved, density level)
- [ ] Store section signature choices (key decisions made for each section type)
- [ ] Implement `GetRepetitionPenalty(operatorId) → double` (0.0-1.0):
  - [ ] Returns higher penalty for operators used in recent bars
  - [ ] Configurable decay curve (linear, exponential)
- [ ] Memory is deterministic (same sequence of decisions → same memory state)
- [ ] Unit tests: verify repetition penalty increases with recent usage

#### Files to Create

```
Generator/Agents/Common/
  ├── AgentMemory.cs                (concrete memory implementation)
  └── FillShape.cs                  (fill metadata structure)
```

#### Technical Notes

- Memory uses **circular buffer** for efficiency (last N bars)
- Penalty calculation: `penalty = usageCount / windowSize * decayFactor`
- **Deterministic ordering:** memory lookups use sorted keys for stable results
- Section signatures stored as `Dictionary<eSectionType, List<string>>` (operator IDs)

#### Example Usage

```csharp
var memory = new AgentMemory(windowSize: 8, decayCurve: DecayCurve.Exponential);
memory.RecordDecision(barNumber: 5, operatorId: "GhostBeforeBackbeat", candidateId: "ghost-2.75");
memory.RecordDecision(barNumber: 6, operatorId: "GhostBeforeBackbeat", candidateId: "ghost-2.75");

// At bar 7, penalty for GhostBeforeBackbeat is higher (used in bars 5, 6)
double penalty = memory.GetRepetitionPenalty("GhostBeforeBackbeat"); // e.g., 0.6
```

---

### Story 1.3 — Implement Operator Selection Engine

**Priority:** High  
**Estimate:** 3-4 days  
**Depends On:** Story 1.1, Story 1.2

#### Intent

Select candidates from operators using weighted scoring, respecting density targets and caps. Deterministic tie-breaking ensures same seed → same output.

#### Acceptance Criteria

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
  - [ ] Caps enforced (never exceed even if target not reached)
  - [ ] Density target respected (selection stops at target)

#### Files to Create

```
Generator/Agents/Common/
  └── OperatorSelectionEngine.cs    (selection logic)
```

#### Technical Notes

- Selection is **greedy with scoring:** pick highest-scored candidate until density target reached
- **Deterministic RNG:** use dedicated stream per (bar, role, purpose) via `Rng.RngFor()`
- **Cap enforcement:** hard stop—if cap reached, no more candidates regardless of target
- **Tie-breaking:** when scores equal (within epsilon 0.0001), use lexicographic sort

#### Selection Algorithm

```
1. Compute finalScore for each candidate
2. Sort by: finalScore desc → operatorId asc → candidateId asc
3. currentDensity = 0
4. selectedCandidates = []
5. For each candidate in sorted order:
     If currentDensity >= densityTarget: STOP
     If selectedCandidates.Count >= hardCap: STOP
     Add candidate to selectedCandidates
     currentDensity += candidate.DensityContribution
6. Return selectedCandidates
```

---

### Story 1.4 — Implement Style Configuration Model

**Priority:** Medium  
**Estimate:** 2-3 days  
**Depends On:** Story 1.1

#### Intent

Separate style (Pop Rock, Jazz, Metal, etc.) from operator logic. Same operators work across genres with different weights, caps, and idioms.

#### Acceptance Criteria

- [ ] Create `StyleConfiguration` record:
  - [ ] `StyleId` (e.g., "PopRock", "Jazz", "Metal")
  - [ ] `AllowedOperatorIds` (list of enabled operators for this style)
  - [ ] `OperatorWeights` (Dictionary<operatorId, double>)
  - [ ] `RoleDensityDefaults` (Dictionary<role, double>)
  - [ ] `RoleCaps` (Dictionary<role, int>)
  - [ ] `FeelRules` (GrooveFeel, SwingAmount, etc.)
  - [ ] `GridRules` (AllowedSubdivision)
- [ ] Create `StyleConfigurationLibrary` with static method `GetStyle(styleId)`
- [ ] Implement "PopRock" as first configuration (detailed values TBD in Phase 5)
- [ ] Unit tests: verify PopRock configuration loads correctly

#### Files to Create

```
Generator/Agents/Common/
  ├── StyleConfiguration.cs         (configuration record)
  └── StyleConfigurationLibrary.cs  (configuration registry)
```

#### Technical Notes

- `StyleConfiguration` is a **record** (immutable) for safety
- Configurations are **static/hardcoded** initially (future: load from JSON/DB)
- Missing operator weights default to 0.5 (medium priority)
- Missing role caps default to `int.MaxValue` (no cap)

#### Example Configuration (Stub for PopRock)

```csharp
public static StyleConfiguration PopRock => new StyleConfiguration
{
    StyleId = "PopRock",
    AllowedOperatorIds = new List<string>
    {
        // Populated in Phase 3-4 when operators exist
    },
    OperatorWeights = new Dictionary<string, double>
    {
        // Populated in Phase 5 (Story 5.1)
    },
    RoleDensityDefaults = new Dictionary<string, double>
    {
        { GrooveRoles.Kick, 0.6 },
        { GrooveRoles.Snare, 0.5 },
        { GrooveRoles.ClosedHat, 0.7 },
        // More in Phase 5
    },
    RoleCaps = new Dictionary<string, int>
    {
        { GrooveRoles.Kick, 8 },
        { GrooveRoles.Snare, 6 },
        { GrooveRoles.ClosedHat, 16 },
        // More in Phase 5
    },
    FeelRules = new FeelRules
    {
        DefaultFeel = GrooveFeel.Straight,
        SwingAmount = 0.0
    },
    GridRules = new GridRules
    {
        AllowedSubdivision = GridSubdivision.Sixteenth
    }
};
```

---

## Testing Strategy

### Unit Tests (Required for All Stories)

| Story | Test Coverage |
|-------|---------------|
| 1.1 | Interface contracts compile; mocks work; enum values stable |
| 1.2 | Memory tracks usage; penalties increase with repetition; decay works; determinism |
| 1.3 | Selection respects targets/caps; determinism (same seed → same output); tie-breaking |
| 1.4 | Configurations load; defaults apply; style lookup works |

### Test Conventions

- Use **xUnit** framework (existing project standard)
- Constructor-based RNG initialization: `Rng.Initialize(42);`
- Method naming: `<Component>_<Condition>_<ExpectedResult>`
- Use `#region` blocks to organize test categories
- **Determinism tests mandatory** for 1.2 and 1.3

### Example Test Structure

```csharp
public class OperatorSelectionEngineTests
{
    public OperatorSelectionEngineTests()
    {
        Rng.Initialize(42); // Determinism
    }

    #region Density Target Tests

    [Fact]
    public void SelectCandidates_StopsAtDensityTarget()
    {
        // Arrange: candidates with known densities
        // Act: select until target
        // Assert: total density >= target, but no excess beyond next candidate
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void SelectCandidates_SameSeed_IdenticalOutput()
    {
        // Arrange: same candidates, same seed
        // Act: select twice
        // Assert: outputs match exactly
    }

    #endregion
}
```

---

## Dependencies

### Upstream Dependencies (Must Exist Before Start)

| Dependency | Status | Notes |
|------------|--------|-------|
| `Rng` system | ✅ Exists | `Generator/Core/Randomization/Rng.cs` |
| `GrooveRoles` constants | ✅ Exists | Used in style configuration |
| `MusicConstants.eSectionType` | ✅ Exists | Used in `AgentContext` |
| `GrooveFeel` enum | ✅ Exists | Used in style configuration |

### Downstream Dependencies (Blocked Until This Epic Complete)

| Dependency | Blocked Work | Phase |
|------------|--------------|-------|
| Drummer-specific context | `DrummerContext` extends `AgentContext` | Phase 3 |
| Drum operators | All drum operators implement `IMusicalOperator<DrumCandidate>` | Phase 4 |
| Guitar/Keys/Bass/Vocal agents | All future agents use these foundations | Stages 12-15 |

---

## Definition of Done (Epic Level)

### Code Complete

- [ ] All 4 stories implemented and tested
- [ ] All files created in `Generator/Agents/Common/` namespace
- [ ] Zero compilation errors or warnings
- [ ] All unit tests passing (minimum 90% coverage for new code)

### Quality Gates

- [ ] **Determinism verified:** Same seed → identical memory state and selection results
- [ ] **Reusability verified:** Interfaces work with mocked drummer, guitar, keys candidates
- [ ] **Zero breaking changes:** Existing groove system and material system still compile and pass tests
- [ ] **AI comments added:** All public interfaces/classes have compact AI-facing comments per coding standards

### Documentation

- [ ] AI comments follow 140-char limit and key:value style
- [ ] Public APIs documented with purpose, invariants, deps, change guidance
- [ ] `ProjectArchitecture.md` updated with new namespace and file references

### Knowledge Transfer

- [ ] Code review completed by at least one other developer
- [ ] Shared patterns demonstrated with toy example (e.g., mock drummer operator)

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Interfaces too rigid** | Future agents can't adapt | Prototype with 2 mock agents (drums, guitar) before finalizing |
| **Determinism breaks** | Same seed produces different output | Mandatory determinism tests; use sorted collections |
| **Premature abstraction** | Over-engineered for unknown needs | Keep interfaces minimal; extend later if needed |
| **Memory overhead** | Storing 8 bars × all operators = memory bloat | Circular buffer; configurable window size |

---

## Out of Scope (Explicitly NOT in This Epic)

- ❌ Drummer-specific operators (Phase 4)
- ❌ Physicality constraints (Phase 5)
- ❌ Performance rendering (Phase 7)
- ❌ Integration with Generator.cs (Phase 8)
- ❌ Pop Rock style weights/densities (Phase 5)
- ❌ Diagnostics collection (Phase 7)
- ❌ Golden snapshot tests (Phase 8)

---

## Success Metrics

### Objective Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Test coverage** | ≥90% for new code | Code coverage tools |
| **Zero breaking changes** | 100% existing tests pass | CI/CD pipeline |
| **Determinism** | 100% of selection/memory tests pass with fixed seed | Unit tests |
| **Reusability** | 2+ mock agents use interfaces successfully | Prototype validation |

### Subjective Metrics (Post-Epic Review)

- Interfaces feel "right" for drums, guitar, keys, bass, vocals
- Developers understand how to extend for new operators
- Memory system prevents obvious repetition in manual tests

---

## Appendix A: File Structure After Epic

```
Music/
  Generator/
    Agents/
      Common/                           # NEW (this epic)
        IMusicalOperator.cs
        AgentContext.cs
        IAgentMemory.cs
        AgentMemory.cs
        OperatorFamily.cs
        OperatorSelectionEngine.cs
        StyleConfiguration.cs
        StyleConfigurationLibrary.cs
        FillShape.cs
```

---

## Appendix B: Integration with Existing Systems

### How This Epic Connects to Groove System

- `AgentContext` includes `MotifPresenceScore` (queries `MotifPresenceMap`)
- `AgentContext` includes `EnergyLevel` and `TensionLevel` (from Stage 7 intent system)
- Selection engine uses existing `Rng` system for determinism

### How This Epic Connects to Material System

- `AgentContext.MotifPresenceScore` indicates how busy the arrangement is
- Memory system's `GetLastFillShape()` will be used by `MotifPresenceMap` integration (Phase 6)

### How This Epic Enables Stage 11-15

- **Stage 11 (Drummer):** `DrummerContext` extends `AgentContext`; drum operators implement `IMusicalOperator<DrumCandidate>`
- **Stage 12 (Guitar):** `GuitarContext` extends `AgentContext`; guitar operators implement `IMusicalOperator<GuitarCandidate>`
- **Stage 13-15 (Keys, Bass, Vocals):** Same pattern

---

## Appendix C: Example Mock Implementation (Validation)

After Story 1.1, validate interfaces with a toy example:

```csharp
// Mock drummer operator for validation
public class MockGhostNoteOperator : IMusicalOperator<MockDrumCandidate>
{
    public string OperatorId => "MockGhostNote";
    public OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

    public bool CanApply(AgentContext context)
    {
        // Only apply if energy > 0.5 and not in fill window
        return context.EnergyLevel > 0.5 && context.BarsUntilSectionEnd > 1;
    }

    public IEnumerable<MockDrumCandidate> GenerateCandidates(AgentContext context)
    {
        // Generate ghost note before backbeat (beat 1.75)
        yield return new MockDrumCandidate
        {
            CandidateId = "ghost-1.75",
            Beat = 1.75m,
            Role = "Snare",
            Score = 0.8
        };
    }

    public double Score(MockDrumCandidate candidate, AgentContext context)
    {
        return candidate.Score; // Simplified
    }
}
```

---

## Next Steps After Epic Completion

1. **Begin Phase 2:** Complete `MotifRenderer` (Story 9.2) for quick audible wins
2. **Begin Phase 3:** Build `DrummerContext` and drum-specific agent core (Stories 2.1-2.5)
3. **Update ProjectArchitecture.md:** Document new namespace and integration points
4. **Demo shared infrastructure:** Show how drum and guitar operators would use same interfaces

---

*This epic establishes the foundation for all Stage 11-15 agent work. Success here means smooth development for 5 instrument agents.*
