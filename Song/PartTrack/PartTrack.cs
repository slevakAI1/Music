// AI: purpose=Container for a single instrument's sequence of note events used for MIDI/transform pipelines.
// AI: invariants=PartTrackNoteEvents is the canonical event list; order matters for rendering; overlaps/duplicates allowed.
// AI: deps=Consumed by MIDI export and generators; changing property names breaks serialization/UI mappings.
// AI: constraints=MidiProgramNumber expected 0-255; MidiProgramName maps to program number in UI; PartTrack constructed with event list.
// AI: change=Meta property added for Story M1; PartTrackId nested type co-located with entity it identifies.

using Music.MyMidi;
using Music.Song.Material;

namespace Music.Generator
{
    // AI: Lightweight DTO: holds program info and ordered PartTrackEvent list; keep constructor semantics stable.
    // AI: Meta property carries domain/kind/provenance; defaults to song role track for backward compatibility.
    public sealed class PartTrack
    {
        /// <summary>
        /// Stable identity for a PartTrack across editing sessions.
        /// </summary>
        public readonly record struct PartTrackId(string Value)
        {
            public static PartTrackId NewId() => new(Guid.NewGuid().ToString("N"));

            public override string ToString() => Value;
        }

        // AI: Meta: identity/domain/kind metadata; defaults to SongAbsolute+RoleTrack for backward compatibility.
        public PartTrackMeta Meta { get; set; } = new();

        // AI: MidiProgramName: UI label; may be null/empty; do not embed runtime state here.
        public string MidiProgramName { get; set; }
        // AI: MidiProgramNumber: 0..255 per MIDI; consumers rely on this for program selection.
        public int MidiProgramNumber { get; set; }

        // AI: PartTrackNoteEvents: ordered list of events (absolute ticks/durations); used directly for MIDI generation.
        public List<PartTrackEvent> PartTrackNoteEvents { get; set; } = new();

        // AI: ctor expects a pre-built list; callers may reuse or mutate the list but tests expect predictable ordering.
        public PartTrack(List<PartTrackEvent> partTrackNoteEvents)
        {
            PartTrackNoteEvents = partTrackNoteEvents;
        }
    }
}
