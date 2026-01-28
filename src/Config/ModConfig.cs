using System.Collections.Generic;
using BepInEx.Configuration;

namespace NewGamePlus;

public static class ModConfig
{
	private static Dictionary<string, ConfigEntry<bool>> options = new Dictionary<string, ConfigEntry<bool>>();

	public static void SetOption(string key, ConfigEntry<bool> entry)
	{
		options[key] = entry;
	}

	public static ConfigEntry<bool> GetOption(string key)
	{
		return options.TryGetValue(key, out var entry) ? entry : null;
	}

	// Flag to track that Player.OnStartNewGame has been called (indicates a new game session has started)
	public static bool newGameSessionStarted = false;
}
