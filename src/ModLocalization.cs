using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BepInEx;
using BepInEx.Configuration;

namespace NewGamePlus;

public class UIOption
{
	public string ConfigKey { get; set; }
	public string ConfigSection { get; set; }
	public bool DefaultValue { get; set; }
	public string Description { get; set; }
	public string ToggleId { get; set; }
	public string LabelKey { get; set; }
	public string TooltipKey { get; set; }
	public Dictionary<string, string> LabelTranslations { get; set; }
	public Dictionary<string, string> TooltipTranslations { get; set; }
}

public class LocalizedString
{
	public string Key { get; set; }
	public Dictionary<string, string> Translations { get; set; }
}

public static class ModLocalization
{
	// String key constants
	public const string ModTitle = "ModTitle";
	public const string ExportSuccess = "ExportSuccess";
	public const string ExportCurrentSave = "ExportCurrentSave";
	public const string ConfigTitle = "ConfigTitle";
	public const string InGameOptionsTitle = "InGameOptionsTitle";
	public const string ImportSettingsTitle = "ImportSettingsTitle";
	public const string DebugInventorySlots = "DebugInventorySlots";
	public const string DebugImport = "DebugImport";
	public const string ModTooltip = "ModTooltip";

	// Internal storage for localized strings
	private static Dictionary<string, Dictionary<string, Func<Dictionary<string, object>, string>>> strings = new Dictionary<string, Dictionary<string, Func<Dictionary<string, object>, string>>>();

	// UI Options
	public static readonly List<UIOption> InGameOptions = new()
	{
		new UIOption
		{
			ConfigKey = "showCfgButtonInGame",
			ConfigSection = "config",
			DefaultValue = true,
			Description = "",
			ToggleId = "showCfgButtonInGameToggle",
			LabelKey = "ShowExportButton",
			TooltipKey = "ShowExportButtonTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Show Export Button In-Game" },
				{ "JP", "ゲーム内にエクスポートボタンを表示" },
				{ "CN", "在游戏中显示导出按钮" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Shows the \"Export Current Save\" action when interacting with the Hearth Stone." },
				{ "JP", "炉石と対話する際に「現在の保存をエクスポート」アクションを表示します。" },
				{ "CN", "与炉石交互时显示\"导出当前存档\"操作。" }
			}
		},
		new UIOption
		{
			ConfigKey = "showDebugOptions",
			ConfigSection = "config",
			DefaultValue = false,
			Description = "Show debug options in-game",
			ToggleId = "showDebugOptionsToggle",
			LabelKey = "ShowDebugOptions",
			TooltipKey = "ShowDebugOptionsTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Show Debug Options" },
				{ "JP", "デバッグオプションを表示" },
				{ "CN", "显示调试选项" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Show debug options." },
				{ "JP", "デバッグオプションを表示します。" },
				{ "CN", "显示调试选项。" }
			}
		}
	};

	public static readonly List<UIOption> ImportOptions = new()
	{
		new UIOption
		{
			ConfigKey = "includeToolbar",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Include toolbar items when importing",
			ToggleId = "importIncludeToolbarToggle",
			LabelKey = "ImportIncludeToolbar",
			TooltipKey = "ImportIncludeToolbarTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Toolbar" },
				{ "JP", "ツールバーを含める" },
				{ "CN", "包含工具栏" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include toolbar items." },
				{ "JP", "ツールバーのアイテムを含めます。" },
				{ "CN", "包含工具栏物品。" }
			}
		},
		new UIOption
		{
			ConfigKey = "includeWornEquipment",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Include worn equipment when importing",
			ToggleId = "importIncludeWornEquipmentToggle",
			LabelKey = "ImportIncludeWornEquipment",
			TooltipKey = "ImportIncludeWornEquipmentTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Worn Equipment" },
				{ "JP", "装備品を含める" },
				{ "CN", "包含已装备物品" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include worn equipment. Content of equipped containers is controlled by the next option." },
				{ "JP", "装備品を含めます。装備されたコンテナの内容は次のオプションで制御されます。" },
				{ "CN", "包含已装备物品。已装备容器的内容由下一个选项控制。" }
			}
		},
		new UIOption
		{
			ConfigKey = "includeBackpackContents",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Include inventory and container contents when importing",
			ToggleId = "importIncludeBackpackContentsToggle",
			LabelKey = "ImportIncludeBackpackContents",
			TooltipKey = "ImportIncludeBackpackContentsTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Inventory and Container Contents" },
				{ "JP", "インベントリとコンテナの内容を含める" },
				{ "CN", "包含库存和容器内容" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include contents of inventory and containers." },
				{ "JP", "インベントリとコンテナの内容を含めます。" },
				{ "CN", "包含库存和容器的内容。" }
			}
		},
		new UIOption
		{
			ConfigKey = "includeAbilities",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Include your spells and counts when importing",
			ToggleId = "importIncludeAbilitiesToggle",
			LabelKey = "ImportIncludeAbilities",
			TooltipKey = "ImportIncludeAbilitiesTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Abilities" },
				{ "JP", "アビリティを含める" },
				{ "CN", "包含能力" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Includes your spells and counts." },
				{ "JP", "あなたの呪文と回数を含めます。" },
				{ "CN", "包含您的法术和次数。" }
			}
		},
		new UIOption
		{
			ConfigKey = "includeSkills",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Include your skill levels (general skills, craft skills, etc.) when importing",
			ToggleId = "importIncludeSkillsToggle",
			LabelKey = "ImportIncludeSkills",
			TooltipKey = "ImportIncludeSkillsTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Skills" },
				{ "JP", "スキルを含める" },
				{ "CN", "包含技能" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Includes your skill levels (general skills, craft skills, etc.)." },
				{ "JP", "あなたのスキルレベル（一般スキル、クラフトスキルなど）を含めます。" },
				{ "CN", "包含您的技能等级（通用技能、制作技能等）。" }
			}
		},
		new UIOption
		{
			ConfigKey = "includePiety",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Includes your faith and number of days with the current deity",
			ToggleId = "importIncludePietyToggle",
			LabelKey = "ImportIncludePiety",
			TooltipKey = "ImportIncludePietyTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Faith" },
				{ "JP", "信仰を含める" },
				{ "CN", "包含虔诚" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Includes your faith and number of days with the current deity" },
				{ "JP", "あなたの信仰と現在の神との日数を含めます" },
				{ "CN", "包含您的信仰和与当前神的天数" }
			}
		},
		new UIOption
		{
			ConfigKey = "includeCraftRecipes",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Include craft recipes and levels in them when importing",
			ToggleId = "importIncludeCraftRecipesToggle",
			LabelKey = "ImportIncludeCraftRecipes",
			TooltipKey = "ImportIncludeCraftRecipesTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Craft Recipes" },
				{ "JP", "クラフトレシピを含める" },
				{ "CN", "包含制作配方" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Includes craft recipes and levels in them" },
				{ "JP", "クラフトレシピとそのレベルを含めます" },
				{ "CN", "包含制作配方及其等级" }
			}
		},
		new UIOption
		{
			ConfigKey = "includeBank",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Include money and items stored in the bank",
			ToggleId = "importIncludeBankToggle",
			LabelKey = "ImportIncludeBank",
			TooltipKey = "ImportIncludeBankTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Bank" },
				{ "JP", "銀行を含める" },
				{ "CN", "包含银行" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include money and items stored in the bank" },
				{ "JP", "銀行に保管されたお金とアイテムを含める" },
				{ "CN", "包含银行中存储的金钱和物品" }
			}
		},
		new UIOption
		{
			ConfigKey = "includePlayerLevel",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Include character levels and experience you've gained",
			ToggleId = "importIncludePlayerLevelToggle",
			LabelKey = "ImportIncludePlayerLevel",
			TooltipKey = "ImportIncludePlayerLevelTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Player Level" },
				{ "JP", "プレイヤーレベルを含める" },
				{ "CN", "包含玩家等级" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Includes character levels and experience you've gained." },
				{ "JP", "獲得したキャラクターレベルと経験値を含めます。" },
				{ "CN", "包含您获得的角色等级和经验值。" }
			}
		},
		new UIOption
		{
			ConfigKey = "includeAcquiredFeats",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Include feats you've purchased. Unchecking this will refund all points.",
			ToggleId = "importIncludeAcquiredFeatsToggle",
			LabelKey = "ImportIncludeAcquiredFeats",
			TooltipKey = "ImportIncludeAcquiredFeatsTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Acquired Feats" },
				{ "JP", "獲得した特技を含める" },
				{ "CN", "包含已获得特技" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Includes feats you've purchased. Unchecking this will refund all points." },
				{ "JP", "購入した特技を含めます。これをオフにすると、すべてのポイントが返金されます。" },
				{ "CN", "包含您购买的特技。取消选中此选项将退还所有点数。" }
			}
		},
		new UIOption
		{
			ConfigKey = "includeKarma",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Include your karma when importing",
			ToggleId = "importIncludeKarmaToggle",
			LabelKey = "ImportIncludeKarma",
			TooltipKey = "ImportIncludeKarmaTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Karma" },
				{ "JP", "カルマを含める" },
				{ "CN", "包含业力" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Includes your karma" },
				{ "JP", "あなたのカルマを含めます" },
				{ "CN", "包含您的业力" }
			}
		},
		new UIOption
		{
			ConfigKey = "includeFame",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Include your fame when importing",
			ToggleId = "importIncludeFameToggle",
			LabelKey = "ImportIncludeFame",
			TooltipKey = "ImportIncludeFameTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Include Fame" },
				{ "JP", "名声を含める" },
				{ "CN", "包含声望" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Includes your fame" },
				{ "JP", "あなたの名声を含めます" },
				{ "CN", "包含您的声望" }
			}
		},
		new UIOption
		{
			ConfigKey = "cureDiseases",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Cure diseases when importing",
			ToggleId = "importCureDiseasesToggle",
			LabelKey = "ImportCureDiseases",
			TooltipKey = "ImportCureDiseasesTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Cure Ether Diseases" },
				{ "JP", "エーテル病を治療" },
				{ "CN", "治愈以太疾病" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Cure ether diseases and mutations caused by ether." },
				{ "JP", "エーテル病とそれによる変異を治療します。" },
				{ "CN", "治愈以太疾病及其引起的突变。" }
			}
		},
		new UIOption
		{
			ConfigKey = "cureMutations",
			ConfigSection = "import",
			DefaultValue = true,
			Description = "Cure mutations when importing",
			ToggleId = "importCureMutationsToggle",
			LabelKey = "ImportCureMutations",
			TooltipKey = "ImportCureMutationsTooltip",
			LabelTranslations = new Dictionary<string, string>
			{
				{ "EN", "Cure Mutations" },
				{ "JP", "変異を治療" },
				{ "CN", "治愈突变" }
			},
			TooltipTranslations = new Dictionary<string, string>
			{
				{ "EN", "Cure mutations not caused by ether." },
				{ "JP", "エーテル以外の原因による変異を治療します。" },
				{ "CN", "治愈非以太引起的突变。" }
			}
		}
	};

	// General localized strings (non-option)
	public static readonly List<LocalizedString> GeneralStrings = new()
	{
		new LocalizedString
		{
			Key = "ModTitle",
			Translations = new Dictionary<string, string>
			{
				{ "EN", "New Game++" },
				{ "JP", "New Game++" },
				{ "CN", "New Game++" }
			}
		},
		new LocalizedString
		{
			Key = "ExportSuccess",
			Translations = new Dictionary<string, string>
			{
				{ "EN", "Character exported! They will load when you start a new game." },
				{ "JP", "キャラクターがエクスポートされました！新しいゲームを開始すると自動的に読み込まれます。" },
				{ "CN", "角色已导出！开始新游戏时将自动加载。" }
			}
		},
		new LocalizedString
		{
			Key = "ExportCurrentSave",
			Translations = new Dictionary<string, string>
			{
				{ "EN", "Export Current Save" },
				{ "JP", "現在の保存をエクスポート" },
				{ "CN", "导出当前存档" }
			}
		},
		new LocalizedString
		{
			Key = "ConfigTitle",
			Translations = new Dictionary<string, string>
			{
				{ "EN", "New Game++ Configuration" },
				{ "JP", "New Game++ 設定" },
				{ "CN", "新游戏++ 配置" }
			}
		},
		new LocalizedString
		{
			Key = "InGameOptionsTitle",
			Translations = new Dictionary<string, string>
			{
				{ "EN", "In-Game Options" },
				{ "JP", "ゲーム内オプション" },
				{ "CN", "游戏内选项" }
			}
		},
		new LocalizedString
		{
			Key = "ImportSettingsTitle",
			Translations = new Dictionary<string, string>
			{
				{ "EN", "Import Settings" },
				{ "JP", "インポート設定" },
				{ "CN", "导入设置" }
			}
		},
		new LocalizedString
		{
			Key = "DebugInventorySlots",
			Translations = new Dictionary<string, string>
			{
				{ "EN", "Debug Inventory Slots" },
				{ "JP", "デバッグインベントリスロット" },
				{ "CN", "调试库存槽位" }
			}
		},
		new LocalizedString
		{
			Key = "DebugImport",
			Translations = new Dictionary<string, string>
			{
				{ "EN", "Debug Import" },
				{ "JP", "デバッグインポート" },
				{ "CN", "调试导入" }
			}
		},
		new LocalizedString
		{
			Key = "ModTooltip",
			Translations = new Dictionary<string, string>
			{
				{ "EN", "Export your current character data to carry over to a new game." },
				{ "JP", "現在のキャラクターデータをエクスポートして、新しいゲームに引き継ぎます。" },
				{ "CN", "导出当前角色数据以在新游戏中继承。" }
			}
		}
	};

	public static List<UIOption> AllOptions => InGameOptions.Concat(ImportOptions).ToList();

	// Localization methods
	public static string Get(string key, Dictionary<string, object> values = null)
	{
		if (values == null)
		{
			values = new Dictionary<string, object>();
		}

		string lang = EClass.core.config.lang;
		if (lang != "CN" && lang != "JP")
		{
			lang = "EN";
		}

		return GetForLanguage(key, lang, values);
	}

	public static string GetForLanguage(string key, string lang, Dictionary<string, object> values = null)
	{
		if (values == null)
		{
			values = new Dictionary<string, object>();
		}

		if (lang != "CN" && lang != "JP")
		{
			lang = "EN";
		}

		if (strings.TryGetValue(key, out var langDict) && langDict.TryGetValue(lang, out var func))
		{
			return func(values);
		}

		if (strings.TryGetValue(key, out langDict) && langDict.TryGetValue("EN", out func))
		{
			return func(values);
		}

		return key;
	}

	private static void AddOptionStrings(string labelKey, string tooltipKey, Dictionary<string, string> labelTranslations, Dictionary<string, string> tooltipTranslations)
	{
		if (!strings.ContainsKey(labelKey))
		{
			strings[labelKey] = new Dictionary<string, Func<Dictionary<string, object>, string>>();
			foreach (var kvp in labelTranslations)
			{
				string lang = kvp.Key;
				string text = kvp.Value;
				strings[labelKey][lang] = _ => text;
			}
		}

		if (!strings.ContainsKey(tooltipKey))
		{
			strings[tooltipKey] = new Dictionary<string, Func<Dictionary<string, object>, string>>();
			foreach (var kvp in tooltipTranslations)
			{
				string lang = kvp.Key;
				string text = kvp.Value;
				strings[tooltipKey][lang] = _ => text;
			}
		}
	}

	private static void AddString(string key, Dictionary<string, string> translations)
	{
		if (!strings.ContainsKey(key))
		{
			strings[key] = new Dictionary<string, Func<Dictionary<string, object>, string>>();
			foreach (var kvp in translations)
			{
				string lang = kvp.Key;
				string text = kvp.Value;
				strings[key][lang] = _ => text;
			}
		}
	}

	// Configuration registration
	public static void RegisterAll(BaseUnityPlugin plugin)
	{
		foreach (UIOption option in AllOptions)
		{
			ConfigEntry<bool> entry = plugin.Config.Bind<bool>(option.ConfigSection, option.ConfigKey, option.DefaultValue, option.Description);
			ModConfig.SetOption(option.ConfigKey, entry);
		}
	}

	public static void PopulateLocalizedStrings()
	{
		foreach (UIOption option in AllOptions)
		{
			AddOptionStrings(option.LabelKey, option.TooltipKey, option.LabelTranslations, option.TooltipTranslations);
		}

		foreach (LocalizedString str in GeneralStrings)
		{
			AddString(str.Key, str.Translations);
		}
	}

	public static void RegisterUIToggles(EvilMask.Elin.ModOptions.UI.OptionUIBuilder builder)
	{
		foreach (UIOption option in AllOptions)
		{
			ConfigEntry<bool> configEntry = ModConfig.GetOption(option.ConfigKey);
			if (configEntry != null)
			{
				UI.UIController.RegisterImportToggle(builder, option.ToggleId, option.LabelKey, option.TooltipKey, configEntry);
			}
		}
	}

	public static ValidationResult ValidateXML(string xmlPath)
	{
		if (!File.Exists(xmlPath))
		{
			return new ValidationResult { IsValid = false, Message = "XML file not found" };
		}

		try
		{
			XDocument doc = XDocument.Load(xmlPath);
			HashSet<string> xmlToggleIds = doc.Descendants("toggle")
				.Select(t => t.Attribute("id")?.Value)
				.Where(id => !string.IsNullOrEmpty(id))
				.ToHashSet();

			HashSet<string> registryToggleIds = AllOptions
				.Select(o => o.ToggleId)
				.ToHashSet();

			List<string> missingKeys = registryToggleIds.Except(xmlToggleIds).ToList();
			List<string> extraKeys = xmlToggleIds.Except(registryToggleIds).ToList();

			if (missingKeys.Count == 0 && extraKeys.Count == 0)
			{
				return new ValidationResult { IsValid = true };
			}

			string message = "";
			if (missingKeys.Count > 0)
			{
				message += $"Missing in XML: {string.Join(", ", missingKeys)}\n";
			}
			if (extraKeys.Count > 0)
			{
				message += $"Extra in XML: {string.Join(", ", extraKeys)}\n";
			}

			return new ValidationResult { IsValid = false, Message = message.TrimEnd() };
		}
		catch (Exception ex)
		{
			return new ValidationResult { IsValid = false, Message = $"Error reading XML: {ex.Message}" };
		}
	}

	public static void ValidateAndReport()
	{
		string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		string xmlPath = Path.Combine(directoryName, "NewGamePlusConfig.xml");
		ValidationResult result = ValidateXML(xmlPath);
		if (!result.IsValid && !string.IsNullOrEmpty(result.Message))
		{
			Msg.SayRaw(result.Message);
		}
	}

	public class ValidationResult
	{
		public bool IsValid { get; set; }
		public string Message { get; set; }
	}
}
