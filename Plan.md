# Music Generator Agile Plan (updated from current code)

## Current state (what exists today)

- **Song design-time context:** `SongContext` wires together `HarmonyTrack`, `GrooveTrack`, `SectionTrack`, `Timingtrack`, derived `BarTrack`, plus `Song` output.
- **Harmony vocabulary + validation:**
  - Canonical short chord qualities exist via `ChordQuality`.
  - UI editor `HarmonyEditorForm` displays long names but **stores short names**.
  - `HarmonyEventNormalizer` and `HarmonyValidator` already exist (ordering, key parse, degree 1–7, quality validity, bass validity, optional diatonic checks).
- **Timing / ticks:** `Timingtrack` provides time signatures by bar; `BarTrack.RebuildFromTimingTrack` derives bar start/end ticks; `BarTrack.ToTick(bar, onsetBeat)` converts 1-based quarter-note beat units to absolute ticks.
- **Generation (MVP):** `Generator.Core.Generator` produces monophonic bass + guitar, block-chord keys, and simple drums driven by groove onsets, with constrained randomness (`PitchRandomizer`).
- **Export:** MIDI conversion pipeline works and assumes absolute event ticks.

## End goal

Generate real music that resembles skilled human composition and performance:
- coherent structure (sections, phrases, repetition)
- idiomatic rhythm (groove-aware, with intentional variations)
- idiomatic harmony (harmonic rhythm, voice-leading, chord color)
- idiomatic parts (basslines, comping/voicings, drums, melodies)
- deterministic “choices with intent” (randomness as a tie-breaker, not note roulette)

---

# Stage 1 — Make musical time + authority explicit (foundation)

**Why:** generation currently depends on `SectionTrack.TotalBars` and bar-only harmony activation; onset duration logic is duplicated; future realism needs a single time model.

## Story 1.1 — Make `SectionTrack` the sole authority for song length - COMPLETED

**Intent:** remove ambiguity: the song’s total bar count is defined by the arranged sections, not by how many notes exist in any track.

Acceptance criteria:
- Define and enforce the invariant: **song length = sum of section lengths**.
  - In code terms: `Song.TotalBars == SongContext.SectionTrack.TotalBars`.

COMPLETED - code updated	
	
- If `SectionTrack.Sections.Count == 0`, treat the song as **not arranged** (invalid for generation/export), and surface a clear, actionable error.

COMPLETED - code updated


- Rebuild tick ruler from this authority:
  - `SongContext.BarTrack.RebuildFromTimingTrack(song.TimeSignatureTrack, totalBars: Song.TotalBars)`.

N/A - Generators should be able to rely on `BarTrack` for accurate bar start/end ticks, and `Timingtrack` for time signature info.

- - Enforce the “no notes outside the song form” rule:
  - Generators must not emit events whose `absoluteTimeTicks` are outside `[0, endTickOfLastBar)`.
  - Editors/importers should validate and/or trim notes that fall outside the arranged bars.

N/A - Generators will have access to section/bar/tick info to avoid/enfore this; the timing track editor already rebuilds upon editing
so the generators can rely that the `BarTrack` is always accurate;

===============================================================================================================

## Story 1.1b — Track per-part bar coverage (progress) moved to backlog

The detailed per-part coverage story has been deferred and moved to `Backlog.md`. See `Backlog.md` for the full deferred story and acceptance criteria.

=============================================================================================================== 

## Story 1.2 — Harmony activation by bar + beat (not bar-only) - DONE

**Intent:** unlock mid-bar harmony changes, anticipations, and more realistic harmonic rhythm.

Acceptance criteria:
- Update HarmonyTrack.GetActiveHarmonyEvent to work with beat parameter: `HarmonyTrack.GetActiveHarmonyEvent(int bar, decimal beat)` (beat is 1-based, quarter-note units to match groove onsets).
- Ordering invariant remains: `StartBar`, then `StartBeat`.
- Update generator to query active harmony at each onset (`bar`, `onsetBeat`).

=============================================================================================================== 

## Story 1.3 — Centralize onset/duration tick math

**Intent:** eliminate repeated “next onset else end of measure” logic and make durations consistent across instruments.

Acceptance criteria:
- Introduce an `OnsetGrid` (or `OnsetSlot`) model:
  - Input: bar number, list of onset beats, `BarTrack`.
  - Output: list of `OnsetSlot { Bar, OnsetBeat, StartTick, EndTick, DurationTicks, IsStrongBeat }`.
- Refactor bass/guitar/keys/drums generators to consume `OnsetGrid`.
- Edge cases handled:
  - duplicates/sortedness of onsets (either normalize or validate + fail fast)
  - onsets outside the bar return errors/are rejected consistently.

## Story 1.4 — Groove onset normalization + diagnostics

**Intent:** groove presets are raw lists; generation should be robust and produce actionable diagnostics.

Acceptance criteria:
- Add `GrooveOnsetNormalizer.NormalizeLayer(GrooveInstanceLayer layer, int beatsPerBar)`.
- Normalization operations:
  - sort ascending
  - distinct within tolerance (exact decimal equality is fine for now)
  - remove invalid (<1 or >= beatsPerBar+1)
- Generator uses normalized onsets.
- Unit tests for normalization edge cases.

---

# Stage 2 — Upgrade chord/scale policy into an explicit “Harmony Policy”

**Why:** the code already has `HarmonyValidator` and partial diatonic logic; “real music” needs explicit, configurable treatment of non-diatonic tones (borrowed chords, secondary dominants).

## Story 2.1 — Define `HarmonyPolicy` used by validator + pitch context

Acceptance criteria:
- Add `HarmonyPolicy` (settings object) with options such as:
  - `AllowNonDiatonicChordTones` (default false for now)
  - `AllowSecondaryDominants` (future)
  - `AllowBorrowedChords` (future)
  - `StrictChordToneScaleMembership` (current Stage-1 behavior)
- `HarmonyValidator.ValidateTrack` takes `HarmonyPolicy` (or embeds into existing `HarmonyValidationOptions`).

## Story 2.2 — Produce a structured diagnostics report

Acceptance criteria:
- Introduce `HarmonyDiagnostics`:
  - `Errors[]`, `Warnings[]`, and **per-event** diagnostics (bar:beat, summary).
- `HarmonyEditorForm` can display advisory messages from diagnostics (existing “Adv” column is a good hook).

## Story 2.3 — Make chord tone generation policy-aware

Acceptance criteria:
- Update `HarmonyPitchContextBuilder`/`ChordVoicingHelper` usage to allow policy-based chord tone sets.
- When `AllowNonDiatonicChordTones=false`, ensure chord tone pitch classes are in scale (as today).
- When enabled later, expand scale membership checks accordingly.

---

# Stage 3 — Harmonic rhythm + voice-leading (keys/pads realism)

**Why:** keys currently emit static chord blocks; realism requires voice-leading, inversions, and controlled chord color.

## Story 3.1 — Add a `ChordRealization` model

Acceptance criteria:
- Define `ChordRealization { IReadOnlyList<int> MidiNotes, string Inversion, bool HasAddedColor, ... }`.
- `PitchRandomizer.SelectKeysVoicing` evolves to return `ChordRealization`.

## Story 3.2 — Voice-leading selector for consecutive chords

Acceptance criteria:
- Implement `VoiceLeadingSelector` that chooses inversion/voicing to minimize total movement from previous chord.
- Integrate into keys generation (store previous voicing per track).
- Add tests: consecutive chords should not jump unnecessarily.

## Story 3.3 — Chord color policy per section

Acceptance criteria:
- Introduce `SectionColorProfile` (per `SectionType` or per section instance):
  - probability of add9/add11/6
  - max chord density (3–5 notes)
  - register preferences
- Keys generation uses this profile rather than hardcoded add9 probability.

---

# Stage 4 — Make comp into comp (chords/partials, not a lead line)

**Why:** guitar comp is currently monophonic pitch-picking; real comp uses chord fragments, guide tones, register/voicing rules.

## Story 4.1 — Introduce part “roles” explicitly

Acceptance criteria:
- Add a simple enum/config: `PartRole` (e.g., `Bass`, `Comp`, `Pads`, `Lead`, `Drums`).
- `Generator.Generate` maps tracks to roles.
- Groove onsets map to roles (`CompOnsets`, `PadsOnsets`, etc.).

## Story 4.2 — Comp voicing fragment selection

Acceptance criteria:
- Add `CompVoicingSelector.SelectVoicing(ctx, onset, previousVoicing)` -> `List<int>` MIDI notes.
- Rules:
  - strong beats include guide tones (3rd/7th when available)
  - allow root omission
  - limit to 2–4 notes
  - avoid muddy close voicings below threshold
- Use for guitar comp and optionally for keys comp (if split later).

## Story 4.3 — Strumming / articulation (MVP)

Acceptance criteria:
- Add optional micro-offsets within a chord (very small tick offsets) for strum/roll.
- Must remain deterministic from seed.

---

# Stage 5 — Bassline patterns (from safe roots to musical lines)

**Why:** current bass is mostly root/fifth/octave; need pattern libraries and phrase-aware development.

## Story 5.1 — Bass pattern library keyed by groove + section type

Acceptance criteria:
- Create `BassPattern` definitions:
  - root-only
  - root–fifth
  - approach-note (diatonic neighbor) on weak beats (policy-gated)
  - octave pop
- Selection uses deterministic rules first, RNG only among equivalent candidates.

## Story 5.2 — Phrase-aware bassline variation

Acceptance criteria:
- Detect phrase boundaries from sections (and later motifs).
- Add simple “pickup” behavior into section transitions.

---

# Stage 6 — Variation engine (controlled transformations)

**Why:** realism comes from structured repetition with controlled variation.

## Story 6.1 — Variation operators

Acceptance criteria:
- Introduce deterministic transforms:
  - rhythm: drop hit, add anticipation, shift by 0.5 beat
  - pitch: swap chord tone role, add/remove diatonic non-chord tone on weak beats
  - register: lift chorus, widen voicings
- Operators must be composable and traceable (loggable).

## Story 6.2 — Section energy profile drives variation

Acceptance criteria:
- Add `SectionEnergyProfile` (density, register, velocity, rhythmic busyness).
- Apply to each role.

---

# Stage 7 — Motifs / hooks as first-class objects

**Why:** after accompaniment is competent, motifs provide identity.

## Story 7.1 — Motif model and placement policy

Acceptance criteria:
- Model `Motif { Id, Role, TargetSections, RhythmShape, ContourIntent, Constraints }`.
- Placement rules: hook in chorus; riff in intro/verse depending on style.

## Story 7.2 — Motif renderer

Acceptance criteria:
- Render motif using:
  - `OnsetGrid`
  - harmony context (bar+beat)
  - variation operators
- Motif should repeat recognizably with small controlled variation.

---

# Stage 8 — Melody/lead + lyric integration (optional next major)

**Why:** vocals/leads dominate arrangement; accompaniment must make space.

## Story 8.1 — Syllable-to-onset mapping

Acceptance criteria:
- Use `LyricTrack` syllables/phonetics to derive note placement windows.

## Story 8.2 — Melody generator (MVP)

Acceptance criteria:
- Melody primarily uses chord tones on strong beats, controlled passing tones on weak beats.
- Register and range driven by voice/instrument profile.

## Story 8.3 — Arrangement collision avoidance

Acceptance criteria:
- When a lead is present:
  - comp simplifies under the lead
  - pads register shifts away from lead
  - bass avoids conflicting rhythmic density

---

## Notes on implementation style

- Prefer **small, test-backed** increments.
- Keep `BarTrack` a derived helper (timing authority remains `Timingtrack`).
- Keep randomness deterministic (`RandomizationSettings.Seed`) and use RNG only as tie-breaker among valid musical options.
