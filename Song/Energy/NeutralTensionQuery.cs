// AI: purpose=Default/placeholder implementation of ITensionQuery returning neutral tension.
// AI: invariants=Always returns valid [0..1] tension values; deterministic; thread-safe.
// AI: deps=Implements ITensionQuery; will be replaced by proper tension planner in Story 7.5.2.

namespace Music.Generator;

/// <summary>
/// Default tension query implementation that returns neutral (zero) tension for all sections.
/// This is a placeholder implementation; proper tension computation will be added in Story 7.5.2.
/// </summary>
public sealed class NeutralTensionQuery : ITensionQuery
{
    private readonly int _sectionCount;
    private readonly Dictionary<int, (Section Section, MicroTensionMap Map)> _sectionData;

    /// <summary>
    /// Creates a neutral tension query for the given sections.
    /// </summary>
    public NeutralTensionQuery(IReadOnlyList<Section> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        _sectionCount = sections.Count;
        _sectionData = new Dictionary<int, (Section, MicroTensionMap)>();

        // Pre-compute neutral maps for all sections
        for (int i = 0; i < sections.Count; i++)
        {
            var section = sections[i];
            var map = MicroTensionMap.Flat(section.BarCount, 0.0);
            _sectionData[i] = (section, map);
        }
    }

    /// <inheritdoc/>
    public SectionTensionProfile GetMacroTension(int absoluteSectionIndex)
    {
        ValidateSectionIndex(absoluteSectionIndex);
        return SectionTensionProfile.Neutral(absoluteSectionIndex);
    }

    /// <inheritdoc/>
    public double GetMicroTension(int absoluteSectionIndex, int barIndexWithinSection)
    {
        ValidateSectionIndex(absoluteSectionIndex);
        var map = _sectionData[absoluteSectionIndex].Map;
        return map.GetTension(barIndexWithinSection);
    }

    /// <inheritdoc/>
    public MicroTensionMap GetMicroTensionMap(int absoluteSectionIndex)
    {
        ValidateSectionIndex(absoluteSectionIndex);
        return _sectionData[absoluteSectionIndex].Map;
    }

    /// <inheritdoc/>
    public (bool IsPhraseEnd, bool IsSectionEnd, bool IsSectionStart) GetPhraseFlags(
        int absoluteSectionIndex,
        int barIndexWithinSection)
    {
        ValidateSectionIndex(absoluteSectionIndex);
        var map = _sectionData[absoluteSectionIndex].Map;
        return map.GetFlags(barIndexWithinSection);
    }

    /// <inheritdoc/>
    public bool HasTensionData(int absoluteSectionIndex)
    {
        return absoluteSectionIndex >= 0 && absoluteSectionIndex < _sectionCount;
    }

    /// <inheritdoc/>
    public int SectionCount => _sectionCount;

    private void ValidateSectionIndex(int index)
    {
        if (index < 0 || index >= _sectionCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                $"Section index {index} out of range [0..{_sectionCount - 1}]");
        }
    }
}
