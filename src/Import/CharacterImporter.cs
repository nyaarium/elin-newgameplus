using System;
using System.Collections.Generic;
using System.Linq;

namespace NewGamePlus;

public static class CharacterImporter
{
	public static void ImportBio(Chara c, string dumpFilePath)
	{
		CharacterDumpData dumpData = DumpSerializer.LoadDumpData(dumpFilePath);
		if (dumpData == null)
		{
			return;
		}
		int importedCount = 0;
		if (dumpData.cardIdPortrait != null && dumpData.cardIdPortrait.Count > 0)
		{
			((Card)c).c_idPortrait = dumpData.cardIdPortrait[0];
			importedCount++;
		}
		if (dumpData.cardIdRace != null && dumpData.cardIdRace.Count > 0)
		{
			c.ChangeRace(dumpData.cardIdRace[0]);
			importedCount++;
		}
		if (dumpData.cardIdJob != null && dumpData.cardIdJob.Count > 0)
		{
			c.ChangeJob(dumpData.cardIdJob[0]);
			importedCount++;
		}
		EMono.player.RefreshDomain();
		if (dumpData.cardAltName != null && dumpData.cardAltName.Count > 0)
		{
			((Card)c).c_altName = dumpData.cardAltName[0];
			importedCount++;
		}
		if (!string.IsNullOrEmpty(dumpData.charaAlias)) { c._alias = dumpData.charaAlias; importedCount++; }
		if (dumpData.bioIds != null && dumpData.bioIds.Count > 0) { ((Card)c).bio.ints = dumpData.bioIds.ToArray(); importedCount++; }
	}

	public static void ImportStat(Chara c, string dumpFilePath)
	{
		CharacterDumpData dumpData = DumpSerializer.LoadDumpData(dumpFilePath);
		if (dumpData == null)
		{
			return;
		}

		if (dumpData.playerHolyWell > 0)
		{
			EClass.player.holyWell = dumpData.playerHolyWell;
		}
		if (dumpData.player_well_wish > 0)
		{
			EClass.player.ModKeyItem("well_wish", dumpData.player_well_wish, true);
		}
		if (dumpData.player_well_enhance > 0)
		{
			EClass.player.ModKeyItem("well_enhance", dumpData.player_well_enhance, true);
		}
		if (dumpData.player_jure_feather > 0)
		{
			EClass.player.ModKeyItem("jure_feather", dumpData.player_jure_feather, true);
		}
		if (dumpData.player_lucky_coin > 0)
		{
			EClass.player.ModKeyItem("lucky_coin", dumpData.player_lucky_coin, true);
		}
		if (dumpData.player_little_dead > 0)
		{
			EClass.player.little_dead = dumpData.player_little_dead;
		}
		if (dumpData.player_little_saved > 0)
		{
			EClass.player.little_saved = dumpData.player_little_saved;
		}
		if (dumpData.playerDeepest > 0)
		{
			EClass.player.stats.deepest = dumpData.playerDeepest;
		}
		if (dumpData.playerKumi > 0)
		{
			EClass.player.stats.kumi = dumpData.playerKumi;
		}
		if (dumpData.playerKnownBGMs != null && dumpData.playerKnownBGMs.Count > 0)
		{
			EClass.player.knownBGMs = dumpData.playerKnownBGMs.ToHashSet();
		}
		if (dumpData.playerSketches != null && dumpData.playerSketches.Count > 0)
		{
			EClass.player.sketches = dumpData.playerSketches.ToHashSet();
		}

		// Import workElements (work/hobby bonuses from faction branch)
		// Note: This may not fully restore if character's home branch changed, but preserves the bonuses
		if (dumpData.workElements != null && dumpData.workElements.Count > 0)
		{
			if (c.workElements == null)
			{
				c.workElements = new ElementContainer();
				// workElements parent is set by RefreshWorkElements() based on branch, but we'll link to character for now
				c.workElements.SetParent(c);
			}
			foreach (ElementData elementData in dumpData.workElements)
			{
				c.workElements.SetBase(elementData.id, elementData.vBase, 0);
			}
		}

		if (ModConfig.GetOption("includeSkills")?.Value == true && dumpData.playerKnownCraft != null && dumpData.playerKnownCraft.Count > 0)
		{
			EClass.player.knownCraft = dumpData.playerKnownCraft.ToHashSet();
		}

		if (ModConfig.GetOption("includeCraftRecipes")?.Value == true && dumpData.playerKnownRecipe != null && dumpData.playerKnownRecipe.Count > 0)
		{
			ImportRecipeConfig(dumpData.playerKnownRecipe);
		}

		if (ModConfig.GetOption("includeCodex")?.Value == true && dumpData.playerCodex != null && dumpData.playerCodex.Count > 0)
		{
			EClass.player.codex.creatures.Clear();
			foreach (CodexCreatureData entry in dumpData.playerCodex)
			{
				if (entry.ints == null || !EClass.sources.charas.map.ContainsKey(entry.id))
					continue;
				var creature = new CodexCreature { id = entry.id, _ints = (int[])entry.ints.Clone() };
				EClass.player.codex.creatures[entry.id] = creature;
			}
			EClass.player.codex.OnLoad();
		}

		if (ModConfig.GetOption("includeInfluence")?.Value == true && dumpData.zoneInfluence != null && dumpData.zoneInfluence.Count > 0 && EClass.game?.spatials?.Zones != null)
		{
			foreach (Zone zone in EClass.game.spatials.Zones)
			{
				if (dumpData.zoneInfluence.TryGetValue(zone.id, out int influence))
					zone.influence = influence;
			}
		}

		if (ModConfig.GetOption("includeKarma")?.Value == true && dumpData.playerKarma != 0)
		{
			EClass.player.karma = dumpData.playerKarma;
		}

		if (ModConfig.GetOption("includeFame")?.Value == true && dumpData.playerFame != 0)
		{
			EClass.player.fame = dumpData.playerFame;
		}

		// Import level and experience (if enabled)
		if (ModConfig.GetOption("includePlayerLevel")?.Value == true)
		{
			if (dumpData.playerTotalFeat > 0)
			{
				int currentLevel = ((Card)c).LV;
				// playerTotalFeat is the number of level-ups gained, not the level itself
				// Level = 1 (start) + playerTotalFeat (level-ups)
				int targetLevel = dumpData.playerTotalFeat + 1;
				int levelsToGain = targetLevel - currentLevel;
				if (levelsToGain > 0)
				{
					for (int i = 0; i < levelsToGain; i++)
					{
						((Card)c).LevelUp();
					}
				}
			}

			// Import level experience
			((Card)c).exp = dumpData.charaLevelExp;
		}

		((Card)c).feat = dumpData.charaFreeFeatPoints;

		// Import all elements
		if (dumpData.charaElements != null && dumpData.charaElements.Count > 0)
		{
			// Filter elements by category based on import options
			List<ElementData> elementsToImport = new List<ElementData>();
			foreach (ElementData elementData in dumpData.charaElements)
			{
				// Get element source to check category
				SourceElement.Row sourceRow = null;
				if (EClass.sources.elements.map.TryGetValue(elementData.id, out sourceRow))
				{
					string category = sourceRow.category;

					bool include = category switch
					{
						"ability" => ModConfig.GetOption("includeAbilities")?.Value == true,
						"attribute" => ModConfig.GetOption("includeAttributes")?.Value == true,
						"skill" => ModConfig.GetOption("includeSkills")?.Value == true,
						_ => true
					};
					if (include)
						elementsToImport.Add(elementData);
				}
				else
				{
					// If source not found, import anyway (fallback for unknown elements)
					elementsToImport.Add(elementData);
				}
			}
			if (elementsToImport.Count > 0)
			{
				string exportedRaceId = (dumpData.cardIdRace != null && dumpData.cardIdRace.Count > 0) ? dumpData.cardIdRace[0] : null;
				string exportedJobId = (dumpData.cardIdJob != null && dumpData.cardIdJob.Count > 0) ? dumpData.cardIdJob[0] : null;
				ImportElementConfig((Card)(object)c, elementsToImport, exportedRaceId, exportedJobId);
			}
		}

		// Handle purchasable feats: remove if "Include Player Level" or "Include Acquired Feats" is unchecked
		bool includeLevel = ModConfig.GetOption("includePlayerLevel")?.Value == true;
		bool includeFeats = ModConfig.GetOption("includeAcquiredFeats")?.Value == true;
		if (!includeLevel || !includeFeats)
		{
			// Get current race/job's elementMaps to check for starting feats
			string currentRaceId = ((Card)c).c_idRace;
			string currentJobId = ((Card)c).c_idJob;
			Dictionary<int, int> raceElementMap = null;
			Dictionary<int, int> jobElementMap = null;
			if (!string.IsNullOrEmpty(currentRaceId) && EClass.sources.races.map.TryGetValue(currentRaceId, out SourceRace.Row raceRow))
			{
				raceElementMap = raceRow.elementMap;
			}
			if (!string.IsNullOrEmpty(currentJobId) && EClass.sources.jobs.map.TryGetValue(currentJobId, out SourceJob.Row jobRow))
			{
				jobElementMap = jobRow.elementMap;
			}

			foreach (ElementData elementData in dumpData.charaElements)
			{
				SourceElement.Row sourceRow = null;
				if (EClass.sources.elements.map.TryGetValue(elementData.id, out sourceRow))
				{
					if (IsPurchasableFeat(sourceRow))
					{
						// Calculate base tier from race/job (use higher if feat appears in both)
						int baseTier = 0;
						if (raceElementMap != null && raceElementMap.TryGetValue(elementData.id, out int raceTier))
						{
							baseTier = System.Math.Max(baseTier, raceTier);
						}
						if (jobElementMap != null && jobElementMap.TryGetValue(elementData.id, out int jobTier))
						{
							baseTier = System.Math.Max(baseTier, jobTier);
						}

						// vBase = purchased portion only (race/job contribution is in vSource, not exported)
						// If vBase > 0, player purchased levels that should be stripped
						int purchasedLevels = elementData.vBase;

						// Skip if no purchased levels to strip
						if (purchasedLevels <= 0)
						{
							continue;
						}

						// Refund feat points only if BOTH:
						// - "Include Player Level" is true (they had levels/points)
						// - "Include Acquired Feats" is false (we're removing purchased feats)
						// If level is unchecked, they never had the points to begin with, so don't refund
						if (includeLevel && !includeFeats)
						{
							// Calculate per-tier refund for the purchased levels
							// purchasedLevels represents how many tiers above race/job base were bought
							// We need to refund cost for tiers (baseTier+1) through (baseTier+purchasedLevels)
							int refund = 0;
							if (sourceRow.cost != null && sourceRow.cost.Length > 0)
							{
								for (int tier = baseTier + 1; tier <= baseTier + purchasedLevels; tier++)
								{
									// Use TryGet pattern: cost[tier-1], fallback to last element if out of range
									int costIndex = System.Math.Min(tier - 1, sourceRow.cost.Length - 1);
									int tierCost = sourceRow.cost[costIndex];
									if (tierCost > 0)
									{
										refund += tierCost;
									}
								}
							}
							((Card)c).feat += refund;
						}

						// Downgrade to base tier (or remove if baseTier is 0)
						c.SetFeat(elementData.id, baseTier);
					}
				}
			}
		}

		// Import extra body parts (from Chaos Shape feat, etc.) if Include Acquired Feats is enabled
		// This must be done after feats are imported because Chaos Shape adds body parts
		if (includeFeats && dumpData.charaBodyParts != null && dumpData.charaBodyParts.Count > 0)
		{
			ImportBodyParts(c, dumpData.charaBodyParts);
		}

		if (ModConfig.GetOption("includeBank")?.Value == true && dumpData.bankItems != null && dumpData.bankItems.Count > 0)
		{
			Card bankContainer = (Card)EClass.game.cards.container_deposit;
			if (bankContainer != null && bankContainer.IsContainer)
			{
				foreach (ThingData bankItemData in dumpData.bankItems)
				{
					try
					{
						Card bankItem = ThingUtils.RestoreThingFromData(bankItemData);
						if (bankItem != null)
						{
							StorageAuto.InsertToSubContainer(bankContainer, bankItem.Thing);
						}
					}
					catch (System.Exception)
					{
						// Item failed to import, continue with next
					}
				}
			}
		}

		ImportThingConfig(dumpData, c);

		// Import corruption and corruption history (must be before mutations)
		if (dumpData.c_corruptionHistory != null && dumpData.c_corruptionHistory.Count > 0)
		{
			c.c_corruptionHistory = dumpData.c_corruptionHistory.ToList();
		}
		c.corruption = dumpData.corruption;

		// Import tempElements (temporary modifiers)
		if (dumpData.tempElements != null && dumpData.tempElements.Count > 0)
		{
			if (c.tempElements == null)
			{
				c.tempElements = new ElementContainer();
				c.tempElements.SetParent(c);
			}
			foreach (ElementData elementData in dumpData.tempElements)
			{
				c.tempElements.SetBase(elementData.id, elementData.vBase, 0);
			}
		}

		// Import faith: goddess and days with god (piety will be recalculated from these)
		// Note: element 85 (piety) must be imported before this via charaElements above
		if (ModConfig.GetOption("includePiety")?.Value == true && !string.IsNullOrEmpty(dumpData.charaIdFaith))
		{
			c.idFaith = dumpData.charaIdFaith;
			((Card)c).c_daysWithGod = dumpData.charaDaysWithGod;
			// Recalculate faith elements from piety (requires element 306/85 which may or may not be imported)
			c.RefreshFaithElement();
		}

		// Conditions are always cured via HealAll() - never import them for new game plus

		// Import mutations (ether disease feats)
		if (dumpData.mutations != null && dumpData.mutations.Count > 0 && ModConfig.GetOption("cureDiseases")?.Value != true)
		{
			foreach (MutationData mutationData in dumpData.mutations)
			{
				// Apply mutation via SetFeat
				c.SetFeat(mutationData.featId, mutationData.value);
			}
		}

		c.CalculateMaxStamina();
		// Always set HP to max to account for level ups
		c.hp = c.MaxHP;

		// Refresh character state and UI
		c.Refresh();
		if (c.IsPC)
		{
			LayerChara.Refresh();
			WidgetEquip.SetDirty();
		}

		if (ModConfig.GetOption("cureDiseases")?.Value == true)
		{
			c.ModCorruption(-100000);
			// Clear temp modifier debuffs from ether disease (Temporary Weakness etc.)
			// CureTempElements only reduces negative temp elements; does not touch corruption/disease state
			c.CureTempElements(100, body: true, mind: true);
		}

		if (ModConfig.GetOption("cureMutations")?.Value == true)
		{
			bool skillsExcluded = ModConfig.GetOption("includeSkills")?.Value != true;
			bool attributesExcluded = ModConfig.GetOption("includeAttributes")?.Value != true;

			// Snapshot vBase of excluded elements before curing mutations.
			// When "Include Skills" or "Include Attributes" is off, those elements were not
			// imported from the dump, so their vBase is at new-character defaults. Curing a
			// mutation calls Apply(-value) which subtracts stat bonuses that were never applied
			// on this character, driving vBase negative. Restoring from the snapshot prevents that.
			Dictionary<int, int> excludedSnapshot = null;
			if (skillsExcluded || attributesExcluded)
			{
				excludedSnapshot = new Dictionary<int, int>();
				foreach (var kvp in c.elements.dict)
				{
					string cat = kvp.Value.source?.category;
					if ((cat == "skill" && skillsExcluded) || (cat == "attribute" && attributesExcluded))
					{
						excludedSnapshot[kvp.Key] = kvp.Value.vBase;
					}
				}
			}

			// Cure non-ether mutations (category == "mutation") - removes the feat/label only
			foreach (Element element in c.elements.dict.Values.ToList())
			{
				if (element.source?.category == "mutation" && element.Value != 0)
				{
					c.SetFeat(element.id, 0);
				}
			}

			// Restore excluded elements to their pre-cure vBase
			if (excludedSnapshot != null)
			{
				foreach (var kvp in excludedSnapshot)
				{
					Element el = c.elements.GetElement(kvp.Key);
					if (el != null && el.vBase != kvp.Value)
					{
						c.elements.ModBase(kvp.Key, kvp.Value - el.vBase);
					}
				}

				// Elements in excluded categories that did not exist before the cure (so not in
				// snapshot) can be created by the cure with negative vBase. Restore them to 0.
				foreach (Element el in c.elements.dict.Values.ToList())
				{
					string cat = el.source?.category;
					bool excluded = (cat == "skill" && skillsExcluded) || (cat == "attribute" && attributesExcluded);
					if (excluded && el.vBase < 0 && !excludedSnapshot.ContainsKey(el.id))
					{
						c.elements.ModBase(el.id, -el.vBase);
					}
				}
			}
		}

		// Use CureType.Heal - cures conditions/buffs/debuffs, does not clear tempElements
		c.Cure(CureType.Heal, 100);
		// Manually set HP/mana/stamina
		c.hp = c.MaxHP;
		c.mana.value = c.mana.max / 2;
		c.stamina.value = c.stamina.max / 2;
	}

	private static void ImportElementConfig(Card card, List<ElementData> elements, string exportedRaceId, string exportedJobId)
	{
		if (elements == null || elements.Count == 0)
		{
			return;
		}

		Chara c = (Chara)(object)card.Thing;
		if (c == null || string.IsNullOrEmpty(exportedRaceId) || string.IsNullOrEmpty(exportedJobId))
		{
			// Fallback to simple import if not a character or missing race/job info
			foreach (ElementData elementData in elements)
			{
				Element val = ((ElementContainer)card.elements).SetBase(elementData.id, elementData.vBase, elementData.vPotential);
				val.vExp = elementData.vExp;
				val.vTempPotential = elementData.vTempPotential;
			}
			return;
		}

		// Get current race/job IDs (already set by ImportBio to desired race/class)
		string currentRaceId = ((Card)c).c_idRace;
		string currentJobId = ((Card)c).c_idJob;

		// Step 1: Remove current race/class bonuses (only affects vSource, vBase stays)
		if (!string.IsNullOrEmpty(currentRaceId) && EClass.sources.races.map.ContainsKey(currentRaceId))
		{
			c.ApplyRace(remove: true);
		}
		if (!string.IsNullOrEmpty(currentJobId) && EClass.sources.jobs.map.ContainsKey(currentJobId))
		{
			c.ApplyJob(remove: true);
		}

		// Step 3: Apply exported race/class bonuses (adds vSource from exported race/class)
		if (EClass.sources.races.map.ContainsKey(exportedRaceId))
		{
			string tempRace = ((Card)c).c_idRace;
			((Card)c).c_idRace = exportedRaceId;
			c._race = null;
			c.ApplyRace(remove: false);
			((Card)c).c_idRace = tempRace;
			c._race = null;
		}
		if (EClass.sources.jobs.map.ContainsKey(exportedJobId))
		{
			string tempJob = ((Card)c).c_idJob;
			((Card)c).c_idJob = exportedJobId;
			c._job = null;
			c.ApplyJob(remove: false);
			((Card)c).c_idJob = tempJob;
			c._job = null;
		}

		// Step 2: Import exported vBase values (which include training from exported character)
		// This sets vBase to the exported values
		foreach (ElementData elementData in elements)
		{
			Element val = ((ElementContainer)card.elements).SetBase(elementData.id, elementData.vBase, elementData.vPotential);
			val.vExp = elementData.vExp;
			val.vTempPotential = elementData.vTempPotential;
		}

		// Step 3: Remove exported race/class bonuses
		if (EClass.sources.races.map.ContainsKey(exportedRaceId))
		{
			string tempRace = ((Card)c).c_idRace;
			((Card)c).c_idRace = exportedRaceId;
			c._race = null;
			c.ApplyRace(remove: true);
			((Card)c).c_idRace = tempRace;
			c._race = null;
		}
		if (EClass.sources.jobs.map.ContainsKey(exportedJobId))
		{
			string tempJob = ((Card)c).c_idJob;
			((Card)c).c_idJob = exportedJobId;
			c._job = null;
			c.ApplyJob(remove: true);
			((Card)c).c_idJob = tempJob;
			c._job = null;
		}

		// Step 4: Restore current race/class bonuses (desired race/class from ImportBio)
		if (!string.IsNullOrEmpty(currentRaceId) && EClass.sources.races.map.ContainsKey(currentRaceId))
		{
			c.ApplyRace(remove: false);
		}
		if (!string.IsNullOrEmpty(currentJobId) && EClass.sources.jobs.map.ContainsKey(currentJobId))
		{
			c.ApplyJob(remove: false);
		}
	}

	private static void ImportRecipeConfig(List<RecipeData> recipes)
	{
		if (recipes != null)
		{
			foreach (RecipeData recipe in recipes)
			{
				RecipeSource val = RecipeManager.Get(recipe.id);
				if (val != null && !EClass.player.recipes.knownRecipes.ContainsKey(recipe.id))
				{
					for (int k = 0; k < recipe.count; k++)
					{
						EClass.player.recipes.Add(recipe.id, true);
					}
				}
			}
		}
	}

	private static void ImportThingConfig(CharacterDumpData dumpData, Chara c)
	{
		// Map old UIDs (from export) to new UIDs (after spawning containers)
		Dictionary<int, int> oldUidToNewUid = new Dictionary<int, int>();

		// Clear equipment and toolbelt before importing (if Equipment is checked)
		if (ModConfig.GetOption("includeWornEquipment")?.Value == true)
		{
			// Clear only known equipment slots (30-43), not special slots like Toolbelt (44) or AccessorySlot (45)
			foreach (BodySlot slot in c.body.slots)
			{
				if (slot.thing != null)
				{
					// Only clear actual equipment slots, not special container slots
					if (slot.elementId >= ItemSlotManager.BodySlot.Head && slot.elementId <= ItemSlotManager.BodySlot.Token)
					{
						c.body.Unequip(slot, refresh: false);
					}
				}
			}

			// Clear toolbelt container contents (but not the container itself)
			Card toolbeltContainer = ItemSlotManager.GetToolbeltContainer(c);
			if (toolbeltContainer != null)
			{
				toolbeltContainer.things.DestroyAll((Func<Thing, bool>)null);
			}
		}

		// 1. Toolbar items (if importIncludeToolbar enabled). Empty slots null
		if (ModConfig.GetOption("includeToolbar")?.Value == true && dumpData.toolbarItems != null)
		{
			int n = System.Math.Min(dumpData.toolbarItems.Count, ItemSlotManager.ToolbarSlotCount);
			for (int i = 0; i < n; i++)
			{
				ThingData item = dumpData.toolbarItems[i];
				if (item == null) continue;
				try
				{
					StorageFixed.SpawnToToolbar(c, item, i);
				}
				catch (System.Exception)
				{
					// Item failed to spawn, continue with next
				}
			}
		}

		// 2. Toolbelt items (if importIncludeWornEquipment enabled - toolbelt is part of equipment)
		if (ModConfig.GetOption("includeWornEquipment")?.Value == true && dumpData.toolbeltItems != null && dumpData.toolbeltItems.Count > 0)
		{
			Card toolbeltContainer = ItemSlotManager.GetToolbeltContainer(c);
			if (toolbeltContainer != null)
			{
				for (int i = 0; i < dumpData.toolbeltItems.Count && i < ItemSlotManager.AccessorySlotCount; i++)
				{
					try
					{
						// Create the item first to check if it's a container and map UIDs
						Card spawnedCard = ThingUtils.RestoreThingFromData(dumpData.toolbeltItems[i]);
						if (spawnedCard == null)
						{
							continue;
						}

						// Spawn the item into toolbelt
						int slot = StorageAuto.InsertToSubContainer(toolbeltContainer, spawnedCard.Thing);
						if (slot >= 0)
						{
							// If this is a container, map old UID to new UID (use exported UID from ThingData)
							if (spawnedCard.IsContainer && dumpData.toolbeltItems[i].containerUid.HasValue)
							{
								int oldUid = dumpData.toolbeltItems[i].containerUid.Value;
								if (spawnedCard._ints != null && spawnedCard._ints.Length > 1)
								{
									int newUid = spawnedCard._ints[CardIntsIndices.UidOrType];
									oldUidToNewUid[oldUid] = newUid;
								}
							}
						}
					}
					catch (System.Exception)
					{
						// Item failed to spawn, continue with next
					}
				}
			}
		}

		// 3. Worn equipment (if importIncludeWornEquipment enabled)
		if (ModConfig.GetOption("includeWornEquipment")?.Value == true && dumpData.wornEquipment != null && dumpData.wornEquipment.Count > 0)
		{
			foreach (ThingData thingData in dumpData.wornEquipment)
			{
				if (!thingData.slotElementId.HasValue || !thingData.slotIndex.HasValue)
				{
					continue;
				}

				try
				{
					// Create the item first to check if it's a container and map UIDs
					Card spawnedCard = ThingUtils.RestoreThingFromData(thingData);
					if (spawnedCard == null)
					{
						continue;
					}

					// Equip the item
					StorageFixed.InsertToEquipment(c, spawnedCard.Thing, thingData.slotElementId.Value, thingData.slotIndex.Value);

					// If this is a container, map old UID to new UID (use exported UID from ThingData)
					if (spawnedCard.IsContainer && thingData.containerUid.HasValue)
					{
						int oldUid = thingData.containerUid.Value;
						if (spawnedCard._ints != null && spawnedCard._ints.Length > CardIntsIndices.UidOrType)
						{
							int newUid = spawnedCard._ints[CardIntsIndices.UidOrType];
							oldUidToNewUid[oldUid] = newUid;
						}
					}
				}
				catch (System.Exception)
				{
					// If slot doesn't exist (body part missing), put item in inventory instead
					try
					{
						StorageAuto.SpawnToInventory(c, thingData);
					}
					catch (System.Exception)
					{
						// Item failed to place in inventory, continue with next
					}
				}
			}
		}

		// 4. Container contents (if importIncludeBackpackContents enabled)
		// Note: Containers (including Toolbelt and AccessorySlot) are now included in wornEquipment, not handled separately
		if (ModConfig.GetOption("includeBackpackContents")?.Value == true && dumpData.containerContents != null && dumpData.containerContents.Count > 0)
		{
			// Build map of containers that exist on the player by UID
			// Check both equipped containers (body.slots) AND toolbelt container AND inventory containers
			Dictionary<int, Card> uidMap = new Dictionary<int, Card>();
			foreach (BodySlot slot in c.body.slots)
			{
				if (slot.thing != null && slot.thing.IsContainer)
				{
					Card container = (Card)slot.thing;
					// Card.uid is stored in _ints[CardIntsIndices.UidOrType]
					if (container._ints != null && container._ints.Length > CardIntsIndices.UidOrType)
					{
						int uid = container._ints[CardIntsIndices.UidOrType];
						uidMap[uid] = container;
					}
				}
			}

			// Also check toolbelt container (it's not equipped, it just exists)
			Card toolbeltContainer = ItemSlotManager.GetToolbeltContainer(c);
			if (toolbeltContainer != null && toolbeltContainer._ints != null && toolbeltContainer._ints.Length > 1)
			{
				int toolbeltUid = toolbeltContainer._ints[CardIntsIndices.UidOrType];
				if (!uidMap.ContainsKey(toolbeltUid))
				{
					uidMap[toolbeltUid] = toolbeltContainer;
				}
			}

			// Also search inventory for containers (containers in inventory need their UIDs tracked)
			Card charaCard = (Card)c;

			System.Action<Thing> addInventoryContainersToMap = (Thing t) =>
			{
				if (t.IsContainer)
				{
					Card container = (Card)t;
					// Only add if not already in map (equipped containers already added above)
					if (container._ints != null && container._ints.Length > 1)
					{
						int uid = container._ints[CardIntsIndices.UidOrType];
						if (!uidMap.ContainsKey(uid))
						{
							uidMap[uid] = container;
						}
					}
				}
			};

			charaCard.things.Foreach(addInventoryContainersToMap, onlyAccessible: false);

			// Recursively search all containers for nested containers (e.g., purse in toolbelt)
			// Use a queue to process containers as we find them
			Queue<Card> containersToSearch = new Queue<Card>(uidMap.Values);
			while (containersToSearch.Count > 0)
			{
				Card container = containersToSearch.Dequeue();
				if (container == null || !container.IsContainer) continue;

				// Search inside this container for nested containers
				System.Action<Thing> addNestedContainersToMap = (Thing t) =>
				{
					if (t.IsContainer)
					{
						Card nestedContainer = (Card)t;
						if (nestedContainer._ints != null && nestedContainer._ints.Length > 1)
						{
							int nestedUid = nestedContainer._ints[CardIntsIndices.UidOrType];
							if (!uidMap.ContainsKey(nestedUid))
							{
								uidMap[nestedUid] = nestedContainer;
								containersToSearch.Enqueue(nestedContainer); // Search inside this one too
							}
						}
					}
				};

				container.things.Foreach(addNestedContainersToMap, onlyAccessible: false);
			}

			// First pass: Import player inventory items (UID 0) by direct AddThing to a free slot
			// Pick()/GetDest() can return invalid here, so bypass and place directly
			foreach (ContainerItemData containerItemData in dumpData.containerContents)
			{
				if (containerItemData.parentContainerUid == 0)
				{
					Card spawnedCard = ThingUtils.RestoreThingFromData(containerItemData.item);
					if (spawnedCard != null)
					{
						List<int> occupied = StorageAuto.GetOccupiedInventorySlots(c);
						int freeSlot = 0;
						while (freeSlot < ItemSlotManager.PlayerInventoryMaxSlots && occupied.Contains(freeSlot))
							freeSlot++;

						bool placedOnChara = false;
						if (freeSlot < ItemSlotManager.PlayerInventoryMaxSlots)
						{
							if (EClass._zone != null && c.pos != null && c.pos.IsValid)
								EClass._zone.AddCard(spawnedCard, c.pos);
							c.AddThing(spawnedCard.Thing, tryStack: true, destInvX: freeSlot, destInvY: -1);
							Card itemCard = (Card)spawnedCard.Thing;
							placedOnChara = (itemCard.parent == c);
							if (placedOnChara && itemCard.invX < 0)
							{
								itemCard.invX = freeSlot;
								itemCard.invY = ItemSlotManager.InvY.Inventory;
							}
						}

						if (!placedOnChara && EClass._zone != null && c.pos != null && c.pos.IsValid)
							EClass._zone.AddCard(spawnedCard, c.pos);

						if (spawnedCard.IsContainer && containerItemData.item.containerUid.HasValue)
						{
							int containerOldUid = containerItemData.item.containerUid.Value;
							if (spawnedCard._ints != null && spawnedCard._ints.Length > CardIntsIndices.UidOrType)
							{
								int containerNewUid = spawnedCard._ints[CardIntsIndices.UidOrType];
								oldUidToNewUid[containerOldUid] = containerNewUid;
								if (!placedOnChara)
									uidMap[containerNewUid] = spawnedCard;
							}
						}
					}
				}
			}

			// Rebuild uidMap to include newly imported containers from inventory
			System.Action<Thing> rebuildUidMapFromInventory = (Thing t) =>
			{
				if (t.IsContainer)
				{
					Card container = (Card)t;
					if (container._ints != null && container._ints.Length > 1)
					{
						int uid = container._ints[CardIntsIndices.UidOrType];
						if (!uidMap.ContainsKey(uid))
						{
							uidMap[uid] = container;
						}
					}
				}
			};

			charaCard.things.Foreach(rebuildUidMapFromInventory, onlyAccessible: false);

			// Second pass: Import items that go into containers (non-UID 0 items)
			foreach (ContainerItemData containerItemData in dumpData.containerContents)
			{
				// Skip UID 0 items (already imported in first pass)
				if (containerItemData.parentContainerUid == 0)
				{
					continue;
				}

				// Translate old UID to new UID using the mapping
				int targetUid = containerItemData.parentContainerUid;
				if (oldUidToNewUid.ContainsKey(containerItemData.parentContainerUid))
				{
					targetUid = oldUidToNewUid[containerItemData.parentContainerUid];
				}

				if (uidMap.ContainsKey(targetUid))
				{
					// Item belongs in a container (toolbelt, purse, etc.)
					Card container = uidMap[targetUid];
					StorageAuto.SpawnToSubContainer(container, containerItemData.item);
				}
			}
		}
		else if (ModConfig.GetOption("includeWornEquipment")?.Value == true && ModConfig.GetOption("includeBackpackContents")?.Value != true && dumpData.containerContents != null && dumpData.containerContents.Count > 0)
		{
			// Equipment is enabled but container contents is disabled - handle money items (orens, gold, platinum)
			// Find a purse container if one exists
			Card purseContainer = null;
			Card charaCard = (Card)c;

			System.Action<Thing> findPurseInInventory = (Thing t) =>
			{
				Card thingCard = (Card)t;
				if (thingCard.IsContainer && thingCard.id == "purse")
				{
					purseContainer = thingCard;
				}
			};

			charaCard.things.Foreach(findPurseInInventory, onlyAccessible: false);

			// Also check inside toolbelt and equipped containers for purses
			if (purseContainer == null)
			{
				Card toolbeltContainer = ItemSlotManager.GetToolbeltContainer(c);
				if (toolbeltContainer != null)
				{
					System.Action<Thing> findPurseInToolbelt = (Thing t) =>
					{
						Card thingCard = (Card)t;
						if (thingCard.IsContainer && thingCard.id == "purse")
						{
							purseContainer = thingCard;
						}
					};

					toolbeltContainer.things.Foreach(findPurseInToolbelt, onlyAccessible: false);
				}

				foreach (BodySlot slot in c.body.slots)
				{
					if (slot.thing != null && slot.thing.IsContainer)
					{
						Card container = (Card)slot.thing;
						if (container.id == "purse")
						{
							purseContainer = container;
							break;
						}
					}
				}
			}

			// Process money items from container contents
			foreach (ContainerItemData containerItemData in dumpData.containerContents)
			{
				string itemId = containerItemData.item.id;
				// Check if this is a money item
				if (itemId == "money" || itemId == "money2" || itemId == "plat")
				{
					if (purseContainer != null)
					{
						// Place in purse
						StorageAuto.SpawnToSubContainer(purseContainer, containerItemData.item);
					}
					else
					{
						// Dump into inventory
						StorageAuto.SpawnToInventory(c, containerItemData.item);
					}
				}
			}
		}
	}

	/// <summary>
	/// Checks if a feat is purchasable (can be bought with feat points in the feat purchase UI).
	/// Based on the same criteria as Chara.ListAvailabeFeats().
	/// </summary>
	private static bool IsPurchasableFeat(SourceElement.Row source)
	{
		if (source == null)
		{
			return false;
		}

		// Must be in FEAT group
		if (source.group != "FEAT")
		{
			return false;
		}

		// Must have a cost (cost[0] != -1 means purchasable)
		if (source.cost == null || source.cost.Length == 0 || source.cost[0] == -1)
		{
			return false;
		}

		// Must have a categorySub (required for purchasable feats)
		if (string.IsNullOrEmpty(source.categorySub))
		{
			return false;
		}

		// Must not have tags that exclude it from purchase
		if (source.tag != null && source.tag.Length > 0)
		{
			foreach (string tag in source.tag)
			{
				string tagLower = tag.ToLower();
				if (tagLower == "class" || tagLower == "hidden" || tagLower == "innate")
				{
					return false;
				}
			}
		}

		return true;
	}

	/// <summary>
	/// Imports extra body parts (from Chaos Shape feat, etc.) that were gained beyond the race baseline.
	/// Compares exported body parts with current race's baseline and adds any extras.
	/// </summary>
	private static void ImportBodyParts(Chara c, List<int> exportedBodyParts)
	{
		if (exportedBodyParts == null || exportedBodyParts.Count == 0)
		{
			return;
		}

		// Get current body parts (after race has been applied)
		HashSet<int> validBodySlotIds = ItemSlotManager.GetValidBodySlotElementIds().ToHashSet();

		// Count how many of each body part type the character currently has
		Dictionary<int, int> currentPartCounts = new Dictionary<int, int>();
		foreach (BodySlot slot in c.body.slots)
		{
			if (validBodySlotIds.Contains(slot.elementId))
			{
				if (!currentPartCounts.ContainsKey(slot.elementId))
				{
					currentPartCounts[slot.elementId] = 0;
				}
				currentPartCounts[slot.elementId]++;
			}
		}

		// Count how many of each body part type were exported
		Dictionary<int, int> exportedPartCounts = new Dictionary<int, int>();
		foreach (int elementId in exportedBodyParts)
		{
			if (validBodySlotIds.Contains(elementId))
			{
				if (!exportedPartCounts.ContainsKey(elementId))
				{
					exportedPartCounts[elementId] = 0;
				}
				exportedPartCounts[elementId]++;
			}
		}

		// Add any extra body parts that were exported but not present
		bool addedParts = false;
		foreach (KeyValuePair<int, int> exported in exportedPartCounts)
		{
			int elementId = exported.Key;
			int exportedCount = exported.Value;
			int currentCount = currentPartCounts.ContainsKey(elementId) ? currentPartCounts[elementId] : 0;

			int partsToAdd = exportedCount - currentCount;
			for (int i = 0; i < partsToAdd; i++)
			{
				c.body.AddBodyPart(elementId);
				addedParts = true;
			}
		}

		// Refresh body parts if we added any
		if (addedParts)
		{
			c.body.RefreshBodyParts();
			if (c.IsPC && WidgetEquip.Instance != null)
			{
				WidgetEquip.Instance.Rebuild();
			}
		}
	}
}
