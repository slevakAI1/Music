

#solution

#file:'C:\Users\sleva\source\repos\Music\Music\AIPlans\CurrentEpic.md'

#file:'C:\Users\sleva\source\repos\Music\Music\AIPlans\ProjectArchitecture.md'

# Pre-Analysis Request for Agile Story Implementation

I need you to analyze an agile story from the current epic and provide **context and questions only** — no technical implementation suggestions.

## Story Format
Stories follow this structure:
- **Story ID**: (e.g., "Story C3")
- **Title**: Brief description
- **User Story**: "As a [role] I want [feature] So that [benefit]"
- **Acceptance Criteria**: Checklist of specific requirements

## What I Need From You

### 1. Story Intent Summary
- **What** is this story trying to accomplish? (1-2 sentences)
- **Why** is this important to the system? (business/technical value)
- **Who** benefits from this? (developer, generator, end-user)

### 2. Acceptance Criteria Checklist
- Extract all AC checkboxes into a simple numbered list
- Group related criteria together if helpful
- Highlight any AC that seem ambiguous or unclear

### 3. Dependencies & Integration Points
- List other stories this depends on (by ID)
- Identify what existing code/types this story will interact with
- Note what this story provides for future stories

### 4. Inputs & Outputs
- What data/objects does this story consume?
- What data/objects does this story produce?
- What configuration/settings does it read?

### 5. Constraints & Invariants
- What rules must ALWAYS be true? (e.g., "never prune IsMustHit onsets")
- What are the hard limits? (e.g., "MaxHitsPerBar")
- What order must operations happen in?

### 6. Edge Cases to Test
- Brainstorm scenarios that could break the implementation
- Identify boundary conditions (empty lists, zero values, null checks)
- List error cases (invalid input, configuration conflicts)
- Suggest combination scenarios (multiple constraints active)

### 7. Clarifying Questions
- List ANY ambiguous requirements that need clarification
- Identify potential interpretation conflicts
- Highlight missing information (e.g., "What happens when X and Y both true?")

### 8. Test Scenario Ideas
- Suggest specific unit test names based on AC
- Propose test data setups for complex scenarios
- Identify determinism verification points

## What NOT To Include

❌ **Do NOT suggest:**
- Technical implementation approaches
- Code patterns, algorithms, or data structures  
- API designs or method signatures
- Specific programming solutions
- Class hierarchies or architecture
- Performance optimization techniques

## Output Format
Use clear markdown with headers for each section. Be concise but thorough. 
Write complete response to file PreAnalysis_<Story number>.md in Music/AIPlans folder. 
Limit response in chat pane to 4 bullets only summarizing what was done.

---

Focus on **understanding the problem**, not solving it.
Now analyze this story: SC1
