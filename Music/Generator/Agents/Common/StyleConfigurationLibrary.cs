// AI: purpose=Registry of style configurations; provides GetStyle lookup and predefined styles.
// AI: invariants=Styles are immutable; StyleId lookup is case-insensitive; unknown style returns null.
// AI: deps=StyleConfiguration, FeelRules, GridRules, GrooveRoles; DrummerVelocityHintSettings, DrummerTimingHintSettings for performance hints.
// AI: change=Add new styles as static properties; Story 6.1 added velocity hints; Story 6.2 added timing hints.

using Music.Generator.Agents.Drums.Performance;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Common
{
    /// <summary>
    /// Registry of predefined style configurations.
    /// Provides style lookup by ID and access to standard configurations.
    /// </summary>
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

        /// <summary>
        /// Gets a style configuration by ID (case-insensitive).
        /// Returns null if style not found.
        /// </summary>
        public static StyleConfiguration? GetStyle(string styleId)
        {
            ArgumentNullException.ThrowIfNull(styleId);
            return _styles.TryGetValue(styleId, out var style) ? style : null;
        }

        /// <summary>
        /// Gets all available style IDs.
        /// </summary>
        public static IReadOnlyList<string> AvailableStyleIds => _styles.Keys.ToList();

        /// <summary>
        /// Checks if a style ID exists.
        /// </summary>
        public static bool StyleExists(string styleId)
        {
            ArgumentNullException.ThrowIfNull(styleId);
            return _styles.ContainsKey(styleId);
        }

        #region Predefined Styles

        /// <summary>
        /// Pop/Rock style configuration.
        /// Straight feel, sixteenth grid, balanced density.
        /// Operator weights to be populated in Phase 5.
        /// </summary>
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

        /// <summary>
        /// Jazz style configuration.
        /// Swing feel, eighth triplet grid, sparser density.
        /// Operator weights to be populated in Phase 5.
        /// </summary>
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

        /// <summary>
        /// Metal style configuration.
        /// Straight feel, sixteenth grid, dense driving patterns.
        /// Operator weights to be populated in Phase 5.
        /// </summary>
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
