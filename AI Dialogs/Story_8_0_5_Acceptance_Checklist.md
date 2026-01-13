# Story 8.0.5 — Acceptance Criteria Checklist

## Story: Create `KeysModeRealizer` to apply mode to onset selection + duration

**Status:** ? **ALL CRITERIA MET**

---

## Acceptance Criteria from Plan

### ? 1. Create `KeysRealizationResult` model
**Requirement:** Result record with selected onsets, duration multiplier, and split voicing index.

**Implementation:**
- **File:** `Generator\Keys\KeysModeRealizer.cs` (lines 7-21)
- **Fields:**
  - `SelectedOnsets`: IReadOnlyList<decimal> ?
  - `DurationMultiplier`: double, default 1.0 ?
  - `SplitUpperOnsetIndex`: int, default -1 ?
- **Immutability:** Sealed record ?

**Verification:** Compile-time type check + tests

---

### ? 2. Implement `KeysModeRealizer.Realize()` method
**Requirement:** Main entry point with parameters: mode, padsOnsets, densityMultiplier, bar, seed.

**Implementation:**
- **File:** `Generator\Keys\KeysModeRealizer.cs` (lines 29-56)
- **Signature:** ?
  ```csharp
  public static KeysRealizationResult Realize(
      KeysRoleMode mode,
      IReadOnlyList<decimal> padsOnsets,
      double densityMultiplier,
      int bar,
      int seed)
  ```
- **Null/empty handling:** Lines 48-52 ?
- **Density clamping:** Line 54 `Math.Clamp(densityMultiplier, 0.5, 2.0)` ?
- **Mode dispatch:** Lines 56-62 (switch expression) ?

**Verification:**
- `Test_NullOnsets_ReturnsEmptyResult` ?
- `Test_EmptyOnsets_ReturnsEmptyResult` ?
- `Test_DensityClampedToValidRange` ?

---

### ? 3. Implement `RealizeSustain()` mode
**Requirement:**
- Only first onset of bar
- Long duration (2x slot)

**Implementation:**
- **File:** `Generator\Keys\KeysModeRealizer.cs` (lines 60-68)
- **Onset selection:** `new[] { padsOnsets[0] }` (first only) ?
- **Duration:** `DurationMultiplier = 2.0` ?

**Verification:**
- `Test_Sustain_ReturnsOnlyFirstOnset` ?
- `Test_Sustain_DurationMultiplier` ?

---

### ? 4. Implement `RealizePulse()` mode
**Requirement:**
- Selected strong beats
- Normal duration
- Always include beat 1 if present
- Deterministic selection based on seed

**Implementation:**
- **File:** `Generator\Keys\KeysModeRealizer.cs` (lines 72-103)
- **Strong beat preference:** Lines 77-78 (filters integer beats) ?
- **Beat 1 inclusion:** Lines 84-88 ?
- **Target count:** Line 82 (50% of onsets * density) ?
- **Duration:** `DurationMultiplier = 1.0` ?
- **Seed-based variation:** Lines 94-99 (hash-based offbeat selection) ?

**Verification:**
- `Test_Pulse_PrefersStrongBeats` ?
- `Test_Pulse_IncludesBeat1` ?
- `Test_Pulse_DurationMultiplier` ?
- `Test_Pulse_DensityAffectsOnsetCount` ?

---

### ? 5. Implement `RealizeRhythmic()` mode
**Requirement:**
- Use most/all onsets
- Shorter duration (0.7x)
- Density affects count

**Implementation:**
- **File:** `Generator\Keys\KeysModeRealizer.cs` (lines 107-117)
- **Onset count:** Lines 110-111 (up to 130% of count * density, capped) ?
- **Duration:** `DurationMultiplier = 0.7` ?
- **Density responsive:** Uses `Math.Min(densityMultiplier, 1.3)` ?

**Verification:**
- `Test_Rhythmic_UsesMostOnsets` ?
- `Test_Rhythmic_DurationMultiplier` ?
- `Test_Rhythmic_DensityAffectsOnsetCount` ?

---

### ? 6. Implement `RealizeSplitVoicing()` mode
**Requirement:**
- Two onsets (low voicing first, upper voicing second)
- Mark which onset is upper voicing
- Duration multiplier 1.2
- Fallback gracefully if <2 onsets

**Implementation:**
- **File:** `Generator\Keys\KeysModeRealizer.cs` (lines 121-143)
- **Fallback:** Lines 127-133 (if count < 2) ?
- **Onset selection:** Lines 136-137 (first + middle) ?
- **Split marking:** Line 142 `SplitUpperOnsetIndex = 1` ?
- **Duration:** Line 141 `DurationMultiplier = 1.2` ?

**Verification:**
- `Test_SplitVoicing_ReturnsTwoOnsets` ?
- `Test_SplitVoicing_MarksSplitUpperIndex` ?
- `Test_SplitVoicing_SelectsFirstAndMiddle` ?
- `Test_SplitVoicing_DurationMultiplier` ?
- `Test_SplitVoicing_FallbackWithOneOnset` ?

---

### ? 7. Duration multipliers bounded [0.5..2.0]
**Requirement:** All mode duration multipliers must be in range [0.5..2.0].

**Implementation:**
- **Sustain:** 2.0 (upper bound) ?
- **Pulse:** 1.0 ?
- **Rhythmic:** 0.7 ?
- **SplitVoicing:** 1.2 ?
- **All within [0.5..2.0]** ?

**Verification:**
- `Test_DurationMultipliersAreBounded` ?

---

### ? 8. Deterministic behavior
**Requirement:** Same inputs ? same outputs; seed affects variation.

**Implementation:**
- **Deterministic by design:** All methods use deterministic logic ?
- **Seed usage:** Pulse mode hash-based offbeat selection (line 95) ?
- **No random behavior:** No `Random` class used ?

**Verification:**
- `Test_Determinism_SameInputs_SameOutput` ?
- `Test_DifferentSeeds_ProduceDifferentResults` ?

---

### ? 9. Output onsets are valid subset of input
**Requirement:** Selected onsets must all exist in input list (no new onsets).

**Implementation:**
- **All modes filter from input:** No onset creation ?
- **Sustain:** Takes first ?
- **Pulse:** Filters strong beats + selects offbeats ?
- **Rhythmic:** Takes first N ?
- **SplitVoicing:** Takes specific indices ?

**Verification:**
- `Test_OutputOnsetsAreSubsetOfInput` ?

---

### ? 10. Selected onsets are sorted
**Requirement:** Output onset list must be in ascending order.

**Implementation:**
- **Pulse:** Line 101 `.OrderBy(o => o).ToList()` ?
- **Rhythmic:** Input already sorted, Take preserves order ?
- **SplitVoicing:** Two-element array constructed in order ?
- **Sustain:** Single element (trivially sorted) ?

**Verification:**
- `Test_SelectedOnsetsAreSorted` ?

---

## Additional Invariants Verified

### ? Edge case handling
- **Null onsets:** Returns empty result ?
- **Empty onsets:** Returns empty result ?
- **Single onset:** All modes handle gracefully ?
- **Invalid density:** Clamped to valid range ?

**Verification:**
- `Test_NullOnsets_ReturnsEmptyResult` ?
- `Test_EmptyOnsets_ReturnsEmptyResult` ?
- `Test_SingleOnset_HandlesAllModes` ?
- `Test_DensityClampedToValidRange` ?

### ? Mode differentiation
- **Different modes produce different results:** ?
  - Onset counts differ: Sustain < Pulse < Rhythmic
  - Duration multipliers differ
  - Onset selections differ

**Verification:**
- `Test_DifferentModes_ProduceDifferentOnsetCounts` ?

---

## Test Coverage Summary

| Category | Tests | Status |
|----------|-------|--------|
| **Determinism** | 2 | ? Pass |
| **Edge Cases** | 4 | ? Pass |
| **Sustain Mode** | 2 | ? Pass |
| **Pulse Mode** | 4 | ? Pass |
| **Rhythmic Mode** | 3 | ? Pass |
| **SplitVoicing Mode** | 5 | ? Pass |
| **Invariants** | 4 | ? Pass |
| **Total** | **24** | **? All Pass** |

---

## Build & Compilation Status

- ? **Build successful** (no errors)
- ? **No warnings**
- ? **All tests compile**
- ? **All tests pass**

---

## Integration Readiness

### Ready for Story 8.0.6
- ? Public API stable and documented
- ? Result type ready for consumption
- ? Determinism guarantees match system requirements
- ? Edge cases handled
- ? Performance characteristics documented

### Contracts Established
1. **Input contract:** Mode, onsets, density [0.5..2.0], bar (1-based), seed
2. **Output contract:** Valid subset of onsets, sorted, duration in [0.5..2.0]
3. **Determinism contract:** Same inputs ? same outputs
4. **Safety contract:** No crashes on null/empty/invalid inputs

---

## Compliance with Stage 8.0 Goals

### ? Audible Differentiation
- Sustain ? 1 onset, 2x duration (very sparse, long)
- Pulse ? 2-4 onsets, 1x duration (moderate)
- Rhythmic ? 6-8 onsets, 0.7x duration (busy, choppy)
- SplitVoicing ? 2 onsets, 1.2x duration (special effect)

### ? Seed Sensitivity
- Pulse mode uses seed for offbeat selection variation
- Different seeds produce different results
- Verified by test

### ? Determinism Preserved
- All behavior deterministic
- No uncontrolled randomness
- Verified by multiple tests

---

## Final Checklist

- [x] All acceptance criteria met
- [x] All tests passing
- [x] Build successful
- [x] Documentation complete
- [x] Code comments added
- [x] Edge cases handled
- [x] Determinism verified
- [x] Performance acceptable
- [x] Integration contracts defined
- [x] Ready for Story 8.0.6

---

**Story 8.0.5 Status: ? COMPLETE AND APPROVED**

All acceptance criteria have been met. The `KeysModeRealizer` is ready for integration in Story 8.0.6.
