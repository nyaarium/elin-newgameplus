using System;
using System.IO;
using System.Reflection;
using BepInEx;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;
using UnityEngine;

namespace NewGamePlus.UI;

public static class UIController
{
	public static void RegisterUI()
	{
		foreach (object item in ModManager.ListPluginObject)
		{
			BaseUnityPlugin plugin = (BaseUnityPlugin)((item is BaseUnityPlugin) ? item : null);
			if (plugin == null || !(plugin.Info.Metadata.GUID == "evilmask.elinplugins.modoptions"))
			{
				continue;
			}
			ModOptionController controller = ModOptionController.Register("nyaarium.newgameplusplus", "mod.tooltip", Array.Empty<object>());

			string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string version = ModLocalization.GetVersionFromPackageXml(directoryName);

			// Register mod name translation (GUID -> display name)
			controller.SetTranslation("EN", "nyaarium.newgameplusplus", ModLocalization.GetForLanguage(ModLocalization.ModTitle, "EN"));
			controller.SetTranslation("JP", "nyaarium.newgameplusplus", ModLocalization.GetForLanguage(ModLocalization.ModTitle, "JP"));
			controller.SetTranslation("CN", "nyaarium.newgameplusplus", ModLocalization.GetForLanguage(ModLocalization.ModTitle, "CN"));

			// Register mod tooltip translations with ModOptions for all languages
			controller.SetTranslation("EN", "mod.tooltip", ModLocalization.GetForLanguage(ModLocalization.ModTooltip, "EN"));
			controller.SetTranslation("JP", "mod.tooltip", ModLocalization.GetForLanguage(ModLocalization.ModTooltip, "JP"));
			controller.SetTranslation("CN", "mod.tooltip", ModLocalization.GetForLanguage(ModLocalization.ModTooltip, "CN"));

			string xmlPath = Path.Combine(directoryName, "NewGamePlusConfig.xml");
			if (File.Exists(xmlPath))
			{
				using StreamReader streamReader = new StreamReader(xmlPath);
				controller.SetPreBuildWithXml(streamReader.ReadToEnd());
			}
			RegisterEvents(controller, version);
		}
	}

	private static void RegisterEvents(ModOptionController controller, string version)
	{
		string configPanelTitle = ModLocalization.Get(ModLocalization.ModTitle) + (!string.IsNullOrEmpty(version) ? " v" + version : "");

		Action<OptionUIBuilder> onBuildUIHandler = (OptionUIBuilder builder) =>
		{
			SetSectionTitle(builder, "vlayout01", configPanelTitle);
			SetSectionTitle(builder, "vlayout02", ModLocalization.Get(ModLocalization.InGameOptionsTitle));
			SetSectionTitle(builder, "vlayout03", ModLocalization.Get(ModLocalization.MainImportSettingsTitle));
			SetSectionTitle(builder, "vlayoutSpecial", ModLocalization.Get(ModLocalization.SpecialImportSettingsTitle));

			ModLocalization.RegisterUIToggles(builder);
		};

		controller.OnBuildUI += onBuildUIHandler;
	}

	private static void SetSectionTitle(OptionUIBuilder builder, string layoutId, string titleText)
	{
		OptVLayout layout = builder.GetPreBuild<OptVLayout>(layoutId);
		if (layout?.Base == null)
			return;
		Transform topicClone = layout.Base.transform.Find("TopicDefault(Clone)");
		Transform topicTransform = topicClone?.Find("topic");
		var topicText = topicTransform?.GetComponent<UnityEngine.UI.Text>();
		if (topicText != null)
			topicText.text = titleText;
	}

	public static void RegisterImportToggle(OptionUIBuilder builder, string toggleId, string contentId, string tooltipId, BepInEx.Configuration.ConfigEntry<bool> configEntry)
	{
		OptToggle toggle = builder.GetPreBuild<OptToggle>(toggleId);
		if (toggle != null)
		{
			if (toggle.Base != null)
			{
				var mainTextTransform = toggle.Base.transform.Find("MainText");
				if (mainTextTransform != null)
				{
					var contentText = mainTextTransform.GetComponent<UnityEngine.UI.Text>();
					if (contentText != null)
					{
						contentText.text = ModLocalization.Get(contentId);
					}
				}
				// Set tooltip text manually
				if (toggle.Base.tooltip != null)
				{
					toggle.Base.tooltip.text = ModLocalization.Get(tooltipId);
					toggle.Base.tooltip.enable = true;
				}
			}
			toggle.Checked = configEntry.Value;

			Action<bool> onValueChangedHandler = (bool isChecked) =>
			{
				configEntry.Value = isChecked;
			};

			toggle.OnValueChanged += onValueChangedHandler;
		}
	}

}
