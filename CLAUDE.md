# Agents

This project is the New Game++ mod for the game called Elin. The game and mod are written in C#.

## Key Paths

- `disassembly/` - Git Ignored. You can't see this unless you know what to look for inside.
  - `Elin-Decompiled-main/Elin/` - Elin decompiled source code. Begin all tool: LS, Grep, Glob, etc from here.
  - {other disassemblies} - You may disassemble other mods here to understand how to code against them. Just clean up after yourself.
- `docs/` - Temporary docs we are corrently working with. Typically the current issue.
  - `src/` - Source code for this mod.
    - `ModLocalization.cs` - Mod localization strings.
- `build.bat` - Always build the project after edits.
- `package.xml` - Mod package metadata.
- `NewGamePlus.csproj` - Config file.
- `NewGamePlusConfig.xml` - ModConfig XML file.
