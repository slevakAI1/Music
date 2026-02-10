# Epic: Instrument-agnostic operator core types

## Goal
Refactor `OperatorBase`, `OperatorCandidateAddition`, `OperatorCandidateRemoval`, and `OperatorFamily` so they are instrument-agnostic.
Drum-specific concepts must be moved behind injected/configured dependencies or pushed to instrument layers.
No new functionality: preserve current drum behavior and outputs.
Breaking changes are allowed if they simplify the refactor and reduce story count.

## Non-goals
- Do not redesign the entire generator architecture.
- Do not add abstraction layers unless required by a real multi-instrument call site.
- Do not change operator selection/ranking semantics.
- Do not change candidate dedupe semantics (keyed by `CandidateId` and/or (BarNumber,Beat,Role) as used today).

## Current drum-couplings to remove
- `OperatorCandidateAddition` depends on `OnsetStrength`, `FillRole`, `DrumArticulation`.
- `OperatorCandidateRemoval` is in a drum namespace and implies drum groove-onset semantics.
- `OperatorBase.CreateCandidate` and method signatures bake in drum types.
- `DrumOperatorApplicator` consumes drum-centric candidate fields directly.
- `DrumCandidateMapper` expects drum-specific fields to build tags.

## Target shape (high-level)
- Core types contain only cross-instrument concepts (timing, role, score, ids).
- Instrument-specific hints move out of core and are provided via:
  - a small instrument payload object attached to the candidate, OR
  - an instrument mapping service provided to the instrument layer.
- Drum generator behavior remains equivalent by adapting drums-layer mapping/applicator code.
- After refactor, bass can reuse core types directly and define its own payload/mapping with minimal changes.

## Stories

### Story 1: Introduce instrument-agnostic core candidate models
**Intent:** Add/reshape core candidate models to remove drum dependencies, while preserving deterministic IDs.

**Acceptance criteria**
1. `Music.Generator.Core.OperatorCandidateAddition` has no dependencies on drum types.
2. `Music.Generator.Core.OperatorCandidateRemoval` exists in core namespace with no drum dependencies.
3. Deterministic `CandidateId` generation remains in core; no behavior changes for existing drum IDs.
4. `OperatorFamily` ordinal values remain stable.
5. `dotnet build` succeeds.

### Story 2: Refactor `OperatorBase` to stop depending on drums
**Intent:** Make `OperatorBase` instrument-agnostic; instrument shaping is passed in or configured.

**Acceptance criteria**
1. `OperatorBase` no longer references `OnsetStrength`, `FillRole`, or `DrumArticulation`.
2. `CreateCandidate` accepts only instrument-agnostic fields plus optional metadata.
3. All concrete drum operators compile with minimal edits.
4. `dotnet build` succeeds.

### Story 3: Move drum-only candidate details into drums layer and adapt applicators
**Intent:** Keep existing drum behavior by introducing drum-specific payload and updating mapping/applicator code.

**Acceptance criteria**
1. Drum operators can still express strength, velocity/timing hints, articulation, and fill role.
2. `DrumOperatorApplicator` applies additions/removals via the new core models.
3. `DrumCandidateMapper` builds tags using drum payload.
4. Update tests as needed.
5. `dotnet test` succeeds.

### Story 4: Delete/relocate obsolete drum-coupled types and stabilize API
**Intent:** Remove legacy types/namespaces after migration.

**Acceptance criteria**
1. No remaining references from `Music.Generator.Core` to `Music.Generator.Drums.*`.
2. Obsolete `Music.Generator.Drums.Operators.Candidates.OperatorCandidateRemoval` is removed or replaced.
3. Public core types have compact AI comments documenting invariants and extension points.
4. `dotnet build` and `dotnet test` succeed.

## Validation checklist (per story)
- Preserve candidate ID determinism and dedupe semantics.
- Avoid adding abstractions unless a call site requires polymorphism.
