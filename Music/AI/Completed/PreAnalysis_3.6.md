# Pre-Analysis — Story 3.6: Operator Registry and Discovery

## 1. Story Intent Summary
- **What:** Create a centralized registry system for discovering, filtering, and accessing all 28 drum operators (across 5 families: MicroAddition, SubdivisionTransform, PhrasePunctuation, PatternSubstitution, StyleIdiom).
- **Why:** Operators need to be discoverable at runtime by family, by ID, or by style configuration. The registry provides a single source of truth for all operators and enables filtering based on style rules (e.g., which operators are allowed in PopRock vs Jazz).
- **Who:** Beneficiaries include the drummer agent (runtime operator discovery), developers (clear registration point), and future style configurations (enable/disable operators per genre).

## 2. Acceptance Criteria Checklist

### DrumOperatorRegistry Class
1. `RegisterOperator(IDrumOperator operator)` — add an operator to the registry
2. `GetOperatorsByFamily(OperatorFamily family) → IReadOnlyList<IDrumOperator>` — retrieve all operators of a specific family
3. `GetOperatorById(operatorId) → IDrumOperator?` — lookup by unique ID
4. `GetAllOperators() → IReadOnlyList<IDrumOperator>` — retrieve all registered operators
5. `GetEnabledOperators(StyleConfiguration style) → IReadOnlyList<IDrumOperator>` — filter by style's allowed list

### DrumOperatorRegistryBuilder Class
6. Create builder that registers all 28 operators (7 MicroAddition + 5 SubdivisionTransform + 7 PhrasePunctuation + 4 PatternSubstitution + 5 StyleIdiom)
7. Total operator count validation: exactly 28 operators

### Unit Tests
8. Registry contains all 28 operators
9. Filtering by family works correctly
10. Filtering by style configuration works correctly
11. Lookup by ID works correctly

**Ambiguities:** None significant; acceptance criteria are clear and measurable.

## 3. Dependencies & Integration Points

### Dependent Stories (Prerequisites)
- **Story 1.1** (IMusicalOperator interface) — operators implement this contract
- **Story 1.4** (StyleConfiguration) — style filtering depends on `StyleConfiguration.AllowedOperatorIds`
- **Story 2.2** (DrumCandidate) — operators generate these candidates
- **Story 3.1** (MicroAddition operators) — 7 operators to register
- **Story 3.2** (SubdivisionTransform operators) — 5 operators to register
- **Story 3.3** (PhrasePunctuation operators) — 7 operators to register
- **Story 3.4** (PatternSubstitution operators) — 4 operators to register
- **Story 3.5** (StyleIdiom operators) — 5 operators to register

### Existing Code This Interacts With
- `IDrumOperator` interface (from Story 1.1)
- `OperatorFamily` enum (from Story 1.1)
- `StyleConfiguration` and `StyleConfigurationLibrary` (from Story 1.4)
- All operator implementations (Stories 3.1-3.5)
- `DrummerCandidateSource` (from Story 2.4) — will consume registry to get enabled operators

### What This Story Provides
- Central registry for all drum operators (discovery hub)
- Deterministic registration order (reproducibility)
- Style-based filtering (genre behavior)
- Foundation for Stage 5 (Pop Rock style configuration will reference operator IDs)
- Foundation for Story 2.4 updates (DrummerCandidateSource will query registry)

## 4. Inputs & Outputs

### Inputs (Consumed)
- All 28 `IDrumOperator` instances (created by builder)
- `StyleConfiguration` (for filtering via `AllowedOperatorIds`)
- `OperatorFamily` enum value (for family-based filtering)
- `string operatorId` (for ID-based lookup)

### Outputs (Produced)
- `IReadOnlyList<IDrumOperator>` (filtered or complete operator lists)
- `IDrumOperator?` (nullable for ID lookups)
- Registry state (internal collection of all registered operators)

### Configuration/Settings Read
- `StyleConfiguration.AllowedOperatorIds` (list of enabled operator IDs per style)
- `StyleConfiguration.IsOperatorAllowed(operatorId)` method (checks if operator is allowed)

## 5. Constraints & Invariants

### Invariants (MUST Always Hold)
1. **Deterministic registration order:** Operators registered in same order every time (MicroAddition → SubdivisionTransform → PhrasePunctuation → PatternSubstitution → StyleIdiom)
2. **Unique operator IDs:** No two operators can have the same `OperatorId`
3. **Total count:** Registry must contain exactly 28 operators after `BuildComplete()`
4. **Immutability after freeze:** Registry becomes read-only after builder completes (no mutations)
5. **Family consistency:** `GetOperatorsByFamily(family)` must return only operators with matching `OperatorFamily`

### Hard Limits
- Operator count: exactly 28 (7+5+7+4+5)
- No duplicate registrations (same operator registered twice)

### Operation Order
1. Create empty registry
2. Register operators in deterministic order (family by family)
3. Freeze registry (make immutable)
4. Use registry for queries (no further modifications)

## 6. Edge Cases to Test

### Boundary Conditions
- Empty style `AllowedOperatorIds` list (should return all operators)
- Style with no operators allowed (empty list return)
- Family with zero operators (should return empty list)
- Unknown operator ID lookup (should return null)
- Unknown family enum value (should return empty list or throw?)

### Error Cases
- Registering same operator twice (duplicate `OperatorId`) — should throw or ignore?
- Registering operator with null `OperatorId` — validation needed?
- Querying registry before freeze — allowed or throw?
- Mutating registry after freeze — should throw
- Null `StyleConfiguration` passed to `GetEnabledOperators` — should throw or return all?

### Combination Scenarios
- Style allows subset of operators from multiple families (cross-family filtering)
- Style allows zero operators from one family (family-specific suppression)
- Operator in multiple families (not possible per design, but validate?)
- All operators disabled by style (valid edge case, return empty)

### Determinism Verification
- Same registration order → same iteration order (test with GetAllOperators)
- Family grouping stable across runs
- Style filtering deterministic (same inputs → same outputs)

## 7. Clarifying Questions

### 1. Registry Mutability and Lifecycle
- **Q:** Can operators be added to the registry after `BuildComplete()` / freeze, or is the registry immutable once built?
- **Rationale:** Affects whether registry needs locking, thread-safety, or copy-on-write semantics.

**Answer 1:** Registry is immutable after freeze. `BuildComplete()` returns a frozen registry. Any attempt to register after freeze throws `InvalidOperationException`. ✅ IMPLEMENTED

### 2. Duplicate Registration Handling
- **Q:** What happens if the same operator (same `OperatorId`) is registered twice? Throw exception, silently ignore, or replace?
- **Rationale:** Defensive programming vs fail-fast behavior.

**Answer 2:** Throw `InvalidOperationException` with clear message including the duplicate operator ID. Duplicate IDs indicate a programming error and should be caught immediately. ✅ IMPLEMENTED

### 3. Unknown Family Enum Handling
- **Q:** If `GetOperatorsByFamily(unknownFamily)` is called with an enum value not in the registry, return empty list or throw?
- **Rationale:** Forward compatibility if new families added later.

**Answer 3:** Return empty list (defensive via `Array.Empty<IDrumOperator>()`). Future-proof for enum extensions without breaking existing code. ✅ IMPLEMENTED

### 4. Style Configuration Edge Cases
- **Q:** If `StyleConfiguration` is null or has null `AllowedOperatorIds`, should `GetEnabledOperators` return all operators or throw?
- **Rationale:** Null-safety vs explicit contracts.

**Answer 4:** `ArgumentNullException` for null style. Empty `AllowedOperatorIds` list means all operators allowed (per StyleConfiguration design from Story 1.4). ✅ IMPLEMENTED

### 5. Registration Order Stability
- **Q:** Must the order of operators within a family be deterministic, or only the family order?
- **Rationale:** Affects iteration order for selection engines, diagnostics.

**Answer 5:** Order within family matches the order operators are registered in builder methods. Uses `List<T>` internally (not `HashSet`) to preserve insertion order. ✅ IMPLEMENTED

### 6. Thread Safety Requirements
- **Q:** Is the registry expected to be accessed from multiple threads, or single-threaded usage only?
- **Rationale:** Affects whether collections need to be thread-safe.

**Answer 6:** Single-threaded usage expected (generator is single-threaded). No locking needed, but immutability after freeze provides thread-safety as side effect. ✅ IMPLEMENTED

### 7. Operator ID Format Constraints
- **Q:** Are there any constraints on `OperatorId` format (e.g., must start with "Drum", must be PascalCase)?
- **Rationale:** Validation and diagnostics clarity.

**Answer 7:** No format constraints enforced by registry. Operators are responsible for their own ID format (convention: "Drum{OperatorName}" per existing implementations). ✅ NO VALIDATION NEEDED

### 8. Builder Pattern vs Factory Method
- **Q:** Should `DrumOperatorRegistryBuilder` be a static class with factory methods, or an instance-based builder?
- **Rationale:** Affects testability and builder reuse.

**Answer 8:** Static class with `BuildComplete()` factory method (returns frozen registry). Simpler usage, no builder instance management needed. ✅ IMPLEMENTED

### 9. Filtering Performance Expectations
- **Q:** Are there performance requirements for filtering (e.g., must be O(1) lookup, or O(n) scan acceptable)?
- **Rationale:** Affects internal data structure choice (dictionary vs list).

**Answer 9:** O(n) scan acceptable. Operator count is small (28), and filtering happens once per bar. Uses simple `List<T>` and LINQ for clarity. ✅ IMPLEMENTED

### 10. Operator Count Validation Timing
- **Q:** Should operator count (28) be validated at registration time, freeze time, or first query?
- **Rationale:** Fail-fast vs deferred validation.

**Answer 10:** Validate at freeze time in `BuildComplete()`. Throw if count != 28 with diagnostic message listing all registered operators by family. ⚠️ NEEDS IMPLEMENTATION

## 8. Test Scenario Ideas

### Registry Construction and Validation Tests
- `Registry_BuildComplete_ContainsExactly28Operators` — verify total count
- `Registry_BuildComplete_IsImmutable` — verify frozen state (mutations throw)
- `Registry_RegisterDuplicateOperatorId_Throws` — duplicate ID detection
- `Registry_GetAllOperators_ReturnsInRegistrationOrder` — deterministic iteration

### Family-Based Filtering Tests
- `Registry_GetOperatorsByFamily_MicroAddition_Returns7Operators` — family count
- `Registry_GetOperatorsByFamily_SubdivisionTransform_Returns5Operators`
- `Registry_GetOperatorsByFamily_PhrasePunctuation_Returns7Operators`
- `Registry_GetOperatorsByFamily_PatternSubstitution_Returns4Operators`
- `Registry_GetOperatorsByFamily_StyleIdiom_Returns5Operators`
- `Registry_GetOperatorsByFamily_UnknownFamily_ReturnsEmpty` — defensive behavior
- `Registry_GetOperatorsByFamily_OnlyReturnsMatchingFamily` — no leakage

### ID-Based Lookup Tests
- `Registry_GetOperatorById_KnownId_ReturnsOperator` — happy path
- `Registry_GetOperatorById_UnknownId_ReturnsNull` — not found case
- `Registry_GetOperatorById_NullId_Throws` — validation
- `Registry_GetOperatorById_CaseSensitive` — verify exact match required

### Style-Based Filtering Tests
- `Registry_GetEnabledOperators_EmptyAllowList_ReturnsAll` — allow-all semantics
- `Registry_GetEnabledOperators_SubsetAllowed_ReturnsOnlyAllowed` — filtering works
- `Registry_GetEnabledOperators_NoneAllowed_ReturnsEmpty` — all disabled
- `Registry_GetEnabledOperators_CrossFamilyFilter_Works` — mixed families
- `Registry_GetEnabledOperators_NullStyle_Throws` — null safety
- `Registry_GetEnabledOperators_Deterministic` — same inputs → same outputs

### Integration Tests
- `Registry_AllOperatorsHaveUniqueIds` — no collisions across families
- `Registry_AllOperatorsHaveValidFamily` — enum values valid
- `Registry_OperatorCountMatchesDocumentation` — 7+5+7+4+5=28
- `Registry_RegistrationOrderMatchesDocumentation` — family order correct

### Determinism Tests
- `Registry_BuildComplete_TwiceWithSameOperators_Identical` — deterministic build
- `Registry_GetAllOperators_OrderStable` — iteration order stable
- `Registry_FilteringDeterministic_SameSeed` — same filter inputs → same outputs

---

// End of pre-analysis for Story 3.6
