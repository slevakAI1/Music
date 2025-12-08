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

        public void Stop()
        {
            _playback?.Stop();
            _playback?.Dispose();
            _playback = null;

            _outputDevice?.Dispose();
            _outputDevice = null;
        }
    }
}