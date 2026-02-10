# Bass phrase generator: pitch and duration from operators

## Summary
Updated bass phrase generator and dependencies so that bass operators (when added) will set MIDI note pitch and duration per-onset, unlike drums which derive note from role mapping.

## Changes

### OperatorCandidateAddition (Core)
- Added `MidiNote` (int?) — optional MIDI note [0..127] for pitched instruments.
- Added `DurationTicks` (int?) — optional note duration in ticks.
- Added validation for both in `TryValidate()`.

### OperatorBase (Core)
- Added `SongContext` property — set by applicator before `GenerateCandidates`; gives operators access to `HarmonyTrack` and groove.
- Added `midiNote` and `durationTicks` optional params to `CreateCandidate()`.

### GrooveOnset (Groove)
- Added `MidiNote` (int?) and `DurationTicks` (int?) properties.

### BassOperatorApplicator
- `Apply()` now accepts optional `SongContext` and sets it on each operator before use.
- Propagates `MidiNote` and `DurationTicks` from candidates to `GrooveOnset`.

### BassPhraseGenerator
- `ExtractAnchorOnsets` now extracts bass-role onsets only (via `GetOnsets(GrooveRoles.Bass)`).
- `ConvertToPartTrack` uses `onset.MidiNote` and `onset.DurationTicks`; skips unpitched onsets.
- Removed `MapRoleToMidiNote` (drum-specific mapping).
- Passes `SongContext` through to `BassOperatorApplicator`.

## How bass operators will work
1. Operator receives `Bar` and `seed` via `GenerateCandidates(bar, seed)`.
2. Operator accesses `SongContext.HarmonyTrack` to resolve active harmony at bar/beat.
3. Operator accesses `SongContext.GroovePresetDefinition` to get bass groove onsets.
4. Operator creates candidates with `MidiNote` and `DurationTicks` set.
5. Applicator propagates pitch/duration through to `GrooveOnset` → `PartTrackEvent`.

## Tests
- `dotnet build` succeeds.
