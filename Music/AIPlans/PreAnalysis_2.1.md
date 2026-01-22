# Pre-Analysis: Story 2.1 — Define Drummer-Specific Context

Story ID: 2.1
Title: Define Drummer-Specific Context

User Story
As a drummer agent I want drum-specific context extending the common agent context So that operators have access to drum-relevant information

---

1. Story Intent Summary

- What: Provide a concrete, immutable `DrummerContext` (and builder) that extends the shared `AgentContext` with drum-specific fields used by drummer operators and policy providers.
- Why: Centralizes drum-relevant inputs (role availability, recent hits, subdivision mode, fill windows) so operators and selection engines can make deterministic, well-scoped decisions. This reduces duplication and keeps generators consistent.
- Who: Generator developers (implementing operators), the drummer agent (selection & policy layers), and ultimately end-users through improved drum output fidelity.

---

2. Acceptance Criteria Checklist

1. Create `DrummerContext` extending `AgentContext` containing these fields:
   - `ActiveRoles` (which drum roles are enabled: Kick, Snare, ClosedHat, OpenHat, Crash, Ride, Toms)
   - `LastKickBeat` (for coordination with bass)
   - `LastSnareBeat` (for ghost note placement)
   - `CurrentHatMode` (Closed, Open, Ride)
   - `HatSubdivision` (Eighth, Sixteenth, None)
   - `IsFillWindow` (true if in phrase-end fill zone)
   - `IsAtSectionBoundary` (true if at section start/end)
   - `BackbeatBeats` (e.g., [2, 4] for 4/4)
2. Create `DrummerContextBuilder` that builds `DrummerContext` from `GrooveBarContext` + policies.
3. Unit tests: context builds correctly from groove inputs.

Notes: Grouped ACs: (fields) vs (builder) vs (tests).

Ambiguities to confirm in ACs (highlighted):
- Types and shapes: `ActiveRoles` (list of strings vs enum set), `LastKickBeat`/`LastSnareBeat` (decimal beat? absolute tick?), `BackbeatBeats` semantics for non-4/4 meters.
- Definition of `IsFillWindow` and `IsAtSectionBoundary` (how many bars/beats count as "window")

---

3. Dependencies & Integration Points

- Story dependencies (explicit/implicit):
  - Stage 1: Shared Agent Infrastructure (1.1–1.4) — `AgentContext`, `IAgentMemory`, `Rng`, `OperatorFamily` (already completed in repo).
  - Groove system stories (A1..): expects `GrooveBarContext`/`GroovePolicyDecision` to feed builder inputs.

- Existing code/types to interact with:
  - `Generator/Agents/Common/AgentContext` (base type)
  - `Generator/Groove/BarContext` or `GrooveBarContext` (per-bar groove inputs)
  - `Generator/Groove/GroovePolicyDecision` (policy overrides per bar/role)
  - `MusicConstants.eSectionType`, `SectionTrack` (to determine boundary), `BarTrack` (timing), `Rng` (for deterministic decisions if needed)
  - `GrooveRoles` constants (role names)

- What this story provides for future stories:
  - The `DrummerContext` is consumed by: `DrummerCandidate` generation (Story 2.2), `DrummerPolicyProvider` (2.3), candidate source (2.4), and memory (2.5). It is the canonical single-argument context for drummer operators.

---

4. Inputs & Outputs

- Inputs (consumed)
  - `GrooveBarContext` / per-bar groove information (bar number, section, phrase position)
  - `GroovePolicyDecision` or style overrides (enabled tags, density biases)
  - `BarTrack` / timing data (numerator/denominator, start ticks)
  - `SectionTrack` or section metadata (to compute boundaries)
  - Recent performance state (from `AgentMemory` or runtime state): last event beats for kick/snare
  - Configuration: style-level defaults (Backbeat defaults, hat subdivision preferences)

- Outputs (produced)
  - `DrummerContext` record/DTO containing computed fields listed in ACs
  - The builder may also produce diagnostics/metadata (optional) used by downstream tests

- Configuration/settings read
  - Style configuration (e.g., PopRock defaults for hat subdivision, backbeat beats)
  - Groove protection/policy for fill windows and enabled roles
  - System constants: beats-per-bar conventions, ticks-per-quarter (for beat->tick conversions)

---

5. Constraints & Invariants

- Bars/beats are 1-based everywhere (in repo). Builder must respect this.
- `ActiveRoles` must be a subset of known `GrooveRoles`.
- `BackbeatBeats` values must be valid beat numbers within a bar's numerator.
- `LastKickBeat` / `LastSnareBeat` if present must be within [1, numerator) or use nullable to indicate unknown.
- `IsFillWindow`/`IsAtSectionBoundary` must be deterministic given same inputs.
- Builder must not mutate shared/global state; it produces an immutable `DrummerContext`.

Order constraints
- Determine section & phrase position before computing `IsFillWindow`.
- Compute timing/grid info (numerator/denominator) before mapping beats.

Hard limits / ranges
- Bar numbers >= 1; beat fractions must be validated (e.g., beat < Numerator + 1).
- `ActiveRoles` length bounded by number of roles (small fixed list).

---

6. Edge Cases to Test

- Empty or missing `GrooveBarContext` or `GroovePolicyDecision` → builder should fail fast or return sane defaults.
- Non-4/4 time signatures: ensure `BackbeatBeats` computed correctly (e.g., 3/4 has backbeat at 2).
- No prior `LastKickBeat`/`LastSnareBeat` recorded (null/none) → ensure fields indicate "unknown" monotonically.
- `IsFillWindow` near song boundaries (first/last bar) and very short sections (1 bar long).
- Conflicting inputs: policy enables a role but groove anchor/protection disables it.
- High numerator (7/8) and unusual subdivisions: hat subdivision mapping must still produce sensible enum.
- Concurrency: multiple builders invoked in parallel sharing underlying tracks (ensure no mutation).
- Invalid `BackbeatBeats` (values outside bar range) — builder must sanitize or reject.

Combination scenarios
- Fill window active + `IsAtSectionBoundary` true + high energy: verify all flags set coherently.
- Role disabled in `ActiveRoles` but also present in `GrooveBarContext.AnchorLayer` — precedence rules required.

---

7. Clarifying Questions

1. Type details: What exact types are expected for these fields?
   - `ActiveRoles`: `IReadOnlyList<string>` or typed `IReadOnlySet<GrooveRoles>` or enum flags?
   - `LastKickBeat` / `LastSnareBeat`: decimal beat (e.g., 2.5) or absolute tick? Nullable allowed?
   - `BackbeatBeats`: integer beat numbers or decimals for irregular meters?
2. `HatSubdivision` and `CurrentHatMode` enums: are there existing enums to reuse, or must they be created? If existing, what are the canonical names/values?
3. `IsFillWindow` definition: how wide is the window (bars or beats)? Who supplies the threshold: GroovePolicyDecision or a hard default?
4. `IsAtSectionBoundary` semantics: does it include both start and end? How many bars/which beat counts as boundary?
5. `DrummerContextBuilder` inputs: should the builder accept raw `GrooveBarContext` + `GroovePolicyDecision` or higher-level `BarContext` + `SongContext`? Is diagnostic collection required during build?
6. Determinism: Should the builder use `Rng` for any decisions (e.g., tie-breaks), or purely deterministic mapping from inputs?
7. Error handling: If required bar/section/timing info is missing, should the builder throw or return a minimal context with conservative defaults?
8. Unit test expectations: are there canonical test fixtures (TestDesigns/GrooveTestSetup) to reuse for builder tests? Any golden snapshots expected?

---

8. Test Scenario Ideas (unit test names + fixtures)

- `DrummerContextBuilder_Builds_DefaultContext_ForSimple4_4Groove` — simple 4/4 GrooveBarContext, verify default ActiveRoles and BackbeatBeats
- `DrummerContextBuilder_Sets_IsFillWindow_AtPhraseEnd` — fixture where phrase end within N bars; assert `IsFillWindow==true`
- `DrummerContextBuilder_Handles_NoPriorHits` — ensure LastKickBeat/LastSnareBeat null/unknown handled
- `DrummerContextBuilder_Respects_Policy_DisabledRole` — policy disables Snare; ActiveRoles does not contain Snare
- `DrummerContextBuilder_Maps_HatSubdivision_For_EnergyLevels` — vary style/policy and check `HatSubdivision`
- `DrummerContextBuilder_Non4_4_BackbeatBeats_Computed` — test 3/4 and 6/8 cases
- `DrummerContextBuilder_Deterministic_SameInputsSameResult` — run twice with same inputs, expect identical DrummerContext (structural equality)
- `DrummerContextBuilder_InvalidBackbeat_ThrowsOrSanitizes` — invalid backbeat beats input

Fixtures and data setups
- Reuse `GrooveTestSetup.BuildPopRockBasicGrooveForTestSong` and `TestDesigns.SetTestDesignD1` where available to create realistic `GrooveBarContext` inputs.
- Synthetic `GrooveBarContext` with explicit `Section`, `BarNumber`, `PhrasePosition`, and `EnergyLevel` to test boundary logic.

---

Summary: This story creates the canonical per-bar context for the drummer agent. Key clarifications center on type shapes, exact semantics for fill/boundary flags, and whether the builder should be defensive or strict on missing inputs. The `DrummerContext` is a small but critical contract consumed by many downstream stories (2.2–3.6).
