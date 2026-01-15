// AI: purpose=Renders placed motifs into actual note events against harmony and groove context.
// AI: invariants=Output deterministic by inputs+seed; events sorted by AbsoluteTimeTicks; no overlaps; valid MIDI range.
// AI: deps=Consumes MotifSpec, MotifPlacement, HarmonyTrack, BarTrack, GroovePreset, ISongIntentQuery; produces PartTrack.
// AI: change=Story 9.2 implementation; converts MaterialLocal motif specs to SongAbsolute rendered tracks.

using Music.Generator;
using Music.MyMidi;

namespace Music.Song.Material;

/// <summary>
/// Renders motif specs into note sequences against song harmony and groove.
/// Story 9.2: converts MotifSpec + MotifPlacement into PartTrack in SongAbsolute domain.
/// </summary>
public static class MotifRenderer
{
    /// <summary>
    /// Renders a placed motif into a PartTrack (simplified generator-friendly overload).
    /// Used by role generators that have already built harmony contexts and onset grids.
    /// </summary>
    public static PartTrack Render(
        MotifSpec spec,
        MotifPlacement placement,
        IReadOnlyList<HarmonyPitchContext> harmonyContexts,
        IReadOnlyList<OnsetSlot> onsetGrid,
        int velocityAccentBias,
        int seed)
    {
        var events = new List<PartTrackEvent>();

        if (harmonyContexts.Count == 0 || onsetGrid.Count == 0)
            return CreateEmptyTrack(spec, placement, 0);

        int previousPitch = spec.Register.CenterMidiNote;

        // Render each onset
        for (int onsetIndex = 0; onsetIndex < onsetGrid.Count && onsetIndex < harmonyContexts.Count; onsetIndex++)
        {
            var slot = onsetGrid[onsetIndex];
            var harmonyContext = harmonyContexts[onsetIndex];

            // Calculate contour position (0..1 across all notes)
            double contourPosition = onsetGrid.Count > 1 ? (double)onsetIndex / (onsetGrid.Count - 1) : 0.5;

            // Select pitch
            int pitch = SelectPitch(
                spec,
                harmonyContext,
                contourPosition,
                slot.IsStrongBeat,
                previousPitch,
                0, // barOffset
                onsetIndex,
                seed);

            // Apply variation
            pitch = ApplyVariation(
                pitch,
                spec,
                placement,
                harmonyContext,
                contourPosition,
                0, // barOffset
                onsetIndex,
                seed);

            // Calculate velocity
            int velocity = 85 + velocityAccentBias;
            if (slot.IsStrongBeat)
                velocity += 10;
            velocity = Math.Clamp(velocity, 40, 127);

            // Duration from slot
            int duration = slot.DurationTicks;
            duration = ApplyDurationVariation(duration, placement);

            // Prevent overlaps
            duration = PreventOverlaps(events, slot.StartTick, duration);

            if (duration > 0)
            {
                events.Add(new PartTrackEvent(
                    noteNumber: pitch,
                    absoluteTimeTicks: (int)slot.StartTick,
                    noteDurationTicks: duration,
                    noteOnVelocity: velocity));

                previousPitch = pitch;
            }
        }

        var sortedEvents = events.OrderBy(e => e.AbsoluteTimeTicks).ToList();

        return new PartTrack(sortedEvents)
        {
            MidiProgramNumber = 0,
            MidiProgramName = spec.IntendedRole,
            Meta = new PartTrackMeta
            {
                TrackId = PartTrack.PartTrackId.NewId(),
                Name = $"{spec.Name} (rendered)",
                IntendedRole = spec.IntendedRole,
                Domain = PartTrackDomain.SongAbsolute,
                Kind = PartTrackKind.RoleTrack,
                MaterialKind = spec.Kind,
                Tags = spec.Tags
            }
        };
    }

    /// <summary>
    /// Renders a placed motif into a PartTrack with actual pitches and timing.
    /// </summary>
    /// <param name="spec">The motif specification (rhythm, contour, register, tone policy).</param>
    /// <param name="placement">Where/when the motif appears (section, bar, variation intensity, transforms).</param>
    /// <param name="harmonyTrack">Song harmony for pitch realization.</param>
    /// <param name="barTrack">Timing ruler for tick calculation.</param>
    /// <param name="groovePreset">Groove preset for onset slot alignment.</param>
    /// <param name="intentQuery">Tension/phrase context.</param>
    /// <param name="sectionTrack">Song section structure.</param>
    /// <param name="seed">Seed for deterministic pitch selection.</param>
    /// <param name="midiProgramNumber">MIDI program number for output track.</param>
    /// <returns>PartTrack in SongAbsolute domain with rendered notes.</returns>
    public static PartTrack Render(
        MotifSpec spec,
        MotifPlacement placement,
        HarmonyTrack harmonyTrack,
        BarTrack barTrack,
        GroovePreset groovePreset,
        ISongIntentQuery intentQuery,
        SectionTrack sectionTrack,
        int seed,
        int midiProgramNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(harmonyTrack);
        ArgumentNullException.ThrowIfNull(barTrack);
        ArgumentNullException.ThrowIfNull(groovePreset);
        ArgumentNullException.ThrowIfNull(intentQuery);
        ArgumentNullException.ThrowIfNull(sectionTrack);

        var events = new List<PartTrackEvent>();

        // Get section from section track to calculate absolute bar numbers
        if (placement.AbsoluteSectionIndex >= sectionTrack.Sections.Count)
            return CreateEmptyTrack(spec, placement, midiProgramNumber);

        var section = sectionTrack.Sections[placement.AbsoluteSectionIndex];
        int absoluteStartBar = section.StartBar + placement.StartBarWithinSection;

        // Get intent context for this section
        var sectionIntent = intentQuery.GetSectionIntent(placement.AbsoluteSectionIndex);

        // Track previous pitch for voice-leading
        int previousPitch = spec.Register.CenterMidiNote;

        // Render motif across its duration bars
        for (int barOffset = 0; barOffset < placement.DurationBars; barOffset++)
        {
            int absoluteBar = absoluteStartBar + barOffset;
            if (!barTrack.TryGetBar(absoluteBar, out var bar))
                continue;

            // Get bar-level intent
            int barIndexWithinSection = placement.StartBarWithinSection + barOffset;
            var barIntent = intentQuery.GetBarIntent(placement.AbsoluteSectionIndex, barIndexWithinSection);

            // Get harmony for this bar
            var harmonyEvent = harmonyTrack.GetActiveHarmonyEvent(absoluteBar, 1m);
            if (harmonyEvent == null)
                continue;

            var harmonyContext = HarmonyPitchContextBuilder.Build(harmonyEvent);

            // Map rhythm shape to this bar
            var onsetsForBar = MapRhythmToBar(
                spec.RhythmShape,
                barOffset,
                placement.DurationBars,
                groovePreset,
                absoluteBar,
                barTrack);

            // Render each onset
            for (int onsetIndex = 0; onsetIndex < onsetsForBar.Count; onsetIndex++)
            {
                var (absoluteTick, durationTicks, isStrongBeat) = onsetsForBar[onsetIndex];

                // Calculate contour position (0..1 across all notes)
                double contourPosition = CalculateContourPosition(barOffset, onsetIndex, onsetsForBar.Count, placement.DurationBars);

                // Select pitch based on contour, harmony, and tone policy
                int pitch = SelectPitch(
                    spec,
                    harmonyContext,
                    contourPosition,
                    isStrongBeat,
                    previousPitch,
                    barOffset,
                    onsetIndex,
                    seed);

                // Apply variation operators if needed
                pitch = ApplyVariation(
                    pitch,
                    spec,
                    placement,
                    harmonyContext,
                    contourPosition,
                    barOffset,
                    onsetIndex,
                    seed);

                // Calculate velocity from energy/tension
                int velocity = CalculateVelocity(
                    barIntent,
                    isStrongBeat,
                    contourPosition,
                    seed);

                // Apply duration multiplier based on variation
                int adjustedDuration = ApplyDurationVariation(durationTicks, placement);

                // Avoid overlaps with previous note
                adjustedDuration = PreventOverlaps(events, absoluteTick, adjustedDuration);

                if (adjustedDuration > 0)
                {
                    events.Add(new PartTrackEvent(
                        noteNumber: pitch,
                        absoluteTimeTicks: (int)absoluteTick,
                        noteDurationTicks: adjustedDuration,
                        noteOnVelocity: velocity));

                    previousPitch = pitch;
                }
            }
        }

        // Sort by time
        var sortedEvents = events.OrderBy(e => e.AbsoluteTimeTicks).ToList();

        return new PartTrack(sortedEvents)
        {
            MidiProgramNumber = midiProgramNumber,
            MidiProgramName = spec.IntendedRole,
            Meta = new PartTrackMeta
            {
                TrackId = PartTrack.PartTrackId.NewId(),
                Name = $"{spec.Name} (rendered)",
                IntendedRole = spec.IntendedRole,
                Domain = PartTrackDomain.SongAbsolute,
                Kind = PartTrackKind.RoleTrack,
                MaterialKind = spec.Kind,
                Tags = spec.Tags
            }
        };
    }

    /// <summary>
    /// Maps motif rhythm shape ticks to absolute song ticks for a specific bar.
    /// Returns list of (absoluteTick, durationTicks, isStrongBeat) tuples.
    /// </summary>
    private static List<(long AbsoluteTick, int DurationTicks, bool IsStrongBeat)> MapRhythmToBar(
        IReadOnlyList<int> rhythmShape,
        int barOffsetInMotif,
        int totalMotifBars,
        GroovePreset groovePreset,
        int absoluteBar,
        BarTrack barTrack)
    {
        var result = new List<(long, int, bool)>();

        if (!barTrack.TryGetBar(absoluteBar, out var bar))
            return result;

        int ticksPerBar = (int)bar.TicksPerMeasure;
        int motifTicksPerBar = MusicConstants.TicksPerQuarterNote * 4; // Assume 4/4 for motif definition

        // Calculate which rhythm shape ticks fall in this bar
        int barStartTickInMotif = barOffsetInMotif * motifTicksPerBar;
        int barEndTickInMotif = (barOffsetInMotif + 1) * motifTicksPerBar;

        for (int i = 0; i < rhythmShape.Count; i++)
        {
            int motifTick = rhythmShape[i];

            // Check if this onset falls in the current bar of the motif
            if (motifTick >= barStartTickInMotif && motifTick < barEndTickInMotif)
            {
                // Convert motif-local tick to bar-relative tick
                int tickInBar = motifTick - barStartTickInMotif;

                // Scale to actual bar length if different
                if (ticksPerBar != motifTicksPerBar)
                {
                    tickInBar = (int)((long)tickInBar * ticksPerBar / motifTicksPerBar);
                }

                // Convert to absolute tick
                long absoluteTick = bar.StartTick + tickInBar;

                // Calculate duration to next onset or end of motif
                int durationTicks = CalculateOnsetDuration(
                    rhythmShape, i, barEndTickInMotif, motifTicksPerBar, ticksPerBar);

                // Determine if strong beat (on quarter note boundaries)
                bool isStrongBeat = (tickInBar % MusicConstants.TicksPerQuarterNote) == 0;

                result.Add((absoluteTick, durationTicks, isStrongBeat));
            }
        }

        return result;
    }

    /// <summary>
    /// Calculates duration from current onset to next onset or bar end.
    /// </summary>
    private static int CalculateOnsetDuration(
        IReadOnlyList<int> rhythmShape,
        int currentIndex,
        int barEndTickInMotif,
        int motifTicksPerBar,
        int actualTicksPerBar)
    {
        int currentTick = rhythmShape[currentIndex];

        // Find next onset in same bar or use bar end
        int nextTick = barEndTickInMotif;
        for (int i = currentIndex + 1; i < rhythmShape.Count; i++)
        {
            if (rhythmShape[i] < barEndTickInMotif)
            {
                nextTick = rhythmShape[i];
                break;
            }
        }

        int durationInMotifTicks = nextTick - currentTick;

        // Scale to actual bar length
        if (actualTicksPerBar != motifTicksPerBar)
        {
            durationInMotifTicks = (int)((long)durationInMotifTicks * actualTicksPerBar / motifTicksPerBar);
        }

        // Ensure minimum duration
        return Math.Max(durationInMotifTicks, MusicConstants.TicksPerQuarterNote / 8);
    }

    /// <summary>
    /// Calculates contour position (0..1) based on note position in the motif.
    /// </summary>
    private static double CalculateContourPosition(
        int barOffset,
        int onsetIndex,
        int onsetsInBar,
        int totalBars)
    {
        if (totalBars <= 0 || onsetsInBar <= 0)
            return 0.5;

        // Estimate total onsets (rough approximation)
        double estimatedTotalOnsets = onsetsInBar * totalBars;
        double currentOnsetNumber = barOffset * onsetsInBar + onsetIndex;

        return Math.Clamp(currentOnsetNumber / estimatedTotalOnsets, 0.0, 1.0);
    }

    /// <summary>
    /// Selects pitch based on contour intent, harmony context, and tone policy.
    /// AI: Filters harmony chord/scale tones to motif register range before selection; prevents out-of-register notes.
    /// </summary>
    private static int SelectPitch(
        MotifSpec spec,
        HarmonyPitchContext harmony,
        double contourPosition,
        bool isStrongBeat,
        int previousPitch,
        int barOffset,
        int onsetIndex,
        int seed)
    {
        var register = spec.Register;
        var tonePolicy = spec.TonePolicy;

        // Calculate target pitch from contour
        int contourPitch = CalculateContourPitch(spec.Contour, register, contourPosition);

        // Determine if we should use chord tone (strong beats with high bias)
        var hash = HashCode.Combine(seed, barOffset, onsetIndex);
        double roll = (double)(Math.Abs(hash) % 100) / 100.0;

        bool useChordTone = isStrongBeat
            ? roll < tonePolicy.ChordToneBias
            : roll < tonePolicy.ChordToneBias * 0.5;

        // AI: CRITICAL - Filter chord/scale tones to register range BEFORE selecting
        // This prevents bass motifs from selecting chord tones in higher octaves
        int minRegister = register.CenterMidiNote - register.RangeSemitones;
        int maxRegister = register.CenterMidiNote + register.RangeSemitones;

        // Get candidate pitches, filtered to register range
        var chordTones = harmony.ChordMidiNotes
            .Where(n => n >= minRegister && n <= maxRegister)
            .ToList();
        
        // If no chord tones in range, transpose them by octaves to fit
        if (chordTones.Count == 0 && harmony.ChordMidiNotes.Count > 0)
        {
            foreach (var note in harmony.ChordMidiNotes)
            {
                // Try octave transpositions
                for (int octaveShift = -3; octaveShift <= 3; octaveShift++)
                {
                    int transposed = note + (octaveShift * 12);
                    if (transposed >= minRegister && transposed <= maxRegister)
                    {
                        chordTones.Add(transposed);
                    }
                }
            }
            chordTones = chordTones.Distinct().OrderBy(n => n).ToList();
        }
        
        var scaleTones = GetScaleTones(harmony, register);

        int selectedPitch;

        if (useChordTone && chordTones.Count > 0)
        {
            // Find closest chord tone to contour pitch
            selectedPitch = FindClosestPitch(chordTones, contourPitch, register);
        }
        else if (tonePolicy.AllowPassingTones && scaleTones.Count > 0)
        {
            // Find closest scale tone to contour pitch
            selectedPitch = FindClosestPitch(scaleTones, contourPitch, register);
        }
        else if (chordTones.Count > 0)
        {
            // Fall back to chord tone
            selectedPitch = FindClosestPitch(chordTones, contourPitch, register);
        }
        else
        {
            // Fall back to contour pitch
            selectedPitch = contourPitch;
        }

        // Apply voice-leading smoothing (prefer small intervals)
        selectedPitch = ApplyVoiceLeading(selectedPitch, previousPitch, register);

        // Clamp to valid MIDI range
        return Math.Clamp(selectedPitch, 21, 108);
    }

    /// <summary>
    /// Calculates target pitch from contour intent.
    /// </summary>
    private static int CalculateContourPitch(ContourIntent contour, RegisterIntent register, double position)
    {
        int center = register.CenterMidiNote;
        int range = register.RangeSemitones / 2;

        return contour switch
        {
            ContourIntent.Up => center - range + (int)(position * range * 2),
            ContourIntent.Down => center + range - (int)(position * range * 2),
            ContourIntent.Arch => CalculateArchPitch(center, range, position),
            ContourIntent.Flat => center,
            ContourIntent.ZigZag => CalculateZigZagPitch(center, range, position),
            _ => center
        };
    }

    private static int CalculateArchPitch(int center, int range, double position)
    {
        // Peak at position 0.5
        double offset = 1.0 - Math.Abs(position - 0.5) * 2.0;
        return center + (int)(offset * range);
    }

    private static int CalculateZigZagPitch(int center, int range, double position)
    {
        // Alternate above/below center
        int segment = (int)(position * 6);
        bool above = (segment % 2) == 0;
        double localPosition = (position * 6) - segment;
        int offset = (int)(localPosition * range * 0.6);
        return above ? center + offset : center - offset;
    }

    /// <summary>
    /// Gets scale tones in the register range.
    /// </summary>
    private static List<int> GetScaleTones(HarmonyPitchContext harmony, RegisterIntent register)
    {
        var result = new List<int>();
        int min = register.CenterMidiNote - register.RangeSemitones;
        int max = register.CenterMidiNote + register.RangeSemitones;

        foreach (var pitchClass in harmony.KeyScalePitchClasses)
        {
            for (int octave = 0; octave < 10; octave++)
            {
                int midi = pitchClass + octave * 12;
                if (midi >= min && midi <= max)
                    result.Add(midi);
            }
        }

        return result;
    }

    /// <summary>
    /// Finds the closest pitch from candidates to target, respecting register.
    /// </summary>
    private static int FindClosestPitch(IReadOnlyList<int> candidates, int target, RegisterIntent register)
    {
        if (candidates.Count == 0)
            return target;

        int min = register.CenterMidiNote - register.RangeSemitones;
        int max = register.CenterMidiNote + register.RangeSemitones;

        // Find candidates in range, or closest to range
        int bestPitch = candidates[0];
        int bestDistance = int.MaxValue;

        foreach (var pitch in candidates)
        {
            // Try octave transpositions to fit in range
            for (int octaveShift = -2; octaveShift <= 2; octaveShift++)
            {
                int shiftedPitch = pitch + octaveShift * 12;
                if (shiftedPitch >= min && shiftedPitch <= max)
                {
                    int distance = Math.Abs(shiftedPitch - target);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestPitch = shiftedPitch;
                    }
                }
            }
        }

        return Math.Clamp(bestPitch, 21, 108);
    }

    /// <summary>
    /// Applies voice-leading smoothing to prefer smaller intervals.
    /// </summary>
    private static int ApplyVoiceLeading(int targetPitch, int previousPitch, RegisterIntent register)
    {
        // If jump is large (> perfect 5th), try octave adjustment
        int interval = Math.Abs(targetPitch - previousPitch);
        if (interval > 7)
        {
            // Try octave shift toward previous pitch
            int shiftedUp = targetPitch + 12;
            int shiftedDown = targetPitch - 12;

            int min = register.CenterMidiNote - register.RangeSemitones;
            int max = register.CenterMidiNote + register.RangeSemitones;

            if (shiftedDown >= min && Math.Abs(shiftedDown - previousPitch) < interval)
                return shiftedDown;
            if (shiftedUp <= max && Math.Abs(shiftedUp - previousPitch) < interval)
                return shiftedUp;
        }

        return targetPitch;
    }

    /// <summary>
    /// Applies variation operators based on placement settings.
    /// </summary>
    private static int ApplyVariation(
        int pitch,
        MotifSpec spec,
        MotifPlacement placement,
        HarmonyPitchContext harmony,
        double contourPosition,
        int barOffset,
        int onsetIndex,
        int seed)
    {
        if (placement.VariationIntensity <= 0.0)
            return pitch;

        var hash = HashCode.Combine(seed, "variation", barOffset, onsetIndex);
        double roll = (double)(Math.Abs(hash) % 100) / 100.0;

        // Skip variation if roll exceeds intensity
        if (roll > placement.VariationIntensity)
            return pitch;

        int result = pitch;

        // Check transform tags
        if (placement.TransformTags.Contains("OctaveUp"))
        {
            result = ApplyOctaveDisplacement(result, 12, spec.Register);
        }
        else if (placement.TransformTags.Contains("OctaveDown"))
        {
            result = ApplyOctaveDisplacement(result, -12, spec.Register);
        }

        // Small pitch adjustments (±2 semitones) at higher variation intensity
        if (placement.VariationIntensity > 0.3)
        {
            var pitchHash = HashCode.Combine(seed, "pitchAdj", barOffset, onsetIndex);
            int adjustment = (Math.Abs(pitchHash) % 5) - 2; // -2 to +2
            result = Math.Clamp(result + adjustment, 21, 108);

            // Ensure still in scale if possible
            if (harmony.KeyScalePitchClasses.Count > 0)
            {
                int pitchClass = result % 12;
                if (!harmony.KeyScalePitchClasses.Contains(pitchClass))
                {
                    // Snap to nearest scale tone
                    int nearestScale = harmony.KeyScalePitchClasses
                        .OrderBy(pc => Math.Min(Math.Abs(pc - pitchClass), 12 - Math.Abs(pc - pitchClass)))
                        .First();
                    result = (result / 12) * 12 + nearestScale;
                }
            }
        }

        return Math.Clamp(result, 21, 108);
    }

    /// <summary>
    /// Applies octave displacement within register bounds.
    /// </summary>
    private static int ApplyOctaveDisplacement(int pitch, int semitones, RegisterIntent register)
    {
        int newPitch = pitch + semitones;
        int min = register.CenterMidiNote - register.RangeSemitones;
        int max = register.CenterMidiNote + register.RangeSemitones;

        if (newPitch >= min && newPitch <= max)
            return newPitch;

        return pitch; // Stay in range
    }

    /// <summary>
    /// Calculates velocity based on tension and beat strength.
    /// </summary>
    private static int CalculateVelocity(BarIntentContext barIntent, bool isStrongBeat, double contourPosition, int seed)
    {
        // Base velocity
        int baseVelocity = 85;

        // Strong beat accent
        if (isStrongBeat)
            baseVelocity += 10;

        // Tension-based bias
        baseVelocity += (int)(barIntent.MicroTension * 10);

        // Phrase peak/end accents
        if (barIntent.IsPhraseEnd && contourPosition > 0.7)
            baseVelocity += 8;

        // Deterministic micro-variation
        var hash = HashCode.Combine(seed, "velocity", contourPosition);
        int microVar = (Math.Abs(hash) % 11) - 5; // -5 to +5
        baseVelocity += microVar;

        return Math.Clamp(baseVelocity, 40, 127);
    }

    /// <summary>
    /// Applies duration variation based on placement intensity.
    /// </summary>
    private static int ApplyDurationVariation(int durationTicks, MotifPlacement placement)
    {
        // High variation shortens notes slightly
        double multiplier = 1.0 - (placement.VariationIntensity * 0.2);
        return Math.Max((int)(durationTicks * multiplier), MusicConstants.TicksPerQuarterNote / 8);
    }

    /// <summary>
    /// Prevents overlaps with previous notes by shortening duration.
    /// </summary>
    private static int PreventOverlaps(List<PartTrackEvent> existingEvents, long absoluteTick, int durationTicks)
    {
        if (existingEvents.Count == 0)
            return durationTicks;

        var lastEvent = existingEvents[^1];
        long lastEventEnd = lastEvent.AbsoluteTimeTicks + lastEvent.NoteDurationTicks;

        // If new note starts before last note ends, shorten last note
        if (absoluteTick < lastEventEnd && existingEvents.Count > 0)
        {
            int gap = 10; // Small gap to prevent stuck notes
            int newDuration = (int)(absoluteTick - lastEvent.AbsoluteTimeTicks) - gap;
            if (newDuration > 0)
            {
                // Modify the last event's duration
                var modifiedEvent = new PartTrackEvent(
                    lastEvent.NoteNumber,
                    (int)lastEvent.AbsoluteTimeTicks,
                    newDuration,
                    lastEvent.NoteOnVelocity);
                existingEvents[^1] = modifiedEvent;
            }
        }

        return durationTicks;
    }

    /// <summary>
    /// Creates an empty track for invalid placements.
    /// </summary>
    private static PartTrack CreateEmptyTrack(MotifSpec spec, MotifPlacement placement, int midiProgramNumber)
    {
        return new PartTrack(new List<PartTrackEvent>())
        {
            MidiProgramNumber = midiProgramNumber,
            MidiProgramName = spec.IntendedRole,
            Meta = new PartTrackMeta
            {
                TrackId = PartTrack.PartTrackId.NewId(),
                Name = $"{spec.Name} (empty)",
                IntendedRole = spec.IntendedRole,
                Domain = PartTrackDomain.SongAbsolute,
                Kind = PartTrackKind.RoleTrack,
                MaterialKind = spec.Kind
            }
        };
    }
}
