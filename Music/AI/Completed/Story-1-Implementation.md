# Story 1 Implementation Summary

## Goal
Make `OperatorBase`, `OperatorCandidateAddition`, and `OperatorCandidateRemoval` instrument-agnostic while keeping behavior intact.

## Changes Made

### 1. Core types are instrument-agnostic
- `OperatorCandidateAddition` keeps only cross-instrument fields and `Metadata` for adapter-provided data.
- `OperatorCandidateRemoval` is in `Music.Generator.Core` and has no instrument references.
- `OperatorBase` no longer references instrument-specific types or parameters.
- All core comments and signatures avoid instrument-specific wording.

### 2. Instrument adapter introduced
- Added `IOperatorCandidateInstrumentAdapter` in core.
- `OperatorBase.CreateCandidate` uses adapter to obtain `Metadata` and optional discriminator.
- `OperatorBase.DefaultInstrumentAdapter` allows parameterless operator construction without core knowing instruments.

### 3. Instrument layer wiring (drums)
- Added `DrumCandidateData` and `DrumOperatorCandidateInstrumentAdapter` in drums layer.
- `DrumOperatorRegistryBuilder` sets `OperatorBase.DefaultInstrumentAdapter` before operator creation.
- Updated drum operators to pass `DrumCandidateData` into `CreateCandidate`.
- Existing mapping/applicator logic continues to read metadata via existing extensions.

## Validation

- `dotnet build` succeeds.
- No occurrences of instrument-specific wording in core operator types.

## Behavior Preservation
- Candidate IDs remain deterministic.
- Operator scoring and selection behavior unchanged.

