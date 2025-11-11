using System.Xml;

namespace MusicXml.Domain
{
	public class Note
	{
		public Note()
		{
			Type = string.Empty;
			Duration = -1;
			Voice = -1;
			Staff = -1;
			IsChordTone = false;
		}

		public string Type { get; set; }
		
		public int Voice { get; set; }

		public int Duration { get; set; }

		public Lyric? Lyric { get; set; }
		
		public Pitch? Pitch { get; set; }

		/// <summary>
		/// Staff number for multi-staff parts (1-based).
		/// Indicates which staff this note appears on (e.g., 1 for treble, 2 for bass in piano).
		/// A value of -1 or 0 indicates no specific staff assignment (single-staff part).
		/// </summary>
		public int Staff { get; set; }

		public bool IsChordTone { get; set; }

		public bool IsRest { get; set; }

		public bool IsGrace { get; set; }

		public Tie Tie { get; set; }
		
		public string? Accidental { get; set; }

		/// <summary>
		/// Time modification for tuplets. If null, no tuplet modification is applied.
		/// </summary>
		public TimeModification? TimeModification { get; set; }

		/// <summary>
		/// Tuplet notation for visual bracket display.
		/// </summary>
		public TupletNotation? TupletNotation { get; set; }
	}
}
