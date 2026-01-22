# PreAnalysis H1 — Clarified (Revised)

## Story
**ID:** H1  
**Title:** Full Groove Phase Unit Tests (Core)

## Summary
This document resolves open clarifying questions from the original PreAnalysis_H1.md and records explicit test rules and expectations so the Story H1 acceptance criteria are unambiguous. This revision corrects errors in the initial clarified document and aligns with actual codebase conventions discovered during source code analysis.

---

## Implementation Context (Critical Findings)

### Existing Test Coverage Status
Analysis of `Music.Tests/Generator/Groove/` reveals the following tests **already exist**:
- **Story A1 (Output Contracts):** `GrooveOutputContractsTests.cs` ✅ (15 tests passing)
- **Story A2 (RNG Streams):** `GrooveRngStreamPolicyTests.cs` ✅
- **Story A3 (Policy Hook):** `GroovePolicyHookTests.cs` ✅
- **Story B1 (Variation Merge):** `VariationLayerMergeTests.cs` ✅
- **Story B2 (Candidate Filter):** `CandidateFilterTests.cs` ✅
- **Story B3 (Weighted Selection):** `WeightedCandidateSelectionTests.cs` ✅
- **Story B4 (Candidate Source):** `GrooveCandidateSourceTests.cs` ✅
- **Story C1 (Density Target):** `DensityTargetComputationTests.cs` ✅
- **Story C3 (Caps Enforcement):** `CapEnforcementTests.cs` ✅
- **Story D1 (Onset Strength):** `OnsetStrengthClassifierTests.cs` ✅ (66 tests passing)
- **Story D2 (Velocity Shaping):** `VelocityShaperTests.cs` ✅ (41 tests passing)
- **Story E1 (Feel Timing):** `FeelTimingEngineTests.cs` ✅
- **Story E2 (Role Timing):** `RoleTimingEngineTests.cs` ✅
- **Story F1 (Override Merge Policy):** Tests exist in various files ✅
- **Story G1 (Diagnostics):** `GrooveBarDiagnosticsTests.cs` ✅
- **Story G2 (Provenance):** `GrooveOnsetProvenanceTests.cs` ✅
- **Story SC1 (Bar Coverage):** `PartTrackBarCoverageAnalyzerTests.cs` ✅

### H1 Scope Clarification
Story H1 is **NOT about writing all new tests from scratch**. It is about:
1. **Verification pass:** Ensure existing tests cover all acceptance criteria comprehensively
2. **Gap filling:** Add missing test cases for edge scenarios not yet covered
3. **Integration tests:** Add narrow integration tests that exercise multiple components together
4. **Regression locks:** Ensure determinism is locked via snapshot/golden tests where appropriate
5. **Matrix coverage:** Ensure combinatorial scenarios (override policy matrix, feel × swing) are covered


---

## Resolved Rules (Answers to Clarifying Questions)

### 1. Test Framework & Project Structure
**CRITICAL CORRECTION:** The test project uses **xUnit**, NOT MSTest.

**Rules:**
- Use **xUnit** test framework (`[Fact]`, `[Theory]`, `[InlineData]`)
- Place tests in `Music.Tests/Generator/Groove/` directory
- Test project: `Music.Tests.csproj` (targets `net9.0-windows7.0`)
- Available packages: `xunit`, `FluentAssertions`, `NSubstitute`, `AutoFixture.AutoNSubstitute`
- Follow existing test file naming: `<ComponentUnderTest>Tests.cs`
- Test class naming: `public class <ComponentUnderTest>Tests`
- Test method naming: `<MethodOrBehavior>_<Condition>_<ExpectedResult>`

### 2. Test Scope: Unit vs Integration
**Rule:** H1 includes:
- **Unit tests** (already largely exist): Isolated component tests with focused assertions
- **Narrow integration tests** (gap to fill): Single-bar or small bar-range tests that exercise 2-3 components together (e.g., selection → caps → diagnostics)
- **No end-to-end tests:** Full multi-bar generation belongs in Story H2

**Integration test characteristics:**
- Scope: Single bar + single role maximum
- Use `GrooveTestSetup` fixtures for realistic catalog/policy setup
- Test component interactions (e.g., caps enforcer respects policy overrides)
- Fast: Target <2s per integration test

### 3. Deterministic Seeds & RNG Conventions
**Rule:** Every test using randomness MUST initialize RNG in constructor.

**Pattern observed in existing tests:**
```csharp
public class MyTests
{
    public MyTests()
    {
        // Initialize RNG before each test with known seed
        Rng.Initialize(42); // or 12345 - use consistent seed per test file
    }

    [Fact]
    public void TestMethod_Reinitializes_WhenNeeded()
    {
        // For tests verifying determinism, re-initialize inline:
        Rng.Initialize(42);
        var result1 = CallCodeUnderTest();
        
        Rng.Initialize(42); // Reset to same seed
        var result2 = CallCodeUnderTest();
        
        Assert.Equal(result1, result2); // Must be identical
    }
}
```

**Rules:**
- Constructor initialization: Use seed `42` or `12345` (be consistent per file)
- Re-initialize inline when testing determinism across runs
- Use `GrooveRngHelper.RngFor(bar, role, streamKey)` to get appropriate `RandomPurpose`
- Assert RNG sequences when verifying stream independence

### 4. GrooveFeel Coverage and Swing Amounts
**Rule:** Based on Story E1 and `FeelTimingEngine`, cover:
- **Feel values:** `GrooveFeel.Straight`, `GrooveFeel.Swing`, `GrooveFeel.Shuffle`, `GrooveFeel.TripletFeel`
- **Swing amounts:** `0.0` (no swing), `0.5` (moderate), `1.0` (full)
- **Test combinations:**
  - `Straight` with any swing amount (no-op)
  - `Swing` with `{0.0, 0.5, 1.0}`
  - `Shuffle` with `{0.0, 0.5, 1.0}`
  - `TripletFeel` with `{0.5}` (representative quantization test)

**Existing coverage:** `FeelTimingEngineTests.cs` likely covers basic cases; H1 should verify edge cases (boundary beats, bar-end wraps).

### 5. Override Merge Policy Matrix Coverage
**Rule:** Test `GrooveOverrideMergePolicy` with focused matrix coverage.

**Policy flags:**
1. `OverrideReplacesLists`
2. `OverrideCanRemoveProtectedOnsets`
3. `OverrideCanRelaxConstraints`
4. `OverrideCanChangeFeel`

**Test matrix (10 tests):**
- 8 focused per-flag tests (2 each): Toggle one flag true/false while others at baseline (all false or documented default)
- 2 combined tests: All flags true, all flags false

**Naming pattern:**
```csharp
[Fact]
public void OverrideMergePolicy_ReplacesListsTrue_ReplacesVariationTags() { }

[Fact]
public void OverrideMergePolicy_ReplacesListsFalse_UnionsVariationTags() { }

// ... repeat for other 3 flags

[Fact]
public void OverrideMergePolicy_AllFlagsTrue_BehaviorCombined() { }

[Fact]
public void OverrideMergePolicy_AllFlagsFalse_NoOverridesApplied() { }
```

### 6. Diagnostics Fields to Assert
**Rule:** When diagnostics enabled, verify structure and content stability.

**Minimum assertions for `GrooveBarDiagnostics`:**
- Non-null when enabled, null when disabled
- `BarNumber`, `Role` match context
- `EnabledTags` is stable list (not empty when tags present)
- `CandidateGroupCount`, `TotalCandidateCount` are non-negative
- `FiltersApplied` list is populated when filtering occurs
- `DensityTarget.ComputedTargetCount` matches expected calculation
- `SelectedCandidates` list has stable `CandidateId` and `Weight` per selection
- `PruneEvents` list is populated when pruning occurs
- `FinalOnsetSummary.FinalCount` matches `FinalOnsets.Count`

**Parity test (critical):**
```csharp
[Fact]
public void Diagnostics_EnabledVsDisabled_ProducesSameFinalOnsets()
{
    // Same inputs, diagnostics off
    var planWithoutDiag = GenerateBarPlan(diagnosticsEnabled: false);
    
    // Same inputs, diagnostics on
    var planWithDiag = GenerateBarPlan(diagnosticsEnabled: true);
    
    // Assert FinalOnsets identical
    Assert.Equal(planWithoutDiag.FinalOnsets.Count, planWithDiag.FinalOnsets.Count);
    // ... assert each onset property matches
    
    // Assert diagnostics presence
    Assert.Null(planWithoutDiag.Diagnostics);
    Assert.NotNull(planWithDiag.Diagnostics);
}
```

### 7. Golden Fixtures and H2 Preparation
**Rule:** H1 should create compact snapshot artifacts for H2 use.

**Storage location:** `Music.Tests/TestFixtures/GrooveSnapshots/`

**Snapshot format (suggestions):**
- JSON or simple text format
- Per bar/role: `{ bar, role, onsets: [ { beat, velocity, timingOffset }, ... ] }`
- Deterministic: Same inputs → same snapshot
- Compact: Only essential fields (beat, velocity, timing)

**H1 responsibility:**
- Create 2-3 representative snapshots (e.g., verse bar, chorus bar, fill bar)
- Provide helper to serialize/deserialize
- H2 will add full regression suite using these

### 8. Performance & CI Constraints
**Rules:**
- Unit tests: Target <200ms each (most existing tests are much faster)
- Integration tests: Target <2s each
- Use `[Trait("Category", "Integration")]` for slower tests
- Avoid: Large catalogs, multi-bar generation, heavy I/O in unit tests

**CI optimization:**
- Fast unit tests run on every build
- Integration tests run on PR validation
- Long-running/stress tests marked separately (not part of H1)

### 9. Mocking vs Real Test Data
**Rule:** Prefer lightweight real data via `GrooveTestSetup`.

**When to use real data:**
- Testing selection, merge, filter, caps logic with realistic catalogs
- Integration tests needing multi-component interaction
- Existing `GrooveTestSetup.BuildPopRockBasicGroove()` provides good baseline

**When to mock (`NSubstitute`):**
- Isolating a component from external dependencies (e.g., mock `IGrooveCandidateSource` to control exact candidate list)
- Testing error paths or edge cases not easily constructible with real data
- Verifying specific method calls/interactions

**Never mock:**
- Code under test (internal components of groove system)
- Simple value types or DTOs

### 10. Parallelization and Test Isolation
**Rule:** Because `Rng` is global state, tests must be isolated.

**xUnit isolation strategies:**
- **Preferred:** Constructor-based RNG initialization (already in use)
  - Each test class gets new instance → constructor runs → RNG reset
- **Alternative:** Use `[Collection]` attribute to group tests that share state (not needed if constructor init works)
- **Not needed:** `[DoNotParallelize]` equivalent (xUnit runs test classes in parallel, but test methods within a class sequentially by default)

**State cleanup:**
- No explicit teardown needed for RNG (constructor reinitializes)
- If adding mutable static caches, ensure they're cleared in constructor or test method setup


---

## Additional Explicit Test Rules and Conventions

### Assertion Style
Use **FluentAssertions** for readability (already in project):
```csharp
// Preferred
result.Should().NotBeNull();
result.Count.Should().Be(5);
result.Select(o => o.Beat).Should().Equal(new[] { 1m, 1.5m, 2m, 2.5m, 3m });

// Acceptable (xUnit Assert)
Assert.NotNull(result);
Assert.Equal(5, result.Count);
```

### Test Organization
- Group related tests with `#region` blocks (existing pattern)
- Order: Setup/helpers → happy path → edge cases → error cases
- One assertion focus per test (avoid mega-tests)

### Edge Case Coverage
**Required edge cases per component:**
- **Empty inputs:** Empty candidate list, empty enabled tags
- **Null handling:** Null policy overrides, null segment profile
- **Boundary values:** Zero density, max density, zero weight, negative weight
- **Tie scenarios:** Equal weights, equal scores with deterministic tie-break
- **Cap violations:** Exceeding per-bar, per-beat, per-role caps
- **Protection conflicts:** Protected onset + cap violation
- **Grid boundaries:** Bar-end beats, anticipations wrapping to next bar
- **Meter edge cases:** 3/4, 6/8, 12/8 (in addition to 4/4)

### Test Naming Conventions (Observed Pattern)
```csharp
// Component_Scenario_ExpectedOutcome
ComputeWeight_PositiveBiases_ReturnsProduct
MergeLayersForBar_AdditiveLayer_UnionsWithExistingGroups
FeelTiming_SwingFeel_ShiftsOffbeatsLater

// For verification/validation tests
Diagnostics_EnabledVsDisabled_ProducesSameFinalOnsets
Selection_SameSeed_ProducesIdenticalResults
```

---

## Test Coverage Verification Checklist

### Story B1 — Variation Layer Merge
Existing: `VariationLayerMergeTests.cs`  
Verify:
- [ ] Additive union behavior (deduplication by group ID)
- [ ] Replace behavior (discards previous working set)
- [ ] Tag-gated apply/skip (layers with non-matching tags ignored)
- [ ] Deterministic ordering (stable sort by layer order + group ID)
- [ ] Edge: Empty catalog, no matching tags, all layers skipped

### Story B2 — Candidate Filtering
Existing: `CandidateFilterTests.cs`  
Verify:
- [ ] Group tag filtering (any group tag intersects enabled tags)
- [ ] Candidate tag filtering (any candidate tag intersects enabled tags)
- [ ] Empty/null tag semantics (match all when empty)
- [ ] Policy override tags integration
- [ ] Edge: No enabled tags, all null tags, no candidates pass filter

### Story B3 — Weighted Selection
Existing: `WeightedCandidateSelectionTests.cs`  
Verify:
- [ ] Weight computation (candidate × group bias)
- [ ] Deterministic tie-breaking (weight desc, then stable ID)
- [ ] RNG determinism (same seed → same selections)
- [ ] Zero/negative weights treated as zero
- [ ] Edge: All zero weights, single candidate, empty pool

### Story C1 — Density Target Computation
Existing: `DensityTargetComputationTests.cs`  
Verify:
- [ ] Base computation: `round(Density01 × MaxEventsPerBar)`
- [ ] Section multiplier applied
- [ ] Policy override applied
- [ ] Clamping to `[0..MaxEventsPerBar]`
- [ ] Edge: Density=0, Density=1, MaxEvents=0

### Story C3 — Caps Enforcement
Existing: `CapEnforcementTests.cs`  
Verify:
- [ ] MaxHitsPerBar enforcement
- [ ] MaxHitsPerBeat enforcement
- [ ] RoleMaxDensityPerBar enforcement
- [ ] Per-candidate MaxAddsPerBar enforcement
- [ ] Per-group MaxAddsPerBar enforcement
- [ ] Pruning policy (never prune `IsMustHit`/`IsNeverRemove`)
- [ ] Pruning order (lowest-scored non-protected first)
- [ ] Deterministic tie-break for pruning
- [ ] Edge: All protected, multiple caps violated simultaneously

### Story D1 — Onset Strength Classification
Existing: `OnsetStrengthClassifierTests.cs` (66 tests)  
Verify:
- [ ] Downbeat classification (beat 1)
- [ ] Backbeat classification (meter-specific: 4/4, 3/4, 6/8, 12/8)
- [ ] Strong beat classification (meter defaults)
- [ ] Offbeat classification (grid-aware: eighth vs triplet)
- [ ] Pickup classification (grid-aware: sixteenth vs triplet)
- [ ] Fallback rules for uncommon meters
- [ ] Explicit strength override (candidate.Strength overrides computed)
- [ ] Edge: Bar-end anticipation, epsilon tolerance boundaries

### Story D2 — Velocity Shaping
Existing: `VelocityShaperTests.cs` (41 tests)  
Verify:
- [ ] Role × strength lookup
- [ ] `Typical + AccentBias` computation
- [ ] Ghost velocity precedence (RoleGhostVelocity wins)
- [ ] Fallback chain (role missing, strength missing)
- [ ] Policy multiplier + additive override order
- [ ] Clamping (rule Min/Max, then MIDI 1-127)
- [ ] Existing velocity preservation (don't overwrite non-null)
- [ ] Edge: Out-of-range Min/Max, all lookups fail

### Story E1 — Feel Timing
Existing: `FeelTimingEngineTests.cs`  
Verify:
- [ ] Straight feel (no shift)
- [ ] Swing feel (offbeats shifted proportional to swing amount)
- [ ] Shuffle feel (eighth offbeats toward triplet grid)
- [ ] TripletFeel (quantize to triplet grid)
- [ ] Swing amounts: 0.0, 0.5, 1.0
- [ ] Grid compliance (no illegal slots created)
- [ ] Edge: Bar-end anticipations, boundary beats

### Story E2 — Role Timing
Existing: `RoleTimingEngineTests.cs`  
Verify:
- [ ] TimingFeel → tick offset conversion (Ahead/OnTop/Behind/LaidBack)
- [ ] RoleTimingBiasTicks added to base feel offset
- [ ] Policy override (RoleTimingFeelOverride, RoleTimingBiasTicksOverride)
- [ ] Clamping by MaxAbsTimingBiasTicks
- [ ] Additive with E1 feel timing
- [ ] Edge: Bias exceeds max, multiple overrides combined

### Story F1 — Override Merge Policy
Tests likely distributed across multiple files  
Verify (via dedicated matrix tests):
- [ ] `OverrideReplacesLists` true/false
- [ ] `OverrideCanRemoveProtectedOnsets` true/false
- [ ] `OverrideCanRelaxConstraints` true/false
- [ ] `OverrideCanChangeFeel` true/false
- [ ] Combined: all true, all false
- [ ] Edge: Conflicting overrides, partial overrides

### Story G1 — Diagnostics
Existing: `GrooveBarDiagnosticsTests.cs`  
Verify:
- [ ] Diagnostics toggle (on/off) produces identical `FinalOnsets`
- [ ] When enabled, all required fields populated
- [ ] When disabled, diagnostics are null (zero-cost)
- [ ] Diagnostics structure stable across runs (determinism)
- [ ] Edge: Empty tags, no filters applied, no pruning occurred

### Story G2 — Provenance
Existing: `GrooveOnsetProvenanceTests.cs`  
Verify:
- [ ] Anchor source vs variation source
- [ ] GroupId/CandidateId captured for variations
- [ ] TagsSnapshot optional but stable when present
- [ ] Provenance does not affect sorting or output determinism
- [ ] Edge: Provenance null, partial provenance fields

---

## Gap Analysis: Tests to Add for H1

Based on existing test files and acceptance criteria, the following gaps need filling:

### 1. Integration Tests (NEW)
**File:** `Music.Tests/Generator/Groove/GroovePhaseIntegrationTests.cs`

Tests to add:
```csharp
[Fact]
public void FullBarPipeline_SingleBar_ProducesDeterministicOutput()
// Tests: Selection → Caps → Velocity → Timing → Diagnostics (single bar)

[Fact]
public void FullBarPipeline_WithPolicyOverrides_AppliesCorrectly()
// Tests: Policy provider integration with selection + caps

[Fact]
public void FullBarPipeline_WithSegmentOverride_MergePolicyEnforced()
// Tests: Segment override + merge policy + final output
```

### 2. Override Merge Policy Matrix (NEW)
**File:** `Music.Tests/Generator/Groove/OverrideMergePolicyMatrixTests.cs`

Tests to add (10 total):
```csharp
[Fact] public void OverrideMergePolicy_ReplacesListsTrue_...
[Fact] public void OverrideMergePolicy_ReplacesListsFalse_...
[Fact] public void OverrideMergePolicy_CanRemoveProtectedTrue_...
[Fact] public void OverrideMergePolicy_CanRemoveProtectedFalse_...
[Fact] public void OverrideMergePolicy_CanRelaxConstraintsTrue_...
[Fact] public void OverrideMergePolicy_CanRelaxConstraintsFalse_...
[Fact] public void OverrideMergePolicy_CanChangeFeelTrue_...
[Fact] public void OverrideMergePolicy_CanChangeFeelFalse_...
[Fact] public void OverrideMergePolicy_AllFlagsTrue_CombinedBehavior()
[Fact] public void OverrideMergePolicy_AllFlagsFalse_NoOverridesApplied()
```

### 3. Snapshot/Golden Helpers (NEW for H2 prep)
**File:** `Music.Tests/TestFixtures/GrooveSnapshotHelper.cs`

```csharp
public static class GrooveSnapshotHelper
{
    public static string SerializeBarPlan(GrooveBarPlan plan) { }
    public static GrooveBarPlan DeserializeBarPlan(string json) { }
    public static void SaveSnapshot(string name, GrooveBarPlan plan) { }
    public static GrooveBarPlan LoadSnapshot(string name) { }
}
```

### 4. Edge Case Tests (gaps in existing coverage)
Review each test file and add missing edge cases listed in "Edge Case Coverage" section above.

### 5. Cross-Component Tests
**File:** `Music.Tests/Generator/Groove/GrooveCrossComponentTests.cs`

Tests to verify component interactions:
```csharp
[Fact] public void SelectionAndCaps_TargetExceedsCap_CapsWin()
[Fact] public void VelocityAndTiming_BothApplied_OrderCorrect()
[Fact] public void DiagnosticsAndPerformance_MinimalOverhead()
```

---

## Test Implementation Priority

1. **Verify existing tests pass** (run `dotnet test` on Music.Tests project)
2. **Add override merge policy matrix tests** (10 tests, F1 coverage)
3. **Add narrow integration tests** (3-5 tests, cross-phase verification)
4. **Add snapshot helpers** (H2 preparation)
5. **Fill edge case gaps** (review each AC, add missing scenarios)
6. **Add cross-component interaction tests** (verify pipeline ordering)

---

## Final Notes

### Critical Corrections from Initial Analysis
1. ❌ **MSTest** → ✅ **xUnit** (test framework)
2. ❌ `[TestInitialize]`/`[TestCleanup]` → ✅ Constructor initialization pattern
3. ❌ "Tests don't exist" → ✅ Most tests already exist; H1 is verification + gap-fill
4. ✅ Correct RNG initialization pattern confirmed via source code
5. ✅ Correct diagnostics structure confirmed from `GrooveBarDiagnostics.cs`

### Implementation Guidance
- **Start by running existing tests:** Ensure baseline passes
- **Review each acceptance criterion:** Cross-reference with existing test files
- **Focus on gaps:** Override policy matrix, integration tests, edge cases
- **Maintain conventions:** Follow existing patterns for naming, assertions, setup
- **Document findings:** Update this document if new patterns discovered

---

<!-- End of PreAnalysis_H1_Clarified (Revised) -->
