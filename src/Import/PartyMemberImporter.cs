using System;
using System.Collections.Generic;
using System.Linq;

namespace NewGamePlus;

/// <summary>
/// Imports carried party members on NG+ start. Each member is freshly spawned via
/// CharaGen.Create and overlaid with their saved state through helpers shared with the PC importer.
/// Spawned members go global, get placed at PC position, and are joined to PC's party.
/// </summary>
public static class PartyMemberImporter
{
	/// <summary>
	/// Runs a named import phase under a log + try/catch envelope. A throw in one phase
	/// gets logged with a stack trace and downstream phases continue. Without this, a single
	/// bad apply (e.g., RefreshFaithElement on a chara with broken element 85 state) would
	/// nuke all subsequent applies for that member.
	/// </summary>
	private static void Phase(string memberId, string name, Action work)
	{
		try
		{
			work();
		}
		catch (Exception ex)
		{
			DebugLogger.DebugLog("PartyMemberImporter.ImportPartyMember", $"phase: {name} threw", null, new Dictionary<string, object>
			{
				{ "memberId", memberId ?? "?" },
				{ "exception", ex.GetType().Name },
				{ "message", ex.Message },
				{ "stackTrace", ex.StackTrace ?? "" }
			});
		}
	}

	public static void ImportPartyMembers(string dumpFilePath)
	{
		if (ModConfig.GetOption("includePartyMembers")?.Value != true)
			return;

		CharacterDumpData dumpData = DumpSerializer.LoadDumpData(dumpFilePath);
		if (dumpData?.partyMembers == null || dumpData.partyMembers.Count == 0)
			return;

		if (EClass.pc == null || EClass._zone == null || EClass.pc.pos == null)
			return;

		foreach (MemberData data in dumpData.partyMembers)
		{
			try
			{
				ImportPartyMember(data);
			}
			catch (Exception ex)
			{
				DebugLogger.DebugLog("PartyMemberImporter.ImportPartyMembers", "member import threw", null, new Dictionary<string, object>
				{
					{ "memberId", data?.id ?? "unknown" },
					{ "exception", ex.GetType().Name },
					{ "message", ex.Message },
					{ "stackTrace", ex.StackTrace ?? "" }
				});
				Msg.SayRaw($"NG+: Failed to import party member '{data?.id ?? "unknown"}': {ex.Message}");
			}
		}
	}

	private static Chara ImportPartyMember(MemberData data)
	{
		if (data == null || string.IsNullOrEmpty(data.id))
			return null;
		if (!EClass.sources.charas.map.ContainsKey(data.id))
			return null;

		Chara c = CharaGen.Create(data.id);
		if (c == null)
			return null;

		Phase(data.id, "identity", () => ApplyIdentity(c, data));
		Phase(data.id, "raceJob", () => ApplyRaceJob(c, data));
		Phase(data.id, "level", () => ApplyLevel(c, data));
		Phase(data.id, "elements", () => ApplyElements(c, data));
		Phase(data.id, "bodyParts", () => ApplyBodyParts(c, data));

		Phase(data.id, "clearDefaultGear", () => CharaThingsImporter.ClearEquipment(c));
		Action<string, Exception> onItemError = (id, ex) =>
			Msg.SayRaw($"NG+: Failed to import equipment '{id ?? "unknown"}' for member '{((Card)c).id}': {ex.Message}");
		Dictionary<int, int> oldToNewContainerUid = new Dictionary<int, int>();
		Phase(data.id, "equip", () =>
		{
			Dictionary<int, int> result = CharaThingsImporter.Equip(c, data.wornEquipment, onItemError);
			foreach (var kv in result) oldToNewContainerUid[kv.Key] = kv.Value;
		});
		Phase(data.id, "containerContents", () => ApplyContainerContents(c, data, oldToNewContainerUid));

		Phase(data.id, "corruption", () => ApplyCorruption(c, data));
		Phase(data.id, "tempElements", () => ApplyTempElements(c, data));
		Phase(data.id, "faith", () => ApplyFaith(c, data));
		Phase(data.id, "mutations", () => ApplyMutations(c, data));
		Phase(data.id, "genes", () => ApplyGenes(c, data));
		Phase(data.id, "stripRaceTraits", () => CharacterImporter.StripRaceSpecificTraits(c, data.idRace));
		Phase(data.id, "marriage", () => ApplyMarriage(c, data));
		Phase(data.id, "affinity", () => ApplyAffinity(c, data));

		c.CalculateMaxStamina();
		c.hp = c.MaxHP;

		// Wire as PC's ally before adding to party. Match the MakeAlly pattern but skip the
		// homeBranch.AddMemeber path (would turn them into a colony resident, not a party member).
		// Track each registration so a mid-sequence throw can roll back; otherwise the chara
		// would leak into globalCharas / Zone / Party as an orphan.
		bool global = false;
		bool inZone = false;
		bool inParty = false;
		try
		{
			// Set the rollback flag BEFORE each registering call. If the call itself throws
			// mid-mutation (e.g., AddMemeber adds to members but throws before c.party=this),
			// our cleanup must still run. Setting the flag after means a partial throw skips
			// the rollback for that step.
			global = true;
			c.SetGlobal();
			c.SetFaction(EClass.Home);
			c.SetHostility(Hostility.Ally);
			c.orgPos = null;

			// Cross-save ref: this member's old homeZone uid is meaningless in the new save.
			// Reattach them to PC's homeZone so they belong to the same world anchor as PC.
			c.homeZone = EClass.pc.homeZone;

			inZone = true;
			EClass._zone.AddCard(c, EClass.pc.pos);
			inParty = true;
			EClass.pc.party.AddMemeber(c);

			// SetInt(32) is the turnLastSeen stamp _MakeAlly uses to suppress the spurious
			// "welcome back" greeting on first idle tick after recruit (AI_Idle.cs:600-607).
			c.SetInt(32, EClass.world.date.GetRaw());

			c.Refresh();
		}
		catch (Exception ex)
		{
			DebugLogger.DebugLog("PartyMemberImporter.ImportPartyMember", "finalize threw, rolling back", null, new Dictionary<string, object>
			{
				{ "id", data.id },
				{ "global", global },
				{ "inZone", inZone },
				{ "inParty", inParty },
				{ "exception", ex.GetType().Name },
				{ "message", ex.Message },
				{ "stackTrace", ex.StackTrace ?? "" }
			});
			if (inParty) EClass.pc.party.RemoveMember(c);
			if (inZone) EClass._zone.RemoveCard(c);
			if (global) c.RemoveGlobal();
			throw;
		}

		return c;
	}

	private static void ApplyAffinity(Chara c, MemberData data)
	{
		c._affinity = data.affinity;
	}

	private static void ApplyIdentity(Chara c, MemberData data)
	{
		Card card = (Card)c;
		if (!string.IsNullOrEmpty(data.altName)) card.c_altName = data.altName;
		if (!string.IsNullOrEmpty(data.alias)) c._alias = data.alias;
		if (!string.IsNullOrEmpty(data.idPortrait)) card.c_idPortrait = data.idPortrait;
		if (data.bioIds != null && data.bioIds.Count > 0) card.bio.ints = data.bioIds.ToArray();
	}

	private static void ApplyRaceJob(Chara c, MemberData data)
	{
		Card card = (Card)c;
		if (!string.IsNullOrEmpty(data.idRace) && data.idRace != card.c_idRace
			&& EClass.sources.races.map.ContainsKey(data.idRace))
		{
			c.ChangeRace(data.idRace);
		}
		if (!string.IsNullOrEmpty(data.idJob) && data.idJob != card.c_idJob
			&& EClass.sources.jobs.map.ContainsKey(data.idJob))
		{
			c.ChangeJob(data.idJob);
		}
	}

	private static void ApplyLevel(Chara c, MemberData data)
	{
		Card card = (Card)c;
		int delta = data.LV - card.LV;
		for (int i = 0; i < delta; i++)
			card.LevelUp();
		card.exp = data.exp;
		card.feat = data.feat;
	}

	private static void ApplyElements(Chara c, MemberData data)
	{
		if (data.elements == null || data.elements.Count == 0)
			return;
		CharacterImporter.ImportElementConfig(c, data.elements, data.idRace, data.idJob);
	}

	private static void ApplyBodyParts(Chara c, MemberData data)
	{
		if (data.bodyParts == null || data.bodyParts.Count == 0)
			return;
		CharacterImporter.ImportBodyParts(c, data.bodyParts);
	}

	private static void ApplyCorruption(Chara c, MemberData data)
	{
		if (data.c_corruptionHistory != null && data.c_corruptionHistory.Count > 0)
			c.c_corruptionHistory = data.c_corruptionHistory.ToList();
		c.corruption = data.corruption;
	}

	private static void ApplyTempElements(Chara c, MemberData data)
	{
		if (data.tempElements == null || data.tempElements.Count == 0)
			return;
		if (c.tempElements == null)
		{
			c.tempElements = new ElementContainer();
			c.tempElements.SetParent(c);
		}
		foreach (ElementData ed in data.tempElements)
			c.tempElements.SetBase(ed.id, ed.vBase, 0);
	}

	private static void ApplyFaith(Chara c, MemberData data)
	{
		if (string.IsNullOrEmpty(data.idFaith))
			return;
		c.idFaith = data.idFaith;
		((Card)c).c_daysWithGod = data.daysWithGod;
		c.RefreshFaithElement();
	}

	private static void ApplyMutations(Chara c, MemberData data)
	{
		if (data.mutations == null || data.mutations.Count == 0)
			return;
		foreach (MutationData m in data.mutations)
			c.SetFeat(m.featId, m.value);
	}

	private static void ApplyGenes(Chara c, MemberData data)
	{
		if (data.genes == null || data.genes.items == null || data.genes.items.Count == 0)
			return;
		CharaGenes genes = new CharaGenes();
		genes.inferior = data.genes.inferior;
		foreach (GeneData gd in data.genes.items)
		{
			DNA dna = new DNA
			{
				id = gd.id,
				ints = gd.ints != null ? (int[])gd.ints.Clone() : null,
				vals = gd.vals != null ? new List<int>(gd.vals) : null
			};
			genes.items.Add(dna);
		}
		c.c_genes = genes;
	}

	/// <summary>
	/// Resolves container parents iteratively so arbitrary nesting depth (backpack > satchel >
	/// potion) reconstructs correctly. Pass 1 spawns top-level items so their containers register
	/// new UIDs. Pass 2 loops until quiescent, picking up items whose parent has just been
	/// resolved. Items whose parent never resolves drop into the chara's main inventory as a
	/// last-resort fallback.
	///
	/// `StorageAuto.SpawnToSubContainer` returns only an int slot, discarding the spawned Card,
	/// so a sub-container spawned by it would never register its new UID. Pass 2 uses
	/// `RestoreThingFromData` + `InsertToSubContainer` directly to keep the Card and register it.
	/// </summary>
	private static void ApplyContainerContents(Chara c, MemberData data, Dictionary<int, int> oldToNewContainerUid)
	{
		if (data.containerContents == null || data.containerContents.Count == 0)
			return;

		Dictionary<int, Card> uidMap = CharaThingsImporter.BuildContainerUidMap(c);
		string memberId = ((Card)c).id;

		foreach (ContainerItemData ci in data.containerContents)
		{
			if (ci.parentContainerUid != 0) continue;
			try
			{
				Card spawned = ThingUtils.RestoreThingFromData(ci.item);
				if (spawned != null)
				{
					c.AddThing(spawned.Thing, tryStack: true);
					CharaThingsImporter.RegisterIfContainer(spawned, ci.item, oldToNewContainerUid, uidMap);
				}
			}
			catch (Exception ex)
			{
				Msg.SayRaw($"NG+: Failed to import inventory item '{ci.item?.id ?? "unknown"}' for member '{memberId}': {ex.Message}");
			}
		}

		List<ContainerItemData> remaining = data.containerContents.Where(ci => ci.parentContainerUid != 0).ToList();
		bool progress = true;
		while (progress && remaining.Count > 0)
		{
			progress = false;
			for (int i = remaining.Count - 1; i >= 0; i--)
			{
				ContainerItemData ci = remaining[i];
				int targetUid = ci.parentContainerUid;
				if (oldToNewContainerUid.TryGetValue(ci.parentContainerUid, out int translated))
					targetUid = translated;

				if (!uidMap.TryGetValue(targetUid, out Card container))
					continue;

				try
				{
					Card spawned = ThingUtils.RestoreThingFromData(ci.item);
					if (spawned != null)
					{
						int slot = StorageAuto.InsertToSubContainer(container, spawned.Thing);
						// On a full or otherwise-failing container, the spawned card is parented but
						// slotless. Route it to the chara's main inventory so it stays visible.
						if (slot < 0)
							c.AddThing(spawned.Thing, tryStack: true);
						CharaThingsImporter.RegisterIfContainer(spawned, ci.item, oldToNewContainerUid, uidMap);
					}
				}
				catch (Exception ex)
				{
					Msg.SayRaw($"NG+: Failed to import inventory item '{ci.item?.id ?? "unknown"}' for member '{memberId}': {ex.Message}");
				}
				remaining.RemoveAt(i);
				progress = true;
			}
		}

		foreach (ContainerItemData ci in remaining)
		{
			try
			{
				Card spawned = ThingUtils.RestoreThingFromData(ci.item);
				if (spawned != null)
				{
					c.AddThing(spawned.Thing, tryStack: true);
					CharaThingsImporter.RegisterIfContainer(spawned, ci.item, oldToNewContainerUid, uidMap);
				}
			}
			catch (Exception ex)
			{
				Msg.SayRaw($"NG+: Failed to import inventory item '{ci.item?.id ?? "unknown"}' for member '{memberId}': {ex.Message}");
			}
		}
	}

	private static void ApplyMarriage(Chara c, MemberData data)
	{
		if (string.IsNullOrEmpty(data.loveDataJson))
			return;
		try
		{
			LoveData love = Newtonsoft.Json.JsonConvert.DeserializeObject<LoveData>(data.loveDataJson);
			if (love == null) return;

			// Reset the one-shot anniversary flag so the carried spouse can give the next gift in NG+.
			love.gotMusicBox = false;

			// Cross-save ref: uidZoneMarriage was the wedding zone uid in save A. Try to
			// resolve it in save B's spatial map. If the uid is dead, fall back to a name
			// lookup against nameZoneMarriage. Log the outcome either way so we know whether
			// the ref carried, was remapped by name, or is genuinely dead.
			ResolveMarriageZoneRef(love, ((Card)c).id);

			((Card)c).c_love = love;

			// Marriage establishment increments Player.stats.married. Carrying a married spouse
			// without bumping the lifetime counter would silently drop it; mirror the increment.
			if (love.dateMarriage != 0 && EClass.player?.stats != null)
				EClass.player.stats.married++;
		}
		catch (System.Exception ex)
		{
			Msg.SayRaw($"NG+: Marriage data malformed for member '{((Card)c).id}', skipping: {ex.Message}");
		}
	}

	/// <summary>
	/// Reattaches the wedding-zone uid to a target that won't dangle in the new save.
	/// nameZoneMarriage stays intact in all paths; the historical-fact string ("married
	/// in Nyaaville Longhouse") is what WindowChara displays, regardless of where the
	/// uid points.
	///
	/// Resolution order:
	///   (1) Carried uid resolves AND the resolved zone's Name == carried name -> rare
	///       lottery, ref carried verbatim.
	///   (2) Carried uid resolves to a non-random zone (permanent template) -> leave
	///       uid alone. Even with a name mismatch, the target is stable: won't be
	///       garbage-collected, won't crash if anything ever dereferences.
	///   (3) Carried uid is dead OR points to a random zone (which can be destroyed /
	///       abandoned) -> redirect uid to PC's spawn zone so it always points at
	///       something stable.
	/// </summary>
	private static void ResolveMarriageZoneRef(LoveData love, string memberId)
	{
		if (love == null) return;
		if (EClass.game?.spatials == null) return;

		int oldUid = love.uidZoneMarriage;
		string carriedName = love.nameZoneMarriage;

		Spatial direct = EClass.game.spatials.Find(oldUid);
		Zone directZone = direct as Zone;
		if (directZone != null)
		{
			bool nameMatches = !string.IsNullOrEmpty(carriedName) && directZone.Name == carriedName;
			if (nameMatches)
			{
				DebugLogger.DebugLog("PartyMemberImporter.ResolveMarriageZoneRef", "uid resolves and name matches", null, new Dictionary<string, object>
				{
					{ "memberId", memberId ?? "?" },
					{ "uid", oldUid },
					{ "name", carriedName },
					{ "resolvedZoneId", directZone.id ?? "" }
				});
				return;
			}

			if (!directZone.isRandomSite)
			{
				DebugLogger.DebugLog("PartyMemberImporter.ResolveMarriageZoneRef", "uid points to non-random zone, kept (stable target)", null, new Dictionary<string, object>
				{
					{ "memberId", memberId ?? "?" },
					{ "uid", oldUid },
					{ "carriedName", carriedName ?? "" },
					{ "resolvedZoneName", directZone.Name ?? "" },
					{ "resolvedZoneId", directZone.id ?? "" }
				});
				return;
			}

			DebugLogger.DebugLog("PartyMemberImporter.ResolveMarriageZoneRef", "uid points to random zone (unstable), redirecting to spawn", null, new Dictionary<string, object>
			{
				{ "memberId", memberId ?? "?" },
				{ "uid", oldUid },
				{ "carriedName", carriedName ?? "" },
				{ "resolvedZoneName", directZone.Name ?? "" },
				{ "resolvedZoneId", directZone.id ?? "" }
			});
		}

		Zone spawnZone = EClass._zone;
		if (spawnZone == null)
		{
			DebugLogger.DebugLog("PartyMemberImporter.ResolveMarriageZoneRef", "ref dead and no spawn zone available, leaving uid as-is", null, new Dictionary<string, object>
			{
				{ "memberId", memberId ?? "?" },
				{ "oldUid", oldUid },
				{ "carriedName", carriedName ?? "" }
			});
			return;
		}

		love.uidZoneMarriage = spawnZone.uid;
		DebugLogger.DebugLog("PartyMemberImporter.ResolveMarriageZoneRef", "redirected to spawn zone", null, new Dictionary<string, object>
		{
			{ "memberId", memberId ?? "?" },
			{ "carriedName", carriedName ?? "" },
			{ "oldUid", oldUid },
			{ "newUid", spawnZone.uid },
			{ "spawnZoneId", spawnZone.id ?? "" },
			{ "spawnZoneName", spawnZone.Name ?? "" }
		});
	}
}
