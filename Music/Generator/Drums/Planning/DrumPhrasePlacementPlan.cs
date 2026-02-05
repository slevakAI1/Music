// AI: purpose=Phrase placement plan for drum tracks; stores where phrases land and optional evolution metadata.
// AI: invariants=StartBar>=1; BarCount>=1; EndBar is exclusive; placements must not overlap (enforced by planner).
// AI: deps=Used by DrumGenerator; if adding fields update GenerateFromPlan and tests in DrumsV2.

namespace Music.Generator.Drums.Planning;

public sealed record DrumPhrasePlacement
{
    public required string PhraseId { get; init; }
    public required int StartBar { get; init; }
    public required int BarCount { get; init; }
    public int EvolutionLevel { get; init; }
    public DrumPhraseEvolutionParams? Evolution { get; init; }
    public int EndBar => StartBar + BarCount;
}

public sealed record DrumPhraseEvolutionParams
{
    public double GhostIntensity { get; init; }
    public double HatVariation { get; init; }
    public double Simplification { get; init; }
    public double RandomVariation { get; init; }
}

public sealed class DrumPhrasePlacementPlan
{
    public List<DrumPhrasePlacement> Placements { get; } = [];
    public HashSet<int> FillBars { get; } = [];

    public bool IsBarCovered(int bar)
        => Placements.Any(p => bar >= p.StartBar && bar < p.EndBar);

    public bool IsFillBar(int bar) => FillBars.Contains(bar);

    public DrumPhrasePlacement? GetPlacementForBar(int bar)
        => Placements.FirstOrDefault(p => bar >= p.StartBar && bar < p.EndBar);
}
