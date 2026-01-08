# Music Generator Plan (Post–Stage 2)

## Stage 3 — Make harmony sound connected (keys/pads voice-leading first) - COMPLETED

**Why now:** keys/pads are the fastest way to stop the output from sounding like a MIDI test pattern. Voice-leading makes everything feel intentional.

### Story 3.1 — `ChordRealization` becomes the unit of harmony rendering - COMPLETED

**Intent:** stop treating a chord as “list of chord tones.” Treat it as “chosen voicing + register + optional color,” stable for a window.

**Acceptance criteria:**
- Create `ChordRealization` (your existing Stage 3.1 idea) with fields you will actually use:
  - `IReadOnlyList<int> MidiNotes`
  - `string Inversion`
  - `int RegisterCenterMidi`
  - `bool HasColorTone` + `string? ColorToneTag` (e.g., `"add9"`)
  - `int Density` (note count)
- Keys generation consumes `ChordRealization` per onset slot (not raw notes).

### Story 3.2 — `VoiceLeadingSelector` for consecutive harmony contexts - COMPLETED

**Intent:** choose inversions/voicings by minimizing movement, not by randomness.

**Acceptance criteria:**
- Implement `VoiceLeadingSelector`:
  - **Inputs:** previous `ChordRealization` (or previous MIDI set), current `HarmonyPitchContext`, section profile
  - **Output:** next `ChordRealization`
- “Cost function” MVP:
  - minimize total absolute semitone movement (optionally penalize voice crossings)
  - keep top note within a target range for the role
- Keys and pads track keep independent “previous voicing” state.

### Story 3.3 — Section-based voicing / density profile (very small) -  - COMPLETED

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

### Story 4.3 — Strum/roll micro-timing (optional but high payoff)

**Intent:** block chords sound fake; tiny offsets make it human.

**Acceptance criteria:**
- Add deterministic per-note offsets within a chord (e.g., `0–N` ticks spread).
- Apply only to comp (and maybe keys later).

---

## Stage 5 — Bassline writing (lock to groove + outline harmony)

**Why now:** bass and drums define “song feels like a genre.”

### Story 5.1 — Bass pattern library keyed by groove + section type

**Acceptance criteria:**
- Define a handful of reusable bass patterns:
  - root anchors
  - root–fifth movement
  - octave pop
  - diatonic approach into chord change (policy-gated)
- Pattern selection is deterministic by section + bar; RNG only for tie-break.

### Story 5.2 — Chord-change awareness (approaches + pickups)

**Acceptance criteria:**
- Detect when next harmony changes within `N` beats.
- Insert approach note on weak beat or pickup onset (only if the groove has a slot there).
- Keep rules strict initially (no chromatic yet unless you later enable it via policy).

---

## Stage 6 — Drums: from static groove to performance (variation + fills)

**Why now:** your drum track is literally the preset. Real songs have micro-variation and transitions.

### Story 6.1 — `GroovePreset` becomes “template”; add variation operators

**Acceptance criteria:**
- Add a `DrumVariationEngine` that:
  - can drop/add hat hits
  - adds ghost notes (snare) at low velocity
  - adds occasional kick variations that still respect style
- Must be deterministic from seed.

### Story 6.2 — Section transitions: fills and turnarounds

**Acceptance criteria:**
- At the end of a section (or last bar of a phrase), generate a fill pattern appropriate to the groove.
- Fills must not break bar boundaries and must respect `Song.TotalBars`.

---

## Stage 7 — Section identity via energy/density (structured repetition)

**Why now:** “music” is repetition + contrast, not just correct notes.

### Story 7.1 — `SectionEnergyProfile` drives each role

**Acceptance criteria:**
- Create `SectionEnergyProfile` with simple knobs:
  - `DensityMultiplier` per role
  - `VelocityBias` per role
  - `RegisterLift` per role
  - `BusyProbability` per role
- Apply across bass/comp/keys/pads/drums so verse/chorus differ immediately.

### Story 7.2 — A/A’ logic (repeat with controlled changes)

**Acceptance criteria:**
- For repeated sections of same type (Verse 1 vs Verse 2):
  - reuse core pattern choices
  - apply small transforms (drop/add hits, slight register change, swap voicing fragment, etc.)
- This is where “surprise” belongs: intentional transforms, not random notes.

---

## Stage 8 — Motifs / hooks as first-class objects (only after accompaniment behaves)

**Why now:** once accompaniment is credible, motifs actually land.

### Story 8.1 — Motif model + placement policy

**Acceptance criteria:**
- Add `Motif`:
  - `Role` (lead OR riff role like guitar hook)
  - `TargetSections`
  - `RhythmShape` (derived from onset slots)
  - `ContourIntent` (up/down/arch)
  - `Constraints` (range, chord-tone bias)
- Placement: chorus hook; intro riff optional depending on style.

### Story 8.2 — Motif renderer with variation

**Acceptance criteria:**
- Render motif against:
  - `OnsetGrid`
  - harmony at `(bar, beat)`
  - section energy profile
- Repeat recognizably; variation is operator-driven (Stage 7), not note roulette.

---

## Stage 9 — Melody + lyric integration (optional major milestone after accompaniment)

**Why now:** you already have syllable/phonetic infrastructure; melody requires rhythmic windows.

### Story 9.1 — Syllable timing windows → onset slot mapping

**Acceptance criteria:**
- Convert syllable count/stress into a set of candidate onset slots per phrase.
- Support “stretch” (melisma) only if there’s space.

### Story 9.2 — Melody generator MVP

**Acceptance criteria:**
- Strong beats: chord tones
- Weak beats: controlled passing tones (policy gated)
- Range driven by voice profile

### Story 9.3 — Arrangement collision avoidance

**Acceptance criteria:**
- When melody exists:
  - comp simplifies / drops density under vocal phrases
  - pads move away in register
  - bass avoids too much rhythmic density under dense lyric phrases

---

## Bottom line

- **Yes:** keep improving `Generator` incrementally.
- **No:** don’t build “hooks/riffs system” first; it will be premature and you’ll rewrite it.
- Use sections now to drive identity and variation; otherwise you’re just generating a loop.
- “Surprise” should come from structured transforms and section contrast, with RNG only as a tie-breaker among valid options.
