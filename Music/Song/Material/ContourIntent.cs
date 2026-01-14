// AI: purpose=Melodic/rhythmic direction intent for motif rendering; consumed by Stage 9 renderer
// AI: invariants=Immutable enum; semantics: Up=rising, Down=falling, Arch=rise-then-fall, Flat=horizontal, ZigZag=alternating
namespace Music.Song.Material;

public enum ContourIntent
{
    Up,
    Down,
    Arch,
    Flat,
    ZigZag
}
