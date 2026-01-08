# Music Generator Plan (Post–Stage 2)

## Stage 3 — Make harmony sound connected (keys/pads voice-leading first)

**Why now:** keys/pads are the fastest way to stop the output from sounding like a MIDI test pattern. Voice-leading makes everything feel intentional.

### Story 3.1 — `ChordRealization` becomes the unit of harmony rendering

**Intent:** stop treating a chord as “list of chord tones.” Treat it as “chosen voicing + register + optional color,” stable for a window.

**Acceptance criteria:**
- Create `ChordRealization`  with fields you will actually use:
  - `IReadOnlyList<int> MidiNotes`
  - `string Inversion`
  - `int RegisterCenterMidi`
  - `bool HasColorTone` + `string? ColorToneTag` (e.g., `"add9"`)
  - `int Density` (note count)
- Keys generation consumes `ChordRealization` per onset slot (not raw notes).

### Story 3.2 — `VoiceLeadingSelector` for consecutive harmony contexts

**Intent:** choose inversions/voicings by minimizing movement, not by randomness.

**Acceptance criteria:**
- Implement `VoiceLeadingSelector`:
  - **Inputs:** previous `ChordRealization` (or previous MIDI set), current `HarmonyPitchContext`, section profile
  - **Output:** next `ChordRealization`
- “Cost function” MVP:
  - minimize total absolute semitone movement (optionally penalize voice crossings)
  - keep top note within a target range for the role
- Keys and pads track keep independent “previous voicing” state.

### Story 3.3 — Section-based voicing / density profile (very small)

**Intent:** chorus should lift (register, density), verse should be lighter.

**Acceptance criteria:**
- Add lightweight per-section “arrangement hints” (doesn’t need a huge model):
  - `RegisterLift` (e.g., `+0` / `+12`)
  - `MaxDensity` (`3–5`)
  - `ColorToneProbability` by section type
- Keys/pads use this instead of a global `KeysAdd9Probability`.

---

## Stage 4 — Make comp into comp (not a monophonic pseudo-lead)

**Why now:** your guitar “comp” is currently a single-note line. That will always read as lead-ish.

### Story 4.1 — Comp rhythm patterns, not just groove onsets

**Intent:** groove onsets are a skeleton, not the part.

**Acceptance criteria:**
- Add a small `CompRhythmPattern` library keyed by groove preset + section type:
  - uses the bar’s onset slots as candidates
  - pattern selects a subset (e.g., anticipate beat 1, skip beat 3, etc.)
- Still deterministic: “pattern selection” is deterministic by section + bar index.

### Story 4.2 — `CompVoicingSelector`: chord fragments & guide tones

**Intent:** comp = chord fragments (2–4 notes), guide tones on strong beats, root often omitted.

**Acceptance criteria:**
- Implement `CompVoicingSelector.Select(ctx, slot, previousVoicing, sectionProfile) -> List<int>`
- MVP rules:
  - strong beats: prefer including 3rd/7th when available
  - allow root omission
  - avoid muddy close voicings below a low threshold (e.g., keep lowest comp tone above ~E3-ish)
  - keep top comp tone below the “lead space” ceiling
- Guitar comp emits multi-note events per slot (simultaneous notes).

### Story 4.3 — Strum/r
