namespace MusicXml.Domain
{
	/// <summary>
	/// Represents a time modification for tuplets in MusicXML.
	/// Defines the rhythmic ratio for how many "actual" notes fit into the duration of "normal" notes.
	/// </summary>
	public class TimeModification
	{
		/// <summary>
		/// The number of notes being played (e.g., 3 notes in a triplet).
		/// </summary>
		public int ActualNotes { get; set; }

		/// <summary>
		/// The number of notes that would normally take up that time (e.g., 2 quarters).
		/// </summary>
		public int NormalNotes { get; set; }

		/// <summary>
		/// Optional: the rhythmic value of the "normal" side (e.g., "quarter", "eighth").
		/// If null, it's implied by the context.
		/// </summary>
		public string? NormalType { get; set; }
	}
}