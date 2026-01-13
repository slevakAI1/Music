// AI: purpose=Complete metadata for PartTrack: identity, domain/kind, role, provenance, tags.
// AI: invariants=Defaults produce valid song role track (SongAbsolute, RoleTrack); TrackId auto-generated.
// AI: deps=Aggregates PartTrack.PartTrackId, PartTrackDomain, PartTrackKind, MaterialKind, MaterialProvenance.

using Music.Generator;

namespace Music.Song.Material;

/// <summary>
/// The minimal metadata needed to safely treat PartTracks as either song tracks or material.
/// </summary>
public sealed record PartTrackMeta
{
    public PartTrack.PartTrackId TrackId { get; init; } = PartTrack.PartTrackId.NewId();

    /// <summary>
    /// Display name shown in grids/viewers (e.g., "Hook A1", "Melody Phrase V2 Attempt 3").
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Longer description; can be used by grid viewer details panel.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Intended musical role (matches VoiceSet role name conventions).
    /// Examples: "Comp", "Keys", "Lead", "Bass", "Drums".
    /// </summary>
    public string IntendedRole { get; init; } = "";

    public PartTrackDomain Domain { get; init; } = PartTrackDomain.SongAbsolute;

    public PartTrackKind Kind { get; init; } = PartTrackKind.RoleTrack;

    public MaterialKind MaterialKind { get; init; } = MaterialKind.Unknown;

    public MaterialProvenance? Provenance { get; init; }

    /// <summary>
    /// Optional user/system tags for searching/filtering later.
    /// </summary>
    public IReadOnlySet<string> Tags { get; init; } = new HashSet<string>();
}
