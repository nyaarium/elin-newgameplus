using System.IO;
using HarmonyLib;

namespace NewGamePlus;

[HarmonyPatch(typeof(Player), "OnStartNewGame")]
internal static class OnStartNewGame_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Player __instance)
	{
		// This is the signal that a new game has started (runs on story screen, zone is null)
		// Set flag so Game.StartNewGame knows this is a new game session, not just a zone transition
		string dumpFilePath = NewGamePlus.GetDumpFilePath();
		bool fileExists = dumpFilePath != null && File.Exists(dumpFilePath);
		if (fileExists)
		{
			ModConfig.newGameSessionStarted = true;
		}
	}
}
