using System;
using System.Collections.Generic;
using System.Linq;

namespace NewGamePlus;

public static class CharacterExporter
{
	public static List<ThingData> ExportBankItems()
	{
		List<ThingData> bankItems = new List<ThingData>();
		if (EClass.game?.cards?.container_deposit != null)
		{
			Card bankContainer = (Card)EClass.game.cards.container_deposit;
			if (bankContainer != null && bankContainer.IsContainer)
			{
				Action<Thing> exportBankItem = (Thing t) =>
				{
					var thingData = ThingUtils.CreateThingData(t, null);
					if (thingData != null)
					{
						bankItems.Add(thingData);
					}
				};

				bankContainer.things.Foreach(exportBankItem, onlyAccessible: false);
			}
		}
		return bankItems;
	}

	public static ItemExportResult ExportAllItems(Chara c)
	{
		ItemExportResult result = new ItemExportResult();
		Card charaCard = (Card)c;

		// Step 1: Build equipped item map from body.slots
		// Note: Player inventory is directly on the character (no separate backpack container)
		Dictionary<Thing, BodySlotInfo> equippedMap = new Dictionary<Thing, BodySlotInfo>();
		int equippedCount = 0;
		foreach (BodySlot slot in c.body.slots)
		{
			if (slot.thing != null)
			{
				// Only exclude the toolbelt container itself (Toolbelt), include everything else from Head to AccessorySlot
				if (slot.elementId != ItemSlotManager.BodySlot.Toolbelt && (slot.elementId >= ItemSlotManager.BodySlot.Head && slot.elementId <= ItemSlotManager.BodySlot.AccessorySlot))
				{
					equippedMap[slot.thing] = new BodySlotInfo
					{
						elementId = slot.elementId,
						slotIndex = slot.index
					};
					equippedCount++;
				}
			}
		}

		// Step 2: Build container UID map for looking up parent containers during categorization
		Dictionary<Card, int> containerUidMap = new Dictionary<Card, int>();
		int containerCount = 0;

		Action<Thing> buildContainerUidMap = (Thing t) =>
		{
			if (t.IsContainer)
			{
				Card containerCard = (Card)t;
				if (containerCard._ints != null && containerCard._ints.Length > 1)
				{
					int uid = containerCard._ints[CardIntsIndices.UidOrType];
					if (!containerUidMap.ContainsKey(containerCard))
					{
						containerUidMap[containerCard] = uid;
						containerCount++;
					}
				}
			}
		};

		charaCard.things.Foreach(buildContainerUidMap, onlyAccessible: false);

		// Step 3: Categorize all items using ItemLocationHelper.ClassifyItem()
		Action<Thing> categorizeItem = (Thing t) =>
		{
			Card thingCard = (Card)t;
			ItemLocation location = ItemLocationHelper.ClassifyItem(t, c);
			var thingData = ThingUtils.CreateThingData(t, null);

			if (thingData == null) return;

			switch (location)
			{
				case ItemLocation.Equipment:
					// Equipped items - get slot info from equippedMap
					if (equippedMap.ContainsKey(t))
					{
						BodySlotInfo slotInfo = equippedMap[t];
						thingData = ThingUtils.CreateThingData(t, slotInfo.elementId, slotInfo.slotIndex);
						if (thingData != null)
						{
							result.wornEquipment.Add(thingData);
						}
					}
					break;

				case ItemLocation.Toolbar:
					result.toolbarItems.Add(thingData);
					break;

				case ItemLocation.Toolbelt:
					// Toolbelt items - add to separate toolbeltItems list
					result.toolbeltItems.Add(thingData);
					break;

				case ItemLocation.Inventory:
					// Player inventory item (35 slots, invY == 0)
					// Use UID 0 to indicate player inventory (no container)
					result.containerContents.Add(new ContainerItemData
					{
						item = thingData,
						parentContainerUid = 0 // Special marker for player inventory
					});
					break;

				case ItemLocation.ContainerContents:
					// Item in a container - lookup parent UID from containerUidMap
					Card parentCard = t.parent as Card;
					if (parentCard != null)
					{
						if (containerUidMap.ContainsKey(parentCard))
						{
							int parentContainerUid = containerUidMap[parentCard];
							result.containerContents.Add(new ContainerItemData
							{
								item = thingData,
								parentContainerUid = parentContainerUid
							});
						}
					}
					break;

				case ItemLocation.NonPlayer:
				case ItemLocation.Unknown:
					// Skip items not belonging to player or unknown location
					break;
			}
		};

		charaCard.things.Foreach(categorizeItem, onlyAccessible: false);

		return result;
	}

	public static List<int> ExportBodyParts(List<BodySlot> slots)
	{
		List<int> bodyParts = new List<int>();
		foreach (BodySlot slot in slots)
		{
			int elementId = slot.elementId;
			if (elementId != ItemSlotManager.BodySlot.Toolbelt && elementId != ItemSlotManager.BodySlot.AccessorySlot)
			{
				// Only include valid body slot element IDs
				if (ItemSlotManager.GetValidBodySlotElementIds().Contains(elementId))
				{
					bodyParts.Add(elementId);
				}
			}
		}
		return bodyParts;
	}

	public static List<ElementData> ExportTempElements(Chara c)
	{
		List<ElementData> elements = new List<ElementData>();
		if (c.tempElements != null && c.tempElements.dict.Count > 0)
		{
			foreach (Element element in c.tempElements.dict.Values)
			{
				if (element.vBase != 0)
				{
					elements.Add(new ElementData
					{
						id = element.id,
						vBase = element.vBase,
						vExp = 0,
						vPotential = 0,
						vTempPotential = 0
					});
				}
			}
		}
		return elements;
	}

	public static List<ElementData> ExportFaithElements(Chara c)
	{
		List<ElementData> elements = new List<ElementData>();
		if (c.faithElements != null && c.faithElements.dict.Count > 0)
		{
			foreach (Element element in c.faithElements.dict.Values)
			{
				if (element.vBase != 0)
				{
					elements.Add(new ElementData
					{
						id = element.id,
						vBase = element.vBase,
						vExp = 0,
						vPotential = 0,
						vTempPotential = 0
					});
				}
			}
		}
		return elements;
	}

	public static List<ElementData> ExportWorkElements(Chara c)
	{
		List<ElementData> elements = new List<ElementData>();
		if (c.workElements != null && c.workElements.dict.Count > 0)
		{
			foreach (Element element in c.workElements.dict.Values)
			{
				if (element.vBase != 0)
				{
					elements.Add(new ElementData
					{
						id = element.id,
						vBase = element.vBase,
						vExp = 0,
						vPotential = 0,
						vTempPotential = 0
					});
				}
			}
		}
		return elements;
	}

	public static List<ConditionData> ExportConditions(Chara c)
	{
		List<ConditionData> conditions = new List<ConditionData>();
		foreach (Condition condition in c.conditions)
		{
			string condId = "unknown";
			if (condition.source != null)
			{
				if (!string.IsNullOrEmpty(condition.source.alias))
				{
					condId = condition.source.alias;
				}
				else if (condition.source.id != 0)
				{
					condId = condition.source.id.ToString();
				}
			}

			var condData = new ConditionData
			{
				id = condId,
				power = condition.power,
				value = condition.value,
				refVal = condition.refVal,
				refVal2 = condition.refVal2,
				elements = new List<ElementData>()
			};

			var condElements = condition.GetElementContainer();
			if (condElements != null && condElements.dict.Count > 0)
			{
				foreach (Element element in condElements.dict.Values)
				{
					if (element.vBase != 0)
					{
						condData.elements.Add(new ElementData
						{
							id = element.id,
							vBase = element.vBase,
							vExp = 0,
							vPotential = 0,
							vTempPotential = 0
						});
					}
				}
			}

			conditions.Add(condData);
		}
		return conditions;
	}

	public static List<MutationData> ExportMutations(Chara c)
	{
		List<MutationData> mutations = new List<MutationData>();
		foreach (Element element in c.elements.dict.Values)
		{
			if (element.source?.category == "ether" && element.Value != 0)
			{
				mutations.Add(new MutationData
				{
					featId = element.id,
					value = element.Value
				});
			}
		}
		return mutations;
	}
}
