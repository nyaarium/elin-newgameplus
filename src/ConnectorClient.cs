#if DEVMODE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using HarmonyLib;

namespace NewGamePlus;

/// <summary>
/// WebSocket client that connects to the agent-team-bridge connector and handles tool invocations.
/// Connects to ws://localhost:20000/ws. The connector routes MCP tool calls from IDE agents.
/// All reads are from already-initialized static SourceThing tables (no mutation).
/// </summary>
public static class ConnectorClient
{
	const string ConnectorUrl = "ws://localhost:20000/connector/elin-newgameplus/ws";
	const int ReconnectDelayMs = 3000;
	const int ReceiveBufferSize = 65536;

	static readonly ConcurrentQueue<Action> _pendingActions = new ConcurrentQueue<Action>();

	static ClientWebSocket _ws;
	static Thread _thread;
	static volatile bool _running;

	public static void Start()
	{
		if (_running) return;

		_running = true;
		_thread = new Thread(ConnectionLoop) { IsBackground = true, Name = "ConnectorClient" };
		_thread.Start();
	}

	public static void DrainActions()
	{
		while (_pendingActions.TryDequeue(out Action action))
			action();
	}

	public static void Stop()
	{
		_running = false;
		try { _ws?.Abort(); } catch { }
	}

	static void ConnectionLoop()
	{
		while (_running)
		{
			try
			{
				using (var ws = new ClientWebSocket())
				{
					_ws = ws;
					ws.ConnectAsync(new Uri(ConnectorUrl), CancellationToken.None).GetAwaiter().GetResult();

					var buffer = new byte[ReceiveBufferSize];
					while (_running && ws.State == WebSocketState.Open)
					{
						var result = ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).GetAwaiter().GetResult();

						if (result.MessageType == WebSocketMessageType.Close)
							break;

						if (result.MessageType == WebSocketMessageType.Text)
						{
							string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
							HandleMessage(ws, message);
						}
					}
				}
			}
			catch (Exception ex)
			{
			}

			_ws = null;
			if (_running)
			{
				Thread.Sleep(ReconnectDelayMs);
			}
		}
	}

	static void HandleMessage(ClientWebSocket ws, string message)
	{
		string type = JsonReadString(message, "type");
		if (type != "invoke") return;

		string id = JsonReadString(message, "id") ?? "";
		string tool = JsonReadString(message, "tool") ?? "";

		// Extract the args sub-object
		int argsIdx = message.IndexOf("\"args\"", StringComparison.Ordinal);
		string argsJson = "{}";
		if (argsIdx >= 0)
		{
			int braceStart = message.IndexOf('{', argsIdx);
			if (braceStart >= 0)
			{
				int depth = 0;
				int end = braceStart;
				for (int i = braceStart; i < message.Length; i++)
				{
					if (message[i] == '{') depth++;
					else if (message[i] == '}') depth--;
					if (depth == 0) { end = i; break; }
				}
				argsJson = message.Substring(braceStart, end - braceStart + 1);
			}
		}

		string responseBody;
		try
		{
			switch (tool)
			{
				case "elinGameStatus":      responseBody = HandleStatus(); break;
				case "elinSearchItems":     responseBody = HandleSearchItems(argsJson); break;
				case "elinGetItem":         responseBody = HandleGetItem(argsJson); break;
				case "elinListCategories":  responseBody = HandleListCategories(); break;
				case "elinGiveItem":        responseBody = HandleGiveItem(argsJson); break;
				case "elinSearchElements":  responseBody = HandleSearchElements(argsJson); break;
				case "elinElement":         responseBody = HandleElement(argsJson); break;
				case "elinFeat":            responseBody = HandleFeat(argsJson); break;
				case "elinStat":            responseBody = HandleStat(argsJson); break;
				case "elinGenes":           responseBody = HandleGenes(); break;
				default:
					responseBody = "{\"error\":\"Unknown tool: " + Esc(tool) + "\"}";
					break;
			}
		}
		catch (Exception ex)
		{
			responseBody = Json("error", ex.Message);
		}

		SendResult(ws, id, responseBody);
	}

	static void SendResult(ClientWebSocket ws, string id, string dataJson)
	{
		string response = "{\"type\":\"result\",\"id\":\"" + Esc(id) + "\",\"data\":" + dataJson + "}";
		byte[] buf = Encoding.UTF8.GetBytes(response);
		try
		{
			ws.SendAsync(new ArraySegment<byte>(buf), WebSocketMessageType.Text, true, CancellationToken.None).GetAwaiter().GetResult();
		}
		catch { }
	}

	// --- Handlers ---

	static string HandleStatus()
	{
		bool sourcesReady = EClass.sources != null && EClass.sources.things != null;
		int itemCount = sourcesReady ? EClass.sources.things.rows.Count : 0;
		return "{\"ok\":true,\"itemCount\":" + itemCount + "}";
	}

	static string HandleSearchItems(string reqBody)
	{
		string query = JsonReadString(reqBody, "query") ?? "";
		int limit = 25;
		string limitStr = JsonReadString(reqBody, "limit");
		if (limitStr != null && int.TryParse(limitStr, out int parsedLimit) && parsedLimit > 0)
			limit = parsedLimit;

		var things = EClass.sources.things;

		if (string.IsNullOrEmpty(query))
		{
			// No query - return first N items
			var results = new List<string>();
			foreach (var row in things.rows)
			{
				if (results.Count >= limit) break;
				results.Add(ItemSummaryJson(row, 0));
			}
			return "{\"items\":[" + string.Join(",", results) + "]}";
		}

		string queryLower = query.ToLowerInvariant();

		// Score each row and collect those with score > 0
		var scored = new List<KeyValuePair<int, SourceThing.Row>>();
		foreach (var row in things.rows)
		{
			int score = ItemSearchScore(row, queryLower);
			if (score > 0)
				scored.Add(new KeyValuePair<int, SourceThing.Row>(score, row));
		}

		// Sort descending by score
		scored.Sort((a, b) => b.Key.CompareTo(a.Key));

		var ranked = new List<string>();
		for (int i = 0; i < scored.Count && ranked.Count < limit; i++)
			ranked.Add(ItemSummaryJson(scored[i].Value, scored[i].Key));

		return "{\"items\":[" + string.Join(",", ranked) + "]}";
	}

	/// <summary>
	/// Returns a relevance score > 0 if the row matches the query.
	/// Scoring tiers:
	///   100 = exact ID match
	///    90 = ID starts with query
	///    70 = substring match in id, name, name_JP, or category
	///   10-40 = fuzzy Levenshtein match on words (distance 1-4)
	/// </summary>
	static int ItemSearchScore(SourceThing.Row row, string queryLower)
	{
		string id       = row.id.ToLowerInvariant();
		string name     = (row.name ?? "").ToLowerInvariant();
		string nameJP   = row.name_JP ?? "";
		string category = (row.category ?? "").ToLowerInvariant();

		// Exact ID match
		if (id == queryLower) return 100;

		// ID starts with query
		if (id.StartsWith(queryLower, StringComparison.Ordinal)) return 90;

		// Substring match in any field
		if (id.IndexOf(queryLower, StringComparison.Ordinal) >= 0)       return 70;
		if (name.IndexOf(queryLower, StringComparison.Ordinal) >= 0)     return 70;
		if (nameJP.IndexOf(queryLower, StringComparison.Ordinal) >= 0)   return 70;
		if (category.IndexOf(queryLower, StringComparison.Ordinal) >= 0) return 70;

		// Fuzzy Levenshtein on individual words (split by space/underscore)
		int minDist = MinWordDistance(id, queryLower);
		int d = MinWordDistance(name, queryLower);
		if (d < minDist) minDist = d;

		if (minDist <= 3) return 50 - (minDist * 10); // dist 1->40, 2->30, 3->20
		if (minDist == 4) return 10;

		return 0;
	}

	/// <summary>
	/// Splits text by spaces and underscores and returns the minimum Levenshtein distance
	/// between any word and the query. Falls back to full-string distance.
	/// </summary>
	static int MinWordDistance(string text, string query)
	{
		if (string.IsNullOrEmpty(text)) return int.MaxValue;
		string[] words = text.Split(new char[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
		int min = Levenshtein(text, query);
		foreach (string word in words)
		{
			int d = Levenshtein(word, query);
			if (d < min) min = d;
		}
		return min;
	}

	static int Levenshtein(string a, string b)
	{
		if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
		if (string.IsNullOrEmpty(b)) return a.Length;

		int[,] d = new int[a.Length + 1, b.Length + 1];
		for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
		for (int j = 0; j <= b.Length; j++) d[0, j] = j;

		for (int i = 1; i <= a.Length; i++)
			for (int j = 1; j <= b.Length; j++)
			{
				int cost = char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;
				d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
			}

		return d[a.Length, b.Length];
	}

	static string HandleGetItem(string reqBody)
	{
		string id = JsonReadString(reqBody, "id") ?? "";
		if (string.IsNullOrEmpty(id))
			return "{\"error\":\"id required\"}";

		var things = EClass.sources.things;
		if (!things.map.TryGetValue(id, out var row))
			return "{\"error\":\"not found\"}";

		return ItemDetailJson(row);
	}

	static string HandleListCategories()
	{
		var seen = new HashSet<string>();
		var cats = new List<string>();
		foreach (var row in EClass.sources.things.rows)
		{
			if (!string.IsNullOrEmpty(row.category) && seen.Add(row.category))
				cats.Add("\"" + Esc(row.category) + "\"");
		}
		return "{\"categories\":[" + string.Join(",", cats) + "]}";
	}

	static string HandleGiveItem(string reqBody)
	{
		string id = JsonReadString(reqBody, "id") ?? "";
		if (string.IsNullOrEmpty(id))
			return "{\"error\":\"id required\"}";

		int count = 1;
		string countStr = JsonReadString(reqBody, "count");
		if (countStr != null && int.TryParse(countStr, out int parsed) && parsed > 0)
			count = parsed;

		string resultJson = null;
		var done = new ManualResetEventSlim(false);

		_pendingActions.Enqueue(() =>
		{
			try
			{
				if (!EClass.sources.cards.map.ContainsKey(id))
				{
					resultJson = "{\"error\":\"item not found: " + Esc(id) + "\"}";
					return;
				}
				Thing t = ThingGen.Create(id);
				if (count > 1) t.SetNum(count);
				EClass.pc.Pick(t);
				resultJson = "{\"ok\":true,\"id\":\"" + Esc(id) + "\",\"name\":\"" + Esc(t.Name ?? "") + "\",\"count\":" + count + "}";
			}
			catch (Exception ex)
			{
				resultJson = "{\"error\":\"" + Esc(ex.Message) + "\"}";
			}
			finally
			{
				done.Set();
			}
		});

		if (!done.Wait(TimeSpan.FromSeconds(5)))
			return "{\"error\":\"timeout - game may be paused or loading\"}";

		return resultJson;
	}

	static string HandleSearchElements(string reqBody)
	{
		string query = JsonReadString(reqBody, "query") ?? "";
		string category = JsonReadString(reqBody, "category");

		string queryLower = query.ToLowerInvariant();
		string catLower = category?.ToLowerInvariant();

		var results = new List<string>();
		foreach (var row in EClass.sources.elements.rows)
		{
			if (results.Count >= 25) break;

			if (catLower != null)
			{
				string rowCat = (row.category ?? "").ToLowerInvariant();
				if (rowCat != catLower) continue;
			}

			if (!string.IsNullOrEmpty(queryLower))
			{
				string rowIdStr = row.id.ToString();
				string rowName  = (row.name ?? "").ToLowerInvariant();
				string rowCat   = (row.category ?? "").ToLowerInvariant();
				if (rowIdStr.IndexOf(queryLower, StringComparison.Ordinal) < 0
				 && rowName.IndexOf(queryLower, StringComparison.Ordinal) < 0
				 && rowCat.IndexOf(queryLower, StringComparison.Ordinal) < 0)
					continue;
			}

			string tagsJson = row.tag != null && row.tag.Length > 0
				? "[\"" + string.Join("\",\"", row.tag) + "\"]"
				: "[]";

			results.Add("{\"id\":" + row.id
				+ ",\"name\":\"" + Esc(row.name ?? "") + "\""
				+ ",\"category\":\"" + Esc(row.category ?? "") + "\""
				+ ",\"group\":\"" + Esc(row.group ?? "") + "\""
				+ ",\"tags\":" + tagsJson + "}");
		}

		return "{\"elements\":[" + string.Join(",", results) + "]}";
	}

	static string HandleElement(string reqBody)
	{
		string elementIdStr = JsonReadString(reqBody, "elementId");
		if (elementIdStr == null || !int.TryParse(elementIdStr, out int elementId))
			return "{\"error\":\"elementId required\"}";

		string valueStr = JsonReadString(reqBody, "value");

		if (valueStr == null)
		{
			// GET: read-only, safe on WebSocket thread
			var c = EClass.pc;
			var el = c.elements.GetElement(elementId);
			if (el == null)
				return "{\"found\":false}";

			return "{\"found\":true,\"id\":" + el.id
				+ ",\"vBase\":" + el.vBase
				+ ",\"vSource\":" + el.vSource
				+ ",\"vLink\":" + el.vLink
				+ ",\"value\":" + el.Value
				+ ",\"vExp\":" + el.vExp
				+ ",\"vPotential\":" + el.vPotential + "}";
		}

		// SET: must run on main thread
		if (!int.TryParse(valueStr, out int newValue))
			return "{\"error\":\"value must be a number\"}";

		string resultJson = null;
		var done = new ManualResetEventSlim(false);

		_pendingActions.Enqueue(() =>
		{
			try
			{
				var c = EClass.pc;
				c.elements.SetBase(elementId, newValue);
				c.Refresh();

				var el = c.elements.GetElement(elementId);
				if (el == null)
					resultJson = "{\"found\":false}";
				else
					resultJson = "{\"found\":true,\"id\":" + el.id
						+ ",\"vBase\":" + el.vBase
						+ ",\"vSource\":" + el.vSource
						+ ",\"vLink\":" + el.vLink
						+ ",\"value\":" + el.Value
						+ ",\"vExp\":" + el.vExp
						+ ",\"vPotential\":" + el.vPotential + "}";
			}
			catch (Exception ex)
			{
				resultJson = "{\"error\":\"" + Esc(ex.Message) + "\"}";
			}
			finally
			{
				done.Set();
			}
		});

		if (!done.Wait(TimeSpan.FromSeconds(5)))
			return "{\"error\":\"timeout - game may be paused or loading\"}";

		return resultJson;
	}

	static string HandleFeat(string reqBody)
	{
		string featIdStr = JsonReadString(reqBody, "featId");
		if (featIdStr == null || !int.TryParse(featIdStr, out int featId))
			return "{\"error\":\"featId required\"}";

		string tierStr = JsonReadString(reqBody, "tier");

		// Helper to build feat JSON from element
		Func<Element, string> featJson = (el) =>
		{
			if (el == null) return "{\"found\":false}";

			SourceElement.Row src = null;
			EClass.sources.elements.map.TryGetValue(el.id, out src);

			string catStr  = src != null ? Esc(src.category ?? "") : "";
			bool innate    = src != null && src.tag != null && Array.IndexOf(src.tag, "innate") >= 0;
			int maxTier    = src?.max ?? 0;

			return "{\"found\":true,\"id\":" + el.id
				+ ",\"vBase\":" + el.vBase
				+ ",\"vSource\":" + el.vSource
				+ ",\"vLink\":" + el.vLink
				+ ",\"value\":" + el.Value
				+ ",\"vExp\":" + el.vExp
				+ ",\"vPotential\":" + el.vPotential
				+ ",\"category\":\"" + catStr + "\""
				+ ",\"innate\":" + (innate ? "true" : "false")
				+ ",\"maxTier\":" + maxTier + "}";
		};

		if (tierStr == null)
		{
			// GET: read-only
			var el = EClass.pc.elements.GetElement(featId);
			return featJson(el);
		}

		// SET: must run on main thread
		if (!int.TryParse(tierStr, out int tier))
			return "{\"error\":\"tier must be a number\"}";

		string resultJson = null;
		var done = new ManualResetEventSlim(false);

		_pendingActions.Enqueue(() =>
		{
			try
			{
				var c = EClass.pc;
				c.SetFeat(featId, tier, msg: true);
				c.Refresh();

				var el = c.elements.GetElement(featId);
				resultJson = featJson(el);
			}
			catch (Exception ex)
			{
				resultJson = "{\"error\":\"" + Esc(ex.Message) + "\"}";
			}
			finally
			{
				done.Set();
			}
		});

		if (!done.Wait(TimeSpan.FromSeconds(5)))
			return "{\"error\":\"timeout - game may be paused or loading\"}";

		return resultJson;
	}

	static string HandleStat(string reqBody)
	{
		string field = JsonReadString(reqBody, "field") ?? "";
		string value = JsonReadString(reqBody, "value");
		var c = EClass.pc;

		if (string.IsNullOrEmpty(field))
		{
			// Return all fields as summary
			return "{\"level\":" + ((Card)c).LV
				+ ",\"feat\":" + ((Card)c).feat
				+ ",\"race\":\"" + Esc(((Card)c).c_idRace ?? "") + "\""
				+ ",\"job\":\"" + Esc(((Card)c).c_idJob ?? "") + "\""
				+ ",\"hp\":" + c.hp
				+ ",\"karma\":" + EClass.player.karma
				+ ",\"fame\":" + EClass.player.fame
				+ ",\"corruption\":" + c.corruption
				+ ",\"faith\":\"" + Esc(c.idFaith ?? "") + "\""
				+ ",\"exp\":" + ((Card)c).exp + "}";
		}

		if (value == null)
		{
			// GET single field
			switch (field)
			{
				case "level":      return "{\"field\":\"level\",\"value\":" + ((Card)c).LV + "}";
				case "feat":       return "{\"field\":\"feat\",\"value\":" + ((Card)c).feat + "}";
				case "race":       return "{\"field\":\"race\",\"value\":\"" + Esc(((Card)c).c_idRace ?? "") + "\"}";
				case "job":        return "{\"field\":\"job\",\"value\":\"" + Esc(((Card)c).c_idJob ?? "") + "\"}";
				case "hp":         return "{\"field\":\"hp\",\"value\":" + c.hp + "}";
				case "karma":      return "{\"field\":\"karma\",\"value\":" + EClass.player.karma + "}";
				case "fame":       return "{\"field\":\"fame\",\"value\":" + EClass.player.fame + "}";
				case "corruption": return "{\"field\":\"corruption\",\"value\":" + c.corruption + "}";
				case "faith":      return "{\"field\":\"faith\",\"value\":\"" + Esc(c.idFaith ?? "") + "\"}";
				case "exp":        return "{\"field\":\"exp\",\"value\":" + ((Card)c).exp + "}";
				default:           return "{\"error\":\"unknown field: " + Esc(field) + "\"}";
			}
		}

		// SET: must run on main thread
		string resultJson = null;
		var done = new ManualResetEventSlim(false);

		_pendingActions.Enqueue(() =>
		{
			try
			{
				switch (field)
				{
					case "level":
					{
						int targetLevel = int.Parse(value);
						int currentLevel = ((Card)c).LV;
						int delta = targetLevel - currentLevel;
						if (delta > 0)
						{
							for (int i = 0; i < delta; i++)
								((Card)c).LevelUp();
						}
						else if (delta < 0)
						{
							// No LevelDown exists, so set directly and adjust feat points
							((Card)c).SetLv(targetLevel);
							((Card)c).feat += delta; // remove feat points for lost levels
						}
						c.Refresh();
						resultJson = "{\"field\":\"level\",\"value\":" + ((Card)c).LV + ",\"feat\":" + ((Card)c).feat + "}";
						break;
					}
					case "feat":
						resultJson = "{\"error\":\"feat points are read-only, managed by the level system\"}";
						break;
					case "race":
						c.ChangeRace(value);
						EMono.player.RefreshDomain();
						c.Refresh();
						resultJson = "{\"field\":\"race\",\"value\":\"" + Esc(((Card)c).c_idRace ?? "") + "\"}";
						break;
					case "job":
						c.ChangeJob(value);
						EMono.player.RefreshDomain();
						c.Refresh();
						resultJson = "{\"field\":\"job\",\"value\":\"" + Esc(((Card)c).c_idJob ?? "") + "\"}";
						break;
					case "hp":
					{
						int hp = int.Parse(value);
						if (hp > c.MaxHP) hp = c.MaxHP;
						c.hp = hp;
						resultJson = "{\"field\":\"hp\",\"value\":" + c.hp + "}";
						break;
					}
					case "karma":
						EClass.player.karma = int.Parse(value);
						resultJson = "{\"field\":\"karma\",\"value\":" + EClass.player.karma + "}";
						break;
					case "fame":
						EClass.player.fame = int.Parse(value);
						resultJson = "{\"field\":\"fame\",\"value\":" + EClass.player.fame + "}";
						break;
					case "corruption":
						c.corruption = int.Parse(value);
						resultJson = "{\"field\":\"corruption\",\"value\":" + c.corruption + "}";
						break;
					case "faith":
						c.SetFaith(value);
						resultJson = "{\"field\":\"faith\",\"value\":\"" + Esc(c.idFaith ?? "") + "\"}";
						break;
					case "exp":
						((Card)c).exp = int.Parse(value);
						resultJson = "{\"field\":\"exp\",\"value\":" + ((Card)c).exp + "}";
						break;
					default:
						resultJson = "{\"error\":\"unknown field: " + Esc(field) + "\"}";
						break;
				}
			}
			catch (Exception ex)
			{
				resultJson = "{\"error\":\"" + Esc(ex.Message) + "\"}";
			}
			finally
			{
				done.Set();
			}
		});

		if (!done.Wait(TimeSpan.FromSeconds(5)))
			return "{\"error\":\"timeout - game may be paused or loading\"}";

		return resultJson;
	}

	static string HandleGenes()
	{
		var c = EClass.pc;
		var genes = c.c_genes;

		var geneList = new List<string>();

		if (genes != null)
		{
			foreach (var dna in genes.items)
			{
				if (dna == null) continue;

				var valEntries = new List<string>();
				if (dna.vals != null)
				{
					for (int i = 0; i + 1 < dna.vals.Count; i += 2)
					{
						int eid   = dna.vals[i];
						int eval  = dna.vals[i + 1];
						string ename = "";
						if (EClass.sources.elements.map.TryGetValue(eid, out var erow))
							ename = erow.name ?? "";
						valEntries.Add("{\"elementId\":" + eid + ",\"elementName\":\"" + Esc(ename) + "\",\"value\":" + eval + "}");
					}
				}

				geneList.Add("{\"id\":\"" + Esc(dna.id ?? "") + "\""
					+ ",\"type\":\"" + Esc(dna.type.ToString()) + "\""
					+ ",\"cost\":" + dna.cost
					+ ",\"slot\":" + dna.slot
					+ ",\"vals\":[" + string.Join(",", valEntries) + "]}");
			}
		}

		int inferior    = genes?.inferior ?? 0;
		int currentSlot = c.CurrentGeneSlot;
		int maxSlot     = c.MaxGeneSlot;

		return "{\"genes\":[" + string.Join(",", geneList) + "]"
			+ ",\"inferior\":" + inferior
			+ ",\"currentSlot\":" + currentSlot
			+ ",\"maxSlot\":" + maxSlot + "}";
	}

	// --- JSON helpers ---

	static string ItemSummaryJson(SourceThing.Row row, int score)
	{
		return "{\"id\":\"" + Esc(row.id) + "\",\"name\":\"" + Esc(row.name ?? "") + "\",\"name_JP\":\"" + Esc(row.name_JP ?? "") + "\",\"category\":\"" + Esc(row.category ?? "") + "\",\"value\":" + row.value + ",\"LV\":" + row.LV + ",\"score\":" + score + "}";
	}

	static string ItemDetailJson(SourceThing.Row row)
	{
		var sb = new StringBuilder();
		sb.Append("{");
		sb.Append("\"id\":\"").Append(Esc(row.id)).Append("\",");
		sb.Append("\"category\":\"").Append(Esc(row.category ?? "")).Append("\",");
		sb.Append("\"value\":").Append(row.value).Append(",");
		sb.Append("\"LV\":").Append(row.LV).Append(",");
		sb.Append("\"weight\":").Append(row.weight).Append(",");
		sb.Append("\"name\":\"").Append(Esc(row.name ?? "")).Append("\",");
		sb.Append("\"name_JP\":\"").Append(Esc(row.name_JP ?? "")).Append("\",");
		sb.Append("\"trait\":").Append(row.trait != null ? "[\"" + string.Join("\",\"", row.trait) + "\"]" : "null").Append(",");
		sb.Append("\"components\":").Append(row.components != null ? "[\"" + string.Join("\",\"", row.components) + "\"]" : "null");
		sb.Append("}");
		return sb.ToString();
	}

	static string Json(string key, string value)
	{
		return "{\"" + key + "\":\"" + Esc(value) + "\"}";
	}

	/// <summary>
	/// Minimal JSON string field reader for flat JSON objects.
	/// Handles both string and number values (returns as string for numbers).
	/// </summary>
	static string JsonReadString(string json, string key)
	{
		if (string.IsNullOrEmpty(json)) return null;
		string searchKey = "\"" + key + "\"";
		int keyIdx = json.IndexOf(searchKey, StringComparison.Ordinal);
		if (keyIdx < 0) return null;
		int colonIdx = json.IndexOf(':', keyIdx + searchKey.Length);
		if (colonIdx < 0) return null;
		int valueStart = colonIdx + 1;
		while (valueStart < json.Length && json[valueStart] == ' ') valueStart++;
		if (valueStart >= json.Length) return null;

		if (json[valueStart] == '"')
		{
			// String value
			int end = json.IndexOf('"', valueStart + 1);
			if (end < 0) return null;
			return json.Substring(valueStart + 1, end - valueStart - 1);
		}
		else
		{
			// Number or other literal - read until delimiter
			int end = valueStart;
			while (end < json.Length && json[end] != ',' && json[end] != '}' && json[end] != ' ')
				end++;
			return json.Substring(valueStart, end - valueStart);
		}
	}

	static string Esc(string s)
	{
		return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
	}
}

/// <summary>
/// Drains the ConnectorClient action queue on the main thread each frame.
/// Required so that elinGiveItem (and any future main-thread actions) execute safely.
/// </summary>
[HarmonyPatch]
public static class ConnectorClientDrainPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Core), nameof(Core.Update))]
	static void Postfix()
	{
		ConnectorClient.DrainActions();
	}
}
#endif
