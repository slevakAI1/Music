# Story 7.5.8 Usage Guide

## Using the Unified Tension Context API

### Overview
Story 7.5.8 introduces `GetTensionContext`, a unified query method that returns all tension-related information in a single immutable context object. This is the **preferred API** for Stage 8/9 integration.

---

## Quick Start

### Basic Usage
```csharp
// Create or obtain an ITensionQuery instance
ITensionQuery tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

// Query tension context for a specific bar
TensionContext context = tensionQuery.GetTensionContext(
    absoluteSectionIndex: 0,
    barIndexWithinSection: 2
);

// Access all tension information from the context
double macroTension = context.MacroTension.MacroTension;  // Section-level tension [0..1]
double microTension = context.MicroTension;               // Bar-level tension [0..1]
TensionDriver drivers = context.TensionDrivers;           // Why tension exists
SectionTransitionHint hint = context.TransitionHint;      // Build/Release/Sustain/Drop

// Check phrase position
if (context.IsPhraseEnd)
{
    // Place drum fill, vocal breath, etc.
}

if (context.IsSectionStart)
{
    // Trigger orchestration change, crash cymbal, etc.
}
```

---

## Stage 8 Use Cases

### Phrase-Aware Energy Deltas
```csharp
public void ApplySectionArc(int sectionIndex, int barIndex)
{
    var context = tensionQuery.GetTensionContext(sectionIndex, barIndex);
    
    // Rising micro tension into phrase end = increase intensity
    if (context.IsPhraseEnd && context.MicroTension > 0.6)
    {
        // Apply velocity lift, add fill, increase density
        velocityMultiplier = 1.0 + (context.MicroTension * 0.3);
    }
    
    // Section start = reset or apply orchestration change
    if (context.IsSectionStart)
    {
        ApplyOrchestrationForTransition(context.TransitionHint);
    }
}
```

### Cross-Role Density Budgets
```csharp
public void ApplyDensityBudget(int sectionIndex, int barIndex)
{
    var context = tensionQuery.GetTensionContext(sectionIndex, barIndex);
    
    // Low tension at section end = thin out for breathing room
    if (context.IsSectionEnd && context.MacroTension.MacroTension < 0.4)
    {
        // Reduce comp/pads density
        compDensityMultiplier = 0.7;
        padsSustainReduction = 0.5;
    }
}
```

---

## Stage 9 Use Cases

### Motif Placement Decisions

#### High-Energy + Low Tension (Release Moments)
```csharp
public bool IsIdealForPrimaryHook(int sectionIndex, int barIndex, double currentEnergy)
{
    var context = tensionQuery.GetTensionContext(sectionIndex, barIndex);
    
    // High energy + low tension = triumphant release (perfect for hook)
    bool isHighEnergy = currentEnergy > 0.7;
    bool isLowTension = context.MacroTension.MacroTension < 0.5;
    bool isRelease = context.TensionDrivers.HasFlag(TensionDriver.Resolution);
    
    return isHighEnergy && isLowTension && isRelease;
}
```

#### High Tension (Anticipatory Motifs)
```csharp
public bool IsIdealForAnticipationMotif(int sectionIndex, int barIndex)
{
    var context = tensionQuery.GetTensionContext(sectionIndex, barIndex);
    
    // High tension building toward next section = anticipatory motif placement
    bool isHighTension = context.MacroTension.MacroTension > 0.6;
    bool isBuilding = context.TransitionHint == SectionTransitionHint.Build;
    bool hasAnticipation = context.TensionDrivers.HasFlag(TensionDriver.Anticipation) ||
                           context.TensionDrivers.HasFlag(TensionDriver.PreChorusBuild);
    
    return isHighTension && (isBuilding || hasAnticipation);
}
```

### Call & Response Patterns
```csharp
public void PlaceMotifBasedOnTransition(int sectionIndex, int barIndex)
{
    var context = tensionQuery.GetTensionContext(sectionIndex, barIndex);
    
    switch (context.TransitionHint)
    {
        case SectionTransitionHint.Build:
            // Use rhythmic foreshadowing motif
            PlaceRhythmicMotif(context);
            break;
            
        case SectionTransitionHint.Release:
            // Use melodic hook motif
            PlaceMelodicHook(context);
            break;
            
        case SectionTransitionHint.Drop:
            // Sudden silence or minimal motif
            PlaceMinimalMotif(context);
            break;
            
        case SectionTransitionHint.Sustain:
            // Continue current motif pattern
            SustainCurrentMotif(context);
            break;
    }
}
```

---

## Stage 10 Use Cases

### Lyric-Driven Accompaniment Ducking
```csharp
public void ApplyVocalDucking(int sectionIndex, int barIndex, bool vocalActive)
{
    var context = tensionQuery.GetTensionContext(sectionIndex, barIndex);
    
    // Low tension + release + vocals = reduce accompaniment density
    if (vocalActive && 
        context.MacroTension.MacroTension < 0.5 &&
        context.TransitionHint == SectionTransitionHint.Release)
    {
        // Duck comp and pads to make room for vocals
        compDensityMultiplier = 0.5;
        padsRegisterShift = -12; // Move down an octave
        keysVoicingSimplification = true;
    }
}
```

### Phrase Breath Marks
```csharp
public void ApplyVocalPhraseShaping(int sectionIndex, int barIndex)
{
    var context = tensionQuery.GetTensionContext(sectionIndex, barIndex);
    
    // Phrase end = natural breath point for vocals
    if (context.IsPhraseEnd)
    {
        // Shorten accompaniment notes for breathing room
        noteOffsetBeforeBarEnd = 120; // Ticks before bar end
        
        // Micro tension influences phrase ending intensity
        if (context.MicroTension > 0.7)
        {
            // Strong cadence = louder ending
            finalNoteVelocity = 100;
        }
        else
        {
            // Soft cadence = quieter ending
            finalNoteVelocity = 70;
        }
    }
}
```

---

## Transition Hint Decision Matrix

### How to Interpret `SectionTransitionHint`

| Transition Hint | Energy Delta | Tension Delta | Arrangement Strategy |
|----------------|--------------|---------------|---------------------|
| **Build** | Increasing | Increasing | Add layers, increase density, anticipation |
| **Release** | Sustaining or slight drop | Significant drop | Pull back, let it breathe, feature hook |
| **Drop** | Significant drop | Significant drop | Sudden reduction for impact, breakdown |
| **Sustain** | Minimal change | Minimal change | Maintain current orchestration |
| **None** | N/A (last section) | N/A | Natural ending, decay |

### Example: Orchestration Strategy
```csharp
public void ApplyTransitionStrategy(SectionTransitionHint hint)
{
    switch (hint)
    {
        case SectionTransitionHint.Build:
            // Add cymbal crescendo
            // Increase drum density
            // Add pad layers
            // Raise register
            crashCymbal = true;
            hatBusyness = 1.3;
            padLayers = 2;
            registerLift = +12;
            break;
            
        case SectionTransitionHint.Release:
            // Open space for hook
            // Reduce comp density
            // Simplify drums
            compDensity = 0.6;
            drumSimplification = true;
            openSpace = true;
            break;
            
        case SectionTransitionHint.Drop:
            // Sudden silence or minimal layer
            // Impact cymbal
            // Restart with sparse arrangement
            silenceDuration = 480; // Half bar
            impactCymbal = true;
            minimalReentry = true;
            break;
            
        case SectionTransitionHint.Sustain:
            // Continue current pattern
            // Minor variation only
            continueCurrent = true;
            minorVariation = 0.1;
            break;
    }
}
```

---

## Tension Driver Usage

### Checking for Specific Drivers
```csharp
var context = tensionQuery.GetTensionContext(sectionIndex, barIndex);

// Check for anticipation
if (context.TensionDrivers.HasFlag(TensionDriver.Anticipation))
{
    // This section is building toward something
    ApplyAnticipationBehavior();
}

// Check for resolution
if (context.TensionDrivers.HasFlag(TensionDriver.Resolution))
{
    // This section is releasing tension
    ApplyResolutionBehavior();
}

// Check for multiple drivers (flags can combine)
if (context.TensionDrivers.HasFlag(TensionDriver.PreChorusBuild | TensionDriver.Anticipation))
{
    // Strong build into chorus
    ApplyStrongBuildBehavior();
}
```

### Driver Interpretation Table

| Driver | Typical Sections | Musical Meaning | Arrangement Implication |
|--------|-----------------|-----------------|------------------------|
| `None` | Any | No specific tension driver | Default behavior |
| `Opening` | Intro | Section opening creating initial tension | Gentle entry, build anticipation |
| `PreChorusBuild` | Pre-Chorus | Building tension toward chorus | Increase layers, anticipation |
| `Anticipation` | Before high-energy section | Expecting something to happen | Raise tension, prepare for release |
| `Resolution` | Chorus | Tension release moment | Open space, feature hook |
| `BridgeContrast` | Bridge | Providing contrast | Different texture/orchestration |
| `Peak` | Solo, Final Chorus | Peak moment | Maximum energy/intensity |
| `Cadence` | Section/phrase end | Approaching resolution | Pull/fill, phrase ending |
| `Breakdown` | Breakdown section | Reduced instrumentation | Thin out, anticipation |
| `Drop` | EDM drop, Breakdown | Sudden energy/tension release | Dramatic reduction |

---

## Performance Considerations

### Efficient Querying
```csharp
// ? Good: Single query for all information
var context = tensionQuery.GetTensionContext(sectionIndex, barIndex);
ProcessAllAspects(context);

// ? Less efficient: Multiple queries for same information
var macro = tensionQuery.GetMacroTension(sectionIndex);
var micro = tensionQuery.GetMicroTension(sectionIndex, barIndex);
var flags = tensionQuery.GetPhraseFlags(sectionIndex, barIndex);
var hint = tensionQuery.GetTransitionHint(sectionIndex);
```

### Caching Contexts
```csharp
// Cache contexts for reuse within a section rendering pass
private Dictionary<(int section, int bar), TensionContext> _contextCache = new();

public TensionContext GetCachedContext(int sectionIndex, int barIndex)
{
    var key = (sectionIndex, barIndex);
    if (!_contextCache.TryGetValue(key, out var context))
    {
        context = tensionQuery.GetTensionContext(sectionIndex, barIndex);
        _contextCache[key] = context;
    }
    return context;
}
```

---

## Migration Guide

### Before Story 7.5.8
```csharp
// Multiple queries required
var macro = tensionQuery.GetMacroTension(sectionIndex);
var micro = tensionQuery.GetMicroTension(sectionIndex, barIndex);
var (isPhraseEnd, isSectionEnd, isSectionStart) = 
    tensionQuery.GetPhraseFlags(sectionIndex, barIndex);

// Access fields individually
double tension = macro.MacroTension;
TensionDriver drivers = macro.Driver;
```

### After Story 7.5.8 (Recommended)
```csharp
// Single unified query
var context = tensionQuery.GetTensionContext(sectionIndex, barIndex);

// All information available from context
double macroTension = context.MacroTension.MacroTension;
double microTension = context.MicroTension;
TensionDriver drivers = context.TensionDrivers;  // Convenience property
SectionTransitionHint hint = context.TransitionHint;  // Now included
bool isPhraseEnd = context.IsPhraseEnd;
```

---

## Best Practices

### 1. Use `GetTensionContext` for Most Scenarios
Prefer the unified query unless you specifically need only one piece of information.

### 2. Check Transition Hints for Section Boundaries
Always consider the transition hint when making orchestration decisions at section starts.

### 3. Combine Energy and Tension
Don't rely on tension alone; combine with energy for best results:
```csharp
bool isIdealMoment = (energy > 0.7) && (context.MacroTension.MacroTension < 0.5);
```

### 4. Use Driver Flags for Explainability
Tension drivers help explain *why* certain tension values exist, which is valuable for debugging and diagnostics.

### 5. Respect Phrase Boundaries
Use `IsPhraseEnd` to place fills, breaths, and other phrase-boundary events.

---

## Troubleshooting

### Context Returns Unexpected Values
- **Check section/bar indices**: Ensure indices are valid (0-based, within bounds)
- **Verify tension query type**: `NeutralTensionQuery` returns zero tension by design
- **Review energy arc**: Tension is derived from energy constraints

### Transition Hint is Always None
- **Last section**: Final section always has `None` hint (expected)
- **Using NeutralTensionQuery**: Neutral implementation returns `None` by design
- **Check DeterministicTensionQuery**: Ensure using deterministic implementation

### Drivers Don't Match Expectations
- **Multiple drivers**: Flags can combine (use `HasFlag` to check)
- **Section type**: Some drivers are section-type specific
- **Energy deltas**: Anticipation requires significant energy increase to next section

---

## Additional Resources

- **Story 7.5.8 Implementation Summary**: `AI Dialogs\Story_7_5_8_Implementation_Summary.md`
- **Test Suite**: `Song\Energy\TensionContextIntegrationTests.cs`
- **Story 7.5.1 (Tension Model)**: Original tension model and contracts
- **Story 7.5.2 (Macro Tension)**: Section-level tension computation
- **Story 7.5.3 (Micro Tension)**: Within-section tension maps
