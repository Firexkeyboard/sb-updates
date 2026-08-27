using System;
using System.Windows.Media;
using RuriLib.ViewModels;

namespace RuriLib;

public class LogEntry : ViewModelBase
{
	private string logString = "";

	private Color logColor = Colors.White;

	private DateTime logTime = DateTime.Now;

	private LogLevel logLevel;

	private string logComponent = "";

	public string LogString
	{
		get
		{
			return logString;
		}
		set
		{
			logString = value;
			OnPropertyChanged("LogString");
		}
	}

	public Color LogColor
	{
		get
		{
			return logColor;
		}
		set
		{
			logColor = value;
			OnPropertyChanged("LogColor");
		}
	}

	public DateTime LogTime
	{
		get
		{
			return logTime;
		}
		set
		{
			logTime = value;
			OnPropertyChanged("LogTime");
		}
	}

	public LogLevel LogLevel
	{
		get
		{
			return logLevel;
		}
		set
		{
			logLevel = value;
			OnPropertyChanged("LogLevel");
		}
	}

	public string LogComponent
	{
		get
		{
			return logComponent;
		}
		set
		{
			logComponent = value;
			OnPropertyChanged("LogComponent");
		}
	}

	public LogEntry(string logString, Color logColor)
	{
		LogString = logString;
		LogColor = logColor;
		LogTime = DateTime.Now;
	}

	public LogEntry(string logComponent, string logString, LogLevel logLevel)
	{
		LogString = logString;
		LogTime = DateTime.Now;
		LogLevel = logLevel;
		LogComponent = logComponent;
		switch (LogLevel)
		{
		case LogLevel.Info:
			LogColor = Colors.White;
			break;
		case LogLevel.Warning:
			LogColor = Colors.Gold;
			break;
		case LogLevel.Error:
			LogColor = Colors.Tomato;
			break;
		}
	}
}
