// AI: purpose=Style configuration model separating genre behavior from operator logic.
// AI: invariants=StyleConfiguration is immutable record; StyleId stable for lookup; defaults applied for missing values.
// AI: deps=GrooveFeel and AllowedSubdivision from Groove.cs; GrooveRoles for role constants.
// AI: change=Add new styles by extending StyleConfigurationLibrary; weights/caps populated when operators exist.

using Music.Generator.Groove;

namespace Music.Generator.Agents.Common
{
    /// <summary>
    /// Rules for rhythmic feel and timing behavior in a style.
    /// </summary>
    public sealed record FeelRules
    {
        /// <summary>Default feel for the style (Straight, Swing, Shuffle, TripletFeel).</summary>
        public required GrooveFeel DefaultFeel { get; init; }

        /// <summary>Default swing amount (0.0 = no swing, 1.0 = full swing). Only meaningful when feel != Straight.</summary>
        public required double SwingAmount { get; init; }

        /// <summary>Whether feel can be overridden per-section.</summary>
        public bool AllowFeelOverrides { get; init; } = true;

        /// <summary>
        /// Creates default straight feel rules.
        /// </summary>
        public static FeelRules Straight => new()
        {
            DefaultFeel = GrooveFeel.Straight,
            SwingAmount = 0.0
        };

        /// <summary>
        /// Creates swing feel rules with specified amount.
        /// </summary>
        public static FeelRules Swing(double amount = 0.5) => new()
        {
            DefaultFeel = GrooveFeel.Swing,
            SwingAmount = Math.Clamp(amount, 0.0, 1.0)
        };
    }

    /// <summary>
    /// Rules for rhythmic grid and subdivision constraints in a style.
    /// </summary>
    public sealed record GridRules
    {
        /// <summary>Allowed subdivisions for this style (flags enum).</summary>
        public required AllowedSubdivision AllowedSubdivisions { get; init; }

        /// <summary>Whether triplet subdivisions are allowed (convenience check).</summary>
        public bool AllowTriplets => AllowedSubdivisions.HasFlag(AllowedSubdivision.EighthTriplet) ||
                                      AllowedSubdivisions.HasFlag(AllowedSubdivision.SixteenthTriplet);

        /// <summary>
        /// Creates default sixteenth-note grid rules (common for Pop/Rock).
        /// </summary>
        public static GridRules SixteenthGrid => new()
        {
            AllowedSubdivisions = AllowedSubdivision.Quarter | AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth
        };

        /// <summary>
        /// Creates eighth-note grid rules with triplets (common for Jazz/Shuffle).
        /// </summary>
        public static GridRules EighthWithTriplets => new()
        {
            AllowedSubdivisions = AllowedSubdivision.Quarter | AllowedSubdivision.Eighth | AllowedSubdivision.EighthTriplet
        };
    }

    /// <summary>
    /// Complete style configuration separating genre behavior from operator logic.
    /// Same operators work across genres with different weights, caps, and idioms.
    /// </summary>
    public sealed record StyleConfiguration
    {
        /// <summary>Unique style identifier (e.g., "PopRock", "Jazz", "Metal").</summary>
        public required string StyleId { get; init; }

        /// <summary>Human-readable style name.</summary>
        public required string DisplayName { get; init; }

        /// <summary>List of operator IDs enabled for this style. Empty = all operators allowed.</summary>
        public required IReadOnlyList<string> AllowedOperatorIds { get; init; }

        /// <summary>Per-operator weight multipliers (operatorId → weight). Missing = 0.5 default.</summary>
        public required IReadOnlyDictionary<string, double> OperatorWeights { get; init; }

        /// <summary>Default density targets per role (role → density). Missing = style default.</summary>
        public required IReadOnlyDictionary<string, double> RoleDensityDefaults { get; init; }

        /// <summary>Hard caps per role (role → max count). Missing = int.MaxValue (no cap).</summary>
        public required IReadOnlyDictionary<string, int> RoleCaps { get; init; }

        /// <summary>Feel rules (straight, swing, shuffle).</summary>
        public required FeelRules FeelRules { get; init; }

        /// <summary>Grid/subdivision rules.</summary>
        public required GridRules GridRules { get; init; }

        /// <summary>Default style weight when operator not in OperatorWeights (0.5).</summary>
        public const double DefaultOperatorWeight = 0.5;

        /// <summary>Default density when role not in RoleDensityDefaults.</summary>
        public const double DefaultRoleDensity = 0.5;

        /// <summary>
        /// Gets operator weight, returning default if not configured.
        /// </summary>
        public double GetOperatorWeight(string operatorId)
        {
            ArgumentNullException.ThrowIfNull(operatorId);
            return OperatorWeights.TryGetValue(operatorId, out double weight) ? weight : DefaultOperatorWeight;
        }

        /// <summary>
        /// Gets role density target, returning default if not configured.
        /// </summary>
        public double GetRoleDensity(string role)
        {
            ArgumentNullException.ThrowIfNull(role);
            return RoleDensityDefaults.TryGetValue(role, out double density) ? density : DefaultRoleDensity;
        }

        /// <summary>
        /// Gets role cap, returning int.MaxValue if not configured (no cap).
        /// </summary>
        public int GetRoleCap(string role)
        {
            ArgumentNullException.ThrowIfNull(role);
            return RoleCaps.TryGetValue(role, out int cap) ? cap : int.MaxValue;
        }

        /// <summary>
        /// Checks if an operator is allowed in this style.
        /// Empty AllowedOperatorIds = all operators allowed.
        /// </summary>
        public bool IsOperatorAllowed(string operatorId)
        {
            ArgumentNullException.ThrowIfNull(operatorId);

            // Empty list = all operators allowed
            if (AllowedOperatorIds.Count == 0)
                return true;

            return AllowedOperatorIds.Contains(operatorId);
        }
    }
}
