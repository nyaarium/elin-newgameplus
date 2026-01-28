using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace NewGamePlus;

public static class DumpSerializer
{
	public static string SerializeDumpData(CharacterDumpData data)
	{
		using (MemoryStream stream = new MemoryStream())
		{
			DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(CharacterDumpData));
			serializer.WriteObject(stream, data);
			stream.Position = 0;
			using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
			{
				return reader.ReadToEnd();
			}
		}
	}

	public static CharacterDumpData DeserializeDumpData(string json)
	{
		using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
		{
			DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(CharacterDumpData));
			return (CharacterDumpData)serializer.ReadObject(stream);
		}
	}

	public static CharacterDumpData LoadDumpData(string dumpFilePath)
	{
		if (dumpFilePath == null || !File.Exists(dumpFilePath))
		{
			return null;
		}
		string jsonLine = File.ReadAllText(dumpFilePath);
		try
		{
			CharacterDumpData dumpData = DeserializeDumpData(jsonLine);
			return dumpData;
		}
		catch (Exception)
		{
			return null;
		}
	}
}
