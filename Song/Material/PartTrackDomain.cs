// AI: purpose=Define time semantics for PartTrackEvent.AbsoluteTimeTicks (song absolute vs material local).
// AI: invariants=SongAbsolute=absolute song ticks; MaterialLocal=local ticks from fragment start (>=0).
// AI: deps=Referenced by PartTrackMeta to disambiguate AbsoluteTimeTicks meaning across song tracks vs material fragments.

namespace Music.Song.Material;

/// <summary>
/// Time semantics for PartTrackEvent.AbsoluteTimeTicks.
/// CRITICAL: AbsoluteTimeTicks is always "absolute" within its domain:
///   - SongAbsolute: ticks from song start (global timeline)
///   - MaterialLocal: ticks from fragment start (local timeline, always >= 0)
/// </summary>
public enum PartTrackDomain
{
    /// <summary>
    /// Events are in absolute song time (ticks from song start).
    /// </summary>
    SongAbsolute = 0,

    /// <summary>
    /// Events are in local material time (ticks from fragment start, always >= 0).
    /// </summary>
    MaterialLocal = 1
}
