# Saved Prompt + Assistant Response

---

## Prompt (user)

[Full user prompt begins]

Let's discuss stage 6 of the current epic.
First, I want to understand in detail how the `Music.Generator.Agents.Drums.DrumArticulation` enum will affect the notes being written. Does the articulation alter notes, add to them, replace them? Do some work differently in this respect? For example, we have the kick, snare and hi hat as the drum set, notes are currently generated from the anchor. how and when is drum articulation applied, once the agent is wired up? How and when are articulations picked, or skipped and not picked (none i think is an option)?

## Stage 6 — Performance Rendering (Human Realism)

**Goal:** Apply velocity and timing nuance for realistic drum output.

---

### Story 6.1 — Implement Drummer Velocity Shaper

(acceptance criteria omitted here for brevity in the stored prompt)

### Story 6.2 — Implement Drummer Timing Nuance

(acceptance criteria omitted here for brevity in the stored prompt)

### Story 6.3 — Implement Articulation Mapping

(acceptance criteria omitted here for brevity in the stored prompt)

[End of user prompt]

---

## Assistant Response (saved)

Summary: How `DrumArticulation` affects generated drum output, when articulations are chosen/kept/skipped, how they are applied in the pipeline, and practical design notes for Stage 6 (velocity/timing/articulation mapping).

1) Short answer

- `DrumArticulation` is an operator hint that usually *replaces* the default MIDI note number for a role (e.g., snare rimshot vs standard snare). It does not add an extra note except for `Flam`, which is a grace note (adds a short preceding event).
- Operators pick articulations at candidate generation time. The articulation travels with the selected candidate through selection, shaping, and final mapping.
- Final mapping of (role + articulation) → MIDI note (and possibly duration or additional events) is performed in the performance rendering stage by a `DrumArticulationMapper` (Story 6.3).

2) Detailed behavior by articulation

- `None` (0)
  - Means: use the role's standard note (no articulation change).
  - No extra events; downstream velocity/timing shaping still applies.

- `Rimshot` / `SideStick` (snare variants)
  - Replace standard snare MIDI note with the GM/target VST note for rimshot or side-stick (example GM2 mapping: SideStick=37, AcousticSnare=38, Rimshot=40).
  - Implementation: single note with different MIDI note number and same timing/velocity pipeline.

- `OpenHat` / `ClosedHat`
  - `ClosedHat` is the default (42 GM). `OpenHat` maps to 46 GM.
  - Open hat may also imply a longer duration/sustain and possibly slightly higher velocity.

- `Crash`, `CrashChoke`
  - `Crash` maps to an appropriate crash note (e.g., GM2 49 or 57 for different crashes). `CrashChoke` can be implemented two ways: map to a different MIDI note that represents a choked crash, or use regular crash note but generate a short note-off (short duration) and optionally a choke controller/event if the synth supports it.

- `Ride` / `RideBell`
  - Replace ride-on-bow note with ride-bell note (e.g., GM2 ride=51, ride bell=53).

- `Flam`
  - Special: adds an extra grace note immediately before the main hit (small timing offset, lower velocity) and then the main note. So it produces an additional event + the primary event.
  - The grace note should be scheduled inside the same micro-timing window and obey physicality/sticking rules (no impossible limb collisions).

3) When articulations are chosen and how they flow through the pipeline

- Candidate generation (Operators)
  - Operators decide `ArticulationHint` when calling `GenerateCandidates()`.
  - Selection logic (operator weighting, style config, memory penalties) picks final candidates; articulation hint is preserved on the candidate.
  - Example: `BackbeatVariantOperator` may emit snare candidates with `ArticulationHint = SideStick` or `Rimshot` based on section and energy.

- Candidate mapping
  - `DrumCandidateMapper` converts `DrumCandidate` → `GrooveOnsetCandidate` (preserves articulation as a tag on the candidate/provenance).

- Physicality filter & selection
  - Physicality checks should treat different articulations as identical for limb assignment unless the articulation implies a different limb (rare). They must ensure `Flam`'s added grace note does not introduce limb conflicts.

- Performance shaping
  - `DrummerVelocityShaper` (Story 6.1) uses operator`VelocityHint` and onset `Strength` to compute final MIDI velocity. Articulation does not change velocity calculation rules, but style rules can bias velocities for specific articulations (e.g., rimshots slightly louder).
  - `DrummerTimingShaper` (Story 6.2) applies role-based microtiming. Articulated hits receive the same timing adjustments unless an articulation implies a characteristic offset (e.g., side-stick may be slightly earlier/later by style).

- Articulation mapping (final step)
  - `DrumArticulationMapper` (Story 6.3) maps `(Role, DrumArticulation)` → final `NoteNumber`, and for special articulations may produce additional events or modify `NoteDurationTicks`.
  - If a VST does not support the requested articulation, mapper falls back to the role's default note. The mapper should be configurable per instrument/patch.

4) Replace vs Add

- Replace: Most articulations replace the single note's MIDI note number. No extra note is added.
- Add: Only `Flam` (and potential ornament variants like drag/ruff if later added) adds extra note events.
- Modify: `CrashChoke` or choking behavior may be implemented by changing duration or emitting a short explicit choke message/event — conceptually a modification rather than an addition.

5) Practical pipeline (end-to-end) — canonical order

1. Operators generate `DrumCandidate` with `ArticulationHint` and optional `VelocityHint`/`TimingHint`.
2. `DrumCandidateMapper` → `GrooveOnsetCandidate` (carry articulation as tag/provenance).
3. `PhysicalityFilter` prunes impossible candidates (honors articulation but doesn't treat it as a new limb by default).
4. `OperatorSelectionEngine` picks final onsets deterministically.
5. `DrummerVelocityShaper` computes final MIDI velocity for each selected onset.
6. `DrummerTimingShaper` applies microtiming offsets (role-specific biases).
7. `DrumArticulationMapper` maps `(Role, Articulation)` → final `NoteNumber` (and optional extra events, durations, choke actions).
8. PartTrack events created and exported to MIDI.

6) Determinism and articulation selection

- Articulation picks should be deterministic (seeded RNG stream). Use a dedicated stream key such as `RandomPurpose.DrummerArticulationPick` for any operator-level randomness related to articulation.
- Tie-break rules and operator-order determinism still apply: deterministic ordering -> consistent results for same seed and inputs.

7) Design recommendations and implementation notes (Stage 6 practical)

- Implement a configurable mapping table:
  - `Dictionary<(string role, DrumArticulation articulation), int midiNote>`
  - Allow per-preset / per-VST override: `DrumArticulationProfile` to map articulations to the synth's supported note numbers and to define fallbacks.

- `Flam` handling:
  - Mapper should expose an API that can return zero, one, or multiple `PartTrackEvent` per `GrooveOnset` so `Flam` can create the grace note + main note.
  - Grace note offsets should be small (e.g., -8 to -24 ticks depending on style/tempo) and use lower velocity.

- Crash choke:
  - If the VST supports choke groups, mapper can return a choke instruction (or short duration) and optionally a subsequent note-off/choke event. If not, short duration is acceptable fallback.

- Tests to add:
  - Verify mapping table yields expected GM note numbers for canonical articulations (SideStick→37, Snare→38, Rimshot→40, ClosedHat→42, OpenHat→46, Crash→49, Ride→51, RideBell→53).
  - Verify `Flam` produces two events with correct offsets/velocities.
  - Verify fallback behavior when profile lacks mapping: default role note used and no extra events.
  - End-to-end test: operator sets articulation → candidate preserved → selection → final MIDI note number matches mapping.

8) Short example (4/4 backbeat snare rimshot)

- Operator emits candidate: role=`Snare`, beat=2.0, `ArticulationHint=Rimshot`, `VelocityHint=95`.
- After selection and shaping:
  - `DrummerVelocityShaper` finalizes velocity=100
  - `DrummerTimingShaper` leaves timing on-grid or adds +3 ticks depending on feel
  - `DrumArticulationMapper` maps `(Snare,Rimshot)` → MIDI note 40
  - PartTrackEvent created with NoteNumber=40, Velocity=100, AbsoluteTime=barTick+offset

9) Edge cases and constraints

- Operators that generate pattern-replacements (e.g., `HalfTimeFeelOperator`) may emit many candidates; articulations should remain local to specific roles within the pattern.
- Don't let articulations override `IsProtected` / must-hit anchors unless policy explicitly allows it.
- Ensure flam/grace-note events do not violate limb/sticking constraints.

10) Conclusion

- Articulations are primarily *note replacements*, carried from operator→candidate→selection→mapping. `Flam` is the primary case that adds events.
- Mapping and fallbacks live in `DrumArticulationMapper` implemented in Story 6.3. Velocity/timing shaping happens before mapping but mapping may affect durations or add choke events.
- Use a per-VST articulation profile and deterministic RNG streams for pick variability.


---

End of saved assistant response.

