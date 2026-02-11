# Bass Phrase Operators (<=25)
# Assumptions from your codebase:
# - Input is List<GrooveOnset> (already contains anchor notes w/ MidiNote+DurationTicks).
# - Operators can: add/remove/alter onsets, set MidiNote, DurationTicks, Velocity, TimingOffsetTicks.
# - Harmony is available via SongContext.HarmonyTrack.GetActiveHarmonyEvent(bar, beat).
# - Groove anchors exist per role (bass, drums, comp...), so operators can reference drum anchor beats too.
#
# Research grounding (high-level):
# - Groove perception relates strongly to syncopation and event density at moderate levels. (groove/syncopation literature) :contentReference[oaicite:0]{index=0}
# - Expert rhythm sections intentionally use “pushed/on/laid-back” timing styles; microtiming magnitude can matter, but more deviation is not always “more groove.” :contentReference[oaicite:1]{index=1}
# - Walking/approach-note practice emphasizes chord tones + stepwise approach into targets. :contentReference[oaicite:2]{index=2}

operator_families:
  - FoundationVariation: "Transform anchors into common bass foundations (pedal, pulse, root/5, etc.)"
  - HarmonicTargeting: "Choose chord-tone targets + approach notes (voice-leading)."
  - RhythmicPlacement: "Syncopation, anticipations, pickups, pushes, rests."
  - DensityAndSubdivision: "Increase/decrease rhythmic density using patterns while staying playable."
  - RegisterAndContour: "Octaves, leaps, contour shaping, range constraints."
  - Humanization: "Timing/velocity offsets and controlled randomness (NOT articulation)."
  - CleanupAndConstraints: "De-conflict overlaps, enforce bar boundaries, prevent nonsense."

operators:

  # =========================
  # FoundationVariation (4)
  # =========================

  - id: BassPedalRootBar
    family: FoundationVariation
    what_it_does: >
      Converts each bar to a pedal-style sustained root (or current anchor’s chord root),
      minimizing motion while keeping harmony correct.
    when_to_apply: "Any style; especially useful as contrast against dense operators."
    algorithm:
      - "For each bar: pick the earliest bass onset in the bar (or create at beat 1 if none)."
      - "Set that onset MidiNote to chord root at BaseOctave (use ChordVoicingHelper root)."
      - "Set DurationTicks to bar end tick - onset tick (cap to int.MaxValue)."
      - "Remove any other bass onsets in that bar OR set their Velocity to 0 and MidiNote null (choose one policy)."
    constraints:
      - "If harmony event missing at beat 1, fall back to existing onset MidiNote."
      - "Do not cross bar boundary."
    parameters:
      base_octave: 2
      removal_policy: "remove"   # remove | nullify

  - id: BassRootFifthOstinato
    family: FoundationVariation
    what_it_does: >
      Rewrites anchors into a classic root–5th (or root–octave) ostinato aligned to existing onsets.
    algorithm:
      - "For each bar: collect bass onsets sorted by beat."
      - "For onset i: get harmony event at that onset; compute chord root."
      - "Alternate pitch choice: even i => root; odd i => perfect fifth above root (or octave if fifth out of range)."
      - "If chord quality suggests diminished/altered, still use perfect fifth as a stable power-tone unless style says otherwise."
    constraints:
      - "Enforce range: keep MidiNote within [minNote,maxNote] by octave shifting."
    parameters:
      base_octave: 2
      min_note: 28   # E1
      max_note: 52   # E3
      alt_second_tone: "fifth" # fifth | octave

  - id: BassChordTonePulse
    family: FoundationVariation
    what_it_does: >
      Replaces anchor pitches with a rotating chord-tone set (R, 3, 5, 7) to imply harmony more than root-only.
    algorithm:
      - "For each onset: fetch harmony event; build chord tones for the chord at Bass register."
      - "Choose tone index as (onsetIndexWithinBar mod availableTonesCount)."
      - "Set MidiNote accordingly; keep DurationTicks unchanged."
    constraints:
      - "If chord tones generation yields <2 notes, keep existing note."
    parameters:
      base_octave: 2
      include_7th_when_available: true

  - id: BassPedalWithTurnaroundLastBeat
    family: FoundationVariation
    what_it_does: >
      Pedal most of the bar, but creates a small 1-beat turnaround into the next bar’s beat-1 chord.
      This is a “safe fill” pattern.
    algorithm:
      - "In each bar: set a sustained root from beat 1 to beat 3 (or to beat 3.5 if you support 8ths)."
      - "At last beat window (default beat 4): add 2-4 notes that approach next bar’s beat-1 chord root."
      - "Approach rules: stepwise diatonic +/- chromatic neighbor optional; final note must land exactly on next bar beat 1 root."
      - "If next bar harmony is same chord, use a simple leading-tone-to-root or 5->root."
    constraints:
      - "Do not add turnaround if bar is last bar in generation."
      - "If groove preset has no beat-4 anchor, you may still insert in beat 4 as long as it doesn’t collide with existing onset (resolve collisions)."
    parameters:
      turnaround_notes: 3
      turnaround_start_beat: 4.0
      subdivision: "8th"  # 8th | 16th
      allow_chromatic_neighbor: true

  # =========================
  # HarmonicTargeting (5)
  # =========================

  - id: BassTargetNextChordRoot
    family: HarmonicTargeting
    what_it_does: >
      On chord changes, forces the first onset AFTER the change to be the new chord root (strong voice-leading anchor).
    algorithm:
      - "Detect chord-change points in a bar by sampling harmony at each onset beat (and optionally beat 1)."
      - "For each change: find the earliest onset at/after the change beat; set MidiNote to new chord root."
      - "If no onset exists after change, insert one at change beat using groove-safe beat snapping (see params)."
    constraints:
      - "Inserted onset DurationTicks ends at next onset tick (or bar end)."
    parameters:
      snap_to_existing_subdivision: "8th"
      insert_if_missing: true

  - id: BassApproachNoteIntoTarget
    family: HarmonicTargeting
    what_it_does: >
      Adds a 1-step (diatonic or chromatic) approach note immediately before a strong target (usually beat 1 or a chord-change hit).
      Common “expert” move in walking and groove styles. :contentReference[oaicite:3]{index=3}
    algorithm:
      - "Pick target onsets: by default, any onset on beat 1 or any onset that is the first after a chord change."
      - "For each target: choose approach pitch = target-1 semitone OR target+1 semitone (bias downward)."
      - "Insert new onset at (targetBeat - approachOffsetBeats). DurationTicks = ticksPerSubdivision."
      - "If approach note would be outside range, invert direction."
    constraints:
      - "Do not insert if targetBeat - approachOffsetBeats < barStart."
      - "Do not insert if collision with existing onset at same beat; if collision, shorten existing note and reuse its beat."
    parameters:
      approach_offset_beats: 0.5  # 8th note lead-in (at 4/4)
      chromatic_probability: 0.6
      bias_downward: 0.7
      min_note: 28
      max_note: 52

  - id: BassEnclosureIntoTarget
    family: HarmonicTargeting
    what_it_does: >
      Adds a 2-note “enclosure” into a target: above then below (or vice versa) before landing.
      This is a stronger fill-like move but still harmony-aware.
    algorithm:
      - "Select one target onset per bar (prefer beat 1 or last onset)."
      - "Compute target pitch (keep existing MidiNote)."
      - "Insert two notes in the beat before target: (target+1) then (target-1) then target (or swap order)."
      - "Use semitone neighbors by default; optionally diatonic neighbors if key context available."
    constraints:
      - "Requires at least 2*subdivision window before target."
      - "Skip if target is already very dense (>=3 onsets in prior beat)."
    parameters:
      subdivision: "16th"
      order: "above_below" # above_below | below_above
      use_diatonic_neighbors: false

  - id: BassGuideToneEmphasis
    family: HarmonicTargeting
    what_it_does: >
      On chord changes, prefers 3rd or 7th (guide tones) on weak beats while keeping roots on strong beats,
      making the line sound “aware” of harmony without going full jazz. :contentReference[oaicite:4]{index=4}
    algorithm:
      - "For each onset: classify beat strength (strong: 1,3; weak: 2,4; 'and' beats weaker)."
      - "If strong: set to chord root."
      - "If weak: choose chord 3rd or 7th if available; otherwise 5th."
      - "Octave-shift chosen tone into range with minimal movement from previous note."
    constraints:
      - "If chord is power-chord-like or style is heavy rock, optionally disable 3rd/7th via param."
    parameters:
      enable_color_tones: true
      prefer_7th_probability: 0.4
      min_note: 28
      max_note: 55

  - id: BassStepwiseVoiceLeading
    family: HarmonicTargeting
    what_it_does: >
      Minimizes leaps between consecutive onsets by octave-shifting chord-tone choices and preferring stepwise motion.
      This is a “make it sound played” constraint operator.
    algorithm:
      - "Walk through onsets in time order."
      - "For each onset: generate candidate chord-tone pitches across multiple octaves (e.g., baseOctave-1..baseOctave+1)."
      - "Score each candidate by absolute semitone distance from previous onset’s MidiNote (prefer <=5 semitones)."
      - "Pick lowest distance; tie-break toward chord root on strong beats."
      - "Set MidiNote to chosen candidate."
    constraints:
      - "If previous note missing, keep current."
    parameters:
      base_octave: 2
      octave_span: 1
      max_preferred_leap_semitones: 5

  # =========================
  # RhythmicPlacement (5)
  # =========================

  - id: BassAnticipateDownbeat
    family: RhythmicPlacement
    what_it_does: >
      Creates anticipations: moves a strong-beat hit slightly earlier (e.g., the “and of 4” into beat 1),
      a key groove device across styles. :contentReference[oaicite:5]{index=5}
    algorithm:
      - "Pick targets: onsets on beat 1 of a bar (excluding bar 1 optionally)."
      - "For each target: move its Beat earlier by anticipationOffsetBeats (default 0.5)."
      - "Shorten previous note so it ends at new anticipated beat (if overlapping)."
      - "Keep pitch same OR set pitch to approach note (optional via param)."
    constraints:
      - "Do not move earlier than bar start."
      - "If an onset already exists at the anticipated beat, merge: keep earlier onset and delete/mute target."
    parameters:
      anticipation_offset_beats: 0.5
      apply_pitch_approach: false

  - id: BassSyncopationSwap
    family: RhythmicPlacement
    what_it_does: >
      Increases syncopation by swapping one on-beat onset with an off-beat onset (or adding off-beat and removing on-beat),
      targeting “moderate syncopation” rather than chaos. :contentReference[oaicite:6]{index=6}
    algorithm:
      - "In each bar: identify candidate strong onsets (beats 1/3) and candidate off-beats (1.5,2.5,3.5,4.5 if exist)."
      - "Choose one strong onset to remove or shorten."
      - "Insert an off-beat onset within same half-bar; copy pitch from removed onset or set to chord tone."
      - "Set DurationTicks to end at next onset."
    constraints:
      - "Keep at least one onset on beat 1 per bar."
    parameters:
      preserve_beat1: true
      offbeat_pool: [1.5, 2.5, 3.5, 4.5]
      remove_probability: 0.6

  - id: BassKickLockQuantized
    family: RhythmicPlacement
    what_it_does: >
      Locks bass hits to the kick drum anchor onsets (rhythmic unison), but only where bass already has density room.
      This is the classic “bass + kick” tightness move.
    algorithm:
      - "For each bar: get drum kick anchor beats from groovePreset.AnchorLayer.GetOnsets(GrooveRoles.Kick or DrumsKick if you have it)."
      - "For each kick beat: if no bass onset within toleranceBeats, insert a bass onset at that beat."
      - "Pitch: chord root or last bass pitch (choose via param)."
      - "Duration: until next bass onset or small default (e.g., 8th)."
    constraints:
      - "Do not exceed maxOnsetsPerBar."
      - "If you don’t have explicit kick role, skip."
    parameters:
      tolerance_beats: 0.1
      pitch_policy: "root" # root | hold_last
      max_onsets_per_bar: 8
      default_duration_subdivision: "8th"

  - id: BassRestStrategicSpace
    family: RhythmicPlacement
    what_it_does: >
      Removes one onset (or turns it into a rest) to create space—often what separates “busy” from “pro.”
      Groove often improves with intentional gaps. :contentReference[oaicite:7]{index=7}
    algorithm:
      - "In each bar: compute density = onsetCount."
      - "If density <= minDensity, do nothing."
      - "Choose an onset on a weak position (e.g., beat 2 or 4, or off-beat) and remove it."
      - "If removal creates a long gap, lengthen previous onset to cover part of it (optional)."
    constraints:
      - "Never remove beat-1 onset."
    parameters:
      min_density: 3
      extend_previous: true

  - id: BassPickupIntoNextBar
    family: RhythmicPlacement
    what_it_does: >
      Adds a short pickup at the end of a bar (1–2 notes) leading into next bar’s beat 1.
      This is the “setup hit” analog you described for drums, but harmony-aware.
    algorithm:
      - "If bar is last bar, skip."
      - "Target = next bar beat 1 root."
      - "Insert one note at beat 4.5 (8th pickup) OR beat 4.75 (16th pickup) based on subdivision."
      - "Pitch = approach into target (target-1 semitone default) OR 5th->root."
      - "Ensure target onset at next bar beat 1 remains (or insert if missing)."
    constraints:
      - "Do not insert if beat 4.5 already occupied unless merge policy allows."
    parameters:
      pickup_beat: 4.5
      subdivision: "8th"
      approach_mode: "chromatic_below" # chromatic_below | fifth_to_root | diatonic

  # =========================
  # DensityAndSubdivision (4)
  # =========================

  - id: BassSplitLongNote
    family: DensityAndSubdivision
    what_it_does: >
      Splits a long sustained note into repeated shorter notes (same pitch), creating pulse without harmonic complexity.
    algorithm:
      - "Choose one onset with DurationTicks >= splitThresholdTicks."
      - "Compute numberOfSlices = floor(DurationTicks / sliceTicks)."
      - "Replace original onset with a sequence of onsets at slice boundaries, each with DurationTicks=sliceTicks (last gets remainder)."
      - "Pitch copied; velocity optionally tapered."
    constraints:
      - "Cap slices per note to avoid machine-gun effect."
    parameters:
      slice: "8th"
      max_slices: 4
      velocity_taper: true

  - id: BassAddPassingEighths
    family: DensityAndSubdivision
    what_it_does: >
      Between two existing onsets separated by >= 1 beat, inserts passing eighth notes that move stepwise toward the next pitch.
      This is “walking-lite.”
    algorithm:
      - "Scan consecutive onset pairs (A,B). If gapBeats >= 1.0:"
      - "Determine intermediate beats at 0.5 increments."
      - "For each intermediate beat: choose pitch by moving 1–2 semitones toward B each step (clamp so you land on B)."
      - "Insert onsets with short durations (8th)."
    constraints:
      - "If B pitch equals A pitch, insert repeated pulses instead of pitch walk."
      - "Do not exceed maxOnsetsPerBar."
    parameters:
      max_onsets_per_bar: 10
      step_semitones: 1
      use_chord_scale_filter: true  # if you can derive scale from key

  - id: BassReduceToQuarterNotes
    family: DensityAndSubdivision
    what_it_does: >
      Simplifies any bar into quarter-note grid (beats 1,2,3,4) using chord-root or simple chord tones.
    algorithm:
      - "Remove all bass onsets in bar."
      - "Insert onsets at beats [1,2,3,4]."
      - "Pitch policy: beat 1=root; beat 3=5th (or root); beats 2/4=root or 3rd/7th if enabled."
      - "DurationTicks = quarter note ticks."
    constraints:
      - "If time signature not 4/4, adapt beats list from BarTrack meter (if available) else skip."
    parameters:
      include_color_tones_on_2_4: false

  - id: BassBurstSixteenthsOnOneBeat
    family: DensityAndSubdivision
    what_it_does: >
      Adds a brief 16th-note burst on a chosen beat (usually beat 4) without traveling far in pitch.
      It’s a controlled “ramp” effect. :contentReference[oaicite:8]{index=8}
    algorithm:
      - "Choose a target beat window (default beat 4)."
      - "If existing density in that beat is already high, skip."
      - "Insert 3-4 onsets at 16th offsets within that beat."
      - "Pitch: alternate root and octave OR root and 5th, with minimal leaps."
    constraints:
      - "Do not overwrite beat-1 onset."
    parameters:
      target_beat: 4.0
      notes_in_burst: 4
      pitch_pattern: "root_octave" # root_octave | root_fifth

  # =========================
  # RegisterAndContour (3)
  # =========================

  - id: BassOctavePopAccents
    family: RegisterAndContour
    what_it_does: >
      On selected onsets, jumps to octave (or drops to lower octave) to create shape, then returns.
      This is the “downbeat reinforce” energy move without articulation modeling.
    algorithm:
      - "Select candidate onsets on strong beats (1 and/or 3)."
      - "For selected: set MidiNote += 12 (octave up)."
      - "Ensure the next onset returns to original register by octave-shifting back if needed."
    constraints:
      - "Do not exceed range."
    parameters:
      probability_per_strong_beat: 0.35
      min_note: 28
      max_note: 55

  - id: BassRangeClampAndReoctave
    family: RegisterAndContour
    what_it_does: >
      Enforces playable range by octave-shifting notes into [min,max] while preserving pitch class.
      This is a safety operator that prevents absurd register drift.
    algorithm:
      - "For each onset with MidiNote: while MidiNote < min => +12; while > max => -12."
      - "If still out of range (extreme), clamp."
    parameters:
      min_note: 28
      max_note: 55
      hard_clamp: true

  - id: BassContourSmoother
    family: RegisterAndContour
    what_it_does: >
      Reduces large leaps by re-octaving later notes to keep contour singable and “hand-friendly.”
    algorithm:
      - "Iterate onsets; for each pair prev->cur:"
      - "If abs(cur-prev) > leapThreshold:"
      - "Try cur-12 and cur+12; pick the one with smallest distance to prev that stays in range."
      - "Set cur to adjusted value."
    parameters:
      leap_threshold_semitones: 9
      min_note: 28
      max_note: 55

  # =========================
  # Humanization (3)
  # =========================

  - id: BassTimingFeelPushPull
    family: Humanization
    what_it_does: >
      Applies small TimingOffsetTicks to suggest “pushed” or “laid-back” feel on selected off-beats,
      bounded so it doesn’t flam against drums too much.
      Experts deliberately use timing styles; more deviation is not always better. :contentReference[oaicite:9]{index=9}
    algorithm:
      - "For each onset: classify as on-beat vs off-beat."
      - "If off-beat: add offsetTicks sampled from [-maxAdvance, +maxDelay] with bias based on mode."
      - "If on-beat strong (beat 1): keep near zero (or smaller range)."
    constraints:
      - "Clamp so onset doesn’t cross previous/next onset tick (keep ordering)."
    parameters:
      mode: "laid_back" # laid_back | pushed | neutral
      max_delay_ticks: 18
      max_advance_ticks: 12
      strongbeat_scale: 0.35

  - id: BassVelocityContour
    family: Humanization
    what_it_does: >
      Shapes velocity across the bar (accent strong beats, soften off-beats), making patterns read as intentional.
    algorithm:
      - "For each bar: set baseline velocity = settings.DefaultVelocity."
      - "Accents: beat 1 (+a), beat 3 (+b), off-beats (-c)."
      - "Clamp to [1..127]."
    parameters:
      accent_beat1: 10
      accent_beat3: 6
      deaccent_offbeats: 8
      clamp_min: 40
      clamp_max: 120

  - id: BassRandomMicroVariationSafe
    family: Humanization
    what_it_does: >
      Tiny random variation in duration (and optionally velocity) to avoid copy-paste sameness,
      while preserving bar alignment.
    algorithm:
      - "For selected onsets: DurationTicks += random(-durJitter,+durJitter) but clamp to >=minDur."
      - "Optionally Velocity += random(-velJitter,+velJitter)."
      - "Ensure duration does not overlap next onset tick; if overlap, shorten."
    parameters:
      duration_jitter_ticks: 12
      velocity_jitter: 4
      min_duration_ticks: 60
      probability: 0.5

  # =========================
  # CleanupAndConstraints (3)
  # =========================

  - id: BassResolveOverlapsAndOrder
    family: CleanupAndConstraints
    what_it_does: >
      Ensures monophonic bass: no overlapping notes, strictly increasing start times.
      This should be safe to run after any density operator.
    algorithm:
      - "Sort onsets by (bar, beat)."
      - "Convert each to (startTick,endTick) using BarTrack."
      - "If startTick < prevEndTick: set startTick = prevEndTick OR shorten prev duration so prevEndTick=startTick (choose policy)."
      - "If endTick <= startTick: set minimal duration."
      - "Write back by adjusting TimingOffsetTicks and/or DurationTicks."
    parameters:
      policy: "shorten_previous" # shorten_previous | push_current
      min_duration_ticks: 60

  - id: BassSnapBeatsToSubdivision
    family: CleanupAndConstraints
    what_it_does: >
      Snaps Beat values to nearest supported subdivision (e.g., 1/2 or 1/4 beat),
      preventing impossible micro-placements from accumulating across operators.
    algorithm:
      - "For each onset: Beat = round(Beat / grid) * grid."
      - "If Beat becomes 0, set to 1 (bar start)."
      - "If Beat exceeds bar length, clamp to last valid grid point."
    parameters:
      grid_beats: 0.25  # 16th-note grid in 4/4

  - id: BassPreventOverDensity
    family: CleanupAndConstraints
    what_it_does: >
      Caps max onsets per bar by removing lowest-importance notes (weak beats, duplicates, very short notes).
      Keeps generation from spiraling when multiple density operators stack.
    algorithm:
      - "Group onsets per bar."
      - "If count <= max, keep."
      - "Score each onset for removal: weak beat +1, duplicate pitch w/ neighbor +1, very short duration +1, not beat-1 +1."
      - "Remove highest removal-score until count==max (never remove beat-1 onset)."
    parameters:
      max_onsets_per_bar: 10
      protect_beat1: true

# Suggested implementation notes:
# - Keep operators purely local (bar or bar+next) since section awareness is deferred.
# - Prefer 'insert' operations to set DurationTicks using tick distance to next onset (or bar end).
# - After applying N random operators, run cleanup family operators (ResolveOverlaps, PreventOverDensity, SnapBeats)
#   even if they were not randomly selected, otherwise some combinations will break monophony.

