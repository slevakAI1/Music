// AI: purpose=Drummer-specific memory extending AgentMemory; tracks fills, crashes, hat modes, ghost frequency.
// AI: invariants=Deterministic: same sequence of records = same state; anti-repetition enforced for fills.
// AI: deps=AgentMemory, FillShape, HatMode, MusicConstants.eSectionType; consumed by operators and selection engine.
// AI: change=Story 2.5; extend with additional drummer-specific tracking as operator needs emerge.

using Music.Generator.Agents.Common;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Tracks a single hat mode change event for history.
    /// </summary>
    /// <param name="BarNumber">1-based bar where the mode change occurred.</param>
    /// <param name="Mode">The hat mode that became active.</param>
    /// <param name="Subdivision">The hat subdivision in effect.</param>
    public sealed record HatModeHistoryEntry(int BarNumber, HatMode Mode, HatSubdivision Subdivision);

    /// <summary>
    /// Drummer-specific memory extending base AgentMemory with fill tracking,
    /// crash patterns, hat mode history, and ghost note frequency.
    /// Story 2.5: Implement Drummer Memory.
    /// </summary>
    public sealed class DrummerMemory : AgentMemory
    {
        private readonly int _fillLookbackBars;
        private readonly int _ghostWindowSize;
        private readonly double _fillShapeTolerance;

        // Track last fill bar and shape (beyond base _lastFillShape which is inherited)
        private int _lastFillBar;

        // Previous section's fill shape for anti-repetition across sections
        private FillShape? _previousSectionFillShape;
        private MusicConstants.eSectionType? _previousSectionType;

        // Chorus crash pattern: beat positions where crashes occur (section-relative)
        private readonly SortedSet<decimal> _chorusCrashPattern = new();
        private bool _chorusCrashPatternEstablished;

        // Hat mode history: ordered list of mode changes
        private readonly List<HatModeHistoryEntry> _hatModeHistory = new();

        // Ghost note counts per bar for rolling average
        private readonly Dictionary<int, int> _ghostCountsPerBar = new();

        /// <summary>
        /// Creates a new drummer memory with configurable settings.
        /// </summary>
        /// <param name="operatorWindowSize">Window for operator repetition (default 4).</param>
        /// <param name="fillLookbackBars">Window for fill anti-repetition (default 8).</param>
        /// <param name="ghostWindowSize">Window for ghost note frequency average (default 8).</param>
        /// <param name="decayCurve">How penalty decays over the window (default Exponential).</param>
        /// <param name="decayFactor">Base for exponential decay (default 0.5 for drummers).</param>
        /// <param name="fillShapeTolerance">Density tolerance for "same fill" comparison (default 0.1).</param>
        public DrummerMemory(
            int operatorWindowSize = 4,
            int fillLookbackBars = 8,
            int ghostWindowSize = 8,
            DecayCurve decayCurve = DecayCurve.Exponential,
            double decayFactor = 0.5,
            double fillShapeTolerance = 0.1)
            : base(operatorWindowSize, decayCurve, decayFactor)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(fillLookbackBars, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(ghostWindowSize, 1);
            ArgumentOutOfRangeException.ThrowIfNegative(fillShapeTolerance);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(fillShapeTolerance, 1.0);

            _fillLookbackBars = fillLookbackBars;
            _ghostWindowSize = ghostWindowSize;
            _fillShapeTolerance = fillShapeTolerance;
        }

        /// <summary>
        /// Gets the bar number where the last fill occurred.
        /// Returns 0 if no fills have been recorded.
        /// </summary>
        public int LastFillBar => _lastFillBar;

        /// <summary>
        /// Gets the fill shape from the previous section (for anti-repetition across sections).
        /// </summary>
        public FillShape? PreviousSectionFillShape => _previousSectionFillShape;

        /// <summary>
        /// Gets the chorus crash pattern (beat positions where crashes occur).
        /// Empty if no pattern established.
        /// </summary>
        public IReadOnlyList<decimal> ChorusCrashPattern => _chorusCrashPattern.ToList();

        /// <summary>
        /// Gets whether a chorus crash pattern has been established.
        /// Once established, crashes should follow this pattern for consistency.
        /// </summary>
        public bool IsChorusCrashPatternEstablished => _chorusCrashPatternEstablished;

        /// <summary>
        /// Gets the hat mode change history (ordered by bar number).
        /// </summary>
        public IReadOnlyList<HatModeHistoryEntry> HatModeHistory => _hatModeHistory;

        /// <summary>
        /// Gets the rolling average of ghost notes per bar over the window.
        /// Returns 0.0 if no ghost notes recorded.
        /// </summary>
        public double GhostNoteFrequency => ComputeGhostNoteFrequency();

        /// <summary>
        /// Records a fill occurrence and updates fill tracking.
        /// Extends base RecordFillShape with bar tracking and section-boundary detection.
        /// </summary>
        /// <param name="barNumber">1-based bar where fill occurred.</param>
        /// <param name="fillShape">The shape of the fill.</param>
        /// <param name="sectionType">The section type where fill occurred.</param>
        public void RecordFill(int barNumber, FillShape fillShape, MusicConstants.eSectionType sectionType)
        {
            ArgumentNullException.ThrowIfNull(fillShape);
            ArgumentOutOfRangeException.ThrowIfLessThan(barNumber, 1);

            // Check if we're entering a new section
            if (_previousSectionType.HasValue && _previousSectionType.Value != sectionType)
            {
                // Entering new section: save current fill as previous section's fill
                _previousSectionFillShape = GetLastFillShape();
            }

            _previousSectionType = sectionType;
            _lastFillBar = barNumber;

            // Call base to update _lastFillShape
            RecordFillShape(fillShape);
        }

        /// <summary>
        /// Checks if a fill shape would repeat the previous section's fill.
        /// Returns true if the fill is considered "same" as previous section's fill.
        /// </summary>
        /// <param name="proposedFill">The fill shape being considered.</param>
        /// <returns>True if this would be a repetition; false if acceptable.</returns>
        public bool WouldRepeatPreviousSectionFill(FillShape proposedFill)
        {
            ArgumentNullException.ThrowIfNull(proposedFill);

            if (_previousSectionFillShape == null || !_previousSectionFillShape.HasContent)
                return false;

            return AreFillShapesSimilar(_previousSectionFillShape, proposedFill);
        }

        /// <summary>
        /// Computes a repetition penalty for a proposed fill based on similarity to previous section fill.
        /// Returns 0.0 if no repetition concern, up to 1.0 for exact repeat.
        /// </summary>
        /// <param name="proposedFill">The fill shape being considered.</param>
        /// <returns>Penalty in range [0.0, 1.0].</returns>
        public double GetFillRepetitionPenalty(FillShape proposedFill)
        {
            ArgumentNullException.ThrowIfNull(proposedFill);

            if (!WouldRepeatPreviousSectionFill(proposedFill))
                return 0.0;

            // Similar fill in adjacent section: apply high penalty
            return 0.8;
        }

        /// <summary>
        /// Records a crash hit for chorus pattern tracking.
        /// </summary>
        /// <param name="beat">Beat position (1-based) within the bar.</param>
        /// <param name="sectionType">Current section type (only Chorus is tracked).</param>
        public void RecordCrashHit(decimal beat, MusicConstants.eSectionType sectionType)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(beat, 1.0m);

            if (sectionType != MusicConstants.eSectionType.Chorus)
                return;

            _chorusCrashPattern.Add(beat);

            // Establish pattern after first chorus completes (heuristic: 2+ hits)
            if (_chorusCrashPattern.Count >= 2)
                _chorusCrashPatternEstablished = true;
        }

        /// <summary>
        /// Checks if a beat position matches the established chorus crash pattern.
        /// Returns true if no pattern established (anything allowed) or if beat is in pattern.
        /// </summary>
        /// <param name="beat">Beat position to check.</param>
        /// <returns>True if crash at this beat would match pattern.</returns>
        public bool IsCrashBeatInPattern(decimal beat)
        {
            if (!_chorusCrashPatternEstablished)
                return true; // No pattern yet, any beat is acceptable

            return _chorusCrashPattern.Contains(beat);
        }

        /// <summary>
        /// Records a hat mode change.
        /// </summary>
        /// <param name="barNumber">1-based bar where change occurred.</param>
        /// <param name="mode">New hat mode.</param>
        /// <param name="subdivision">New subdivision.</param>
        public void RecordHatModeChange(int barNumber, HatMode mode, HatSubdivision subdivision)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(barNumber, 1);

            // Only record if different from last entry
            if (_hatModeHistory.Count > 0)
            {
                var last = _hatModeHistory[^1];
                if (last.Mode == mode && last.Subdivision == subdivision)
                    return; // No change
            }

            _hatModeHistory.Add(new HatModeHistoryEntry(barNumber, mode, subdivision));
        }

        /// <summary>
        /// Gets the most recent hat mode entry before or at the given bar.
        /// Returns null if no history.
        /// </summary>
        /// <param name="barNumber">Bar number to query.</param>
        /// <returns>Most recent hat mode entry, or null.</returns>
        public HatModeHistoryEntry? GetHatModeAt(int barNumber)
        {
            HatModeHistoryEntry? result = null;

            foreach (var entry in _hatModeHistory)
            {
                if (entry.BarNumber <= barNumber)
                    result = entry;
                else
                    break;
            }

            return result;
        }

        /// <summary>
        /// Records ghost note count for a bar.
        /// </summary>
        /// <param name="barNumber">1-based bar number.</param>
        /// <param name="ghostCount">Number of ghost notes in this bar.</param>
        public void RecordGhostNotes(int barNumber, int ghostCount)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(barNumber, 1);
            ArgumentOutOfRangeException.ThrowIfNegative(ghostCount);

            _ghostCountsPerBar[barNumber] = ghostCount;

            // Prune old entries outside window
            PruneGhostEntries(barNumber);
        }

        /// <summary>
        /// Clears all drummer-specific memory in addition to base memory.
        /// </summary>
        public new void Clear()
        {
            base.Clear();
            _lastFillBar = 0;
            _previousSectionFillShape = null;
            _previousSectionType = null;
            _chorusCrashPattern.Clear();
            _chorusCrashPatternEstablished = false;
            _hatModeHistory.Clear();
            _ghostCountsPerBar.Clear();
        }

        /// <summary>
        /// Compares two fill shapes to determine if they're similar enough to count as repetition.
        /// Uses roles (sorted), density (with tolerance), duration, and fill tag.
        /// </summary>
        private bool AreFillShapesSimilar(FillShape a, FillShape b)
        {
            // Compare roles (sorted for determinism)
            var rolesA = a.RolesInvolved.OrderBy(r => r).ToList();
            var rolesB = b.RolesInvolved.OrderBy(r => r).ToList();

            if (!rolesA.SequenceEqual(rolesB))
                return false;

            // Compare density with tolerance
            if (Math.Abs(a.DensityLevel - b.DensityLevel) > _fillShapeTolerance)
                return false;

            // Compare duration
            if (a.DurationBars != b.DurationBars)
                return false;

            // Compare fill tag (null == null is OK, different tags are different)
            if (a.FillTag != b.FillTag)
                return false;

            return true;
        }

        /// <summary>
        /// Computes rolling average of ghost notes per bar.
        /// </summary>
        private double ComputeGhostNoteFrequency()
        {
            if (_ghostCountsPerBar.Count == 0 || CurrentBarNumber == 0)
                return 0.0;

            int startBar = Math.Max(1, CurrentBarNumber - _ghostWindowSize + 1);
            var relevantEntries = _ghostCountsPerBar
                .Where(kvp => kvp.Key >= startBar && kvp.Key <= CurrentBarNumber)
                .ToList();

            if (relevantEntries.Count == 0)
                return 0.0;

            double totalGhosts = relevantEntries.Sum(kvp => kvp.Value);
            return totalGhosts / relevantEntries.Count;
        }

        /// <summary>
        /// Removes ghost count entries outside the window.
        /// </summary>
        private void PruneGhostEntries(int currentBar)
        {
            int oldestAllowed = currentBar - _ghostWindowSize + 1;
            var keysToRemove = _ghostCountsPerBar.Keys.Where(k => k < oldestAllowed).ToList();

            foreach (var key in keysToRemove)
            {
                _ghostCountsPerBar.Remove(key);
            }
        }
    }
}
