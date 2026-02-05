# Pre-Analysis: Story 8.1 — Wire Drummer Agent into Generator

**Epic:** Human Drummer Agent  
**Stage:** 8 — Integration & Testing  
**Story ID:** 8.1

---

## 1. Story Intent Summary

**What:**  
This story creates a facade class (`DrummerAgent`) that unifies all drummer components (policy provider, candidate source, memory, operator registry) and integrates it into the main generator pipeline as a replaceable drummer implementation.

**Why:**  
After completing Stages 1-4 (infrastructure, operators, physicality), this story makes the drummer agent actually usable for generating real drum tracks. It bridges the gap between standalone components and the production pipeline, enabling end-to-end testing and validation.

**Who:**  
- **Developers** benefit from a clean integration point and testing surface
- **Generator system** gains a complete human-drummer-modeled instrument agent
- **End users** indirectly benefit (no direct user-facing features yet; this enables future capability)

---

## 2. Acceptance Criteria Checklist

### DrummerAgent Class Construction
1. Create `DrummerAgent` facade class
2. Constructor takes `StyleConfiguration` parameter
3. Implements `IGroovePolicyProvider` (delegates to `DrummerPolicyProvider`)
4. Implements `IGrooveCandidateSource` (delegates to `DrummerCandidateSource`)
5. Owns `DrummerMemory` instance (lifecycle management)
6. Owns `DrumOperatorRegistry` instance (lifecycle management)
7. Provides `Generate(SongContext) → PartTrack` entry point

### Generator Integration
8. Update `Generator.cs` to use `DrummerAgent` when available
9. Fallback to existing groove-only generation when agent not configured

### Verification
10. Manual test: run generation with different seeds, verify variation

**Ambiguities:**
- AC #8-9: What determines "when available" vs "when agent not configured"? Is this a runtime check, a configuration flag, or a null-check?
- AC #10: "Verify variation" — what constitutes sufficient variation? Different note counts? Different patterns? Different operators used?

---

## 3. Dependencies & Integration Points

### Depends On (Completed Stories):
- **1.1-1.4:** Shared agent infrastructure (`IMusicalOperator`, `AgentContext`, `StyleConfiguration`)
- **2.1-2.5:** Drummer core components (`DrummerContext`, `DrumCandidate`, `DrummerPolicyProvider`, `DrummerCandidateSource`, `DrummerMemory`)
- **3.1-3.6:** All 28 drum operators and `DrumOperatorRegistry`
- **4.1-4.4:** Physicality constraints (`PhysicalityFilter`, `PhysicalityRules`)
- **6.1:** `DrummerVelocityShaper` (if complete; check actual status)
- **Stage G:** Groove system hooks (`IGroovePolicyProvider`, `IGrooveCandidateSource`)

### Existing Code This Interacts With:
- `Generator.cs` — Main generation entry point (location: `Generator/Core/Generator.cs`)
- `SongContext` — Input data model containing all song information
- `PartTrack` — Output data model for drum events
- `BarTrack` — Timing ruler from SongContext
- `SectionTrack` — Song structure from SongContext
- `GroovePresetDefinition` — Groove configuration from SongContext
- `DrumTrackGenerator` — Existing drum generation (location: `Generator/Drums/DrumTrackGenerator.cs`)

### Provides For Future Stories:
- **8.2:** Unit tests can now test full drummer pipeline
- **8.3:** Golden regression tests can capture end-to-end output
- **Stage 9+:** Other instrument agents can follow the same facade pattern

---

## 4. Inputs & Outputs

### Inputs (DrummerAgent Constructor):
- `StyleConfiguration` — operator weights, role caps, feel rules, grid rules

### Inputs (Generate Method):
- `SongContext` containing:
  - `BarTrack` — timing ruler with bar boundaries
  - `SectionTrack` — song structure (Intro, Verse, Chorus, etc.)
  - `SegmentGrooveProfiles` — per-section groove overrides
  - `GroovePresetDefinition` — base groove preset with anchor layer
  - `VoiceSet` — instrument/MIDI program mapping
  - Energy/tension context (from Stage 7, if available)
  - Total bars count

### Outputs:
- `PartTrack` — drum track with:
  - `PartTrackMeta` indicating drum kit role
  - `PartTrackNoteEvents` sorted by `AbsoluteTimeTicks`
  - MIDI program number for drum kit
  - All note velocities, durations, timing offsets

### Configuration Read:
- `StyleConfiguration` fields:
  - `StyleId` (e.g., "PopRock")
  - `AllowedOperatorIds`
  - `OperatorWeights`
  - `RoleDensityDefaults`
  - `RoleCaps`
  - `FeelRules` (GrooveFeel, SwingAmount)
  - `GridRules` (AllowedSubdivision)

---

## 5. Constraints & Invariants

### Must Always Be True:
1. **Determinism:** Same `SongContext` + same seed → identical `PartTrack` output
2. **Delegation only:** `DrummerAgent` does NOT contain generation logic; it delegates to existing components
3. **Interface compliance:** Must implement both groove system interfaces exactly as specified
4. **Memory isolation:** Each `DrummerAgent` instance owns its own memory (no shared state)
5. **Sorted events:** Output `PartTrack` events must be sorted by `AbsoluteTimeTicks`
6. **Valid MIDI:** All note numbers, velocities, durations must be in valid MIDI ranges

### Hard Limits (Inherited from Dependencies):
- From `PhysicalityRules`: MaxHitsPerBeat, MaxHitsPerBar, MaxHitsPerRolePerBar
- From `StyleConfiguration.RoleCaps`: per-role max event counts
- From `StickingRules`: MaxConsecutiveSameHand, MaxGhostsPerBar
- MIDI note range: 35-81 (GM2 drum kit)
- Velocity range: 1-127
- Timing offset range: typically ±20 ticks (from `PhysicalityRules.MaxAbsTimingBiasTicks`)

### Operation Order:
1. Initialize `DrummerAgent` with `StyleConfiguration`
2. Build `DrumOperatorRegistry` (or receive pre-built registry)
3. Create `DrummerMemory` instance
4. On `Generate()` call:
   - Validate `SongContext`
   - Build per-bar contexts (`DrummerContext` per bar/role)
   - Call `DrummerPolicyProvider.GetPolicy()` per bar/role
   - Call `DrummerCandidateSource.GetCandidateGroups()` per bar/role
   - Groove system performs selection/filtering (using delegated interfaces)
   - Convert `GrooveOnset` results to `PartTrack` events
   - Return sorted `PartTrack`

---

## 6. Edge Cases to Test

### Boundary Conditions:
- Empty song (0 bars) — should return empty `PartTrack` or error?
- Single bar song
- Very long song (200+ bars) — memory usage/performance
- Song with no sections defined in `SectionTrack`
- Song with unknown section types
- `StyleConfiguration` with empty `AllowedOperatorIds` (all operators allowed)
- `StyleConfiguration` with `AllowedOperatorIds` excluding all operators (nothing generates)

### Null/Missing Data:
- `SongContext.BarTrack` is null
- `SongContext.SectionTrack` is null
- `SongContext.GroovePresetDefinition` is null
- `SongContext.SegmentGrooveProfiles` is empty list
- `StyleConfiguration` is null (constructor validation)
- `DrumOperatorRegistry` fails to build (missing operators)

### Configuration Conflicts:
- `StyleConfiguration.RoleCaps` conflict with `PhysicalityRules.MaxHitsPerBar`
- Operator allowed by style but disabled by policy provider for all bars
- All operators filtered out by physicality constraints for a bar
- Memory-based repetition penalty makes all operators score 0.0

### Determinism Edge Cases:
- Same seed + same context → verify byte-for-byte identical output
- Different seeds → verify outputs differ
- Repeated calls with same inputs → verify no state leakage between calls

### Performance Edge Cases:
- Song with many time signature changes (frequent `BarTrack` lookups)
- Song with many section changes (frequent policy overrides)
- Dense drum parts (approaching `MaxHitsPerBar` limits)
- All 28 operators enabled and generating candidates every bar

### Integration Edge Cases:
- Fallback to groove-only generation when `DrummerAgent` not configured
- Switching between `DrummerAgent` and groove-only generation in same session
- Multiple `DrummerAgent` instances with different `StyleConfiguration`

---

## 7. Clarifying Questions

### Configuration & Lifecycle:
1. **How is `DrummerAgent` "configured" vs "not configured"?**
   - Is there a null-check in `Generator.cs` (if agent is null, use fallback)?
   - Is there a config flag (e.g., `SongContext.UseDrummerAgent`)?
   - Is there a feature flag or registry lookup?

   **Answer:** Use a null-check approach. `Generator.Generate()` will accept an optional `DrummerAgent?` parameter. When null, fallback to existing `DrumTrackGenerator`. This follows the existing pattern where dependencies are passed in rather than configured via flags.

2. **Who creates the `DrumOperatorRegistry`?**
   - Does `DrummerAgent` constructor create it via `DrumOperatorRegistryBuilder.BuildComplete()`?
   - Or does caller pass a pre-built registry?
   - Should registry be frozen at construction time?

   **Answer:** `DrummerAgent` constructor creates the registry internally via `DrumOperatorRegistryBuilder.BuildComplete()`. This encapsulates the complexity and ensures the registry is always properly built and frozen. The caller only needs to provide `StyleConfiguration`.

3. **What is the lifecycle of `DrummerMemory`?**
   - Created fresh per `Generate()` call?
   - Or persisted across multiple `Generate()` calls (song-level memory)?
   - If persisted, how/when is it reset?

   **Answer:** Memory is created once in the `DrummerAgent` constructor and persists for the lifetime of the agent. This allows song-level memory (e.g., "don't repeat the same fill shape across sections"). Different songs should use different `DrummerAgent` instances. A `Reset()` method can be added for reuse if needed later.

### Validation & Error Handling:
4. **What validation should `Generate()` perform?**
   - Throw exceptions for invalid `SongContext`?
   - Return empty `PartTrack` with diagnostic message?
   - Log errors and continue with fallback?

   **Answer:** Follow existing pattern in `Generator.cs`: throw `ArgumentNullException` for null context, throw `ArgumentException` for missing required tracks (BarTrack, SectionTrack, GroovePresetDefinition). This is consistent with existing validation methods like `ValidateSectionTrack()`.

5. **What happens if operators generate zero candidates for all bars?**
   - Return empty drum track?
   - Return anchor-only track (from `GroovePresetDefinition.AnchorLayer`)?
   - Throw exception?

   **Answer:** Return anchor-only track from `GroovePresetDefinition.AnchorLayer`. The groove anchors (kick on 1/3, snare on 2/4) are the foundation and should always be present. The drummer agent adds variation on top but never removes groove fundamentals.

### Delegation Semantics:
6. **Should `DrummerAgent` cache policy decisions per bar?**
   - Or call `DrummerPolicyProvider.GetPolicy()` every time groove system queries?
   - What about performance with repeated calls?

   **Answer:** No caching needed. `DrummerPolicyProvider.GetPolicy()` is a pure function (deterministic, no side effects) and cheap to compute. Caching would add complexity and potential for bugs without measurable benefit.

7. **How does `DrummerAgent` handle diagnostics?**
   - Does it create `DrummerDiagnosticsCollector` instances (Story 7.1)?
   - Or is diagnostics collection opt-in via a flag?
   - If opt-in, how is that flag passed (constructor? method parameter?)?

   **Answer:** Diagnostics are opt-in via an optional `enableDiagnostics` parameter in the constructor. When enabled, the agent creates and exposes a `DrummerDiagnosticsCollector`. This follows the zero-cost-when-disabled pattern established in the groove system.

### Fallback Behavior:
8. **What is "existing groove-only generation"?**
   - Does this refer to `DrumTrackGenerator` from earlier stages?
   - Or a simpler anchor-only approach?
   - Should fallback use same `GroovePresetDefinition` as drummer agent?

   **Answer:** Fallback refers to the existing `DrumTrackGenerator.Generate()` method which extracts anchor patterns from `GroovePresetDefinition`. It uses the same inputs but without operator-based variation. This preserves backward compatibility.

9. **When should fallback be triggered?**
   - Only when `DrummerAgent` is explicitly null/unavailable?
   - Or also when generation fails (catches exceptions)?
   - Should there be logging/warnings when fallback is used?

   **Answer:** Fallback triggers only when `DrummerAgent` is null. Exceptions during generation should propagate (fail fast, don't hide bugs). Silent fallback on error would mask real problems.

### Testing & Verification:
10. **What constitutes "sufficient variation" for AC #10 manual test?**
    - Different note counts per seed?
    - Different operators selected per seed?
    - Different fill placements per seed?
    - Quantifiable metric or subjective listening test?

    **Answer:** For automated tests: verify different total note counts, different operator IDs in diagnostics. For manual verification: listen for different fill patterns, ghost note placements, and hat densities across seeds. Section identity (verse simpler than chorus) is the key musical check.

11. **Should Story 8.1 include any automated tests?**
    - Or is testing entirely in Story 8.2?
    - If automated tests are needed, what scope?

    **Answer:** Story 8.1 includes core automated tests for the facade: construction, delegation verification, basic determinism, and integration with Generator.cs. Story 8.2 adds comprehensive operator and musical behavior tests.

---

## 8. Test Scenario Ideas

### Unit Tests (Story 8.2 Scope, but Informing Design):

#### Construction Tests:
- `DrummerAgent_Constructor_WithValidStyleConfig_Succeeds`
- `DrummerAgent_Constructor_WithNullStyleConfig_ThrowsArgumentNullException`
- `DrummerAgent_Constructor_InitializesMemory`
- `DrummerAgent_Constructor_BuildsOperatorRegistry`

#### Delegation Tests:
- `DrummerAgent_GetPolicy_DelegatesToPolicyProvider`
- `DrummerAgent_GetCandidateGroups_DelegatesToCandidateSource`
- `DrummerAgent_ImplementsIGroovePolicyProvider_Correctly`
- `DrummerAgent_ImplementsIGrooveCandidateSource_Correctly`

#### Determinism Tests:
- `DrummerAgent_Generate_SameSeedSameContext_ProducesSameOutput`
- `DrummerAgent_Generate_DifferentSeeds_ProducesDifferentOutputs`
- `DrummerAgent_Generate_RepeatedCalls_NoStateLeakage`

#### Edge Case Tests:
- `DrummerAgent_Generate_EmptySong_ReturnsEmptyTrack`
- `DrummerAgent_Generate_SingleBar_ProducesValidTrack`
- `DrummerAgent_Generate_NoOperatorsAllowed_ReturnsAnchorOnlyTrack`
- `DrummerAgent_Generate_NullBarTrack_ThrowsValidationException`
- `DrummerAgent_Generate_NullGroovePreset_ThrowsValidationException`

#### Integration Tests:
- `Generator_WithDrummerAgent_ProducesDrumTrack`
- `Generator_WithoutDrummerAgent_FallsBackToGrooveGeneration`
- `Generator_SwitchBetweenAgentAndFallback_BothWork`

### Manual Test Scenarios (AC #10):

**Setup:**
- Create test `SongContext` with:
  - 48 bars
  - Intro-Verse-Chorus-Verse-Chorus-Bridge-Chorus-Outro structure
  - 4/4 time signature
  - 120 BPM tempo
  - PopRock style configuration

**Test Cases:**
1. **Seed Variation Test:**
   - Generate with seed 1, seed 2, seed 3
   - Listen to each output
   - Verify: Different fills, different ghost placements, different hat densities
   - Verify: Same anchors (kick on 1/3, snare on 2/4) across all seeds

2. **Section Identity Test:**
   - Generate with seed 42
   - Verify: Verse is simpler than Chorus
   - Verify: Bridge has contrast from Verse/Chorus
   - Verify: Fills at section boundaries

3. **Operator Diversity Test:**
   - Generate with seed 100
   - Enable diagnostics (if available)
   - Verify: Multiple operator families used across the song
   - Verify: Not just one operator dominating all bars

4. **Fallback Comparison Test:**
   - Generate same song with `DrummerAgent`
   - Generate same song with fallback (groove-only)
   - Compare outputs
   - Verify: Agent output is more varied/human-like
   - Verify: Fallback output is simpler but still musical

### Performance Test Scenarios:
- `Generate_LongSong_200Bars_CompletesInReasonableTime` (define "reasonable" — 5s? 10s?)
- `Generate_DenseDrumPart_AllOperatorsEnabled_NoPerformanceDegradation`

### Snapshot Test Scenarios (Story 8.3):
- Create deterministic fixture with known seed
- Generate drum track
- Serialize to JSON snapshot
- Compare against expected golden file
- Verify: Same input always produces same snapshot

---

## Summary

**Story 8.1** is a **facade/integration story** that unifies completed drummer components into a single entry point and wires it into the generator pipeline. The core challenge is **clean delegation** (no generation logic in `DrummerAgent` itself) and **graceful fallback** when the agent is not available.

**Key Design Questions:**
1. How is agent availability determined? (null-check, config flag, registry)
2. Who builds the operator registry? (agent constructor or caller)
3. What is memory lifecycle? (per-generate or song-level)
4. What validation/error handling is needed?
5. What constitutes "sufficient variation" for manual testing?

**Testing Focus:**
- Determinism (same inputs → same outputs)
- Delegation correctness (interfaces implemented properly)
- Edge cases (empty songs, null data, config conflicts)
- Integration (generator uses agent correctly, fallback works)

**Next Steps:**
1. Clarify open questions (especially #1-5 above)
2. Define validation/error handling strategy
3. Decide on memory lifecycle model
4. Implement facade with delegation pattern
5. Wire into `Generator.cs` with fallback
6. Manual test variation with multiple seeds
