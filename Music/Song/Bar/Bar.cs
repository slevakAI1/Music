// AI: purpose=Represents a musical bar; authoritative StartTick; caches timing values.
// AI: invariants=Numerator>0; Denominator!=0; StartTick init-only; caches stale if TS fields change.
// AI: deps=MusicConstants.TicksPerQuarterNote; thread-safety=none; change=when altering TS, clear caches & notify callers.

namespace Music.Generator
{
    // AI: type=Bar; owns=bar timing/TS; not=validation heavy; callers maintain TS validity and Start/End consistency.
    public class Bar
    {
        // AI: logical position; no enforcement of 0/1 base; callers maintain uniqueness/order if required.
        public int BarNumber;

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
    }
}
