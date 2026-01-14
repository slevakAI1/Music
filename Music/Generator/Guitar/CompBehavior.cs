// AI: purpose=Deterministic selection of comp playing behavior based on energy/tension/section.
// AI: invariants=Selection is deterministic by (sectionType, absoluteSectionIndex, barIndex, energy, busyProbability, seed).
// AI: change=Add new behaviors by extending enum and updating SelectBehavior logic.

namespace Music.Generator
{
    /// <summary>
    /// Distinct playing behaviors for comp part that produce audibly different results.
    /// </summary>
    public enum CompBehavior
    {
        /// <summary>
        /// Sparse anchors: mostly strong beats, fewer hits, longer sustains.
        /// Typical for: Verse (low energy), Intro, Outro.
        /// </summary>
        SparseAnchors,
        
        /// <summary>
        /// Standard pattern: balanced strong/weak beats, moderate sustains.
        /// Typical for: Verse (mid energy), Bridge.
        /// </summary>
        Standard,
        
        /// <summary>
        /// Push/anticipate: adds anticipations into chord changes, shorter notes.
        /// Typical for: PreChorus, build sections.
        /// </summary>
        Anticipate,
        
        /// <summary>
        /// Syncopated chop: more offbeats, short durations, frequent re-attacks.
        /// Typical for: Chorus (high energy), Dance sections.
        /// </summary>
        SyncopatedChop,
        
        /// <summary>
        /// Driving full: all available onsets, consistent attacks, driving feel.
        /// Typical for: Chorus (max energy), Outro (big ending).
        /// </summary>
        DrivingFull
    }

    /// <summary>
    /// Deterministic selector for comp behavior based on musical context.
    /// </summary>
    public static class CompBehaviorSelector
    {
        /// <summary>
        /// Selects comp behavior deterministically from context.
        /// </summary>
        /// <param name="sectionType">Current section type (Verse, Chorus, etc.)</param>
        /// <param name="absoluteSectionIndex">0-based index of section in song</param>
        /// <param name="barIndexWithinSection">0-based bar index within section</param>
        /// <param name="energy">Section energy [0..1]</param>
        /// <param name="busyProbability">Comp busy probability [0..1]</param>
        /// <param name="seed">Master seed for deterministic variation</param>
        /// <returns>Selected CompBehavior</returns>
        public static CompBehavior SelectBehavior(
            MusicConstants.eSectionType sectionType,
            int absoluteSectionIndex,
            int barIndexWithinSection,
            double energy,
            double busyProbability,
            int seed)
        {
            // Clamp inputs
            energy = Math.Clamp(energy, 0.0, 1.0);
            busyProbability = Math.Clamp(busyProbability, 0.0, 1.0);

            // Combined activity score
            double activityScore = (energy * 0.6) + (busyProbability * 0.4);

            // Section-type specific thresholds and biases
            CompBehavior baseBehavior = sectionType switch
            {
                MusicConstants.eSectionType.Intro => activityScore < 0.4 ? CompBehavior.SparseAnchors : CompBehavior.Standard,
                MusicConstants.eSectionType.Verse => SelectVerseBehavior(activityScore),
                MusicConstants.eSectionType.Chorus => SelectChorusBehavior(activityScore),
                MusicConstants.eSectionType.Bridge => activityScore > 0.6 ? CompBehavior.Anticipate : CompBehavior.Standard,
                MusicConstants.eSectionType.Outro => activityScore > 0.7 ? CompBehavior.DrivingFull : CompBehavior.SparseAnchors,
                MusicConstants.eSectionType.Solo => CompBehavior.SparseAnchors, // Back off for solo
                _ => CompBehavior.Standard
            };

            // Apply deterministic per-bar variation using seed
            // Every 4th bar within section, consider upgrade/downgrade
            if (barIndexWithinSection > 0 && barIndexWithinSection % 4 == 0)
            {
                int variationHash = HashCode.Combine(seed, absoluteSectionIndex, barIndexWithinSection);
                bool shouldVary = (variationHash % 100) < 30; // 30% chance of variation
                
                if (shouldVary)
                {
                    baseBehavior = ApplyVariation(baseBehavior, variationHash);
                }
            }

            return baseBehavior;
        }

        private static CompBehavior SelectVerseBehavior(double activityScore)
        {
            return activityScore switch
            {
                < 0.35 => CompBehavior.SparseAnchors,
                < 0.55 => CompBehavior.Standard,
                < 0.75 => CompBehavior.Anticipate,
                _ => CompBehavior.SyncopatedChop
            };
        }

        private static CompBehavior SelectChorusBehavior(double activityScore)
        {
            return activityScore switch
            {
                < 0.3 => CompBehavior.Standard,
                < 0.5 => CompBehavior.Anticipate,
                < 0.75 => CompBehavior.SyncopatedChop,
                _ => CompBehavior.DrivingFull
            };
        }

        private static CompBehavior ApplyVariation(CompBehavior baseBehavior, int variationHash)
        {
            // Deterministic upgrade/downgrade based on hash
            bool upgrade = (variationHash % 2) == 0;
            
            return baseBehavior switch
            {
                CompBehavior.SparseAnchors => upgrade ? CompBehavior.Standard : CompBehavior.SparseAnchors,
                CompBehavior.Standard => upgrade ? CompBehavior.Anticipate : CompBehavior.SparseAnchors,
                CompBehavior.Anticipate => upgrade ? CompBehavior.SyncopatedChop : CompBehavior.Standard,
                CompBehavior.SyncopatedChop => upgrade ? CompBehavior.DrivingFull : CompBehavior.Anticipate,
                CompBehavior.DrivingFull => upgrade ? CompBehavior.DrivingFull : CompBehavior.SyncopatedChop,
                _ => baseBehavior
            };
        }
    }
}
