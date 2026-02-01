using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NewGamePlus;

[DataContract]
public class ElementData
{
	[DataMember] public int id { get; set; }
	[DataMember] public int vBase { get; set; }
	[DataMember] public int vExp { get; set; }
	[DataMember] public int vPotential { get; set; }
	[DataMember] public int vTempPotential { get; set; }
	// DO NOT export: vSource, vLink, vSourcePotential (recalculated on import)
}

[DataContract]
public class ThingData
{
	[DataMember] public int? containerUid { get; set; }  // Container UID (Card._ints[CardIntsIndices.UidOrType]) - only set for containers
	[DataMember] public string id { get; set; }
	[DataMember] public bool isIdentified { get; set; }
	[DataMember] public bool isCrafted { get; set; }
	[DataMember] public List<ElementData> elements { get; set; }
	[DataMember] public string refCardId { get; set; }
	[DataMember] public int? slotElementId { get; set; }  // null for toolbar items, set for equipped items
	[DataMember] public int? slotIndex { get; set; }  // For multi-slot items (rings, hands) - distinguishes which slot
	[DataMember] public int[] ints { get; set; }  // Full _ints array (contains type, use count, and other item-specific data)
	[DataMember] public Dictionary<int, int> mapInt { get; set; }  // mapInt dictionary (contains c_dyeMat[3], c_charges[7], c_ammo[27], etc.)
}

[DataContract]
public class CodexCreatureData
{
	[DataMember] public string id { get; set; }
	[DataMember] public int[] ints { get; set; }  // CodexCreature._ints, always length 5
}

[DataContract]
public class RecipeData
{
	[DataMember] public string id { get; set; }
	[DataMember] public int count { get; set; }
}

[DataContract]
public class ConditionData
{
	[DataMember] public string id { get; set; }  // source.alias or source.id
	[DataMember] public int power { get; set; }
	[DataMember] public int value { get; set; }  // duration/turns remaining
	[DataMember] public int refVal { get; set; }
	[DataMember] public int refVal2 { get; set; }
	[DataMember] public List<ElementData> elements { get; set; }  // Condition's element contributions
}

[DataContract]
public class MutationData
{
	[DataMember] public int featId { get; set; }  // Feat ID (category == "ether")
	[DataMember] public int value { get; set; }  // Mutation value
}

[DataContract]
public class ContainerItemData
{
	[DataMember] public ThingData item { get; set; }
	[DataMember] public int parentContainerUid { get; set; }  // Card.uid from _ints[CardIntsIndices.UidOrType]
}

// Unified item export structure
public class ItemExportResult
{
	public List<ThingData> toolbarItems = new List<ThingData>();
	public List<ThingData> toolbeltItems = new List<ThingData>();
	public List<ThingData> wornEquipment = new List<ThingData>();
	public List<ContainerItemData> containerContents = new List<ContainerItemData>();
}

// Body slot info for equipped item lookup
public class BodySlotInfo
{
	public int elementId;
	public int slotIndex;
}

[DataContract]
public class CharacterDumpData
{
	[DataMember] public List<string> cardIdPortrait { get; set; }
	[DataMember] public List<string> cardIdRace { get; set; }
	[DataMember] public List<string> cardIdJob { get; set; }
	[DataMember] public List<string> cardAltName { get; set; }
	[DataMember] public string charaAlias { get; set; }
	[DataMember] public string charaIdFaith { get; set; }
	[DataMember] public int charaDaysWithGod { get; set; }
	[DataMember] public int charaLevelExp { get; set; }
	[DataMember] public int charaFreeFeatPoints { get; set; }
	[DataMember] public int playerKarma { get; set; }
	[DataMember] public int playerFame { get; set; }
	[DataMember] public int playerTotalFeat { get; set; }
	[DataMember] public int playerHolyWell { get; set; }
	[DataMember] public int player_well_wish { get; set; }
	[DataMember] public int player_well_enhance { get; set; }
	[DataMember] public int player_jure_feather { get; set; }
	[DataMember] public int player_lucky_coin { get; set; }
	[DataMember] public int player_little_dead { get; set; }
	[DataMember] public int player_little_saved { get; set; }
	[DataMember] public List<int> playerKnownBGMs { get; set; }
	[DataMember] public List<int> playerSketches { get; set; }
	[DataMember] public List<int> playerKnownCraft { get; set; }
	[DataMember] public List<RecipeData> playerKnownRecipe { get; set; }
	[DataMember] public List<CodexCreatureData> playerCodex { get; set; }
	[DataMember] public Dictionary<string, int> zoneInfluence { get; set; }
	[DataMember] public int playerDeepest { get; set; }
	[DataMember] public List<ElementData> charaElements { get; set; }
	[DataMember] public List<int> charaBodyParts { get; set; }  // Element IDs directly, not Japanese strings
	[DataMember] public List<int> bioIds { get; set; }

	// New fields for proper stat export/import
	[DataMember] public int corruption { get; set; }
	[DataMember] public List<int> c_corruptionHistory { get; set; }
	[DataMember] public List<ElementData> tempElements { get; set; }
	[DataMember] public List<ElementData> faithElements { get; set; }
	[DataMember] public List<ElementData> workElements { get; set; }
	[DataMember] public List<ConditionData> conditions { get; set; }
	[DataMember] public List<MutationData> mutations { get; set; }

	// Structured item lists (replaces charaThings)
	[DataMember] public List<ThingData> toolbarItems { get; set; }
	[DataMember] public List<ThingData> toolbeltItems { get; set; }
	[DataMember] public List<ThingData> wornEquipment { get; set; }
	[DataMember] public List<ContainerItemData> containerContents { get; set; }
	[DataMember] public List<ThingData> bankItems { get; set; }  // Items in bank container (including money & items)
}
