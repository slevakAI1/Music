// AI: purpose=StyleConfiguration: separates genre behavior from operator logic; immutable record
// AI: invariants=StyleId stable; defaults applied when values missing; records should remain backward compatible
// AI: deps=Uses GrooveFeel, AllowedSubdivision, GrooveRoles, DrummerVelocity/TimingHintSettings
using Music.Generator.Drums.Performance;
using Music.Generator.Groove;

namespace Music.Generator.Core
{
    // AI: purpose=FeelRules: defines default feel and swing; AllowFeelOverrides controls per-section overrides
    public sealed record FeelRules
    {
        public required GrooveFeel DefaultFeel { get; init; }
        public required double SwingAmount { get; init; }
        public bool AllowFeelOverrides { get; init; } = true;

        // AI: factory=Straight default: GrooveFeel.Straight, SwingAmount=0.0
        public static FeelRules Straight => new()
        {
            DefaultFeel = GrooveFeel.Straight,
            SwingAmount = 0.0
        };

        // AI: factory=Swing default factory clamps amount to [0.0,1.0]
        public static FeelRules Swing(double amount = 0.5) => new()
        {
            DefaultFeel = GrooveFeel.Swing,
            SwingAmount = Math.Clamp(amount, 0.0, 1.0)
        };
    }

    // AI: purpose=GridRules: allowed rhythmic subdivisions for a style; convenience AllowTriplets check
    public sealed record GridRules
    {
        public required AllowedSubdivision AllowedSubdivisions { get; init; }

        // AI: convenience=Returns true when any triplet subdivision flag is set
        public bool AllowTriplets => AllowedSubdivisions.HasFlag(AllowedSubdivision.EighthTriplet) ||
                                      AllowedSubdivisions.HasFlag(AllowedSubdivision.SixteenthTriplet);

        // AI: factory=SixteenthGrid common default (Quarter/Eighth/Sixteenth)
        public static GridRules SixteenthGrid => new()
        {
            AllowedSubdivisions = AllowedSubdivision.Quarter | AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth
        };

        // AI: factory=EighthWithTriplets includes EighthTriplet flag
        public static GridRules EighthWithTriplets => new()
        {
            AllowedSubdivisions = AllowedSubdivision.Quarter | AllowedSubdivision.Eighth | AllowedSubdivision.EighthTriplet
        };
    }

    // AI: purpose=StyleConfiguration: complete style rules and operator weights/caps for a genre
    public sealed record StyleConfiguration
    {
        public required string StyleId { get; init; }
        public required string DisplayName { get; init; }
        public required IReadOnlyList<string> AllowedOperatorIds { get; init; }
        public required IReadOnlyDictionary<string, double> OperatorWeights { get; init; }
        public required IReadOnlyDictionary<string, double> RoleDensityDefaults { get; init; }
        public required IReadOnlyDictionary<string, int> RoleCaps { get; init; }
        public required FeelRules FeelRules { get; init; }
        public required GridRules GridRules { get; init; }

        // AI: optional=Drummer velocity/timing hint configs; null -> conservative defaults
        public DrummerVelocityHintSettings? DrummerVelocityHints { get; init; }
        public DrummerTimingHintSettings? DrummerTimingHints { get; init; }

        public const double DefaultOperatorWeight = 0.5;
        public const double DefaultRoleDensity = 0.5;

        // AI: util=Return per-operator weight or default when missing
        public double GetOperatorWeight(string operatorId)
        {
            ArgumentNullException.ThrowIfNull(operatorId);
            return OperatorWeights.TryGetValue(operatorId, out double weight) ? weight : DefaultOperatorWeight;
        }

        // AI: util=Return role density or default when missing
        public double GetRoleDensity(string role)
        {
            ArgumentNullException.ThrowIfNull(role);
            return RoleDensityDefaults.TryGetValue(role, out double density) ? density : DefaultRoleDensity;
        }

        // AI: util=Return role cap or int.MaxValue when missing (no cap)
        public int GetRoleCap(string role)
        {
            ArgumentNullException.ThrowIfNull(role);
            return RoleCaps.TryGetValue(role, out int cap) ? cap : int.MaxValue;
        }

        // AI: check=AllowedOperatorIds empty implies all operators allowed; membership check otherwise
        public bool IsOperatorAllowed(string operatorId)
        {
            ArgumentNullException.ThrowIfNull(operatorId);
            if (AllowedOperatorIds.Count == 0)
                return true;
            return AllowedOperatorIds.Contains(operatorId);
        }

        // AI: helper=Return drummer velocity hints or conservative defaults
        public DrummerVelocityHintSettings GetDrummerVelocityHints()
        {
            return DrummerVelocityHints ?? DrummerVelocityHintSettings.ConservativeDefaults;
        }

        // AI: helper=Return drummer timing hints or conservative defaults
        public DrummerTimingHintSettings GetDrummerTimingHints()
        {
            return DrummerTimingHints ?? DrummerTimingHintSettings.ConservativeDefaults;
        }
    }
}
