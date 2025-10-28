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

	public class Metronome
	{
		// e.g. "quarter"
		public string BeatUnit { get; set; }

		// e.g. 112
		public int PerMinute { get; set; }
	}

	public class Sound
	{
		// tempo attribute value
		public decimal Tempo { get; set; }
	}
}