# Automated Debugging Prompt

You are now in **Automated Debugging Mode** using iterative trace analysis.

## Available Tools
- **Tracer class** at `Music\Errors\Tracer.cs` with methods:
  - `Tracer.DebugTrace(string text)` - writes to DebugTrace.txt
  - `Tracer.Write(string filename, string text)` - writes to custom file
- **DebugTrace.txt** location: `C:\Users\sleva\source\repos\Music\Music.Tests\Errors\DebugTrace.txt`

## Debugging Process (Iterative Until Fixed)

### Step 1: Analyze Problem Statement
- Review the problem statement and context provided above
- Identify key methods, classes, and data flows involved
- Determine what data needs to be traced to diagnose the issue

### Step 2: Add Trace Instrumentation
- Insert `Tracer.DebugTrace()` calls strategically:
  - At method entry/exit points with parameter values
  - Before/after critical operations or calculations
  - At decision points (if/switch branches taken)
  - Where exceptions might occur
  - To capture intermediate variable states
- Use clear, structured messages: `"MethodName: variable=value, state=X"`
- Add timestamps are automatic (Eastern Time)

### Step 3: Create/Update Unit Tests
- Write or modify unit tests in `Music.Tests` project to:
  - Reproduce the problem scenario
  - Exercise the instrumented code paths
  - Clear DebugTrace.txt at test start: `File.WriteAllText(path, string.Empty)`
  - Run the problematic code flow
- Ensure tests actually execute the traced code

### Step 4: Analyze Trace Data
- Read `C:\Users\sleva\source\repos\Music\Music.Tests\Errors\DebugTrace.txt`
- Analyze the captured data:
  - Trace execution flow through methods
  - Identify unexpected values or state
  - Find where actual behavior diverges from expected
  - Detect missing logic, wrong calculations, or incorrect branching

### Step 5: Fix or Iterate
Based on analysis, choose the appropriate action:

**A) If problem root cause is identified:**
- Remove or comment out unnecessary trace statements
- Fix the identified bug(s)
- Run tests to verify the fix
- Read DebugTrace.txt again to confirm correct behavior
- If fixed: **STOP** and report resolution
- If not fixed: proceed to B or C

**B) If more data needed:**
- Add new trace statements for additional variables/methods
- Adjust existing traces for better clarity
- Go back to Step 3

**C) If data is noisy/unclear:**
- Remove trace statements that aren't helpful
- Refine messages for better readability
- Go back to Step 3

## Critical Rules
1. **Always clear DebugTrace.txt** at the start of each test run
2. **Read DebugTrace.txt after every test run** before making decisions
3. **Be surgical with traces** - capture what you need, remove what you don't
4. **One iteration at a time** - add traces → run tests → analyze → decide
5. **Verify fixes** - after fixing code, run tests and check trace output confirms correct behavior
6. **Stop when done** - clearly state when the problem is resolved

## Output Format for Each Iteration
```
### Iteration N: [Brief Description]

**Hypothesis:** [What you think might be wrong]

**Trace Strategy:** [What data you're capturing and why]

**Test Changes:** [What tests you're running/modifying]

**Trace Analysis:** [What the DebugTrace.txt revealed]

**Action Taken:** [Fix applied / More data needed / Traces adjusted]

**Status:** [CONTINUING | FIXED]
```

---

## Ready to Debug
Proceed with Step 1: Analyze the problem statement above and begin the iterative debugging process.
