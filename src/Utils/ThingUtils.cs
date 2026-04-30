using System;
using System.Collections.Generic;

namespace NewGamePlus;

public static class ThingUtils
{
	public static List<ElementData> ExportElementConfig(Card card)
	{
		List<ElementData> elements = new List<ElementData>();
		foreach (Element value in ((ElementContainer)card.elements).dict.Values)
		{
			// Export all elements - empty elements (all zeros) are automatically removed on import by SetBase()
			elements.Add(new ElementData
			{
				id = value.id,
				vBase = value.vBase,
				vExp = value.vExp,
				vPotential = value.vPotential,
				vTempPotential = value.vTempPotential
				// DO NOT export: vSource, vSourcePotential, vLink (recalculated on import)
			});
		}
		return elements;
	}

	public static ThingData CreateThingData(Thing thing, int? slotElementId = null, int? slotIndex = null)
	{
		if (((Card)thing).trait is TraitToolBelt || ((Card)thing).trait is TraitTutorialBook || ((Card)thing).trait is TraitAbility)
		{
			return null;
		}

		Card cardThing = (Card)thing;

		// Export container UID if this is a container
		int? containerUid = null;
		if (thing.IsContainer && cardThing._ints != null && cardThing._ints.Length > CardIntsIndices.UidOrType)
		{
			containerUid = cardThing._ints[CardIntsIndices.UidOrType];
		}

		// Export full _ints array (contains type, use count, and other item-specific data)
		int[] intsArray = null;
		if (cardThing._ints != null && cardThing._ints.Length > 0)
		{
			intsArray = new int[cardThing._ints.Length];
			Array.Copy(cardThing._ints, intsArray, cardThing._ints.Length);
		}

		// Export mapInt dictionary (contains c_dyeMat[3], c_charges[7], c_ammo[27], etc.)
		Dictionary<int, int> mapIntDict = null;
		if (cardThing.mapInt != null && cardThing.mapInt.Count > 0)
		{
			mapIntDict = new Dictionary<int, int>(cardThing.mapInt);
		}

		// Export mod sockets for ranged weapons (guns/bows): slot count = list length, 0 = empty, else elementId*1000+encLv
		List<int> socketsList = null;
		if (cardThing.IsRangedWeapon && cardThing.sockets != null && cardThing.sockets.Count > 0)
		{
			socketsList = new List<int>(cardThing.sockets);
		}

		// Export mapStr passthrough (c_altName, c_extraNameRef, c_idTalk, etc.)
		Dictionary<int, string> mapStrDict = null;
		if (cardThing.mapStr != null && cardThing.mapStr.Count > 0)
		{
			mapStrDict = new Dictionary<int, string>(cardThing.mapStr);
		}

		// Container settings/upgrades from mapObj. Read via TryGetValue to avoid triggering lazy-create on every Thing.
		var containerCaps = ExtractContainerSettings(cardThing);

		return new ThingData
		{
			id = ((Card)thing).id,
			isIdentified = ((Card)thing).IsIdentified,
			isCrafted = ((Card)thing).isCrafted,
			elements = ExportElementConfig((Card)(object)thing),
			refCardId = (((Card)thing).refCard != null) ? ((Card)thing).refCard.id : null,
			slotElementId = slotElementId,
			slotIndex = slotIndex,
			containerUid = containerUid,
			ints = intsArray,
			mapInt = mapIntDict,
			mapStr = mapStrDict,
			sockets = socketsList,
			containerUpgradeCap = containerCaps.cap,
			containerUpgradeCool = containerCaps.cool,
			windowSaveDataJson = containerCaps.windowJson
		};
	}

	internal static (int? cap, int? cool, string windowJson) ExtractContainerSettings(Card card)
	{
		int? cap = null;
		int? cool = null;
		string windowJson = null;
		if (card?.mapObj != null)
		{
			if (card.mapObj.TryGetValue(COBJ.containerUpgrade, out object upgObj) && upgObj is ContainerUpgrade cu)
			{
				cap = cu.cap;
				cool = cu.cool;
			}
			if (card.mapObj.TryGetValue(COBJ.windowSaveData, out object wsObj) && wsObj is Window.SaveData ws)
			{
				try
				{
					windowJson = Newtonsoft.Json.JsonConvert.SerializeObject(ws);
				}
				catch (Exception ex)
				{
					DebugLogger.DebugLog("ThingUtils.cs:ExtractContainerSettings", "Failed to serialize Window.SaveData", null, new Dictionary<string, object> { { "id", card.id }, { "error", ex.Message } });
				}
			}
		}
		return (cap, cool, windowJson);
	}

	internal static void ApplyContainerSettings(Card card, Dictionary<int, string> mapStr, int? upgradeCap, int? upgradeCool, string windowJson)
	{
		if (card == null) return;

		if (mapStr != null && mapStr.Count > 0)
		{
			if (card.mapStr == null) card.mapStr = new Dictionary<int, string>();
			foreach (var kvp in mapStr) card.mapStr[kvp.Key] = kvp.Value;
		}

		if (upgradeCap.HasValue || upgradeCool.HasValue)
		{
			if (card.mapObj == null) card.mapObj = new Dictionary<int, object>();
			ContainerUpgrade cu = new ContainerUpgrade
			{
				cap = upgradeCap ?? 0,
				cool = upgradeCool ?? 0
			};
			card.mapObj[COBJ.containerUpgrade] = cu;
		}

		if (!string.IsNullOrEmpty(windowJson))
		{
			try
			{
				Window.SaveData ws = Newtonsoft.Json.JsonConvert.DeserializeObject<Window.SaveData>(windowJson);
				if (ws != null)
				{
					if (card.mapObj == null) card.mapObj = new Dictionary<int, object>();
					card.mapObj[COBJ.windowSaveData] = ws;
				}
			}
			catch (Exception ex)
			{
				DebugLogger.DebugLog("ThingUtils.cs:ApplyContainerSettings", "Failed to deserialize Window.SaveData; defaults will apply", null, new Dictionary<string, object> { { "id", card.id }, { "error", ex.Message } });
			}
		}
	}

	/// <summary>
	/// Reconstructs a Card from a <see cref="ThingData"/> descriptor. Inverse of <see cref="CreateThingData"/>.
	/// </summary>
	public static Card RestoreThingFromData(ThingData thingData)
	{
		if (thingData == null) return null;
		if (thingData.ints == null || thingData.ints.Length == 0) return null;

		// Read idMaterial, lv, num from _ints array (required by ThingGen.Create)
		int idMaterial = thingData.ints.Length > CardIntsIndices.IdMaterial ? thingData.ints[CardIntsIndices.IdMaterial] : -1;
		int lv = thingData.ints.Length > CardIntsIndices.Lv ? thingData.ints[CardIntsIndices.Lv] : 1;
		int num = thingData.ints.Length > CardIntsIndices.Num ? thingData.ints[CardIntsIndices.Num] : 1;

		Card card = (Card)(object)((Card)ThingGen.Create(thingData.id, idMaterial, lv)).SetNum(num);
		if (card == null) return null;

		// Guard: if this card is somehow a Chara, warn and preserve UID to avoid cardMap corruption
		if (card.isChara)
		{
			Msg.SayRaw($"NG+: Warning, attempted to restore a Chara '{thingData.id}' via RestoreThingFromData. UID preserved.");
		}

		if (card.IsContainer)
		{
			card.things.DestroyAll((Func<Thing, bool>)null);
		}

		if (!card.IsIdentified && thingData.isIdentified)
		{
			card.c_IDTState = 0;
		}

		if (thingData.isCrafted)
		{
			card.isCrafted = true;
		}

		// Restore full _ints array (mirrors game deserialization - _ints is the source of truth)
		// Preserve the new UID assigned by ThingGen.Create to avoid registry corruption
		if (thingData.ints != null && thingData.ints.Length > 0)
		{
			int newUid = (card._ints != null && card._ints.Length > CardIntsIndices.UidOrType)
				? card._ints[CardIntsIndices.UidOrType]
				: 0;
			card._ints = new int[thingData.ints.Length];
			Array.Copy(thingData.ints, card._ints, thingData.ints.Length);
			if (newUid != 0 && card._ints.Length > CardIntsIndices.UidOrType)
			{
				card._ints[CardIntsIndices.UidOrType] = newUid;
			}
		}

		// Unpack _bits1/_bits2 from _ints (mirrors Card._OnDeserialized - game never calls ChangeMaterial on load)
		if (card._ints != null && card._ints.Length >= 3)
		{
			card._bits1.SetInt(card._ints[0]);
			card._bits2.SetInt(card._ints[2]);
		}

		if (!string.IsNullOrEmpty(thingData.refCardId))
		{
			if (EClass.sources.cards.map.ContainsKey(thingData.refCardId))
			{
				card.MakeRefFrom(EClass.sources.cards.map[thingData.refCardId].model, (Card)null);
			}
		}

		// Clear all elements first to remove orphaned elements from ThingGen.Create using wrong tier
		// ThingGen.Create may use character level to determine item tier, generating elements we don't want
		List<int> allElementIds = new List<int>();
		foreach (Element elem in ((ElementContainer)card.elements).dict.Values)
		{
			allElementIds.Add(elem.id);
		}
		foreach (int elementId in allElementIds)
		{
			((ElementContainer)card.elements).SetBase(elementId, 0, 0);
		}

		// Restore exported elements
		if (thingData.elements != null && thingData.elements.Count > 0)
		{
			foreach (ElementData elementData in thingData.elements)
			{
				Element val = ((ElementContainer)card.elements).SetBase(elementData.id, elementData.vBase, elementData.vPotential);
				val.vExp = elementData.vExp;
				val.vTempPotential = elementData.vTempPotential;
			}
		}

		// Refresh vSource from material.elementMap (mirrors Card._OnDeserialized - game does NOT call ChangeMaterial on load)
		card.ApplyMaterialElements(remove: false);

		// Restore mapInt dictionary (contains c_dyeMat[3], c_charges[7], c_ammo[27], etc.)
		if (thingData.mapInt != null && thingData.mapInt.Count > 0)
		{
			if (card.mapInt == null)
			{
				card.mapInt = new Dictionary<int, int>();
			}
			foreach (var kvp in thingData.mapInt)
			{
				card.mapInt[kvp.Key] = kvp.Value;
			}
		}

		// Restore mapStr + container settings/upgrades (mapObj). Order matters less than ensuring this happens
		// before any code path observes the container's c_* properties, which lazy-create defaults.
		ApplyContainerSettings(card, thingData.mapStr, thingData.containerUpgradeCap, thingData.containerUpgradeCool, thingData.windowSaveDataJson);

		// ThingGen.Create called things.SetOwner(this) before mapInt was restored, caching width/height from a default
		// c_containerSize. Re-run SetOwner so wrench-extended containers (extend_v / extend_h) pick up the saved grid.
		// Mirrors the Duplicate-path idiom at disassembly Card.cs:3555-3565.
		if (card.IsContainer && card.c_containerSize != 0)
		{
			card.things.SetOwner(card);
		}

		// Restore mod sockets for ranged weapons so slot count and embedded attachments are preserved
		if (thingData.sockets != null && thingData.sockets.Count > 0 && card.IsRangedWeapon)
		{
			card.sockets = new List<int>(thingData.sockets);
		}

		return card;
	}
}
