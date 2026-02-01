using System.Collections.Generic;
using System.Linq;

namespace NewGamePlus;

public static class SlotTester
{
	public static void TestSlots(Chara c)
	{
		Card charaCard = (Card)c;

		// Find containers using ItemSlotManager helpers
		// Note: Player inventory is directly on the character (no separate backpack container)
		Card toolbeltContainer = ItemSlotManager.GetToolbeltContainer(c);

		// Note: backpackCard can be null if character only has toolbelt in slot 44 (that's OK, will use virtual inventory)
		if (toolbeltContainer == null)
		{
			Msg.SayRaw("Error: No accessory container found. Cannot test toolbelt slots.");
			return;
		}

		// 1. Fill toolbar (both bars) with amber
		for (int x = 0; x < ItemSlotManager.ToolbarSlotCount; x++)
		{
			Card card = (Card)ThingGen.Create("throw_putit");
			if (card != null)
			{
				int slot = StorageFixed.InsertToToolbar(c, card.Thing, x);
			}
		}

		// 2. Fill inventory with 35 lanterns (auto-fill)
		// Note: Player inventory is directly on the character (no separate backpack container)
		// Fill inventory with 35 lanterns using StorageAuto (auto-placement)
		// Item ID "998" is "an old lantern" (not "lantern" which creates a rubber duck)
		int capacity = 35;
		for (int i = 0; i < capacity; i++)
		{
			Card lantern = (Card)ThingGen.Create("998");
			if (lantern != null)
			{
				StorageAuto.InsertToInventory(c, lantern.Thing);
			}
		}

		// 3. Fill toolbelt (accessory slots) with 5 orens (one per slot)
		for (int x = 0; x < ItemSlotManager.AccessorySlotCount; x++)
		{
			// Use "money" (oren) - one per toolbelt slot
			Card card = (Card)ThingGen.Create("money");
			if (card != null)
			{
				int slot = StorageFixed.InsertToToolbelt(c, card.Thing, x);
			}
		}

		// 4. Equip items to equipment slots (using actual item IDs from decompiled code)
		int equippedCount = 0;
		var equipmentSlots = new Dictionary<int, string[]>
		{
			{ ItemSlotManager.BodySlot.Head, new[] { "helm_seeker", "cap" } },
			{ ItemSlotManager.BodySlot.Neck, new[] { "amulet_moonnight", "amulet_begger" } },
			{ ItemSlotManager.BodySlot.Torso, new[] { "shirt" } },
			{ ItemSlotManager.BodySlot.Back, new[] { "cloak" } },
			{ ItemSlotManager.BodySlot.Arm, new[] { "gloves" } },
			{ ItemSlotManager.BodySlot.Hand, new[] { "sword" } },
			{ ItemSlotManager.BodySlot.Finger, new[] { "ring" } },
			{ ItemSlotManager.BodySlot.Waist, new[] { "girdle" } },
			{ ItemSlotManager.BodySlot.Foot, new[] { "shoes" } }
		};

		// Light source (elementId 45)
		var lightSourceIds = new[] { "torch_held", "torch" };
		foreach (string lightId in lightSourceIds)
		{
			Card card = (Card)ThingGen.Create(lightId);
			if (card != null)
			{
				try
				{
					// Find first empty slot for AccessorySlot
					int? firstEmptySlotIndex = null;
					foreach (BodySlot slot in c.body.slots)
					{
						if (slot.elementId == ItemSlotManager.BodySlot.AccessorySlot && slot.thing == null)
						{
							firstEmptySlotIndex = slot.index;
							break;
						}
					}
					if (firstEmptySlotIndex.HasValue)
					{
						int slotIndex = StorageFixed.InsertToEquipment(c, card.Thing, ItemSlotManager.BodySlot.AccessorySlot, firstEmptySlotIndex.Value);
						equippedCount++;
						break;
					}
				}
				catch (System.Exception)
				{
					// Try next item
				}
			}
		}

		foreach (var kvp in equipmentSlots)
		{
			int maxAttempts = 1;
			if (kvp.Key == ItemSlotManager.BodySlot.Finger)
			{
				maxAttempts = 2; // Two rings
			}
			else if (kvp.Key == ItemSlotManager.BodySlot.Hand)
			{
				maxAttempts = 2; // Main hand then off-hand
			}

			for (int attempt = 0; attempt < maxAttempts; attempt++)
			{
				// Find empty slot for this elementId (attempt 0 = first empty, attempt 1 = second empty)
				int? emptySlotIndex = null;
				int emptyCount = 0;
				foreach (BodySlot slot in c.body.slots)
				{
					if (slot.elementId == kvp.Key && slot.thing == null)
					{
						if (emptyCount == attempt)
						{
							emptySlotIndex = slot.index;
							break;
						}
						emptyCount++;
					}
				}

				if (!emptySlotIndex.HasValue)
				{
					continue; // No empty slot for this attempt
				}

				foreach (string itemId in kvp.Value)
				{
					Card card = (Card)ThingGen.Create(itemId);
					if (card != null)
					{
						try
						{
							int slotIndex = StorageFixed.InsertToEquipment(c, card.Thing, kvp.Key, emptySlotIndex.Value);
							equippedCount++;
							break;
						}
						catch (System.Exception)
						{
							// Try next item
						}
					}
				}
			}
		}

		// Final verification
		int toolbarAmberCount = 0;
		int inventoryLanternCount = 0;
		int toolbeltOrenCount = 0;
		charaCard.things.Foreach((System.Action<Thing>)delegate (Thing t)
		{
			Card thingCard = (Card)t;
			if (thingCard.id == "throw_putit" && t.invY == ItemSlotManager.InvY.Toolbar && t.parent == c)
			{
				toolbarAmberCount++;
			}
			else if (thingCard.id == "998" && t.invY == ItemSlotManager.InvY.Inventory && t.parent == c)
			{
				// Item is in player inventory (directly on character)
				inventoryLanternCount++;
			}
			else if (thingCard.id == "money")
			{
				Card parentCard = t.parent != null ? (Card)t.parent : null;
				if (parentCard != null && parentCard.id == ItemSlotManager.ContainerId.AccessoryContainer)
				{
					toolbeltOrenCount++;
				}
			}
		}, onlyAccessible: false);

		Msg.SayRaw($"Slot test complete:\n- Toolbar: {toolbarAmberCount} ambers\n- Inventory: {inventoryLanternCount} lanterns\n- Toolbelt: {toolbeltOrenCount} orens\n- Equipment: {equippedCount} items equipped");
	}
}
