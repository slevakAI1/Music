# Pre-Analysis: Story 10.8.1 — Wire Drummer Agent into Generator

## 1. Story Intent Summary

**What**: Integrate the DrummerAgent into the existing generation pipeline so it becomes the active path for drum track generation.

**Why**: This is the culmination of Stage 10.2-10.7—all drummer components (operators, memory, physicality, performance) exist but aren't yet wired into the actual song generation flow. Without this integration, the drummer agent cannot produce output.

**Who**: Developers benefit from a unified entry point; the generator benefits from policy-driven drum generation; end-users benefit from varied, realistic drum tracks.

---

## 2. Acceptance Criteria Checklist

### DrummerAgent Facade Creation
1. Constructor accepts `StyleConfiguration`
2. Implements `IGroovePolicyProvider` (delegates to `DrummerPolicyProvider`)
3. Implements `IGrooveCandidateSource` (delegates to `DrummerCandidateSource`)
4. Owns `DrummerMemory` instance
5. Owns `DrumOperatorRegistry` instance
6. Provides `Generate(SongContext) → PartTrack` entry point

### Generator.cs Integration
7. Update `Generator.cs` to use `DrummerAgent` when available
8. Fallback to existing groove-only generation when agent not configured

### Validation
9. Manual test: run generation with different seeds, verify variation

**Ambiguous AC:**
- AC #7: "when available" — what determines availability? Configuration flag? Presence of StyleConfiguration? Null check?
- AC #6: The signature `Generate(SongContext) → PartTrack` conflicts with the dual-interface pattern (IGroovePolicyProvider + IGrooveCandidateSource) which suggests the agent plugs INTO the existing groove pipeline rather than replacing it with a top-level Generate method.

---

## 3. Dependencies & Integration Points

### Depends On (Prior Stories)
- **10.1.1-10.1.4**: Common agent infrastructure (IMusicalOperator, AgentContext, IAgentMemory, StyleConfiguration)
- **10.2.1-10.2.5**: Drummer core (DrummerContext, DrumCandidate, DrummerPolicyProvider, DrummerCandidateSource, DrummerMemory)
- **10.3.1-10.3.6**: Operator implementations (28 operators + registry)
- **10.4.1-10.4.4**: Physicality constraints
- **10.6.1-10.6.3**: Performance rendering (velocity/timing shapers)
- **10.7.1**: Diagnostics collector
- **Groove System (A1-H1)**: IGroovePolicyProvider, IGrooveCandidateSource, GrooveBarContext

### Existing Code Interaction
- **Generator.cs**: Top-level generation entry point (currently delegates to DrumTrackGenerator)
- **DrumTrackGenerator**: Existing drum generation pipeline (10-stage pipeline documented in ProjectArchitecture.md)
- **IGroovePolicyProvider**: Groove system hook for policy decisions
- **IGrooveCandidateSource**: Groove system hook for candidate generation
- **SongContext**: Input container with all song design tracks
- **PartTrack**: Output container for MIDI events

### Provides for Future Stories
- **10.8.2**: Unit tests for drummer agent integration
- **10.8.3**: Golden file regression tests
- **Future Agents**: Pattern for integrating Guitar, Keys, Bass, Vocal agents

---

## 4. Inputs & Outputs

### Inputs
- **Constructor**: `StyleConfiguration` (operator weights, caps, feel rules, grid rules)
- **Generate method**: `SongContext` (bars, sections, harmony, groove preset, tempo, time signature)
- **Policy queries**: `GrooveBarContext` (bar number, section type, energy, tension, motif presence)
- **Candidate queries**: `GrooveBarContext` + role

### Outputs
- **Policy decisions**: `GroovePolicyDecision` (density overrides, operator allow lists, feel overrides)
- **Candidate groups**: `IReadOnlyList<GrooveCandidateGroup>` (grouped by operator family)
- **Final output**: `PartTrack` (sorted MIDI events with absolute time ticks)

### Configuration Reads
- `StyleConfiguration.OperatorWeights`
- `StyleConfiguration.RoleCaps`
- `StyleConfiguration.FeelRules`
- `StyleConfiguration.GridRules`
- `SongContext.GroovePresetDefinition` (from existing groove system)
- `SongContext.SegmentGrooveProfiles` (per-section overrides)

---

## 5. Constraints & Invariants

### Always True
- DrummerAgent must implement BOTH `IGroovePolicyProvider` AND `IGrooveCandidateSource`
- Must delegate to existing DrummerPolicyProvider and DrummerCandidateSource (no duplicate logic)
- Memory instance must be shared across all policy/candidate queries within a single generation
- Registry must be frozen before any generation begins
- Output PartTrack events must be sorted by `AbsoluteTimeTicks` (MIDI export validation requirement)
- Determinism: same seed + same SongContext → identical PartTrack output

### Hard Limits
- Style configuration must not be null
- SongContext must be validated before generation (per Generator.cs pattern)
- Registry must contain at least 1 enabled operator for generation to proceed

### Operation Order
1. Construct DrummerAgent with StyleConfiguration
2. Initialize memory (empty state)
3. Build registry from operators
4. Freeze registry
5. For each bar: query policy → query candidates → groove system selects → repeat
6. Convert final onsets to PartTrack
7. Validate PartTrack (sorted events)

---

## 6. Edge Cases to Test

### Boundary Conditions
- Zero bars in SongContext
- Single bar generation
- Empty style configuration (no operators enabled)
- Missing StyleConfiguration (null)
- All operators filtered by physicality (no valid candidates)
- Section with zero energy (should still produce something)
- Fill window at bar 1 (edge of song)

### Error Cases
- Invalid SongContext (null BarTrack, null GroovePresetDefinition)
- Style configuration with negative operator weights
- Style configuration with zero density targets for all sections
- Conflicting overrides (SegmentGrooveProfile vs StyleConfiguration)
- Registry not frozen before generation
- Memory not initialized

### Configuration Conflicts
- StyleConfiguration enables operator, but groove preset forbids it
- Density target = 0.0 but operators generate must-hit candidates
- Role caps exceeded by must-hit protected onsets
- Physicality rules reject all candidates in a bar

### Combination Scenarios
- High energy + fill window + section boundary (all triggers active)
- Memory penalty high for all operators (anti-repetition blocks everything)
- Multiple SegmentGrooveProfiles overlap (which wins?)
- Agent configured but groove preset is null (fallback or error?)

---

## 7. Clarifying Questions

### Integration Strategy
1. **How does DrummerAgent fit into DrumTrackGenerator's 10-stage pipeline?** Does it replace stages 11-13 (generate candidates, apply physicality, select operators) or does it plug into the existing groove hooks?
2. **What does "when available" mean in AC #7?** Is it:
   - Presence of StyleConfiguration in SongContext?
   - Feature flag?
   - Null check on agent instance?
3. **Does Generator.cs create a new DrummerAgent per generation call, or is it a singleton?** Implications for memory state.

### Entry Point Confusion
4. **Why does AC #6 specify `Generate(SongContext) → PartTrack`?** This suggests a top-level entry point, but the dual interface (IGroovePolicyProvider + IGrooveCandidateSource) suggests the agent is a *component* within the existing groove system, not a replacement.
5. **Should DrummerAgent own the full generation pipeline (including conversion to PartTrack), or delegate to DrumTrackGenerator?** ProjectArchitecture.md shows DrumTrackGenerator owns the 10-stage pipeline.

### Fallback Behavior
6. **What is "existing groove-only generation"?** Is this:
   - Groove system with no agent (anchor + variation only)?
   - Legacy drum generation (pre-groove)?
   - Just the base groove anchor layer with no variations?
7. **When should fallback occur?** Only when agent is not configured, or also when agent fails/throws?

### Memory & Registry Lifecycle
8. **Should memory persist across multiple generations (entire song) or reset per PartTrack?** If generating 8-bar Intro then 16-bar Verse, does memory span both?
9. **Is the registry shared across all DrummerAgent instances (static) or per-instance?** Implications for multi-threading.

### Diagnostics Integration
10. **Should DrummerAgent enable diagnostics collection by default, or only when SongContext requests it?** Story 10.7.1 says "opt-in".
11. **Where do diagnostics get stored if enabled?** In PartTrack metadata? Separate output?

### StyleConfiguration Source
12. **Where does StyleConfiguration come from?** Is it:
    - In SongContext already?
    - Loaded from StyleConfigurationLibrary by Generator.cs?
    - User-selected in UI?
13. **Can StyleConfiguration be null, or is that an error?** AC #1 says constructor "takes" it, but doesn't say "requires" it.

### Section Coordination
14. **Do SegmentGrooveProfiles override StyleConfiguration, or merge with it?** If Intro has `EnergyLevel=0.4` in profile but `StyleConfiguration.RoleDensityDefaults["Kick"] = 0.8`, which wins?

---

## 8. Test Scenario Ideas

### Unit Test Names (Based on AC)

#### Facade Behavior
- `DrummerAgent_Constructor_AcceptsStyleConfiguration`
- `DrummerAgent_ImplementsIGroovePolicyProvider`
- `DrummerAgent_ImplementsIGrooveCandidateSource`
- `DrummerAgent_OwnsMemoryInstance_NotShared`
- `DrummerAgent_OwnsRegistryInstance_ImmutableAfterConstruction`

#### Policy Delegation
- `DrummerAgent_GetPolicy_DelegatesToDrummerPolicyProvider`
- `DrummerAgent_GetPolicy_UsesSharedMemory`
- `DrummerAgent_GetPolicy_ConsistentForSameBarContext`

#### Candidate Delegation
- `DrummerAgent_GetCandidates_DelegatesToDrummerCandidateSource`
- `DrummerAgent_GetCandidates_UsesSharedRegistry`
- `DrummerAgent_GetCandidates_RespectsStyleConfiguration`

#### Generator Integration
- `Generator_UsesDrummerAgent_WhenConfigured`
- `Generator_FallsBackToGrooveOnly_WhenAgentNotAvailable`
- `Generator_ValidatesSongContext_BeforeCallingAgent`

#### Output Validation
- `DrummerAgent_Generate_ReturnsSortedPartTrack`
- `DrummerAgent_Generate_ProducesValidMidiEvents`
- `DrummerAgent_Generate_RespectsBarBoundaries`

#### Determinism
- `DrummerAgent_Generate_SameSeed_IdenticalOutput`
- `DrummerAgent_Generate_DifferentSeed_DifferentOutput`
- `DrummerAgent_Generate_DeterministicTieBreak`

#### Edge Cases
- `DrummerAgent_Generate_EmptySongContext_ThrowsValidationError`
- `DrummerAgent_Generate_ZeroBars_ReturnsEmptyPartTrack`
- `DrummerAgent_Generate_NoEnabledOperators_FallsBackToAnchorOnly`
- `DrummerAgent_Generate_PhysicalityRejectsAll_ReturnsAnchorOnly`

#### Memory Behavior
- `DrummerAgent_MemoryPersists_AcrossBars`
- `DrummerAgent_MemoryResets_PerGenerationCall`
- `DrummerAgent_Memory_AntiRepetition_AffectsSelection`

#### Configuration Merging
- `DrummerAgent_SegmentOverride_MergesWithStyle`
- `DrummerAgent_SegmentOverride_PrecedenceOverStyle`

### Test Data Setups

#### Minimal Valid Setup
- 4-bar SongContext
- Single section (Verse)
- PopRock StyleConfiguration
- Basic groove preset (kick 1,3; snare 2,4; hat 8ths)
- Seed = 12345

#### Complex Setup
- 32-bar SongContext with 4 sections (Intro-Verse-Chorus-Verse)
- Multiple SegmentGrooveProfiles with overrides
- High energy (0.9) in Chorus
- Fill windows enabled
- Memory pre-seeded with recent operator usage

#### Stress Setup
- 100-bar generation
- All 28 operators enabled
- Conflicting constraints (high density + strict physicality)
- Multiple role caps active
- Diagnostics enabled

---

## Key Takeaways

### Critical Ambiguities Needing Resolution
1. **Entry point architecture**: Does DrummerAgent replace DrumTrackGenerator or plug into it?
2. **Availability check**: What makes the agent "available" vs requiring fallback?
3. **Memory lifecycle**: Per-generation or persistent across song sections?
4. **Configuration precedence**: How do SegmentGrooveProfiles merge with StyleConfiguration?

### Implementation Risks
- Dual interface (policy + candidate source) may create circular dependency if not carefully managed
- Memory state management across bar-by-bar queries could leak or reset incorrectly
- Fallback path needs careful design to avoid duplicate logic or inconsistent behavior

### Testing Priorities
1. Determinism (must lock before golden tests)
2. Memory behavior (most likely source of bugs)
3. Configuration merging (complex precedence rules)
4. Edge case handling (zero bars, no candidates, etc.)
