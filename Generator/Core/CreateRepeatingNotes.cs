using Music.MyMidi;

namespace Music.Generator
{
    /// <summary>
    /// Transforms WriterFormData to PartTrack objects with MIDI tick-based timing.
    /// </summary>
    public static class CreateRepeatingNotes
    {
        /// <summary>
        /// Creates a PartTrack with a repeating set of the specified MIDI note number.
        /// </summary>
        /// <param name="noteNumber">The MIDI note number (0-127). Use 60 for Middle C.</param>
        /// <param name="repeatCount">Number of times to repeat the note.</param>
        /// <param name="noteDurationTicks">Duration of each note in MIDI ticks. Default is 480 (quarter note).</param>
        /// <param name="noteOnVelocity">MIDI velocity (0-127). Default is 100.</param>
        /// <returns>A PartTrack object containing the repeating notes.</returns>
        public static PartTrack Execute(
            int noteNumber,
            int noteDurationTicks,
            int repeatCount,
            int noteOnVelocity = 100)
        {
            var SongTrackNoteEvents = new List<MetaMidiEvent>();
            int currentPosition = 0;

            for (int i = 0; i < repeatCount; i++)
            {
                var songTrackNoteEvent = new MetaMidiEvent(
                    noteNumber: noteNumber,
                    absolutePositionTicks: currentPosition,
                    noteDurationTicks: noteDurationTicks,
                    noteOnVelocity: noteOnVelocity);

                SongTrackNoteEvents.Add(songTrackNoteEvent);
                currentPosition += noteDurationTicks;
            }

            var songTrack = new PartTrack(SongTrackNoteEvents);
            return songTrack;
        }
    }
}
