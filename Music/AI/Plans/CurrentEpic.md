# Epic: Remove operator context + CanApply gate from drum operator pipeline

## Goal
Remove `DrummerContext` and `GeneratorContext` entirely. Simplify the drum operator pipeline so operators are always eligible (no `CanApply()`), and operator generation/scoring uses direct inputs (e.g., `Bar`) rather than a shared context type.

## Non-goals
- No new classes.
- No new methods.
- No unit tests.
- No behavior “enhancements” beyond keeping existing generation output logic intact where practical.

## Constraints / principles
- Minimize the total number of types and public surface area.
- Prefer deletion over refactoring.
- Prefer mechanical/consistent signature changes to avoid regressions.
- Breaking changes are acceptable if they reduce work and complexity.

## Acceptance criteria
- Solution builds successfully.
- `GeneratorContext` type deleted.
- `DrummerContext` type deleted.
- `IMusicalOperator` no longer exposes `CanApply()`.
- Drum operators in registry compile and run with no context casting.

---

## Story 1 (breaking): Delete `GeneratorContext` and remove all references
### Why
`GeneratorContext` only exists to carry seed/stream-key but RNG streams already provide isolation by purpose.

### Scope
- Delete `Music\Generator\Core\GeneratorContext.cs`.
- Replace any remaining references to `GeneratorContext` with concrete parameters required by each call site (likely `Bar` + seed values already present in call graph).

### Deliverables
- `GeneratorContext.cs` removed from project.
- Build succeeds.

---

## Story 2 (breaking): Remove `CanApply()` from operator contracts and execution pipeline
### Why
All operators are deemed applicable; gating adds complexity and requires context types.

### Scope
- Update `Music\Generator\Core\IMusicalOperator.cs`:
  - Remove `CanApply(...)` from interface.
  - Ensure only generation/scoring methods remain.
- Update `Music\Generator\Drums\Operators\IDrumOperator.cs` to match.
- Update any code that previously called `CanApply(...)` (candidate sources, selection engines, applicators) to stop calling it.

### Deliverables
- No `CanApply` methods required/implemented.
- Registry and selection pipeline compile.

---

## Story 3 (breaking): Remove `DrummerContext` and push required inputs into generation/scoring methods
### Why
`DrummerContext` is a wrapper around bar + a few cross-bar fields; with no `CanApply` and simplified RNG, it is unnecessary.

### Scope
- Delete `Music\Generator\Drums\Context\DrummerContext.cs`.
- Update `Music\Generator\Drums\Context\DrummerContextBuilder.cs`:
  - Remove builder output type `DrummerContext` usage.
  - If the builder becomes unused, delete it (preferred).
- Update `DrumOperatorBase` and all concrete drum operators:
  - Replace method signatures that accept a context with minimal direct inputs.
  - Preferred parameter for bar-derived decisions: `Bar bar`.
  - Keep existing candidate logic intact; replace `context.Bar` usage with `bar`.
  - Replace `context.Seed` usage by threading seed from existing call site(s); if seed is not needed, remove it from signatures.

### Deliverables
- `DrummerContext` deleted.
- Operator compilation restored with new signatures.

---

## Story 4 (fixing): Simplify candidate generation entrypoints to match new operator signatures
### Why
Once contexts are removed, the entrypoints that enumerate operators must pass the new minimal inputs.

### Scope
- Update `Music\Generator\Drums\Operators\Candidates\DrummerOperatorCandidates.cs`:
  - Stop constructing/passing `DrummerContext`.
  - Pass `Bar` (and seed if required) to each operator.
- Update `Music\Generator\Drums\Operators\DrumOperatorApplicator.cs` (and any similar pipeline code):
  - Align to the new operator signatures.
- Update any downstream mapping/scoring that depended on context.

### Deliverables
- Drum generation pipeline compiles end-to-end.
- Build succeeds.

---

## Story 5 (fixing): Remove dead code and unused operator infrastructure caused by contract changes
### Why
After context + CanApply removal, there will be leftover overloads, adapters, and unused helpers.

### Scope
- Delete (or reduce) obsolete overloads in `DrumOperatorBase` and operators:
  - Remove any ‘context-based’ overloads.
- Remove unused context-related code paths, casts, and comments.
- Remove obsolete tests if they block compilation (no new tests added).

### Deliverables
- No references to `DrummerContext`, `GeneratorContext`, or `CanApply` in production code.
- Build succeeds.
