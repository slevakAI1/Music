# Story 3: Refactor call sites to canonical SongContext sources

## Summary
- Moved drum context to use canonical `Bar` and derived energy while keeping motif access on `DrummerContext`.
- Updated drum operators and candidate source to use canonical bar data and motif map with the new multiplier signature.
- Aligned `AgentContext` tests with the reduced contract and rebuilt successfully.

## Tests
- `dotnet build`
