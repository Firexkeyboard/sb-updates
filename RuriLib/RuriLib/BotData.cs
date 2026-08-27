using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using OpenQA.Selenium;
using RuriLib.Models;
using RuriLib.Runner;
using RuriLib.SB.JS;
using RuriLib.ViewModels;
using SilverBullet.Tesseract;

namespace RuriLib;

public class BotData
{
	public Random random;

	public string CustomStatus = "CUSTOM";

	public int BotsAmount { get; set; }

	public BotStatus Status { get; set; }

	public string StatusString
	{
		get
		{
			if (Status != BotStatus.CUSTOM)
			{
				return Status.ToString();
			}
			return CustomStatus;
		}
	}

	public int BotNumber { get; set; }

	public WebDriver Driver { get; set; }

	public bool BrowserOpen { get; set; }

	public Dictionary<string, object> CustomObjects { get; set; }

	public float OcrRate { get; set; }

	public CData Data { get; set; }

	public CProxy Proxy { get; set; }

	public bool UseProxies { get; set; }

	public RLSettingsViewModel GlobalSettings { get; set; }

	public ConfigSettings ConfigSettings { get; set; }

	public decimal Balance { get; set; }

	public List<string> Screenshots { get; set; }

	public bool IsImage { get; set; }

	public string Address
	{
		get
		{
			return Variables.Get("ADDRESS").Value;
		}
		set
		{
			Variables.SetHidden("ADDRESS", value);
		}
	}

	public string ResponseCode
	{
		get
		{
			return Variables.Get("RESPONSECODE").Value;
		}
		set
		{
			Variables.SetHidden("RESPONSECODE", value);
		}
	}

	public Dictionary<string, string> ResponseHeaders
	{
		get
		{
			return Variables.Get("HEADERS").Value;
		}
		set
		{
			Variables.SetHidden("HEADERS", value);
		}
	}

	public Dictionary<string, string> Cookies
	{
		get
		{
			return Variables.Get("COOKIES").Value;
		}
		set
		{
			Variables.SetHidden("COOKIES", value);
		}
	}

	public string ResponseSource
	{
		get
		{
			return Variables.Get("SOURCE").Value;
		}
		set
		{
			Variables.SetHidden("SOURCE", value);
		}
	}

	public Stream ResponseStream { get; set; } = Stream.Null;

	/// <summary>Raw bytes of the last HTTP response body (for RAWSOURCE in LoliCode/LoliScript).</summary>
	public byte[] RawSourceBytes { get; set; }

	public VariableList Variables { get; set; }

	public VariableList GlobalVariables { get; set; }

	public Dictionary<string, string> GlobalCookies { get; set; }

	public List<LogEntry> LogBuffer { get; set; }

	public bool IsDebug { get; set; }

	public OcrEngine OcrEngine { get; set; }

	public JsEngine JsEngine { get; set; }

	public AbortableBackgroundWorker Worker { get; set; }

	public BotData(RLSettingsViewModel globalSettings, ConfigSettings configSettings, CData data, CProxy proxy, bool useProxies, Random random, int botNumber = 0, bool isDebug = true)
	{
		Data = data;
		Proxy = proxy;
		UseProxies = useProxies;
		this.random = new Random(random.Next(0, int.MaxValue));
		Status = BotStatus.NONE;
		GlobalSettings = globalSettings;
		ConfigSettings = configSettings;
		Balance = 0m;
		Screenshots = new List<string>();
		Variables = new VariableList();
		Address = "";
		ResponseCode = "0";
		ResponseSource = "";
		ResponseStream = null;
		Cookies = new Dictionary<string, string>();
		ResponseHeaders = new Dictionary<string, string>();
		try
		{
			foreach (CVar variable in Data.GetVariables(ConfigSettings.EncodeData))
			{
				Variables.Set(variable);
			}
		}
		catch
		{
		}
		GlobalVariables = new VariableList();
		GlobalCookies = new Dictionary<string, string>();
		LogBuffer = new List<LogEntry>();
		Driver = null;
		BrowserOpen = false;
		IsDebug = isDebug;
		BotNumber = botNumber;
		CustomObjects = new Dictionary<string, object>();
	}

	public void Log(LogEntry entry)
	{
		if (GlobalSettings.General.EnableBotLog || IsDebug)
		{
			LogBuffer.Add(entry);
		}
	}

	public void Log(string message)
	{
		Log(new LogEntry(message, Colors.White));
	}

	public void LogRange(List<LogEntry> list)
	{
		if (GlobalSettings.General.EnableBotLog || IsDebug)
		{
			LogBuffer.AddRange(list);
		}
	}

	public void LogNewLine()
	{
		LogBuffer.Add(new LogEntry("", Colors.White));
	}

	public Action OnFlush;
	public void Flush() => OnFlush?.Invoke();

	public object GetCustomObject(string key)
	{
		if (!CustomObjects.ContainsKey(key))
		{
			return null;
		}
		return CustomObjects[key];
	}
}
