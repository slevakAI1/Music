namespace MusicXml.Domain
{
	public class Clef
	{
		public Clef()
		{
			Line = 0;
			Sign = string.Empty;
		}

		public int Line { get; internal set; }
		
		public string Sign { get; internal set; }
	}
}
