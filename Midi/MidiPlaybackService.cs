// AI: purpose=Manage MIDI playback via DryWetMidi OutputDevice and Playback; simple lifecycle wrapper for UI usage.
// AI: invariants=Playback (_playback) is tied to an OutputDevice; Stop disposes both playback and device; Resume only valid after Pause before Stop.
// AI: deps=Relies on MidiSongDocument.Raw providing GetPlayback(OutputDevice); changing that breaks Play behavior.
// AI: thread-safety=Not thread-safe; intended for UI thread calls; perform heavy I/O off UI thread if needed.

using Melanchall.DryWetMidi.Multimedia;

namespace Music.MyMidi
{
    public class MidiPlaybackService
    {
        private OutputDevice? _outputDevice;
        private Playback? _playback;

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
        public void Play(MidiSongDocument doc)
        {
            // Stop any existing playback first
            Stop();

            // Get or create output device
            if (_outputDevice == null)
            {
                _outputDevice = OutputDevice.GetAll().FirstOrDefault();
            }

            if (_outputDevice == null)
                return;

            _playback = doc.Raw.GetPlayback(_outputDevice);
            _playback.Start();
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

        private bool _isPaused = false;

        // AI: Stop stops and disposes playback and output device, resetting internal state. After Stop(), Resume() will not work.
        public void Stop()
        {
            _playback?.Stop();
            _playback?.Dispose();
            _playback = null;
            _isPaused = false;

            _outputDevice?.Dispose();
            _outputDevice = null;
        }
    }
}