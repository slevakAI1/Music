# Pre-Analysis: Story 6.3 — Implement Articulation Mapping

## 1) Story Intent Summary
- What: Provide a compact description of what Story 6.3 intends to accomplish.
  - Map drummer `DrumArticulation` intents (rimshot, side stick, open hat, crash variants, flam, etc.) to concrete MIDI outputs or articulation hints suitable for downstream renderers.
- Why: Business/technical value
  - Improves realism by enabling downstream renderers or instrument mappings to realize expressive articulations; keeps agent output semantically rich and render-agnostic.
- Who benefits:
  - Developers: clear contract between agent output and renderers
  - Generator: richer candidate metadata for selection and diagnostics
  - End-users: more realistic/performance-like drum rendering when supported by target instrument/samples

## 2) Acceptance Criteria Checklist
1. Create `DrumArticulationMapper` that maps `DrumArticulation` enum to MIDI note variations or articulation tokens.
2. Map common articulations: Rimshot, SideStick, OpenHat, ClosedHat, Crash (types), Ride, Flam.
3. Provide graceful fallback to standard MIDI notes when articulation is unsupported by the target mapping.
4. Support GM/GM2 standard mappings where available; document assumptions and fallback behavior.
5. Unit tests verifying articulation→MIDI mapping for supported articulations.
6. Unit tests verifying fallback behavior when articulation unavailable.
7. Integration tests that articulation metadata survives mapping through the candidate→GrooveOnset conversion and is available to final renderer (opt-in diagnostics).
8. No runtime failures for missing mappings or null inputs; deterministic outcomes for same inputs.

Ambiguous/unclear ACs (highlighted):
- "Maps to MIDI note variations" — which MIDI mapping standard(s) are required (GM, GM2, or sample-kit-specific)?
- "Crash types" — how many crash variants are expected and how should they be selected?
- Degree of integration with specific synths/VSTs vs. generic MIDI fallback is not specified.

## 3) Dependencies & Integration Points
- Depends on:
  - Story 2.2 (`DrumCandidate`, `DrumArticulation`, `FillRole`) — uses the `DrumArticulation` enum and candidate pipeline.
  - Story 6.1 / 6.2 (Velocity and Timing shapers) — articulations may interact with velocity or timing hints; tests should ensure non-breaking interactions.
  - Groove mapping: `DrumCandidateMapper` and `GrooveOnset` conversion path (candidate → onset) where articulation hint must be preserved or translated.
- Interacts with existing types:
  - `DrumArticulation` enum, `DrumCandidate`, `GrooveOnset`, `StyleConfiguration` (potential style-specific articulation preferences), and MIDI export/converter layers (`Converters/` and `Midi/`).
- Provides for future stories:
  - Story 6.3 enables later audio-renderer integration, VST articulation selection, and improved diagnostics.

## 4) Inputs & Outputs
- Inputs:
  - `DrumArticulation` values attached to `DrumCandidate`.
  - Optional target mapping context (e.g., desired mapping standard: GM/GM2, sample-kit name) if provided by caller or configuration.
  - Style or instrument preferences (optional) to influence mapping selection.
- Outputs:
  - Mapping result: explicit MIDI note number(s) or articulation token(s) (string tag) that consumers can use to render the articulation.
  - A documented fallback indicator when the articulation could not be mapped to a specific MIDI note.
- Configuration/settings read:
  - Optional mapping tables (GM/GM2 defaults), and optional per-style/per-kit overrides if present in configuration.

## 5) Constraints & Invariants
- MUST NOT change existing runtime behavior when mapping missing — agent must still produce functional MIDI (fallback to standard notes).
- Determinism: same `DrumArticulation` + same mapping context → same output mapping.
- Safety: mapping calls must be null-safe and not throw for unknown articulations or missing mapping tables.
- Integration order: articulation mapping occurs at or after candidate→GrooveOnset conversion but before final MIDI emission to ensure mapping is available to converters.
- Hard limits: mappings must produce valid MIDI note numbers (0..127) or a well-defined token; outputs outside this range are invalid and must be clamped or rejected with fallback.

## 6) Edge Cases to Test
- Unknown articulation value (future enum value): mapper should return fallback token or standard note.
- Null or empty candidate/candidate id: mapping should return gracefully without throwing.
- Missing mapping table/config: use built-in GM/Conservative fallback.
- Multiple articulations requested for one candidate: define precedence or combined token behavior.
- Target instrument does not support articulation tokens: ensure fallback to note-only mapping.
- Crash variants requested but mapping table only has one crash note: ensure deterministic selection (e.g., pick the primary crash mapping).
- Per-style or per-kit overrides conflict: ensure deterministic resolution or documented precedence.
- Interaction with velocity/timing shapers: confirm articulation mapping does not inadvertently modify velocity/timing hints.

## 7) Clarifying Questions

1. Which mapping standards must be supported out of the box (GM, GM2, custom sample-kit names)?
**Answer:** Support GM2 (General MIDI Level 2) standard mappings as the baseline. GM2 provides the most comprehensive drum note mappings including articulations. Future extensions can add custom kit mappings via configuration.

2. Should the mapper return a MIDI note number only, or also an articulation token (string) that renderers can interpret?
**Answer:** Return both: (a) MIDI note number for immediate playback compatibility, and (b) optional articulation metadata string for advanced renderers. This dual approach ensures backward compatibility while enabling future enhancements.

3. How many crash variants are required and how should a specific variant be selected (operator hint, style, seed)?
**Answer:** Support 2 crash variants (Crash1, Crash2) mapped to GM2 notes 49 and 57. Selection is deterministic based on the role name suffix if provided by operators (e.g., "Crash" → Crash1, "Crash2" → Crash2), otherwise default to Crash1.

4. When a VST/sampler exposes articulation by bank/program/CC rather than MIDI note, do we need a mapping abstraction or only note-level mapping for now?
**Answer:** Note-level mapping only for Story 6.3. Bank/program/CC mappings are future enhancements. The articulation metadata string provides extensibility for such cases.

5. Should `DrumArticulationMapper` be style-aware (consult `StyleConfiguration`) when selecting articulations?
**Answer:** No style awareness in Story 6.3. Keep the mapper simple and deterministic based solely on articulation enum + role. Style influence happens earlier in operator selection, not in MIDI mapping.

6. What exact fallback behavior is preferred when an articulation is unmapped: (a) return a neutral note, (b) return null and let converter choose, or (c) return a token indicating "unsupported"?
**Answer:** (a) Return the standard MIDI note for the role (e.g., snare → 38, kick → 36) with articulation metadata indicating "fallback". This ensures playable MIDI output always.

7. Are there any performance or allocation constraints on mapping calls (hot path concerns)?
**Answer:** Minimal allocations. Use static readonly dictionaries for mappings. The mapper is called during candidate→onset conversion, not per-frame rendering, so performance is not critical but should still be efficient.

8. Where should mapping tables live (code, config files, resource bundles) and who owns them (agent vs. converter subsystem)?
**Answer:** Hardcoded in `DrumArticulationMapper` as static readonly dictionaries for Story 6.3. Future stories can externalize to config. Ownership is agent subsystem since it's part of the drummer performance layer.

9. For unit tests, which canonical mapping table should tests assert against (GM2 defaults?), and where is that authoritative source?
**Answer:** Tests assert against GM2 specification. Authoritative source is the hardcoded mapping tables in `DrumArticulationMapper` which document GM2 note numbers in comments.

10. Should articulation mapping influence selection/scoring earlier in the pipeline (e.g., prefer articulations available in target kit), or only be applied at render-time?
**Answer:** Render-time only for Story 6.3. Operators freely specify articulations; the mapper handles graceful fallback. Kit-aware operator scoring is a future enhancement.

## 8) Test Scenario Ideas
- Unit tests:
  - `ArticulationMapper_KnownArticulation_ReturnsExpectedMidiNote` (parametrized for rimshot, side stick, open hat, crash)
  - `ArticulationMapper_UnknownArticulation_ReturnsFallbackTokenOrNote`
  - `ArticulationMapper_NullInput_GracefulFallback`
  - `ArticulationMapper_CrashVariants_UsesDeterministicSelection`
  - `ArticulationMapper_PerStyleOverride_AppliesWhenConfigured`
  - `ArticulationMapper_Integration_PreservesTokenThroughMapperToConverter`
- Integration tests:
  - `DrumCandidateMapper_PreservesArticulationToken_AfterConversion` (candidate → groove onset → converter) asserting token presence
  - `MidiExport_WithArticulations_ProducesValidMidiNotesOrTokens` verifying no out-of-range notes
- Determinism checks:
  - `SameInputs_SameMappingOutput` with identical mapping context and seed
  - `DifferentKit_DifferentMappingOutput` when using alternate kit override
- Test data setups:
  - Minimal mapping table (GM defaults) used as authoritative fixture
  - Custom kit override table to validate precedence and fallback

---

*End of Pre-Analysis for Story 6.3.*
