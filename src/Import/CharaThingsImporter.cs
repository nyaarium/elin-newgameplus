using System;
using System.Collections.Generic;

namespace NewGamePlus;

/// <summary>
/// Shared owner of "spawn items onto a Chara" primitives. Both PC import and party-member
/// import drive their own dispatch loops on top of these helpers, so a future fix to e.g.
/// container UID translation lands once and applies everywhere.
/// </summary>
internal static class CharaThingsImporter
{
	/// <summary>
	/// Unequips currently-worn equipment in the standard equipment-slot range. Leaves
	/// special slots (Toolbelt, AccessorySlot) alone. Used to wipe default gear before
	/// applying carried equipment so the chara does not end up wearing both.
	/// </summary>
	public static void ClearEquipment(Chara c)
	{
		if (c?.body?.slots == null) return;
		foreach (BodySlot slot in c.body.slots)
		{
			if (slot.thing != null
				&& slot.elementId >= ItemSlotManager.BodySlot.Head
				&& slot.elementId <= ItemSlotManager.BodySlot.Token)
			{
				c.body.Unequip(slot, refresh: false);
			}
		}
	}

	/// <summary>
	/// Spawns and equips each ThingData onto the chara. Returns a map from exported (old)
	/// container UIDs to live (new) container UIDs so subsequent container-content imports
	/// can resolve their parents. Items missing slot info are skipped. Per-item errors are
	/// reported through the optional callback; on error the spawned card (if any) is added
	/// to inventory as a fallback.
	/// </summary>
	public static Dictionary<int, int> Equip(Chara c, IList<ThingData> wornEquipment, Action<string, Exception> onItemError = null)
	{
		Dictionary<int, int> oldToNew = new Dictionary<int, int>();
		if (wornEquipment == null || wornEquipment.Count == 0)
			return oldToNew;

		foreach (ThingData td in wornEquipment)
		{
			if (td == null) continue;
			if (!td.slotElementId.HasValue || !td.slotIndex.HasValue)
				continue;

			Card spawned = null;
			try
			{
				spawned = ThingUtils.RestoreThingFromData(td);
				if (spawned == null) continue;
				StorageFixed.InsertToEquipment(c, spawned.Thing, td.slotElementId.Value, td.slotIndex.Value);

				if (spawned.IsContainer
					&& td.containerUid.HasValue
					&& spawned._ints != null
					&& spawned._ints.Length > CardIntsIndices.UidOrType)
				{
					oldToNew[td.containerUid.Value] = spawned._ints[CardIntsIndices.UidOrType];
				}
			}
			catch (Exception ex)
			{
				onItemError?.Invoke(td.id, ex);
				if (spawned != null)
				{
					spawned.c_equippedSlot = 0;
					c.AddThing(spawned.Thing, tryStack: false);
					if (((Card)spawned.Thing).parent != c)
						DropAtFeet(c, spawned);
				}
				else
				{
					DropAtFeet(c, td);
				}
			}
		}
		return oldToNew;
	}

	/// <summary>
	/// Drops an item at the chara's feet when spawn/equip fails (e.g., weight overflow on
	/// weak races like Fairy Weak). Containers get Install() so they render correctly as
	/// placed furniture.
	/// </summary>
	public static void DropAtFeet(Chara c, Card card)
	{
		if (card == null) return;
		if (EClass._zone == null || c?.pos == null || !c.pos.IsValid) return;
		card.c_equippedSlot = 0;
		EClass._zone.AddCard(card, c.pos);
		if (card.IsContainer)
			card.Install();
	}

	public static void DropAtFeet(Chara c, ThingData descriptor)
	{
		if (descriptor == null) return;
		Card card = ThingUtils.RestoreThingFromData(descriptor);
		if (card == null) return;
		DropAtFeet(c, card);
	}

	/// <summary>
	/// Builds a map from container UID to live Card across the chara's body slots and
	/// inventory, recursing into nested containers via explicit BFS. The naive
	/// `things.Foreach(action, onlyAccessible: false)` walks one level then internally
	/// re-calls `Foreach(action)` (default `onlyAccessible: true`), which silently skips
	/// locked or NPC-property containers from depth 1 onward. The BFS here forces
	/// `onlyAccessible: false` at every depth.
	/// </summary>
	public static Dictionary<int, Card> BuildContainerUidMap(Chara c)
	{
		Dictionary<int, Card> map = new Dictionary<int, Card>();
		if (c?.body?.slots == null) return map;

		Queue<Card> queue = new Queue<Card>();
		foreach (BodySlot slot in c.body.slots)
		{
			if (slot.thing != null && slot.thing.IsContainer)
			{
				Card container = (Card)slot.thing;
				if (TryAddToMap(container, map))
					queue.Enqueue(container);
			}
		}

		Action<Thing> seedFromInventory = (Thing t) =>
		{
			if (t.IsContainer)
			{
				Card container = (Card)t;
				if (TryAddToMap(container, map))
					queue.Enqueue(container);
			}
		};
		((Card)c).things.Foreach(seedFromInventory, onlyAccessible: false);

		while (queue.Count > 0)
		{
			Card current = queue.Dequeue();
			if (current?.things == null) continue;
			Action<Thing> walkNested = (Thing t) =>
			{
				if (t.IsContainer)
				{
					Card nested = (Card)t;
					if (TryAddToMap(nested, map))
						queue.Enqueue(nested);
				}
			};
			current.things.Foreach(walkNested, onlyAccessible: false);
		}

		return map;
	}

	/// <summary>
	/// If the given spawned card is a container, records its UID in both the old-to-new
	/// translation map (keyed by exported uid) and the live uid lookup map. Safe to call
	/// on non-containers.
	/// </summary>
	public static void RegisterIfContainer(Card spawned, ThingData td, Dictionary<int, int> oldToNewContainerUid, Dictionary<int, Card> uidMap)
	{
		if (spawned == null || !spawned.IsContainer) return;
		if (spawned._ints == null || spawned._ints.Length <= CardIntsIndices.UidOrType) return;
		int newUid = spawned._ints[CardIntsIndices.UidOrType];
		if (td?.containerUid.HasValue == true && oldToNewContainerUid != null)
			oldToNewContainerUid[td.containerUid.Value] = newUid;
		if (uidMap != null && !uidMap.ContainsKey(newUid))
			uidMap[newUid] = spawned;
	}

	private static bool TryAddToMap(Card container, Dictionary<int, Card> map)
	{
		if (container?._ints == null || container._ints.Length <= CardIntsIndices.UidOrType) return false;
		int uid = container._ints[CardIntsIndices.UidOrType];
		if (map.ContainsKey(uid)) return false;
		map[uid] = container;
		return true;
	}
}
