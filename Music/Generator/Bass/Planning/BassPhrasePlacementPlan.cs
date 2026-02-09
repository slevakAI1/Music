// AI: purpose=Phrase placement plan for Bass tracks; stores phrase placements and optional evolution metadata.
// AI: invariants=StartBar>=1; BarCount>=1; EndBar is exclusive; placements must not overlap.
// AI: deps=Consumed by bassTrackGenerator; changes may require updates to GenerateFromPlan and basssV2 tests.

namespace Music.Generator.Bass.Planning;

public sealed record BassPhrasePlacementPlan
{
    // AI: contract=PhraseId and StartBar/BarCount required; EndBar computed exclusive; EvolutionLevel/Evolution optional
    public required string PhraseId { get; init; }
    public required int StartBar { get; init; }
    public required int BarCount { get; init; }
    public int EvolutionLevel { get; init; }
    public BassPhraseEvolutionParams? Evolution { get; init; }
    public int EndBar => StartBar + BarCount;
}

public sealed record BassPhraseEvolutionParams
{
    public double GhostIntensity { get; init; }
    public double HatVariation { get; init; }
    public double Simplification { get; init; }
    public double RandomVariation { get; init; }
}

public sealed class bassPhrasePlacementPlan
{
    // AI: note=Placements and FillBars are mutable and not thread-safe; planner enforces non-overlap before use
    public List<BassPhrasePlacementPlan> Placements { get; } = [];
    public HashSet<int> FillBars { get; } = [];

    public bool IsBarCovered(int bar)
        => Placements.Any(p => bar >= p.StartBar && bar < p.EndBar);

    public bool IsFillBar(int bar) => FillBars.Contains(bar);

    public BassPhrasePlacementPlan? GetPlacementForBar(int bar)
        => Placements.FirstOrDefault(p => bar >= p.StartBar && bar < p.EndBar);
}
