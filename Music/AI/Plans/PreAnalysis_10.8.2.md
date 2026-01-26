# PreAnalysis_10.8.2 — MaterialBank (motif storage and retrieval)

## 1. Story Intent Summary
- What: Provide a reliable storage and retrieval container for motif/material fragments (`MaterialBank`) so placed motifs can be looked up, reused and queried by generators and tools.
- Why: Centralizes motif access for deterministic placement and rendering, enables reuse, discovery by role/kind, and supports downstream analysis and rendering (Stage 9+).
- Who benefits: Developers (clear API, testability), Generator (MotifPlacementPlanner, MotifRenderer), End-users (reproducible motifs across songs and tests).

## 2. Acceptance Criteria Checklist
1. Ability to add a `PartTrack` representing a motif to the bank (`Add(PartTrack)`).
2. Ability to try-get a motif by `PartTrack.PartTrackId` (`TryGet(id, out PartTrack?)`).
3. Query methods: `GetByKind(PartTrackKind)`, `GetByMaterialKind(MaterialKind)`, `GetByRole(string)`.
4. Convenience methods: `GetMotifsByRole(string)` and `GetMotifByName(string)`.
5. Deterministic iteration/lookup order for reproducibility (stable ordering across runs given same inputs).
6. Defensive handling of duplicate IDs or duplicate names (explicit behavior documented).
7. Thread-safety expectations (clarify whether concurrent reads/writes are supported).

Notes: Criteria 5–7 are not explicit in source but are important assumptions; they should be confirmed.

## 3. Dependencies & Integration Points
- Depends on: Story 10.8.1 (MotifSpec definitions) and Stage 9 (MotifPlacementPlanner / MotifRenderer) for usage.
- Interacts with types: `PartTrack`, `PartTrack.PartTrackId`, `PartTrackKind`, `MaterialKind`, `MotifSpec`, `MaterialBank`, `BarTrack` (indirectly via placement).
- Provides for future stories: a canonical motif repository for `MotifPlacementPlanner`, `MotifRenderer`, `MotifPresenceMap`, and serialization/benchmarking.

## 4. Inputs & Outputs
- Inputs consumed: `PartTrack` instances (material fragments), `PartTrack.PartTrackId`, names, role tags, and classification metadata embedded in `PartTrack.Meta`.
- Outputs produced: Queryable collections (IReadOnlyList/ IReadOnlySet) of `PartTrack` motifs; boolean success flags from `TryGet`.
- Configuration read: none explicit; may read or expose conventions for name uniqueness, indexing strategies, and serialization/versioning.

## 5. Constraints & Invariants
- Invariants that must hold:
  - `PartTrack.PartTrackId` is the canonical identifier; lookups by id must be exact.
  - Adding a motif with an existing id must either reject or replace deterministically (policy required).
  - Public APIs must not mutate stored `PartTrack` instances unexpectedly (store as-is or document copy semantics).
  - Query methods should be case-insensitive where names/roles are free-form, or the behavior must be documented.
- Hard limits: none specified, but memory/resource considerations apply for very large banks.
- Operation order: index/update paths (Add) must keep query caches consistent (if caching used).

## 6. Edge Cases to Test
- Adding a null `PartTrack` or a `PartTrack` missing `Meta` fields.
- Adding a motif with duplicate `PartTrackId` and with duplicate `Meta.Name`.
- Querying by unknown id/name/role/key returns empty results / false without throwing.
- Empty bank queries (no motifs) return empty collections.
- Large numbers of motifs: ensure predictable ordering and acceptable performance.
- Case-sensitivity: names/roles with different casing — clarify expected normalization.
- Concurrency: simultaneous Add and Get calls (race conditions) — clarify thread model.

## 7. Clarifying Questions

Question 1:
On duplicate `PartTrackId`: should `Add()` throw, ignore, or replace? Is an explicit `Update()` API desired?

Answer:
`Add()` should throw `ArgumentException` with a clear message when attempting to add a motif with a duplicate `PartTrackId`. This enforces explicit intent and prevents accidental overwrites. No separate `Update()` API is needed for this story—clients should remove and re-add if replacement is intended. This aligns with the fail-fast principle used throughout the project (e.g., `DrumOperatorRegistry.RegisterOperator` throws on duplicates).

Question 2:
Are motif names guaranteed unique? If not, what does `GetMotifByName` return (first, all, error)?

Answer:
Motif names are NOT guaranteed unique (multiple motifs can have the same name for variations). `GetMotifByName(string name)` returns the **first** motif with matching name in registration order, or `null` if no match found. This provides simple, deterministic behavior. For multiple matches, clients should use `GetByRole()` or `GetByMaterialKind()` with additional filtering. Name matching is **case-sensitive** (consistent with C# string comparison defaults and operator IDs).

Question 3:
Thread-safety: must the `MaterialBank` support concurrent reads and writes, or is it single-threaded setup then read-only?

Answer:
`MaterialBank` must be **read-safe after initialization** (concurrent reads allowed). Write operations (`Add()`) are NOT thread-safe and should occur during single-threaded setup phase (song context initialization). This matches the usage pattern: motifs are added once during `SongContext` setup, then queried during generation. No concurrent writes are expected. Document this constraint in XML comments: "Not thread-safe for writes. Add all motifs during initialization before concurrent access."

Question 4:
Ownership semantics: does `MaterialBank` take ownership (and clone) `PartTrack` objects, or keep references as-is?

Answer:
`MaterialBank` **stores references as-is** without cloning. This is consistent with other project patterns (e.g., `GroovePresetDefinition` stores reference to `GrooveInstanceLayer`). `PartTrack` instances added to the bank should not be modified after addition. The bank returns the same reference via `TryGet()` and query methods. Document in XML comments: "Stores PartTrack references. Do not modify PartTrack instances after adding to bank."

Question 5:
Persistence: is serialization/persistence part of this story or handled elsewhere (serializer expectations, versioning)?

Answer:
Serialization is **out of scope** for this story. `MaterialBank` is a runtime container only. Persistence is handled by separate serialization infrastructure (future work, similar to how `DrumTrackFeatureData` has `DrumFeatureDataSerializer`). The bank should be serialization-friendly (public getters, no hidden state), but no serializer implementation is required here.

Question 6:
Query casing: should role/name queries be case-insensitive and trimmed? Are tags normalized elsewhere?

Answer:
- **Role queries:** Case-insensitive (use `StringComparer.OrdinalIgnoreCase`). Roles like "Kick", "kick", "KICK" should match. This is consistent with how roles are used throughout the codebase (flexible string identifiers).
- **Name queries:** Case-sensitive (exact match). Names are user-defined identifiers; preserve exact casing. This is consistent with operator IDs being case-sensitive.
- **No trimming:** Do not automatically trim whitespace. Callers are responsible for providing clean input.
- Tags are already normalized by `PartTrackMeta` construction; no additional normalization needed in `MaterialBank`.

Question 7:
Ordering: what determines deterministic ordering for query results (registration order, name-sorted, id-sorted)?

Answer:
Query results follow **registration order** (order of `Add()` calls). This ensures deterministic, reproducible results across runs with the same initialization sequence. Internally, use `List<PartTrack>` to preserve insertion order. All query methods (`GetByKind`, `GetByRole`, etc.) return results in registration order. This aligns with `DrumOperatorRegistry` pattern (operators returned in registration order).

## 8. Test Scenario Ideas
- `MaterialBank_AddAndTryGet_ById_ReturnsSamePartTrack`
  - Add a motif, then `TryGet(id)` → returns the same reference or an equivalent copy.
- `MaterialBank_GetByKind_MatchesExpectedMotifs`
  - Add motifs with different `PartTrackKind` and verify filtering.
- `MaterialBank_GetByRole_ReturnsMotifsForRole_CaseInsensitive`
  - Ensure role lookup normalizes casing if required.
- `MaterialBank_AddDuplicateId_ThrowsOrReplaces_BehaviorDefined`
  - Parameterized test for expected duplicate-id policy.
- `MaterialBank_GetMotifByName_MultipleSameName_ReturnsFirstOrAll_BehaviorDefined`
  - Clarify and assert behavior for duplicate names.
- `MaterialBank_EmptyQueries_ReturnEmptyCollections`
  - Query an empty bank for all methods.
- `MaterialBank_ConcurrentReads_NoDataRace`
  - If concurrent reads expected, ensure thread-safety for multiple readers.

---

// EOF
