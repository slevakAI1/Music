using Melanchall.DryWetMidi.Multimedia;

namespace Music.MyMidi
{
    internal class MidiPlaybackService
    {
        private OutputDevice? _outputDevice;
        private Playback? _playback;

        public IEnumerable<string> EnumerateOutputDevices()
        {
            return OutputDevice.GetAll().Select(d => d.Name);
        }

        public void SelectOutput(string name)
        {
            _outputDevice?.Dispose();
            _outputDevice = OutputDevice.GetAll().FirstOrDefault(d => d.Name == name);
        }

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

        /// <summary>
        /// Pauses playback at the current position. Call Resume() to continue from this point.
        /// </summary>
        public void Pause()
        {
            if (_playback?.IsRunning == true)
            {
                _playback.Stop();
                // Store pause state flag so Resume() knows not to restart from beginning
                _isPaused = true;
            }
        }

        /// <summary>
        /// Resumes playback from the paused position.
        /// </summary>
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