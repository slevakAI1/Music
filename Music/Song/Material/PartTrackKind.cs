// AI: purpose=Categorize PartTrack intent: song role track vs reusable material fragment/variant.
// AI: invariants=RoleTrack normally pairs with SongAbsolute domain; MaterialFragment/MaterialVariant with MaterialLocal.
// AI: deps=Referenced by PartTrackMeta; used for filtering in MaterialBank and future UI.

namespace Music.Song.Material;

/// <summary>
/// Intent/category of a track.
/// </summary>
public enum PartTrackKind
{
    /// <summary>
    /// A rendered role track intended to be played as part of the song (Bass/Comp/Keys/Drums/etc).
    /// </summary>
    RoleTrack = 0,

    /// <summary>
    /// A reusable template-like fragment (riff/hook/melody phrase etc) in local time.
    /// </summary>
    MaterialFragment = 1,

    /// <summary>
    /// A realized/transformed output derived from a fragment (still local time).
    /// </summary>
    MaterialVariant = 2
}
