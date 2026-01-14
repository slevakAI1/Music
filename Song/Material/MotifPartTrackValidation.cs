// AI: purpose=Validation helpers for motif-specific PartTrack constraints
// AI: invariants=Non-throwing validation; returns list of issues (empty = valid)
// AI: deps=Parallel to PartTrackMaterialValidation from Story M1
using Music.Generator;

namespace Music.Song.Material;

public static class MotifPartTrackValidation
{
    // AI: purpose=Validate motif PartTrack meets all constraints
    // AI: errors=Returns list of issue descriptions; empty list = valid
    public static IReadOnlyList<string> ValidateMotifTrack(PartTrack track)
    {
        ArgumentNullException.ThrowIfNull(track);
        
        var issues = new List<string>();

        // AI: Rule 1: Motif tracks must use MaterialLocal domain
        if (track.Meta.Domain != PartTrackDomain.MaterialLocal)
        {
            issues.Add($"Motif track '{track.Meta.Name}' must use MaterialLocal domain (was: {track.Meta.Domain})");
        }

        // AI: Rule 2: Motif tracks must have valid MaterialKind
        var validKinds = new[]
        {
            MaterialKind.Riff,
            MaterialKind.Hook,
            MaterialKind.MelodyPhrase,
            MaterialKind.DrumFill,
            MaterialKind.BassFill,
            MaterialKind.CompPattern,
            MaterialKind.KeysPattern
        };

        if (!validKinds.Contains(track.Meta.MaterialKind))
        {
            issues.Add($"Motif track '{track.Meta.Name}' has invalid MaterialKind: {track.Meta.MaterialKind}");
        }

        // AI: Rule 3: MaterialLocal tracks must not have negative ticks
        if (track.PartTrackNoteEvents.Any(e => e.AbsoluteTimeTicks < 0))
        {
            issues.Add($"Motif track '{track.Meta.Name}' has events with negative ticks (MaterialLocal requires ticks >= 0)");
        }

        return issues;
    }
}
