# Pre-Analysis — Story 2.4: Implement Drummer Candidate Source

## 1. Story Intent Summary
- What: Provide a concise pre-analysis for Story 2.4 which implements `DrummerCandidateSource` as an `IGrooveCandidateSource` that gathers operator-generated `DrumCandidate`s, maps them to groove candidates, groups them, filters by physicality, and returns deterministic candidate groups for the groove pipeline.
- Why: Enables the groove selection pipeline to receive structured, playable, and style-aware candidate onsets produced by the drummer agent; this is the bridge between operator generation and the selection/placement stage.
- Who benefits: Developers (integration and tests), the groove selection system (consumes standardized candidate groups), and ultimately the end-user via improved generated drum grooves.

## 2. Acceptance Criteria Checklist
1. Create `DrummerCandidateSource` implementing `IGrooveCandidateSource`.
2. Implement `GetCandidateGroups(barContext, role)` returning `IReadOnlyList<GrooveCandidateGroup>`.
3. Calls enabled operators to generate candidates.
4. Converts `DrumCandidate` to `GrooveOnsetCandidate` with proper mapping.
5. Groups candidates by operator family for structured selection.
6. Applies physicality filter before returning candidates.
7. Ensure candidates are deterministic for same context + seed.
8. Unit tests:
   - Same context + seed → same candidates.
   - Operators generate expected candidate types.

Ambiguous/unclear ACs:
- "Proper mapping" — which `DrumCandidate` fields must map to which `GrooveOnsetCandidate` fields (velocity/timing/articulation/protected flags) is not fully specified.
- "Groups candidates by operator family" — the expected `GrooveCandidateGroup` shape and whether groups preserve internal ordering or aggregate scores is unspecified.
- Scope of the physicality filter (per-operator vs. global, and interaction with protected onsets) needs clarification.

## 3. Dependencies & Integration Points
- Required completed stories/components (explicit or implied):
  - Story 2.1 `DrummerContext` (context input)
  - Story 2.2 `DrumCandidate` type
  - Story 2.3 `DrummerPolicyProvider` (to know enabled operators & density overrides)
  - Story 3.6 `DrumOperatorRegistry` (operator discovery)
  - Story 4.3 `PhysicalityFilter` and `PhysicalityRules`
  - Common infrastructure from Stage 1: `IGrooveCandidateSource`, `GrooveOnsetCandidate`, `GrooveCandidateGroup`, RNG utilities, `IAgentMemory` / selection engine contracts
- Types/units interacted with:
  - `GrooveBarContext` / `DrummerContext`
  - `IDrumOperator` implementations
  - `DrumCandidate`, `DrumCandidateMapper` (mapping helper)
  - `GrooveOnsetCandidate`, `GrooveCandidateGroup`
  - `PhysicalityFilter`, `PhysicalityRules`, `LimbModel`, `StickingRules`
  - Deterministic RNG stream (seed + streamKey)
  - Diagnostics collector (opt-in)
- What this story provides for future stories:
  - Canonical candidate groups feeding `OperatorSelectionEngine`
  - A mapping helper and clear contract for physicality-validated candidate sets
  - Diagnostics points for policy/operator tuning

## 4. Inputs & Outputs
Inputs (consumed):
- `GrooveBarContext` (bar-level info) and `role` parameter
- Derived `DrummerContext` (style, hat mode, backbeat, fill window)
- Enabled operator list (from registry + policy provider)
- Operator-specific RNG/seed and stream key
- `PhysicalityRules` / `PhysicalityFilter` configuration
- Style configuration and memory state (for gating / operator allow list)
- Diagnostics toggle/config

Outputs (produced):
- `IReadOnlyList<GrooveCandidateGroup>` (grouped, mapped, and filtered candidates)
- Side artifacts: diagnostics entry describing candidates considered/filtered

Configuration read:
- StyleConfiguration (allowed operators, weights, role caps)
- PhysicalityRules (limb mapping, sticking limits, density caps)
- RNG seed/streamKey conventions
- Memory settings that may affect operator enablement

## 5. Constraints & Invariants
- Determinism invariant: same context + same global seed + same streamKey → identical returned groups and ordering/ties.
- Safety invariant: No returned candidate may violate the configured `PhysicalityRules` (unless explicitly marked protected and allowed by policy).
- Grouping invariant: Candidates must be associated with their originating operator family so selection engine can apply family-level rules.
- Non-destructive invariant: Candidate generation must not mutate operator or global memory state (generation = read-only with respect to memory); recording decisions happens later.
- Order of operations (must be preserved):
  1. Resolve enabled operators for the bar (policy + style + memory).
  2. Invoke each operator to produce zero-or-more `DrumCandidate`s (use deterministic RNG for each operator).
  3. Map `DrumCandidate` → `GrooveOnsetCandidate` (preserve candidate ids and hints).
  4. Group mapped candidates by operator family into `GrooveCandidateGroup`s.
  5. Run `PhysicalityFilter` and overcrowding pruning on the grouped collection.
  6. Emit final `IReadOnlyList<GrooveCandidateGroup>` plus diagnostics if enabled.

## 6. Edge Cases to Test
- No enabled operators → return empty group list (or empty groups) gracefully.
- Operators return invalid or duplicate `CandidateId`s → detect and normalize or fail fast.
- Operators throw exceptions during generation → ensure errors are surfaced or operators are isolated so generation for other operators continues (behavior needs clarification).
- Physicality filter removes all candidates for a role/bar → how should the pipeline proceed (fallback, relax rules, or return empty)?
- Protected onsets: if an operator marks a candidate as protected, ensure pruning respects protection.
- Determinism checks: different seeds produce different outputs; same seed/context produce identical outputs on repeated runs.
- Overcrowding pruning ties: ensure deterministic tie-break (score desc → operatorId asc → candidateId asc) and that protected onsets are never pruned.
- Timing/velocity hint extremes (null, out-of-range values) → mapping must handle safely.
- High concurrency or repeated calls for adjacent bars: shared RNG streams or cached state must not cross-contaminate results.

## 7. Clarifying Questions

1. What exact fields must be present on the resulting `GrooveOnsetCandidate`? Which `DrumCandidate` fields map to which `GrooveOnsetCandidate` slots (especially articulation, timing hint, velocity hint, protected flag)?

**Answer:** Map DrumCandidate → GrooveOnsetCandidate as follows:
- `Role` → `Role` (direct)
- `Beat` → `OnsetBeat` (direct)
- `Strength` → `Strength` (direct)
- `Score` → `ProbabilityBias` (score becomes selection weight)
- `VelocityHint` and `TimingHint` are not stored in GrooveOnsetCandidate directly; they influence downstream velocity/timing shaping via Tags or are passed through an extended mapping helper.
- `FillRole` → mapped to Tags (e.g., FillRole.FillStart adds "FillStart" tag)
- `ArticulationHint` → mapped to Tags (e.g., Rimshot adds "Rimshot" tag)
- MaxAddsPerBar defaults to 1 unless operator specifies otherwise.
- DrumCandidate does not have a protected flag currently; protection is determined by FillRole (FillEnd candidates are protected) or by explicit marking in future operators.

2. What is the concrete shape and semantics of `GrooveCandidateGroup`? Should each group include operator-family metadata, aggregated scores, or preserve per-candidate scores only?

**Answer:** Use existing `GrooveCandidateGroup` structure:
- `GroupId` = "{OperatorFamily}" (e.g., "MicroAddition", "PhrasePunctuation")
- `GroupTags` = list of family-specific tags for filtering
- `MaxAddsPerBar` = aggregate cap for the group (sum of individual caps or configured limit)
- `BaseProbabilityBias` = average score of candidates in group
- `Candidates` = list of GrooveOnsetCandidate with individual ProbabilityBias preserved
- Groups are keyed by OperatorFamily enum to ensure family-level selection rules work.

3. Should the physicality filter run per-group (before grouping), per-candidate (after mapping), or on the entire candidate set (global pruning)? Which behavior is expected when pruning removes all candidates for a group?

**Answer:** Physicality filter runs globally on the entire candidate set AFTER grouping:
- Collect all candidates from all groups
- Apply limb conflict detection and sticking rules
- Remove violating candidates from their groups
- If a group becomes empty, keep it in the result with empty Candidates list
- This allows selection engine to see that the family produced no valid options.

4. How should operator errors be handled during generation: fail fast, skip operator with diagnostic entry, or bubble exceptions upward?

**Answer:** Skip operator with diagnostic entry (resilient mode):
- Catch exceptions from individual operator.GenerateCandidates() calls
- Log to diagnostics with operator ID and exception message
- Continue with remaining operators
- This ensures one broken operator doesn't prevent drum track generation.

5. Are some candidates considered "protected" by operators or policy and therefore exempt from overcrowding pruning? If so, how is protection indicated?

**Answer:** Yes, candidates can be protected:
- FillRole == FillEnd indicates a protected onset (crash after fill)
- Candidates with Strength == Downbeat or Backbeat are semi-protected (prefer to keep)
- Add "Protected" tag to GrooveOnsetCandidate.Tags when protected
- PhysicalityFilter respects protection by keeping protected candidates even if over cap.

6. What determinism tie-break rules must be used when pruning or ordering candidates? (The selection engine doc hints one approach but confirm if it should be reused here.)

**Answer:** Reuse OperatorSelectionEngine tie-break rules:
- Primary: Score descending (higher score = kept)
- Secondary: OperatorId ascending (alphabetical stability)
- Tertiary: CandidateId ascending (full determinism)
- Protected candidates are never pruned regardless of score.

7. Is the `DrumCandidateMapper` expected to be reversible or to preserve a stable `CandidateId` mapping strategy (operatorId + param hash)?

**Answer:** Mapper is one-way (not reversible) but preserves CandidateId:
- Original DrumCandidate.CandidateId is preserved as-is in the mapped result
- Add CandidateId to GrooveOnsetCandidate.Tags as "CandidateId:{value}" for traceability
- Mapping is deterministic: same DrumCandidate always produces same GrooveOnsetCandidate.

8. Should `DrummerCandidateSource` own diagnostics collection for candidates or rely on an external diagnostics collector hook? What level of diagnostic detail is required?

**Answer:** Use external GrooveDiagnosticsCollector hook (consistent with existing pattern):
- Accept optional GrooveDiagnosticsCollector in constructor
- Record: operators invoked, candidates generated per operator, candidates filtered, final group counts
- Level: summary counts in normal mode; per-candidate detail if verbose flag set
- Zero-cost when diagnostics disabled (null collector).

9. Are cancellation/timeout semantics required for `GetCandidateGroups` (e.g., CancellationToken) or is synchronous behavior acceptable for now?

**Answer:** Synchronous behavior is acceptable for now:
- Operators are expected to be fast (no I/O, no heavy computation)
- Future: can add CancellationToken parameter if operators become async
- Keep interface simple for MVP.

10. Performance expectation: any hard latency or allocation budgets for candidate generation per bar?

**Answer:** Soft guidance, no hard budget:
- Target: <1ms per bar for candidate generation
- Avoid allocations in hot path where possible (reuse lists, use spans)
- For MVP: correctness > performance; optimize after profiling if needed.

## 8. Test Scenario Ideas
- `GetCandidateGroups_ReturnsDeterministicCandidates_ForSameSeedAndContext`
  - Setup: single bar context, fixed seed, register 3 simple operators that each emit deterministic candidates.
  - Verify: repeated calls yield identical groups and ordering.

- `GetCandidateGroups_AppliesPhysicalityFilter_RemovesImpossibleCandidates`
  - Setup: operators produce overlapping limb assignments that violate `LimbModel` or `StickingRules`.
  - Verify: returned groups exclude impossible candidates and diagnostics note rejections.

- `GetCandidateGroups_MapsDrumCandidate_ToGrooveOnsetCandidate_Correctly`
  - Setup: operator emits candidate with velocity/timing/articulation hints and protected flag.
  - Verify: mapped `GrooveOnsetCandidate` fields reflect those hints and candidate id is stable.

- `GetCandidateGroups_PreservesOperatorFamilyGrouping`
  - Setup: operators from two families generate candidates.
  - Verify: returned `GrooveCandidateGroup` list contains distinct groups keyed by family and candidates remain associated with origin operator ids.

- `GetCandidateGroups_PrunesToRoleCaps_Deterministically`
  - Setup: many candidates exceeding `MaxHitsPerRolePerBar`.
  - Verify: pruning retains highest scored/protected candidates using deterministic tie-break rules.

- `GetCandidateGroups_HandlesEmptyOperatorList_Gracefully`
  - Setup: style/policy disables all operators for a role/bar.
  - Verify: function returns empty groups (or explicit empty result) and no exception thrown.

- `GetCandidateGroups_OperatorException_DoesNotBreak_OtherOperators`
  - Setup: one operator throws during generation; others return candidates.
  - Verify: generation continues for other operators and diagnostics capture the failure (behavior to be confirmed).


---

Notes: This analysis intentionally avoids implementation recommendations and focuses on clarifying behavior, dependencies, invariants, edge cases, and tests needed to implement Story 2.4 with low ambiguity.
