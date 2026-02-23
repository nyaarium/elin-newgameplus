using System;
using System.Collections.Generic;
using System.Linq;

namespace NewGamePlus;

// The 5 slots next to equipment that can hold any item (purses, backpacks, etc.)
// Internally the game calls this container "toolbelt" but we call them "Accessory Slots"
public static class ItemSlotManager
{
	// --- Slot Element IDs (from SLOT.cs) ---
	public static class BodySlot
	{
		public const int Head = 30;
		public const int Neck = 31;
		public const int Torso = 32;
		public const int Back = 33;
		public const int Arm = 34;
		public const int Hand = 35;      // Main/Off hand
		public const int Finger = 36;    // Rings
		public const int Waist = 37;
		public const int Leg = 38;  // Not sure. Could not confirm. Might be on another race or class?
		public const int Foot = 39;
		public const int Tool = 40;
		public const int Range = 41;
		public const int Ammo = 42;
		public const int Token = 43;  // Not sure. Could not confirm. Might be on another race or class?
		public const int Toolbelt = 44;      // Toolbelt container (the 5-slot accessory container)
		public const int AccessorySlot = 45;       // Accessory row (light sources, necklaces, rings)
	}

	// --- Inventory Y Position (invY) ---
	public static class InvY
	{
		public const int Inventory = 0;
		public const int Toolbar = 1;
	}

	// --- Container Identifiers ---
	public static class ContainerId
	{
		public const string AccessoryContainer = "toolbelt";  // The 5-slot container
	}

	// Slot counts (game has 2 hotbar pages × 10 slots = 20)
	public const int ToolbarSlotCount = 20;
	public const int AccessorySlotCount = 5;
	public const int PlayerInventoryMaxSlots = 35; // Player's main inventory has 35 slots (0-34)

	// Valid body slot element IDs (excluding special slots like backpack/accessory)
	private static readonly HashSet<int> ValidBodySlotElementIds = new HashSet<int>
	{
		BodySlot.Head,
		BodySlot.Neck,
		BodySlot.Torso,
		BodySlot.Back,
		BodySlot.Arm,
		BodySlot.Hand,
		BodySlot.Finger,
		BodySlot.Waist,
		BodySlot.Foot,
	};

	// Get all valid body slot element IDs (excluding special slots like backpack/accessory)
	public static IEnumerable<int> GetValidBodySlotElementIds()
	{
		return ValidBodySlotElementIds;
	}

	// Get the toolbelt container (id="toolbelt")
	public static Card GetToolbeltContainer(Chara c)
	{
		// Search all things for container with id "toolbelt"
		Card charaCard = (Card)c;
		Card toolbeltContainer = null;

		System.Action<Thing> findToolbeltContainer = (Thing t) =>
		{
			Card thingCard = (Card)t;
			if (thingCard.IsContainer && thingCard.id == ContainerId.AccessoryContainer)
			{
				toolbeltContainer = thingCard;
			}
		};

		charaCard.things.Foreach(findToolbeltContainer, onlyAccessible: false);

		return toolbeltContainer;
	}
}

// Slot classification enum
public enum ItemLocation
{
	Unknown,
	Toolbar,           // invY == 1, in the toolbar (bottom of screen)
	Inventory,         // invY == 0, in player's 35-slot inventory (directly on character)
	Equipment,         // Equipped via body slot
	Toolbelt,          // Items inside the toolbelt container where the equipment UI is (container id="toolbelt")
	ContainerContents,      // Items **inside** a container that exists on the player (money in a purse)
	NonPlayer          // Not belonging to player
}

// Item classification helper
public static class ItemLocationHelper
{
	// Determine where an item is located
	public static ItemLocation ClassifyItem(Thing item, Chara owner)
	{
		if (item == null || owner == null) return ItemLocation.Unknown;

		// Check if equipped
		if (IsEquipped(item, owner))
			return ItemLocation.Equipment;

		// Check if in toolbar
		if (IsInToolbar(item, owner))
			return ItemLocation.Toolbar;

		// Check if in toolbelt container
		if (IsInAccessorySlot(item))
			return ItemLocation.Toolbelt;

		// Check if in container (purse, backpack, etc. that exists on the player)
		if (IsInContainer(item, owner))
			return ItemLocation.ContainerContents;

		// Check if in inventory (35-slot player inventory directly on character)
		if (IsInInventory(item, owner))
			return ItemLocation.Inventory;

		// Not belonging to player
		return ItemLocation.NonPlayer;
	}

	// Check if item is in toolbar
	public static bool IsInToolbar(Thing item, Chara owner)
	{
		if (item == null || owner == null) return false;
		return item.invY == ItemSlotManager.InvY.Toolbar && item.parent == owner;
	}

	// Check if item is equipped
	public static bool IsEquipped(Thing item, Chara owner)
	{
		if (item == null || owner == null) return false;
		return owner.body.slots.Any(s => s.thing == item);
	}

	// Check if item is in toolbelt container (container id="toolbelt", not slot 45)
	public static bool IsInAccessorySlot(Thing item)
	{
		if (item == null || item.parent == null) return false;
		Card parentCard = item.parent as Card;
		return parentCard != null && parentCard.id == ItemSlotManager.ContainerId.AccessoryContainer;
	}

	// Check if item is in a container that exists on the player
	// Containers can be placed in toolbelt, in inventory, etc. They're not "worn", they just exist
	public static bool IsInContainer(Thing item, Chara owner)
	{
		if (item == null || item.parent == null || owner == null) return false;

		Card parentCard = item.parent as Card;
		if (parentCard == null || !parentCard.IsContainer) return false;

		// Check if parent container belongs to the player (equipped, in inventory, or directly owned)
		// Equipped items have parent == owner (via AddCard in Equip()), inventory items also have parent == owner
		if (parentCard.parent == owner)
		{
			return true;
		}

		// Check if parent container is inside another container that exists on the player (recursive check)
		// Example: purse inside toolbelt container (toolbelt exists on player, purse is inside it)
		Thing currentParent = parentCard.Thing;
		while (currentParent != null && currentParent.parent != null)
		{
			Card currentParentCard = currentParent.parent as Card;
			if (currentParentCard != null && currentParentCard.IsContainer)
			{
				// Check if ancestor container belongs to player
				if (currentParentCard.parent == owner)
				{
					return true;
				}
				// Continue up the chain
				currentParent = currentParentCard.Thing;
			}
			else
			{
				break;
			}
		}

		return false;
	}

	// Check if item is in inventory (35-slot player inventory directly on character)
	private static bool IsInInventory(Thing item, Chara owner)
	{
		if (item == null || owner == null) return false;
		// Player inventory: parent == Chara, invY == 0, invX is 0-34
		return item.invY == ItemSlotManager.InvY.Inventory && item.parent == owner;
	}
}

// Auto-placement storage manager (Inventory and Subcontainers)
// Handles storage that supports auto-placement - game decides where items go
public static class StorageAuto
{
	// Insert item into inventory (35-slot player inventory directly on character) - auto-placement
	// Returns the slot (invX) it was placed into, or -1 if failed/dropped to ground
	// Uses Pick() which automatically handles slot assignment via GetDest/AddThing
	public static int InsertToInventory(Chara c, Thing item)
	{
		if (item == null) return -1;

		Card itemCard = (Card)item;

		// Use Pick() - it uses GetDest() to find placement and handles slot assignment automatically
		// Pick() will drop to ground if inventory is full (dest.IsValid == false)
		c.Pick(item, msg: false, tryStack: true);

		// Check if item was placed in inventory
		if (itemCard.parent == c && itemCard.invY == ItemSlotManager.InvY.Inventory && itemCard.invX >= 0)
		{
			return itemCard.invX;
		}

		// Item was dropped to ground (inventory full) or placed elsewhere
		// Pick() already handled dropping to ground, so just return -1
		return -1;
	}

	// Insert item into a subcontainer (purse, etc.) - auto-placement
	// Returns the slot (invX) it was placed into, or -1 if failed
	public static int InsertToSubContainer(Card container, Thing item)
	{
		if (container == null || !container.IsContainer || item == null) return -1;

		// Auto-fill: use tryStack: true and destInvX: -1 to let game auto-place items (stacking allowed)
		// destInvY: -1 means use container's default (not toolbar/inventory)
		container.AddThing(item, tryStack: true, destInvX: -1, destInvY: -1);
		Card itemCard = (Card)item;

		// Verify item ended up in the container (not toolbar/inventory)
		if (itemCard.parent != container)
		{
			return -1; // Item was placed elsewhere, not in the container
		}
		return itemCard.invX >= 0 ? itemCard.invX : -1;
	}

	// Get occupied slots for inventory (excludes subcontainer contents)
	// Only returns slots from items directly in the player inventory, not items inside subcontainers
	public static List<int> GetOccupiedInventorySlots(Chara c)
	{
		List<int> occupiedSlots = new List<int>();

		// Player inventory is directly on the character (parent == Chara, invY == 0)
		Card charaCard = (Card)c;

		System.Action<Thing> addOccupiedInventorySlot = (Thing t) =>
		{
			Card thingCard = (Card)t;
			// Items directly in player inventory: parent == Chara, invY == 0, invX >= 0
			if (thingCard.invY == ItemSlotManager.InvY.Inventory && thingCard.invX >= 0 && thingCard.parent == c)
			{
				occupiedSlots.Add(thingCard.invX);
			}
		};

		charaCard.things.Foreach(addOccupiedInventorySlot, onlyAccessible: false);

		return occupiedSlots;
	}

	// Spawn item to inventory from ThingData descriptor - auto-placement
	// Returns the slot (invX) it was placed into, or -1 if failed
	public static int SpawnToInventory(Chara c, ThingData descriptor)
	{
		if (descriptor == null) return -1;

		Card card = ThingUtils.RestoreThingFromData(descriptor);
		if (card == null) return -1;

		return InsertToInventory(c, card.Thing);
	}

	// Spawn item to subcontainer from ThingData descriptor - auto-placement
	// Returns the slot (invX) it was placed into, or -1 if failed
	public static int SpawnToSubContainer(Card container, ThingData descriptor)
	{
		if (descriptor == null) return -1;

		Card card = ThingUtils.RestoreThingFromData(descriptor);
		if (card == null) return -1;

		return InsertToSubContainer(container, card.Thing);
	}
}

// Fixed-slot storage manager (Toolbar, Toolbelt, Equipment)
// Handles storage that requires explicit slot specification - strict bounds checking
public static class StorageFixed
{
	// Insert item into toolbar at normalized slot (0-19, both hotbar pages)
	// Throws ArgumentOutOfRangeException if slot >= 20
	// Returns the slot if successful, throws if failed
	public static int InsertToToolbar(Chara c, Thing item, int normalizedSlot)
	{
		if (normalizedSlot < 0 || normalizedSlot >= ItemSlotManager.ToolbarSlotCount)
		{
			throw new ArgumentOutOfRangeException(nameof(normalizedSlot), $"Toolbar slot must be 0-{ItemSlotManager.ToolbarSlotCount - 1}, got {normalizedSlot}");
		}

		if (item == null) throw new ArgumentNullException(nameof(item));

		Card charaCard = (Card)c;
		Thing result = charaCard.AddThing(item, tryStack: false, destInvX: normalizedSlot, destInvY: ItemSlotManager.InvY.Toolbar); if (result == null) throw new InvalidOperationException("Failed to add item to toolbar");

		// AddThing resets invX to -1, so we must set both invX and invY after (matches ActionMode.cs pattern)
		result.invX = normalizedSlot;
		result.invY = ItemSlotManager.InvY.Toolbar;// Mark toolbar widget as dirty to refresh UI
		WidgetCurrentTool.dirty = true;

		return normalizedSlot;
	}

	// Insert item into toolbelt at normalized slot (0-4)
	// Throws ArgumentOutOfRangeException if slot >= 5
	// Returns the slot if successful, throws if failed
	public static int InsertToToolbelt(Chara c, Thing item, int normalizedSlot)
	{
		if (normalizedSlot < 0 || normalizedSlot >= ItemSlotManager.AccessorySlotCount)
		{
			throw new ArgumentOutOfRangeException(nameof(normalizedSlot), $"Toolbelt slot must be 0-{ItemSlotManager.AccessorySlotCount - 1}, got {normalizedSlot}");
		}

		if (item == null) throw new ArgumentNullException(nameof(item));

		Card container = ItemSlotManager.GetToolbeltContainer(c); if (container == null) throw new InvalidOperationException("No toolbelt container found");

		container.AddThing(item, tryStack: false, destInvX: normalizedSlot, destInvY: ItemSlotManager.InvY.Inventory);

		Card itemCardAfter = (Card)item; if (itemCardAfter.invX != normalizedSlot) throw new InvalidOperationException($"Failed to place item in toolbelt slot {normalizedSlot}");

		return normalizedSlot;
	}

	// Insert item into equipment slot
	// slotIndex: Raw slot.index value from the game (0, 1, 2, 3, etc.) - must match the exact slot the item was exported from
	// Throws if elementId is invalid, slotIndex is null, or slot not found
	// Returns the slotIndex if successful
	public static int InsertToEquipment(Chara c, Thing item, int elementId, int slotIndex)
	{
		if (item == null) throw new ArgumentNullException(nameof(item));

		// Find slot by matching both elementId and slot.index
		BodySlot slot = null;
		foreach (BodySlot bodySlot in c.body.slots)
		{
			if (bodySlot.elementId == elementId && bodySlot.index == slotIndex)
			{
				slot = bodySlot;
				break;
			}
		}

		if (slot == null)
			throw new ArgumentException($"No equipment slot found for elementId {elementId} with slotIndex {slotIndex}");

		bool result = c.body.Equip(item, slot);
		if (!result)
			throw new InvalidOperationException($"Failed to equip item to elementId {elementId}, slotIndex {slot.index}");

		return slot.index;
	}

	// Spawn item to toolbar from ThingData descriptor at normalized slot (0-19)
	// Throws ArgumentOutOfRangeException if slot >= 20
	// Returns the slot if successful, throws if failed
	public static int SpawnToToolbar(Chara c, ThingData descriptor, int normalizedSlot)
	{
		if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

		Card card = ThingUtils.RestoreThingFromData(descriptor);
		if (card == null) throw new InvalidOperationException("Failed to create item from descriptor");

		return InsertToToolbar(c, card.Thing, normalizedSlot);
	}
}
