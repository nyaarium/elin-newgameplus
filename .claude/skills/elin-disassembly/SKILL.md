---
name: elin-disassembly
description: Analyzes the Elin disassembly codebase. Use when looking at, reading, searching, or analyzing any file under disassembly/Elin-Decompiled-main/Elin/, including questions about game types, methods, Chara, Card, or decompiled source.
---

# Elin Disassembly

All access to `disassembly/Elin-Decompiled-main/Elin/` goes through the `elin-disassembly-analyst` subagent, using whichever tool your environment provides: `Task`, `mcp_task`, `runSubagent`. Always delegate to subagent rather than reading it yourself.

## When to invoke

Any time you need to:

- Look up a type, method, field, or property in the decompiled source
- Trace logic or inheritance across files
- Verify how the game implements a specific behavior
- Answer any question — yours or the user's — that requires inspecting `.cs` files under the disassembly

## How to invoke

Pass a clear, specific prompt — a type name, method, or behavior you need to understand. The subagent is stateless and can't ask follow-up questions, so be thorough: specify the type or method you're targeting, what you want to know about it, and what you need back (signature, line range, explaination prose, etc.). You don't need to specify file paths or grep patterns; the analyst handles navigation internally.

## Using the result

The analyst returns findings with file, type, method, and line references. Reason over those results freely — use them to inform code you're writing, answer the user, or feed into further subagent calls.
