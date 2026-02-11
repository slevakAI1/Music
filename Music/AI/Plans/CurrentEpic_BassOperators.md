# Epic: Bass Phrase Operators

## Goal

Implement 18 bass phrase operators so `BassPhraseGenerator` randomly selects and applies N operators to pitched anchor onsets, mirroring how `DrumPhraseGenerator` works with drum operators. After this epic, generating a bass phrase produces varied, harmony-aware bass lines.

## Scope

- 18 operators across 5 families (FoundationVariation, HarmonicTargeting, RhythmicPlacement, DensityAndSubdivision, RegisterAndContour)
- Cleanup/Constraints operators: **deferred**
- Humanization operators: **deferred**
- No unit tests
- No architecture changes — operators extend `OperatorBase`, register in `BassOperatorRegistryBuilder`, selected by `BassOperatorApplicator`

## Family-to-OperatorFamily Mapping

| Plan Family | OperatorFamily Enum | Folder |
|---|---|---|
| FoundationVariation | PatternSubstitution | PatternSubstitution/ |
| HarmonicTargeting | MicroAddition | MicroAddition/ |
| RhythmicPlacement | SubdivisionTransform | SubdivisionTransform/ |
| DensityAndSubdivision | PhrasePunctuation | PhrasePunctuation/ |
| RegisterAndContour | StyleIdiom | StyleIdiom/ |

## Shared Helper

Several operators need the same harmony/pitch/duration helpers. A single static helper class avoids duplication.

---

## Story 0: Create BassOperatorHelper

**Files:** `Music/Generator/Bass/Operators/BassOperatorHelper.cs` (new)

Create `BassOperatorHelper` static class with shared methods used by multiple operators:

- `GetChordRootMidiNote(SongContext, int barNumber, decimal beat, int baseOctave)` → `int?` — looks up harmony event, returns chord root MIDI note via `ChordVoicingHelper`, or null if harmony missing.
- `GetChordToneMidiNotes(SongContext, int barNumber, decimal beat, string voicing, int baseOctave)` → `List<int>` — returns full chord voicing (root, 3rd, 5th, 7th) as MIDI notes.
- `GetNextOnsetBeat(Bar bar, decimal currentBeat, IReadOnlyList<decimal> anchorBeats)` → `decimal?` — returns next anchor beat in same bar or null if last.
- `DurationTicksToNextBeat(BarTrack barTrack, int barNumber, decimal currentBeat, decimal? nextBeat)` → `int` — tick distance from current beat to next beat (or bar end).
- `ClampToRange(int midiNote, int minNote, int maxNote)` → `int` — octave-shifts note into range.
- `IsStrongBeat(decimal beat)` → `bool` — returns true for beats 1 and 3 (4/4 assumption).
- `GetBassAnchorBeats(SongContext songContext, int barNumber)` → `IReadOnlyList<decimal>` — gets bass anchor beats from groove preset.

**AC:**
- Static class in `Music.Generator.Bass.Operators` namespace.
- All methods are static and guard null inputs.

---

## Story 1: BassPedalRootBarOperator

**Files:** `Music/Generator/Bass/Operators/PatternSubstitution/BassPedalRootBarOperator.cs` (new)

Converts each bar to a single sustained root note covering the full bar. Yields one candidate at beat 1 with chord root pitch and duration spanning to bar end. Also yields removals for all other bass beats in the bar.

**AC:**
- OperatorId: `BassPedalRootBar`
- OperatorFamily: `PatternSubstitution`
- `GenerateCandidates`: one candidate at beat 1 with root pitch, duration = bar length in ticks.
- `GenerateRemovals`: removal candidates for all bass anchor beats except beat 1.

---

## Story 2: BassRootFifthOstinatoOperator

**Files:** `Music/Generator/Bass/Operators/PatternSubstitution/BassRootFifthOstinatoOperator.cs` (new)

Rewrites anchor pitches into root–5th alternation. Even-indexed onsets get chord root, odd-indexed get perfect 5th (7 semitones above root), octave-clamped to range.

**AC:**
- OperatorId: `BassRootFifthOstinato`
- OperatorFamily: `PatternSubstitution`
- Alternates root/5th per onset index within bar.
- Clamps notes to [28, 52] via `BassOperatorHelper.ClampToRange`.

---

## Story 3: BassChordTonePulseOperator

**Files:** `Music/Generator/Bass/Operators/PatternSubstitution/BassChordTonePulseOperator.cs` (new)

Replaces anchor pitches with rotating chord tones (R, 3, 5, 7) to imply richer harmony.

**AC:**
- OperatorId: `BassChordTonePulse`
- OperatorFamily: `PatternSubstitution`
- Uses `BassOperatorHelper.GetChordToneMidiNotes` to get available chord tones.
- Cycles through tones by onset index within bar.
- Keeps existing `DurationTicks` unchanged.

---

## Story 4: BassPedalWithTurnaroundOperator

**Files:** `Music/Generator/Bass/Operators/PatternSubstitution/BassPedalWithTurnaroundOperator.cs` (new)

Pedals root for most of the bar, then adds 2-3 approach notes on beat 4 leading into next bar's chord root. Skips if bar is last bar in generation.

**AC:**
- OperatorId: `BassPedalWithTurnaround`
- OperatorFamily: `PatternSubstitution`
- Candidate at beat 1: root, duration to beat 4.
- 2-3 candidates at beats 4.0, 4.5 (8th subdivision): stepwise chromatic approach into next bar's root.
- Looks ahead to bar+1 harmony for target pitch. If same chord, uses leading tone approach.
- Skips turnaround if bar is last bar (no next harmony available).

---

## Story 5: BassTargetNextChordRootOperator

**Files:** `Music/Generator/Bass/Operators/MicroAddition/BassTargetNextChordRootOperator.cs` (new)

On chord changes within a bar, forces the first onset after the change to the new chord root.

**AC:**
- OperatorId: `BassTargetNextChordRoot`
- OperatorFamily: `MicroAddition`
- Samples harmony at each anchor beat; detects when chord changes from previous beat.
- At change point: yields candidate with new root pitch overriding existing onset.

---

## Story 6: BassApproachNoteOperator

**Files:** `Music/Generator/Bass/Operators/MicroAddition/BassApproachNoteOperator.cs` (new)

Adds a chromatic or diatonic approach note 0.5 beats before beat 1 targets (or chord-change targets).

**AC:**
- OperatorId: `BassApproachNote`
- OperatorFamily: `MicroAddition`
- Inserts candidate at (targetBeat - 0.5) with pitch = target ± 1 semitone (bias downward).
- Duration = 8th note ticks.
- Skips if approach beat < 1.0 (bar start).

---

## Story 7: BassEnclosureOperator

**Files:** `Music/Generator/Bass/Operators/MicroAddition/BassEnclosureOperator.cs` (new)

Adds a 2-note enclosure (above then below) before a target onset, typically on beat 1 or last onset.

**AC:**
- OperatorId: `BassEnclosure`
- OperatorFamily: `MicroAddition`
- Selects one target per bar (prefer beat 1).
- Inserts two 16th-note candidates before target: (target+1 semitone) then (target-1 semitone).
- Skips if insufficient space before target (< 0.5 beats).

---

## Story 8: BassGuideToneEmphasisOperator

**Files:** `Music/Generator/Bass/Operators/MicroAddition/BassGuideToneEmphasisOperator.cs` (new)

On weak beats, replaces pitch with chord 3rd or 7th; keeps root on strong beats.

**AC:**
- OperatorId: `BassGuideToneEmphasis`
- OperatorFamily: `MicroAddition`
- Strong beats (1, 3): yield candidate with root pitch.
- Weak beats (2, 4, offbeats): yield candidate with 3rd or 7th from chord tones.
- Falls back to 5th if chord has fewer than 3 tones.

---

## Story 9: BassStepwiseVoiceLeadingOperator

**Files:** `Music/Generator/Bass/Operators/MicroAddition/BassStepwiseVoiceLeadingOperator.cs` (new)

Minimizes leaps between consecutive onsets by choosing the closest octave variant of each chord tone.

**AC:**
- OperatorId: `BassStepwiseVoiceLeading`
- OperatorFamily: `MicroAddition`
- For each onset: generates chord-tone candidates across octaves 1-3.
- Picks candidate with smallest semitone distance from previous onset's pitch.
- Yields candidate with adjusted pitch. Keeps duration unchanged.

---

## Story 10: BassAnticipateDownbeatOperator

**Files:** `Music/Generator/Bass/Operators/SubdivisionTransform/BassAnticipateDownbeatOperator.cs` (new)

Moves beat-1 onset earlier by 0.5 beats (anticipation), creating a push feel.

**AC:**
- OperatorId: `BassAnticipateDownbeat`
- OperatorFamily: `SubdivisionTransform`
- For bars > 1: yields candidate at beat 0.5 (i.e., the "and of 4" of previous bar semantically, but placed at current bar beat 0.5 — or implement as end-of-previous-bar beat 4.5).
- Yields removal of the original beat-1 onset.
- Keeps same pitch and shorter duration.

---

## Story 11: BassSyncopationSwapOperator

**Files:** `Music/Generator/Bass/Operators/SubdivisionTransform/BassSyncopationSwapOperator.cs` (new)

Swaps one on-beat onset with an off-beat onset for moderate syncopation.

**AC:**
- OperatorId: `BassSyncopationSwap`
- OperatorFamily: `SubdivisionTransform`
- Picks one strong-beat onset (beat 2 or 4, never beat 1).
- Yields removal for that onset.
- Yields candidate at nearest offbeat (e.g., 2.5 or 3.5) with same pitch.

---

## Story 12: BassKickLockOperator

**Files:** `Music/Generator/Bass/Operators/SubdivisionTransform/BassKickLockOperator.cs` (new)

Adds bass onsets aligned to kick drum anchor beats where bass is not already present.

**AC:**
- OperatorId: `BassKickLock`
- OperatorFamily: `SubdivisionTransform`
- Gets kick onsets from `SongContext.GroovePresetDefinition.AnchorLayer.KickOnsets`.
- For each kick beat not already a bass anchor beat: yields candidate with chord root pitch, 8th-note duration.
- Caps additions to avoid exceeding 8 onsets per bar.

---

## Story 13: BassRestStrategicSpaceOperator

**Files:** `Music/Generator/Bass/Operators/SubdivisionTransform/BassRestStrategicSpaceOperator.cs` (new)

Removes one weak-beat onset per bar to create intentional space.

**AC:**
- OperatorId: `BassRestStrategicSpace`
- OperatorFamily: `SubdivisionTransform`
- Removal-only operator (`GenerateCandidates` returns empty).
- Targets one weak-beat onset (beat 2 or 4, or offbeats) per bar.
- Never removes beat 1.
- Skips bars with ≤ 2 existing onsets.

---

## Story 14: BassPickupIntoNextBarOperator

**Files:** `Music/Generator/Bass/Operators/SubdivisionTransform/BassPickupIntoNextBarOperator.cs` (new)

Adds a short pickup note at beat 4.5 leading into next bar's chord root.

**AC:**
- OperatorId: `BassPickupIntoNextBar`
- OperatorFamily: `SubdivisionTransform`
- Skips last bar.
- Looks ahead to bar+1 harmony for target pitch.
- Yields candidate at beat 4.5, pitch = target-1 semitone (chromatic approach below), 8th-note duration.

---

## Story 15: BassSplitLongNoteOperator

**Files:** `Music/Generator/Bass/Operators/PhrasePunctuation/BassSplitLongNoteOperator.cs` (new)

Splits one long sustained note into repeated shorter notes (same pitch) to create pulse.

**AC:**
- OperatorId: `BassSplitLongNote`
- OperatorFamily: `PhrasePunctuation`
- Finds one onset with duration ≥ 2 quarter notes (960 ticks).
- Yields 2-4 candidates splitting the duration into 8th-note slices (same pitch).
- Yields removal of original onset.

---

## Story 16: BassAddPassingEighthsOperator

**Files:** `Music/Generator/Bass/Operators/PhrasePunctuation/BassAddPassingEighthsOperator.cs` (new)

Between two existing onsets separated by ≥ 1 beat, inserts passing eighth notes stepping toward the next pitch.

**AC:**
- OperatorId: `BassAddPassingEighths`
- OperatorFamily: `PhrasePunctuation`
- Scans consecutive bass anchor beat pairs.
- For gaps ≥ 1.0 beat: inserts 8th-note candidates at 0.5-beat increments stepping 1-2 semitones toward next pitch.
- If pitches are equal, inserts repeated root pulses.
- Caps to 10 onsets per bar.

---

## Story 17: BassReduceToQuarterNotesOperator

**Files:** `Music/Generator/Bass/Operators/PhrasePunctuation/BassReduceToQuarterNotesOperator.cs` (new)

Simplifies a bar into quarter-note grid (beats 1, 2, 3, 4) using chord root.

**AC:**
- OperatorId: `BassReduceToQuarterNotes`
- OperatorFamily: `PhrasePunctuation`
- Yields removal for all existing bass onsets.
- Yields 4 candidates at beats 1-4, each with chord root pitch, quarter-note duration.

---

## Story 18: BassBurstSixteenthsOperator

**Files:** `Music/Generator/Bass/Operators/PhrasePunctuation/BassBurstSixteenthsOperator.cs` (new)

Adds a 3-4 note 16th-note burst on beat 4, alternating root and octave.

**AC:**
- OperatorId: `BassBurstSixteenths`
- OperatorFamily: `PhrasePunctuation`
- Yields 4 candidates at beats 4.0, 4.25, 4.5, 4.75.
- Pitch alternates: root, root+12, root, root+12. Clamped to range.
- Duration = 16th note ticks (120).

---

## Story 19: BassOctavePopAccentsOperator

**Files:** `Music/Generator/Bass/Operators/StyleIdiom/BassOctavePopAccentsOperator.cs` (new)

On strong beats, jumps pitch up one octave for accent, then returns.

**AC:**
- OperatorId: `BassOctavePopAccents`
- OperatorFamily: `StyleIdiom`
- For each strong-beat onset (beat 1 or 3): yields candidate with MidiNote + 12.
- Skips if result > 55 (max range).
- Uses deterministic selection based on bar number (not every bar).

---

## Story 20: BassRangeClampOperator

**Files:** `Music/Generator/Bass/Operators/StyleIdiom/BassRangeClampOperator.cs` (new)

Enforces playable range [28, 55] by octave-shifting notes. Safety operator.

**AC:**
- OperatorId: `BassRangeClamp`
- OperatorFamily: `StyleIdiom`
- For each bass anchor beat: yields candidate with clamped pitch via `BassOperatorHelper.ClampToRange`.
- Only yields if pitch actually changed.

---

## Story 21: BassContourSmootherOperator

**Files:** `Music/Generator/Bass/Operators/StyleIdiom/BassContourSmootherOperator.cs` (new)

Reduces large leaps (>9 semitones) between consecutive onsets by re-octaving.

**AC:**
- OperatorId: `BassContourSmoother`
- OperatorFamily: `StyleIdiom`
- Walks consecutive bass anchor beats.
- If absolute pitch distance > 9 semitones: tries ±12 on current note, picks closest to previous that stays in range.
- Yields candidate with adjusted pitch.

---

## Story 22: Register All Operators

**Files:** `Music/Generator/Bass/Operators/BassOperatorRegistryBuilder.cs` (modify)

Register all 18 new operators plus update using directives. Remove the retired `BassChordRootHalfNoteOperator` file.

**AC:**
- All operators registered in deterministic family order.
- `BassChordRootHalfNoteOperator.cs` deleted.
- Build succeeds.

---

## Story 23: Verify End-to-End

Manual verification only.

**AC:**
- Solution builds clean.
- Bass Phrase button on WriterForm generates a bass phrase with operator variation applied.
