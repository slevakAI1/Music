namespace MusicXml.Domain
{
	public class MeasureAttributes
	{
		public MeasureAttributes()
		{
			Divisions = 0;
			Clefs = new List<Clef>();
		}

		public int Divisions { get; set; }
		
		public Key? Key { get; set; }
		
		public Time? Time { get; set; }

		/// <summary>
		/// Legacy single clef property - maintained for backward compatibility.
		/// For multi-staff parts, use the Clefs list instead.
		/// </summary>
		public Clef? Clef { get; set; }

		/// <summary>
		/// Number of staves for this part (e.g., 2 for piano).
		/// If absent or 1, the part has a single staff.
		/// </summary>
		public int? Staves { get; set; }

		/// <summary>
		/// List of clefs for multi-staff parts.
		/// Each clef can have a Number property indicating which staff it applies to.
		/// </summary>
		public List<Clef> Clefs { get; set; }
	}
}
