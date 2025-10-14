using System.Collections.Generic;

namespace Music.Services
{
    public interface IMidiPlaybackService
    {
        IEnumerable<string> EnumerateOutputDevices();
        void SelectOutput(string name);
        void Play(MidiSongDocument doc);
        void Stop();
    }
}