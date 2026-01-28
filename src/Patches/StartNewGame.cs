using HarmonyLib;

namespace NewGamePlus;

[HarmonyPatch(typeof(Game), "StartNewGame")]
internal static class StartNewGame_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Game __instance)
	{
		NewGamePlus.ImportStat(EClass.pc);
	}
}
