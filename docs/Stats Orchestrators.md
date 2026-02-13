# Stats Orchestrators

## Variables / Maps / Arrays

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

### Chara Element Containers

- **`elements`** (`ElementContainerCard`, inherited from `Card`) - Main container for character stats (STR, MAG, END, etc.). Contains elements with `vBase`, `vSource`, `vLink` values. Overrides `ValueBonus()` to add faction bonuses and special calculations (lucky coin, machine bonuses, etc.).
- **`tempElements`** (`ElementContainer`) - Separate container for temporary modifiers (afflictions, ether diseases). Created lazily on first `ModTempElement()` call. Elements in this container have their own `vBase` values, separate from main `elements`. Linked to character via `SetParent(this)` on creation. Used for UI display of "Temporary Weakness -X" lines via `BonusInfo.WriteNote()`.
- **`faithElements`** (`ElementContainer`) - Container for faith-based bonuses. Created when character has a faith (`Chara.faith`) via `RefreshFaith()`. Elements are set via `SetBase()` based on piety value and faith's element map, then linked to character via `SetParent(this)`. Displayed in UI via `BonusInfo.WriteNote()` which calls `faithElements.Value(id)`. Can be null if character has no faith.
- **`workElements`** (`ElementContainer`) - Container for work/hobby bonuses from character's home branch. Created dynamically via `RefreshWorkElements()` when character joins/leaves a faction branch. Contains bonuses from hobbies and works at the branch, calculated based on efficiency. Elements are set via `ModBase()` and linked to character via `SetParent(parent)`, affecting character's `vLink`. Can be null if character has no home branch or is in PC party.
- **`baseWorkElements`** (`ElementContainer`, property) - Base work elements from all hobbies/works (not branch-specific). Lazy-initialized property that builds elements from `ListHobbies()` and `ListWorks()` via `ModBase()`. Used internally, not linked to character.
- **`body.slots`** (array of `BodySlot`) - Array representing equipped items. Each slot has `slot.thing.elements` containing the item's element contributions. UI reads directly from `body.slots` to display "Equipment +X", not from `vLink`. Equipment elements are linked to character's `vLink` via `SetParent(owner)` when equipped.
- **`conditions`** (List of `Condition`) - List of status effects. Each condition can have an `ElementContainer` via `GetElementContainer()` which links to character's `vLink` via `SetParent(owner)`. Displayed in UI via `BonusInfo.WriteNote()` which iterates conditions and sums their element contributions.
- **`corruption`** (int) - Stat that influences Ether Disease progression. Threshold = `corruption / 100`. When threshold crosses, `MutateRandom()` is called.
- **`EClass.pc.faction.charaElements`** (`ElementContainerFaction`) - Faction-based element container for PC faction members. Contains global elements from equipped items (items with `IsGlobalElement = true`). Added via `OnEquip()` when PC faction members equip items, removed via `OnUnequip()` when unequipped. Elements are added to this container via `ModBase()` when global equipment is equipped. **How it affects stats**: Added via `ElementContainer.ValueBonus()` → `faction.charaElements.Value(ele)` for PC faction members. Also added to `vLink` for display in "Base X + Y" in `Element._WriteNote()`: `num += faction.charaElements.Value(id)`. Included in `Element.Value` calculation: `Value = ValueWithoutLink + vLink + ValueBonus(owner)` where `ValueBonus()` returns `faction.charaElements.Value(ele)`. **Special behavior**: `OnEquip()`/`OnUnequip()` check `IsEffective(t)` to filter by deity alignment. Calls `CheckDirty()` to refresh all PC faction members when modified.

### Zone Containers

- **`EClass._zone.elements`** (`ElementContainerZone`) - Zone-based element container. Contains zone-specific elements (policies, techs, land feats). Overrides `OnLearn()` and `OnLevelUp()` for zone-specific messages. Used for zone-wide bonuses and unlocks.
- **`Faction.elements`** (`ElementContainerZone`) - Faction's zone container. Same as zone elements but for faction-wide effects.

## Functions (Recalculations / Display)

### Container Type Behavior

- **`ElementContainer`** (base class) - Base container class. `ValueBonus()` returns 0 by default. Uses `dict` (`Dictionary<int, Element>`) internally to store and retrieve elements.
- **`ElementContainerCard`** - Used for `Card.elements`. Overrides `ValueBonus()` to add:
  - Faction bonuses: `faction.charaElements.Value(ele)`
  - Lucky coin bonuses (for LUK)
  - Machine bonuses (for STR/DEX/SPD)
  - Party-wide bonuses (for certain stats)
  - Multiplier bonuses (percentage-based)
- **`ElementContainerCondition`** - Used for condition element containers. Overrides `LimitLink = false` to allow unlimited linking (conditions can link to character without limit).
- **`ElementContainerFaction`** - Used for `faction.charaElements`. Handles global elements from equipment via `OnEquip()`/`OnUnequip()`. Tracks `isDirty` flag and refreshes all PC faction members when modified.
- **`ElementContainerZone`** - Used for zones and factions. Overrides `OnLearn()` and `OnLevelUp()` for zone-specific messages and logging.
- **`ElementContainerField`** - Used for field effects. Empty subclass (just inherits base functionality).

### Element Value Calculation

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

### Element Modification

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
  - If element becomes empty (all values 0): Removes element via `Remove(ele.id)`
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
  - **Special cases**: Handles lucky coin (for LUK), machine bonuses (for STR/DEX/SPD), etc.

### Condition Access

- **`BaseCondition.GetElementContainer()`** (virtual method) - Returns `ElementContainer` for condition's stat contributions:
  - Base implementation returns `elements` (the condition's `ElementContainerCondition`)
  - Can be overridden (e.g., `ConDisease.GetElementContainer()` returns a different container)
  - Returns `null` if condition has no element contributions (`UseElements = false`)
  - **Used by**: `BonusInfo.WriteNote()` to get condition contributions, `WidgetStatsBar` to calculate stat display
- **`Chara.HasCondition<T>()`** - Checks if character has a condition of type `T`:
  - Iterates through `conditions` list
  - Returns `true` if any condition is of type `T`, `false` otherwise
  - **Used by**: Various game logic checks (e.g., `HasCondition<ConPoison>()`, `HasCondition<ConBurning>()`)
- **`Chara.GetCondition<T>()`** - Gets condition of type `T` from character:
  - Iterates through `conditions` list
  - Returns first condition of type `T`, or `null` if not found
  - **Used by**: To access specific condition properties (e.g., `GetCondition<ConGravity>()?.GetPhase()`)
- **`Chara.RemoveCondition<T>()`** - Removes condition of type `T`:
  - Finds condition in `conditions` list
  - Calls `condition.Kill()` to remove it
  - **Note**: Already documented in "Conditions" orchestrator section

## Stat Modification

### Temporary Modifiers

- **`Chara.ModTempElement(int ele, int a, bool naturalDecay, bool onlyRenew)`** - Modifies elements within `tempElements`. Used for afflictions, temporary stat changes, etc.
  - **Early return**: If `a < 0` and character has "sustain\_" element for this stat, returns early (sustain prevents negative temp modifiers)
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

### Feats / Traits

- **`Chara.SetFeat(int id, int value = 1, bool msg = false)`** - Sets a feat/trait:
  1. Gets existing feat: `elements.GetElement(id) as Feat`
  2. If feat exists and `feat.Value > 0`: Calls `feat.Apply(-feat.Value, elements)` to remove old effects (reverses previous modifications)
  3. Sets feat's own `vBase` via `elements.SetBase(id, value - (feat?.vSource ?? 0))` - stores only the delta above race/job so that `Value` = `vBase + vSource` = desired total tier. There is no separate "demote" API; use `SetFeat(id, lowerValue)` to downgrade.
  4. If `feat.Value != 0`: Calls `feat.Apply(feat.Value, elements)` to apply new effects
  5. Calls `Refresh()` and `CalculateMaxStamina()` if game is started
  6. If `msg=true`: Displays message about gaining/changing feat
- **Feat tier and cost (disassembly)**: `SourceElement.Row.cost` is `int[]` with per-tier cost: `cost[0]` = tier 1, `cost[1]` = tier 2, etc. `FEAT.CostLearn` returns `source.cost.TryGet(Value - 1)` (index by current tier). For multi-ID tier chains (e.g. mutations), `aliasParent` points from **child to parent**; `SetMutation()` clears the parent feat before setting the child. No predefined tier-order list; hierarchy is from `aliasParent` traversal.
- **`Feat.Apply(int a, ElementContainer owner, bool hint = false)`** - Applies a feat's effects:
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
  - **Value calculation**:
    - If element exists: `num = element.Value + vec` (or random ±1 if `vec == 0`)
    - Clamps `num` to `element.source.max - 1` if exceeds max
    - Skips if mutation would decrease when `vec > 0` (only allows increases when adding)
  - **Mutation application**: Calls `SetFeat(row.id, num)` to apply/update the mutation feat
  - **Tracking** (when `vec > 0 && ether && flag2`): Creates `c_corruptionHistory` list if null, adds `row.id` to track mutation order
  - Returns `true` if mutation was applied, `false` if nothing happened after all tries
- **`Chara.ModCorruption(int a)`** - Orchestrator for Ether Disease progression/curing:
  - **Early return**: If `a > 0` and character has high ether resistance (`Evalue(962) >= 25` or `ResistLv(962) > 0` with random chance), returns early
  - **Resistance reduction**: If `a > 0` and character has element 412, reduces `a` based on resistance: `a = a * 100 / Max(100 + element412 * 2, 10)`
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

### Equipment

- **`CharaBody.Equip(Thing thing, BodySlot slot = null, bool msg = true)`** - Equips an item:
  1. Finds appropriate slot via `GetSlot(thing.category.slot)` (or `GetSlot(thing.category.slot, onlyEmpty: false)` if no empty slot)
  2. Validates slot matches `thing.category.slot` and checks `IsEquippable()`
  3. If slot already has item: Unequips existing item first (calls `Unequip(slot, refresh: false)`)
  4. Unequips `thing` from any other slot if already equipped (calls `Unequip(thing, refresh: false)`)
  5. If `thing.parent != owner`: Calls `owner.AddCard(thing)` to transfer ownership
  6. Sets `slot.thing = thing` and `thing.c_equippedSlot = slot.index + 1`
  7. **Critical**: Calls `thing.elements.SetParent(owner)` - this links equipment elements to character via `ModLink()`
  8. Calls `thing.trait.OnEquip(owner, onSetOwner: false)` for trait effects
  9. If PC faction: Calls `faction.charaElements.OnEquip(owner, thing)` for faction bonuses (global elements only)
  10. Calls `owner.SetTempHand()`, `owner.Refresh()` if character is created, and UI refresh if PC
- **`CharaBody.Unequip(BodySlot slot, bool refresh = true)`** - Unequips an item:
  1. Gets `thing` from `slot.thing` (returns early if null)
  2. If PC faction: Calls `faction.charaElements.OnUnequip(owner, thing)` to remove faction bonuses
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
- **UI Display** (`Element.BonusInfo.WriteNote()`): Reads directly from `body.slots`:
  - Iterates `c.body.slots`, for each slot with `thing != null` and `slot.elementId != 44` (not backpack):
    - Gets `slot.thing.elements.GetOrCreateElement(id)` and sums `orCreateElement.Value`
    - Excludes global elements (`!orCreateElement.IsGlobalElement`)
  - Displays sum as "Equipment +X" line
  - **The UI does NOT read from `vLink` for equipment display** - it calculates directly from `body.slots`
- **Faction Equipment Bonuses** (`ElementContainerFaction.OnEquip(Thing t)`):
  - Checks `IsEffective(t)` (deity matching)
  - For each element in equipment: If `value.IsGlobalElement`, calls `ModBase(value.id, value.Value)` on faction container
  - Also sets `vExp = value.vExp` to preserve experience
  - Sets `isDirty = true` and calls `CheckDirty()` to refresh all PC faction characters
  - Global elements from equipment are added to faction's `vBase` directly (separate from parent-child linking mechanism)

### Conditions

- **`Chara.AddCondition<T>(int p = 100, bool force = false)`** - Adds a condition by type. Calls `AddCondition(typeof(T).Name, p, force)`.
- **`Chara.AddCondition(string id, int p = 100, bool force = false)`** - Adds a condition by ID. Creates condition via `Condition.Create(id, p)` then calls `AddCondition(Condition c, force)`.
- **`Chara.AddCondition(Condition c, bool force = false)`** - Adds a condition object:
  1. Sets `c.owner = this`
  2. Checks resistances and applies reduction if needed
  3. Checks negate list (conditions that prevent this condition)
  4. Calculates duration via `c.EvaluateTurn(c.power)`
  5. Handles stacking/overriding if condition already exists
  6. Adds to `conditions` list
  7. Calls `c.SetOwner(this)` which creates `ElementContainerCondition` if `UseElements` is true, sets elements via `SetBase()`, then **calls `elements.SetParent(owner)`** - this links condition's elements to character's `vLink`
  8. Calls `c.OnAdded()` hook and `owner.Refresh()` if needed
- **`Chara.RemoveCondition<T>()`** - Removes condition by type. Finds condition in `conditions` list and calls `condition.Kill()`.
- **`Condition.Kill(bool silent = false)`** - Removes condition:
  1. Sets `value = 0` and removes from `owner.conditions` list
  2. Plays end effect and message (if not silent)
  3. **If `elements != null`: Calls `elements.SetParent()` (no parent)** - this unlinks condition's elements from character's `vLink`
  4. Calls `OnRemoved()` hook and refreshes emo icon
- **`Chara.TickConditions()`** - Called each turn to update conditions. Iterates `conditions` list and calls `condition.Tick()` for each.
- **`Condition.Tick()`** - Default implementation calls `Mod(-1)` to decrease `value` by 1 each turn. When `value <= 0`, `OnValueChanged()` calls `Kill()` to remove condition.
- **Condition element contributions**: Conditions work similarly to equipment - when added with `UseElements = true`, they create `ElementContainerCondition`, set elements via `SetBase()`, and link to character's `vLink` via `SetParent(owner)`. When removed, they unlink via `SetParent()` (no parent). Their contributions are separate from `tempElements` - conditions have their own container system.

### Training / Permanent Bonuses

- **`ElementContainer.Train(int ele, int a = 10)`** - Initiates training for an element. Calls `OnTrain(ele)` (virtual hook, can be overridden), then calls `ModTempPotential(ele, a)` to increase temporary potential. Training itself doesn't directly increase `vBase` - it increases potential which affects experience gain rate.
- **`ElementContainer.ModTempPotential(int ele, int v, int threshMsg = 0)`** - Modifies temporary potential. Gets/creates element via `GetOrCreateElement(ele)`, modifies `orCreateElement.vTempPotential += v` (clamped to max 1000), then calls `OnModTempPotential(orCreateElement, v, threshMsg)` hook. Higher potential = faster experience gain (affects `ModExp` calculations).
- **`ElementContainer.ModExp(int ele, float a, bool chain = false)`** - Adds experience to an element:
  - **Early returns**: If `(Card != null && Card.isChara && Card.Chara.isDead) || a == 0f`, returns early. Gets element and checks `CanGainExp` (requires `ValueWithoutLink > 0`), returns if element is null or can't gain exp.
  - **Experience modifiers** (only applied when `a > 0f`):
    1. **Days together bonus**: If `!chain && Card != null && Card.isChara`, multiplies `a` by `GetDaysTogetherBonus() / 100` (bonus based on how long character has been in party)
    2. **UseExpMod formula**: If `element.UseExpMod` is true, applies formula: `a = a * Clamp(UsePotential ? Potential : 100, 10, 1000) / (100 + Max(0, ValueWithoutLink) * 25)` (higher potential = more exp, higher value = less exp). After calculation, probabilistically rounds up fractional part: `if (rndf(1f) < a % 1f) { a += 1f; }`
    3. **Parent factor**: If `!chain && element.source.parentFactor > 0f && Card != null && !element.source.aliasParent.IsEmpty()`, gets parent element and recursively calls `ModExp(parent.id, Clamp(a * parentFactor / 100f, 1f, 1000f), chain: true)` to grant experience to parent (prevents infinite loops via `chain: true`)
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
    4. **Modifies `Element.vSource`** by calculated amount: `orCreateElement.vSource += (int)num2 * num` (clamped to 99999999)
    5. If `applyFeat=true` and element is a `Feat`: Calls `Feat.Apply((int)num2, this)` which modifies OTHER elements' `vBase` via `ModBase()`
  - Resets random seed
  - **Critical**: Race/Job bonuses modify `Element.vSource` for ALL elements in the map. The feat's own value goes to `vSource`. If `applyFeat=true`, `Feat.Apply()` is called AFTER modifying `vSource`, which then modifies OTHER elements' `vBase` (not the feat's own `vBase`).
- **`Feat.Apply(int a, ElementContainer owner, bool hint = false)`** - Applies feat effects (called from `ApplyElementMap` or `SetFeat`):
  - **Important**: This modifies OTHER elements' `vBase`, NOT the feat's own `vBase`
  - If `hint=false` and `a > 0`: Sets `vPotential = owner.Chara.LV`
  - Switches on feat ID to apply specific effects via `ModBase()` on OTHER elements
  - Example: Feat ID 1610 calls `ModBase(60, a * 4)` - modifies element 60's `vBase`, not the feat's `vBase`
- **`Chara.SetFeat(int id, int value = 1, bool msg = false)`** - Sets a feat directly:
  1. Gets existing feat and removes old effects: `feat.Apply(-feat.Value, elements)`
  2. Sets feat's own `vBase` via `elements.SetBase(id, value - (feat?.vSource ?? 0))` - sets `Element.vBase` directly
  3. Applies new feat effects: `feat.Apply(feat.Value, elements)` - modifies OTHER elements' `vBase`
  - **Note**: When set directly, the feat's own value is in `vBase`, but its effects modify OTHER elements' `vBase`
- **`Card.ChangeMaterial(SourceMaterial.Row row, bool ignoreFixedMaterial = false)`** - Changes character material:
  - Calls `ApplyMaterial(remove: true)` to remove old material bonuses
  - Sets `_material = row`, `idMaterial = row.id`, `decay = 0`
  - Marks `dirtyWeight = true` and calls `SetDirtyWeight()` if PC
  - Calls `ApplyMaterial()` to apply new material bonuses via `ApplyMaterialElementMap()` → `ApplyElementMap()`
- **`Chara.InitStats(bool onDeserialize = false)`** - Initial stat setup:
  - Calls `ApplyRace()` and `ApplyJob()` to apply initial race/job bonuses (modify `vSource`)
  - Calls `ChangeMaterial()` for initial material

## Orchestrators

**Summary**: The stat system uses **on-demand calculation** with **reactive hooks** for UI updates. There is **no single "recalculate all stats" function** - stat values are calculated dynamically when accessed via properties, and UI updates are triggered through targeted hooks when values change.

### Core Refresh/Recalculate Functions

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
