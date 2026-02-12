using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewGamePlus;

public static class DebugLogger
{
	private static string SerializeValue(object value)
	{
		if (value == null) return "null";
		if (value is string str) return "\"" + str.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
		if (value is bool b) return b ? "true" : "false";
		if (value is int || value is long || value is short || value is byte) return value.ToString();
		if (value is float f) return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
		if (value is double d) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
		if (value is Dictionary<string, object> dict)
		{
			var sb = new StringBuilder("{");
			var first = true;
			foreach (var kvp in dict)
			{
				if (!first) sb.Append(",");
				first = false;
				sb.Append($"\"{kvp.Key}\":{SerializeValue(kvp.Value)}");
			}
			sb.Append("}");
			return sb.ToString();
		}
		if (value is System.Collections.IEnumerable enumerable && !(value is string))
		{
			var sb = new StringBuilder("[");
			var first = true;
			foreach (var item in enumerable)
			{
				if (!first) sb.Append(",");
				first = false;
				sb.Append(SerializeValue(item));
			}
			sb.Append("]");
			return sb.ToString();
		}
		// Fallback: try to convert to string and escape
		return "\"" + value.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
	}

	public static void DebugLog(string location, string message, string hypothesisId = null, Dictionary<string, object> data = null)
	{
		try
		{
			var logPath = @"s:\Steam\steamapps\common\Elin\NewGamePlus\.cursor\debug.log";
			var logDir = Path.GetDirectoryName(logPath);
			if (!Directory.Exists(logDir))
			{
				Directory.CreateDirectory(logDir);
			}

			var timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
			var id = $"log_{timestamp}_{Guid.NewGuid().ToString().Substring(0, 8)}";
			var dataJson = data != null ? SerializeValue(data) : "{}";
			var escapedMessage = message.Replace("\\", "\\\\").Replace("\"", "\\\"");
			var logLine = $"{{\"id\":\"{id}\",\"timestamp\":{timestamp},\"location\":\"{location}\",\"message\":\"{escapedMessage}\",\"data\":{dataJson},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"{hypothesisId ?? ""}\"}}";
			File.AppendAllText(logPath, logLine + Environment.NewLine);
		}
		catch
		{
			// Silently fail - don't break the game if logging fails
		}
	}
}
