GitHub Copilot
Purpose (short)
•	A HarmonyPolicy makes harmony rules explicit and configurable so validation, pitch selection and generation behave predictably instead of silently relying on implicit assumptions.
•	It decouples “what the editor allows/suggests” from “what the generator must enforce”, and lets you evolve behavior (e.g., allow secondary dominants later) without changing validator/generator internals.
Why not simply allow everything
•	Musical coherence: allowing every possible chord form (chromatic tones, borrowed chords, secondary dominants, extreme alterations) produces correct-but-unmusical results for many styles. Policies constrain choices so generated parts sound idiomatic.
•	Predictability & testing: a fixed policy keeps generation deterministic and makes unit tests meaningful — you can assert behavior under a known rule set.
•	Safety for consumers: editors, teachers, or non-expert users expect helpful diagnostics; hard-to-explain or surprising non-diatonic chords should be surfaced, not silently accepted.
•	Algorithm complexity: pitch-context, voicing, and voice-leading code is simpler and safer when assumptions about scale membership hold. Permitting everything forces more sophisticated (and error-prone) logic earlier.
•	UX: editors can surface warnings/advice when policy disallows a chord; this preserves user intent while guiding authorship.
Genres and policy examples
•	Pop / Folk / Simple rock: StrictChordToneScaleMembership = true, AllowNonDiatonicChordTones = false. Keeps harmonies diatonic and safe.
•	Jazz / Fusion: AllowNonDiatonicChordTones = true, AllowSecondaryDominants = true, StrictChordToneScaleMembership = false. Accepts altered/extended harmonies and chromaticism.
•	Modal / Ambient: AllowBorrowedChords = true, relax scale membership — different rules per modal context.
•	Film/Orchestral: may permit wide palette but prefer editor-led authoring and explicit section policies.
Recommended usage patterns
•	Keep conservative default (as implemented): strict diatonic behavior, warnings for non‑diatonic chords.
•	Allow overrides:
•	Per-song or per-section HarmonyPolicy so sections (verse/chorus/bridge) can have different harmonic flavors.
•	Policy in HarmonyValidationOptions (already added) so callers choose behavior at validation time.
•	Diagnostics-first: validators should produce structured warnings/errors (not throw) so UI can show advisories while still allowing generation where appropriate.
•	Phased rollout: enable new flags (AllowSecondaryDominants, AllowBorrowedChords) only after adding tests and extending pitch-context logic.
Next steps (if you want)
•	Add per-section policy support (small change to Section model + generator).
•	Add unit tests showing generator/validator behavior under different HarmonyPolicy presets.
•	Expose policy presets in the editor UI so users pick a style (Pop, Jazz, Modal, Custom).
If you want I can:
•	Add per-section policy plumbing, or
•	Create a small set of named presets and unit tests demonstrating differences.
