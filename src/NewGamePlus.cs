using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;

namespace NewGamePlus;

[BepInDependency("evilmask.elinplugins.modoptions", BepInEx.BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin("nyaarium.newgameplusplus", "New Game++", "1.2.2")]
public class NewGamePlus : BaseUnityPlugin
{
	private static NewGamePlus instance;


	private void Awake()
	{
		instance = this;
		ModLocalization.RegisterAll(this);
		ModLocalization.PopulateLocalizedStrings();
		new Harmony("nyaarium.newgameplusplus").PatchAll();
	}

	private void Start()
	{
		UI.UIController.RegisterUI();
	}

	public static void ExportCharacter(Chara c)
	{
		// Use unified export function
		ItemExportResult itemResult = CharacterExporter.ExportAllItems(c);

		CharacterDumpData dumpData = new CharacterDumpData
		{
			cardIdPortrait = ((Card)c).c_idPortrait is string strPortrait ? new List<string> { strPortrait } : ((Card)c).c_idPortrait?.Cast<string>().ToList() ?? new List<string>(),
			cardIdRace = ((Card)c).c_idRace is string strRace ? new List<string> { strRace } : ((Card)c).c_idRace?.Cast<string>().ToList() ?? new List<string>(),
			cardIdJob = ((Card)c).c_idJob is string strJob ? new List<string> { strJob } : ((Card)c).c_idJob?.Cast<string>().ToList() ?? new List<string>(),
			cardAltName = ((Card)c).c_altName is string strAltName ? new List<string> { strAltName } : ((Card)c).c_altName?.Cast<string>().ToList() ?? new List<string>(),
			charaAlias = c._alias,
			charaIdFaith = c.idFaith,
			charaDaysWithGod = ((Card)c).c_daysWithGod,
			charaLevelExp = ((Card)c).exp,
			charaFreeFeatPoints = ((Card)c).feat,
			playerKarma = EClass.player.karma,
			playerFame = EClass.player.fame,
			playerTotalFeat = EClass.player.totalFeat,
			playerHolyWell = EClass.player.holyWell,
			player_well_wish = EClass.player.CountKeyItem("well_wish"),
			player_well_enhance = EClass.player.CountKeyItem("well_enhance"),
			player_jure_feather = EClass.player.CountKeyItem("jure_feather"),
			player_lucky_coin = EClass.player.CountKeyItem("lucky_coin"),
			player_little_dead = EClass.player.little_dead,
			player_little_saved = EClass.player.little_saved,
			playerKumi = EClass.player.stats.kumi,
			playerKnownBGMs = EClass.player.knownBGMs.ToList(),
			playerSketches = EClass.player.sketches.ToList(),
			playerKnownCraft = EClass.player.knownCraft.ToList(),
			playerKnownRecipe = EClass.player.recipes.knownRecipes.Select(kv => new RecipeData { id = kv.Key, count = kv.Value }).ToList(),
			playerCodex = ExportCodex(),
			zoneInfluence = ExportZoneInfluence(),
			playerDeepest = EClass.player.stats.deepest,
			charaElements = ThingUtils.ExportElementConfig((Card)(object)c),
			charaBodyParts = CharacterExporter.ExportBodyParts(c.body.slots),
			bioIds = ((Card)c).bio.ints.ToList(),

			// New fields for proper stat export/import
			corruption = c.corruption,
			c_corruptionHistory = c.c_corruptionHistory != null ? c.c_corruptionHistory.ToList() : new List<int>(),
			tempElements = CharacterExporter.ExportTempElements(c),
			faithElements = CharacterExporter.ExportFaithElements(c),
			workElements = CharacterExporter.ExportWorkElements(c),
			conditions = CharacterExporter.ExportConditions(c),
			mutations = CharacterExporter.ExportMutations(c),

			// Structured item lists from unified export
			toolbarItems = itemResult.toolbarItems,
			toolbeltItems = itemResult.toolbeltItems,
			wornEquipment = itemResult.wornEquipment,
			containerContents = itemResult.containerContents,
			bankItems = CharacterExporter.ExportBankItems()
		};

		string dumpFilePath = GetDumpFilePath();
		if (dumpFilePath != null)
		{
			string jsonLine = DumpSerializer.SerializeDumpData(dumpData);
			try
			{
				File.WriteAllText(dumpFilePath, jsonLine);
			}
			catch (Exception)
			{
				// Failed to write dump file, continue silently
			}
		}

		string successMsg = ModLocalization.Get(ModLocalization.ExportSuccess);
		Msg.SayRaw(successMsg);
		ModLocalization.ValidateAndReport();
	}

	private static List<CodexCreatureData> ExportCodex()
	{
		if (EClass.player?.codex?.creatures == null || EClass.player.codex.creatures.Count == 0)
			return null;
		var list = new List<CodexCreatureData>();
		foreach (var kv in EClass.player.codex.creatures)
		{
			list.Add(new CodexCreatureData { id = kv.Key, ints = (int[])kv.Value._ints.Clone() });
		}
		return list;
	}

	private static Dictionary<string, int> ExportZoneInfluence()
	{
		if (EClass.game?.spatials?.Zones == null || EClass.game.spatials.Zones.Count == 0)
			return null;
		var dict = new Dictionary<string, int>();
		foreach (Zone zone in EClass.game.spatials.Zones)
			dict[zone.id] = zone.influence;
		return dict;
	}

	public static void DebugInventorySlotsTest(Chara c)
	{
		SlotTester.TestSlots(c);
	}


	public static void DebugImportTest(Chara c)
	{
		DebugImportTester.Test(c);
	}

	public static string GetDumpFilePath()
	{
		if (instance == null) return null;
		string configDir = Path.GetDirectoryName(instance.Config.ConfigFilePath);
		return Path.Combine(configDir, "NewGamePlus.dump.jsonl");
	}


	public static void ImportBio(Chara c)
	{
		CharacterImporter.ImportBio(c, GetDumpFilePath());
	}


	public static void ImportStat(Chara c)
	{
		CharacterImporter.ImportStat(c, GetDumpFilePath());
	}

}
