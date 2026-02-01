# Story 2.1 Save Selected to Material Bank

## Summary
- Added save-to-bank handling in `WriterFormGridOperations` to convert selected tracks into `MaterialPhrase` entries.
- Wired `WriterForm` save button to invoke save-to-bank and persist `Globals.SongContext`.
- Added localized resource strings for save-to-bank messages.

## Tests
- `dotnet build`
