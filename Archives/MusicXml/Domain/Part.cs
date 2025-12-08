using System.Collections.Generic;

namespace MusicXml.Domain
{
	public class Part
	{
		public Part()
		{
			Id = string.Empty;
			Name = string.Empty; 
			Measures = new List<Measure>();
			InstrumentName = string.Empty;
			MidiChannel = -1;
		}

		public string Id { get; internal set; }
		
		public string Name { get; internal set; }
		
		public List<Measure> Measures { get; internal set; }

		// Playback/instrument mapping (for part-list)
		// If empty/-1, serializer will omit the optional blocks.
		public string InstrumentName { get; internal set; } // e.g., "Violins I"
		public int MidiChannel { get; internal set; }        // 1–16, -1 = unset
	}
}
