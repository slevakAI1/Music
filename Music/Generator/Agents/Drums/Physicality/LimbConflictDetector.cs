// AI: purpose=Detects limb conflicts where same limb is required for multiple simultaneous drum events.
// AI: invariants=DetectConflicts is deterministic (same input order → same output); empty input → empty conflicts.
// AI: deps=LimbAssignment, Limb enum; consumed by PhysicalityFilter in Story 4.3.
// AI: change=Story 4.1 defines detector; Story 4.3 integrates into PhysicalityFilter.

namespace Music.Generator.Agents.Drums.Physicality
{
    /// <summary>
    /// Represents a conflict where the same limb is required for multiple events at the same position.
    /// Story 4.1: Define Limb Conflict record.
    /// </summary>
    /// <param name="Limb">The limb that has a conflict.</param>
    /// <param name="BarNumber">Bar number where the conflict occurs.</param>
    /// <param name="Beat">Beat position where the conflict occurs.</param>
    /// <param name="ConflictingAssignments">All assignments that require this limb at this position (2 or more).</param>
    public readonly record struct LimbConflict(
        Limb Limb,
        int BarNumber,
        decimal Beat,
        IReadOnlyList<LimbAssignment> ConflictingAssignments)
    {
        /// <summary>
        /// Gets the number of conflicting assignments.
        /// </summary>
        public int ConflictCount => ConflictingAssignments.Count;

        /// <summary>
        /// Gets the roles involved in this conflict.
        /// </summary>
        public IEnumerable<string> ConflictingRoles => ConflictingAssignments.Select(a => a.Role);
    }

    /// <summary>
    /// Detects conflicts where the same limb is required for multiple events at the same position.
    /// A conflict occurs when two or more LimbAssignments have the same (BarNumber, Beat, Limb) tuple.
    /// Story 4.1: Define Limb Conflict Detector.
    /// </summary>
    public sealed class LimbConflictDetector
    {
        /// <summary>
        /// Detects all limb conflicts in the provided assignments.
        /// A conflict occurs when the same limb is assigned to multiple events at the same (BarNumber, Beat).
        /// </summary>
        /// <param name="assignments">List of limb assignments to check for conflicts.</param>
        /// <returns>List of conflicts found. Empty if no conflicts.</returns>
        public IReadOnlyList<LimbConflict> DetectConflicts(IReadOnlyList<LimbAssignment> assignments)
        {
            ArgumentNullException.ThrowIfNull(assignments);

            if (assignments.Count < 2)
                return [];

            // Group by (BarNumber, Beat, Limb) - conflicts occur when group has 2+ members
            var conflicts = new List<LimbConflict>();

            var groups = assignments
                .GroupBy(a => (a.BarNumber, a.Beat, a.Limb))
                .Where(g => g.Count() > 1);

            foreach (var group in groups)
            {
                var conflictingAssignments = group.ToList();
                conflicts.Add(new LimbConflict(
                    group.Key.Limb,
                    group.Key.BarNumber,
                    group.Key.Beat,
                    conflictingAssignments));
            }

            // Sort for deterministic output: by bar, beat, then limb
            conflicts.Sort((a, b) =>
            {
                int barCompare = a.BarNumber.CompareTo(b.BarNumber);
                if (barCompare != 0) return barCompare;

                int beatCompare = a.Beat.CompareTo(b.Beat);
                if (beatCompare != 0) return beatCompare;

                return a.Limb.CompareTo(b.Limb);
            });

            return conflicts;
        }

        /// <summary>
        /// Converts DrumCandidates to LimbAssignments using the specified LimbModel,
        /// then detects conflicts.
        /// Candidates with unknown roles (no limb mapping) are skipped.
        /// </summary>
        /// <param name="candidates">Drum candidates to check.</param>
        /// <param name="limbModel">Limb model for role→limb mapping.</param>
        /// <returns>List of conflicts found. Empty if no conflicts.</returns>
        public IReadOnlyList<LimbConflict> DetectConflicts(
            IReadOnlyList<DrumCandidate> candidates,
            LimbModel limbModel)
        {
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentNullException.ThrowIfNull(limbModel);

            if (candidates.Count < 2)
                return [];

            var assignments = new List<LimbAssignment>();

            foreach (var candidate in candidates)
            {
                var assignment = LimbAssignment.FromCandidate(candidate, limbModel);
                if (assignment.HasValue)
                {
                    assignments.Add(assignment.Value);
                }
            }

            return DetectConflicts(assignments);
        }

        /// <summary>
        /// Checks if any conflicts exist in the provided assignments.
        /// Faster than DetectConflicts when only existence check is needed.
        /// </summary>
        /// <param name="assignments">List of limb assignments to check.</param>
        /// <returns>True if at least one conflict exists.</returns>
        public bool HasConflicts(IReadOnlyList<LimbAssignment> assignments)
        {
            ArgumentNullException.ThrowIfNull(assignments);

            if (assignments.Count < 2)
                return false;

            // Use HashSet for O(n) conflict detection
            var seen = new HashSet<(int BarNumber, decimal Beat, Limb Limb)>();

            foreach (var assignment in assignments)
            {
                var key = (assignment.BarNumber, assignment.Beat, assignment.Limb);
                if (!seen.Add(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Default singleton instance.
        /// </summary>
        public static LimbConflictDetector Default { get; } = new();
    }
}
