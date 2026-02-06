# Copilot Instructions

## General Guidelines
- Always use the minimum changes possible to meet each and every acceptance criteria.
- After code is completed, ensure each individual acceptance criterion is met.
- Do not update the epic containing he story, only update what has been asked for.
- Where possible, use existing classes, methods, constants as opposed to creating new classes.
- Do not make assumptions outside of the context provided.

## Response Format
- When the request is to implement a story, do not write the summary to the Copilot chat pane, write story implementation summaries to C:\Users\sleva\source\repos\Music\Music\AI\Completed\.

## Documentation
- Add/update/delete/replace documentation with ONLY compact AI-facing comments that improve future code-change accuracy. Keep comments concise and information-dense.
- Ensure each important point for each method and class needed to improve future AI response is covered in the comments. Do not omit important information just to save space.

### Hard Limits
- Each comment line MUST be <= 140 characters.
- Use ONLY `//` comments. No XML docs, no regions, no long blocks.
- Do NOT repeat what is obvious from code. Document intent, constraints, edge cases, and “don’t break” rules only.
- Do NOT add comments to every method by default—only where it materially reduces ambiguity for future changes.
- Do NOT introduce newlines inside a comment to “cheat” the limits.
- Do NOT write comments documentating what changed (Example: Story 5.1 Move this method from some other class).

### Code Change Rules
- If the file is missing critical context, add 1–3 lines of `// TODO?` questions at the top (counting toward the limit).

### Where to Place Comments (prefer fewer, higher leverage)
- Very top of file: capturing the high-signal “AI contract”:
  - purpose in 1 line
  - key invariants (MUST hold)
  - integration points (external deps, configs, formats)
  - performance/security footguns
- Directly above the most important public entry points OR the trickiest logic: 1–2 lines each, only if needed.
- Above a code line or code block if it provides an AI coding benefit.

### Comment Format (compact, AI-optimized)
- Use terse key:value / tags style. Examples:
  - // AI: purpose=...; owns=...; not=...
  - // AI: invariants=...; order=...; unique=...
  - // AI: deps=Db:X; cfg:KeyY; io=json(v1,casing=camel)
  - // AI: errors=throws Foo when ...; returns null when ...
  - // AI: perf=hotpath O(n); avoid alloc; cache=...
  - // AI: security=no PII logs; sanitize=...; auth=...

### Priority of Information (only include if applicable)
1) Invariants / “must not break” rules
2) External contracts (I/O schema, serialization, DB schema expectations, API routes)
3) Threading / async expectations
4) Failure modes / error semantics
5) Performance constraints / hot paths
6) Security / logging constraints
7) Extension guidance (“if adding feature, modify these places”)

### Guidance
- Assume existing comments are of good quality but they may be out of date, incomplete or potentially do not benefit AI coding.
- Analyze nearby methods (i.e. there is a reference between the classes/methods) for context to better understand intent of the method(s) in this class.
