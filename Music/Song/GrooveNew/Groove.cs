namespace Music.GrooveModel
{
    // =========================
    // 1) Subdivision grid + swing/shuffle feel
    // =========================

    /// <summary>
    /// Enumerates rhythmic feel templates that affect how subdivisions are interpreted (e.g., swing/shuffle).
    /// </summary>
    public enum GrooveFeel
    {
        Straight,        // No swing; subdivisions land exactly on-grid.
        Swing,           // Swing 8ths feel (unequal 8ths).
        Shuffle,         // Shuffle feel (often triplet-based 8ths).
        TripletFeel      // General triplet subdivision bias.
    }

    /// <summary>
    /// Enumerates the allowed rhythmic subdivision families a generator may use for onsets.
    /// </summary>
    [Flags]
    public enum AllowedSubdivision
    {
        None = 0,
        Quarter = 1 << 0, // 1/4
        Eighth = 1 << 1, // 1/8
        Sixteenth = 1 << 2, // 1/16
        EighthTriplet = 1 << 3, // 1/8 triplet grid
        SixteenthTriplet = 1 << 4 // 1/16 triplet grid (rare, but useful for fills)
    }

    /// <summary>
    /// Defines the rhythmic grid constraints and feel for a groove.
    /// </summary>
    public sealed class GrooveSubdivisionPolicy
    {
        public AllowedSubdivision AllowedSubdivisions { get; set; } // Which grids are legal for generation.
        public GrooveFeel Feel { get; set; }                        // Straight/Swing/Shuffle/TripletFeel.
        public double SwingAmount01 { get; set; }                   // 0..1 intensity; meaningful when Feel != Straight.
    }

    // =========================
    // 2) Backbeat / anchor protection (must-hit events)
    // 3) Optional event candidates (variation catalog)
    // 4) Role-specific “rhythm vocabulary” constraints
    // 5) Accents and dynamics pattern (velocity shape)
    // 6) Timing feel / pocket (micro-timing template)
    // 7) Phrase-position hooks (fill windows, cadence bars)
    // 8) Instrumentation presence defaults (role orchestration)
    // 9) Groove identity tags + compatibility hints
    // =========================

    /// <summary>
    /// Stable role identifiers used by groove policies, candidates, and profiles.
    /// Keep as string for extensibility; later you can wrap as enum if desired.
    /// </summary>
    public static class GrooveRoles
    {
        public const string Kick = "Kick";       // Drum lane: kick
        public const string Snare = "Snare";     // Drum lane: snare
        public const string Hat = "Hat";         // Drum lane: hats/ride
        public const string DrumKit = "DrumKit"; // Optional combined lane
        public const string Bass = "Bass";       // Bass role
        public const string Comp = "Comp";       // Comping role (guitar/keys comp fragments)
        public const string Pads = "Pads";       // Pads / sustained harmony
        public const string Keys = "Keys";       // Keys role (if separate from Pads)
        public const string Lead = "Lead";       // Future lead/vocal
    }

    // =========================
    // GroovePresetIdentity (includes #9 + basic meter linkage)
    // =========================

    /// <summary>
    /// Identity + semantic tags that describe what the groove "is" and what it is compatible with.
    /// </summary>
    public sealed class GroovePresetIdentity
    {
        public string Name { get; set; } = "";                   // Unique name, e.g. "PopRockBasic".
        public int BeatsPerBar { get; set; }                      // Meter numerator expectation; must align with song timing.
        public string StyleFamily { get; set; } = "";             // e.g. "PopRock", "EDM", "Jazz".
        public List<string> Tags { get; set; } = new();           // Groove identity tags (#9): "Straight8", "Backbeat", etc.
        public List<string> CompatibilityTags { get; set; } = new(); // Hints for which variation packs/algorithms can apply.
    }

    // =========================
    // 5) Accents and dynamics pattern (velocity shape)
    // =========================

    /// <summary>
    /// Classifies onsets into strength buckets for accent logic.
    /// </summary>
    public enum OnsetStrength
    {
        Downbeat,   // Beat 1 (or bar start emphasis points)
        Backbeat,   // Beats 2/4 in 4/4 (or style-defined backbeat)
        Strong,     // Other strong beats (style-dependent)
        Offbeat,    // Eighth offbeats, syncopations
        Pickup,     // Anticipations leading into a strong beat / phrase start
        Ghost       // Very low intensity decorative hit (mostly drums)
    }

    /// <summary>
    /// Velocity rule for one strength bucket (bounded by min/max and typical target).
    /// </summary>
    public sealed class VelocityRule
    {
        public int Min { get; set; }          // Minimum velocity (1..127).
        public int Max { get; set; }          // Maximum velocity (1..127).
        public int Typical { get; set; }      // Typical velocity target used before jitter/bias.
        public int AccentBias { get; set; }   // Additive bias applied in this bucket (can be negative).
    }

    /// <summary>
    /// Velocity shaping per role based on onset strength buckets.
    /// </summary>
    public sealed class GrooveAccentPolicy
    {
        public Dictionary<string, Dictionary<OnsetStrength, VelocityRule>> RoleStrengthVelocity { get; set; } = new(); // Role -> strength -> rule.
        public Dictionary<string, VelocityRule> RoleGhostVelocity { get; set; } = new(); // Optional per-role ghost velocity range.
    }

    // =========================
    // 6) Timing feel / pocket (micro-timing template)
    // =========================

    /// <summary>
    /// High-level pocket feel per role; used to bias micro-timing deterministically.
    /// </summary>
    public enum TimingFeel
    {
        Ahead,      // Slightly ahead of grid.
        OnTop,      // Very close to grid.
        Behind,     // Slightly behind grid.
        LaidBack    // More behind / relaxed.
    }

    /// <summary>
    /// Micro-timing template describing per-role timing bias.
    /// </summary>
    public sealed class GrooveTimingPolicy
    {
        public Dictionary<string, TimingFeel> RoleTimingFeel { get; set; } = new(); // Role -> feel.
        public Dictionary<string, int> RoleTimingBiasTicks { get; set; } = new();   // Role -> nominal tick bias (can be 0).
        public int MaxAbsTimingBiasTicks { get; set; }                              // Safety clamp for any applied micro-timing.
    }

    // =========================
    // 4) Role-specific “rhythm vocabulary” constraints
    // =========================

    /// <summary>
    /// Constraint knobs that limit how rhythm is generated for a role.
    /// </summary>
    public sealed class RoleRhythmVocabulary
    {
        public int MaxHitsPerBar { get; set; }                // Hard cap on number of onsets emitted in a bar for the role.
        public int MaxHitsPerBeat { get; set; }               // Hard cap to prevent "machine-gun" density.
        public bool AllowSyncopation { get; set; }            // Whether offbeat emphasis is allowed.
        public bool AllowAnticipation { get; set; }           // Whether hits may occur before strong beats (pickups).
        public bool SnapStrongBeatsToChordTones { get; set; } // Harmonic snap policy hint (mainly for pitched roles).
    }

    /// <summary>
    /// Rhythm constraints and caps per role.
    /// </summary>
    public sealed class GrooveRoleConstraintPolicy
    {
        public Dictionary<string, RoleRhythmVocabulary> RoleVocabulary { get; set; } = new(); // Role -> vocab constraints.
        public Dictionary<string, int> RoleMaxDensityPerBar { get; set; } = new();            // Role -> max number of note events per bar.
        public Dictionary<string, int> RoleMaxSustainSlots { get; set; } = new();             // Role -> cap on sustained holds (pads/keys).
    }

    // =========================
    // 7) Phrase-position hooks (fill windows, cadence bars)
    // =========================

    /// <summary>
    /// Rules that indicate where fills/pulls are allowed and how they are constrained.
    /// </summary>
    public sealed class GroovePhraseHookPolicy
    {
        public bool AllowFillsAtPhraseEnd { get; set; }            // Whether any fill candidates may be used at phrase ends.
        public int PhraseEndBarsWindow { get; set; }               // Number of bars from phrase end considered "fill window".
        public bool AllowFillsAtSectionEnd { get; set; }           // Whether section-end fills are allowed.
        public int SectionEndBarsWindow { get; set; }              // Bars from section end for end-of-section behavior.
        public bool ProtectDownbeatOnPhraseEnd { get; set; }        // Prevent removing the first beat anchor in phrase-end bars.
        public bool ProtectBackbeatOnPhraseEnd { get; set; }        // Prevent removing backbeat in phrase-end bars (style dependent).
        public List<string> EnabledFillTags { get; set; } = new();  // Which candidate group tags count as fills, e.g., "Fill", "Pickup".
    }

    // =========================
    // 8) Instrumentation presence defaults (role orchestration)
    // =========================

    /// <summary>
    /// Default role presence for a section type (lightweight orchestration hint).
    /// Keep section type as string to avoid coupling; e.g. "Verse", "Chorus".
    /// </summary>
    public sealed class SectionRolePresenceDefaults
    {
        public string SectionType { get; set; } = "";               // e.g. "Verse".
        public Dictionary<string, bool> RolePresent { get; set; } = new(); // Role -> present?
        public Dictionary<string, int> RoleRegisterLiftSemitones { get; set; } = new(); // Role -> default lift in this section type.
        public Dictionary<string, double> RoleDensityMultiplier { get; set; } = new();  // Role -> default density multiplier.
    }

    /// <summary>
    /// Orchestration policy keyed by section type, used to turn roles on/off and provide baseline density/register hints.
    /// </summary>
    public sealed class GrooveOrchestrationPolicy
    {
        public List<SectionRolePresenceDefaults> DefaultsBySectionType { get; set; } = new(); // Per section type orchestration defaults.
    }

    // =========================
    // GrooveProtectionPolicy (global + override merging) with hierarchical layers
    // =========================

    /// <summary>
    /// Protection sets for a single role.
    /// Beats/onsets are expressed in your existing 1-based quarter-note beat units (e.g., 1.5, 2.0, 4.5).
    /// </summary>
    public sealed class RoleProtectionSet
    {
        public List<decimal> MustHitOnsets { get; set; } = new();        // Must be present; cannot be removed by variation.
        public List<decimal> ProtectedOnsets { get; set; } = new();      // Strongly discouraged to remove; may be removed only by explicit policy.
        public List<decimal> NeverRemoveOnsets { get; set; } = new();    // Hard prohibition on removal (useful for backbeat).
        public List<decimal> NeverAddOnsets { get; set; } = new();       // Onsets that are forbidden for additions (keep style clean).
    }

    /// <summary>
    /// A single protection layer. Multiple layers form a hierarchy where later layers are more refined.
    /// </summary>
    public sealed class GrooveProtectionLayer
    {
        public string LayerId { get; set; } = "";                        // Identifier, e.g. "Base", "PopRockRefine", "ChorusRefine".
        public List<string> AppliesWhenTagsAll { get; set; } = new();     // If set, layer applies only when all tags are enabled.
        public Dictionary<string, RoleProtectionSet> RoleProtections { get; set; } = new(); // Role -> protections.
        public bool IsAdditiveOnly { get; set; }                          // If true, layer only adds protections; never removes earlier ones.
    }

    /// <summary>
    /// Global groove protection policy with hierarchical layers and override merge semantics.
    /// </summary>
    public sealed class GrooveProtectionPolicy
    {
        public GroovePresetIdentity Identity { get; set; } = new();      // The groove identity this policy belongs to.
        public GrooveSubdivisionPolicy SubdivisionPolicy { get; set; } = new(); // (#1) grid + feel.
        public GrooveRoleConstraintPolicy RoleConstraintPolicy { get; set; } = new(); // (#4) role vocabulary constraints.
        public GroovePhraseHookPolicy PhraseHookPolicy { get; set; } = new(); // (#7) fill windows/cadence behavior.
        public GrooveTimingPolicy TimingPolicy { get; set; } = new();     // (#6) micro-timing.
        public GrooveAccentPolicy AccentPolicy { get; set; } = new();     // (#5) velocity shaping.
        public GrooveOrchestrationPolicy OrchestrationPolicy { get; set; } = new(); // (#8) role presence defaults.

        // Hierarchical layers: [0]=parent/base, [1]=child refinement, [2]=grandchild refinement.
        public List<GrooveProtectionLayer> HierarchyLayers { get; set; } = new();

        // Merge strategy for overrides (global + segment overrides).
        public GrooveOverrideMergePolicy MergePolicy { get; set; } = new(); // Controls how overrides combine with base.
    }

    /// <summary>
    /// Defines how an override layer merges with the base policy.
    /// This is metadata only; merging implementation can be added later.
    /// </summary>
    public sealed class GrooveOverrideMergePolicy
    {
        public bool OverrideReplacesLists { get; set; }           // If true, override list replaces; otherwise union/append.
        public bool OverrideCanRemoveProtectedOnsets { get; set; } // If true, override may remove protected items (dangerous; default false).
        public bool OverrideCanRelaxConstraints { get; set; }     // If true, segment can relax density caps/vocab (usually false).
        public bool OverrideCanChangeFeel { get; set; }           // Whether a segment can change swing/shuffle settings.
    }

    // =========================
    // GrooveVariationCatalog (candidates grouped by tags) with hierarchical layers
    // =========================

    /// <summary>
    /// One candidate onset event that may be added (or used as replacement) during variation.
    /// </summary>
    public sealed class GrooveOnsetCandidate
    {
        public string Role { get; set; } = "";                // Which role this candidate applies to.
        public decimal OnsetBeat { get; set; }                // Beat position in 1-based quarter-note units (e.g., 2.5).
        public OnsetStrength Strength { get; set; }           // Strength bucket for accent/dynamics integration.
        public int MaxAddsPerBar { get; set; }                // Local cap for this candidate.
        public double ProbabilityBias { get; set; }           // Bias hint (0..1 typical); deterministic selection uses this as weight.
        public List<string> Tags { get; set; } = new();       // Tags like "Fill", "Pickup", "Drive", "GhostSnare", "OpenHat".
    }

    /// <summary>
    /// Grouping of onset candidates with shared tags and constraints.
    /// </summary>
    public sealed class GrooveCandidateGroup
    {
        public string GroupId { get; set; } = "";             // Stable identifier for the group.
        public List<string> GroupTags { get; set; } = new();  // Group tags for enabling/disabling.
        public int MaxAddsPerBar { get; set; }                // Group-level bar cap.
        public double BaseProbabilityBias { get; set; }       // Group-level bias for selection.
        public List<GrooveOnsetCandidate> Candidates { get; set; } = new(); // The candidate onsets.
    }

    /// <summary>
    /// A hierarchical layer of variation candidates. Later layers refine earlier ones.
    /// </summary>
    public sealed class GrooveVariationLayer
    {
        public string LayerId { get; set; } = "";                     // e.g. "BaseCandidates", "PopRockRefine", "ChorusRefine".
        public List<string> AppliesWhenTagsAll { get; set; } = new();  // Layer applies only if all tags are enabled.
        public List<GrooveCandidateGroup> CandidateGroups { get; set; } = new(); // Groups of candidates.
        public bool IsAdditiveOnly { get; set; }                        // If true, only adds; does not remove earlier candidates.
    }

    /// <summary>
    /// Catalog of optional rhythmic candidates used to vary the groove, grouped by tags.
    /// </summary>
    public sealed class GrooveVariationCatalog
    {
        public GroovePresetIdentity Identity { get; set; } = new();      // The groove identity this catalog belongs to.

        // Hierarchical layers: [0]=parent/base, [1]=child refinement, [2]=grandchild refinement.
        public List<GrooveVariationLayer> HierarchyLayers { get; set; } = new();

        // Explicit list of tags that may be referenced; useful for UI checklists.
        public List<string> KnownTags { get; set; } = new();            // e.g., "Fill", "Pickup", "Drive", "GhostSnare", "OpenHat".

        // Optional compatibility hints for selection/planning systems (#9).
        public List<string> CompatibilityTags { get; set; } = new();    // e.g., "NoTriplets", "SafeForMotifs", etc.
    }

    // =========================
    // SegmentGrooveProfile (just tag enables + density targets)
    // =========================

    /// <summary>
    /// Simple density targets per role for a segment.
    /// This is not the same as "max caps"; it's a desired target, used to select subsets of candidates.
    /// </summary>
    public sealed class RoleDensityTarget
    {
        public string Role { get; set; } = "";          // Role name (e.g., "Comp").
        public double Density01 { get; set; }           // Target density in 0..1 for the segment.
        public int MaxEventsPerBar { get; set; }        // Practical bar cap for this segment (can be <= global cap).
    }

    /// <summary>
    /// Represents per-segment groove configuration as lightweight enabling/disabling of tags and density targets.
    /// This is the "handle" your generator/planner can use per section/phrase/bar window.
    /// </summary>
    public sealed class SegmentGrooveProfile
    {
        public string SegmentId { get; set; } = "";     // e.g., "Verse1", "Chorus2", or arbitrary key.
        public int? SectionIndex { get; set; }          // Optional mapping to an arranged section instance.
        public int? PhraseIndex { get; set; }           // Optional mapping to a phrase within section.
        public int? StartBar { get; set; }              // Optional explicit bar window start.
        public int? EndBar { get; set; }                // Optional explicit bar window end (inclusive or exclusive by convention later).

        // Enabled tags for candidate groups (variation) for this segment.
        public List<string> EnabledVariationTags { get; set; } = new(); // e.g., enable "Pickup" in pre-chorus.

        // Enabled tags for protection layers (if you want segment-specific protection enabling).
        public List<string> EnabledProtectionTags { get; set; } = new(); // e.g., "HalfTimeBackbeatProtection".

        // Density targets per role for this segment.
        public List<RoleDensityTarget> DensityTargets { get; set; } = new();

        // Optional feel overrides; kept lightweight and policy-controlled by merge rules.
        public GrooveFeel? OverrideFeel { get; set; }               // Optional segment feel override.
        public double? OverrideSwingAmount01 { get; set; }          // Optional segment swing intensity.
    }

    // =========================
    // Optional: A single "GroovePresetDefinition" that ties identity + policies + catalogs together
    // (Not required, but often useful as an authoring container.)
    // =========================

    /// <summary>
    /// Represents a complete groove preset definition including identity, base anchor onsets, protection policy, and variation catalog.
    /// This is definitions-only; realization/selection algorithms can be added later.
    /// </summary>
    public sealed class GroovePresetDefinition
    {
        public GroovePresetIdentity Identity { get; set; } = new();           // Who/what this groove is.
        public GrooveInstanceLayer AnchorLayer { get; set; } = new();         // Your existing anchor onsets.
        public GrooveProtectionPolicy ProtectionPolicy { get; set; } = new(); // Global protections + feel/constraints/etc.
        public GrooveVariationCatalog VariationCatalog { get; set; } = new(); // Candidates grouped by tags.
    }

    // =========================
    // Your existing concept placeholder (matches your current code shape)
    // =========================

    /// <summary>
    /// Existing onset layer: onsets in 1-based quarter-note beat units.
    /// </summary>
    public sealed class GrooveInstanceLayer
    {
        public List<decimal> KickOnsets { get; set; } = new(); // Kick onsets.
        public List<decimal> SnareOnsets { get; set; } = new(); // Snare onsets.
        public List<decimal> HatOnsets { get; set; } = new(); // Hat onsets.
        public List<decimal> BassOnsets { get; set; } = new(); // Bass onsets.
        public List<decimal> CompOnsets { get; set; } = new(); // Comp onsets.
        public List<decimal> PadsOnsets { get; set; } = new(); // Pads onsets.
    }
}
