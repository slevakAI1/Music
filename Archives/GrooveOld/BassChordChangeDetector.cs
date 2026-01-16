// AI: purpose=Detects upcoming chord changes to enable bass approach notes and pickups (Story 5.2).
// AI: invariants=LookaheadBeats is positive; detection considers (bar, beat) positions in harmony track.
// AI: deps=Used by Generator.GenerateBassTrack; relies on HarmonyTrack.GetActiveHarmonyEvent.
// AI: change=When adding chromatic approaches, update IsChangeImminent to support policy gates.

namespace Music.Generator
{
    /// <summary>
    /// Detects when the next harmony change is imminent to enable approach notes and pickups.
    /// Implements Story 5.2: Chord-change awareness.
    /// </summary>
    public static class BassChordChangeDetector
    {
        // Default lookahead distance in beats (typically 1-2 beats)
        private const decimal DefaultLookaheadBeats = 2m;

        /// <summary>
        /// Detects if a harmony change is imminent within N beats from the current position.
        /// </summary>
        /// <param name="harmonyTrack">Harmony track to search.</param>
        /// <param name="currentBar">Current bar position (1-based).</param>
        /// <param name="currentBeat">Current beat position (1-based decimal).</param>
        /// <param name="currentHarmony">Current active harmony event.</param>
        /// <param name="lookaheadBeats">Number of beats to look ahead (default: 2).</param>
        /// <returns>True if a chord change is detected within lookahead window.</returns>
        public static bool IsChangeImminent(
            HarmonyTrack harmonyTrack,
            int currentBar,
            decimal currentBeat,
            HarmonyEvent currentHarmony,
            decimal lookaheadBeats = DefaultLookaheadBeats)
        {
            if (harmonyTrack == null || currentHarmony == null)
                return false;

            if (lookaheadBeats <= 0)
                lookaheadBeats = DefaultLookaheadBeats;

            // Calculate target position (current + lookahead)
            decimal targetBeat = currentBeat + lookaheadBeats;
            int targetBar = currentBar;

            // Simple assumption: 4 beats per bar (could be enhanced to use time signature)
            const int beatsPerBar = 4;
            while (targetBeat > beatsPerBar)
            {
                targetBeat -= beatsPerBar;
                targetBar++;
            }

            // Get the harmony event active at the target position
            var futureHarmony = harmonyTrack.GetActiveHarmonyEvent(targetBar, targetBeat);

            if (futureHarmony == null)
                return false;

            // Check if the future harmony is different from current harmony
            // Compare by position rather than content to detect any change
            bool isDifferent = 
                futureHarmony.StartBar != currentHarmony.StartBar ||
                futureHarmony.StartBeat != currentHarmony.StartBeat;

            return isDifferent;
        }

        /// <summary>
        /// Gets the next harmony event after the current position.
        /// </summary>
        /// <param name="harmonyTrack">Harmony track to search.</param>
        /// <param name="currentBar">Current bar position (1-based).</param>
        /// <param name="currentBeat">Current beat position (1-based decimal).</param>
        /// <returns>Next harmony event, or null if none found.</returns>
        public static HarmonyEvent? GetNextHarmonyEvent(
            HarmonyTrack harmonyTrack,
            int currentBar,
            decimal currentBeat)
        {
            if (harmonyTrack == null || harmonyTrack.Events.Count == 0)
                return null;

            // Find first event with (StartBar, StartBeat) > (currentBar, currentBeat)
            return harmonyTrack.Events
                .OrderBy(e => e.StartBar)
                .ThenBy(e => e.StartBeat)
                .FirstOrDefault(e => 
                    e.StartBar > currentBar || 
                    (e.StartBar == currentBar && e.StartBeat > currentBeat));
        }

        /// <summary>
        /// Determines if an approach note should be inserted based on onset availability and policy.
        /// </summary>
        /// <param name="onsetSlots">Available onset slots in the bar.</param>
        /// <param name="currentSlotIndex">Current slot index being processed.</param>
        /// <param name="allowApproaches">Whether approach notes are enabled by policy.</param>
        /// <returns>True if an approach note can be inserted at this position.</returns>
        public static bool ShouldInsertApproach(
            IReadOnlyList<OnsetSlot> onsetSlots,
            int currentSlotIndex,
            bool allowApproaches)
        {
            if (!allowApproaches)
                return false;

            if (onsetSlots == null || onsetSlots.Count == 0)
                return false;

            if (currentSlotIndex < 0 || currentSlotIndex >= onsetSlots.Count)
                return false;

            // Check if current slot is a weak beat (not strong beat)
            var currentSlot = onsetSlots[currentSlotIndex];
            if (currentSlot.IsStrongBeat)
                return false;

            // Ensure there's a next slot available (for the target of the approach)
            // This handles the "pickup into next bar" case
            bool hasNextSlot = currentSlotIndex < onsetSlots.Count - 1;

            return hasNextSlot;
        }

        /// <summary>
        /// Calculates an appropriate diatonic approach note to the target chord root.
        /// </summary>
        /// <param name="targetRootMidi">Target chord root MIDI note.</param>
        /// <param name="approachFromBelow">True to approach from below, false for above.</param>
        /// <returns>MIDI note number for the approach note (whole step away from target).</returns>
        public static int CalculateDiatonicApproach(int targetRootMidi, bool approachFromBelow = true)
        {
            // Diatonic approach: whole step (2 semitones) from target
            // Story 5.2 specifies: "Keep rules strict initially (no chromatic yet)"
            int semitoneOffset = approachFromBelow ? -2 : 2;
            return targetRootMidi + semitoneOffset;
        }
    }
}
