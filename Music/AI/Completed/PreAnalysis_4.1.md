# PreAnalysis_4.1 — Define Limb Model

## 1) Story Intent Summary
- What: Define a literal, testable model of a human drummer's limbs and default role→limb mapping so generators can detect impossible simultaneous requirements.
- Why: Prevents generation of physically impossible drum patterns (improves realism, reduces downstream pruning and surprising rejections).
- Who benefits: Generator authors (deterministic feasibility checks), DrummerAgent selection pipeline (candidate filtering), and end-users (more realistic drum output).

## 2) Acceptance Criteria Checklist
1. Create `LimbModel` class and associated elements:
   1. `Limbs` enum: `RightHand`, `LeftHand`, `RightFoot`, `LeftFoot`.
   2. Configurable `RoleLimbMapping` with defaults:
      - RightHand → Hat/Ride
      - LeftHand → Snare
      - RightFoot → Kick
      - LeftFoot → HiHatPedal
   3. `GetRequiredLimb(role) → Limb` accessor.
2. Create `LimbAssignment` record capturing `(Beat, Role, Limb)`.
3. Create `LimbConflictDetector` with:
   1. `DetectConflicts(List<LimbAssignment>) → List<LimbConflict>`.
   2. Definition: conflict = same limb required for overlapping events.
4. Unit tests that detect basic conflicts (examples: two snares on same beat flagged; hat+snare on same beat OK).

Notes: ACs focus on modeling and deterministic detection; they do not mandate remediation strategy (e.g., candidate removal vs. scoring).

## 3) Dependencies & Integration Points
- Depends on completed stories/components:
  - Story 2.1/2.2: `DrummerContext`, `DrumCandidate`, and the operator registry (candidates produced earlier in pipeline).
  - Story 4.x suite (4.2, 4.3) for later sticking rules and filters that will consume this model.
- Code/files likely touched or consumed:
  - `Generator/Agents/Drums/Physicality/*` (new files per AC)
  - `Generator/Agents/Drums/DrummerCandidateSource.cs` (will call conflict detector/physicality filter)
  - `Generator/Agents/Drums/DrumCandidate.cs` (source of Role/Beat info)
  - `Generator/Groove/OnsetGrid` and `BarTrack` for beat/tick math (to determine overlaps)
  - `PhysicalityRules.cs` (configuration will reference LimbModel once added)
- Provides for future stories:
  - Foundational input for `LimbConflictDetector` usage in `PhysicalityFilter` (Story 4.3)
  - Basis for sticking rules and validation in Story 4.2

## 4) Inputs & Outputs
- Inputs (consumed):
  - `DrumCandidate` or other candidate representations containing `Role`, `BarNumber`, `Beat` (or absolute ticks).
  - `BarTrack` / `TimingTrack` or OnsetGrid for precise tick offsets to compute overlaps.
  - `Role`→`Limb` mapping configuration (may be default or style-specific override).
- Outputs (produced):
  - `LimbAssignment` records enumerating required limb usages per candidate/event.
  - `List<LimbConflict>` from `LimbConflictDetector` (each conflict: limb, conflicting assignments, overlap window).
- Configuration read:
  - Default role→limb mapping (RoleLimbMapping)
  - Any role aliases (e.g., ClosedHat vs OpenHat vs Ride mapping to same limb) and style-level overrides if present

## 5) Constraints & Invariants
- Invariants that must always hold:
  - A single physical limb cannot be assigned to two events that overlap in time beyond the allowed minimal reaction gap.
  - Role→Limb mapping must be total for roles used by the drummer agent (every role must map to at least one limb or explicit multi-limb option).
  - Overlap detection uses canonical timing units (ticks) from `MusicConstants.TicksPerQuarterNote` for determinism.
- Hard limits (to clarify):
  - Whether two simultaneous events at exactly the same tick are allowed for different limbs (allowed) and for same limb (conflict).
  - Default minimal reaction gap (ticks) between two uses of same limb — not specified by story; must be clarified for implementation.
- Operation order:
  - Convert candidates → LimbAssignments → run `DetectConflicts` → produce conflicts list before any physicality-based pruning.

## 6) Edge Cases to Test
- Simultaneous events at same beat/tick:
  - Same limb required twice at exact tick ⇒ conflict.
  - Different limbs (snare + hat) at exact tick ⇒ no conflict.
- Near-simultaneous events within sub-beat resolution (e.g., 1.000 vs 1.002 beats when represented in ticks): verify overlap math.
- Roles that map to multiple possible limbs (e.g., toms could be played by either hand) — how mapping should express alternatives.
- Missing or unknown role in mapping → define expected behavior (default limb? treat as conflict? fail fast?).
- Rounding or grid mismatch between candidate beat representation and BarTrack tick boundaries.
- Empty candidate list → no conflicts returned.
- Large numbers of assignments in a single bar (stress): performance / result correctness.
- Double-pedal/kick scenarios: RightFoot vs LeftFoot when `AllowDoublePedal` present in `PhysicalityRules`.

## 7) Clarifying Questions

1. What is the canonical time unit for overlap detection: candidate-provided beat (decimal) or absolute ticks? Which should be authoritative?
   **Answer:** Use decimal beat within bar as the canonical unit for `LimbAssignment`. This matches `DrumCandidate.Beat` (decimal, 1-based) and avoids requiring BarTrack/tick conversion at this layer. Conflict detection compares `(BarNumber, Beat)` tuples directly.

2. How is "overlapping events" defined exactly (same tick only, any overlap window, or configurable min gap in ticks)?
   **Answer:** For Story 4.1, define overlap as **same (BarNumber, Beat) tuple exactly**. A configurable minimum gap (in beat fractions) can be added in Story 4.2/4.3 for sticking rules; this story focuses on simultaneous conflicts only.

3. Should `RoleLimbMapping` support multiple possible limbs per role (e.g., toms playable by either hand) or only a single preferred limb with later rules able to reassign? If multiple, how should priorities be expressed?
   **Answer:** Single preferred limb per role for Story 4.1. The mapping returns one `Limb` per role. Toms map to `LeftHand` by default (free hand when right hand is on hat). Future stories can add secondary limb preferences or reassignment logic.

4. How should unknown or unmapped roles be handled by `GetRequiredLimb` (throw, return a sentinel, or attempt best-effort mapping)?
   **Answer:** Return `null` (nullable `Limb?`) for unknown roles. This allows callers to decide handling: skip the assignment, use a fallback, or log a warning. No exception thrown.

5. Are pedal roles (HiHatPedal, LeftFoot) considered interchangeable with stick hands for special techniques (e.g., cross-stick with foot token)?
   **Answer:** No. Feet and hands are distinct limb types. `HiHatPedal` maps to `LeftFoot` and is not interchangeable with hand roles. This is physically accurate.

6. For simultaneous articulation (e.g., open hat + closed hat variants mapped to same limb), do we treat them as same-role conflicts or distinct roles? Are role aliases normalized elsewhere?
   **Answer:** `ClosedHat` and `OpenHat` are distinct roles (per `GrooveRoles`) but both map to `RightHand` by default. Simultaneous ClosedHat + OpenHat on same beat = conflict (same limb). Role normalization is not needed; the limb mapping handles the consolidation.

7. What is expected output when conflicts are detected: just a conflict list (AC) or also a recommended resolution (e.g., preference removal) for later filters?
   **Answer:** Just a conflict list per AC. The `LimbConflict` record contains the conflicting `LimbAssignment` pairs. Resolution (which candidate to remove) is handled by `PhysicalityFilter` in Story 4.3.

8. Are style-level overrides for role→limb mapping expected (e.g., metal allowing double-kick mapping), and if so where are they stored?
   **Answer:** Yes, style-level overrides are expected (e.g., Metal with double-pedal maps Kick to both feet). Store overrides in `LimbModel` constructor or via `PhysicalityRules.LimbModel`. The `AllowDoublePedal` flag in `PhysicalityRules` already exists and will influence mapping in later stories.

## 8) Test Scenario Ideas (unit test name suggestions)
- `LimbConflictDetector_Detects_SameLimb_SameTick_AsConflict`
  - Setup: two DrumCandidates both map to `Snare` at same bar and same tick → expect one conflict.
- `LimbConflictDetector_Allows_DifferentLimbs_SameTick`
  - Setup: snare+hat at same tick → expect no conflict.
- `LimbConflictDetector_NoConflicts_OnEmptyAssignments`
  - Setup: empty list → expect zero conflicts.
- `LimbConflictDetector_Respects_TickGranularity_ForOverlap`
  - Setup: events separated by 1 tick vs min-gap threshold → expect either conflict or no conflict depending on threshold.
- `RoleLimbMapping_Defaults_Are_AsDocumented`
  - Verify the documented default mapping entries.
- `GetRequiredLimb_UnknownRole_ThrowsOrReturnsSentinel` (clarify expected behavior)

Determinism points to verify in tests:
- Same input assignments (order-preserving and order-shuffled) → same conflicts listed deterministically.
- Mapping lookup stable across runs and not influenced by RNG.

---

Notes / Open items for implementer: answer Clarifying Questions before implementing overlap math and return semantics.
