using System.Collections.Generic;
using System.Linq;

namespace NewGamePlus;

/// <summary>
/// Exports party members for NG+ carry-over. Filters out unique-trait charas and the PC itself.
/// Reduced schema: only what we need to re-spawn the chara via CharaGen.Create and overlay state.
/// Ride/parasite/host JsonProperty refs are not in the schema, so they are implicitly stripped.
/// </summary>
public static class PartyMemberExporter
{
	public static List<MemberData> ExportPartyMembers(Chara pc)
	{
		if (pc?.party?.members == null || pc.party.members.Count == 0)
			return null;

		List<MemberData> result = new List<MemberData>();
		foreach (Chara member in pc.party.members)
		{
			if (member == null) continue;
			if (member == pc) continue;
			if (member.IsUnique || member.trait is TraitUniqueChara) continue;
			if (member.isDead) continue;
			if (string.IsNullOrEmpty(member.id)) continue;

			MemberData data = BuildMemberData(member);
			if (data != null)
				result.Add(data);
		}
		return result.Count > 0 ? result : null;
	}

	private static MemberData BuildMemberData(Chara c)
	{
		Card card = (Card)c;
		ItemExportResult itemResult = CharacterExporter.ExportAllItems(c);

		string loveJson = null;
		if (card.c_love != null)
		{
			try
			{
				loveJson = Newtonsoft.Json.JsonConvert.SerializeObject(card.c_love);
			}
			catch
			{
				loveJson = null;
			}
		}

		return new MemberData
		{
			id = card.id,
			LV = card.LV,
			exp = card.exp,
			feat = card.feat,
			altName = ExtractStringProperty(card.c_altName),
			alias = c._alias,
			idPortrait = ExtractStringProperty(card.c_idPortrait),
			idRace = ExtractStringProperty(card.c_idRace),
			idJob = ExtractStringProperty(card.c_idJob),
			bioIds = card.bio?.ints?.ToList(),
			elements = ThingUtils.ExportElementConfig(card),
			bodyParts = CharacterExporter.ExportBodyParts(c.body.slots),
			corruption = c.corruption,
			c_corruptionHistory = c.c_corruptionHistory != null ? c.c_corruptionHistory.ToList() : null,
			tempElements = CharacterExporter.ExportTempElements(c),
			idFaith = c.idFaith,
			daysWithGod = card.c_daysWithGod,
			mutations = CharacterExporter.ExportMutations(c),
			genes = CharacterExporter.ExportGenes(c),
			wornEquipment = itemResult.wornEquipment,
			containerContents = itemResult.containerContents,
			loveDataJson = loveJson,
			affinity = c._affinity
		};
	}

	private static string ExtractStringProperty(object value)
	{
		if (value == null) return null;
		if (value is string s) return s;
		if (value is System.Collections.IEnumerable e)
		{
			foreach (object item in e)
				return item as string;
		}
		return null;
	}
}
