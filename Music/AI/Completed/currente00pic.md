# Epic: Bass phrase generator structure parity with drums

## Goal
Make `Music.Generator.Bass.Generation.BassPhraseGenerator` and its dependencies mirror the structure and call-flow of
`Music.Generator.Drums.Generation.DrumPhraseGenerator`, but for bass.
Use the instrument-agnostic operator core types (`OperatorBase`, `OperatorCandidateAddition`, `OperatorCandidateRemoval`,
`OperatorFamily`, `IOperatorCandidateInstrumentAdapter`).

## Non-goals
- Do not add any bass operators in this epic.
- Do not change operator selection/dedupe semantics.
- Do not add new architectural layers.
- Do not add new generation features beyond what the drum phrase generator already does.

## Constraints / expected end state
- Bass walk-through starting at `BassPhraseGenerator.Generate(...)` matches drums flow: validate → anchors → apply operators → MIDI.
- Bass operator registry is present but empty (until a later epic adds operators).
- Naming conventions match drums with drum→bass substitution where appropriate.
- Breaking changes allowed if they reduce drift and simplify parity; each story notes any breaks.

## Stories

### Story 1: Normalize bass generator settings to match drum settings
**Intent:** Make `BassGeneratorSettings` match the shape/behavior of `DrumGeneratorSettings`.

**Acceptance criteria**
1. `BassGeneratorSettings` contains the same members as `DrumGeneratorSettings` (`EnableDiagnostics`, `ActiveRoles`, `DefaultVelocity`, `Default`, `GetActiveRoles()`).
2. Default roles returned by `GetActiveRoles()` are bass-appropriate constants (do not reuse drum-only role names unless already shared).
3. `dotnet build` succeeds.

**Breaking changes**
- Allowed: rename/removal of old bass settings members if they do not match drums structure.

### Story 2: Refactor `BassPhraseGenerator` to mirror `DrumPhraseGenerator`
**Intent:** Make the bass phrase orchestration method signatures and flow match drums.

**Acceptance criteria**
1. `BassPhraseGenerator.Generate(...)` signature matches drum structure, including `maxBars` and `numberOfOperators` parameters.
2. Flow is identical to drums: validate context → compute `totalBars` → select bars → extract anchors → apply operator applicator → convert to `PartTrack`.
3. Bass-specific anchor extraction method exists and mirrors the drum method structure.
4. `dotnet build` succeeds.

**Breaking changes**
- Allowed: change `Generate(...)` signature and any call sites to match drum generator pattern.

### Story 3: Introduce bass operator registry types (empty, deterministic)
**Intent:** Replace the drum registry usage in bass with bass-named equivalents while keeping behavior (empty registry).

**Acceptance criteria**
1. `BassOperatorRegistry` exists (bass namespace) and is the registry type used by bass generation.
2. `BassOperatorRegistryBuilder.BuildComplete()` exists and returns a frozen registry.
3. No bass code references `Music.Generator.Drums.Operators.DrumOperatorRegistry` or `DrumOperatorRegistryBuilder`.
4. Registry contains zero operators by default.
5. `dotnet build` succeeds.

**Breaking changes**
- Allowed: rename/move existing bass registry builder to remove drum references.

### Story 4: Create `BassOperatorApplicator` parity with `DrumOperatorApplicator`
**Intent:** Provide a bass-named applicator with identical algorithm/semantics.

**Acceptance criteria**
1. `BassOperatorApplicator.Apply(...)` exists with the same signature pattern as `DrumOperatorApplicator.Apply(...)`.
2. Dedupe semantics remain keyed by `(BarNumber, Beat, Role)`.
3. Removal behavior uses `OperatorCandidateRemoval` and respects `GrooveOnset` protection flags.
4. `dotnet build` succeeds.

**Breaking changes**
- Allowed: update bass generator call sites to use `BassOperatorApplicator`.

### Story 5: Replace drum-copied bass operator/candidate stubs with bass-named counterparts
**Intent:** Ensure bass folders contain only bass-named types and compile, even with no operators.

**Acceptance criteria**
1. Any drum-copied bass files that reference drum namespaces are deleted or rewritten to bass equivalents.
2. Bass operator-family folders exist (even empty) matching drums structure (`MicroAddition`, `SubdivisionTransform`, `PhrasePunctuation`, `PatternSubstitution`, `StyleIdiom`, `NoteRemoval`).
3. No operator implementations are added.
4. `dotnet build` succeeds.

**Breaking changes**
- Allowed: delete unused bass stub files if they are not part of the target structure.

### Story 6: Build/test validation sweep
**Intent:** Ensure the refactor is complete and does not regress drums.

**Acceptance criteria**
1. `dotnet build` succeeds.
2. `dotnet test` succeeds.

## Validation checklist (per story)
- Keep core operator types instrument-agnostic.
- Avoid adding new abstractions unless required by an existing call site.
- Keep deterministic ordering where applicable (registry registration and applicator plan shuffle).
