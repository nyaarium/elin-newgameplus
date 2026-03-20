#if DEVMODE
using HarmonyLib;

namespace NewGamePlus;

[HarmonyPatch(typeof(BaseModPackage), nameof(BaseModPackage.UpdateMeta))]
internal static class DevModeTitle_Patch
{
	[HarmonyPostfix]
	public static void Postfix(BaseModPackage __instance)
	{
		if (__instance.id == "elin_newgameplus")
		{
			__instance.title = "New Game++ - DEBUG DO NOT SHIP";
		}
	}
}

[HarmonyPatch(typeof(Steam), nameof(Steam.CreateUserContent))]
internal static class DevModeBlockPublish_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(BaseModPackage p)
	{
		if (p.id == "elin_newgameplus")
		{
			Dialog.Ok("NG++: Cannot publish debug build. Use 'build.sh' (release) first.");
			return false;
		}
		return true;
	}
}
#endif
