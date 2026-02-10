// AI: purpose=Manage MIDI playback via DryWetMidi OutputDevice and Playback; simple lifecycle wrapper for UI usage.
// AI: invariants=Playback (_playback) is tied to an OutputDevice; Stop disposes both playback and device; Resume only valid after Pause before Stop.
// AI: deps=Relies on MidiSongDocument.Raw providing GetPlayback(OutputDevice); changing that breaks Play behavior.
// AI: thread-safety=Not thread-safe; intended for UI thread calls; perform heavy I/O off UI thread if needed.

using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;

namespace Music.MyMidi
{
    public class MidiPlaybackService
    {
        private OutputDevice? _outputDevice;
        private Playback? _playback;
        private bool _isPaused = false;
        private CancellationTokenSource? _cancellationTokenSource;

        public IEnumerable<string> EnumerateOutputDevices()
        {
            return OutputDevice.GetAll().Select(d => d.Name);
        }

        // AI: SelectOutput disposes any previous device and selects first device with exact name match.
        // AI: note=If no match found _outputDevice becomes null; callers should verify available devices beforehand.
        public void SelectOutput(string name)
        {
            _outputDevice?.Dispose();
            _outputDevice = OutputDevice.GetAll().FirstOrDefault(d => d.Name == name);
        }

        // AI: Play: stops existing playback, ensures an output device exists (auto-selects first), then starts playback.
        // AI: edge=If no output device is available Play returns silently; callers should check EnumerateOutputDevices first.
        // AI: fix=Always get fresh output device to avoid using disposed device from previous Stop().
        public void Play(MidiSongDocument doc)
        {
            // Stop any existing playback first
            Stop();

            // Create new cancellation token for this playback session
            _cancellationTokenSource = new CancellationTokenSource();

            // Always get a fresh output device (disposed by previous Stop)
            _outputDevice = OutputDevice.GetAll().FirstOrDefault();

            if (_outputDevice == null)
            {
                return;
            }
            _playback = doc.Raw.GetPlayback(_outputDevice);
            _playback.Start();
        }

        // AI: GetCancellationToken: provides token for Task.Delay to allow cancellation when Stop is called.
        public CancellationToken GetCancellationToken()
        {
            return _cancellationTokenSource?.Token ?? CancellationToken.None;
        }

        // AI: Pause stops playback but preserves _playback for Resume; sets _isPaused flag. Do not call Stop() if you plan to Resume().
        public void Pause()
        {
            if (_playback?.IsRunning == true)
            {
                _playback.Stop();
                // Store pause state flag so Resume() knows not to restart from beginning
                _isPaused = true;
            }
        }

        // AI: Resume restarts the existing _playback only if previously paused. Resume after Stop() is a no-op.
        public void Resume()
        {
            if (_isPaused && _playback != null)
            {
                _playback.Start();
                _isPaused = false;
            }
        }

        public bool IsPlaying => _playback?.IsRunning ?? false;

        public bool IsPaused => _isPaused;

        /// <summary>
        /// Gets the current playback position in MIDI ticks. Returns 0 when not playing.
        /// </summary>
        public long CurrentTick
        {
            get
            {
                var playback = _playback;
                if (playback == null)
                    return 0;

                var time = playback.GetCurrentTime(TimeSpanType.Midi);
                return time is MidiTimeSpan midiTime ? midiTime.TimeSpan : 0;
            }
        }

        // AI: Stop stops and disposes playback and output device, resetting internal state. After Stop(), Resume() will not work.
        public void Stop()
        {
            // Cancel any pending Task.Delay to prevent orphaned continuations
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            if (_playback != null)
            {
                _playback.Stop();
                _playback.Dispose();
                _playback = null;
            }

            _isPaused = false;

            if (_outputDevice != null)
            {
                _outputDevice.Dispose();
                _outputDevice = null;
            }
        }
    }
}
