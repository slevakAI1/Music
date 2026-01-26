# Pre-Analysis: Story 7.1 — Implement Drummer Diagnostics Collector

## 1. Story Intent Summary

**What:** Build a diagnostics collection system that captures per-bar decision traces during drum generation, showing which operators were considered, selected, or rejected and why.

**Why:** Essential for debugging drummer agent behavior, tuning operator weights, understanding why certain patterns emerge, and validating that the agent behaves as designed. Without visibility into decision-making, the system is a black box.

**Who Benefits:**
- **Developers:** Debug unexpected patterns, verify operator selection logic, tune weights
- **Future stages:** Establishes pattern for Guitar/Keys/Bass/Vocal agent diagnostics
- **End-users (indirectly):** Better tuning leads to more musical output

---

## 2. Acceptance Criteria Checklist

### DrummerDiagnostics Record Structure
1. [ ] Store `BarNumber` and `Role` for each diagnostic trace
2. [ ] Capture `OperatorsConsidered` with their initial scores
3. [ ] Capture `OperatorsSelected` with their final scores after weighting
4. [ ] Capture `OperatorsRejected` with rejection reasons (physicality, memory, cap)
5. [ ] Capture `MemoryState` (recent operators, fill history)
6. [ ] Capture `DensityTargetVsActual` comparison
7. [ ] Capture `PhysicalityViolationsFiltered` (what was removed and why)

### DrummerDiagnosticsCollector Behavior
8. [ ] Collect diagnostics during generation (opt-in mechanism)
9. [ ] Zero-cost when disabled (no performance impact)
10. [ ] Non-invasive implementation (read-only access to state)

### Integration
11. [ ] Integrate with groove system diagnostics (Story G1: `GrooveBarDiagnostics`)

### Testing
12. [ ] Unit tests verify diagnostics collection doesn't affect output (determinism preserved)

---

## 3. Dependencies & Integration Points

### Story Dependencies
- **Story G1 (Complete):** Groove diagnostics system (`GrooveBarDiagnostics`, `GrooveDiagnosticsCollector`)
- **Stage 1-4 (Complete):** All drummer agent infrastructure (operators, memory, selection, physicality)
- **Story 2.3 (Complete):** `DrummerPolicyProvider` (policy decisions to trace)
- **Story 2.4 (Complete):** `DrummerCandidateSource` (candidate generation to trace)
- **Story 3.6 (Complete):** `DrumOperatorRegistry` (operator metadata)

### Integration Points
**Will Interact With:**
- `DrummerCandidateSource.GetCandidateGroups()` — capture operator invocations
- `OperatorSelectionEngine.SelectCandidates()` — capture selection decisions
- `PhysicalityFilter.Filter()` — capture rejection reasons
- `DrummerMemory` — capture memory state snapshots
- `DrummerPolicyProvider.GetPolicy()` — capture density targets
- `GrooveDiagnosticsCollector` — integrate with existing groove diagnostics

**Provides For:**
- Story 7.2 (Benchmark Feature Extraction) — diagnostic data for analysis
- Story 8.2 (Unit Tests) — diagnostic validation
- Story 8.3 (Golden Tests) — diagnostic snapshot comparison
- Future agent stages (Guitar/Keys/Bass/Vocal) — reusable diagnostics pattern

---

## 4. Inputs & Outputs

### Inputs (Data to Capture)
**From OperatorSelectionEngine:**
- List of candidates with base scores
- List of candidates with final scores (after style weighting + memory penalty)
- Selected candidates
- Rejected candidates with reasons

**From PhysicalityFilter:**
- Violation records (limb conflicts, sticking violations, overcrowding)
- Filtered candidate IDs with rejection reasons

**From DrummerPolicyProvider:**
- Density target (computed from section/energy)
- Actual density achieved

**From DrummerMemory:**
- Recent operator usage history
- Last fill shape
- Section signatures

**From DrummerContext:**
- Bar number
- Role
- Section type
- Energy level
- Fill window status

### Outputs (Produced Data)
**DrummerDiagnostics record per (bar, role):**
```csharp
public sealed record DrummerDiagnostics
{
    public int BarNumber { get; init; }
    public string Role { get; init; }
    public IReadOnlyList<OperatorTrace> OperatorsConsidered { get; init; }
    public IReadOnlyList<OperatorTrace> OperatorsSelected { get; init; }
    public IReadOnlyList<RejectionTrace> OperatorsRejected { get; init; }
    public MemoryStateSnapshot MemoryState { get; init; }
    public DensityComparison DensityTargetVsActual { get; init; }
    public IReadOnlyList<PhysicalityViolation> PhysicalityViolationsFiltered { get; init; }
}
```

**DrummerDiagnosticsCollector:**
- Collection of `DrummerDiagnostics` records (per bar, per role)
- Query methods for accessing diagnostics by bar/role
- Serialization methods for export/analysis

### Configuration
- Opt-in flag (e.g., `bool EnableDiagnostics` in generation settings)
- Diagnostics output path (optional, for file export)
- Verbosity level (optional: minimal, standard, verbose)

---

## 5. Constraints & Invariants

### Zero-Cost When Disabled
- **MUST:** When diagnostics disabled, no allocations or processing overhead
- **MUST:** Production builds should have diagnostics disabled by default
- **MUST:** Diagnostic collection cannot introduce race conditions

### Read-Only / Non-Invasive
- **MUST:** Diagnostics never modify generation state
- **MUST:** Diagnostics never affect decision-making (no side effects)
- **MUST:** Same seed + context → identical output whether diagnostics on or off

### Determinism Preservation
- **MUST:** Diagnostics collection must not affect RNG state
- **MUST:** Diagnostics collection must not alter memory state
- **MUST:** Diagnostics collection must not change operator selection

### Integration with Groove Diagnostics
- **MUST:** Drummer diagnostics complement (not duplicate) groove diagnostics
- **MUST:** Same opt-in mechanism as groove diagnostics
- **MUST:** Diagnostics can be queried together (drummer + groove)

### Memory Management
- **SHOULD:** Diagnostics use bounded memory (cap total records)
- **SHOULD:** Provide mechanism to clear old diagnostics
- **SHOULD:** Serialize large diagnostic sets incrementally

---

## 6. Edge Cases to Test

### Empty/Null Inputs
1. **No operators apply:** All `CanApply()` return false → `OperatorsConsidered` empty
2. **No candidates generated:** Operators apply but generate zero candidates
3. **All candidates rejected:** Physicality filter rejects everything
4. **Memory is empty:** No history to capture in `MemoryState`
5. **Unknown role:** Role not in any operator's capability

### Boundary Conditions
6. **Bar 1:** First bar, minimal memory history
7. **Last bar:** End of song, edge of section
8. **Single operator:** Only one operator applies (no selection competition)
9. **Tied scores:** Multiple candidates with identical scores (deterministic tie-break trace)
10. **Max density hit:** Density cap reached exactly

### Rejection Scenarios
11. **Limb conflict rejection:** Multiple candidates conflict, which survives?
12. **Sticking violation rejection:** Too many same-hand hits
13. **Overcrowding rejection:** MaxHitsPerBar exceeded
14. **Memory penalty rejection:** Operator used too recently
15. **Combined rejections:** Candidate violates multiple constraints

### Integration Scenarios
16. **Diagnostics + groove diagnostics both enabled:** No conflicts, both collect
17. **Diagnostics disabled mid-generation:** Clean disable without errors
18. **Multiple roles in same bar:** Each role gets separate diagnostic trace
19. **Fill window active:** Fill operators behavior traced correctly
20. **Section boundary:** Memory state transition visible

### Performance Scenarios
21. **Long songs (100+ bars):** Memory doesn't grow unbounded
22. **Dense bars (20+ candidates):** Diagnostic collection stays fast
23. **Repeated generation:** Diagnostics reset correctly between runs
24. **Concurrent generation:** Thread-safe if parallelized (future)

---

## 7. Clarifying Questions

### Scope & Granularity
1. **What level of detail for scores?** Capture base score + style weight + memory penalty separately, or just final score?
2. **How much memory history?** Last 8 bars (default memory window) or configurable?
3. **Should we capture intermediate states?** (e.g., before/after physicality filter)
4. **What about sub-operator decisions?** (e.g., fill pattern selection within BuildFillOperator)

### Rejection Reasons Format
5. **Structured or string reasons?** Should rejection reasons be enums or free-form strings?
6. **Nested reasons?** (e.g., "PhysicalityFilter > LimbConflict > RightHand > Snare+Tom1")
7. **Should we capture *why* operators were considered?** (e.g., "high energy" triggered HatLiftOperator)

### Integration with Groove Diagnostics
8. **What overlaps with GrooveBarDiagnostics?** Density target is in both—duplicate or reference?
9. **Should drummer diagnostics reference groove diagnostics?** Or keep separate?
10. **Who owns the opt-in flag?** Global generation settings or per-system?

### Memory Management
11. **Should diagnostics auto-serialize to disk?** Or keep in-memory until export?
12. **What's the memory budget?** How many bars of diagnostics is "too much"?
13. **Should we support streaming/append mode?** For very long generations

### Test Data
14. **How to verify "doesn't affect output"?** Golden test with/without diagnostics enabled?
15. **Should test snapshots include diagnostics?** Or separate diagnostic snapshots?

### Future Extensibility
16. **Will other agents use same pattern?** Should `DrummerDiagnostics` be specialized or generalized?
17. **What about real-time diagnostics?** (e.g., callback per bar for live monitoring)

---

## 8. Test Scenario Ideas

### Core Functionality Tests
1. **`DrummerDiagnostics_CapturesBasicInformation`**
   - Setup: Single operator, simple bar
   - Verify: BarNumber, Role, operator name captured

2. **`DrummerDiagnostics_CapturesOperatorScores`**
   - Setup: Multiple operators with different scores
   - Verify: Base scores and final scores captured correctly

3. **`DrummerDiagnostics_CapturesRejectionReasons`**
   - Setup: Candidates violating limb conflicts
   - Verify: Rejection reasons list populated with "PhysicalityFilter:LimbConflict"

4. **`DrummerDiagnostics_CapturesMemoryState`**
   - Setup: Bar 10 with history of fills
   - Verify: Memory state snapshot includes last fill shape, recent operators

5. **`DrummerDiagnostics_CapturesDensityComparison`**
   - Setup: Policy targets 0.6 density, actual is 0.55
   - Verify: Both values captured

### Zero-Cost / Non-Invasive Tests
6. **`DrummerDiagnostics_DisabledHasNoCost`**
   - Setup: Run generation with diagnostics disabled
   - Verify: Performance benchmark shows no overhead

7. **`DrummerDiagnostics_DoesNotAffectOutput`**
   - Setup: Same seed, same context, diagnostics on vs off
   - Verify: Byte-identical output (golden test pattern)

8. **`DrummerDiagnostics_DoesNotMutateState`**
   - Setup: Capture memory state during collection
   - Verify: Memory state unchanged after collection

9. **`DrummerDiagnostics_PreservesRngState`**
   - Setup: Known RNG seed, track RNG calls
   - Verify: Same RNG sequence with/without diagnostics

### Edge Case Tests
10. **`DrummerDiagnostics_HandlesNoOperators`**
    - Setup: All operators fail `CanApply()`
    - Verify: Empty considered list, no crash

11. **`DrummerDiagnostics_HandlesNoCandidates`**
    - Setup: Operators apply but generate zero candidates
    - Verify: Diagnostics reflect zero candidates generated

12. **`DrummerDiagnostics_HandlesAllRejected`**
    - Setup: All candidates violate physicality
    - Verify: All in rejected list with reasons

13. **`DrummerDiagnostics_HandlesEmptyMemory`**
    - Setup: Bar 1, no history
    - Verify: Memory state snapshot is empty/default

14. **`DrummerDiagnostics_HandlesTiedScores`**
    - Setup: Multiple candidates with score 0.75
    - Verify: Tie-break decision visible in diagnostics

### Integration Tests
15. **`DrummerDiagnostics_IntegratesWithGrooveDiagnostics`**
    - Setup: Both drummer and groove diagnostics enabled
    - Verify: Both collect independently, no conflicts

16. **`DrummerDiagnostics_TracesAcrossMultipleBars`**
    - Setup: 8-bar section
    - Verify: Diagnostics collected for all 8 bars

17. **`DrummerDiagnostics_TracesMultipleRoles`**
    - Setup: Bar with Kick, Snare, ClosedHat
    - Verify: Separate diagnostic trace per role

18. **`DrummerDiagnostics_TracesFillWindow`**
    - Setup: Fill window active (last 2 bars of section)
    - Verify: Fill operator behavior traced

### Serialization / Export Tests
19. **`DrummerDiagnostics_CanSerializeToJson`**
    - Setup: Full diagnostic collection
    - Verify: Serialize to JSON without errors

20. **`DrummerDiagnostics_CanDeserializeFromJson`**
    - Setup: JSON diagnostic snapshot
    - Verify: Deserialize and query diagnostics

21. **`DrummerDiagnostics_ExportsReadableFormat`**
    - Setup: Complex bar with many decisions
    - Verify: Human-readable export (markdown/CSV)

### Determinism Tests
22. **`DrummerDiagnostics_SameSeedProducesSameDiagnostics`**
    - Setup: Run twice with same seed, diagnostics on
    - Verify: Identical diagnostic traces

23. **`DrummerDiagnostics_DifferentSeedProducesDifferentDiagnostics`**
    - Setup: Run with two different seeds
    - Verify: Diagnostics differ (but output structure same)

### Complex Scenario Tests
24. **`DrummerDiagnostics_TracesComplexFillSelection`**
    - Setup: Fill window with 7 fill operators available
    - Verify: All considered, selection logic visible

25. **`DrummerDiagnostics_TracesPhysicalityRejectionCascade`**
    - Setup: Candidates violating multiple physicality rules
    - Verify: Rejection cascade traced in order

26. **`DrummerDiagnostics_TracesMemoryPenaltyEffect`**
    - Setup: Operator used 2 bars ago
    - Verify: Memory penalty applied, visible in score adjustment

27. **`DrummerDiagnostics_TracesDensityCapEnforcement`**
    - Setup: More candidates than density target allows
    - Verify: Cap enforcement decision traced

### Test Data Setups
- **Minimal:** Bar 1, Kick only, one operator
- **Typical:** Bar 8, Verse, Kick+Snare+Hat, 5 operators active
- **Complex:** Bar 32, Fill window, Chorus, all roles, 15+ operators
- **Edge:** Bar 1, unknown role, zero operators

---

## Summary

**Story 7.1** establishes the diagnostics foundation for the drummer agent, enabling visibility into all decision-making without affecting behavior. Key challenges are maintaining zero-cost when disabled, preserving determinism, and integrating cleanly with existing groove diagnostics (Story G1). The pattern established here will be reused for Guitar/Keys/Bass/Vocal agents in later stages.

**Critical Success Factors:**
1. Zero runtime cost when disabled (production requirement)
2. Determinism preserved (same output with/without diagnostics)
3. Integration with groove diagnostics (consistent pattern)
4. Useful output format (developer can actually debug with it)
