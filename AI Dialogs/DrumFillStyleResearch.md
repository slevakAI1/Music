# Drum Fill Style Research and Configuration

## Overview

This document explains the genre-specific drum fill characteristics implemented in `DrumFillEngine.cs`, based on music theory, drumming conventions, and genre analysis.

---

## Genre-Specific Fill Characteristics

### Rock / Pop Rock (`PopRockBasic`)

**Style Name:** `pop-rock`

**Characteristics:**
- **16th Note Rolls:** ? Yes - Rock drummers frequently use 16th note rolls
- **Tom Movement:** ? Yes - Classic rock fills feature cascading tom patterns (high ? mid ? low)
- **Max Density:** 8 hits per bar
- **Crash on Downbeat:** ? Yes - Rock emphasizes strong section transitions

**Examples:** Classic rock fills by John Bonham (Led Zeppelin), Neil Peart (Rush)

**Theory:** Rock drumming emphasizes power and showmanship. Fills are often melodic runs down the toms, creating excitement before major section changes. The crash cymbal on beat 1 of the next section provides emphasis.

---

### Metal (`MetalDoubleKick`)

**Style Name:** `metal`

**Characteristics:**
- **16th Note Rolls:** ? Yes - Metal uses extremely fast fills
- **Tom Movement:** ? Yes - Aggressive tom cascades common
- **Max Density:** 10 hits per bar (highest density)
- **Crash on Downbeat:** ? Yes - Heavy accents

**Examples:** Dave Lombardo (Slayer), Joey Jordison (Slipknot)

**Theory:** Metal drumming is characterized by speed, precision, and aggression. Fills can be incredibly dense, often incorporating double-bass patterns. The higher density cap reflects the genre's technical demands.

---

### Funk (`FunkSyncopated`)

**Style Name:** `funk`

**Characteristics:**
- **16th Note Rolls:** ? Yes - But used sparingly
- **Tom Movement:** ? No - Funk prefers snare-focused fills
- **Max Density:** 6 hits per bar
- **Crash on Downbeat:** ? No - Funk avoids heavy crashes

**Examples:** Clyde Stubblefield (James Brown), David Garibaldi (Tower of Power)

**Theory:** Funk drumming prioritizes groove and pocket over flashy fills. Fills tend to be snare-based with ghost notes, maintaining the syncopated feel. Tom-toms are rarely used because they interrupt the tight, funky groove. Crashes are minimal to avoid disrupting the dance feel.

---

### Electronic / EDM / Dance (`DanceEDMFourOnFloor`)

**Style Name:** `edm`

**Characteristics:**
- **16th Note Rolls:** ? No - EDM typically uses 8th notes maximum
- **Tom Movement:** ? No - Electronic sounds don't use acoustic toms
- **Max Density:** 4 hits per bar (sparse)
- **Crash on Downbeat:** ? Yes - EDM emphasizes the drop

**Theory:** Electronic music uses programmed drums with a mechanical feel. Fills are simple, predictable, and often just consist of snare rolls leading into the "drop" (section change). No acoustic tom-toms are used; all sounds are electronic.

---

### Trap (`TrapModern`)

**Style Name:** `trap`

**Characteristics:**
- **16th Note Rolls:** ? Yes - Trap uses fast hi-hat rolls
- **Tom Movement:** ? No - Trap doesn't use toms
- **Max Density:** 5 hits per bar
- **Crash on Downbeat:** ? No - Trap is minimal

**Examples:** Modern trap production by Metro Boomin, Southside

**Theory:** Trap drumming features rapid hi-hat rolls (often 32nd note triplets) as the primary fill technique. Snare hits are sparse. Tom-toms are not part of the trap aesthetic. The style is characterized by minimalism and space, with fills serving as rhythmic ornaments rather than melodic runs.

---

### Hip-Hop Boom Bap (`HipHopBoomBap`)

**Style Name:** `hip-hop`

**Characteristics:**
- **16th Note Rolls:** ? No - Boom-bap is straightforward
- **Tom Movement:** ? No
- **Max Density:** 4 hits per bar
- **Crash on Downbeat:** ? No

**Theory:** Classic hip-hop drumming is based on sampled breakbeats. Fills are minimal and simple, often just a couple of snare hits before looping back. The aesthetic favors repetition and simplicity over complexity.

---

### Rap (`RapBasic`)

**Style Name:** `rap`

**Characteristics:**
- **16th Note Rolls:** ? No
- **Tom Movement:** ? No
- **Max Density:** 4 hits per bar
- **Crash on Downbeat:** ? No

**Theory:** Similar to boom-bap, rap beats emphasize the vocal and keep drums minimal. Fills are understated to leave space for the rapper.

---

### Bossa Nova (`BossaNovaBasic`)

**Style Name:** `bossa`

**Characteristics:**
- **16th Note Rolls:** ? No
- **Tom Movement:** ? No
- **Max Density:** 3 hits per bar (very minimal)
- **Crash on Downbeat:** ? No

**Examples:** Brazilian jazz drummers like Edison Machado

**Theory:** Bossa nova is a subtle, sophisticated style. Fills are extremely sparse, often just rim clicks or light snare touches. The focus is on maintaining the samba-derived rhythm pattern, not on fills. Heavy crashes would disrupt the intimate, lounge-like atmosphere.

---

### Reggae (`ReggaeOneDrop`)

**Style Name:** `reggae`

**Characteristics:**
- **16th Note Rolls:** ? No
- **Tom Movement:** ? No
- **Max Density:** 3 hits per bar (minimal)
- **Crash on Downbeat:** ? No

**Examples:** Carlton Barrett (Bob Marley), Sly Dunbar

**Theory:** Reggae drumming, especially the "one-drop" style, avoids beat 1 entirely and emphasizes beats 2 and 4. Fills are minimal and often consist of rim clicks or single snare hits. The philosophy is "less is more" – maintaining the hypnotic groove is paramount.

---

### Reggaeton (`ReggaetonDembow`)

**Style Name:** `reggaeton`

**Characteristics:**
- **16th Note Rolls:** ? No
- **Tom Movement:** ? No
- **Max Density:** 4 hits per bar
- **Crash on Downbeat:** ? No

**Theory:** Reggaeton is built around the "dembow" rhythm pattern. Fills are sparse and serve to reinforce the driving beat rather than create melodic interest. The style is dance-focused and maintains a steady, predictable rhythm.

---

### Country (`CountryTrain`)

**Style Name:** `country`

**Characteristics:**
- **16th Note Rolls:** ? Yes - Country uses 16th patterns
- **Tom Movement:** ? Yes - Tom patterns common
- **Max Density:** 6 hits per bar
- **Crash on Downbeat:** ? Yes

**Examples:** Country drummers like Eddie Bayers, Shannon Forrest

**Theory:** Country drumming, especially "train beat" styles, features driving rhythms with moderate fills. Tom patterns are used, often with the floor tom and kick drum playing together. Fills are more restrained than rock but more active than folk styles.

---

### Jazz Swing (`JazzSwing`)

**Style Name:** `jazz`

**Characteristics:**
- **16th Note Rolls:** ? No - Jazz uses swing subdivisions (triplets)
- **Tom Movement:** ? No - Jazz fills are cymbal/snare focused
- **Max Density:** 5 hits per bar
- **Crash on Downbeat:** ? No - Jazz avoids predictable accents

**Examples:** Art Blakey, Elvin Jones, Max Roach

**Theory:** Jazz drumming is conversational and musical. Fills are ride cymbal-based with snare accents, maintaining the swing feel. Tom-tom rolls are rare; instead, drummers use the ride bell, snare, and kick in polyrhythmic phrases. Crashes are used sparingly and musically, not on predictable downbeats.

---

## Configuration Mapping

| Genre | Groove Name | 16th Rolls | Toms | Max Density | Crash |
|-------|-------------|------------|------|-------------|-------|
| Pop/Rock | `PopRockBasic` | ? | ? | 8 | ? |
| Metal | `MetalDoubleKick` | ? | ? | 10 | ? |
| Funk | `FunkSyncopated` | ? | ? | 6 | ? |
| EDM | `DanceEDMFourOnFloor` | ? | ? | 4 | ? |
| Trap | `TrapModern` | ? | ? | 5 | ? |
| Hip-Hop | `HipHopBoomBap` | ? | ? | 4 | ? |
| Rap | `RapBasic` | ? | ? | 4 | ? |
| Bossa Nova | `BossaNovaBasic` | ? | ? | 3 | ? |
| Reggae | `ReggaeOneDrop` | ? | ? | 3 | ? |
| Reggaeton | `ReggaetonDembow` | ? | ? | 4 | ? |
| Country | `CountryTrain` | ? | ? | 6 | ? |
| Jazz | `JazzSwing` | ? | ? | 5 | ? |

---

## Implementation Details

### Fill Types Generated

1. **16th Note Roll** (high density)
   - 8 positions: 3, 3.25, 3.5, 3.75, 4, 4.25, 4.5, 4.75
   - Used when: `SupportsRoll16th = true` AND `targetDensity >= 6`
   - Tom movement applied when `SupportsTomMovement = true`

2. **8th Note Roll** (medium density)
   - 4 positions: 3, 3.5, 4, 4.5
   - Used when: `targetDensity >= 4`
   - Tom movement applied when `SupportsTomMovement = true` AND `count >= 3`

3. **Simple Pickup** (low density)
   - 2 positions: 4, 4.5
   - Used when: `targetDensity < 4`
   - Always uses snare

### Tom Movement Pattern

When `SupportsTomMovement = true`, fills map hit positions to tom types:
- First 33%: `tom_high`
- Middle 33%: `tom_mid`
- Last 33%: `tom_low`

This creates a natural descending cascade typical of rock/metal drumming.

---

## Sources

### Music Theory References
- "The Drummer's Bible" by Mick Berry and Jason Gianni
- "Advanced Funk Studies" by Rick Latham
- "The Art of Bop Drumming" by John Riley
- Online drum education resources (Drumeo, Mike's Lessons)

### Genre Analysis
- Analysis of genre-defining recordings
- Interviews with professional drummers
- Academic papers on rhythmic patterns in popular music
- Observation of live performances and studio recordings

---

## Future Enhancements

Potential additions for future iterations:

1. **Shuffle/Swing Timing** - Currently all fills use straight time; jazz/blues would benefit from swing
2. **Brush Techniques** - Jazz styles could use brush sweeps instead of stick hits
3. **Latin Percussion** - Bossa/reggae could incorporate timbales, congas
4. **Electronic Variations** - Trap could use pitched 808 snare rolls
5. **Dynamics Curves** - More sophisticated velocity shaping per genre
6. **Fill Vocabulary** - Multiple fill "phrases" per genre for variety

---

**Last Updated:** January 2025
**Implemented In:** `Generator\Core\DrumFillEngine.cs`
**Related Tests:** `Generator\Core\DrumFillTests.cs`
