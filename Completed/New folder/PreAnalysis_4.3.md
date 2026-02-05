# PreAnalysis_4.3 — Implement Physicality Filter

Story: 4.3 — Implement Physicality Filter

## 1. Story Intent Summary
- What: Provide a filter that removes drum operator candidates that are physically unplayable or violate sticking/overcrowding rules before selection.
- Why: Prevents the generator from selecting impossible or ergonomically implausible drum patterns, improving realism and preserving downstream assumptions (density caps, anchors).
- Who benefits: Generator (produces playable outputs), end-users/listeners (realistic grooves), and developers (clear guardrails, fewer downstream failures).

## 2. Acceptance Criteria (extracted)
1. Create `PhysicalityFilter` class:
   1. `Filter(List<DrumCandidate>, PhysicalityRules) → List<DrumCandidate>`
   2. Removes candidates that cause limb conflicts
   3. Removes candidates that violate sticking rules
   4. Logs rejections to diagnostics (when enabled)
2. Create `PhysicalityRules` configuration:
   1. `LimbModel`
   2. `StickingRules`
   3. `AllowDoublePedal` (bool)
   4. `StrictnessLevel` (Strict, Normal, Loose)
3. `DrummerCandidateSource` must call the filter before returning candidates
4. Unit tests: impossible patterns rejected, valid patterns pass

Grouped:
- Filtering behavior (2.1.*)
- Configuration model (2.2.*)
- Integration point with `DrummerCandidateSource` (2.3)
- Validation via unit tests (2.4)

Ambiguous / unclear ACs:
- "Logs rejections to diagnostics (when enabled)" — which diagnostics API and expected level/format?
- Exact semantics of `StrictnessLevel` (what changes between Strict/Normal/Loose).
- Whether the filter must be idempotent or preserve original ordering.

## 3. Dependencies & Integration Points
- Depends on:
  - Story 4.1 (LimbModel, LimbAssignment, LimbConflictDetector) — already completed.
  - Story 4.2 (StickingRules, StickingValidation) — described as implemented in architecture.
  - Operator generator pipeline: `DrummerCandidateSource` (must call the filter).
  - Diagnostics subsystem (GrooveBarDiagnostics / DrummerDiagnosticsCollector) for logging rejections.
- Interacts with existing types:
  - `DrumCandidate`
  - `LimbModel`, `LimbAssignment`, `LimbConflictDetector`
  - `StickingRules`, `StickingValidation`
  - `PhysicalityRules` (new)
  - `DrummerCandidateSource`, `DrumCandidateMapper`, `GrooveCandidateGroup`
  - RNG/selection is not directly required (but deterministic tie-breaks for pruning are expected)
- Provides for future stories:
  - Overcrowding prevention (4.4) — pruning hooks and caps extension.
  - Pop Rock style physicality defaults (5.3).
  - Diagnostics for tuning (7.1).

## 4. Inputs & Outputs
- Inputs consumed:
  - `IReadOnlyList<DrumCandidate>` (candidates from operators)
  - `PhysicalityRules` (LimbModel, StickingRules, AllowDoublePedal, StrictnessLevel, later extensions)
  - Optional diagnostics collector/context (to record rejections)
- Outputs produced:
  - Filtered `List<DrumCandidate>` (playable subset)
  - Diagnostic entries describing rejected candidates and reasons (when enabled)
  - Possible metadata about pruned counts / conflicts (for downstream logging)
- Configuration/settings read:
  - `PhysicalityRules` values (caps, strictness)
  - `LimbModel` role→limb mapping
  - `StickingRules` thresholds (MaxConsecutiveSameHand, MaxGhostsPerBar, MinGapBetweenFastHits)
  - `AllowDoublePedal` (affects kick limb conflict logic)
  - `StrictnessLevel` (policy for how to treat violations)

## 5. Constraints & Invariants
- Must ALWAYS preserve groove anchors and protected onsets (e.g., `IsMustHit`, `IsProtected`) — never prune these.
- Conflict detection rule: same limb required for overlapping events at the exact same (BarNumber, Beat) constitutes a limb conflict.
- Sticking rules must be interpreted per-call and deterministically.
- Determinism: filter behavior must be deterministic given same inputs and configuration.
- Hard limits to consider (to be enforced or referenced by filter / later pruning):
  - `MaxHitsPerBeat`, `MaxHitsPerBar`, `MaxHitsPerRolePerBar` (from 4.4, may be applied here or as extension)
- Operation order:
  1. Convert candidates → limb assignments (via `LimbModel`)
  2. Run limb-conflict detection and remove offending candidates (respecting protections)
  3. Run sticking validation; remove or mark violating candidates according to `StrictnessLevel`
  4. (Optional) Apply overcrowding pruning (lowest-scored first) — part of 4.4
  5. Emit diagnostics
- Must not mutate original candidate objects in a way that breaks other components (prefer returning filtered copy).

## 6. Edge Cases to Test
- Empty candidate list → returns empty list, no diagnostics, no exceptions.
- All candidates protected (`IsMustHit` / `IsProtected`) and conflicting → ensure protected ones are preserved and non-protected rejected.
- Unknown roles (role not mapped in `LimbModel`) → per policy, skip limb-conflict checks for unknown roles (architecture note), ensure no false positives.
- Simultaneous multi-role events:
  - Snare + Hat on same beat (different limbs) → allowed.
  - Snare + Tom1 (both same limb) → conflict → remove one or both per strictness rules.
- Edge ticks / fractional beats causing near-overlaps → exact-equality only is conflict per spec; verify span/overlap semantics if multi-tick events considered.
- Sticking rules spanning bar boundaries (Consecutive same-hand across bar boundary) — ensure windowing handles cross-bar detection.
- Double-pedal allowed vs disallowed:
  - If `AllowDoublePedal=false`, two Kick events at same beat should be conflict if they imply double-foot usage.
- MinGapBetweenFastHits boundary conditions: hits exactly at min gap should be allowed; slightly less should trigger violation.
- Conflicting violations combined: limb conflict + sticking violation — ensure deterministic resolution and coherent diagnostic reason ordering.
- Strictness modes:
  - `Strict` = reject any violating candidate
  - `Normal` = try to prune minimally (e.g., lowest score)
  - `Loose` = only log violations, keep candidates (need clarification)
- Performance: very large candidate pools — ensure filter handles reasonable sizes without stateful mutation; test with maximal candidate count.

## 7. Clarifying Questions (answered)

1. Diagnostics:
   - Question: Which diagnostics record should be used? `GrooveBarDiagnostics` or `DrummerDiagnostics`? What exact fields are required for rejection logs?
   - Answer: Use the existing `GrooveDiagnosticsCollector` (builds `GrooveBarDiagnostics`). Record rejections with `RecordFilter(candidateId, reason)` and pruning events with `RecordPrune(onsetId, reason, wasProtected)`. Use concise reason strings with prefix (e.g., "LimbConflict:LeftHand", "Sticking:MaxGhostsPerBar").

2. StrictnessLevel semantics:
   - Question: Define concrete behaviors for `Strict`, `Normal`, and `Loose` modes.
   - Answer: 
     - Strict: Remove all non-protected candidates involved in violations
     - Normal: Minimal pruning—keep highest-scored candidate, remove others (deterministic tie-break)
     - Loose: Log violations only, keep all candidates
     - No partial fixes (timing/role mutation)—filter only removes or logs

3. Pruning policy:
   - Question: What deterministic tie-break rule for pruning?
   - Answer: Primary: Score descending (keep higher). Secondary: OperatorId ascending. Tertiary: CandidateId ascending.

4. Protected onsets:
   - Question: How are protected flags represented?
   - Answer: Protection is carried as `DrumCandidateMapper.ProtectedTag` on `GrooveOnsetCandidate.Tags`. Filter uses `DrumCandidateMapper.IsProtected()` to check. No new fields on `DrumCandidate`.

5. Overlap semantics:
   - Question: Exact position equality or tick-window?
   - Answer: Limb conflicts use exact (BarNumber, Beat) equality. Near-overlaps (tick-based) are handled by StickingRules.MinGapBetweenFastHits.

6. Sticking validation consequences:
   - Question: Remove or mark for later?
   - Answer: Filter applies removal/pruning according to StrictnessLevel. Does not defer to selection engine.

7. Integration point:
   - Question: What type does DrummerCandidateSource expect?
   - Answer: `IReadOnlyList<GrooveOnsetCandidate>` input + barNumber → `IReadOnlyList<GrooveOnsetCandidate>` output. Source rebuilds groups after filtering.

8. Double-pedal modeling:
   - Question: How are double-kick events represented?
   - Answer: Multiple Kick candidates at same position. When AllowDoublePedal=false, these are limb conflicts. When true, permitted.

9. Desired unit test surface:
   - Question: Existing test fixtures?
   - Answer: Use `DrumCandidate.CreateMinimal()` and `LimbModel.Default`. For mapped candidates, construct `GrooveOnsetCandidate` with tags using `DrumCandidateMapper.*TagPrefix` and `ProtectedTag`.

## 8. Test Scenario Ideas (unit test name suggestions)
- `PhysicalityFilter_EmptyCandidates_ReturnsEmptyNoDiagnostics`
- `PhysicalityFilter_PreservesProtectedOnsets_EvenWhenConflicting`
- `PhysicalityFilter_RemovesLimbConflicts_BasedOnLimbModel`
- `PhysicalityFilter_SkipsUnknownRoleMappings_NoConflictRaised`
- `PhysicalityFilter_DetectsStickingViolations_MaxConsecutiveSameHandExceeded`
- `PhysicalityFilter_AllowDoublePedal_AllowsSimultaneousKicksWhenEnabled`
- `PhysicalityFilter_StrictMode_RemovesAllViolations`
- `PhysicalityFilter_NormalMode_PrunesLowestScoredCandidatesToResolveConflicts`
- `PhysicalityFilter_LooseMode_LogsViolationsButKeepsCandidates`
- `PhysicalityFilter_DeterministicPrune_TieBreakByScoreThenOperatorId`
- `DrummerCandidateSource_CallsPhysicalityFilter_BeforeReturn` (integration)
- `PhysicalityFilter_DiagnosticsContainCandidateIdAndReason` (verify diagnostic format)
- `PhysicalityFilter_CrossBarSticking_ViolationsSpanBoundaryHandled`

Test data setups:
- Minimal `LimbModel.Default` with standard mappings.
- Small bars with candidates that force left-hand double-assignments (snare+tom) to assert removal.
- Candidate pools with varied `Score` values to validate deterministic pruning order.
- StickingRules variations: tweak `MaxConsecutiveSameHand`, `MaxGhostsPerBar`, `MinGapBetweenFastHits`.

## Summary of open decisions (priority)
1. Clarify `StrictnessLevel` concrete behaviors.
2. Confirm diagnostics target and schema.
3. Define deterministic pruning tie-break rules.
4. Confirm exact conflict semantics (exact beat equality vs tick-window).
5. Confirm integration contract with `DrummerCandidateSource` (input/output types).
