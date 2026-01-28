using HarmonyLib;

namespace NewGamePlus;

[HarmonyPatch(typeof(UICharaMaker), "SetChara")]
internal static class SetChara_Patch
{
	[HarmonyPostfix]
	public static bool Prefix(UICharaMaker __instance, Chara c)
	{
		NewGamePlus.ImportBio(c);
		return true;
	}
}
