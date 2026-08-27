using System;
using System.Text;
using LiteDB;
using Newtonsoft.Json;

namespace RuriLib.Models;

public class Hit : Persistable<Guid>
{
	public string Data { get; set; }

	public VariableList CapturedData { get; set; }

	[JsonIgnore]
	[BsonIgnore]
	public string CapturedString => CapturedData.ToCaptureString();

	public string Proxy { get; set; }

	public DateTime Date { get; set; }

	public string Type { get; set; }

	public string ConfigName { get; set; }

	public string WordlistName { get; set; }

	public Hit()
	{
	}

	public Hit(string data, VariableList capturedData, string proxy, string type, string configName, string wordlistName)
	{
		Data = data;
		CapturedData = capturedData;
		Proxy = proxy;
		Date = DateTime.Now;
		Type = type;
		ConfigName = configName;
		WordlistName = wordlistName;
	}

	public string ToFormattedString(string format)
	{
		StringBuilder stringBuilder = new StringBuilder(format).Replace("<DATA>", Data).Replace("<PROXY>", Proxy).Replace("<DATE>", Date.ToLongDateString() + " " + Date.ToLongTimeString())
			.Replace("<CONFIG>", ConfigName)
			.Replace("<WORDLIST>", WordlistName)
			.Replace("<TYPE>", Type)
			.Replace("<CAPTURE>", CapturedData.ToCaptureString());
		foreach (CVar item in CapturedData.All)
		{
			stringBuilder.Replace("<" + item.Name + ">", item.ToString());
		}
		return stringBuilder.ToString();
	}

	public int GetHashCode(bool ignoreWordlistName = true)
	{
		return (ignoreWordlistName ? (Data + ConfigName) : (Data + ConfigName + WordlistName)).GetHashCode();
	}
}
