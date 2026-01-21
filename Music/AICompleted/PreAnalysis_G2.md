# PreAnalysis — Story G2: Add “Provenance” to Onsets

## Story
- Story ID: G2
- Title: Add "Provenance" to Onsets
- User Story: As a developer I want each onset to remember where it came from so that later systems (drummer model, ducking, analysis) can reason about it.

## 1) Story Intent Summary
- What: Record origin metadata on every `GrooveOnset` (anchor vs variation and identifiers/tags) so downstream systems can attribute, filter, or learn from origins.
- Why: Enables explainability, supports diagnostics, allows future drummer/operator engines and analysis tooling to reason about which onsets originated from anchors, specific candidate groups, or operator inputs without altering audible output.
- Who benefits: Developers (debugging/tracing), QA (determinism/regression), integrators and future Drummer/Operator engineers (policy training/tuning), and indirectly end-users via safer policy changes.

## 2) Acceptance Criteria Checklist
1. Add `Provenance` fields to `GrooveOnset`:
   1.1 `Source = Anchor | Variation`.
   1.2 `GroupId` (nullable).
   1.3 `CandidateId` (nullable).
   1.4 `TagsSnapshot` (optional) — capture tags at time of selection.
2. Ensure provenance does not affect sorting or output determinism.
3. Unit test: provenance fields are stable for identical runs.

Notes on ambiguities in ACs:
- `TagsSnapshot (optional)`: format and size are unspecified (string list, compressed flags, or full tag objects).
- `CandidateId`/`GroupId` format and stability requirements are not fully specified (composite key? index-based?).

## 3) Dependencies & Integration Points
- Depends on/complements:
  - Story A1 (Groove output contracts) — `GrooveOnset` and `GrooveBarPlan` types.
  - Story G1 (Diagnostics) — diagnostics and collectors will read/write provenance for explanation.
  - Selection/variation stories (B1-B4, C2) — provenance set during variation selection and merge.
  - Caps enforcement/pruning (C3, F1) — pruning/merge logic must respect or record provenance when creating synthetic onsets.
- Interacts with code/types:
  - `GrooveOnset` record
  - `GrooveVariationCatalog`, `GrooveCandidateGroup`, `GrooveOnsetCandidate`
  - `GrooveSelectionEngine`, `GrooveCapsEnforcer`, `ProtectionApplier`, `GrooveDiagnosticsCollector`
  - `GrooveBarPlan` and any serialization/export layers (diagnostics/golden tests)
- Provides for future stories:
  - G1 diagnostics richer traces, G2 feeds DrummerPolicy training and provenance-based rules, H1/H2 regression analysis.

## 4) Inputs & Outputs
- Inputs consumed:
  - Candidate metadata (group id, candidate id, candidate tags) from `GrooveVariationCatalog` or `IGrooveCandidateSource`.
  - Anchor origin information during anchor extraction (anchor identity may be implicit).
  - Policy decisions and merge policy signals (when overrides create or remove onsets).
- Outputs produced:
  - `GrooveOnset.Provenance` populated for anchors and variation-added onsets (nullable for synthetic/created events if chosen).
  - Updated diagnostics entries referencing `Provenance` fields.
- Config/settings read:
  - Whether to capture `TagsSnapshot` (opt-in, size limits?) and any provenance privacy/config rules.

## 5) Constraints & Invariants
- Must ALWAYS be true:
  - Provenance must not change sorting semantics or affect deterministic selection outcomes.
  - `IsMustHit` / `IsNeverRemove` semantics remain authoritative; provenance cannot override protection rules.
  - Provenance fields may be null when not available.
- Hard limits / invariants:
  - Provenance must be immutable once set for a given `GrooveOnset` (no late mutation that would alter decision traces).
  - Candidate/Group identifiers must be stable and comparable across runs for determinism tests.
- Required operation order:
  - Set provenance at the time an onset is created/selected (anchors during extraction; variations during selection/merge);
  - Preserve provenance through pruning/constraint passes (pruned items may still be recorded in diagnostics but removed from final lists).

## 6) Edge Cases to Test
- Missing catalog entries: selection references a candidate that is not found; provenance should reflect best-effort ids (or null) without breaking.
- Duplicate candidates across layers: same beat from two groups — ensure provenance identifies which group/candidate was actually selected.
- Anchors vs variations at same beat: verify precedence and provenance for the final onset.
- Synthetic onsets created by protection applier (`createEvent`) — define whether provenance should mark them as "synthetic" or attribute to a group.
- Large `TagsSnapshot` (many tags) — verify size/perf and that capturing tags doesn't change behavior.
- Serializing/deserializing plans with provenance — ensure stability and no loss of determinism.
- Null/empty provenance for legacy onsets — generator must handle null gracefully.
- Concurrency: if multiple threads touch the bar plan, provenance must remain consistent (likely single-threaded generation but note race risk).

## 7) Clarifying Questions
1. Format and type:
   - What exact type/shape should `GroupId` and `CandidateId` be (string, composite, GUID, index)?
   - Should `TagsSnapshot` be a shallow list of strings, a set, or a richer object (e.g., tag+value)?
2. Scope of provenance capture:
   - Should provenance be captured for synthetic onsets created by `ProtectionApplier.CreateEvent`? If so, what `Source` value should be used (e.g., "Synthetic" or "ProtectionCreated")?
   - Should provenance include layer/variation layer id and candidate index to guarantee uniqueness across catalogs?
3. Determinism and stability:
   - What guarantees are required for `CandidateId` stability across runs and catalog evolution? Is a stable GUID per candidate expected?
   - If catalogs change between runs (catalog updates), how should tests that assert provenance stability be defined?
4. Privacy and size concerns:
   - Are there limits on captured `TagsSnapshot` size or sensitive tags that must be excluded from diagnostics/provenance?
5. Diagnostics interplay:
   - Should diagnostics reference provenance by id only, or include a snapshot (GroupId/CandidateId/Tags) inline to avoid cross-referencing?
6. Persistence/serialization:
   - Will `GrooveBarPlan` be serialized to disk or golden files including provenance? If yes, what is the desired serialized shape/versioning expectations?
7. Backwards compatibility:
   - For existing code that expects `Diagnostics` or `GrooveOnset` without provenance, is schema evolution acceptable or should provenance be optional and benign by design?

## 8) Test Scenario Ideas
- Unit tests (names):
  - `Provenance_Assigned_For_Anchor_On_Extraction`
  - `Provenance_Assigned_For_Variation_On_Selection`
  - `Provenance_Null_For_Synthetic_When_ConfiguredToNull`
  - `Provenance_DoesNotAffect_Output_Sorting`
  - `Provenance_Stable_Across_Deterministic_Runs`
  - `Provenance_Preserved_After_Prune_For_ProtectedOnsets`
  - `Provenance_Handles_Duplicate_Candidates_Across_Layers`
  - `Provenance_TagsSnapshot_Size_Limit_Enforced`
- Test data setups:
  - Minimal catalog with a single group and candidate; select and verify provenance fields populated.
  - Catalog with two layers where second layer replaces a candidate; verify groupId reflects final chosen candidate.
  - Run the selection pipeline twice with the same master seed and assert identical provenance records.
  - Create a protection-created onset and verify provenance value conforms to policy (synthetic marker or null).
- Determinism verification points:
  - Compare provenance records between runs with identical seeds and inputs.
  - Verify that provenance does not change the `FinalOnsets` ordering or counts.

---

**End of PreAnalysis_G2**
