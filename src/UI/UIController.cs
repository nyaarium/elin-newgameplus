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

			// Register mod name translation (GUID -> display name)
			controller.SetTranslation("EN", "nyaarium.newgameplusplus", ModLocalization.GetForLanguage(ModLocalization.ModTitle, "EN"));
			controller.SetTranslation("JP", "nyaarium.newgameplusplus", ModLocalization.GetForLanguage(ModLocalization.ModTitle, "JP"));
			controller.SetTranslation("CN", "nyaarium.newgameplusplus", ModLocalization.GetForLanguage(ModLocalization.ModTitle, "CN"));

			// Register mod tooltip translations with ModOptions for all languages
			controller.SetTranslation("EN", "mod.tooltip", ModLocalization.GetForLanguage(ModLocalization.ModTooltip, "EN"));
			controller.SetTranslation("JP", "mod.tooltip", ModLocalization.GetForLanguage(ModLocalization.ModTooltip, "JP"));
			controller.SetTranslation("CN", "mod.tooltip", ModLocalization.GetForLanguage(ModLocalization.ModTooltip, "CN"));

			string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string xmlPath = Path.Combine(directoryName, "NewGamePlusConfig.xml");
			if (File.Exists(xmlPath))
			{
				using StreamReader streamReader = new StreamReader(xmlPath);
				controller.SetPreBuildWithXml(streamReader.ReadToEnd());
			}
			RegisterEvents(controller);
		}
	}

	private static void RegisterEvents(ModOptionController controller)
	{
		Action<OptionUIBuilder> onBuildUIHandler = (OptionUIBuilder builder) =>
		{
			OptVLayout mainLayout = builder.GetPreBuild<OptVLayout>("vlayout01");
			if (mainLayout != null && mainLayout.Base != null)
			{
				var topicClone = mainLayout.Base.transform.Find("TopicDefault(Clone)");
				if (topicClone != null)
				{
					var topicTransform = topicClone.Find("topic");
					if (topicTransform != null)
					{
						var topicText = topicTransform.GetComponent<UnityEngine.UI.Text>();
						if (topicText != null)
						{
							topicText.text = ModLocalization.Get(ModLocalization.ConfigTitle);
						}
					}
				}
			}

			OptVLayout optionsLayout = builder.GetPreBuild<OptVLayout>("vlayout02");
			if (optionsLayout != null && optionsLayout.Base != null)
			{
				var topicClone = optionsLayout.Base.transform.Find("TopicDefault(Clone)");
				if (topicClone != null)
				{
					var topicTransform = topicClone.Find("topic");
					if (topicTransform != null)
					{
						var topicText = topicTransform.GetComponent<UnityEngine.UI.Text>();
						if (topicText != null)
						{
							topicText.text = ModLocalization.Get(ModLocalization.InGameOptionsTitle);
						}
					}
				}
			}

			OptVLayout importLayout = builder.GetPreBuild<OptVLayout>("vlayout03");
			if (importLayout != null && importLayout.Base != null)
			{
				var topicClone = importLayout.Base.transform.Find("TopicDefault(Clone)");
				if (topicClone != null)
				{
					var topicTransform = topicClone.Find("topic");
					if (topicTransform != null)
					{
						var topicText = topicTransform.GetComponent<UnityEngine.UI.Text>();
						if (topicText != null)
						{
							topicText.text = ModLocalization.Get(ModLocalization.ImportSettingsTitle);
						}
					}
				}
			}

			ModLocalization.RegisterUIToggles(builder);
		};

		controller.OnBuildUI += onBuildUIHandler;
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
