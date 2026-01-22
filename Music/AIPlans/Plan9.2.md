# Story 9.2 — Motif Renderer: Reengineer for Current Codebase

**Story ID:** MUSIC-9.2  (COMPLETED)
**Epic:** Stage 9 — Motif Placement and Rendering  
**Status:** Ready  
**Priority:** High  
**Estimate:** 2-3 days  
**Dependencies:** Stage 8 (Motif Data - Complete), Stage G (Groove System - In Progress)

---

## User Story

**As a** music generator  
**I want** to render motif specifications into actual note sequences against harmony and groove context  
**So that** motifs can be placed in songs as concrete musical material

---

## Context

The `MotifRenderer` class was previously implemented but excluded from the project. It has now been re-included and needs to be reengineered to work with:

1. Current `MotifSpec` and `MotifPlacement` types (Stage 8)
2. Current `HarmonyTrack` and `HarmonyPitchContext` infrastructure
3. Current `BarTrack` timing system
4. Current `GroovePresetDefinition` (renamed from `GroovePreset`)
5. Current `PartTrack` output format

**Build errors exist** from including the file—these must be resolved.

---

## Acceptance Criteria

### Core Functionality

- [ ] `MotifRenderer.Render()` compiles without errors
- [ ] Both render overloads work correctly:
  - [ ] Generator-friendly overload: `Render(MotifSpec, MotifPlacement, harmonyContexts, onsetGrid, ...)`
  - [ ] Full overload: `Render(MotifSpec, MotifPlacement, HarmonyTrack, BarTrack, GroovePresetDefinition, ...)`
- [ ] Renders motif rhythm shapes into absolute song ticks correctly
- [ ] Selects pitches based on:
  - [ ] Contour intent (Up, Down, Arch, Flat, ZigZag)
  - [ ] Harmony context (chord tones vs scale tones)
  - [ ] Tone policy (chord tone bias, allow passing tones)
  - [ ] Register constraints (center note ± range)
- [ ] Applies voice-leading smoothing (prefer smaller intervals)
- [ ] Handles variation intensity and transform tags correctly
- [ ] Prevents note overlaps (shortens previous note if needed)
- [ ] Outputs valid `PartTrack` in `SongAbsolute` domain with correct metadata

### Type Compatibility Fixes

- [ ] Fix `GroovePreset` → `GroovePresetDefinition` references
- [ ] Fix or create `OnsetSlot` type if missing (or use alternative)
- [ ] Ensure compatibility with current `HarmonyPitchContext` structure
- [ ] Ensure compatibility with current `BarTrack` API
- [ ] Ensure output `PartTrack` matches current format expectations

### Determinism

- [ ] Same inputs + seed → identical output notes
- [ ] Pitch selection is deterministic (uses seed-based hashing)
- [ ] Velocity and duration calculations are deterministic
- [ ] Output events are sorted by `AbsoluteTimeTicks`

### Edge Cases

- [ ] Handles empty rhythm shapes gracefully (returns empty track)
- [ ] Handles missing harmony contexts (returns empty track)
- [ ] Handles motif placement beyond section bounds (returns empty track)
- [ ] Clamps all MIDI pitches to valid range [21, 108]
- [ ] Clamps all velocities to valid range [40, 127]
- [ ] Handles register ranges where no chord tones exist (transposes by octave)
- [ ] Handles zero-duration bars gracefully

### Testing

- [ ] Unit tests for pitch selection from contour + harmony
- [ ] Unit tests for voice-leading smoothing
- [ ] Unit tests for variation application
- [ ] Unit tests for rhythm mapping to bars
- [ ] Unit tests for determinism (same seed → same output)
- [ ] Unit tests for edge cases (empty, invalid, out-of-range)
- [ ] Integration test: render a test motif against test harmony track

---

## Implementation Tasks

### Task 1: Fix Compilation Errors

**Goal:** Get the file compiling without errors.

**Steps:**
1. Identify all compilation errors in `MotifRenderer.cs`
2. Fix type name mismatches:
   - `GroovePreset` → `GroovePresetDefinition`
   - Check if `OnsetSlot` exists; if not, define it or use alternative
3. Fix method signature mismatches with current types
4. Fix namespace references if needed
5. Verify all referenced types exist in current codebase
6. Run build and confirm zero errors

**Expected Issues:**
- `GroovePreset` type not found → rename to `GroovePresetDefinition`
- `OnsetSlot` type not found → define simple record or use alternative
- Method signature mismatches with current `BarTrack` or `HarmonyTrack`

### Task 2: Validate Core Rendering Logic

**Goal:** Ensure rendering logic is sound for current system.

**Steps:**
1. Review `MapRhythmToBar()` method:
   - Ensure it correctly maps motif-local ticks to song-absolute ticks
   - Verify it handles bars of different lengths (e.g., 3/4 vs 4/4)
2. Review `SelectPitch()` method:
   - Ensure it filters harmony tones to register range BEFORE selection
   - Verify octave transposition logic when no tones in range
   - Ensure voice-leading logic is correct
3. Review `ApplyVariation()` method:
   - Verify variation intensity and transform tags work correctly
   - Ensure variations stay within register bounds
4. Review `PreventOverlaps()` method:
   - Verify it correctly shortens previous notes to prevent overlaps
   - Ensure it maintains a small gap between notes

**Validation:**
- Read through each method carefully
- Check for any obvious logic errors
- Ensure methods match current system conventions

### Task 3: Define OnsetSlot Type (If Missing)

**Goal:** Provide the `OnsetSlot` type needed by the generator-friendly overload.

**Steps:**
1. Check if `OnsetSlot` exists in current codebase
2. If missing, define it:
   ```csharp
   // Music/Generator/Material/OnsetSlot.cs
   public sealed record OnsetSlot(
       long StartTick,
       int DurationTicks,
       bool IsStrongBeat);
   ```
3. Place in appropriate namespace
4. Update `MotifRenderer.cs` to use it
5. Document its purpose (used for simplified rendering without full context)

**Decision:**
- If similar type exists (e.g., `OnsetGrid` slot), adapt to use that instead
- Keep it simple—just tick position, duration, and beat strength flag

### Task 4: Update Method Signatures for Current Types

**Goal:** Ensure method parameters match current system types.

**Steps:**
1. Update render method signatures to use current types:
   - `GroovePresetDefinition` instead of `GroovePreset`
   - Current `HarmonyTrack`, `BarTrack`, `SectionTrack` signatures
2. Verify parameter types match what callers will provide
3. Ensure return type (`PartTrack`) matches current expectations
4. Update internal method calls to match

**Verification:**
- Check current usages of these types in codebase
- Ensure consistency with other generators (drums, bass, comp)

### Task 5: Create Unit Tests

**Goal:** Lock down behavior with deterministic tests.

**Steps:**
1. Create `Music.Tests/Generator/Material/MotifRendererTests.cs`
2. Implement test categories:
   - **Pitch Selection Tests:**
     - Test contour intent (Up, Down, Arch, Flat, ZigZag)
     - Test chord tone selection with high bias
     - Test scale tone selection with passing tones allowed
     - Test register filtering (no tones outside range)
     - Test octave transposition when no tones in range
   - **Voice-Leading Tests:**
     - Test small interval preference
     - Test octave adjustment for large leaps
   - **Variation Tests:**
     - Test OctaveUp transform
     - Test OctaveDown transform
     - Test variation intensity skipping
   - **Rhythm Mapping Tests:**
     - Test motif ticks map to correct song ticks
     - Test duration calculation between onsets
     - Test bar-end truncation
   - **Determinism Tests:**
     - Test same seed → same output
     - Test different seed → different output (if variations exist)
   - **Edge Case Tests:**
     - Test empty rhythm shape
     - Test missing harmony context
     - Test out-of-bounds placement
     - Test MIDI range clamping
3. Use xUnit framework (existing project standard)
4. Follow naming convention: `<Method>_<Condition>_<ExpectedResult>`
5. Use `#region` blocks to organize categories
6. All tests must pass

**Test Data:**
- Use `MotifLibrary` test motifs where available
- Create minimal test fixtures for specific cases
- Use deterministic seeds (e.g., 42)

### Task 6: Integration Test

**Goal:** Verify end-to-end rendering works correctly.

**Steps:**
1. Create integration test that:
   - Loads a test motif from `MotifLibrary`
   - Creates a test `MotifPlacement`
   - Provides test harmony track (simple progression)
   - Calls `Render()` with full parameters
   - Verifies output `PartTrack` has expected properties:
     - Non-empty event list
     - Events sorted by time
     - Pitches within expected register
     - Velocities in valid range
     - Durations positive
2. Assert specific expected behaviors based on motif spec
3. Verify determinism (render twice with same seed)

**Test Fixture:**
- Use existing test song context infrastructure where possible
- Keep test data minimal but realistic

---

## Technical Notes

### Critical AI Comment from Code

From `MotifRenderer.SelectPitch()`:
```
// AI: CRITICAL - Filter chord/scale tones to register range BEFORE selecting
// This prevents bass motifs from selecting chord tones in higher octaves
```

**Implication:** This filtering is essential for correct register behavior. Do NOT remove or bypass it during fixes.

### Type Mapping Reference

| Old Type | Current Type | Location |
|----------|--------------|----------|
| `GroovePreset` | `GroovePresetDefinition` | `Music.Generator.Groove` |
| `OnsetSlot` | **May need to create** | `Music.Generator.Material` |
| `HarmonyPitchContext` | **Exists** | `Music.Song.Harmony` |
| `BarTrack` | **Exists** | `Music.Song.Bar` |
| `PartTrack` | **Exists** | `Music.Song.PartTrack` |

### Design Decisions

1. **Minimal Changes Only:** Do not refactor or optimize beyond fixing compilation and basic correctness
2. **Preserve Existing Logic:** Keep the core rendering algorithms intact unless they're demonstrably broken
3. **Don't Add Features:** Save enhancements for future stories (9.3, 9.4)
4. **Focus on Determinism:** All randomness must use seed-based hashing
5. **Match Current Conventions:** Follow existing code style and patterns in the codebase

---

## Out of Scope

The following are **explicitly NOT part of this story**:

- ❌ Integration with instrument generators (comes in later stories)
- ❌ Accompaniment coordination (Story 9.3)
- ❌ Call-and-response logic (Story 9.3)
- ❌ Motif diagnostics (Story 9.4)
- ❌ Performance optimizations
- ❌ Additional variation operators
- ❌ MIDI export validation (covered elsewhere)
- ❌ Audio rendering integration
- ❌ UI for motif editing

---

## Definition of Done

### Code Complete

- [ ] `MotifRenderer.cs` compiles with zero errors
- [ ] `MotifRenderer.cs` compiles with zero warnings (related to its code)
- [ ] `OnsetSlot.cs` created if needed
- [ ] All methods implement their contracts correctly
- [ ] Code follows existing project conventions
- [ ] AI comments preserved and accurate

### Testing Complete

- [ ] All unit tests passing (minimum 20 tests covering core scenarios)
- [ ] Integration test passing (end-to-end render)
- [ ] Determinism verified (same seed → identical output)
- [ ] Edge cases covered (empty, invalid, boundary conditions)
- [ ] Test coverage ≥ 85% for new/modified code

### Quality Gates

- [ ] **Build succeeds** with no errors
- [ ] **All existing tests still pass** (no regressions)
- [ ] **Code review** completed (if working with team)
- [ ] **AI comments** maintained per coding standards

### Documentation

- [ ] Method XML comments accurate
- [ ] AI comments follow 140-char limit and key:value style
- [ ] Complex algorithms have explanation comments
- [ ] Edge case handling documented

---

## Files to Create/Modify

### New Files

```
Music.Tests/Generator/Material/
  └── MotifRendererTests.cs          (unit + integration tests)

Music/Generator/Material/
  └── OnsetSlot.cs                    (if needed - simple record)
```

### Modified Files

```
Music/Generator/Material/
  └── MotifRenderer.cs                (fix compilation, minimal updates)
```

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Compilation** | Zero errors | Build output |
| **Test coverage** | ≥85% | Code coverage tools |
| **Tests passing** | 100% | Test runner |
| **Determinism** | 100% | Repeated render tests |
| **No regressions** | 100% existing tests pass | CI/CD |

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| **Type mismatches complex** | Medium | High | Start with simple fixes; ask for help if blocked |
| **Logic errors in existing code** | Low | Medium | Read carefully; validate with tests |
| **Missing OnsetSlot type** | Medium | Low | Create simple record; document decision |
| **Breaking existing tests** | Low | High | Run full test suite after changes |
| **Harmony integration issues** | Low | Medium | Test with known harmony tracks first |

---

## Next Steps After Completion

Once this story is complete:

1. **Story 9.3:** Motif integration with accompaniment (call/response, ducking)
2. **Story 9.4:** Motif diagnostics and tracing
3. **Integrate with generators:** Use `MotifRenderer` in instrument agents
4. **Create motif library:** Expand `MotifLibrary` with more patterns
5. **Benchmark loop:** Compare rendered motifs against target characteristics

---

## References

- **NorthStar.md:** Stage 9 goals and requirements
- **MotifRenderer.cs:** Current implementation to fix
- **MotifSpec, MotifPlacement:** Material system types (Stage 8)
- **HarmonyTrack, HarmonyPitchContext:** Harmony infrastructure
- **BarTrack, GroovePresetDefinition:** Timing and groove infrastructure

---

*This story focuses on minimal changes to get the motif renderer functional with the current codebase. Enhancement and integration work is deferred to subsequent stories.*
