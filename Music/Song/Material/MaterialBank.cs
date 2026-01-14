// AI: purpose=In-memory container for reusable material fragments/variants; not connected to generation yet.
// AI: invariants=Keyed by PartTrack.PartTrackId; supports filtering by kind/materialKind/role; no external dependencies.
// AI: deps=Uses PartTrack.Meta for indexing; deliberately simple with no serialization logic.

using Music.Generator;

namespace Music.Song.Material;

/// <summary>
/// In-memory container for reusable material (fragments and variants).
/// Not connected to generation yet.
/// </summary>
public sealed class MaterialBank
{
    private readonly Dictionary<PartTrack.PartTrackId, PartTrack> _tracks = [];

    public IReadOnlyCollection<PartTrack> Tracks => _tracks.Values;

    public int Count => _tracks.Count;

    public IReadOnlyList<PartTrack> GetAll() => _tracks.Values.ToList();

    public void Add(PartTrack track)
    {
        ArgumentNullException.ThrowIfNull(track);
        _tracks[track.Meta.TrackId] = track;
    }

    public bool Remove(PartTrack.PartTrackId id) => _tracks.Remove(id);

    public bool Contains(PartTrack.PartTrackId id) => _tracks.ContainsKey(id);

    public bool TryGet(PartTrack.PartTrackId id, out PartTrack? track)
        => _tracks.TryGetValue(id, out track);

    public IReadOnlyList<PartTrack> GetByKind(PartTrackKind kind)
        => _tracks.Values.Where(t => t.Meta.Kind == kind).ToList();

    public IReadOnlyList<PartTrack> GetByMaterialKind(MaterialKind materialKind)
        => _tracks.Values.Where(t => t.Meta.MaterialKind == materialKind).ToList();

    public IReadOnlyList<PartTrack> GetByRole(string intendedRole)
        => _tracks.Values
            .Where(t => string.Equals(t.Meta.IntendedRole, intendedRole, StringComparison.OrdinalIgnoreCase))
            .ToList();

    // AI: Motif-specific query methods (Story 8.2)
    public IReadOnlyList<PartTrack> GetMotifsByRole(string intendedRole)
        => GetByRole(intendedRole)
            .Where(t => t.Meta.Kind == PartTrackKind.MaterialFragment)
            .ToList();

    public IReadOnlyList<PartTrack> GetMotifsByKind(MaterialKind kind)
        => GetByMaterialKind(kind)
            .Where(t => t.Meta.Kind == PartTrackKind.MaterialFragment)
            .ToList();

    public PartTrack? GetMotifByName(string name)
        => _tracks.Values
            .FirstOrDefault(t =>
                t.Meta.Kind == PartTrackKind.MaterialFragment &&
                string.Equals(t.Meta.Name, name, StringComparison.OrdinalIgnoreCase));

    public void Clear() => _tracks.Clear();
}
