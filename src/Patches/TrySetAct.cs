using System;
using HarmonyLib;

namespace NewGamePlus;

[HarmonyPatch(typeof(TraitCoreZone), "TrySetAct")]
internal static class TrySetAct_Patch
{
	[HarmonyPrefix]
	public static void Prefix(TraitCoreZone __instance, ActPlan p)
	{
		if (ModConfig.GetOption("showCfgButtonInGame")?.Value == true && EClass._zone.IsPCFaction && ((Trait)__instance).owner.IsInstalled)
		{
			if (ModConfig.GetOption("showDebugOptions")?.Value == true)
			{
				string debugText = ModLocalization.Get(ModLocalization.DebugInventorySlots);

				Func<bool> debugExportAction = () =>
				{
					SlotTester.TestSlots(EClass.pc);
					return false;
				};

				Act debugAct = (Act)new DynamicAct(debugText, debugExportAction, false)
				{
					id = debugText,
					dist = 1,
					isHostileAct = false,
					localAct = true,
					cursor = ((CursorSystem.Arrow == null) ? null : null),
					canRepeat = () => false
				};
				p.TrySetAct(debugAct, (Card)null);

				string debugImportText = ModLocalization.Get(ModLocalization.DebugImport);

				Func<bool> debugImportAction = () =>
				{
					NewGamePlus.DebugImportTest(EClass.pc);
					return false;
				};

				Act debugImportAct = (Act)new DynamicAct(debugImportText, debugImportAction, false)
				{
					id = debugImportText,
					dist = 1,
					isHostileAct = false,
					localAct = true,
					cursor = ((CursorSystem.Arrow == null) ? null : null),
					canRepeat = () => false
				};
				p.TrySetAct(debugImportAct, (Card)null);
			}

			string exportText = ModLocalization.Get(ModLocalization.ExportCurrentSave);

			Func<bool> exportAction = () =>
			{
				NewGamePlus.ExportCharacter(EClass.pc);
				return false;
			};

			Act exportAct = (Act)new DynamicAct(exportText, exportAction, false)
			{
				id = exportText,
				dist = 1,
				isHostileAct = false,
				localAct = true,
				cursor = ((CursorSystem.Arrow == null) ? null : null),
				canRepeat = () => false
			};

			p.TrySetAct(exportAct, (Card)null);
		}
	}
}
