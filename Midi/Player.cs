using Music.Tests;

namespace Music.Writer
{
    // Helper class extracted from WriterForm for non-event, non-lifecycle logic.
    internal static class Player
    {
        /// <summary>
        /// Plays a MIDI document and releases the MIDI device after playback completes.
        /// (Moved from WriterForm.PlayMidiFromPhrasesAsync)
        /// </summary>
        internal static async Task PlayMidiFromPhrasesAsync(
            IMidiPlaybackService playbackService,
            MidiSongDocument midiDoc,
            Form owner)
        {
            if (midiDoc == null)
                throw new ArgumentNullException(nameof(midiDoc));

            // Always stop any existing playback first
            playbackService.Stop();

            // Select first available output device
            var devices = playbackService.EnumerateOutputDevices();
            var first = default(string);
            foreach (var d in devices)
            {
                first = d;
                break;
            }
            if (first == null)
            {
                MessageBox.Show(owner, "No MIDI output device found.", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            playbackService.SelectOutput(first);
            playbackService.Play(midiDoc);

            // Wait for playback duration plus buffer
            var duration = midiDoc?.Duration ?? TimeSpan.Zero;
            var totalDelay = duration.TotalMilliseconds + 250;

            if (totalDelay > 0)
                await Task.Delay((int)Math.Min(totalDelay, int.MaxValue));

            // Always stop to release resources
            playbackService.Stop();
        }
    }
}