using System.Collections.Generic;

namespace MusicXml.Domain
{
	public class Measure
	{
		public Measure()
		{
			Width = -1;
			MeasureElements = new List<MeasureElement>();
		}

		public decimal Width { get; internal set; }
		
		// This can be any musicXML element in the measure tag, i.e. note, backup, etc
		public List<MeasureElement> MeasureElements { get; internal set; }
		
		public MeasureAttributes Attributes { get; internal set; }

		// New: direction for the measure (tempo / metronome, sound)
		public Direction Direction { get; internal set; }
        /*
		 			<direction>
				<direction-type>
					<metronome default-y='20'>
						<beat-unit>quarter</beat-unit>
						<per-minute>112</per-minute>
					</metronome>
				</direction-type>
				<sound tempo='112'/>
			</direction>
		 */
    }
}
