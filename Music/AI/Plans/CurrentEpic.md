# Epic: Disconnect `StyleConfiguration` + `StyleConfigurationLibrary` from drum phrase generation

## Goal
Remove all code references to `Music.Generator.Core.StyleConfiguration` and `Music.Generator.Core.StyleConfigurationLibrary`.
Keep drum phrase generation intact by basing everything on existing groove preset anchors + operators.
Assume all operators are allowed (no style filtering/weights/caps).

## Non-goals
- No unit test updates in this epic.
- No replacement style system.
- No functional enhancements unrelated to dereferencing.

## Current state (to validate)
- `Generator.Generate(songContext, drummerStyle, ...)` routes to `DrumPhraseGenerator` when style is provided.
- `DrumPhraseGenerator` constructs `DrummerCandidateSource`, which currently depends on `StyleConfiguration`.
- `DrummerCandidateSource` uses style config for filtering and/or weighting.

## Target state
- Drum phrase generation uses `songContext.GroovePresetDefinition` anchors + operator registry.
- Candidate generation enables all operators.
- `StyleConfiguration` + `StyleConfigurationLibrary` remain but have zero references from production code.

---

## Story 1: Remove style parameter from public drum generation entry points (Breaking)  (COMPLETED)
### Intent
Stop passing `StyleConfiguration` through public drum generation APIs.

### Changes
- Modify `Music.Generator.Generator.Generate(SongContext, StyleConfiguration?, int)`:
  - Remove the `StyleConfiguration?` parameter (or replace with a non-style primitive).
  - Route operator-based generation without any style object.
- Remove/adjust overloads whose purpose is only style-based routing.

### Breaking change notes
- Breaks callers that pass `StyleConfiguration`.
- Fixed by providing style-free routing in Story 3.

### Acceptance criteria
- No production API surface requires `StyleConfiguration`.
- Solution builds.

---

## Story 2: Make `DrummerCandidateSource` style-free (Breaking)    (COMPLETED)
### Intent
Eliminate `StyleConfiguration` dependency inside candidate generation.

### Changes
- Update `Music.Generator.Drums.Selection.Candidates.DrummerCandidateSource`:
  - Remove `_styleConfig`.
  - Change constructor to not accept `StyleConfiguration`.
  - Replace enabled-operator logic with “all operators enabled”.
  - If weights/caps were used: use neutral/default behavior (no per-style multipliers).

### Breaking change notes
- Breaks existing constructors call sites.
- Updated in Story 3.

### Acceptance criteria
- `DrummerCandidateSource` has zero references to `StyleConfiguration`.
- Candidate generation still returns groups for existing operators.
- Solution builds.

---

## Story 3: Update `DrumPhraseGenerator` to build candidate source without style (Fix break)   (COMPLETED)
### Intent
Keep orchestration local to `DrumPhraseGenerator` without style config.

### Changes
- Update `Music.Generator.Drums.Generation.DrumPhraseGenerator`:
  - Remove the style-based constructor.
  - Build `DrumOperatorRegistry` + `DrummerCandidateSource` using the new style-free constructor.
  - Keep anchor extraction from `songContext.GroovePresetDefinition`.

### Acceptance criteria
- `DrumPhraseGenerator` has zero references to `StyleConfiguration`.
- `Generator` can call it without style.
- Solution builds.

---

## Story 4: Remove remaining references to `StyleConfigurationLibrary` in production code  
### Intent
Ensure `StyleConfigurationLibrary` is fully dereferenced from non-test code.

### Changes
- Search non-test projects for `StyleConfigurationLibrary` usage.
- Replace any usage with existing genre/groove selection mechanisms.
- Keep library code intact but unused.

### Acceptance criteria
- No non-test project references `StyleConfigurationLibrary`.
- Solution builds.

---

## Story 5: Verification + cleanup (Non-breaking)
### Intent
Confirm epic goal is met.

### Changes
- Repo-wide search confirms zero references (production) to:
  - `StyleConfiguration`
  - `StyleConfigurationLibrary`
- Remove unused `using` directives.
- Run `dotnet build`.

### Acceptance criteria
- `dotnet build` succeeds.
- Production code has zero references to both types.
