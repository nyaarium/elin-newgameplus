# New Game++

This mod lets you export your character and import it into a new game, as a new game+ character.

Now requires: [Mod Options](https://steamcommunity.com/sharedfiles/filedetails/?id=3381182341)

> [**Steam Workshop Page**](https://steamcommunity.com/sharedfiles/filedetails/?id=3655459421)

## Game updated? Found a bug?

[File an issue on the Issues tab](https://github.com/nyaarium/elin-newgameplus/issues?q=is%3Aissue) with **what the problem is**. (I work with GitHub repos every day, so I will notice if you file properly)

If you just comment on Steam, I may not notice (I'm not even logged in most of the time).

## Credits

**Original Mod:** [New Game+](https://steamcommunity.com/sharedfiles/filedetails/?id=3373526993)

- **EN:** This is a revival and feature extension of the above mod. Unsubscribe from the original when switching to this version.
- **JP:** この mod は上記の mod を復活して機能を追加して更新しました。本バージョンをサブスク登録した際に元バージョンのサブスクを解除してください。
- **CN:** 这是上述模组的复兴和功能扩展版本。切换到本版本时，请取消订阅原模组。

Credits again to the original author for creating the original mod. This mod has changed quite significantly, so I don't think anyone minds me publishing a fork.

## How to Use

1. Middle-click your heart stone in your old save to export it.
2. Visit the Mod Options if you want to customize what's imported.
3. Start a new game. Your appearance will be restored. Change as desired. You can also change your race/class.
4. Continue until you see the game world. Your stats will restore, and items spawn.

## What will carry over to a new start?

**Always included:**

- Biography or Profile, including name and nickname (except favorite, hobby, and work. _You can use a whip on yourself to change hobby and work anyway._)
- Portrait (but you need to re-slide your PCC)
- Race and Class, unless you re-roll it in character creation (Warmage will need to re-choose domain)
- Level, feats
- Counters for little dead and saved
- Known BGMs (Tapes)
- Sketches (Drawings)
- Card codex (creatures discovered, kills, weakspots, drop bonus)
- Key Items: Fragrance of Goddess, Black Cat's Saliva, Feather Shard of Goddess, and Lucky Coin (you will need to go buy the license again)

**Configuable in Mod Options:**

- Include Toolbar
- Include Worn Equipment
- Include Inventory and Container Contents (properties SHOULD carry over, but let me know if something is not)
- Include Bank
- Include Player Level
- Include Acquired Feats (unchecking will refund the points)
- Include Abilities
- Include Skills
- Include Faith
- Include Karma
- Include Fame (tax is based on fame. importing fame will start you with high taxes)
- Include Craft Recipes
- Include Codex
- Include Influence
- Cure Ether Diseases
- Cure Mutations

## What won't carry over to a new start?

- Any exceptions mentioned above
- 『Unique Names』on legendary/mythical items (these are randomly generated based on item UID and game seed, which changes on a new run)
- Home level, home policy, home skills, or home feats
- Companions
- Buildings
- Guild experience
- Paid debt

## Updates

Bugs Fixes and Rewrites:

- Fixed an exception because the mod was referencing an old field "qualityTier". _(I think it was renamed to "tier" at some point)_
- Included as much item properties as possible _(material, color, etc.)_
- Fixed some stat calculation issues
- Added support for Mod Options configuration UI.
- Switched character dumper to JSON for structure and maintainability.
- Character stats now properly export and restore, including temporary stat modifiers, conditions, mutations, corruption history. _(configurable in Mod Options)_
- Items are now organized by category _(toolbar, equipment, backpack contents)_ and configurable in Mod Options.
- Items start on your character instead of spilling onto the ground.
- Added config options to control whether healing and disease curing occur during import.
- Thanks to AI Agent _(Cursor Code)_ for refactor help, rough translations, and reverse-engineering decompiled code/docs totaling **240000+ lines**
