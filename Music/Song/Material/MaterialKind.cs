// AI: purpose=Optional classification of fragment/variant type for UI filtering and categorization.
// AI: invariants=Unknown=0 is safe default; values are stable for serialization.
// AI: deps=Referenced by PartTrackMeta; used for MaterialBank filtering.

namespace Music.Song.Material;

/// <summary>
/// Optional classification of the fragment/variant type (used for filtering in UI later).
/// </summary>
public enum MaterialKind
{
    Unknown = 0,
    Riff = 1,
    Hook = 2,
    MelodyPhrase = 3,
    DrumFill = 4,
    BassFill = 5,
    CompPattern = 6,
    KeysPattern = 7,
    DrumPhrase = 8
}
