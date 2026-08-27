using System;

namespace RuriLib.Models;

public class WebhookFormat
{
	public string Data { get; set; }

	public string Type { get; set; }

	public string CapturedData { get; set; }

	public double Timestamp { get; set; }

	public string ConfigName { get; set; }

	public string ConfigAuthor { get; set; }

	public string User { get; set; }

	public WebhookFormat(string data, string type, string capturedData, DateTime timestamp, string configName, string configAuthor, string user)
	{
		Data = data;
		Type = type;
		CapturedData = capturedData;
		Timestamp = Math.Round(timestamp.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
		ConfigName = configName;
		ConfigAuthor = configAuthor;
		User = user;
	}
}
