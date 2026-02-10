# Story 2: Remove redundant properties from `AgentContext`

## Summary
- Removed song-level properties from `AgentContext`, leaving only seed and RNG stream key.
- Simplified `CreateMinimal` to use seed and a bar-number-based RNG stream key only.
- Cleaned up `AgentContext` file comments and usings to match the reduced contract.

## Tests
- Not run (Story 2 is a breaking change; epic guidance defers unit test updates).
