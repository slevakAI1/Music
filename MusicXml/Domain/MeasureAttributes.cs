namespace MusicXml.Domain
{
	public class MeasureAttributes
	{
		public MeasureAttributes()
		{
			Divisions = 0;
		}

		public int Divisions { get; internal set; }
		
		public Key Key { get; internal set; }
		
		public Time Time { get; internal set; }

		// Proxies to the hierarchical <time> element
		public int Beats
		{
			get => Time?.Beats ?? 0;
			internal set
			{
				if (Time == null) Time = new Time();
				Time.Beats = value;
			}
		}

		// BeatType maps to Time.Mode (MusicXML uses a numeric string here like "4")
		public string BeatType
		{
			get => Time?.Mode;
			internal set
			{
				if (Time == null) Time = new Time();
				Time.Mode = value;
			}
		}
		 
		public Clef Clef { get; internal set; }
	}
}
