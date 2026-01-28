using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace NewGamePlus;

[BepInPlugin("me.chilemiao.plugin.NewGamePlus", "NewGamePlus", "1.0.0")]
public class NewGamePlus : BaseUnityPlugin
{
	public static ConfigEntry<bool> showCfgButtonInGame;

	public static ConfigEntry<bool> saveExported;

	public static ConfigEntry<string> cardIdPortrait;

	public static ConfigEntry<string> cardIdRace;

	public static ConfigEntry<string> cardIdJob;

	public static ConfigEntry<string> cardAltName;

	public static ConfigEntry<string> charaAlias;

	public static ConfigEntry<string> charaIdFaith;

	public static ConfigEntry<string> charaThings;

	public static ConfigEntry<string> charaThingsElements;

	public static ConfigEntry<string> charaThingsRef;

	public static ConfigEntry<string> charaElements;

	public static ConfigEntry<string> charaBodyParts;

	public static ConfigEntry<int> charaFreeFeatPoints;

	public static ConfigEntry<int> playerKarma;

	public static ConfigEntry<int> playerFame;

	public static ConfigEntry<int> playerTotalFeat;

	public static ConfigEntry<int> playerBankMoney;

	public static ConfigEntry<int> playerHolyWell;

	public static ConfigEntry<int> player_well_wish;

	public static ConfigEntry<int> player_well_enhance;

	public static ConfigEntry<int> player_jure_feather;

	public static ConfigEntry<int> player_lucky_coin;

	public static ConfigEntry<int> player_little_dead;

	public static ConfigEntry<int> player_little_saved;

	public static ConfigEntry<string> playerKnownBGMs;

	public static ConfigEntry<string> playerSketches;

	public static ConfigEntry<string> playerKnownCraft;

	public static ConfigEntry<string> playerKnownRecipe;

	public static ConfigEntry<int> playerDeepest;

	public static ConfigEntry<string> bioIds;

	private void Awake()
	{
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		showCfgButtonInGame = ((BaseUnityPlugin)this).Config.Bind<bool>("config", "showCfgButtonInGame", true, "");
		saveExported = ((BaseUnityPlugin)this).Config.Bind<bool>("data", "saveExported", false, "");
		cardIdPortrait = ((BaseUnityPlugin)this).Config.Bind<string>("data", "cardIdPortrait", "c_f-1", "");
		cardIdRace = ((BaseUnityPlugin)this).Config.Bind<string>("data", "cardIdRace", "yerles", "");
		cardIdJob = ((BaseUnityPlugin)this).Config.Bind<string>("data", "cardIdJob", "warrior", "");
		cardAltName = ((BaseUnityPlugin)this).Config.Bind<string>("data", "cardAltName", "", "");
		charaAlias = ((BaseUnityPlugin)this).Config.Bind<string>("data", "charaAlias", "", "");
		charaIdFaith = ((BaseUnityPlugin)this).Config.Bind<string>("data", "charaIdFaith", "eyth", "");
		charaThings = ((BaseUnityPlugin)this).Config.Bind<string>("data", "charaThings", "shirt/21/1/0/0/0/0/0/131104/0/0/1/0/1,bandage/1/5/0/0/0/0/0/131104/0/0/1/0/8", "");
		charaThingsElements = ((BaseUnityPlugin)this).Config.Bind<string>("data", "charaThingsElements", "64/1/0/0/0/0/0/0;", "");
		charaThingsRef = ((BaseUnityPlugin)this).Config.Bind<string>("data", "charaThingsRef", ";", "");
		charaElements = ((BaseUnityPlugin)this).Config.Bind<string>("data", "charaElements", "75/5/500/0/0/10/0/0,403/20/0/0/0/0/0/0,135/1/0/0/0/0/0/0,6003/1/0/0/0/0/0/0,6012/1/0/0/0/0/0/0,6015/1/0/0/0/0/0/0,6050/1/0/0/0/0/0/0", "");
		charaBodyParts = ((BaseUnityPlugin)this).Config.Bind<string>("data", "charaBodyParts", "頭|首|体|背|手|手|指|指|腕|腰|脚|足|", "");
		charaFreeFeatPoints = ((BaseUnityPlugin)this).Config.Bind<int>("data", "charaFreeFeatPoints", 0, "");
		playerKarma = ((BaseUnityPlugin)this).Config.Bind<int>("data", "playerKarma", 0, "");
		playerFame = ((BaseUnityPlugin)this).Config.Bind<int>("data", "playerFame", 0, "");
		playerTotalFeat = ((BaseUnityPlugin)this).Config.Bind<int>("data", "playerTotalFeat", 0, "");
		playerBankMoney = ((BaseUnityPlugin)this).Config.Bind<int>("data", "playerBankMoney", 0, "");
		playerHolyWell = ((BaseUnityPlugin)this).Config.Bind<int>("data", "playerHolyWell", 0, "");
		player_well_wish = ((BaseUnityPlugin)this).Config.Bind<int>("data", "player_well_wish", 0, "");
		player_well_enhance = ((BaseUnityPlugin)this).Config.Bind<int>("data", "player_well_enhance", 0, "");
		player_jure_feather = ((BaseUnityPlugin)this).Config.Bind<int>("data", "player_jure_feather", 0, "");
		player_lucky_coin = ((BaseUnityPlugin)this).Config.Bind<int>("data", "player_lucky_coin", 0, "");
		player_little_dead = ((BaseUnityPlugin)this).Config.Bind<int>("data", "player_little_dead", 0, "");
		player_little_saved = ((BaseUnityPlugin)this).Config.Bind<int>("data", "player_little_saved", 0, "");
		playerKnownBGMs = ((BaseUnityPlugin)this).Config.Bind<string>("data", "playerKnownBGMs", "1,3", "");
		playerSketches = ((BaseUnityPlugin)this).Config.Bind<string>("data", "playerSketches", "115", "");
		playerKnownCraft = ((BaseUnityPlugin)this).Config.Bind<string>("data", "playerKnownCraft", "", "");
		playerKnownRecipe = ((BaseUnityPlugin)this).Config.Bind<string>("data", "playerKnownRecipe", "", "");
		playerDeepest = ((BaseUnityPlugin)this).Config.Bind<int>("data", "playerDeepest", 0, "");
		bioIds = ((BaseUnityPlugin)this).Config.Bind<string>("data", "bioIds", "1,0,68,177,1,11,452,518,383,308,735,149,237,280,53,13,31,34,35,3", "");
		new Harmony("NewGamePlus").PatchAll();
	}

	public static void ExportBio(Chara c)
	{
		UpdateStringConfig(cardIdPortrait, string.Join(",", ((Card)c).c_idPortrait));
		UpdateStringConfig(cardIdRace, string.Join(",", ((Card)c).c_idRace));
		UpdateStringConfig(cardIdJob, string.Join(",", ((Card)c).c_idJob));
		UpdateStringConfig(cardAltName, string.Join(",", ((Card)c).c_altName));
		UpdateStringConfig(charaAlias, c._alias);
		UpdateStringConfig(charaIdFaith, c.idFaith);
		UpdateIntConfig(charaFreeFeatPoints, ((Card)c).feat);
		UpdateIntConfig(playerKarma, EClass.player.karma);
		UpdateIntConfig(playerFame, EClass.player.fame);
		UpdateIntConfig(playerTotalFeat, EClass.player.totalFeat);
		UpdateIntConfig(playerBankMoney, ((Card)EClass.game.cards.container_deposit).GetCurrency("money"));
		UpdateIntConfig(playerHolyWell, EClass.player.holyWell);
		UpdateIntConfig(player_well_wish, EClass.player.CountKeyItem("well_wish"));
		UpdateIntConfig(player_well_enhance, EClass.player.CountKeyItem("well_enhance"));
		UpdateIntConfig(player_jure_feather, EClass.player.CountKeyItem("jure_feather"));
		UpdateIntConfig(player_lucky_coin, EClass.player.CountKeyItem("lucky_coin"));
		UpdateIntConfig(player_little_dead, EClass.player.little_dead);
		UpdateIntConfig(player_little_saved, EClass.player.little_saved);
		UpdateStringConfig(playerKnownBGMs, string.Join(",", EClass.player.knownBGMs));
		UpdateStringConfig(playerSketches, string.Join(",", EClass.player.sketches));
		UpdateStringConfig(playerKnownCraft, string.Join(",", EClass.player.knownCraft));
		UpdateStringConfig(playerKnownRecipe, string.Join(",", EClass.player.recipes.knownRecipes.Select((KeyValuePair<string, int> kv) => kv.Key + "/" + kv.Value)));
		UpdateIntConfig(playerDeepest, EClass.player.stats.deepest);
		ExportThingConfig();
		UpdateStringConfig(charaElements, string.Join(",", elementToString((Card)(object)c)));
		UpdateStringConfig(charaBodyParts, getFigure(c.body.slots));
		UpdateStringConfig(bioIds, string.Join(",", ((Card)c).bio.ints));
		if (!saveExported.Value)
		{
			saveExported.Value = true;
			AddOrUpdateAppSettings(((ConfigEntryBase)saveExported).Definition.Key, "true");
		}
		static void ExportThingConfig()
		{
			List<string> list = new List<string>();
			List<string> listElement = new List<string>();
			List<string> listRef = new List<string>();
			((Card)EMono.pc).things.Foreach((Action<Thing>)delegate (Thing t)
			{
				CreateThingConfig(t);
			}, true);
			UpdateStringConfig(charaThings, string.Join(",", list));
			UpdateStringConfig(charaThingsElements, string.Join(";", listElement));
			UpdateStringConfig(charaThingsRef, string.Join(";", listRef));
			void CreateThingConfig(Thing thing)
			{
				//IL_0078: Unknown result type (might be due to invalid IL or missing references)
				if (!(((Card)thing).trait is TraitToolBelt) && !(((Card)thing).trait is TraitTutorialBook) && !(((Card)thing).trait is TraitAbility))
				{
					list.Add(((Card)thing).id + "/" + string.Join("/", new int[13]
					{
						((Card)thing).idMaterial,
						((Card)thing).LV,
						blessStateToInt(((Card)thing).blessedState),
						((Card)thing).rarityLv,
						((Card)thing).qualityTier,
						((Card)thing).encLV,
						((Card)thing).decay,
						((BitArray32)(ref ((Card)thing)._bits1)).ToInt(),
						((BitArray32)(ref ((Card)thing)._bits2)).ToInt(),
						((Card)thing).idSkin,
						((Card)thing).IsIdentified ? 1 : 0,
						((Card)thing).isCrafted ? 1 : 0,
						((Card)thing).Num
					}));
					listElement.Add(elementToString((Card)(object)thing));
					listRef.Add((((Card)thing).refCard != null) ? ((Card)thing).refCard.id : "");
				}
			}
		}
		static void UpdateIntConfig(ConfigEntry<int> config, int n)
		{
			config.Value = n;
			AddOrUpdateAppSettings(((ConfigEntryBase)config).Definition.Key, n.ToString());
		}
		static void UpdateStringConfig(ConfigEntry<string> config, string s)
		{
			config.Value = s;
			AddOrUpdateAppSettings(((ConfigEntryBase)config).Definition.Key, s);
		}
		static int blessStateToInt(BlessedState s)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Invalid comparison between Unknown and I4
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Invalid comparison between Unknown and I4
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Invalid comparison between Unknown and I4
			if ((int)s == 1)
			{
				return 1;
			}
			if ((int)s == -1)
			{
				return -1;
			}
			if ((int)s == -2)
			{
				return -2;
			}
			return 0;
		}
		static string elementToString(Card card)
		{
			List<string> list = new List<string>();
			foreach (Element value in ((ElementContainer)card.elements).dict.Values)
			{
				if (value.vBase != 0)
				{
					list.Add(string.Join("/", new int[8] { value.id, value.vBase, value.vExp, value.vPotential, value.vTempPotential, value.vSource, value.vSourcePotential, value.vLink }));
				}
			}
			return string.Join(",", list);
		}
		static string getFigure(List<BodySlot> slots)
		{
			string text = "";
			foreach (BodySlot slot in slots)
			{
				int elementId = slot.elementId;
				if (elementId != 44 && elementId != 45)
				{
					text = text + unParseBodySlot(elementId) + "|";
				}
			}
			return text;
		}
		static string unParseBodySlot(int num)
		{
			return num switch
			{
				32 => "体",
				35 => "手",
				36 => "指",
				33 => "背",
				34 => "腕",
				37 => "腰",
				39 => "足",
				30 => "頭",
				31 => "首",
				_ => "",
			};
		}
	}

	public static void ImportBio(Chara c)
	{
		if (saveExported.Value)
		{
			((Card)c).c_idPortrait = cardIdPortrait.Value;
			c.ChangeRace(cardIdRace.Value);
			c.ChangeJob(cardIdJob.Value);
			EMono.player.RefreshDomain();
			((Card)c).c_altName = cardAltName.Value;
			c._alias = charaAlias.Value;
			((Card)c).bio.ints = Array.ConvertAll(bioIds.Value.Split(','), int.Parse);
		}
	}

	public static void ImportStat(Chara c)
	{
		if (saveExported.Value)
		{
			c.idFaith = charaIdFaith.Value;
			EClass.player.karma = playerKarma.Value;
			EClass.player.fame = playerFame.Value;
			for (int i = 0; i < playerTotalFeat.Value; i++)
			{
				((Card)c).LevelUp();
			}
			((Card)c).feat = charaFreeFeatPoints.Value;
			ImportThingConfig();
			ImportElementConfig((Card)(object)c, charaElements.Value);
			if (playerBankMoney.Value > 0)
			{
				((Card)EClass.game.cards.container_deposit).Add("money", playerBankMoney.Value, 1);
			}
			if (playerHolyWell.Value > 0)
			{
				EClass.player.holyWell = playerHolyWell.Value;
			}
			if (player_well_wish.Value > 0)
			{
				EClass.player.ModKeyItem("well_wish", player_well_wish.Value, true);
			}
			if (player_well_enhance.Value > 0)
			{
				EClass.player.ModKeyItem("well_enhance", player_well_enhance.Value, true);
			}
			if (player_jure_feather.Value > 0)
			{
				EClass.player.ModKeyItem("jure_feather", player_jure_feather.Value, true);
			}
			if (player_lucky_coin.Value > 0)
			{
				EClass.player.ModKeyItem("lucky_coin", player_lucky_coin.Value, true);
			}
			if (player_little_dead.Value > 0)
			{
				EClass.player.little_dead = player_little_dead.Value;
			}
			if (player_little_saved.Value > 0)
			{
				EClass.player.little_saved = player_little_saved.Value;
			}
			if (playerDeepest.Value > 0)
			{
				EClass.player.stats.deepest = playerDeepest.Value;
			}
			if (!ClassExtension.IsEmpty(playerKnownBGMs.Value))
			{
				EClass.player.knownBGMs = Array.ConvertAll(playerKnownBGMs.Value.Split(','), int.Parse).ToHashSet();
			}
			if (!ClassExtension.IsEmpty(playerSketches.Value))
			{
				EClass.player.sketches = Array.ConvertAll(playerSketches.Value.Split(','), int.Parse).ToHashSet();
			}
			if (!ClassExtension.IsEmpty(playerKnownCraft.Value))
			{
				EClass.player.knownCraft = Array.ConvertAll(playerKnownCraft.Value.Split(','), int.Parse).ToHashSet();
			}
			ImportRecipeConfig();
			ImportBodyConfig(charaBodyParts.Value);
			c.CalculateMaxStamina();
			c.HealAll();
		}
		void ImportBodyConfig(string figure)
		{
			string[] array = c.race.figure.Split('|', '\0');
			foreach (string s in array)
			{
				int num = ParseBodySlot(s);
				if (num != -1)
				{
					c.body.RemoveBodyPart(num);
				}
			}
			string[] array2 = figure.Split('|', '\0');
			foreach (string s2 in array2)
			{
				int num2 = ParseBodySlot(s2);
				if (num2 != -1)
				{
					c.body.AddBodyPart(num2, (Thing)null);
				}
			}
			c.body.RefreshBodyParts();
		}
		static void ImportElementConfig(Card card, string elementList)
		{
			if (!ClassExtension.IsEmpty(elementList))
			{
				string[] array = elementList.Split(',');
				foreach (string text in array)
				{
					int[] array2 = Array.ConvertAll(text.Split('/'), int.Parse);
					Element val = ((ElementContainer)card.elements).SetBase(array2[0], array2[1], 0);
					val.vExp = array2[2];
					val.vPotential = array2[3];
					val.vTempPotential = array2[4];
					val.vSource = array2[5];
					val.vSourcePotential = array2[6];
					val.vLink = array2[7];
				}
			}
		}
		static void ImportRecipeConfig()
		{
			if (!ClassExtension.IsEmpty(playerKnownRecipe.Value))
			{
				string[] array = playerKnownRecipe.Value.Split(',');
				string[] array2 = array;
				foreach (string text in array2)
				{
					string text2 = text.Split('/')[0];
					int num = ClassExtension.ToInt(text.Split('/')[1]);
					RecipeSource val = RecipeManager.Get(text2);
					if (val != null && !EClass.player.recipes.knownRecipes.ContainsKey(text2))
					{
						for (int k = 0; k < num; k++)
						{
							EClass.player.recipes.Add(text2, true);
						}
					}
				}
			}
		}
		void ImportThingConfig()
		{
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			Point nearestPoint = ((Card)c).pos.GetNearestPoint(false, true, true, false);
			string[] array = charaThingsElements.Value.Split(';');
			string[] array2 = charaThingsRef.Value.Split(';');
			int num = 0;
			string[] array3 = charaThings.Value.Split(',');
			foreach (string text in array3)
			{
				string[] array4 = text.Split('/');
				Card val = EClass._zone.AddCard((Card)(object)((Card)ThingGen.Create(array4[0], ClassExtension.ToInt(array4[1]), ClassExtension.ToInt(array4[2]))).SetNum(ClassExtension.ToInt(array4[13])), nearestPoint);
				if (val.IsContainer)
				{
					val.things.DestroyAll((Func<Thing, bool>)null);
				}
				if (ClassExtension.ToInt(array4[3]) != 0)
				{
					val.SetBlessedState(intToBlessState(ClassExtension.ToInt(array4[3])));
				}
				val.rarityLv = ClassExtension.ToInt(array4[4]);
				val.qualityTier = ClassExtension.ToInt(array4[5]);
				val.encLV = ClassExtension.ToInt(array4[6]);
				val.decay = ClassExtension.ToInt(array4[7]);
				((BitArray32)(ref val._bits1)).SetInt(ClassExtension.ToInt(array4[8]));
				((BitArray32)(ref val._bits2)).SetInt(ClassExtension.ToInt(array4[9]));
				val.idSkin = ClassExtension.ToInt(array4[10]);
				if (!val.IsIdentified && array4[11] == "1")
				{
					((Card)val.Thing).c_IDTState = 0;
				}
				if (array4[12] == "1")
				{
					val.isCrafted = true;
				}
				if (array2.Length > num && !ClassExtension.IsEmpty(array2[num]))
				{
					val.MakeRefFrom(EClass.sources.cards.map[array2[num]].model, (Card)null);
				}
				ImportElementConfig(val, array[num++]);
			}
		}
		static int ParseBodySlot(string s)
		{
			return s switch
			{
				"体" => 32,
				"手" => 35,
				"指" => 36,
				"背" => 33,
				"腕" => 34,
				"腰" => 37,
				"足" => 39,
				"頭" => 30,
				"首" => 31,
				_ => -1,
			};
		}
		static BlessedState intToBlessState(int n)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			return (BlessedState)(n switch
			{
				1 => 1,
				-1 => -1,
				-2 => -2,
				_ => 0,
			});
		}
	}

	public static void AddOrUpdateAppSettings(string key, string value)
	{
		try
		{
			Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			KeyValueConfigurationCollection settings = configuration.AppSettings.Settings;
			if (settings[key] == null)
			{
				settings.Add(key, value);
			}
			else
			{
				settings[key].Value = value;
			}
			configuration.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection(configuration.AppSettings.SectionInformation.Name);
		}
		catch (ConfigurationErrorsException)
		{
			Console.WriteLine("Error writing app settings");
		}
	}
}
