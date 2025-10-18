using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System.Collections.Generic;
using System.Linq;

namespace Music.Services
{
    internal class MidiPlaybackService : IMidiPlaybackService
    {
        private OutputDevice? _outputDevice;
        private Playback? _playback;

        public IEnumerable<string> EnumerateOutputDevices()
        {
            return OutputDevice.GetAll().Select(d => d.Name);
        }

        public void SelectOutput(string name)
        {
            _outputDevice = OutputDevice.GetAll().FirstOrDefault(d => d.Name == name);
        }

        public void Play(MidiSongDocument doc)
        {
            // Use default output device (first available)
            _outputDevice ??= OutputDevice.GetAll().FirstOrDefault();
            if (_outputDevice == null)
                return;

            _playback?.Stop();
            _playback?.Dispose();

            _playback = doc.Raw.GetPlayback(_outputDevice);
            _playback.Start();
        }

        public void Stop()
        {
            _playback?.Stop();
            _playback?.Dispose();
            _playback = null;
        }
    }
}