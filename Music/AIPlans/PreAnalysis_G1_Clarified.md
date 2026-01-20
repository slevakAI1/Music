# PreAnalysis_G1_Clarified — Story G1: Add Groove Decision Trace (Opt-in, No Behavior Change)

## 1) Story Intent Summary
- **What**: Add an opt-in diagnostics trace that records per-bar and per-role groove decision data (enabled tags, candidate counts, filters and reasons, density targets, selected candidates with weights and RNG streams, prune reasons, final onset summary).
- **Why**: Improves explainability and debuggability so developers can trace why the generator made specific choices and tune future drummer policies without changing generation behavior.
- **Who benefits**: Developers and integrators (debugging/tuning), QA (determinism/regression tests), and future Drummer/Operator engineers who need provenance and metrics to train or tune policies. End-users benefit indirectly via better-tuned generators.

---

## 2) Acceptance Criteria Checklist
1. Add an opt-in diagnostics flag (config or parameter).
2. When enabled, capture per bar + role:
   2.1. Enabled tags (after phrase/segment/policy).
   2.2. Candidate groups count and candidate count.
   2.3. Filters applied and why (tag mismatch, never-add, grid invalid, etc.).
   2.4. Density target inputs and computed target count.
   2.5. Selected candidates with weights/scores and RNG stream used.
   2.6. Prune events and reasons (cap violated, tie-break, protected preserved).
   2.7. Final onset list summary.
3. When disabled, diagnostics collection is effectively zero-cost and produces identical output.
4. Unit test: diagnostics on/off does not change generated notes.

**Clarified**: All ambiguities resolved in Section 7 (Clarifying Questions — ANSWERED).

---

## 3) Dependencies & Integration Points
- **Dependent stories (must be complete)**:
  - `A1` (Groove Output Contracts) — diagnostics attach to `GrooveBarPlan` via `Diagnostics` field.
  - `A2` (Deterministic RNG Stream Policy) — diagnostics must record `RandomPurpose` enum values deterministically.
  - `A3` (Drummer Policy Hook) — policy decisions should be recorded in diagnostics.
  - Upstream generation stories (B*, C*, D*, E*) — diagnostics will observe outputs and intermediate decisions from those systems.

- **Integration points (existing code/types)**:
  - `GrooveBarPlan.Diagnostics` field (nullable object reference) — attach diagnostics here.
  - Selection engines: `GrooveSelectionEngine`, `GrooveVariationCatalog` (candidate counts, groups).
  - Protection/pruning: `ProtectionPerBarBuilder`, `ProtectionApplier` (prune reasons, preserved protected onsets).
  - RNG utilities: `Rng` / `RandomPurpose` enum (record RNG stream identifiers).
  - Policy provider: `IGroovePolicyProvider` / `GroovePolicyDecision` (record overrides applied).
  - Density/caps: `RoleDensityTarget`, `RoleRhythmVocabulary`, `GrooveRoleConstraintPolicy` (inputs & cap reasons).

- **Enables for future stories**:
  - Provides trace data required by `G2` (provenance validation) and future Drummer/Operator tuning stories.
  - Enables golden-file regression snapshots (H2) that include decision metadata for richer diffs.
  - Supports story F1 (merge policy) diagnostics for override precedence.

---

## 4) Inputs & Outputs

**Inputs (consumed)**:
- `BarContext` / `GrooveBarContext` and `GrooveBarPlan` (base onsets, candidate sets).
- Candidate groups and `GrooveOnsetCandidate` lists from `GrooveVariationCatalog` or `IGrooveCandidateSource`.
- `GroovePolicyDecision` and segment `SegmentGrooveProfile` enabled tags/overrides.
- `RandomPurpose` enum identifiers (for RNG stream names).
- Constraint/cap policies: `RoleRhythmVocabulary`, `GrooveRoleConstraintPolicy`, per-group/candidate max adds.
- Diagnostics enabled flag: passed as parameter to pipeline entry point or per-bar processing method.

**Outputs (produced)**:
- A diagnostics object (structured record type) attached to `GrooveBarPlan.Diagnostics` (nullable).
- When diagnostics disabled: `Diagnostics` field remains null; no diagnostic objects allocated.
- No serialized artifact for MVP; structured object only (JSON serialization for export is future work).

**Configuration/settings read**:
- Diagnostics enabled flag: boolean parameter passed to generation pipeline.
- No verbosity levels for MVP (single level: full trace when enabled).

---

## 5) Constraints & Invariants

- **Diagnostics must be opt-in**: when disabled, generator behavior and outputs must be bitwise identical.
- **Diagnostics must not mutate generation inputs** in a way that changes randomness order (preserve deterministic RNG draws and ordering).
- **Protected onsets** (`IsMustHit`, `IsNeverRemove`, `IsProtected`) must never be removed; diagnostics should report attempted removals but not change behavior.
- **Ordering invariant**: decisions should be recorded in the canonical pipeline order (tag resolution → filtering → density computation → selection → prune → finalization).
- **Determinism invariant**: same inputs + same seed + same diagnostics flag state should produce identical generation results.
- **Diagnostics storage footprint** should be bounded; for MVP, support songs up to 200 bars without special truncation (warn in diagnostics if limits approached).
- **Read-only**: diagnostic collection must be side-effect-free; no mutation of domain state.

---

## 6) Edge Cases to Test

- Empty candidate catalog: diagnostics should show zero groups/candidates and resulting behavior.
- All candidates filtered out by tags or grid: diagnostics should explain the filter reasons and show target not met.
- Conflicting policy overrides: diagnostics should record which override applied and precedence result.
- Ties in pruning/selection requiring RNG tie-breaks: diagnostics must record `RandomPurpose` stream used and deterministic tie-break outcome.
- Large number of bars/roles (e.g., 200+ bars): verify diagnostics memory stays reasonable; no OOM.
- Diagnostics enabled vs disabled: ensure exact output equality and identical RNG draw sequences across both runs.
- Null or malformed `GroovePolicyDecision` fields: diagnostics should handle missing overrides gracefully and record fallback decisions.
- Protected onsets near caps: diagnostics show preserved items and prune candidates chosen instead.
- Bar with no candidates: diagnostics record "no candidates available" for density target.
- Multiple roles per bar: diagnostics correctly scoped per-role with no cross-contamination.

---

## 7) Clarifying Questions — ANSWERED

### Q1. Opt-in scope: should diagnostics be enabled globally (song/generator run) or allow finer granularity (per-segment, per-bar, per-role)?

**Answer**: **Global (per-generator-run) for MVP**.

**Explicit rules**:
- Pass a single `bool enableDiagnostics` parameter to the top-level generator entry point (e.g., `DrumTrackGenerator.Generate(... , bool enableDiagnostics)`).
- When `true`, diagnostics are collected for **all bars and all roles** in that run.
- When `false`, diagnostics collection is disabled entirely; `GrooveBarPlan.Diagnostics` remains null for all bars.
- Fine-grained control (per-bar, per-role, per-segment) is **not** part of Story G1; defer to future enhancement.
- Rationale: Simplest implementation; supports primary use case (debug full song run or disable for production).

---

### Q2. Output format: do we need a canonical schema (POCOs only), or a serialized artifact (JSON/YAML) for snapshot tests and external tools? If serialized, what casing/fields are required?

**Answer**: **Canonical schema (POCOs only) for MVP; serialization deferred to future work**.

**Explicit rules**:
- Define a structured record type `GrooveBarDiagnostics` (sealed record with required fields matching AC 2.1-2.7).
- `GrooveBarPlan.Diagnostics` type changes from `string?` to `GrooveBarDiagnostics?`.
- No JSON/YAML serialization required for Story G1; unit tests assert against in-memory objects.
- If serialization needed for Story H2 (golden tests), define a separate serialization story.
- Rationale: Cleaner API, easier to assert in tests, avoids premature serialization format decisions.

**Schema structure** (preliminary):
```csharp
public sealed record GrooveBarDiagnostics
{
    public required int BarNumber { get; init; }
    public required string Role { get; init; }
    public required IReadOnlyList<string> EnabledTags { get; init; }
    public required int CandidateGroupCount { get; init; }
    public required int TotalCandidateCount { get; init; }
    public required IReadOnlyList<FilterDecision> FiltersApplied { get; init; }
    public required DensityTargetDiagnostics DensityTarget { get; init; }
    public required IReadOnlyList<SelectionDecision> SelectedCandidates { get; init; }
    public required IReadOnlyList<PruneDecision> PruneEvents { get; init; }
    public required OnsetListSummary FinalOnsetSummary { get; init; }
}

public sealed record FilterDecision(string CandidateId, string Reason);
public sealed record DensityTargetDiagnostics(double Density01, int MaxEventsPerBar, int TargetCount);
public sealed record SelectionDecision(string CandidateId, double Weight, string RngStreamUsed);
public sealed record PruneDecision(string OnsetId, string Reason, bool WasProtected);
public sealed record OnsetListSummary(int BaseCount, int VariationCount, int FinalCount);
```

---

### Q3. Verbosity levels: do we require multiple verbosity levels (minimal vs full) or a single full trace only?

**Answer**: **Single full trace only for MVP**.

**Explicit rules**:
- When `enableDiagnostics = true`, capture **all** AC 2.1-2.7 fields.
- When `enableDiagnostics = false`, capture **nothing** (null diagnostics).
- No partial/minimal verbosity levels for Story G1.
- Rationale: Simplifies implementation; avoids premature complexity; full trace is most useful for debugging.
- Future enhancement: add verbosity enum if performance or storage becomes an issue.

---

### Q4. Performance target: what is the acceptable overhead when diagnostics are enabled and when disabled? Any microbenchmark target for "zero-cost-ish"?

**Answer**: **Disabled = zero allocations; enabled = no performance target for MVP (debug-only use)**.

**Explicit rules**:
- **When disabled**: diagnostic collection must allocate zero diagnostic objects; `GrooveBarPlan.Diagnostics` remains null.
- **When enabled**: acceptable overhead is "debug-mode performance" (10-50% slower is acceptable since diagnostics are for development/debugging).
- **No microbenchmark required** for Story G1; rely on manual profiling if performance issues arise.
- Measurement approach for AC 3 ("zero-cost-ish"):
  - Assert that `GrooveBarPlan.Diagnostics == null` when disabled.
  - Assert identical output (final onsets) and identical RNG sequences when enabled vs disabled.
- Rationale: Diagnostics are debug tools, not production features; premature optimization avoided.

---

### Q5. Lifetime and retention: should diagnostics be attached to `GrooveBarPlan` in-memory only, stored on `Song` for later export, or written out to disk by default?

**Answer**: **Attached to `GrooveBarPlan` in-memory only; no persistence for MVP**.

**Explicit rules**:
- Each `GrooveBarPlan` instance holds its own `GrooveBarDiagnostics?` object.
- Diagnostics lifetime = same as `GrooveBarPlan` lifetime (GC collects when plan is released).
- **No automatic persistence** to disk or `Song` object.
- **No export format** for Story G1 (defer to future story if needed).
- Access pattern: tests and debug code inspect `GrooveBarPlan.Diagnostics` immediately after generation.
- Rationale: Simplest storage; avoids premature file I/O or `Song` model pollution.

---

### Q6. Privacy/logging: are there any restrictions on logging detailed trace data (PII/security) or retention policies?

**Answer**: **No PII in groove diagnostics; no special retention policy for MVP**.

**Explicit rules**:
- Groove diagnostics contain only musical decision data: tags, counts, weights, RNG streams, cap reasons.
- **No user data, file paths, or identifiable information** logged.
- RNG seed values **may be logged** (required for determinism debugging).
- **No privacy restrictions** on diagnostic content for Story G1.
- Retention: diagnostics are in-memory only; no disk persistence (see Q5).
- Rationale: Groove decisions are non-sensitive musical data; no PII concerns.

---

### Q7. Canonical names: what exact identifiers should be recorded for RNG streams and candidate/group stable ids to ensure stable diffs across runs?

**Answer**: **Use existing stable identifiers from domain types**.

**Explicit rules for RNG streams**:
- Record `RandomPurpose` enum **name** as string (e.g., `"GrooveCandidatePick"`, `"GrooveTieBreak"`).
- Do **not** record raw seed values in per-decision diagnostics (seed is global context).
- Use `randomPurpose.ToString()` to get canonical string.

**Explicit rules for candidate/group IDs**:
- Use `GrooveCandidateGroup.GroupId` (string or GUID, as defined in catalog).
- Use `GrooveOnsetCandidate.CandidateId` (string or GUID, as defined in catalog).
- If IDs are missing/null in catalog, use index-based fallback: `"Group_{index}"`, `"Candidate_{groupIndex}_{candidateIndex}"`.
- **Document stable ID requirement** in `GrooveCandidateGroup` and `GrooveOnsetCandidate` contracts for future catalog authors.

**Determinism guarantee**:
- Same catalog + same inputs → same IDs in diagnostics.
- Tests should verify ID stability across repeated runs with same seed.

---

### Q8. Test expectations: for the AC test "diagnostics on/off does not change generated notes", should the test also assert the RNG sequence equality, or only final output equality?

**Answer**: **Assert final output equality only; RNG sequence testing is optional**.

**Explicit rules for AC 4 test**:
- **Required assertion**: `FinalOnsets` list (from `GrooveBarPlan.FinalOnsets`) is identical (same count, same beats, same velocities, same timing offsets, same roles) when diagnostics enabled vs disabled.
- **Optional assertion** (recommended): RNG draw sequences are identical (same `RandomPurpose` calls in same order).
  - Implementation: add a test-only RNG instrumentation mode that logs all draws; compare logs.
  - If RNG instrumentation is too complex, defer to manual testing.
- **Not required**: assert that intermediate pipeline state (base onsets, selected variations) is identical (redundant if final onsets match).
- Rationale: Final output equality is the critical invariant; RNG sequence equality is a stronger guarantee but harder to test.

---

### Q9. Truncate policy: for very long songs or many candidates, should diagnostics include sampling/truncation rules or explicit size limits?

**Answer**: **No truncation for MVP; support up to 200 bars without special handling**.

**Explicit rules**:
- Diagnostics are per-bar structures; memory footprint = O(bars × roles × candidates).
- **No truncation** or sampling for Story G1; capture full trace for all bars.
- **Size limit guideline**: system should handle songs up to 200 bars with ~10 roles and ~100 candidates/role without OOM (typical test songs are 48-96 bars).
- If memory becomes an issue in practice:
  - Log a diagnostic warning (non-throwing) when bar count exceeds 200.
  - Future enhancement: add truncation policy (e.g., keep first/last N bars, or sample every Mth bar).
- Rationale: 200-bar limit covers realistic use cases; premature optimization avoided.

---

### Q10. Relationship to `G2` (Provenance): should G1 produce provenance fields if not yet implemented, or only start recording provenance when `G2` is applied?

**Answer**: **G1 does NOT produce provenance fields; defer to G2**.

**Explicit rules**:
- Story G1 diagnostics do **not** populate `GrooveOnset.Provenance` fields.
- Story G1 diagnostics **may reference** existing `Provenance` fields if present (e.g., log provenance of pruned onsets).
- Story G2 is responsible for populating `GrooveOnset.Provenance` (Source, GroupId, CandidateId, TagsSnapshot).
- When G2 is implemented, diagnostics should include provenance data for richer traces (but G1 does not block on G2).
- Rationale: Separation of concerns; G1 focuses on decision trace, G2 focuses on onset-level provenance.

**Interaction after G2**:
- When both G1 and G2 are complete, diagnostics will show:
  - Selected candidates (G1) with their provenance fields (G2).
  - Pruned onsets (G1) with their provenance fields (G2) for "why was this anchor removed?" analysis.

---

## 8) Test Scenario Ideas

All test scenarios updated to reflect clarified rules:

- **`Diagnostics_Enabled_DoesNotChangeGeneratedOnsets`**: 
  - Enable diagnostics; assert `FinalOnsets` identical to disabled run and to golden baseline.
  - Assert `GrooveBarPlan.Diagnostics != null` when enabled.
  - Assert all diagnostic fields populated with expected counts/data.

- **`Diagnostics_Disabled_IsLightweight`**: 
  - Assert `GrooveBarPlan.Diagnostics == null` when disabled.
  - Assert no diagnostic objects allocated (use profiler or GC stats if available).
  - Assert RNG sequence unchanged (if instrumentation available).

- **`Diagnostics_Record_TagFilteringReasons`**: 
  - Use a catalog with mixed-tag candidates; assert diagnostics contains expected `FilterDecision` records per candidate and role.
  - Verify `Reason` field contains "tag mismatch" or "never-add" as appropriate.

- **`Diagnostics_Record_DensityAndTarget`**: 
  - Configure role density and policy overrides; assert `DensityTargetDiagnostics` shows correct inputs (Density01, MaxEventsPerBar) and computed target count.

- **`Diagnostics_Record_SelectionWeightsAndRngStream`**: 
  - Build candidates with different weights; assert chosen candidates and recorded `RandomPurpose` name match expected deterministic outcome for a fixed seed.
  - Verify `SelectionDecision.RngStreamUsed` contains expected stream name (e.g., "GrooveCandidatePick").

- **`Diagnostics_Record_Prune_ReasonsAndProtectedPreserved`**: 
  - Construct a case where caps exceed allowed and protected anchors present; assert diagnostics shows `PruneDecision` records with `WasProtected = true` for preserved onsets and `Reason` field for pruned candidates.

- **`Diagnostics_OnsetSummary_Correct`**: 
  - Assert `OnsetListSummary` counts (BaseCount, VariationCount, FinalCount) match actual onset list sizes.

- **`Diagnostics_MultipleRoles_PerBarScoping`**: 
  - Generate bar with multiple roles; assert diagnostics are per-bar + per-role with no cross-contamination.

- **`Diagnostics_EmptyCandidatePool`**: 
  - Configure empty candidate catalog; assert diagnostics show zero groups/candidates and target not met.

- **`Diagnostics_LargeSong_NoOOM`**: 
  - Generate a 200-bar song with diagnostics enabled; assert no OOM and diagnostics captured for all bars.

---

## 9) Implementation Guidance

### Diagnostic Object Creation Pattern

Follow the pattern established in `VelocityShaper.cs` for dual-mode methods:

```csharp
// Fast path (no diagnostics)
public static GrooveBarPlan ProcessBar(..., bool enableDiagnostics)
{
    // ... pipeline logic ...
    
    GrooveBarDiagnostics? diagnostics = null;
    if (enableDiagnostics)
    {
        diagnostics = BuildDiagnostics(barContext, candidateGroups, selectedCandidates, pruneEvents, ...);
    }
    
    return new GrooveBarPlan 
    { 
        BaseOnsets = baseOnsets,
        SelectedVariationOnsets = selected,
        FinalOnsets = final,
        Diagnostics = diagnostics,
        BarNumber = barNumber
    };
}

// Diagnostic builder (only called when enabled)
private static GrooveBarDiagnostics BuildDiagnostics(...)
{
    return new GrooveBarDiagnostics
    {
        BarNumber = barNumber,
        Role = role,
        EnabledTags = enabledTags.ToList(),
        CandidateGroupCount = candidateGroups.Count,
        TotalCandidateCount = candidateGroups.Sum(g => g.Candidates.Count),
        FiltersApplied = filterDecisions.ToList(),
        DensityTarget = new DensityTargetDiagnostics(density01, maxEvents, targetCount),
        SelectedCandidates = selectionDecisions.ToList(),
        PruneEvents = pruneDecisions.ToList(),
        FinalOnsetSummary = new OnsetListSummary(baseOnsets.Count, selectedOnsets.Count, finalOnsets.Count)
    };
}
```

### Pipeline Integration Points

Diagnostics should be collected at these pipeline stages (per DrumTrackGenerator pipeline order):

1. **Tag resolution** (after phrase/segment/policy): record enabled tags.
2. **Candidate filtering** (RhythmVocabularyFilter, tag filtering): record `FilterDecision` for each filtered candidate.
3. **Density computation** (C1): record `DensityTargetDiagnostics`.
4. **Selection** (C2, B3): record `SelectionDecision` for each selected candidate with weight and RNG stream.
5. **Pruning** (C3): record `PruneDecision` for each prune event with reason and protection status.
6. **Finalization**: record `OnsetListSummary` counts.

### RNG Stream Recording

When calling `Rng.NextInt(RandomPurpose.GrooveCandidatePick, ...)`:
- If diagnostics enabled, record `RandomPurpose.GrooveCandidatePick.ToString()` in the relevant `SelectionDecision` or `PruneDecision`.
- Pattern:
```csharp
var rngStream = RandomPurpose.GrooveCandidatePick;
int randomValue = Rng.NextInt(rngStream, 0, 100);

if (enableDiagnostics)
{
    selectionDecisions.Add(new SelectionDecision(
        candidateId,
        weight,
        rngStream.ToString()  // "GrooveCandidatePick"
    ));
}
```

---

## 10) Notes & Deferred Work

**Completed in Story G1**:
- Opt-in diagnostic trace with structured POCOs
- Per-bar, per-role decision recording
- Zero-cost when disabled (null diagnostics)
- Determinism test (enabled vs disabled)

**Explicitly deferred (not in Story G1)**:
- JSON/YAML serialization (future story for H2 golden tests)
- Fine-grained control (per-bar, per-role enable flags)
- Verbosity levels (minimal vs full)
- Truncation/sampling for very long songs
- RNG sequence instrumentation (optional test enhancement)
- Provenance field population (Story G2)
- Export to disk (future enhancement)

**Pre-conditions for Story G1 implementation**:
- Story A1 complete (GrooveBarPlan exists)
- Story A2 complete (RandomPurpose enum defined)
- Pipeline stages B*, C*, D* complete (sources of diagnostic data)

---

## Summary: Story G1 Scope

**In scope**:
- Structured diagnostic records (`GrooveBarDiagnostics` and sub-records)
- Global enable/disable flag (per-generator-run)
- Per-bar + per-role trace of AC 2.1-2.7
- In-memory only (attached to `GrooveBarPlan`)
- Determinism test (enabled vs disabled → same output)
- RNG stream name recording (via `RandomPurpose.ToString()`)
- Candidate/group stable ID recording
- Protection status and prune reason recording

**Out of scope**:
- Serialization to JSON/YAML
- Fine-grained enable flags
- Verbosity levels
- Truncation/sampling
- Provenance field population (Story G2)
- Disk persistence
- Performance micro-benchmarks

---

**This clarified analysis provides explicit, implementable rules for Story G1 with all ambiguities resolved.**
