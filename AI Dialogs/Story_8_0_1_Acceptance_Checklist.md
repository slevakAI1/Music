# Story 8.0.1 Acceptance Criteria Checklist

## Story 8.0.1 — Create `CompBehavior` enum and deterministic selector

**Intent:** Define audibly-distinct comp behaviors that energy/tension/section can choose from.

---

## ? Acceptance Criteria (from Stage_8_0_Audibility_MiniPlan.md)

### 1. ? CompBehavior enum created
**Location:** `Generator\Guitar\CompBehavior.cs` lines 10-45

**Required behaviors:**
- ? SparseAnchors (lines 16-18)
- ? Standard (lines 21-24)
- ? Anticipate (lines 27-30)
- ? SyncopatedChop (lines 33-36)
- ? DrivingFull (lines 39-42)

**Verification:** All 5 behaviors present with appropriate documentation.

---

### 2. ? CompBehaviorSelector class created
**Location:** `Generator\Guitar\CompBehavior.cs` lines 47-143

**Required method:**
- ? `SelectBehavior()` with 6 parameters (lines 54-70)

**Parameters:**
1. ? `MusicConstants.eSectionType sectionType`
2. ? `int absoluteSectionIndex`
3. ? `int barIndexWithinSection`
4. ? `double energy`
5. ? `double busyProbability`
6. ? `int seed`

**Returns:** ? `CompBehavior`

---

### 3. ? Deterministic selection logic
**Location:** `Generator\Guitar\CompBehavior.cs` lines 72-105

**Implementation details:**
- ? Input clamping (lines 73-74)
- ? Activity score calculation (line 77): `(energy * 0.6) + (busyProbability * 0.4)`
- ? Section-type specific switch (lines 80-88)
- ? Per-bar variation logic (lines 91-100)
- ? Hash-based deterministic variation (line 94)

**Verification:** No random number generation, only deterministic computations and hash-based decisions.

---

### 4. ? Section-specific behavior mappings
**Required mappings:**
- ? Intro (line 81)
- ? Verse (line 82, helper method lines 107-116)
- ? Chorus (line 83, helper method lines 118-127)
- ? Bridge (line 84)
- ? Outro (line 85)
- ? Solo (line 86)
- ? Default fallback (line 87)

**Verification:** All section types handled with appropriate behavior selection.

---

### 5. ? Variation logic
**Location:** `Generator\Guitar\CompBehavior.cs` lines 91-100, 129-143

**Required behavior:**
- ? Only applies when `barIndexWithinSection > 0 && barIndexWithinSection % 4 == 0` (line 93)
- ? 30% chance based on hash (line 95)
- ? Deterministic hash: `HashCode.Combine(seed, absoluteSectionIndex, barIndexWithinSection)` (line 94)
- ? Upgrade/downgrade based on hash parity (line 132, lines 134-141)

**Verification:** Variation is bounded, deterministic, and timing-controlled.

---

### 6. ? AI-facing documentation
**Location:** Lines 1-3

**Requirements from copilot-instructions.md:**
- ? Each comment line ? 140 characters
- ? Only `//` comments (no XML docs, no regions)
- ? Compact key:value format
- ? Documents: purpose, invariants, change guidance

**Verification:**
```
// AI: purpose=Deterministic selection of comp playing behavior based on energy/tension/section.
// AI: invariants=Selection is deterministic by (sectionType, absoluteSectionIndex, barIndex, energy, busyProbability, seed).
// AI: change=Add new behaviors by extending enum and updating SelectBehavior logic.
```

---

### 7. ? Test coverage
**Location:** `Generator\Guitar\CompBehaviorTests.cs`

**Required tests:**
- ? Determinism test (lines 26-45)
- ? Different sections produce different behaviors (lines 50-69)
- ? Seed affects variation (lines 74-92)

**Additional comprehensive tests:**
- ? Energy affects behavior (lines 97-121)
- ? BusyProbability affects behavior (lines 126-144)
- ? Verse behavior mapping (lines 149-177)
- ? Chorus behavior mapping (lines 182-210)
- ? Behavior variation timing (lines 215-243)
- ? ApplyVariation logic (lines 248-279)
- ? Edge case: zero energy (lines 300-313)
- ? Edge case: max energy (lines 318-330)
- ? Edge case: first bar no variation (lines 335-349)

**Total tests:** 12 (exceeds minimum requirement of 3)

---

## ? Code Quality Checks

### Namespace consistency
- ? Uses `Music.Generator` namespace (matches GuitarTrackGenerator.cs)

### Type safety
- ? All parameters strongly typed
- ? Return type is enum (no magic numbers or strings)

### Input validation
- ? Energy clamped to [0..1] (line 73)
- ? BusyProbability clamped to [0..1] (line 74)

### Maintainability
- ? Private helper methods for section-specific logic (lines 107-127)
- ? Clear separation of concerns (base selection vs variation)
- ? Switch expressions used appropriately

---

## ? Build Verification

**Status:** Build successful ?

**No errors:** ?
**No warnings:** ?

---

## ? Integration Readiness

### Ready for Story 8.0.2 (CompBehaviorRealizer):
- ? Public enum can be consumed
- ? Public static method can be called
- ? Return type is strongly typed enum
- ? All parameters available in generation context

### No breaking changes:
- ? New files only, no modifications to existing code
- ? No changes to public APIs
- ? No changes to existing data structures

---

## ? Compliance with Hard Rules

### HARD RULE A: Minimum changes
- ? Only 2 new files created
- ? No refactoring of existing code
- ? No changes to unrelated functionality

### HARD RULE B: Acceptance criteria verification
? All criteria met and verified with evidence:
1. ? Enum created with 5 behaviors
2. ? Selector class created
3. ? Deterministic selection implemented
4. ? Section-specific mappings implemented
5. ? Variation logic implemented
6. ? AI documentation compliant
7. ? Test coverage exceeds requirements

---

## Summary

**Story 8.0.1 Status:** ? **COMPLETE**

**Files created:** 2
1. `Generator\Guitar\CompBehavior.cs` (145 lines)
2. `Generator\Guitar\CompBehaviorTests.cs` (353 lines)

**Documentation created:** 1
1. `AI Dialogs\Story_8_0_1_Implementation_Summary.md` (summary and verification)

**Tests:** 12 comprehensive tests, all passing
**Build:** ? Successful
**Integration:** ? Ready for Story 8.0.2

**All acceptance criteria met:** ?
