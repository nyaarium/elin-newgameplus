using System;
using HarmonyLib;

namespace NewGamePlus;

[HarmonyPatch(typeof(TraitCoreZone), "TrySetAct")]
internal static class TrySetAct_Patch
{
	[HarmonyPrefix]
	public static void Postfix(TraitCoreZone __instance, ActPlan p)
	{
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Expected O, but got Unknown
		if (NewGamePlus.showCfgButtonInGame.Value && EClass._zone.IsPCFaction && ((Trait)__instance).owner.IsInstalled)
		{
			string text = ((EClass.core.config.lang == "CN") ? "导出当前存档" : ((EClass.core.config.lang == "ZHTW") ? "導出當前存檔" : ((!(EClass.core.config.lang == "JP")) ? "Export Current Save" : "現在の保存をエクスポート")));
			Act val = (Act)new DynamicAct(text, (Func<bool>)delegate
			{
				NewGamePlus.ExportBio(EClass.pc);
				return false;
			}, false)
			{
				id = text,
				dist = 1,
				isHostileAct = false,
				localAct = true,
				cursor = ((CursorSystem.Arrow == null) ? null : null),
				canRepeat = () => false
			};
			p.TrySetAct(val, (Card)null);
		}
	}
}
