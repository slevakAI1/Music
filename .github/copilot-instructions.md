   
1. HARD RULES:
   A. Always use the minimum changes possible to achieve the goal by not improving unrelated code.
   B. After code is completed, ensure each individual acceptance criterion is met by identifying the code changes related to the criterion.   
   
2. RESPONSE FORMAT:
   A. In Ask Mode, place a separate link immediately prior to any code blocks that need to be applied. Clicking the link should open the tab that needs updating
   
3. DOCUMENTATION:
   A. Add/update/delete/replace documentation with ONLY compact AI-facing comments that improve future code-change accuracy. Keep comments concise and information-dense.
   B. Ensure each important point for each method and class needed to improve future AI response is covered in the comments. Do not omit important
information just to save space.

3.1. HARD LIMITS
- Each comment line MUST be <= 140 characters.
- Use ONLY `//` comments. No XML docs, no regions, no long blocks.
- Do NOT repeat what is obvious from code. Document intent, constraints, edge cases, and “don’t break” rules only.
- Do NOT add comments to every method by default—only where it materially reduces ambiguity for future changes.
- Do NOT introduce newlines inside a comment to “cheat” the limits.

3.2 CODE CHANGE RULES
- Do NOT change runtime behavior. No logic changes. No refactors. Only comments and whitespace if needed for comment placement.
- If the file is missing critical context, add 1–3 lines of `// TODO?` questions at the top (counting toward the limit).

3.3. WHERE TO PLACE COMMENTS (prefer fewer, higher leverage)
A. Very top of file: capturing the high-signal “AI contract”:
   - purpose in 1 line
   - key invariants (MUST hold)
   - integration points (external deps, configs, formats)
   - performance/security footguns
B. Directly above the most important public entry points OR the trickiest logic: 1–2 lines each, only if needed.

3.4. COMMENT FORMAT (compact, AI-optimized)
- Use terse key:value / tags style. Examples:
  // AI: purpose=...; owns=...; not=...
  // AI: invariants=...; order=...; unique=...
  // AI: deps=Db:X; cfg:KeyY; io=json(v1,casing=camel)
  // AI: errors=throws Foo when ...; returns null when ...
  // AI: perf=hotpath O(n); avoid alloc; cache=...
  // AI: security=no PII logs; sanitize=...; auth=...
  // AI: change=when adding X, update Y and keep Z stable

3.5. PRIORITY OF INFORMATION (only include if applicable)
1) Invariants / “must not break” rules
2) External contracts (I/O schema, serialization, DB schema expectations, API routes)
3) Threading / async expectations
4) Failure modes / error semantics
5) Performance constraints / hot paths
6) Security / logging constraints
7) Extension guidance (“if adding feature, modify these places”)

4. Guidance:
- Assume existing comments are of good quality but may not be 100% accurate or complete.
- Analyze nearby methods (i.e. there is a reference between the classes/methods) for context to better understand intent of the method(s) in this class.




   
