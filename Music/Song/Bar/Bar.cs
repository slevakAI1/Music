// AI: purpose=Represents a musical bar; authoritative StartTick; caches timing values; holds section context.
// AI: invariants=Numerator>0; Denominator!=0; StartTick init-only; caches stale if TS fields change.
// AI: deps=MusicConstants.TicksPerQuarterNote; SectionTrack populates section fields; thread-safety=none.

namespace Music.Generator
{
    // AI: type=Bar; owns=bar timing/TS; not=validation heavy; callers maintain TS validity and Start/End consistency.
    public class Bar
    {
        // AI: logical position; no enforcement of 0/1 base; callers maintain uniqueness/order if required.
        public int BarNumber;

        // AI: section context for this bar; set during BarTrack rebuild; may be null when unknown.
        public Section? Section { get; set; }

        // AI: 0-based index within section; valid when Section is set; 0 at section start.
        public int BarWithinSection { get; set; }

        // AI: bars remaining until section end; valid when Section is set; 0 at last bar.
        public int BarsUntilSectionEnd { get; set; }

        // AI: authoritative start tick for events in this bar - set by RebuildFromTimingTrack()
        public long StartTick { get; set; }

        // AI: absolute tick for bar end - set by RebuildFromTimingTrack()
        public long EndTick { get; set; }  
        
        // AI: TS numerator; must be positive; changing after cache access yields stale cached values.
        public int Numerator;

        // AI: TS denominator; must be non-zero and a valid musical subdivision; changing after cache access stale.
        public int Denominator;

        // AI: cached derived values; lazily computed; not thread-safe; to refresh set these to null where used.
        private int? _ticksPerMeasure;
        private int? _ticksPerBeat;
        private int? _beatsPerBar;

        // AI: computed=(TicksPerQuarterNote*4*Numerator)/Denominator; integer division truncates remainder; cached.
        public int TicksPerMeasure => _ticksPerMeasure ??= (MusicConstants.TicksPerQuarterNote * 4 * Numerator) / Denominator;

        // AI: computed=TicksPerMeasure/Numerator; integer division truncates; cached; depends on TicksPerMeasure.
        public int TicksPerBeat => _ticksPerBeat ??= TicksPerMeasure / Numerator;

        // AI: computed=musical beats in bar; heuristic: for 6/8,9/8,12/8 returns Numerator/3; cached.
        public int BeatsPerBar => _beatsPerBar ??= CalculateBeatsPerBar();

        // AI: computed=phrase position within section [0..1]; 0 for null/short sections.
        public double PhrasePosition => ComputePhrasePosition();

        // AI: computed=true if first or last bar of section; depends on BarWithinSection and BarsUntilSectionEnd.
        public bool IsAtSectionBoundary => BarWithinSection == 0 || BarsUntilSectionEnd == 0;

        // AI: computed=true if within fill window (default 2 bars from section end); phrase-end fill triggers.
        public bool IsFillWindow => BarsUntilSectionEnd <= 2;

        // AI: computed=backbeat positions for current time signature; cached for efficiency.
        public IReadOnlyList<int> BackbeatBeats => _backbeatBeats ??= ComputeBackbeatBeats();
        private IReadOnlyList<int>? _backbeatBeats;


        // AI: heuristic for compound meters: if Denominator==8 and Numerator divisible by 3 and >=6, group beats by 3.
        private int CalculateBeatsPerBar()
        {
            // Detect common compound meters: 6/8, 9/8, 12/8
            // In compound meters, the beat is grouped in threes (dotted quarter = 3 eighths)
            if (Numerator >= 6 && Numerator % 3 == 0 && Denominator == 8)
            {
                return Numerator / 3;
            }

            // Default: simple meter - beats per bar equals numerator
            return Numerator;
        }

        // AI: phrase position uses section length; returns 0.0 when section missing or single-bar.
        private double ComputePhrasePosition()
        {
            if (Section == null)
            {
                return 0.0;
            }

            int totalBars = Section.BarCount;
            if (totalBars <= 1)
            {
                return 0.0;
            }

            return (double)BarWithinSection / (totalBars - 1);
        }

        // AI: backbeat positions vary by time signature; 4/4=[2,4], 3/4=[2], 6/8=[4], etc.
        private IReadOnlyList<int> ComputeBackbeatBeats()
        {
            int beats = BeatsPerBar;
            return beats switch
            {
                2 => [2],
                3 => [2],
                4 => [2, 4],
                5 => [3, 5],
                6 => [4],
                7 => [3, 5, 7],
                _ => beats >= 4
                    ? Enumerable.Range(1, beats).Where(b => b % 2 == 0).ToList()
                    : [beats > 1 ? 2 : 1]
            };
        }
    }
}
