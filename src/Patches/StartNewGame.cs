using System.IO;
using HarmonyLib;

namespace NewGamePlus;

[HarmonyPatch(typeof(Game), "StartNewGame")]
internal static class StartNewGame_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Game __instance)
	{
		// Only import when:
		// 1. Zone is ready (not on story screen)
		// 2. Dump file exists
		// 3. New game session flag is set (Player.OnStartNewGame ran, confirming this is a new game, not just a zone transition)
		string dumpFilePath = NewGamePlus.GetDumpFilePath();
		bool fileExists = dumpFilePath != null && File.Exists(dumpFilePath);
		bool canImport = fileExists && ModConfig.newGameSessionStarted && EClass.pc != null && EClass._zone != null;
		if (canImport)
		{
			NewGamePlus.ImportStat(EClass.pc);

			// Delete dump file and clear flag after successful import to prevent re-importing on subsequent zone loads
			try
			{
				File.Delete(dumpFilePath);
			}
			finally
			{
				ModConfig.newGameSessionStarted = false;
			}
		}
	}
}
