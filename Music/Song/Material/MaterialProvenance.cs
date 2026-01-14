// AI: purpose=Provenance metadata for future locking/regenerate; stores seed and transform info.
// AI: invariants=All fields have safe defaults; SourceFragmentId nullable for non-derived tracks.
// AI: deps=Referenced by PartTrackMeta; TransformTags stable for serialization; PartTrackId is nested in PartTrack.

using Music.Generator;

namespace Music.Song.Material;

/// <summary>
/// Optional provenance details. Not used by generation yet.
/// Exists so later locking/regenerate can rely on stable metadata without refactors.
/// </summary>
public sealed record MaterialProvenance
{
    public int BaseSeed { get; init; }

    public int DerivedSeed { get; init; }

    public int AttemptIndex { get; init; }

    /// <summary>
    /// If this is a variant derived from a fragment, this links back to the fragment id.
    /// </summary>
    public PartTrack.PartTrackId? SourceFragmentId { get; init; }

    /// <summary>
    /// Small stable tags describing transforms used (e.g. "Invert", "Syncopate", "OctaveUp").
    /// </summary>
    public IReadOnlyList<string> TransformTags { get; init; } = [];
}
