---
name: elin-disassembly
description: Analyzes decompiled source under disassembly/. This includes the Elin base game (disassembly/Elin-Decompiled-main/Elin/) and any mod DLLs decompiled into disassembly/<ModName>/. Use when looking at, reading, searching, or analyzing any decompiled code - including game types, methods, Chara, Card, mod implementations, Harmony patches, or decompiled source.
---

# Elin Disassembly

All access to `disassembly/` goes through the `disassembly-analyst` subagent, using whichever tool your environment provides: `Task`, `mcp_task`, `runSubagent`. Always delegate to subagent rather than reading it yourself.

## Disassembly sources

- **Base game (primary):** `disassembly/Elin-Decompiled-main/Elin/` — the decompiled Elin game source
- **Mod disassemblies (extras):** `disassembly/<ModName>/` — workshop mod DLLs decompiled via `ilspycmd`

Workshop mods are located at: `/mnt/workshop/<WorkshopID>/`

Check if the decompile already exists: `ls -1 disassembly`

To decompile a mod DLL: `ilspycmd -p -o disassembly/<ModName>/ /path/to/Mod.dll`

## When to invoke

Any time you need to:

- Look up a type, method, field, or property in the decompiled source
- Trace logic or inheritance across files
- Verify how the game implements a specific behavior
- Analyze how a mod hooks into the game (Harmony patches, overrides, API usage)
- Answer any question - yours or the user's - that requires inspecting `.cs` files under the disassembly

## How to invoke

Pass a clear, specific prompt - a type name, method, or behavior you need to understand. The subagent is stateless and can't ask follow-up questions, so be thorough: specify the type or method you're targeting, what you want to know about it, and what you need back (signature, line range, explaination prose, etc.). You don't need to specify file paths or grep patterns; the analyst handles navigation internally.

For mod analysis, specify which workshop mods by **name or ID** to analyze.

## Using the result

The analyst returns findings with file, type, method, and line references. Reason over those results freely and use them to inform code you're writing, answer the user, or feed into further subagent calls.
