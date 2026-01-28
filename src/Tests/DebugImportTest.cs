using System;
using System.Collections.Generic;
using System.Text;

namespace NewGamePlus;

public static class DebugImportTester
{
	public static void Test(Chara c)
	{
		var debugDataBefore = new Dictionary<string, object>();

		// Capture state BEFORE import
		debugDataBefore["charaLV"] = ((Card)c).LV;
		debugDataBefore["charaHP"] = $"{c.hp}/{c.MaxHP}";
		debugDataBefore["charaMP"] = $"{c.mana.value}/{c.mana.max}";
		debugDataBefore["charaSP"] = $"{c.stamina.value}/{c.stamina.max}";
		debugDataBefore["corruption"] = c.corruption;
		debugDataBefore["raceId"] = ((Card)c).c_idRace;
		debugDataBefore["jobId"] = ((Card)c).c_idJob;
		debugDataBefore["freeFeatPoints"] = ((Card)c).feat;

		// Capture main attributes before
		var mainAttributes = new[] { 70, 71, 72, 73, 74, 75, 76, 77, 78, 80 };
		var elementsBefore = new Dictionary<string, object>();
		foreach (var eleId in mainAttributes)
		{
			var element = c.elements.GetElement(eleId);
			if (element != null)
			{
				elementsBefore[$"ele{eleId}"] = new Dictionary<string, object>
				{
					["vBase"] = element.vBase,
					["vSource"] = element.vSource,
					["vLink"] = element.vLink,
					["Value"] = element.Value,
					["ValueWithoutLink"] = element.ValueWithoutLink,
					["vExp"] = element.vExp,
					["vPotential"] = element.vPotential,
					["vTempPotential"] = element.vTempPotential
				};
			}
		}
		debugDataBefore["mainAttributes"] = elementsBefore;

		// Conditions before
		debugDataBefore["conditionCount"] = c.conditions.Count;
		var conditionNamesBefore = new List<string>();
		foreach (var condition in c.conditions)
		{
			conditionNamesBefore.Add(condition.Name);
		}
		debugDataBefore["conditions"] = conditionNamesBefore;

		// Mutations before
		int mutationCountBefore = 0;
		foreach (var element in c.elements.dict.Values)
		{
			if (element.source?.category == "ether" && element.Value != 0)
			{
				mutationCountBefore++;
			}
		}
		debugDataBefore["mutationCount"] = mutationCountBefore;

		// Equipment count before
		int equippedCountBefore = 0;
		int toolbarCountBefore = 0;
		foreach (var slot in c.body.slots)
		{
			if (slot.thing != null && slot.elementId != 44)
			{
				equippedCountBefore++;
			}
		}
		Card charaCardBefore = (Card)c;
		charaCardBefore.things.Foreach((Action<Thing>)delegate (Thing t)
		{
			if (!t.isEquipped && t.parent == c)
			{
				toolbarCountBefore++;
			}
		}, false);
		debugDataBefore["equippedCount"] = equippedCountBefore;
		debugDataBefore["toolbarCount"] = toolbarCountBefore;

		// Perform the import (simulating "wake up" in new game)
		CharacterImporter.ImportStat(c, NewGamePlus.GetDumpFilePath());

		// Capture state AFTER import
		var debugDataAfter = new Dictionary<string, object>();
		debugDataAfter["charaLV"] = ((Card)c).LV;
		debugDataAfter["charaHP"] = $"{c.hp}/{c.MaxHP}";
		debugDataAfter["charaMP"] = $"{c.mana.value}/{c.mana.max}";
		debugDataAfter["charaSP"] = $"{c.stamina.value}/{c.stamina.max}";
		debugDataAfter["corruption"] = c.corruption;
		debugDataAfter["raceId"] = ((Card)c).c_idRace;
		debugDataAfter["jobId"] = ((Card)c).c_idJob;
		debugDataAfter["freeFeatPoints"] = ((Card)c).feat;

		// Capture main attributes after
		var elementsAfter = new Dictionary<string, object>();
		foreach (var eleId in mainAttributes)
		{
			var element = c.elements.GetElement(eleId);
			if (element != null)
			{
				elementsAfter[$"ele{eleId}"] = new Dictionary<string, object>
				{
					["vBase"] = element.vBase,
					["vSource"] = element.vSource,
					["vLink"] = element.vLink,
					["Value"] = element.Value,
					["ValueWithoutLink"] = element.ValueWithoutLink,
					["vExp"] = element.vExp,
					["vPotential"] = element.vPotential,
					["vTempPotential"] = element.vTempPotential
				};
			}
		}
		debugDataAfter["mainAttributes"] = elementsAfter;

		// Conditions after
		debugDataAfter["conditionCount"] = c.conditions.Count;
		var conditionNamesAfter = new List<string>();
		foreach (var condition in c.conditions)
		{
			conditionNamesAfter.Add(condition.Name);
		}
		debugDataAfter["conditions"] = conditionNamesAfter;

		// Mutations after
		int mutationCountAfter = 0;
		foreach (var element in c.elements.dict.Values)
		{
			if (element.source?.category == "ether" && element.Value != 0)
			{
				mutationCountAfter++;
			}
		}
		debugDataAfter["mutationCount"] = mutationCountAfter;

		// Equipment count after
		int equippedCountAfter = 0;
		int toolbarCountAfter = 0;
		foreach (var slot in c.body.slots)
		{
			if (slot.thing != null && slot.elementId != 44)
			{
				equippedCountAfter++;
			}
		}
		Card charaCardAfter = (Card)c;
		charaCardAfter.things.Foreach((Action<Thing>)delegate (Thing t)
		{
			if (!t.isEquipped && t.parent == c)
			{
				toolbarCountAfter++;
			}
		}, false);
		debugDataAfter["equippedCount"] = equippedCountAfter;
		debugDataAfter["toolbarCount"] = toolbarCountAfter;

		// Calculate differences
		var differences = new Dictionary<string, object>();
		foreach (var eleId in mainAttributes)
		{
			var before = elementsBefore.ContainsKey($"ele{eleId}") ? elementsBefore[$"ele{eleId}"] as Dictionary<string, object> : null;
			var after = elementsAfter.ContainsKey($"ele{eleId}") ? elementsAfter[$"ele{eleId}"] as Dictionary<string, object> : null;
			if (before != null || after != null)
			{
				int vBaseBefore = (int)(before?["vBase"] ?? 0);
				int vBaseAfter = (int)(after?["vBase"] ?? 0);
				int vSourceBefore = (int)(before?["vSource"] ?? 0);
				int vSourceAfter = (int)(after?["vSource"] ?? 0);
				int valueBefore = (int)(before?["Value"] ?? 0);
				int valueAfter = (int)(after?["Value"] ?? 0);

				if (vBaseBefore != vBaseAfter || vSourceBefore != vSourceAfter || valueBefore != valueAfter)
				{
					differences[$"ele{eleId}"] = new Dictionary<string, object>
					{
						["vBase"] = $"{vBaseBefore} -> {vBaseAfter} (diff: {vBaseAfter - vBaseBefore})",
						["vSource"] = $"{vSourceBefore} -> {vSourceAfter} (diff: {vSourceAfter - vSourceBefore})",
						["Value"] = $"{valueBefore} -> {valueAfter} (diff: {valueAfter - valueBefore})"
					};
				}
			}
		}
		var diffData = new Dictionary<string, object>
		{
			["differences"] = differences,
			["lv"] = $"{(int)debugDataBefore["charaLV"]} -> {(int)debugDataAfter["charaLV"]}",
			["corruption"] = $"{(int)debugDataBefore["corruption"]} -> {(int)debugDataAfter["corruption"]}",
			["mutationCount"] = $"{mutationCountBefore} -> {mutationCountAfter}",
			["conditionCount"] = $"{(int)debugDataBefore["conditionCount"]} -> {(int)debugDataAfter["conditionCount"]}",
			["equippedCount"] = $"{equippedCountBefore} -> {equippedCountAfter}",
			["toolbarCount"] = $"{toolbarCountBefore} -> {toolbarCountAfter}"
		};

		// Format readable message
		var message = new System.Text.StringBuilder();
		message.AppendLine("=== Debug Import Test ===");
		message.AppendLine($"Before: LV{debugDataBefore["charaLV"]} | After: LV{debugDataAfter["charaLV"]}");
		message.AppendLine($"Corruption: {debugDataBefore["corruption"]} -> {debugDataAfter["corruption"]}");
		message.AppendLine($"Mutations: {mutationCountBefore} -> {mutationCountAfter}");
		message.AppendLine($"Conditions: {debugDataBefore["conditionCount"]} -> {debugDataAfter["conditionCount"]}");
		message.AppendLine($"Equipment: {equippedCountBefore} -> {equippedCountAfter} | Toolbar: {toolbarCountBefore} -> {toolbarCountAfter}");
		if (differences.Count > 0)
		{
			message.AppendLine("\n=== Stat Changes ===");
			foreach (var kvp in differences)
			{
				var diff = kvp.Value as Dictionary<string, object>;
				if (diff != null)
				{
					message.AppendLine($"{kvp.Key}: {diff["Value"]}");
				}
			}
		}
		else
		{
			message.AppendLine("\nNo stat changes detected");
		}

		// Count lines and append that many blank newlines for vertical centering
		string messageStr = message.ToString();
		int lineCount = messageStr.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
		for (int i = 0; i < lineCount; i++)
		{
			message.AppendLine("");
		}

		Msg.SayRaw(message.ToString());
	}
}
