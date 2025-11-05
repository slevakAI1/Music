namespace MusicXml.Domain
{
	public class Clef
	{
		public Clef()
		{
			Line = 0;
			Sign = string.Empty;
		}

		public int Line { get; set; }
		
		public string Sign { get; set; }

		/// <summary>
		/// Staff number for multi-staff parts (1-based).
		/// Null means the clef applies to a single-staff part or all staves.
		/// </summary>
		public int? Number { get; set; }

		/// <summary>
		/// Clef octave change (e.g., -1 for tenor treble clef).
		/// </summary>
		public int? ClefOctaveChange { get; set; }
	}
}
