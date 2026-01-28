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
			mapInt = mapIntDict
		};
	}

	// Restore ThingData descriptor to Card (inverse of CreateThingData)
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

		// Restore full _ints array FIRST (before MakeRefFrom/ChangeMaterial which may modify it)
		// This ensures MakeRefFrom uses the correct array state from export
		if (thingData.ints != null && thingData.ints.Length > 0)
		{
			// Replace entire array - use exported length to preserve exact state
			card._ints = new int[thingData.ints.Length];
			Array.Copy(thingData.ints, card._ints, thingData.ints.Length);
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

		// ChangeMaterial uses idMaterial from _ints[4], so _ints must be restored first
		// Apply material AFTER restoring elements to add vSource bonuses
		// The bracket notation [DV, PV] depends on elements 64 and 65, which need both vBase and vSource
		if (card._ints != null && card._ints.Length > CardIntsIndices.IdMaterial)
		{
			int restoredIdMaterial = card._ints[CardIntsIndices.IdMaterial];
			if (EClass.sources.materials.map.ContainsKey(restoredIdMaterial))
			{
				// Save decay before ChangeMaterial (it sets decay to 0)
				int savedDecay = card.decay;
				// Use ChangeMaterial to properly restore material (calls ApplyMaterial and sets dirty flags)
				card.ChangeMaterial(EClass.sources.materials.map[restoredIdMaterial], ignoreFixedMaterial: true);
				// Restore decay after ChangeMaterial
				card.decay = savedDecay;
			}
		}

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

		return card;
	}
}
