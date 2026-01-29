// AI: purpose=Play a MidiSongDocument end-to-end using MidiPlaybackService; auto-selects an output device and cleans up.
// AI: invariants=Chooses first available output device; if none found shows error and returns; always stops to release resources.
// AI: deps=Relies on MidiPlaybackService API, MidiSongDocument.Duration, and MessageBoxHelper for UI errors.
// AI: perf=Async waits using Task.Delay for duration+buffer; long songs will asynchronously yield but still occupy workflow.

using Music.Writer;

namespace Music.MyMidi
{
    // Helper class extracted from WriterForm for non-event, non-lifecycle logic.
    internal static class Player
    {
        /// <summary>
        /// Plays a MIDI document and releases the MIDI device after playback completes.
        /// </summary>
        // AI: PlayMidiFromSongTracksAsync: starts playback, awaits duration+250ms, then stops to free device resources.
        // AI: edge=Duration==0 yields immediate stop after short buffer; callers may prefer tighter control for live interactions.
        // AI: fix=Task.Delay now uses CancellationToken to prevent orphaned continuations when Stop is called mid-playback.
        internal static async Task PlayMidiFromSongTracksAsync(
            MidiPlaybackService playbackService,
            MidiSongDocument midiDoc)
        {
            Tracer.DebugTrace("=== Player.PlayMidiFromSongTracksAsync START ===");

            if (midiDoc == null)
            {
                Tracer.DebugTrace("Player: midiDoc is null, throwing exception");
                throw new ArgumentNullException(nameof(midiDoc));
            }

            // Always stop any existing playback first
            Tracer.DebugTrace("Player: Stopping any existing playback");
            playbackService.Stop();

            // Select first available output device
            var devices = playbackService.EnumerateOutputDevices();
            var first = default(string);
            foreach (var d in devices)
            {
                first = d;
                break;
            }

            Tracer.DebugTrace($"Player: Found output device: {first ?? "<none>"}");

            if (first == null)
            {
                Tracer.DebugTrace("Player: No MIDI output device found, showing error");
                MessageBoxHelper.Show("No MIDI output device found.", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            playbackService.SelectOutput(first);

            Tracer.DebugTrace("Player: Starting playback");
            playbackService.Play(midiDoc);

            // Get cancellation token for this playback session
            var cancellationToken = playbackService.GetCancellationToken();
            Tracer.DebugTrace($"Player: Got CancellationToken, CanBeCanceled={cancellationToken.CanBeCanceled}");

            // Wait for playback duration plus buffer
            var duration = midiDoc?.Duration ?? TimeSpan.Zero;
            var totalDelay = duration.TotalMilliseconds + 250;

            Tracer.DebugTrace($"Player: Duration={duration.TotalMilliseconds}ms, TotalDelay={totalDelay}ms");
            Tracer.DebugTrace($"Player: IsPlaying={playbackService.IsPlaying}, IsPaused={playbackService.IsPaused}");

            if (totalDelay > 0)
            {
                try
                {
                    Tracer.DebugTrace($"Player: Beginning cancellable Task.Delay({(int)Math.Min(totalDelay, int.MaxValue)}ms)");
                    await Task.Delay((int)Math.Min(totalDelay, int.MaxValue), cancellationToken);
                    Tracer.DebugTrace("Player: Task.Delay completed normally");
                }
                catch (OperationCanceledException)
                {
                    Tracer.DebugTrace("Player: Task.Delay was CANCELLED (Stop was called)");
                    // Expected when Stop() is called - just return without calling Stop again
                    return;
                }
            }

            // Always stop to release resources (only if we weren't cancelled)
            Tracer.DebugTrace("Player: Calling Stop to release resources");
            playbackService.Stop();
            Tracer.DebugTrace("=== Player.PlayMidiFromSongTracksAsync END ===");
        }
    }
}
