// AI: purpose=Library of predefined energy constraint policies for different musical styles/genres.
// AI: invariants=Policy selection deterministic by style name; all policies have valid rules; empty policy always available.
// AI: deps=Creates EnergyConstraintPolicy instances; uses constraint rule implementations; consumed by EnergyArc.

using Music.Generator.EnergyConstraints;

namespace Music.Generator
{
    /// <summary>
    /// Library of predefined energy constraint policies for different musical styles.
    /// Policies are selected deterministically based on groove/style names.
    /// </summary>
    public static class EnergyConstraintPolicyLibrary
    {
        /// <summary>
        /// Gets the default policy used when no specific policy is requested.
        /// Returns Pop/Rock policy as a safe default.
        /// </summary>
        public static EnergyConstraintPolicy GetDefaultPolicy()
        {
            return GetPopRockPolicy();
        }

        /// <summary>
        /// Gets an appropriate policy for the given groove/style name.
        /// Selection is deterministic based on name patterns.
        /// </summary>
        public static EnergyConstraintPolicy GetPolicyForGroove(string grooveName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(grooveName);

            string lowerName = grooveName.ToLowerInvariant();

            // Match style patterns
            if (lowerName.Contains("jazz") || lowerName.Contains("bossa") || lowerName.Contains("latin"))
                return GetJazzPolicy();
            
            if (lowerName.Contains("edm") || lowerName.Contains("house") || lowerName.Contains("techno"))
                return GetEDMPolicy();
            
            if (lowerName.Contains("rock") || lowerName.Contains("punk") || lowerName.Contains("metal"))
                return GetRockPolicy();

            // Default to Pop/Rock for most cases
            return GetPopRockPolicy();
        }

        /// <summary>
        /// Gets the Pop/Rock policy: moderate verse progression, strong chorus contrast, final chorus peak.
        /// This is the default "safe" policy for most popular music.
        /// </summary>
        public static EnergyConstraintPolicy GetPopRockPolicy()
        {
            var rules = new List<EnergyConstraintRule>
            {
                // Moderate monotonic progression for verses/choruses
                new SameTypeSectionsMonotonicRule(strength: 1.0, minIncrement: 0.02),
                
                // Strong post-chorus drop for contrast
                new PostChorusDropRule(strength: 1.2, maxEnergyAfterChorus: 0.55, typicalDropAmount: 0.20),
                
                // Final chorus should be the peak
                new FinalChorusPeakRule(strength: 1.5, minPeakEnergy: 0.80, peakProximityThreshold: 0.95),
                
                // Bridge creates contrast (either high or low)
                new BridgeContrastRule(strength: 0.8, minContrastAmount: 0.15)
            };

            return new EnergyConstraintPolicy
            {
                PolicyName = "PopRock",
                Rules = rules,
                IsEnabled = true
            };
        }

        /// <summary>
        /// Gets the Rock policy: stronger verse progression, sustained high energy allowed.
        /// Rock songs often build more aggressively and maintain high energy longer.
        /// </summary>
        public static EnergyConstraintPolicy GetRockPolicy()
        {
            var rules = new List<EnergyConstraintRule>
            {
                // Stronger monotonic progression
                new SameTypeSectionsMonotonicRule(strength: 1.3, minIncrement: 0.05),
                
                // Less strict post-chorus drop (rock maintains energy)
                new PostChorusDropRule(strength: 0.8, maxEnergyAfterChorus: 0.65, typicalDropAmount: 0.15),
                
                // Final chorus peak (strong emphasis)
                new FinalChorusPeakRule(strength: 1.8, minPeakEnergy: 0.85, peakProximityThreshold: 0.98),
                
                // Bridge contrast (can be high-energy)
                new BridgeContrastRule(strength: 0.7, minContrastAmount: 0.12)
            };

            return new EnergyConstraintPolicy
            {
                PolicyName = "Rock",
                Rules = rules,
                IsEnabled = true
            };
        }

        /// <summary>
        /// Gets the Jazz policy: relaxed constraints, allows energy drops/rises more freely.
        /// Jazz emphasizes organic dynamics and doesn't follow strict pop/rock energy arcs.
        /// </summary>
        public static EnergyConstraintPolicy GetJazzPolicy()
        {
            var rules = new List<EnergyConstraintRule>
            {
                // Weak monotonic progression (allow freedom)
                new SameTypeSectionsMonotonicRule(strength: 0.3, minIncrement: 0.0),
                
                // No strict post-chorus rules
                // (Jazz doesn't follow verse-chorus structure as strictly)
                
                // Final chorus peak less strict
                new FinalChorusPeakRule(strength: 0.5, minPeakEnergy: 0.70, peakProximityThreshold: 0.85),
                
                // Bridge has more freedom
                new BridgeContrastRule(strength: 0.4, minContrastAmount: 0.10)
            };

            return new EnergyConstraintPolicy
            {
                PolicyName = "Jazz",
                Rules = rules,
                IsEnabled = true
            };
        }

        /// <summary>
        /// Gets the EDM policy: strong build emphasis, dramatic drops allowed.
        /// EDM often features builds into drops rather than traditional verse-chorus patterns.
        /// </summary>
        public static EnergyConstraintPolicy GetEDMPolicy()
        {
            var rules = new List<EnergyConstraintRule>
            {
                // Moderate monotonic progression
                new SameTypeSectionsMonotonicRule(strength: 0.8, minIncrement: 0.03),
                
                // Post-chorus drop rule DISABLED (EDM goes straight into builds)
                // We achieve this by not including the PostChorusDropRule
                
                // Strong final peak (drops build to climax)
                new FinalChorusPeakRule(strength: 2.0, minPeakEnergy: 0.90, peakProximityThreshold: 1.0),
                
                // Bridge can be a breakdown (low energy contrast)
                new BridgeContrastRule(strength: 0.9, minContrastAmount: 0.20)
            };

            return new EnergyConstraintPolicy
            {
                PolicyName = "EDM",
                Rules = rules,
                IsEnabled = true
            };
        }

        /// <summary>
        /// Gets an empty policy with no constraints (all energy decisions from template only).
        /// Useful for testing or when explicit control is needed.
        /// </summary>
        public static EnergyConstraintPolicy GetEmptyPolicy()
        {
            return EnergyConstraintPolicy.Empty("None");
        }

        /// <summary>
        /// Gets a minimal policy with only the most essential constraints.
        /// Good for genres that need some structure but maximum freedom.
        /// </summary>
        public static EnergyConstraintPolicy GetMinimalPolicy()
        {
            var rules = new List<EnergyConstraintRule>
            {
                // Only ensure final chorus is a peak
                new FinalChorusPeakRule(strength: 1.0, minPeakEnergy: 0.75, peakProximityThreshold: 0.90)
            };

            return new EnergyConstraintPolicy
            {
                PolicyName = "Minimal",
                Rules = rules,
                IsEnabled = true
            };
        }

        /// <summary>
        /// Gets all available named policies for diagnostic/testing purposes.
        /// </summary>
        public static Dictionary<string, EnergyConstraintPolicy> GetAllPolicies()
        {
            return new Dictionary<string, EnergyConstraintPolicy>
            {
                ["PopRock"] = GetPopRockPolicy(),
                ["Rock"] = GetRockPolicy(),
                ["Jazz"] = GetJazzPolicy(),
                ["EDM"] = GetEDMPolicy(),
                ["Minimal"] = GetMinimalPolicy(),
                ["None"] = GetEmptyPolicy()
            };
        }
    }
}
