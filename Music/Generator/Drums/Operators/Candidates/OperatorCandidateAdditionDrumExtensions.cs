// AI: purpose=Story 1 temporary helpers to read drum-specific fields from OperatorCandidateAddition.Metadata
// AI: note=Story 3 will replace this with proper drum-specific candidate type; these are bridge methods only
// AI: deps=OperatorCandidateAddition (Core), OnsetStrength, FillRole, DrumArticulation (Drums)

using Music.Generator.Core;
using Music.Generator.Drums.Planning;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.Candidates
{
    // AI: purpose=Extension methods to extract drum fields from Metadata dictionary (temporary bridge for Story 1-2)
    public static class OperatorCandidateAdditionDrumExtensions
    {
        // AI: extract=Get OnsetStrength from Metadata; returns Strong if missing
        public static OnsetStrength GetStrength(this OperatorCandidateAddition candidate)
        {
            if (candidate.Metadata != null && candidate.Metadata.TryGetValue("Strength", out var value))
            {
                return (OnsetStrength)value;
            }
            return OnsetStrength.Strong;
        }

        // AI: extract=Get FillRole from Metadata; returns None if missing
        public static FillRole GetFillRole(this OperatorCandidateAddition candidate)
        {
            if (candidate.Metadata != null && candidate.Metadata.TryGetValue("FillRole", out var value))
            {
                return (FillRole)value;
            }
            return FillRole.None;
        }

        // AI: extract=Get DrumArticulation from Metadata; returns null if missing
        public static DrumArticulation? GetArticulationHint(this OperatorCandidateAddition candidate)
        {
            if (candidate.Metadata != null && candidate.Metadata.TryGetValue("ArticulationHint", out var value))
            {
                return (DrumArticulation)value;
            }
            return null;
        }
    }
}
