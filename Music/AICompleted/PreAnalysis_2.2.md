# Pre-Analysis: Story 2.2 — Define Drum Candidate Type

Story ID: 2.2
Title: Define Drum Candidate Type

## 1. Story Intent Summary
- What: Define a drum-specific candidate type (`DrumCandidate`) that operators can generate containing rich event metadata (role, position, strength, velocity/timing/articulation hints, fill role, score).
- Why: Provides a single, expressive candidate representation that operator implementations produce and the selection/mapper pipeline consumes, enabling richer operator behavior and consistent mapping into the groove system.
- Who benefits: Operator implementers, the drummer agent (selection & policy layers), downstream mapping logic (`DrumCandidateMapper`), and end-users through improved drum realism.

## 2. Acceptance Criteria Checklist
1. Create `DrumCandidate` record with fields:
   1. `CandidateId` (stable identifier: operatorId + hash of params)
   2. `OperatorId`
   3. `Role` (Kick, Snare, ClosedHat, OpenHat, Crash, Ride, Tom1, Tom2, FloorTom)
   4. `BarNumber`, `Beat` (position)
   5. `Strength` (OnsetStrength enum: Downbeat, Backbeat, Strong, Offbeat, Pickup, Ghost)
   6. `VelocityHint` (int 0-127)
   7. `TimingHint` (tick offset)
   8. `ArticulationHint` (optional: Rimshot, SideStick, OpenHat, Crash, etc.)
   9. `FillRole` (None, FillStart, FillBody, FillEnd, Setup)
   10. `Score` (operator-assigned score before style weighting)
2. Create `DrumArticulation` enum
3. Create `FillRole` enum
4. Unit tests: candidates can be created and scored

Ambiguities/unclear ACs:
- How `CandidateId` stability is achieved exactly (hash algorithm, canonicalizing params) is unspecified.
- `TimingHint` units: tick offset relative to grid or absolute ticks? Range not specified.
- `VelocityHint` bounds and whether null/optional allowed for "no preference".

## 3. Dependencies & Integration Points
- Depends on: Story 2.1 (DrummerContext) for position/role semantics and Story 1.1 (AgentContext, IMusicalOperator)
- Integrates with: `IGrooveCandidateSource` (drummer candidate source), `DrumCandidateMapper` (maps `DrumCandidate` → `GrooveOnsetCandidate`/`GrooveOnset`), `OperatorSelectionEngine` (consumes candidates' scores), `DrummerPolicyProvider` (policy may affect allowed roles/strength)
- Interacts with types: `OnsetStrength` enum in Groove system, `GrooveRoles`, `MusicConstants.TicksPerQuarterNote`, `GrooveBarContext`/`BarContext`
- Provides for future stories: canonical candidate type enabling operator implementations (Stage 3), physicality filtering (Stage 4), and performance shaping (Stage 6)

## 4. Inputs & Outputs
Inputs consumed by operators to create `DrumCandidate`:
- `DrummerContext` (bar, beat, active roles, hat mode, energy)
- Operator-specific parameters (timing offsets, substrokes, densities)
- RNG seed/streams for deterministic variation (tie-breaking in param choices)

Outputs produced:
- `DrumCandidate` instances (one per suggested onset)
- Candidate stream/list passed to `DrummerCandidateSource` and `OperatorSelectionEngine`

Configuration/settings read:
- StyleConfiguration operator weights and defaults (for velocity/time hints when unspecified)
- PhysicalityRules (may constrain roles/articulations after candidate creation)

## 5. Constraints & Invariants
- `BarNumber` must be >= 1
- `Beat` must be in [1, BeatsPerBar + fractional) and consistent with `BarTrack`/time signature
- `Role` must be one of known `GrooveRoles` or defined tom roles
- `CandidateId` must be stable for identical parameter sets and operatorId (determinism requirement)
- `VelocityHint` must be within MIDI range 0-127 if provided
- `TimingHint` must not exceed reasonable clamp bounds (e.g., +- MaxAbsTimingBiasTicks or a predefined tick clamp)
- `Score` must be within a defined 0.0-1.0 range (AC states 0.0-1.0 for scores elsewhere)
- `FillRole` semantics must be mutually exclusive per candidate

Order/operation invariants:
- Candidates produced by operators must be filtered by physicality rules before selection
- Mapping to `GrooveOnset` occurs after selection and performance shaping

## 6. Edge Cases to Test
- Creation with minimal required fields only
- Candidate with null/absent `VelocityHint`/`TimingHint` vs explicit hints
- Candidates placed on invalid beat numbers or out-of-range bar numbers
- Duplicate candidates (same `OperatorId` + identical params) — canonical `CandidateId` generation
- High-density candidate lists: `Score` ordering vs selection/density enforcement
- Articulation hints unsupported by MIDI map — mapper fallback behavior
- Fill role overlap across candidates in same bar — selection or memory conflict
- Negative or extreme `TimingHint` values (clamp behavior)
- Invalid `Role` strings — validation

## 7. Clarifying Questions
1. CandidateId format: which hashing/canonicalization method should be used to guarantee stability across runs and languages?
**Answer:** Use a deterministic string format: `"{OperatorId}_{Role}_{BarNumber}_{Beat}"`. This is simple, stable, human-readable for debugging, and avoids hash algorithm dependencies. For candidates with additional distinguishing parameters (e.g., articulation), append a suffix.

2. Is `VelocityHint` optional (nullable) or must it always be present? If optional, what fallback applies?
**Answer:** Nullable (`int?`). When null, downstream velocity shaping (DrummerVelocityShaper in Story 6.1) will compute velocity based on strength and role defaults. Operators should provide hints when they have specific intent.

3. `TimingHint` unit: is it ticks relative to the beat grid, and what is the allowed range and clamp policy?
**Answer:** Ticks relative to the grid beat position (positive = late, negative = early). Range: [-MaxAbsTimingBiasTicks..+MaxAbsTimingBiasTicks] from GrooveTimingPolicy (typically ±48 ticks). Clamping happens at mapper/performance layer, not in the record itself.

4. Should `Score` be constrained to 0.0-1.0 (AC implies) or can operators return raw scores later normalized by selection engine?
**Answer:** Operators should return scores in 0.0-1.0 range. The selection engine applies style weights and memory penalties as multipliers. No normalization step; operators are responsible for returning valid scores.

5. `ArticulationHint` enum scope: provide a minimal set now (Rimshot, SideStick, OpenHat, Crash, Ride, None) or allow extensible strings?
**Answer:** Enum with minimal set for type safety and determinism. Include: None, Rimshot, SideStick, OpenHat, Crash, Ride, CrashChoke, Flam. Extensible later by adding enum values.

6. Should `DrumCandidate` include provenance metadata (operator parameters) beyond `OperatorId` for diagnostics/troubleshooting?
**Answer:** No. Keep DrumCandidate lean. OperatorId + CandidateId provide sufficient traceability. Detailed operator parameters can be logged via DrummerDiagnosticsCollector (Story 7.1) if needed.

7. For fill roles, clarify definitions for `Setup` vs `FillStart` vs `FillBody` vs `FillEnd` (timing relative to bar boundaries)
**Answer:**
- `None`: Standard groove hit, not part of a fill
- `Setup`: Pre-fill accent (typically beat 4 "and" before fill starts)
- `FillStart`: First hit of the fill pattern (often beat 3 or 3.5)
- `FillBody`: Interior fill notes (ascending/descending pattern)
- `FillEnd`: Terminal hit of fill (often crash on beat 1 of next bar, or resolving snare)

8. When multiple operators generate candidates for same onset, how should duplicates be deduped — by `CandidateId` or canonical beat/role pair?
**Answer:** By CandidateId. Two operators can legitimately generate candidates for the same beat/role (e.g., ghost operator and embellishment operator). Selection engine picks among them by score. CandidateId ensures each candidate is uniquely tracked.

## 8. Test Scenario Ideas
- `DrumCandidate_Creation_MinimalFields_Succeeds`
  - Create candidate with required fields, assert non-null fields and valid defaults

- `DrumCandidate_Score_Range_Clamped`
  - Create candidates with scores outside [0.0,1.0] and verify enforcement or expected normalization

- `DrumCandidate_CandidateId_Deterministic_ForSameParams`
  - Create two candidates using identical parameters and assert identical `CandidateId`

- `DrumCandidate_TimingHint_Clamp_OutOfRange`
  - Provide extreme timing hints and assert mapper/clamp behavior

- `DrumCandidate_Articulation_Fallback_WhenUnknown`
  - Use an articulation unsupported by MIDI map and assert fallback mapping

- `DrumCandidate_Mapping_To_GrooveOnset_Produces_Valid_Onset`
  - Integrate with `DrumCandidateMapper` test to ensure a candidate maps to valid `GrooveOnset` fields

- `DrumCandidate_DuplicateDetection_ByCandidateId`
  - Two operators produce identical candidate parameters; duplication detection collapses to single candidate or keeps both but distinct IDs per operator as specified

- Determinism tests for seed influence on candidate param choices (when RNG used)


---

Notes: This pre-analysis focuses on understanding the story constraints, ambiguities, and test targets. It deliberately avoids prescribing implementation details.
