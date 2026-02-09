// AI: purpose=Registry of immutable StyleConfiguration instances and case-insensitive lookup helper
// AI: invariants=Style entries immutable; StyleId lookup is case-insensitive; GetStyle returns null if missing
// AI: deps=StyleConfiguration, FeelRules, GridRules, GrooveRoles, DrummerVelocity/TimingHintSettings
// AI: note=Add new styles as static properties and register in static ctor to expose via GetStyle
using Music.Generator.Drums.Performance;
using Music.Generator.Groove;

namespace Music.Generator.Core
{
    // AI: purpose=Expose predefined StyleConfiguration instances and lookup APIs
    public static class StyleConfigurationLibrary
    {
        private static readonly Dictionary<string, StyleConfiguration> _styles;

        static StyleConfigurationLibrary()
        {
            _styles = new Dictionary<string, StyleConfiguration>(StringComparer.OrdinalIgnoreCase)
            {
                [PopRock.StyleId] = PopRock,
                [Jazz.StyleId] = Jazz,
                [Metal.StyleId] = Metal
            };
        }

        // AI: lookup=Return StyleConfiguration by id (case-insensitive) or null when not found
        public static StyleConfiguration? GetStyle(string styleId)
        {
            ArgumentNullException.ThrowIfNull(styleId);
            return _styles.TryGetValue(styleId, out var style) ? style : null;
        }

        // AI: info=All available StyleIds (case-insensitive keys preserved); returns a snapshot list
        public static IReadOnlyList<string> AvailableStyleIds => _styles.Keys.ToList();

        // AI: query=Return true when styleId exists (case-insensitive)
        public static bool StyleExists(string styleId)
        {
            ArgumentNullException.ThrowIfNull(styleId);
            return _styles.ContainsKey(styleId);
        }

        #region Predefined Styles

        // AI: style=PopRock defaults: straight feel, sixteenth grid, balanced role densities
        public static StyleConfiguration PopRock => new()
        {
            StyleId = "PopRock",
            DisplayName = "Pop/Rock",

            // Empty = all operators allowed (populated in Phase 3-4 when operators exist)
            AllowedOperatorIds = Array.Empty<string>(),

            // Operator weights (populated in Phase 5 - Story 5.1)
            OperatorWeights = new Dictionary<string, double>
            {
                // Placeholder weights - will be populated when operators exist
            },

            // Role density defaults
            RoleDensityDefaults = new Dictionary<string, double>
            {
                { GrooveRoles.Kick, 0.6 },
                { GrooveRoles.Snare, 0.5 },
                { GrooveRoles.ClosedHat, 0.7 },
                { GrooveRoles.OpenHat, 0.2 },
                { GrooveRoles.Bass, 0.5 },
                { GrooveRoles.Comp, 0.4 },
                { GrooveRoles.Pads, 0.3 }
            },

            // Hard caps per role (per bar)
            RoleCaps = new Dictionary<string, int>
            {
                { GrooveRoles.Kick, 8 },
                { GrooveRoles.Snare, 6 },
                { GrooveRoles.ClosedHat, 16 },
                { GrooveRoles.OpenHat, 4 },
                { GrooveRoles.Bass, 8 },
                { GrooveRoles.Comp, 8 },
                { GrooveRoles.Pads, 4 }
            },

            FeelRules = FeelRules.Straight,
            GridRules = GridRules.SixteenthGrid,
            DrummerVelocityHints = DrummerVelocityHintSettings.PopRockDefaults,
            DrummerTimingHints = DrummerTimingHintSettings.PopRockDefaults
        };

        // AI: style=Jazz defaults: swing feel, triplet-capable grid, sparser densities
        public static StyleConfiguration Jazz => new()
        {
            StyleId = "Jazz",
            DisplayName = "Jazz",

            AllowedOperatorIds = Array.Empty<string>(),

            OperatorWeights = new Dictionary<string, double>
            {
                // Placeholder - populated when operators exist
            },

            RoleDensityDefaults = new Dictionary<string, double>
            {
                { GrooveRoles.Kick, 0.3 },
                { GrooveRoles.Snare, 0.4 },
                { GrooveRoles.ClosedHat, 0.5 },
                { GrooveRoles.OpenHat, 0.3 },
                { GrooveRoles.Bass, 0.6 },
                { GrooveRoles.Comp, 0.5 },
                { GrooveRoles.Keys, 0.4 }
            },

            RoleCaps = new Dictionary<string, int>
            {
                { GrooveRoles.Kick, 4 },
                { GrooveRoles.Snare, 4 },
                { GrooveRoles.ClosedHat, 8 },
                { GrooveRoles.OpenHat, 4 },
                { GrooveRoles.Bass, 8 },
                { GrooveRoles.Comp, 8 },
                { GrooveRoles.Keys, 8 }
            },

            FeelRules = FeelRules.Swing(0.6),
            GridRules = GridRules.EighthWithTriplets,
            DrummerVelocityHints = DrummerVelocityHintSettings.JazzDefaults,
            DrummerTimingHints = DrummerTimingHintSettings.JazzDefaults
        };

        // AI: style=Metal defaults: straight feel, dense role densities, double-bass friendly caps
        public static StyleConfiguration Metal => new()
        {
            StyleId = "Metal",
            DisplayName = "Heavy Metal",

            AllowedOperatorIds = Array.Empty<string>(),

            OperatorWeights = new Dictionary<string, double>
            {
                // Placeholder - populated when operators exist
            },

            RoleDensityDefaults = new Dictionary<string, double>
            {
                { GrooveRoles.Kick, 0.8 },      // Double-bass patterns
                { GrooveRoles.Snare, 0.6 },
                { GrooveRoles.ClosedHat, 0.8 },
                { GrooveRoles.OpenHat, 0.3 },
                { GrooveRoles.Bass, 0.7 },
                { GrooveRoles.Comp, 0.6 }
            },

            RoleCaps = new Dictionary<string, int>
            {
                { GrooveRoles.Kick, 16 },       // Allow dense double-bass
                { GrooveRoles.Snare, 8 },
                { GrooveRoles.ClosedHat, 16 },
                { GrooveRoles.OpenHat, 4 },
                { GrooveRoles.Bass, 16 },
                { GrooveRoles.Comp, 16 }
            },

            FeelRules = FeelRules.Straight,
            GridRules = new GridRules
            {
                AllowedSubdivisions = AllowedSubdivision.Quarter |
                                      AllowedSubdivision.Eighth |
                                      AllowedSubdivision.Sixteenth |
                                      AllowedSubdivision.SixteenthTriplet  // For fills
            },
            DrummerVelocityHints = DrummerVelocityHintSettings.MetalDefaults,
            DrummerTimingHints = DrummerTimingHintSettings.MetalDefaults
        };

        #endregion
    }
}
