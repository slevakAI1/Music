// AI: purpose=MaterialBank extension methods for motif-specific queries
// AI: invariants=All methods filter by MaterialFragment kind and specific criteria
// AI: deps=Reuses existing MaterialBank infrastructure; no new storage logic
using Music.Generator;

namespace Music.Song.Material;

internal static class MaterialBankMotifExtensions
{
    // AI: purpose=Get all motifs matching a specific role (case-insensitive)
    // AI: invariants=Only returns MaterialFragment tracks; filters by IntendedRole
    internal static IReadOnlyList<PartTrack> GetMotifsByRole(this MaterialBank bank, string intendedRole)
    {
        ArgumentNullException.ThrowIfNull(bank);
        
        return bank.GetByKind(PartTrackKind.MaterialFragment)
            .Where(t => string.Equals(t.Meta.IntendedRole, intendedRole, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    // AI: purpose=Get all motifs of a specific material kind (Hook, Riff, etc.)
    // AI: invariants=Only returns MaterialFragment tracks; filters by MaterialKind
    internal static IReadOnlyList<PartTrack> GetMotifsByMaterialKind(this MaterialBank bank, MaterialKind kind)
    {
        ArgumentNullException.ThrowIfNull(bank);
        
        return bank.GetByKind(PartTrackKind.MaterialFragment)
            .Where(t => t.Meta.MaterialKind == kind)
            .ToList();
    }

    // AI: purpose=Get a specific motif by name (convenience for tests and diagnostics)
    // AI: invariants=Returns first match (case-insensitive); null if not found
    internal static PartTrack? GetMotifByName(this MaterialBank bank, string name)
    {
        ArgumentNullException.ThrowIfNull(bank);
        
        return bank.GetByKind(PartTrackKind.MaterialFragment)
            .FirstOrDefault(t => string.Equals(t.Meta.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
