# Elin Systems Reference

## Data Model

### Element Fields

- **`vBase`** (int field) - Base value before modifiers. Modified by:
  - Training/leveling: `ModExp()` calls `ModBase(ele, 1)` when `vExp >= ExpToNext` → permanent +1 to `vBase`
  - Feats: When set directly via `SetFeat()`, the feat's own value goes to `vBase`. When applied via `Feat.Apply()`, OTHER elements' `vBase` is modified
  - Permanent mutations: Ether diseases and other permanent effects modify `vBase` via `ModBase()`
  - **Note**: Race/Job bonuses go to `vSource`, NOT `vBase`. Material bonuses also go to `vSource`
  - **Feats (tier)**: For feat elements, `vBase` = purchased tier count only. Race/job tier is in `vSource`. Total tier = `Value` = `vBase + vSource`. `SetFeat(id, value)` uses `SetBase(id, value - (feat?.vSource ?? 0))` so the stored `vBase` is the delta above race/job.
- **`vSource`** (int field) - Accumulates modifiers from element maps (race, job, material). Modified by:
  - `ApplyElementMap()`: Race/Job/Material bonuses modify `vSource` via `orCreateElement.vSource += calculatedValue`
  - For feats from race/job, the map value is the **tier level** (1, 2, 3) and ends up in `vSource`; total feat tier = `vBase + vSource`
  - Added to `vBase` for `ValueWithoutLink` calculation: `ValueWithoutLink = vBase + vSource`
- **`vLink`** (int field) - Bonuses from linked containers (equipment, conditions). Modified by:
  - Equipment: `SetParent(owner)` → `ModLink()` adds equipment's `vBase + vSource` to character's `vLink`
  - Conditions: `SetParent(owner)` → `ModLink()` adds condition's element contributions to character's `vLink`
  - Used in "Base X + Y" display where Y = `vLink + faction bonus`
  - Included in `Value` calculation: `Value = ValueWithoutLink + vLink + ValueBonus(owner)`
- **`Value`** (int property) - Total calculated stat: `ValueWithoutLink + vLink + ValueBonus(owner)`
  - `ValueWithoutLink = vBase + vSource`
  - `ValueBonus(owner)` is typically 0 for base `ElementContainer`, but can be overridden (e.g., faction bonuses)
- **`ValueWithoutLink`** (int property) - Intermediate calculation: `vBase + vSource`
  - Used in training cost calculations and experience gain checks
- **`DisplayValue`** (int property) - Value shown in UI. Default implementation returns `Value`, but can be overridden
- **`vPotential`** (int field) - Base potential stat value. Can be set by feats (e.g., `vPotential = owner.Chara.LV` in `Feat.Apply()`)
  - Used in `Potential` calculation: `Potential = vPotential + vTempPotential + vSourcePotential + MinPotential`
- **`MinPotential`** (int property, virtual) - Minimum potential value. Default implementation returns `100`
  - Used in `Potential` calculation: `Potential = vPotential + vTempPotential + vSourcePotential + MinPotential`
  - Can be overridden by element subclasses to provide different minimum potential values
- **`vSourcePotential`** (int field) - Potential from element maps (race, job, material). Modified by:
  - `ApplyElementMap()`: For skill elements (`category == "skill"`), modifies `vSourcePotential` via `orCreateElement.vSourcePotential += GetSourcePotential(value) * num`
  - Used in `Potential` calculation
- **`vTempPotential`** (int field) - Temporary potential modifier. Modified by:
  - `ModTempPotential()`: Training increases `vTempPotential`, which affects experience gain rate
  - Used in `Potential` calculation and training cost: `CostTrain = Max((ValueWithoutLink / 10 + 5) * (100 + vTempPotential) / 500, 1)`
- **`vExp`** (int field) - Experience points for the element. Modified by `ModExp()`:
  - When `vExp >= ExpToNext`: Triggers level up → `ModBase(ele, 1)` (permanent +1 to `vBase`)
  - When `vExp < 0` and `ValueWithoutLink > 1`: Triggers level down → `ModBase(ele, -1)`
  - Special values: `vExp == -1` = faction-wide element, `vExp == -2` = party-wide element

### Card Storage Layout

`BaseCard` declares three sparse dictionaries (`BaseCard.cs:6-13`): `mapObj`, `mapInt`, `mapStr`. `Card` adds the fixed-size `_ints[30]` array, the packed `_bits1` / `_bits2`, and the `sockets` list on top of those.

- **`Card._ints`** (`int[30]`, declared on `Card`) - Fixed-size primary array. Used indices on `Card`:
  - `[0]`: `_bits1` packed bitfield (isOn, isHidden, isCrafted, etc.)
  - `[1]`: `uid` (unique ID); also stores spell/ability type ID for scrolls/whips
  - `[2]`: `_bits2` packed bitfield (noSell, isCopy, etc.)
  - `[4]`: `idMaterial` (material ID)
  - `[5]`: `dir` (rotation)
  - `[6]`: `Num` (stack quantity)
  - `[7]`: `_x` (pos.x)
  - `[8]`: **unused on `Card`**. Do not confuse with `mapInt[8]` = `c_containerSize`.
  - `[9]`: `_z` (pos.z)
  - `[10]`: `genLv`
  - `[14]`: `hp` (current hit points)
  - `[24]`: `feat` (unspent feat points)
  - `[25]`: `LV` (character level)
  - `[26]`: `exp` (experience points)
- **`Card._bits1` / `Card._bits2`** - Packed bitfields unpacked from `_ints[0]` and `_ints[2]` during deserialization via `SetInt()`. Game never calls `ChangeMaterial` on load. It calls `_bits1.SetInt(_ints[0])` and `_bits2.SetInt(_ints[2])` directly.
- **`Card.mapInt`** (`Dictionary<int, int>`, sparse) - Property keys defined in `CINT.cs`. Common entries on containers:
  - `3`: `c_dyeMat` (dye material)
  - `7`: `c_charges` (charges remaining)
  - `8`: `c_containerSize` (container grid: `width * 100 + height`; 0 means default 8x5)
  - `15`: `c_indexContainerIcon` (icon variant)
  - `21`: `c_priceFix` (price modifier; shop pricing)
  - `27`: `c_ammo` (ammo count)
  - `50`: `c_lockLv` (lock level)
  - `129`: `isAmbushWarned` (added in EA 23.299; no `c_*` getter, accessed via `GetBool(129)` / `GetInt(129)`)
- **`Card.mapObj`** (`Dictionary<int, object>`, sparse) - Boxed object keys defined in `COBJ.cs`. Common entries on containers:
  - `2`: `c_windowSaveData` (`Window.SaveData`; per-container UI prefs; getter at `Card.cs:1848-1858`)
  - `10`: `c_copyContainer` (`Thing` reference for deposit/copy boxes; getter at `Card.cs:1836-1846`)
  - `12`: `c_containerUpgrade` (`ContainerUpgrade` with `cap` and `cool`; getter at `Card.cs:1908-1918`)
  - The `c_containerUpgrade` getter lazy-creates a default `ContainerUpgrade` on read. The `c_windowSaveData` getter does NOT lazy-create (returns null if absent).
- **`Card.mapStr`** (`Dictionary<int, string>`, sparse) - String keys defined in `CSTR.cs`. Common entries on containers:
  - `1`: `c_altName` (player-renamed name)
  - `2`: `c_altName2`
  - `5`: `c_idRefCard` (chara reference ID stuffed in container, e.g. corpses)
  - `12`: `c_extraNameRef` (extra name suffix)
- **`Card.sockets`** (`List<int>`) - Weapon mod sockets for ranged weapons. Count = list length. Value: 0 = empty slot, otherwise `elementId * 1000 + encLv`.

### Chara Element Containers

- **`elements`** (`ElementContainerCard`, inherited from `Card`) - Main container for character stats (STR, MAG, END, etc.). Contains elements with `vBase`, `vSource`, `vLink` values. Overrides `ValueBonus()` to add faction bonuses and special calculations (lucky coin, machine bonuses, etc.).
- **`tempElements`** (`ElementContainer`) - Separate container for temporary modifiers (afflictions, ether diseases). Created lazily on first `ModTempElement()` call. Elements in this container have their own `vBase` values, separate from main `elements`. Linked to character via `SetParent(this)` on creation. Used for UI display of "Temporary Weakness -X" lines via `BonusInfo.WriteNote()`.
- **`faithElements`** (`ElementContainer`) - Container for faith-based bonuses. Created when character has a faith (`Chara.faith`) via `RefreshFaith()`. Elements are set via `SetBase()` based on piety value and faith's element map, then linked to character via `SetParent(this)`. Displayed in UI via `BonusInfo.WriteNote()` which calls `faithElements.Value(id)`. Can be null if character has no faith.
- **`workElements`** (`ElementContainer`) - Container for work/hobby bonuses from character's home branch. Created dynamically via `RefreshWorkElements()` when character joins/leaves a faction branch, during party joins/leaves, branch member refresh, game loading, and certain trait effects. Contains bonuses from hobbies and works at the branch, calculated based on efficiency. Elements are set via `ModBase()` and linked to character via `SetParent(parent)`, affecting character's `vLink`. Can be null if character has no home branch or is in PC party. **Note**: `RefreshWorkElements()` returns early if `IsPCParty || homeBranch == null || homeBranch.owner == null`, so PC party members never get work elements.
- **`baseWorkElements`** (`ElementContainer`, property) - Base work elements from all hobbies/works (not branch-specific). Lazy-initialized property that builds elements from `ListHobbies()` and `ListWorks()` via `ModBase()`. Used internally, not linked to character.
- **`body.slots`** (array of `BodySlot`) - Array representing equipped items. Each slot has `slot.thing.elements` containing the item's element contributions. UI reads directly from `body.slots` to display "Equipment +X", not from `vLink`. Equipment elements are linked to character's `vLink` via `SetParent(owner)` when equipped.
- **`conditions`** (List of `Condition`) - List of status effects. Each condition can have an `ElementContainer` via `GetElementContainer()` which links to character's `vLink` via `SetParent(owner)`. Displayed in UI via `BonusInfo.WriteNote()` which iterates conditions and sums their element contributions.
- **`corruption`** (int) - Stat that influences Ether Disease progression. Threshold = `corruption / 100`. When threshold crosses, `MutateRandom()` is called.
- **`EClass.pc.faction.charaElements`** (`ElementContainerFaction`) - Faction-based element container for PC faction members. Contains global elements from equipped items (items with `IsGlobalElement = true`). Added via `OnEquip()` when PC faction members equip items, removed via `OnUnequip()` when unequipped. Elements are added to this container via `ModBase()` when global equipment is equipped. **How it affects stats**: Added via `ElementContainer.ValueBonus()` → `faction.charaElements.Value(ele)` for PC faction members. Also added to `vLink` for display in "Base X + Y" in `Element._WriteNote()`: `num += faction.charaElements.Value(id)`. Included in `Element.Value` calculation: `Value = ValueWithoutLink + vLink + ValueBonus(owner)` where `ValueBonus()` returns `faction.charaElements.Value(ele)`. **Special behavior**: `OnEquip()`/`OnUnequip()` check `IsEffective(t)` to filter by deity alignment. Calls `CheckDirty()` to refresh all PC faction members when modified.

### Zone Containers

- **`EClass._zone.elements`** (`ElementContainerZone`) - Zone-based element container. Contains zone-specific elements (policies, techs, land feats). Overrides `OnLearn()` and `OnLevelUp()` for zone-specific messages. Used for zone-wide bonuses and unlocks.
- **`Faction.elements`** (`ElementContainerZone`) - Faction's zone container. Same as zone elements but for faction-wide effects.

## Element Value System

### Container Types

- **`ElementContainer`** (base class) - Base container class. `ValueBonus()` returns 0 by default. Uses `dict` (`Dictionary<int, Element>`) internally to store and retrieve elements.
- **`ElementContainerCard`** - Used for `Card.elements`. Overrides `ValueBonus()` to add:
  - Faction bonuses: `faction.charaElements.GetElement(e.id)` (null-checked, then `.Value`)
  - Lucky coin bonuses (for LUK)
  - Machine bonuses (for DV/PV/SPD)
  - Party-wide bonuses (for certain stats)
  - Multiplier bonuses (percentage-based)
- **`ElementContainerCondition`** - Used for condition element containers. Overrides `LimitLink = false` to allow unlimited linking (conditions can link to character without limit).
- **`ElementContainerFaction`** - Used for `faction.charaElements`. Handles global elements from equipment via `OnEquip()`/`OnUnequip()`. Tracks `isDirty` flag and refreshes all PC faction members when modified.
- **`ElementContainerZone`** - Used for zones and factions. Overrides `OnLearn()` and `OnLevelUp()` for zone-specific messages and logging.
- **`ElementContainerField`** - Used for field effects. Empty subclass (just inherits base functionality).

### Value Calculation

- **`Element.Value`** (int property) - Returns calculated total: `ValueWithoutLink + vLink + ((owner != null) ? owner.ValueBonus(this) : 0)`
  - `ValueWithoutLink = vBase + vSource`
  - `vLink` = bonuses from linked containers (equipment, conditions, workElements)
  - `ValueBonus(owner)` = additional bonuses from owner container (faction bonuses, lucky coin, machine bonuses, etc.)
- **`Element.ValueWithoutLink`** (int property) - Returns calculated value: `vBase + vSource`
  - Used in training cost calculations and experience gain checks
- **`ElementContainer.Value(int ele)`** - Gets calculated value for element ID from this container:
  - Gets element via `GetElement(ele)`
  - If element is null:
    - Returns 0 if game not started or card is not PC faction/minion
    - For PC faction/minion and `ele != 78` (LUK): Returns `faction.charaElements.Value(ele)` (faction fallback)
    - For `ele == 78` (LUK): Creates element and returns its `Value`
  - If element exists: Returns `element.Value`
- **`Element.GetSourceValue(long v, int lv, SourceValueType type)`** - Calculates scaled value based on type:
  - **`SourceValueType.Chara`**: Level-scaled with randomness: `v * (100 + (lv - 1 + random(lv/2 + 1)) * source.lvFactor / 10) / 100 + random(lv/3) * source.lvFactor / 100`
  - **`SourceValueType.Fixed`**: No scaling: `v`
  - **Other types** (Equipment, etc.): Encounter-scaled: `v * ((source.encFactor == 0) ? 100 : (50 + random(100) + random(sqrt(lv * 100)) * source.encFactor / 100)) / 100`
  - Used by `ApplyElementMap()` to calculate final value before adding to `vSource`
- **`Element.GetSourcePotential(int v)`** (virtual method) - Calculates potential value for skill elements:
  - Base implementation returns `0`
  - Can be overridden (e.g., `SKILL.GetSourcePotential()`)
  - Used by `ApplyElementMap()` for skill elements (`category == "skill"`): `vSourcePotential += GetSourcePotential(value) * num`

## Element Modification

### Core Functions

- **`ElementContainer.ModLink(int id, int v)`** (private method) - Modifies an element's `vLink` by amount `v`:
  - Gets/creates element: `GetOrCreateElement(id)`
  - Modifies: `orCreateElement.vLink += v`
  - Calls `orCreateElement.OnChangeValue()`
  - If parent container exists and linking is allowed (`!LimitLink || element.CanLink(this)`): Recursively calls `parent.ModLink(id, v)` to propagate change up the parent chain
  - Returns the modified element
  - **Used by**: `SetParent()` to link/unlink containers, `ModBase()` to propagate `vBase` changes
- **`ElementContainer.ModBase(int ele, int v)`** - Directly modifies `Element.vBase` in the container:
  - Gets/creates element: `GetOrCreateElement(ele)`
  - Modifies: `orCreateElement.vBase += v`
  - If parent container exists and element can link: Calls `parent.ModLink(ele, v)` to propagate change to parent's `vLink`
  - Calls `orCreateElement.CheckLevelBonus(this)` and `orCreateElement.OnChangeValue()`
  - If element becomes empty (`vBase == 0 && vSource == 0 && vLink == 0 && vPotential == 0 && vExp == 0`): Removes element via `Remove(id)`
  - **Context matters**:
    - `elements.ModBase()` = **permanent** change to main stats (e.g., feats affecting other elements, curses/blessings on items, training level-ups)
    - `tempElements.ModBase()` = **temporary** change (used by `ModTempElement()`, shown as "Temporary Weakness" in UI)
  - **Example**: When ether disease progresses, it calls `SetFeat()` which modifies main `elements.vBase` (permanent). But "Temporary Weakness -5" display comes from `tempElements.ModBase()` via `ModTempElement()`, which is separate.
- **`ElementContainer.SetBase(int id, int v, int potential = 0)`** - Directly sets `Element.vBase` to a specific value (doesn't add):
  - Gets/creates element: `GetOrCreateElement(id)`
  - If parent exists and element can link: Calls `parent.ModLink(id, -orCreateElement.vBase + v)` to update parent's `vLink` with the difference
  - Sets: `orCreateElement.vBase = v`, `orCreateElement.vExp = 0`, `orCreateElement.vPotential = potential`
  - Calls `orCreateElement.OnChangeValue()`
  - If element becomes empty: Removes element
  - Returns the modified element
  - **Used by**: `SetFeat()` to set feat's own `vBase`, `Learn()` to set initial ability value
- **`ElementContainer.SetParent(ElementContainer newParent = null)`** - Establishes parent-child relationship between containers:
  - **When removing parent** (if `parent != null`):
    - Iterates all elements in `dict.Values`
    - For each element that can link: Calls `parent.ModLink(value.id, -(value.vBase + value.vSource))` to remove contributions from old parent's `vLink`
  - **When setting new parent** (if `newParent != null`):
    - Iterates all elements in `dict.Values`
    - For each element that can link: Calls `newParent.ModLink(value2.id, value2.vBase + value2.vSource)` to add contributions to new parent's `vLink`
  - Sets `parent = newParent`
  - **Used by**: Equipment equipping/unequipping, condition adding/removing, workElements linking
- **`ElementContainer.GetOrCreateElement(int id)`** - Gets existing element or creates new one:
  - Tries to get element from `dict.TryGetValue(id, out value)`
  - If not found: Calls `CreateElement(id)` which creates `Element.Create(id)`, sets `element.owner = this`, and adds to `dict`
  - Returns the element (never null, but element itself may be null if `Element.Create()` fails)
  - **Overloads**: `GetOrCreateElement(string alias)`, `GetOrCreateElement(Element ele)`
- **`ElementContainer.GetElement(int id)`** - Gets element by ID from `dict`:
  - Returns `dict.TryGetValue(id)` (returns null if not found)
  - **Overloads**: `GetElement(string alias)` - converts alias to ID first

### Training / Experience

- **`ElementContainer.Train(int ele, int a = 10)`** - Initiates training for an element. Calls `OnTrain(ele)` (virtual hook, can be overridden), then calls `ModTempPotential(ele, a)` to increase temporary potential. Training itself doesn't directly increase `vBase` - it increases potential which affects experience gain rate.
- **`ElementContainer.ModTempPotential(int ele, int v, int threshMsg = 0)`** - Modifies temporary potential. Gets/creates element via `GetOrCreateElement(ele)`, modifies `orCreateElement.vTempPotential += v` (clamped to max 1000), then calls `OnModTempPotential(orCreateElement, v, threshMsg)` hook. Higher potential = faster experience gain (affects `ModExp` calculations).
- **`ElementContainer.ModExp(int ele, float a, bool chain = false)`** - Adds experience to an element:
  - **Early returns**: If `(Card != null && Card.isChara && Card.Chara.isDead) || a == 0f`, returns early. Gets element and checks `CanGainExp` (requires `ValueWithoutLink > 0`), returns if element is null or can't gain exp.
  - **Experience modifiers** (only applied when `a > 0f`):
    1. **Days together bonus**: If `!chain && Card != null && Card.isChara`, multiplies `a` by `GetDaysTogetherBonus() / 100` (bonus based on how long character has been in party)
    2. **UseExpMod formula**: If `element.UseExpMod` is true, applies formula: `a = a * Clamp(UsePotential ? Potential : 100, 10, 1000) / (100 + Max(0, ValueWithoutLink) * 25)` (higher potential = more exp, higher value = less exp). After calculation, probabilistically rounds up fractional part: `if (rndf(1f) < a % 1f) { a += 1f; }`
    3. **Parent factor**: If `!chain && element.source.parentFactor > 0f && Card != null && !element.source.aliasParent.IsEmpty()`, gets parent element via `element.GetParent(Card)`, checks `element2.CanGainExp`, then recursively calls `ModExp(parent.id, Clamp(a * parentFactor / 100f, 1f, 1000f), chain: true)` to grant experience to parent (prevents infinite loops via `chain: true`)
  - **Experience addition**: Adds to `element.vExp += (int)a` (truncates float to int)
  - **Level Up** (when `vExp >= ExpToNext`):
    1. Calculates overflow: `num = vExp - ExpToNext`
    2. Stores old `vBase` value
    3. **Calls `ModBase(ele, 1)`** - **This is the permanent training bonus!** Increases `vBase` by 1
    4. Calls `OnLevelUp(element, oldVBase)` hook (passes old vBase value)
    5. Sets `vExp = Clamp(num / 2, 0, ExpToNext / 2)` (carries over half of overflow)
    6. Reduces `vTempPotential` if positive: `vTempPotential -= vTempPotential / 4 + rnd(5) + 5` (decay toward 0, clamps to 0 if goes negative). If negative, increases toward 0 similarly: `vTempPotential += -vTempPotential / 4 + rnd(5) + 5` (clamps to 0 if goes positive)
  - **Level Down** (when `vExp < 0` and `ValueWithoutLink > 1`):
    1. If `ValueWithoutLink <= 1`, sets `vExp = 0` and returns early (prevents going below 1)
    2. Stores old `vBase` value
    3. Calls `ModBase(ele, -1)` - Decreases `vBase` by 1
    4. Calls `OnLevelDown(element, oldVBase)` hook (passes old vBase value)
    5. Sets `vExp = Max(ExpToNext / 2, ExpToNext + vExp)` (sets to halfway point)
- **`Element.ExpToNext`** (property) - Experience required for next level: Default `1000` (can be overridden by element source)
- **`Element.CostTrain`** (property) - Cost to train: `Max((ValueWithoutLink / 10 + 5) * (100 + vTempPotential) / 500, 1)`. Higher `ValueWithoutLink` = more expensive. Higher `vTempPotential` = more expensive.
- **`ElementContainer.Learn(int ele, int v = 1)`** - Learns a new element/ability. Directly calls `ModBase(ele, v)` to set initial `vBase` value, then calls `OnLearn(ele)` hook. Used for learning new abilities/spells, not training existing ones. This is separate from the training system - `Learn()` directly sets `vBase`, while training increases `vBase` gradually via experience.

### Temporary Modifiers

- **`Chara.ModTempElement(int ele, int a, bool naturalDecay = false, bool onlyRenew = false)`** - Modifies elements within `tempElements`. Used for afflictions, temporary stat changes, etc.
  - **Early return**: If `a < 0` and `!naturalDecay` and character has "sustain\_" element for this stat, returns early (sustain prevents negative temp modifiers, but natural decay bypasses sustain)
  - **Container creation**: If `tempElements == null`, creates new `ElementContainer()` and calls `tempElements.SetParent(this)` to link it to character
  - **Feat 1215 bonus**: If character has element 1215 and `a > 0`, multiplies `a` by 150/100 (50% bonus to positive temp modifiers)
  - **Clamping logic**: Calculates limits based on `elements.ValueWithoutLink(ele)`:
    - Positive limit: `num3 = (Mathf.Abs(ValueWithoutLink) + 100) / (hasFeat1215 ? 2 : 4)`
    - Negative limit: `num4 = -ValueWithoutLink - 100`
    - If `onlyRenew=true`: Adjusts limits (`num3 = Min(a, num3)`, `num4 = Max(a, -num2 / 3)`)
    - Clamps `a` before calling `ModBase()`: If `a > 0 && (tempBase + a) > num3`, reduces `a` to fit. If `a < 0 && (tempBase + a) < num4`, increases `a` to fit.
  - **Modification**: Calls `tempElements.ModBase(ele, a)` to modify `vBase` in the `tempElements` container
  - **Cleanup**: If element's `vBase` becomes 0 after modification, removes element from `tempElements`. If `tempElements` becomes empty, sets `tempElements = null`.
  - **Note**: This is what creates the "Temporary Weakness -X" display in the UI. If disease progression makes it worse, it would call `ModTempElement()` again with a larger negative value.

## Character Progression

### Leveling / Feat Points

- **`Card.LV`** (int property) - Character level. Stored in `_ints[25]`.
- **`Card.exp`** (int property) - Character experience points toward next level. Stored in `_ints[26]`.
- **`Card.feat`** (int property) - Unspent feat points. Stored in `_ints[24]`. Incremented by 1 each `LevelUp()` call.
- **`Player.totalFeat`** (int field, serialized) - Total feat points ever earned by PC through leveling. Incremented by 1 each time `Card.LevelUp()` is called on the PC. Represents lifetime count, not unspent count. Used in the mod to calculate target level: `targetLevel = totalFeat + 1`.
- **`Card.LevelUp()`** - Levels up the character:
  1. If PC and demo mode with `totalFeat >= 5`: blocks and returns
  2. If PC: increments `Player.totalFeat++`
  3. Always: `feat++` and `LV++`
  4. Plays level-up message, sound, and effect
  5. Auto-upgrades certain feats based on LV thresholds (feat 1415 for all characters, feat 1274 for PC only, feat 1644 for NPC mutants)
  - **Note**: Does NOT directly grant HP, change stats, or modify elements — it only grants 1 feat point and increments LV. HP increase comes from `MaxHP` being a formula that includes `LV`.

### Vital Stats (HP / Mana / Stamina)

- **`Card.hp`** (int property) - Current hit points. Stored in `_ints[14]`.
- **`Chara.MaxHP`** (int property, override) - Calculated on-demand:
  - Formula: `((END*2 + STR + WIL/2) * Min(LV, 25)/25 + END + 10) * Evalue(60)/100 * rarityOrSoloBonus/100`
  - `Evalue(60)` = "Life" element (percentage multiplier, default 100)
  - Non-PC-faction: bonus from rarity (`100 + rarity * 300`)
  - PC: bonus from `lastEmptyAlly * Evalue(1646)` (solo/loneliness feat)
  - Clamped between 1 and 1 billion
- **`Chara.mana`** (Stats property) - Mana resource. Flyweight accessor backed by `_cints[16]`. Has `.value` and `.max`.
  - **`StatsMana.max`**: `((MAG*2 + WIL + LER/2) * Min(LV, 25)/25 + MAG + 10) * (Evalue(61) - Evalue(93))/100 * rarityOrSoloBonus/100`
  - `Evalue(61)` = "Mana" element, `Evalue(93)` subtracted from it
  - Non-PC-faction: rarity bonus is `100 + rarity * 250` (differs from MaxHP's `* 300`)
  - Clamped between 1 and 100,000,000 (100M, not 1B like MaxHP)
- **`Chara.stamina`** (Stats property) - Stamina resource. Flyweight accessor backed by `_cints[12]`. Has `.value` and `.max`.
  - **`StatsStamina.max`**: `_maxStamina * Evalue(62) / 100`
  - `Evalue(62)` = "Stamina" element (percentage multiplier)
- **`Chara.CalculateMaxStamina()`** - Recalculates `_maxStamina` field:
  1. Starts with `END` (Endurance attribute value)
  2. Sums all "skill" category elements: PC uses `vBase`, non-PC uses `ValueWithoutLink`
  3. Applies `EClass.curve(sum, 30, 10, 60)` (diminishing returns curve)
  4. Floor of 10, then adds 15. Result stored in `_maxStamina`
  - Called during `OnCreate`, `SetLv`, and after `SetFeat` (via explicit call in the stat import flow)

### Feats / Traits

- **`Chara.SetFeat(int id, int value = 1, bool msg = false)`** - Sets a feat/trait:
  1. Gets existing feat: `elements.GetElement(id) as Feat`
  2. If feat exists and `feat.Value > 0`:
     - **Early return**: If `value == feat.Value`, returns immediately (no-op)
     - Calls `feat.Apply(-feat.Value, elements)` to remove old effects (reverses previous modifications)
  3. Sets feat's own `vBase` via `elements.SetBase(id, value - (feat?.vSource ?? 0))` - stores only the delta above race/job so that `Value` = `vBase + vSource` = desired total tier. There is no separate "demote" API; use `SetFeat(id, lowerValue)` to downgrade.
  4. If `feat.Value != 0`: Calls `feat.Apply(feat.Value, elements)` to apply new effects
  5. Calls `Refresh()` and `CalculateMaxStamina()` if game is started
  6. If `msg=true`: Displays message about gaining/changing feat, then calls `elements.CheckSkillActions()`
- **Feat tier and cost (disassembly)**: `SourceElement.Row.cost` is `int[]` with per-tier cost: `cost[0]` = tier 1, `cost[1]` = tier 2, etc. `FEAT.CostLearn` returns `source.cost.TryGet(Value - 1)` (index by current tier). For multi-ID tier chains (e.g. mutations), `aliasParent` points from **child to parent**; `SetMutation()` clears the parent feat before setting the child. No predefined tier-order list; hierarchy is from `aliasParent` traversal.
- **`Feat.Apply(int a, ElementContainer owner, bool hint = false)`** - Applies a feat's effects (returns `List<string>`):
  - **Mod event**: Publishes `"elin.feat.apply"` via `BaseModManager.PublishEvent()` before applying effects, allowing other mods to intercept and modify the `a` parameter
  - **Important**: This modifies OTHER elements' `vBase` via `owner.ModBase()`, NOT the feat's own `vBase`
  - If `!hint && a > 0 && owner.Chara != null`: Sets feat's own `vPotential = owner.Chara.LV` (feat's potential, not other elements)
  - Switches on feat ID to apply specific effects:
    - Most feats call `ModBase(ele, value, hide: false)` which calls `owner.ModBase(ele, value)` to modify OTHER elements' `vBase`
    - Some feats call `ModAttribute(ele)` which calls `ModBase()` and `ModPotential()` on OTHER elements
    - Some feats only set `featRef[]` strings for UI display (no stat modifications)
  - Example: Feat ID 1610 calls `ModBase(60, a * 4, hide: false)` → modifies element 60's `vBase` (STR), not the feat's `vBase`
  - **Note**: The feat's own value is stored in its `vBase` (set by `SetFeat()`), but `Apply()` modifies OTHER elements' `vBase`

### Mutations / Ether Diseases

- **`Chara.MutateRandom(int vec = 0, int tries = 100, bool ether = false, BlessedState state = BlessedState.Normal)`** - Applies or removes random mutations:
  - **Early return**: If `!ether && vec >= 0 && HasElement(406)` (resistance), 80% chance to resist and return false
  - **Mutation selection**: Filters elements by category (`ether ? "ether" : "mutation"`) and excludes those with "noRandomMutation" tag
  - **Removal logic** (when `vec < 0 && ether`): On first try (`i == 0`), if `c_corruptionHistory != null && Count > 0`, uses `c_corruptionHistory.LastItem()` to get most recent mutation ID and removes it from list (LIFO stack behavior)
  - **Skip conditions**:
    - When `vec > 0`: Skips mutation 1563 if `corruption < 300`; skips mutation 1562 if `corruption < 1000` and `IsPowerful`; skips if element already at `row.max`
    - When `vec < 0`: Skips if element doesn't exist or `element.Value <= 0`
  - **BlessedState filtering**: When `vec > 0` and state is Blessed, negative mutations are skipped; when Cursed, positive mutations are skipped (and vice versa for `vec < 0`)
  - **aliasParent fallback**: If selected mutation element doesn't exist on character but has a non-empty `aliasParent` and character has the parent element, switches to the parent mutation row instead
  - **Value calculation**:
    - If element is null (new mutation): `num = 1`
    - If element exists: `num = element.Value + vec` (or random ±1 if `vec == 0`)
    - Clamps `num` to `element.source.max - 1` if `num` strictly exceeds `source.max`
  - **Mutation application**: Calls `SetFeat(row.id, num)` to apply/update the mutation feat
  - **Tracking** (when `flag2 && ether`, where `flag2` = mutation tier increased): Creates `c_corruptionHistory` list if null, adds `row.id` to track mutation order. Note: `flag2` defaults to `true` for new mutations and is set to `num > element.Value` for existing ones, so in practice only tracks when mutation tier goes up.
  - Returns `true` if mutation was applied, `false` if nothing happened after all tries
- **`Chara.ModCorruption(int a)`** - Orchestrator for Ether Disease progression/curing:
  - **Early return**: If `a > 0` and character has high ether resistance (`Evalue(962) >= 25` or `ResistLv(962) > 0` with random chance), returns early
  - **Resistance reduction**: If `a > 0` and character has element 412, reduces `a` using float division with probabilistic rounding: `float num2 = (float)a * 100f / (float)Max(100 + element412 * 2, 10); a = (int)num2 + (rndf(1f) < num2 % 1f ? 1 : 0)`
  - **Threshold calculation**: `num3 = (corruption + a) / 100 - corruption / 100` (number of thresholds crossed)
  - **Mutation triggers**: For each threshold crossed (`Mathf.Abs(num3)` times), calls `MutateRandom((num3 > 0) ? 1 : (-1), 100, ether: true)`
    - Positive threshold = adds mutation (`vec = 1`)
    - Negative threshold = removes mutation (`vec = -1`, uses `c_corruptionHistory` LIFO)
  - **Corruption update**: After mutations, updates `corruption += a`
  - **Ether sum recalculation**: Calculates sum of all ether elements (`category == "ether"`), then **sets `corruption = etherSum * 100 + corruption % 100`** (overwrites corruption with ether sum \* 100 + remainder)
- **Curing Ether Disease**:
  - **Method 1**: Call `ModCorruption(-100000)` which triggers `MutateRandom(vec: -1, ether: true)` to remove mutations via `c_corruptionHistory` LIFO stack
  - **Method 2**: `CoreDebug.Fix_EtherDisease()` - Debug function that:
    1. Calls `ModCorruption(-100000)` to reset corruption and remove mutations
    2. Creates fresh character via `CharaGen.Create("chara")` with same race/job (`ChangeRace()`, `ChangeJob()`)
    3. Compares fresh character's `vBase` with current character's `vBase` for attribute elements (`category == "attribute"`)
    4. If fresh `vBase > current vBase` (current is damaged), fixes it via `elements.ModBase(id, value.vBase - orCreateElement.vBase + 1)` - adds +1 extra to ensure it's at least as high as fresh character
  - **Temporary modifiers**: `ModTempElement()` with negative values reduces temporary stat penalties. If `vBase` reaches 0, element is removed from `tempElements`. Temporary modifiers are separate from permanent mutations - mutations modify main `elements.vBase` via `SetFeat()`, while temp modifiers use `tempElements.vBase`.

### Race / Job Changes

- **`Chara.ChangeRace(string idNew)`** - Changes character race mid-run:
  1. Calls `ApplyRace(remove: true)` to remove old race bonuses (inverts element map)
  2. Sets `Card.c_idRace = idNew` and nulls `Chara._race` cache
  3. Calls `ApplyRace()` to apply new race bonuses
  4. Calls `ChangeMaterial()` for race material changes
- **`Chara.ChangeJob(string idNew)`** - Changes character job mid-run:
  1. Calls `ApplyJob(remove: true)` to remove old job bonuses (inverts element map)
  2. Sets `Card.c_idJob = idNew` and nulls `Chara._job` cache
  3. Calls `ApplyJob()` to apply new job bonuses
  4. Updates PCC uniforms if `IsPCC`
- **`Chara.ApplyRace(bool remove = false)`** - Applies/removes race bonuses:
  - Parses `race.figure` string and updates body parts via `body.AddBodyPart()`/`RemoveBodyPart()`
  - Calls `body.RefreshBodyParts()` to update body structure
  - Calls `elements.ApplyElementMap(uid, SourceValueType.Chara, race.elementMap, DefaultLV, remove, applyFeat: true)` - **modifies `vSource`**
  - Special case: If race is "bike" and not "bike_cub", calls `SetFeat(1423, value)`
- **`Chara.ApplyJob(bool remove = false)`** - Applies/removes job bonuses:
  - Calls `elements.ApplyElementMap(uid, SourceValueType.Chara, job.elementMap, DefaultLV, remove, applyFeat: true)` - **modifies `vSource`**
  - If `IsPCC`: Calls `EClass.game.uniforms.Apply()` for PCC uniforms
- **`ElementContainer.ApplyElementMap(int uid, SourceValueType type, Dictionary<int, int> map, int lv, bool invert = false, bool applyFeat = false)`** - Core function for applying element maps (race, job, material):
  - Sets random seed to `uid` for deterministic calculations
  - Iterates through `map` (element ID → value pairs). For feats, the value is the **tier level** (1, 2, 3) granted by race/job; for skills/attributes it is the base multiplier used in `GetSourceValue()`/`GetSourcePotential()`.
  - For each element:
    1. Gets or creates element via `GetOrCreateElement(item.Key)`
    2. If element category is "skill": Modifies `vSourcePotential` via `GetSourcePotential(value) * num`
    3. Calculates final value via `GetSourceValue(value, lv, type)` (level-scaled for Chara, encounter-scaled for others, fixed for Fixed)
    4. **Modifies `Element.vSource`**: `long num2 = GetSourceValue(value, lv, type) * num` (where `num` is the invert multiplier ±1), then `orCreateElement.vSource += (int)num2` (clamped to 99999999)
    5. If `applyFeat=true` and element is a `Feat`: Calls `Feat.Apply((int)num2, this)` which modifies OTHER elements' `vBase` via `ModBase()`
  - Resets random seed
  - **Critical**: Race/Job bonuses modify `Element.vSource` for ALL elements in the map. The feat's own value goes to `vSource`. If `applyFeat=true`, `Feat.Apply()` is called AFTER modifying `vSource`, which then modifies OTHER elements' `vBase` (not the feat's own `vBase`).
- **`Feat.Apply(int a, ElementContainer owner, bool hint = false)`** - Applies feat effects (called from `ApplyElementMap` or `SetFeat`), returns `List<string>`:
  - **Mod event**: Publishes `"elin.feat.apply"` via `BaseModManager.PublishEvent()` before applying effects, allowing other mods to intercept and modify the `a` parameter
  - **Important**: This modifies OTHER elements' `vBase`, NOT the feat's own `vBase`
  - If `hint=false` and `a > 0`: Sets `vPotential = owner.Chara.LV`
  - Switches on feat ID to apply specific effects via `ModBase()` on OTHER elements
  - Example: Feat ID 1610 calls `ModBase(60, a * 4)` - modifies element 60's `vBase`, not the feat's `vBase`
- **`Card.ChangeMaterial(SourceMaterial.Row row, bool ignoreFixedMaterial = false)`** - Changes character material:
  - Calls `ApplyMaterial(remove: true)` to remove old material bonuses
  - Sets `_material = row`, `idMaterial = row.id`, `decay = 0`
  - Marks `dirtyWeight = true` and calls `SetDirtyWeight()` if PC
  - Calls `ApplyMaterial()` to apply new material bonuses via `ApplyMaterialElementMap()` → `ApplyElementMap()`
- **`Chara.InitStats(bool onDeserialize = false)`** - Initial stat setup:
  - If not deserializing: Sets initial `_cints` values (stat constants like hunger, stamina thresholds)
  - Iterates conditions calling `condition.SetOwner(this, onDeserialize)` to re-link condition elements
  - **Note**: Does NOT call `ApplyRace()` or `ApplyJob()` — those are called separately during character creation
- **`Player.RefreshDomain()`** - Rebuilds `domains` list from `EClass.pc.job.domain[]` array (stride-2 pairs of `[elementId, value, ...]`). Should be called after `ChangeRace()`/`ChangeJob()` to sync the player's domain skills with their new job.

## Equipment & Items

### Equip / Unequip

- **`CharaBody.Equip(Thing thing, BodySlot slot = null, bool msg = true)`** - Equips an item:
  1. Finds appropriate slot via `GetSlot(thing.category.slot)` (or `GetSlot(thing.category.slot, onlyEmpty: false)` if no empty slot)
  2. Validates slot matches `thing.category.slot` and checks `IsEquippable()`
  3. If slot already has item: Unequips existing item first (calls `Unequip(slot, refresh: false)`)
  4. Unequips `thing` from any other slot if already equipped (calls `Unequip(thing, refresh: false)`)
  5. If `thing.parent != owner`: Calls `owner.AddCard(thing)` to transfer ownership
  6. Sets `slot.thing = thing` and `thing.c_equippedSlot = slot.index + 1`
  7. **Critical**: Calls `thing.elements.SetParent(owner)` - this links equipment elements to character via `ModLink()`
  8. Calls `thing.trait.OnEquip(owner, onSetOwner: false)` for trait effects
  9. If `EClass.pc != null`: Calls `faction.charaElements.OnEquip(owner, thing)` for faction bonuses (global elements only) — note: triggers for any character's equipment change, not just PC faction members
  10. Calls `owner.SetTempHand()`, `owner.Refresh()` if character is created, and UI refresh if PC
- **`CharaBody.Unequip(BodySlot slot, bool refresh = true)`** - Unequips an item:
  1. Gets `thing` from `slot.thing` (returns early if null)
  2. If `EClass.pc != null`: Calls `faction.charaElements.OnUnequip(owner, thing)` to remove faction bonuses — note: triggers for any character, not just PC faction members
  3. **Critical**: Calls `thing.elements.SetParent()` (no parent) - this unlinks equipment elements via `ModLink()` with negative values
  4. Calls `thing.trait.OnUnequip(owner)`
  5. Sets `thing.c_equippedSlot = 0` and `slot.thing = null`
  6. Calls `owner.Refresh()` and UI refresh if needed
- **`ElementContainer.SetParent(ElementContainer newParent = null)`** - Establishes parent-child relationship:
  - **When removing old parent** (if `parent != null`): For each element, if `!LimitLink || value.CanLink(this)`, calls `parent.ModLink(value.id, -(value.vBase + value.vSource))` - removes old contributions
  - **When setting new parent** (if `newParent != null`): For each element, if `!LimitLink || value2.CanLink(this)`, calls `newParent.ModLink(value2.id, value2.vBase + value2.vSource)` - adds new contributions
  - **Result**: Equipment bonuses modify `Element.vLink` in the character's main `elements` container via `ModLink()`
  - **Note**: Uses `vBase + vSource` (total element value), not just `vBase`
- **`Element.Value` calculation**: `Value = ValueWithoutLink + vLink + ValueBonus(owner)`
  - Equipment bonuses contribute via `vLink` (from `SetParent` → `ModLink`)
  - `ValueBonus` is used for faction bonuses and special cases (returns 0 for base `ElementContainer`)
- **Faction Equipment Bonuses** (`ElementContainerFaction.OnEquip(Thing t)`):
  - Checks `IsEffective(t)` (deity matching)
  - For each element in equipment: If `value.IsGlobalElement`, calls `ModBase(value.id, value.Value)` on faction container
  - Also sets `vExp = value.vExp` to preserve experience
  - Sets `isDirty = true` and calls `CheckDirty()` to refresh all PC faction characters
  - Global elements from equipment are added to faction's `vBase` directly (separate from parent-child linking mechanism)

### Item Material Elements

- **`Card.ApplyMaterialElements(bool remove)`** - Base class is a no-op. Overridden by `Thing`:
  - If item is equipped: temporarily detaches parent link via `elements.SetParent()`, applies material element map, then reattaches via `elements.SetParent(chara)` — prevents spurious `vLink` propagation during modification
  - Delegates to `ElementContainer.ApplyMaterialElementMap(this, remove)` which iterates `material.elementMap` and modifies `vSource` on the Thing's elements
  - **Separate from `ApplyMaterial()`**: `ApplyMaterial()` handles the full material change (DV/PV/damage, fireproof, etc.) and calls `ApplyMaterialElements` internally. `ApplyMaterialElements` handles only the material's element bonuses.
  - **During deserialization**: Called at `Card._OnDeserialized` with `remove: false` to restore material element bonuses after loading
  - **Used by mod**: Called during item restoration to apply material `vSource` bonuses after setting `vBase` from exported data

### Body Parts

- **`CharaBody.AddBodyPart(int ele, Thing thing = null)`** - Creates a new `BodySlot` and appends to `slots`:
  - Sets `.elementId = ele`, `.index = slots.Count`
  - If element 35 (hand): assigns to `slotMainHand` first, then `slotOffHand`
  - If element 41 (ranged): assigns to `slotRange`
  - Does NOT trigger equip refresh or element modification — just adds the slot
- **`CharaBody.RefreshBodyParts()`** - Rebuilds internal slot references (main hand, off hand, range) from the current `slots` list. Should be called after adding/removing body parts.

## Inventory Containers

### `ThingContainer`

- **`Card.things`** (`ThingContainer`, inherits `List<Thing>`) - Container's children plus grid metadata. Constructed once per Card; grid dimensions exposed via `width`/`height` int fields.
- **`ThingContainer.SetOwner(Card owner)`** at `ThingContainer.cs:69-79` - Sets `owner`, then `width = c_containerSize / 100`, `height = c_containerSize % 100`. If `width == 0`, defaults to 8x5. Called once during `Card.Create` (transitively from `ThingGen.Create`, when `c_containerSize` is still empty), and again in `Card._OnDeserialized` and in `Card.Duplicate` at `Card.cs:3559-3565` (the latter is guarded by `if (thing.c_containerSize != 0)`). Modifying `c_containerSize` later does NOT auto-resize the live container; `SetOwner` must be re-run.
- **`ThingContainer.SetSize(int w, int h)`** at line 366 - Writes `owner.c_containerSize = w * 100 + h`, then re-runs `SetOwner(owner)` (which re-derives `width`/`height` from the new value). Use this when changing size at runtime. Does NOT call `RefreshGrid()`.
- **`ThingContainer.ChangeSize(int w, int h)`** at lines 81-88 - Sets `width`/`height` directly, writes `c_containerSize = w * 100 + h`, then calls `RefreshGrid()`. Used when the visible grid layout must redraw.
- **`ThingContainer.GridSize`** => `width * height` (line 48).
- **`ThingContainer.IsMagicChest`** => `owner.trait is TraitMagicChest` (line 54).
- **`ThingContainer.MaxCapacity`** at lines 57-67 - Returns `100 + owner.c_containerUpgrade.cap` for magic chests, else `GridSize`. Magic chests do not write `c_containerSize`; their capacity scales from the upgrade row instead.

### `ContainerUpgrade`

- **`ContainerUpgrade.cs`** - Two `[JsonProperty]` ints: `cap` (storage wrench bonus, +20 per stack) and `cool` (fridge wrench flag, 0 or 1). Stored in `mapObj[12]`.
- **`TraitMagicChest.IsFridge`** at `TraitMagicChest.cs:23` => `owner.c_containerUpgrade.cool > 0`.
- **`TraitMagicChest.OriginalElectricity`** at line 3 => `base + ((IsFridge ? 50 : 0) + cap / 5) * -1` (electricity cost scales with cap and fridge state).

### `Window.SaveData` (per-container UI prefs)

- Defined inside `Window` at `Plugins.UI/Window.cs:98-592`. Marked `[JsonObject(MemberSerialization.OptIn)]`.
- Persisted fields: `int[20] ints` (window position, anchors, autodump enum, sharedType, ContainerFlag, columns, customAnchor, category mode, sortMode, color, priority, etc.), `HashSet<int> cats` (selected category IDs), `string filter` (text filter).
- Hidden `BitArray32 b1` field is NOT `[JsonProperty]`, but rides along: `[OnSerializing]` at line 562 packs `b1.Bits` into `ints[0]`; `[OnDeserialized]` at line 568 unpacks it back. Newtonsoft.Json honors these `System.Runtime.Serialization` callbacks. Round-trip via `JsonConvert.SerializeObject`/`DeserializeObject<Window.SaveData>` is lossless for `b1`'s booleans (open, useBG, excludeDump, excludeCraft, alwaysSort, sort_ascending, etc.).
- **`Card.GetWindowSaveData()`** at `Card.cs:2557-2569` - Returns `Window.dictData["LayerInventoryFloatMain0"]` if `IsPC`, `Window.dictData["ChestMerchant"]` if `trait is TraitChestMerchant`, else `c_windowSaveData` from `mapObj[2]`. So PC main-inventory and merchant-chest UI prefs live in a static `Window.dictData`, NOT on the Card. All other containers (chests, backpacks, bank, shipping, delivery, toolbelt) read from their own `c_windowSaveData`.
- **Lazy-init**: `LayerInventory.CreateContainerPC` and `CreateContainer` (`LayerInventory.cs:455-478`, `:570-581`) lazy-create `c_windowSaveData` on first open if null. Special default: shipping chest gets `autodump = AutodumpFlag.none`. Pre-populated `c_windowSaveData` skips the lazy-init branch entirely.

### Container Trait Taxonomy

| Trait | Extends | `IsContainer` | `IsSpecialContainer` |
|---|---|---|---|
| `TraitBaseContainer` | `Trait` | `true` (override at line 71) | `false` |
| `TraitContainer` | `TraitBaseContainer` | inherited | `false` |
| `TraitMagicChest` | `TraitContainer` | inherited | `true` |
| `TraitDeposit` (bank) | `TraitContainer` | inherited | `true` |
| `TraitShippingChest` | `TraitContainer` | inherited | `true` |
| `TraitDeliveryChest` | `TraitContainer` | inherited | `true` |
| `TraitToolBelt` | `TraitContainer` | inherited | `false` |
| `TraitChestMerchant` | `TraitContainer` | inherited | `false` |

`Card.IsContainer => trait.IsContainer` (`Card.cs:2129`). `IsSpecialContainer` gates the rename UI (`UIInventory.cs:695` hides changeName button when `IsSpecialContainer && !IsMagicChest`), the wrench grid-extend, and a few auto-dump rules.

### Wrench Upgrade Gating

`TraitWrench.IsValidTarget(Thing t)` at `TraitWrench.cs:7-59` requires `t.IsInstalled` first (so equipped or carried containers are unreachable), then by wrench ID:

- `storage` and `fridge`: target must be `TraitMagicChest`. `storage` adds 20 to `c_containerUpgrade.cap`. `fridge` sets `c_containerUpgrade.cool = 1` AND `t.elements.SetBase(405, 50)` (electricity element). `fridge` is one-shot (refuses to apply twice).
- `extend_v` and `extend_h`: rejects `TraitMagicChest`, `TraitDeliveryChest`, and any `IsSpecialContainer`. Targets regular `TraitContainer` only. The upgrade is **single-shot per axis**: `Upgrade()` refuses unless `t.things.height == traitContainer.Height` (or `width == Width` for `extend_h`), so once `SetSize` grows the dimension by 1, a second wrench fails the equality gate.
- `bed`: `TraitBed`; increments `c_containerSize` directly (debug mode adds 1000 for testing).
- `tent_seabed`, `tent_soil`, `tent_elec`: target `TraitTent`; environment-checked.

Implication: bank, shipping, delivery, and toolbelt cannot be wrench-modified through normal play. Bank is rejected by `IsSpecialContainer`. Shipping is rejected by `IsSpecialContainer`. Delivery is rejected explicitly AND by `IsSpecialContainer`. Toolbelt is equipped (not installed). Their `c_containerUpgrade` is always default-zero unless modded.

### Singleton Containers

`CardManager` holds three `Thing` singleton fields (`CardManager.cs:52-58`), spawned in `Game._Create` (`Game.cs:799-801`) before `Game.StartNewGame` runs:

- **`container_shipping`** (id `"container_shipping"`, `TraitShippingChest`) - Outgoing daily-revenue chest. `GameDate.cs:282+` (`ShipGoods`) processes daily ship.
- **`container_deliver`** (id `"container_delivery"` despite the field name; `TraitDeliveryChest`) - Incoming mail/orders. `GameDate.cs:418+` (`ShipPackages`) drains daily.
- **`container_deposit`** (id `"container_deposit"`, `TraitDeposit`) - Bank vault for deposit money and items.

Plus a per-character toolbelt:

- **Toolbelt** - Reachable via `body.slots` filter; `id == "toolbelt"`, `TraitToolBelt`. Spawned with the character. Despite `IsSpecialContainer == false`, it cannot be wrench-extended because it is equipped (not installed) and `IsValidTarget` requires `IsInstalled`.

Plus a per-faction-branch stash:

- **`FactionBranch.stash`** (`FactionBranch.cs:85`) - `container_salary` Thing, created in `OnCreate(Zone)` per branch. Lives with the branch.

Singletons whose Card is not respawned across runs need their `mapObj`/`mapStr` settings copied directly. They do not flow through normal Thing-export paths because the Card on the destination side already exists.

## Conditions & Curing

### Condition Access

- **`BaseCondition.GetElementContainer()`** (virtual method) - Returns `ElementContainer` for condition's stat contributions:
  - Base implementation returns `elements` (the condition's `ElementContainerCondition`)
  - Can be overridden (e.g., `ConDisease.GetElementContainer()` returns a different container)
  - Returns `null` if condition has no element contributions (`UseElements = false`)
  - **Used by**: `BonusInfo.WriteNote()` to get condition contributions, `WidgetStatsBar` to calculate stat display
- **`Chara.HasCondition<T>()`** - Checks if character has a condition of type `T`:
  - Iterates through `conditions` list
  - Returns `true` if any condition is of type `T`, `false` otherwise
- **`Chara.GetCondition<T>()`** - Gets condition of type `T` from character:
  - Iterates through `conditions` list
  - Returns first condition of type `T`, or `null` if not found
- **`Chara.RemoveCondition<T>()`** - Removes condition of type `T`:
  - Finds condition in `conditions` list
  - Calls `condition.Kill()` to remove it

### Condition Lifecycle

- **`Chara.AddCondition<T>(int p = 100, bool force = false)`** - Adds a condition by type. Calls `AddCondition(typeof(T).Name, p, force)`.
- **`Chara.AddCondition(string id, int p = 100, bool force = false)`** - Adds a condition by ID. Creates condition via `Condition.Create(id, p)` then calls `AddCondition(Condition c, force)`.
- **`Chara.AddCondition(Condition c, bool force = false)`** - Adds a condition object:
  1. Sets `c.owner = this`
  2. Checks resistances and applies reduction if needed
  3. Calls `c.power = c.EvaluatePower(c.power)` — returns null if power becomes 0
  4. Checks negate list (conditions that prevent this condition)
  5. Calculates duration via `c.EvaluateTurn(c.power)`
  6. Handles stacking/overriding if condition already exists
  7. Checks `TryNullify(c)` against all existing conditions — returns null if any existing condition nullifies the new one
  8. Adds to `conditions` list; calls `AddResistCon(c)` if `CanGainConResist`
  9. Calls `c.SetOwner(this)` which creates `ElementContainerCondition` if `UseElements` is true, sets elements via `SetBase()`, then **calls `elements.SetParent(owner)`** - this links condition's elements to character's `vLink`
  10. Calls `c.Start()` (which calls `OnBeforeStart()`, `SetPhase()`, `OnStart()`, `OnStartOrStack()`, `PlayEffect()`)
  11. Calls `SetDirtySpeed()`, then `owner.Refresh()` if `c.ShouldRefresh`
  12. If `c.CancelAI`: calls `ai.Cancel()`. If `c.ConsumeTurn` and IsPC: calls `EClass.player.EndTurn()`
  13. If `c.SyncRide` and character has ride/parasite: adds same condition to ride and parasite
- **`Chara.RemoveCondition<T>()`** - Removes condition by type. Finds condition in `conditions` list and calls `condition.Kill()`.
- **`Condition.Kill(bool silent = false)`** - Removes condition:
  1. Sets `value = 0` and removes from `owner.conditions` list
  2. Plays end effect and message (if not silent)
  3. **If `elements != null`: Calls `elements.SetParent()` (no parent)** - this unlinks condition's elements from character's `vLink`
  4. Calls `OnRemoved()` hook and refreshes emo icon
  5. Calls `owner.SetDirtySpeed()`, then `owner.Refresh()` if `ShouldRefresh`
- **`Chara.TickConditions()`** - Called each turn to update conditions. Iterates `conditions` list and calls `condition.Tick()` for each.
- **`Condition.Tick()`** - Default implementation calls `Mod(-1)` to decrease `value` by 1 each turn. When `value <= 0`, `OnValueChanged()` calls `Kill()` to remove condition.
- **Condition element contributions**: Conditions work similarly to equipment - when added with `UseElements = true`, they create `ElementContainerCondition`, set elements via `SetBase()`, and link to character's `vLink` via `SetParent(owner)`. When removed, they unlink via `SetParent()` (no parent). Their contributions are separate from `tempElements` - conditions have their own container system.

### Curing / Healing

- **`Chara.Cure(CureType type, int p = 100, BlessedState state = BlessedState.Normal)`** - Cures conditions based on type:
  - **`CureType.Heal` / `CureType.Prayer`**: Calls `CureCondition<T>(threshold)` for Fear, Blind, Poison, Confuse, Dim, Bleed with power-scaled thresholds. If blessed, reduces SAN by 15. Does NOT call `CureTempElements`.
  - **`CureType.CureBody`**: Cures Blind, Poison, Bleed with higher thresholds. Calls `CureTempElements(p, body: true, mind: false)`.
  - **`CureType.CureMind`**: Cures Fear, Dim. Calls `CureTempElements(p, body: false, mind: true)`.
  - **`CureType.HealComplete` / `Death` / `Jure` / `Boss` / `Unicorn`**: Calls `CureTempElements(p * 100, body: true, mind: true)`. Iterates all conditions, `Kill()`s Bad/Debuff/Disease types. Skips Anorexia unless Death/Unicorn/Jure.
- **`Chara.CureCondition<T>(int v = 99999)`** - Subtracts `v` from `condition.value`; calls `Kill()` only if value drops to 0 or below.
- **`Chara.CureTempElements(int p, bool body, bool mind)`** - Reduces negative temp elements only:
  - `Element.List_Body = { 70 (STR), 72 (DEX), 71 (END), 77 (CHA) }`
  - `Element.List_Mind = { 74 (LER), 75 (WIL), 76 (MAG), 73 (PER) }`
  - For each element in the selected lists: if `tempElements` has it and `vBase < 0`, calls `ModTempElement(ele, Clamp(p/20 + rnd(p/20), 1, -vBase))` — adds a positive amount, never overshooting past 0

## UI & Reactive System

### UI Display

- **`Element._WriteNote(UINote n, ElementContainer owner, Action<UINote> onWriteNote, bool isRef, bool addHeader = true)`** - Renders the main stat panel:
  - **Header**: Adds header with element name (or ability header for `Act` elements)
  - **Detail text**: Adds element detail/flavor text if available
  - **Value display**: Calculates `num = vLink + (IsPCFaction ? faction.charaElements.Value(id) : 0)` for display
  - **For `Act` elements**: Displays "vValue" with `DisplayValue` and `ValueWithoutLink + num` (shows as "X (Base Y + Z)")
  - **For regular elements**: If `ShowValue` is true, displays "vCurrent" with `DisplayValue` and `ValueWithoutLink + num`
  - **Potential display**: If `ShowPotential` is true, displays potential breakdown with `vPotential + vSourcePotential + MinPotential + vTempPotential`
  - **Relative attribute**: If `ShowRelativeAttribute` is true and element has `aliasParent`, shows parent element info
  - **Debug info**: If `EClass.debug.showExtra` is true, shows class name, vExp, vSource, vSourcePotential, vPotential, Potential
  - **Level bonuses**: Calls `CheckLevelBonus(owner, n)` to show level-based unlocks
  - **Bonus breakdown**: If `ShowBonuses` is true and `owner.Chara != null`, creates `BonusInfo` and calls `WriteNote()` to show detailed breakdown
- **`Element.BonusInfo.WriteNote()`** - Adds detailed breakdown lines to UI (called from `_WriteNote`):
  - **Equipment bonuses**: Iterates `body.slots`, sums `thing.elements.GetOrCreateElement(id).Value` for each slot
    - Excludes `slot.elementId == 44` (special slot)
    - Excludes global elements (`!orCreateElement.IsGlobalElement`)
    - Special handling: If `id == 67 || id == 66`, excludes `slot.elementId == 35`
    - Displays as "equipment: +X" or "equipment: -X"
  - **Faction bonuses**: If `IsPCFaction`, gets `faction.charaElements.GetElement(id)` and displays `element.Value` as "sub_faction: +X"
  - **Conditions**: Iterates `conditions` list, for each condition:
    - Gets `condition.GetElementContainer()` (returns `ElementContainerCondition`)
    - Calls `elemContainer.Value(id)` and displays as condition name (e.g., "Poison: -5")
  - **Temporary modifiers**: If `tempElements != null`, calls `tempElements.Value(id)` and displays as "tempStrengthen" or "tempWeaken" based on sign
  - **Faith bonuses**: If `faithElements != null`, calls `faithElements.Value(id)` and displays using faith feat name or default name
  - **Multiplier bonuses**: Iterates `elements.dict.Values`, finds elements with `HasTag("multiplier")` and `aliasRef == ele.source.alias`, displays as percentage fix (e.g., "+X%")
  - **SPD side-effect**: If `id == 79`, calls `c.RefreshSpeed(this)` to recalculate speed info as part of the display
  - **Special cases**: Handles lucky coin (for LUK), machine bonuses (for DV/PV/SPD), etc.
- **UI Display (Equipment)**: Reads directly from `body.slots`:
  - Iterates `c.body.slots`, for each slot with `thing != null` and `slot.elementId != 44` (not backpack):
    - Gets `slot.thing.elements.GetOrCreateElement(id)` and sums `orCreateElement.Value`
    - Excludes global elements (`!orCreateElement.IsGlobalElement`)
  - Displays sum as "Equipment +X" line
  - **The UI does NOT read from `vLink` for equipment display** - it calculates directly from `body.slots`

### Core Refresh Functions

- **`Element.Value`** (int property) - **On-demand calculation**: Returns `ValueWithoutLink + vLink + ((owner != null) ? owner.ValueBonus(this) : 0)`. Calculated every time it's accessed, not cached. This means stats are always up-to-date without explicit recalculation.
- **`Element.ValueWithoutLink`** (int property) - **On-demand calculation**: Returns `vBase + vSource`. Also calculated on-demand.
- **`ElementContainer.Value(int ele)`** - Gets calculated value for element ID. Returns `element.Value` if element exists, or falls back to faction/PC logic if null. Also on-demand.
- **`Chara.Refresh(bool calledRecursive = false)`** - Updates **derived state** (visibility, conditions, telepathy, etc.), but **does NOT recalculate stats**. Stats are already calculated on-demand via properties. Called after major stat changes (e.g., `SetFeat()`, equipment changes) to update derived character state. Also calls `condition.OnRefresh()` for each condition and `SetDirtySpeed()`.

### Reactive Hooks

- **`Element.OnChangeValue()`** (virtual method) - Called when element values change:
  - **Triggered by**: `ModBase()`, `SetBase()`, `ModLink()` (when `vBase`, `vSource`, or `vLink` changes)
  - **Base implementation**: Empty (does nothing)
  - **Overrides**:
    - **`ABILITY.OnChangeValue()`**: If PC (`card._IsPC`), calls `LayerAbility.SetDirty(this)` to mark ability UI for redraw
    - **`AttbMain.OnChangeValue()`**: For specific attributes:
      - **70 (STR), 71 (END)**: Calls `chara.SetDirtyWeight()` to mark weight recalculation needed
      - **79 (SPD)**: Calls `chara.SetDirtySpeed()` to mark speed recalculation needed
    - **`SKILL.OnChangeValue()`**: For specific skills:
      - **207 (weight skill)**: Calls `chara.SetDirtyWeight()`
      - **306 (faith skill)**: Calls `chara.RefreshFaithElement()`
  - **Pattern**: Each element type can hook into its own UI/state update needs when its value changes
- **`ElementContainer.OnChangeValue()`** (virtual method) - Called when container-level changes occur. Base implementation is empty. Can be overridden by subclasses for container-specific updates.

### UI Update Mechanisms

- **`LayerAbility.SetDirty(Element ele)`** - Marks ability UI for redraw. Called from `ABILITY.OnChangeValue()` when ability stats change. Directly calls `Instance.list.Redraw()` to redraw ability lists. Also triggers redraws in related inventory UI elements.
- **`WidgetEquip.SetDirty()` / `WidgetEquip.Redraw()`** - Marks equipment UI for redraw. Called when equipment changes (equip/unequip). Triggers `_Redraw()` which updates equipment slot displays.
- **`Chara.SetDirtySpeed()`** - Marks speed recalculation as needed. Called from `Refresh()`, `AttbMain.OnChangeValue()`, and various movement-related changes. Triggers `RefreshSpeed()` when speed is next accessed.
- **`Chara.RefreshSpeed(Element.BonusInfo info = null)`** - Recalculates speed from SPD stat. Handles special cases (ride, host). Calculates speed from `Evalue(79)` (SPD element). Called on-demand when speed is accessed. Updates speed display in UI if `info` is provided.
- **`Chara.RefreshFaithElement()`** - Refreshes `faithElements` container. Unlinks old `faithElements` via `SetParent()` (no parent), creates new `ElementContainer()`, then sets elements via `SetBase()` based on piety value and faith's element map. Links new container to character via `SetParent(this)`. Called from `SKILL.OnChangeValue()` when faith skill (306) changes, or manually when faith changes.
  - **Import note**: When importing faith, do NOT import `faithElements` directly. Instead, import only the prerequisites (`c.idFaith` and `c_daysWithGod`), then always call `RefreshFaithElement()` to recalculate faith elements dynamically. This ensures `faithElements` stay synchronized with current piety calculations, which depend on element 306 (faith skill) and element 85 (for PC) that may or may not be imported depending on user options.

### Stat Modification Flow

**When a stat is modified** (e.g., `ModBase()`, `SetBase()`, `ModLink()`):

1. **Direct modification**: Field is updated (`vBase += v`, `vSource = v`, `vLink += v`)
2. **Hook triggered**: `OnChangeValue()` is called on the element
3. **Element-specific updates**: If element overrides `OnChangeValue()`, it triggers targeted UI updates (e.g., `LayerAbility.SetDirty()`, `SetDirtyWeight()`)
4. **Parent propagation**: If parent container exists and linking is allowed, change propagates up via `parent.ModLink()`
5. **Derived state**: If needed, `Refresh()` is called to update derived character state (visibility, conditions, etc.)

**When a stat is accessed** (e.g., `element.Value`, `container.Value(id)`):

1. **On-demand calculation**: Property calculates value from current fields (`vBase + vSource + vLink + ValueBonus`)
2. **No caching**: Value is calculated fresh each time - always reflects current state
3. **UI reads directly**: UI code reads `Value` property directly when rendering, ensuring it always sees current values

### Key Insights

- **On-demand calculation**: Stats are calculated when accessed, not pre-calculated
- **Reactive hooks**: UI updates are triggered when values change via `OnChangeValue()`
- **Targeted updates**: Each element type handles its own UI update needs
- **Separation of concerns**:
  - **Stat calculation**: On-demand via properties (always current)
  - **UI updates**: Reactive via `OnChangeValue()` hooks (targeted, efficient)
  - **Derived state**: Updated via `Refresh()` when needed (visibility, conditions, etc.)
- **Propagation**: Changes propagate through parent-child relationships via `ModLink()`, ensuring linked containers (equipment, conditions) stay in sync
- **Efficiency**: On-demand calculation means stats are only computed when needed, and UI updates are targeted to specific elements that changed

## Feat Categorization

### Tag-Based Classification

Feats are categorized via **tags** on their `SourceElement.Row` data (spreadsheet-driven, not code constants). The game uses these tags:

- **`"innate"`** - Race/class-specific feats. Cannot be purchased. Shown in a separate "Innate" section in character windows (`WindowChara.cs:633`, `WindowCharaMini.cs:150`). Excluded from `ListAvailabeFeats()` (`Chara.cs:10059`).
- **`"class"`** - Class/job-specific feats. Excluded from `ListAvailabeFeats()`.
- **`"hidden"`** - Hidden feats. Excluded from `ListAvailabeFeats()`. Only shown in UI when `EClass.debug.showExtra` is true.
- **`"noPet"`** - Feats that cannot be granted to pets. Excluded from `ListAvailabeFeats(pet: true)`.

### Purchasable Feat Criteria

A feat is purchasable (appears in the feat purchase UI) when ALL of these are true (`Chara.ListAvailabeFeats()` at `Chara.cs:10052-10064`):
1. `source.group == "FEAT"`
2. `source.cost[0] != -1` (has a purchasable cost)
3. `!source.categorySub.IsEmpty()` (has a subcategory)
4. NOT tagged `"class"`, `"hidden"`, or `"innate"`
5. Current tier < `source.max`
6. `IsPurchaseFeatReqMet(elements)` returns true (prerequisite elements met)

### Character Window Display

- **Innate feats section**: `elements.ListElements(a => a.source.category == "feat" && a.HasTag("innate") && a.Value != 0)` (`WindowChara.cs:633`)
- **Non-innate feats section**: `elements.ListElements(a => a.source.category == "feat" && !a.HasTag("innate") && a.Value != 0)` (`WindowChara.cs:655`)
- **Character maker**: Shows all feats except feat 1220 (featFate), with `addRaceFeat: true` flag which adds element 29 as a display-only entry (`UICharaMaker.cs:175`)

## Gene System

### Overview

Genes (`DNA` objects) are installable modifications that grant stat boosts, feats, abilities, and body parts to characters. They are stored in `Chara.c_genes` (`CharaGenes` class, `CharaGenes.cs`).

### DNA Data Structure

Each `DNA` object (`DNA.cs`) contains:
- **`id`** (string) - Source character ID the gene was generated from
- **`vals`** (`List<int>`) - Flat list of alternating `(elementId, value)` pairs. Each pair represents one gene effect.
- **`type`** (`DNA.Type` enum) - `Inferior` (0), `Default` (3), `Superior` (5), `Brain` (8)
- **`cost`** (int) - Feat point cost to install
- **`slot`** (int) - Gene slot size requirement (how many slots this gene occupies)
- **`isManiGene`** (bool) - Whether this is a manifestation gene
- **`seed`** (int) - Random seed used during generation
- **`lv`** (int) - Level of the source character

### Gene Val Categories

Gene vals can contain any element type. Categories determine how they're applied (`DNA.Apply()` at `DNA.cs:195-249`):
- **`"feat"`** - Applied via `c.SetFeat(num, ValueWithoutLink + 1)` (forward) or `SetFeat(num, ValueWithoutLink - 1)` (reverse). Modifies the feat's vBase (and triggers `Feat.Apply` side effects on other elements).
- **`"ability"`** - Added to `c.ability` list (forward) or removed (reverse). Not stored in elements vBase.
- **`"slot"`** - Body parts. Added via `c.body.AddBodyPart()` (forward) or `RemoveBodyPart()` (reverse).
- **Default** (skills, attributes, etc.) - Applied via `c.elements.ModBase(num, value)` (forward) or `ModBase(num, -value)` (reverse). Directly modifies vBase.

### Gene Installation Methods

1. **Gene Machine (surgery)** - For non-PC party members only (`TraitGeneMachine.cs:86` filters `!member.IsPC`). Player places ally in stasis chamber, inserts gene via `LayerDragGrid`, waits for duration (`DNA.GetDurationHour()` based on cost), then extracts. At completion: `condition.gene.c_DNA.Apply(target)` (`TraitGeneMachine.cs:127`).

2. **Eating genes (Slime PC only)** - Requires `HasElement(1274)` (Predatory Evolution). PC can eat gene items on the ground via `TraitGene.TrySetHeldAct()` (`TraitGene.cs:41-83`) which triggers `AI_Eat`. Gene is applied at `FoodEffect.cs:469`: `c_DNA.Apply(c2)`. If gene slots are exceeded, oldest genes are auto-removed.

### Gene Installation Flow (`DNA.Apply(Chara c)` at `DNA.cs:178-193`)

1. Creates `c.c_genes = new CharaGenes()` if null
2. If `Type.Inferior`: increments `c.c_genes.inferior` (grants gene removal credit) and returns
3. Deducts feat points: `c.feat -= cost * c.GeneCostMTP / 100`
4. Calls `Apply(c, reverse: false)` to apply all vals
5. Adds DNA to `c_genes.items` list

### Gene Removal (`CharaGenes.Remove()` at `CharaGenes.cs:12-22`)

```
CharaGenes.Remove(Chara c, DNA item):
  c.c_genes.items.Remove(item)           // Remove from list
  c.feat += item.cost * c.GeneCostMTP/100 // Refund feat points
  item.Apply(c, reverse: true)            // Reverse all effects
  c.Refresh()                             // Refresh derived state
  c.RemoveAllStances()                    // Clear stances
```

Removal requires `genes.inferior > 0` (each inferior gene grants one removal credit, checked in `WindowCharaMini.cs:244`). Some genes cannot be removed: `DNA.CanRemove()` (`DNA.cs:251-270`) returns false for feats 1237 (featRoran), 1415 (featFoxMaid), and conditionally for 1228 (featDemigod), 1414 (featWhiteVixen) if character is PC.

### Gene Cost

- **PC**: `GeneCostMTP = 5` (`Chara.cs:1092`) - PCs pay only 5% of nominal cost
- **Non-PC**: `GeneCostMTP = 100` (`Chara.cs:1090`) - NPCs pay full cost
- Gene slots limited by: `MaxGeneSlot = race.geneCap - (HasElement(1237) ? 2 : 0) + Evalue(1242) + Evalue(1273) + ((IsPC && HasElement(1274)) ? (Evalue(1274) - 7) : 0)` (`Chara.cs:1082`)

### Gene Generation (`DNA.Generate()` at `DNA.cs:279-455`)

Gene contents are determined from a model character:
1. `ListGeneFeats()` (`ElementContainer.cs:636-638`) returns feats where `ValueWithoutLink > 0 && category == "feat" && cost[0] > 0 && geneSlot >= 0`
2. If no gene feats found, falls back to `ListAvailabeFeats(pet: true)`
3. Randomly adds attributes, skills, feats, abilities, and body parts based on gene type
4. **No race filtering**: Any feat the model character has (including race-granted feats) can appear in the gene

### Important Notes

- **Clearing `c_genes = null` is unsafe**: Gene effects (stat boosts, feats, body parts, abilities) already applied to elements would NOT be reversed. Must call `DNA.Apply(c, reverse: true)` for each gene before removing.
- **Gene effects on race change**: `ChangeRace()` does not touch `c_genes`. Genes that granted race-specific feats (e.g., 1274 via a slime gene) persist on the character even after changing to a non-slime race.

## Predatory Evolution (featSlimeEvolution)

### Identity

- **Feat ID**: 1274 (`FEAT.featSlimeEvolution` at `FEAT.cs:172`)
- **Category**: `"feat"` (a Feat element, not an ability)
- **Source**: Granted by Slime race via `race.elementMap` (applied to `vSource` via `ApplyElementMap`)

### What It Grants

1. **Extra gene slots**: `MaxGeneSlot` includes `((IsPC && HasElement(1274)) ? (Evalue(1274) - 7) : 0)` (`Chara.cs:1082`). At tier 8 (max), grants 1 extra slot.
2. **Ability to eat genes**: `TraitGene.TrySetHeldAct()` (`TraitGene.cs:43`) and `AI_Eat` (`AI_Eat.cs:32`) check `HasElement(1274)`. Without it, PCs cannot eat gene items.
3. **ActSlime ability (6608)**: `ElementContainerCard.CheckSkillActions()` (`ElementContainerCard.cs:44`) calls `TryLearn(6608, 1274, 0)` -- if character has feat 1274 at any value, they learn ActSlime.
4. **Gene cost display**: `TraitGene.WriteNote()` (`TraitGene.cs:24`) shows adjusted gene cost for slime PCs.
5. **Spell stock behavior**: `Chara.cs:6051` -- if PC has 1274 and a spell has negative `vPotential`, blocks casting with "noSpellStock" message (slimes use spell stocks differently).

### Auto-Levelup

On each `Card.LevelUp()` (`Card.cs:3078-3080`):
```
if (IsPC && HasElement(1274) && Evalue(1274) < 8 && LV >= Evalue(1274) * 5)
    Chara.SetFeat(1274, Evalue(1274) + 1, msg: true);
```
This increments the feat tier (up to max 8) as the PC levels. `SetFeat` sets `vBase = value - vSource`, so auto-levelup modifies `vBase`.

### Apply Effect (`FEAT.cs:643-644`)

```csharp
case 1274:
    featRef[3] = (a - 7).ToString() ?? "";
    break;
```
Only sets display text (`featRef[3]` shows extra gene slots = tier - 7). Does NOT call `ModBase` on other elements -- all of Predatory Evolution's mechanical effects are hardcoded checks (`HasElement(1274)`, `Evalue(1274)`) throughout the codebase rather than stat modifications.

## Element ID Quick Reference

Main attributes (from SKILL.cs constants):
- **60** = life, **61** = mana, **62** = vigor (stamina multiplier elements)
- **64** = DV, **65** = PV, **66** = HIT
- **70** = STR, **71** = END, **72** = DEX, **73** = PER, **74** = LER, **75** = WIL, **76** = MAG, **77** = CHA, **78** = LUC, **79** = SPD, **80** = INT (rarely used)
- **93** = antiMagic, **207** = weightlifting, **303** = manaCapacity, **306** = faith

Key feats (from FEAT.cs constants):
- **1220** = featFate, **1274** = featSlimeEvolution (Predatory Evolution), **1415** = featFoxMaid
- **1237** = featRoran, **1228** = featDemigod, **1414** = featWhiteVixen
- **1242** = featGeneSlot, **1273** = featBloom, **1644** = featBodyParts (Chaos Shape)

Key abilities (from ABILITY.cs constants):
- **6608** = ActSlime (slime eating action, learned from feat 1274)
