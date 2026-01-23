// AI: purpose=Record capturing a limb's assignment to a drum event at a specific position.
// AI: invariants=BarNumber>=1; Beat>=1.0; Limb is valid enum value; used as input to LimbConflictDetector.
// AI: deps=Limb enum from LimbModel.cs; created from DrumCandidate via LimbModel.GetRequiredLimb.
// AI: change=Story 4.1 defines record; consumed by LimbConflictDetector.DetectConflicts.

namespace Music.Generator.Agents.Drums.Physicality
{
    /// <summary>
    /// Represents a limb's assignment to play a drum hit at a specific position.
    /// Used by LimbConflictDetector to identify simultaneous limb usage conflicts.
    /// Story 4.1: Define Limb Assignment record.
    /// </summary>
    /// <param name="BarNumber">Bar number (1-based) where the hit occurs.</param>
    /// <param name="Beat">Beat position (1-based, decimal) within the bar. E.g., 1.0, 2.5, 3.75.</param>
    /// <param name="Role">Drum role for this hit (e.g., "Kick", "Snare", "ClosedHat").</param>
    /// <param name="Limb">Physical limb assigned to play this hit.</param>
    public readonly record struct LimbAssignment(
        int BarNumber,
        decimal Beat,
        string Role,
        Limb Limb)
    {
        /// <summary>
        /// Creates a LimbAssignment from a DrumCandidate using the specified LimbModel.
        /// Returns null if the role is not mapped to any limb.
        /// </summary>
        /// <param name="candidate">Drum candidate to convert.</param>
        /// <param name="limbModel">Limb model for role→limb mapping.</param>
        /// <returns>LimbAssignment if role is mapped, null otherwise.</returns>
        public static LimbAssignment? FromCandidate(DrumCandidate candidate, LimbModel limbModel)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            ArgumentNullException.ThrowIfNull(limbModel);

            var limb = limbModel.GetRequiredLimb(candidate.Role);
            if (!limb.HasValue)
                return null;

            return new LimbAssignment(
                candidate.BarNumber,
                candidate.Beat,
                candidate.Role,
                limb.Value);
        }

        /// <summary>
        /// Checks if this assignment occurs at the same position (bar and beat) as another.
        /// </summary>
        /// <param name="other">Other assignment to compare.</param>
        /// <returns>True if both assignments occur at the same bar and beat.</returns>
        public bool IsSamePosition(LimbAssignment other)
        {
            return BarNumber == other.BarNumber && Beat == other.Beat;
        }

        /// <summary>
        /// Checks if this assignment conflicts with another (same limb at same position).
        /// </summary>
        /// <param name="other">Other assignment to compare.</param>
        /// <returns>True if both assignments require the same limb at the same position.</returns>
        public bool ConflictsWith(LimbAssignment other)
        {
            return IsSamePosition(other) && Limb == other.Limb;
        }
    }
}
