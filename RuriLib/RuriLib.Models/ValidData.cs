using System;
using System.Collections.Generic;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace RuriLib.Models;

public class ValidData : ViewModelBase
{
	private string data;

	private string proxy;

	private float ocrRate;

	private ProxyType proxyType;

	private BotStatus result;

	private string type;

	private string capturedData;

	private int unixDate;

	private string source;

	private List<LogEntry> log;

	public string Data
	{
		get
		{
			return data;
		}
		set
		{
			data = value;
			OnPropertyChanged("Data");
		}
	}

	public string Proxy
	{
		get
		{
			return proxy;
		}
		set
		{
			proxy = value;
			OnPropertyChanged("Proxy");
		}
	}

	public float OcrRate
	{
		get
		{
			return ocrRate;
		}
		set
		{
			ocrRate = value;
			OnPropertyChanged("OcrRate");
		}
	}

	public ProxyType ProxyType
	{
		get
		{
			return proxyType;
		}
		set
		{
			proxyType = value;
			OnPropertyChanged("ProxyType");
		}
	}

	public BotStatus Result
	{
		get
		{
			return result;
		}
		set
		{
			result = value;
			OnPropertyChanged("Result");
		}
	}

	public string Type
	{
		get
		{
			return type;
		}
		set
		{
			type = value;
			OnPropertyChanged("Type");
		}
	}

	public string CapturedData
	{
		get
		{
			return capturedData;
		}
		set
		{
			capturedData = value;
			OnPropertyChanged("CapturedData");
		}
	}

	public int UnixDate
	{
		get
		{
			return unixDate;
		}
		set
		{
			unixDate = value;
			OnPropertyChanged("UnixDate");
			OnPropertyChanged("UnixDate");
		}
	}

	public string Timestamp => new DateTime(1970, 1, 1).AddSeconds(UnixDate).ToShortDateString();

	public DateTime Time => new DateTime(1970, 1, 1).AddSeconds(UnixDate);

	public string Source
	{
		get
		{
			return source;
		}
		set
		{
			source = value;
			OnPropertyChanged("Source");
		}
	}

	public List<LogEntry> Log
	{
		get
		{
			return log;
		}
		set
		{
			log = value;
			OnPropertyChanged("Log");
		}
	}

	public ValidData(string data, float ocrRate, string proxy, ProxyType proxyType, BotStatus result, string type, string capturedData, string source, List<LogEntry> log)
	{
		Data = data;
		Proxy = proxy;
		Result = result;
		Type = type;
		CapturedData = capturedData;
		UnixDate = (int)Math.Round((DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
		Source = source;
		Log = log;
		OcrRate = ocrRate;
	}
}
