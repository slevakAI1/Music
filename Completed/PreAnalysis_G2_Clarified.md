# PreAnalysis — Story G2: Add “Provenance” to Onsets (Clarified)

This document updates `PreAnalysis_G2.md` by resolving ambiguities using:
- `Music/AIPlans/CurrentEpic.md`
- `Music/AIPlans/ProjectArchitecture.md`
- Relevant solution source code (notably `Generator/Groove/GrooveOnsetProvenance.cs`, `GrooveOnset.cs`, and `GrooveDiagnosticsCollector.cs`).

## Story
- Story ID: G2
- Title: Add "Provenance" to Onsets
- User Story: As a developer I want each onset to remember where it came from so that later systems (drummer model, ducking, analysis) can reason about it.

## 1) Story Intent Summary
- What: Ensure every `GrooveOnset` can carry an explicit origin record describing whether it came from anchors or variation selection, with optional IDs/tags.
- Why: Provenance is a foundational explainability hook for diagnostics, policy tuning, and later agent behaviors.
- Who benefits: Developers and future agent/policy work primarily; end-users indirectly via safer iteration/debug.

## 2) Acceptance Criteria Checklist (from CurrentEpic)
1. Add `Provenance` fields to `GrooveOnset`:
   1.1 `Source = Anchor | Variation`
   1.2 `GroupId`, `CandidateId` (nullable)
   1.3 `TagsSnapshot` (optional)
2. Ensure provenance does not affect sorting or output determinism.
3. Unit test: provenance fields are stable for identical runs.

✅ Ambiguities resolved by codebase:
- `Source` is implemented as `GrooveOnsetSource` enum with values `Anchor` and `Variation`.
- `Provenance` is implemented as `GrooveOnsetProvenance` record with:
  - required `Source`
  - optional `GroupId`, `CandidateId`
  - optional `IReadOnlyList<string> TagsSnapshot`

## 3) Dependencies & Integration Points
- Depends on:
  - A1 (output contract where `GrooveOnset` exists)
  - G1 (diagnostics collector uses stable ids; provenance is a parallel explanation hook)
  - B2/B3/C2 (candidate selection provides the identifiers provenance refers to)
- Integrates with:
  - `GrooveOnset` and all producers of onsets (anchor extraction, variation selection, any creation helpers)
  - `GrooveCapsEnforcer` and other constraint steps which prune but should not mutate origin metadata
  - Diagnostics tooling that needs stable ids (`GrooveDiagnosticsCollector.MakeCandidateId/MakeOnsetId`)
- Provides for:
  - Future G1 trace enrichment (joining prune/selection decisions to origins)
  - Future Drummer policy/agent stages (operator attribution, ducking/analysis by origin)

## 4) Inputs & Outputs
- Inputs:
  - Anchor-layer creation of onsets (source = Anchor)
  - Variation selection system output (source = Variation + identifiers)
  - Candidate/group identifiers as already used elsewhere (e.g., diagnostics uses `groupId:beat` candidate ids)
  - Enabled tags used in filtering/selection (when available)
- Outputs:
  - `GrooveOnset.Provenance` populated according to explicit rules below
  - Optional `TagsSnapshot` on provenance when tags are available at the point the onset is created/selected

## 5) Constraints & Invariants
- Must hold:
  - Provenance must not alter any selection weights, pruning decisions, ordering, or other behavior.
  - Provenance is metadata-only.
  - `GrooveOnsetProvenance.Source` is always set when provenance is present.
- Hard limits:
  - Keep provenance nullable (`GrooveOnsetProvenance?`) everywhere to remain compatible with stages that do not yet set it.

## 6) Edge Cases to Test (refined)
- Anchor onset created without any group/candidate context → `Source=Anchor`, `GroupId/CandidateId=null`.
- Variation onset created when candidate IDs are only derivable from `GroupId` + beat → ensure consistent population.
- Tags unavailable at selection time (no enabled tags context provided) → `TagsSnapshot=null`.
- Tags available but empty → treat as empty list snapshot (distinct from null).
- Multiple candidates share beat across different groups → provenance must distinguish by `GroupId`.
- Post-processing stages (caps/pruning/timing/velocity) must not overwrite provenance.

## 7) Clarifying Questions (Resolved into Explicit Rules)

### 7.1 Format and type of `GroupId` and `CandidateId`
**Rule:** Use the existing string identifiers already present in groove candidate structures.
- `GroupId` is a string that matches `GrooveCandidateGroup.GroupId`.
- `CandidateId` is a string; where a dedicated candidate id does not exist, it must be populated using the same stable format used by diagnostics helpers.
  - Existing stable format in codebase: `GrooveDiagnosticsCollector.MakeCandidateId(groupId, beat)` => `"{groupId}:{beat:F2}"`.

### 7.2 `TagsSnapshot` type and semantics
**Rule:** `TagsSnapshot` is `IReadOnlyList<string>?`.
- If the selection/filter stage has a concrete enabled-tag list available at the moment the onset is created/selected, store it as an ordered list snapshot.
- If the enabled-tag list is not available, set `TagsSnapshot = null`.
- If tags are available but there are none, set `TagsSnapshot` to an empty list (not null).

### 7.3 Scope: which onsets must have provenance
**Rule:** Provenance is allowed on all onsets; it is mandatory only for onsets created directly from:
- Anchor layer extraction: set `Source = Anchor`.
- Variation selection: set `Source = Variation`.

**Rule:** For any onset created outside anchors/variation selection (e.g., protection-created, synthetic, or transformed events), provenance may be:
- preserved (if derived from an existing onset), or
- left null if the system cannot attribute it reliably.

(There is no `Synthetic` source in the current enum; do not introduce new values as part of Story G2 clarification.)

### 7.4 Uniqueness and catalog evolution
**Rule:** Provenance stability is defined relative to identical inputs and identical catalog content.
- If the catalog changes, tests asserting exact provenance are out of scope; tests should be constructed with fixed in-test catalogs or fixtures.

### 7.5 Privacy and size concerns
**Rule:** No additional privacy model is introduced by Story G2. Provenance stores only tag strings already used in selection.
- If future tag sets become large/sensitive, that is a separate story; for G2, store tags as-is.

### 7.6 Diagnostics interplay
**Rule:** Diagnostics and provenance are complementary.
- Diagnostics uses stable string ids (`CandidateId` / `OnsetId`) for trace records.
- Provenance provides origin metadata on the `GrooveOnset` itself.
- No requirement to duplicate provenance inside diagnostics in Story G2.

### 7.7 Persistence/serialization
**Rule:** Story G2 does not require persistence format changes.
- Provenance being part of `GrooveOnset` means it will naturally flow into any in-memory consumers.
- Any explicit serialization format commitments (golden snapshots) belong to Story H2.

### 7.8 Backwards compatibility
**Rule:** Provenance must remain optional and benign.
- Existing code that ignores `GrooveOnset.Provenance` must continue to function correctly.

## 8) Test Scenario Ideas (updated)
- `GrooveOnsetProvenance_SourceAnchor_HasNullGroupAndCandidate`
- `GrooveOnsetProvenance_SourceVariation_HasGroupIdAndCandidateId`
- `GrooveOnsetProvenance_TagsSnapshot_NullWhenUnavailable`
- `GrooveOnsetProvenance_TagsSnapshot_EmptyWhenNoTags`
- `GrooveOnsetProvenance_IsStable_ForIdenticalRuns_WithFixedCatalog`
- `GrooveOnsetProvenance_IsPreserved_AfterCapsEnforcement`

Determinism verification points:
- Compare `FinalOnsets` across two runs with same seed to ensure unchanged output.
- Compare provenance fields across two runs with same seed and same fixed catalog fixture.

---

**End of PreAnalysis_G2_Clarified**
