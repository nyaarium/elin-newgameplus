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

	/// <summary>
	/// Set by the OnStartNewGame patch when Player.OnStartNewGame fires, and cleared after import.
	/// Guards against re-importing on subsequent zone loads within the same session.
	/// </summary>
	public static bool newGameSessionStarted = false;
}
