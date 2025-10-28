using System;

namespace MusicXml.Domain
{
	public class Direction
	{
		public DirectionType DirectionType { get; set; }
		public Sound Sound { get; set; }
	}

	public class DirectionType
	{
		public Metronome Metronome { get; set; }
	}

    // Important: This shows on the score only, that's why it can be text.
    public class Metronome
	{
		// e.g. "quarter"
		public string BeatUnit { get; set; }

		// e.g. 112
		public int PerMinute { get; set; }
	}

    // Important: This sets the actual playback tempo
    public class Sound
	{
		// tempo attribute value
		public decimal Tempo { get; set; }
	}
}