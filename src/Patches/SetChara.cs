using HarmonyLib;

namespace NewGamePlus;

[HarmonyPatch(typeof(UICharaMaker), "SetChara")]
internal static class SetChara_Patch
{
	[HarmonyPrefix]
	public static void Prefix(UICharaMaker __instance, Chara c)
	{
		// Import BEFORE SetChara runs, so Refresh() will see the imported data
		NewGamePlus.ImportBio(c);
	}
}
