# Idea: Pitched Anchor Pattern for Bass (and Future Parts)

## Summary

Move chord-root pitch assignment from operator (`BassChordRootHalfNoteOperator`) into `BassPhraseGenerator.ExtractAnchorOnsets`, so bass anchors leave the extraction step already pitched and with duration. Operators then act on a musically complete baseline rather than rhythm-only stubs.

## Current State

### Drums (reference pattern)
- `DrumPhraseGenerator.ExtractAnchorOnsets` pulls kick/snare/hat beats from groove anchor layer.
- Each onset already maps to a playable MIDI note via `MapRoleToMidiNote` (role → fixed note).
- Operators add/remove/modify onsets on top of a **playable** foundation.

### Bass (current)
- `BassPhraseGenerator.ExtractAnchorOnsets` pulls bass beats from groove anchor layer.
- Onsets are **rhythm-only**: `MidiNote = null`, `DurationTicks = null`.
- `ConvertToPartTrack` skips any onset still missing `MidiNote`.
- `BassChordRootHalfNoteOperator` is the only source of pitch today. It walks bass anchor beats, looks up `HarmonyTrack.GetActiveHarmonyEvent(bar, beat)`, and sets `MidiNote` to chord root + `DurationTicks` to half-note length.
- If that operator is not selected by the random applicator, the bass track can be **empty** — no notes survive conversion.

## Proposed Change

### What moves

In `BassPhraseGenerator.ExtractAnchorOnsets`:
1. After creating each rhythm-only onset, immediately look up `HarmonyTrack.GetActiveHarmonyEvent(bar, beat)`.
2. Set `MidiNote` to chord root of the active harmony (octave 2, root voicing via `ChordVoicingHelper`).
3. Set `DurationTicks` to span from this onset beat to the next anchor beat (or bar end if last in bar).

### What stays

- `BassChordRootHalfNoteOperator` can be **retired or repurposed** — its work is absorbed into the anchor step.
- `BassOperatorApplicator` continues to apply operators that modify/add/remove onsets. Operators can still override `MidiNote` and `DurationTicks` via the existing candidate-update path in the applicator.

### New dependency

`ExtractAnchorOnsets` needs access to `SongContext.HarmonyTrack`. Currently it only receives `GroovePresetDefinition`, `totalBars`, and `BarTrack`. The simplest option: pass `SongContext` (already available in `Generate`).

### Validation requirement

`BassPhraseGenerator.ValidateSongContext` should add a check that `SongContext.HarmonyTrack` is present and has events. Without harmony, pitched anchors cannot be created.

## Duration Calculation

The onset duration should extend from the current anchor beat to the next anchor beat within the same bar:

```
Anchor beats in bar: 1.0, 3.0   (typical half-note groove)
Onset at 1.0 → duration = (3.0 - 1.0) × TicksPerQuarterNote = 2 × 480 = 960 ticks (half note)
Onset at 3.0 → duration = (bar_end - 3.0) × TicksPerQuarterNote = 2 × 480 = 960 ticks (to bar boundary)
```

`BarTrack.ToTick(bar, beat)` already provides absolute tick positions, so duration = `nextOnsetTick - currentOnsetTick`. For the last onset in a bar, use `BarTrack.GetBarEndTick(bar) - currentOnsetTick`.

## Why This Is a Good Idea

1. **Playable baseline guaranteed.** The anchor step always produces audible notes. No operator luck required.
2. **Matches drum pattern.** Drums: anchor → playable → operators add complexity. Bass: same flow.
3. **Simplicity first, complexity via pipeline.** The initial bass phrase is the simplest musically valid statement (root notes, sustained). Operators add passing tones, octave jumps, rhythmic subdivisions, chromatic approaches, etc.
4. **Extensible to other parts.** Lead, comp, pads can follow the same anchor-pitch pattern: groove provides rhythm, harmony provides pitch foundation, operators add expression. The role-to-pitch mapping just differs per part (root for bass, voicing for comp, melody contour for lead).

## Risk / Considerations

| Area | Risk | Mitigation |
|------|------|------------|
| HarmonyTrack missing | Generator throws instead of producing empty track | Add clear validation message; UI already gates on timing track — gate on harmony too |
| Operator overlap | Operators that also set root pitch become redundant | Retire `BassChordRootHalfNoteOperator` or convert it to a no-op when anchor already has pitch |
| Duration accuracy | Bar-end calculation assumes contiguous bars | Already true — `BarTrack.GetBarEndTick` handles this |
| Groove presets without bass role | `GetOnsets(Bass)` returns empty list | Same as today — no anchors, no notes. Not a regression |
| Future parts (lead/comp) | Pitch logic is more complex than root note | Each part's generator owns its anchor-pitch strategy; bass is simplest case |

## Scope of Change

| File | Change |
|------|--------|
| `BassPhraseGenerator.cs` | `ExtractAnchorOnsets` signature adds `SongContext`; pitch + duration logic added inside loop |
| `BassPhraseGenerator.cs` | `ValidateSongContext` adds `HarmonyTrack` check |
| `BassChordRootHalfNoteOperator.cs` | Retire or convert to enhancement-only operator (e.g., octave variation) |
| `BassOperatorRegistryBuilder.cs` | Remove `BassChordRootHalfNoteOperator` registration if retiring |

## Unchanged

- `GrooveOnset` record — already has `MidiNote` and `DurationTicks` fields.
- `BassOperatorApplicator` — already handles candidate updates to existing onsets (pitch/duration override path).
- `ConvertToPartTrack` — still skips `MidiNote == null` onsets, but after this change none should remain unpitched.
- `HandleCommandCreateBassPhrase` — no UI changes needed.
- Drum pipeline — completely unaffected.
