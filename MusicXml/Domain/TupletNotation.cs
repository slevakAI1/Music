namespace MusicXml.Domain
{
	/// <summary>
	/// Represents tuplet notation for visual display (bracket/number).
	/// </summary>
	public class TupletNotation
	{
		/// <summary>
		/// Type of tuplet notation: "start", "stop", or null (middle of tuplet).
		/// </summary>
		public string? Type { get; set; }

		/// <summary>
		/// Tuplet number (typically 1, but can be higher for nested tuplets).
		/// </summary>
		public int Number { get; set; } = 1;
	}
}