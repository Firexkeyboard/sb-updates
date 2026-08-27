using System;
using System.Collections.Generic;
using System.Reflection;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class SBSettingsGeneral : ViewModelBase
{
	private bool displayLoliScriptOnLoad;

	private bool recommendedBots = true;

	private int startingWidth = 800;

	private int startingHeight = 620;

	private bool changeRunnerInterface;

	private bool disableQuitWarning;

	private bool disableNotSavedWarning;

	private string defaultAuthor = "";

	private bool liveConfigUpdates;

	private bool disableHTMLView;

	private bool alwaysOnTop;

	private bool autoCreateRunner;

	private bool persistDebuggerLog;

	private bool disableDebuggerLog;

	private bool sendDebuggerLogToNotepadPlus;

	private bool disableSyntaxHelper;

	private bool displayCapturesLast;

	private bool disableCopyPasteBlocks;

	private bool enableLogging;

	private bool logToFile;

	private int logBufferSize = 10000;

	private bool backupDB = true;

	private bool ignoreWordlistOnHitsDedupe;

	private int autoSaveConfigTime = 1;

	private bool scriptCompletion = true;

	private bool autoSaveConfigOnStacker;

	private bool periodicAutoSaveEnabled;

	private bool localHTMLViewer;

	private bool enableCookiesInBrowser;

	public bool DisplayLoliScriptOnLoad
	{
		get
		{
			return displayLoliScriptOnLoad;
		}
		set
		{
			if (displayLoliScriptOnLoad != value)
			{
				displayLoliScriptOnLoad = value;
				OnPropertyChanged("DisplayLoliScriptOnLoad");
			}
		}
	}

	public bool RecommendedBots
	{
		get
		{
			return recommendedBots;
		}
		set
		{
			if (recommendedBots != value)
			{
				recommendedBots = value;
				OnPropertyChanged("RecommendedBots");
			}
		}
	}

	public int StartingWidth
	{
		get
		{
			return startingWidth;
		}
		set
		{
			if (startingWidth != value)
			{
				startingWidth = value;
				OnPropertyChanged("StartingWidth");
			}
		}
	}

	public int StartingHeight
	{
		get
		{
			return startingHeight;
		}
		set
		{
			if (startingHeight != value)
			{
				startingHeight = value;
				OnPropertyChanged("StartingHeight");
			}
		}
	}

	public bool ChangeRunnerInterface
	{
		get
		{
			return changeRunnerInterface;
		}
		set
		{
			if (changeRunnerInterface != value)
			{
				changeRunnerInterface = value;
				OnPropertyChanged("ChangeRunnerInterface");
			}
		}
	}

	public bool DisableQuitWarning
	{
		get
		{
			return disableQuitWarning;
		}
		set
		{
			if (disableQuitWarning != value)
			{
				disableQuitWarning = value;
				OnPropertyChanged("DisableQuitWarning");
			}
		}
	}

	public bool DisableNotSavedWarning
	{
		get
		{
			return disableNotSavedWarning;
		}
		set
		{
			if (disableNotSavedWarning != value)
			{
				disableNotSavedWarning = value;
				OnPropertyChanged("DisableNotSavedWarning");
			}
		}
	}

	public string DefaultAuthor
	{
		get
		{
			return defaultAuthor;
		}
		set
		{
			if (!string.Equals(defaultAuthor, value, StringComparison.Ordinal))
			{
				defaultAuthor = value;
				OnPropertyChanged("DefaultAuthor");
			}
		}
	}

	public bool LiveConfigUpdates
	{
		get
		{
			return liveConfigUpdates;
		}
		set
		{
			if (liveConfigUpdates != value)
			{
				liveConfigUpdates = value;
				OnPropertyChanged("LiveConfigUpdates");
			}
		}
	}

	public bool DisableHTMLView
	{
		get
		{
			return disableHTMLView;
		}
		set
		{
			if (disableHTMLView != value)
			{
				disableHTMLView = value;
				OnPropertyChanged("DisableHTMLView");
			}
		}
	}

	public bool AlwaysOnTop
	{
		get
		{
			return alwaysOnTop;
		}
		set
		{
			if (alwaysOnTop != value)
			{
				alwaysOnTop = value;
				OnPropertyChanged("AlwaysOnTop");
			}
		}
	}

	public bool AutoCreateRunner
	{
		get
		{
			return autoCreateRunner;
		}
		set
		{
			if (autoCreateRunner != value)
			{
				autoCreateRunner = value;
				OnPropertyChanged("AutoCreateRunner");
			}
		}
	}

	public bool PersistDebuggerLog
	{
		get
		{
			return persistDebuggerLog;
		}
		set
		{
			if (persistDebuggerLog != value)
			{
				persistDebuggerLog = value;
				OnPropertyChanged("PersistDebuggerLog");
			}
		}
	}

	public bool DisableDebuggerLog
	{
		get
		{
			return disableDebuggerLog;
		}
		set
		{
			if (disableDebuggerLog != value)
			{
				disableDebuggerLog = value;
				OnPropertyChanged("DisableDebuggerLog");
			}
		}
	}

	public bool SendDebuggerLogToNotepadPlus
	{
		get
		{
			return sendDebuggerLogToNotepadPlus;
		}
		set
		{
			if (sendDebuggerLogToNotepadPlus != value)
			{
				sendDebuggerLogToNotepadPlus = value;
				OnPropertyChanged("SendDebuggerLogToNotepadPlus");
			}
		}
	}

	public bool DisableSyntaxHelper
	{
		get
		{
			return disableSyntaxHelper;
		}
		set
		{
			if (disableSyntaxHelper != value)
			{
				disableSyntaxHelper = value;
				OnPropertyChanged("DisableSyntaxHelper");
			}
		}
	}

	public bool DisplayCapturesLast
	{
		get
		{
			return displayCapturesLast;
		}
		set
		{
			if (displayCapturesLast != value)
			{
				displayCapturesLast = value;
				OnPropertyChanged("DisplayCapturesLast");
			}
		}
	}

	public bool DisableCopyPasteBlocks
	{
		get
		{
			return disableCopyPasteBlocks;
		}
		set
		{
			if (disableCopyPasteBlocks != value)
			{
				disableCopyPasteBlocks = value;
				OnPropertyChanged("DisableCopyPasteBlocks");
			}
		}
	}

	public bool EnableLogging
	{
		get
		{
			return enableLogging;
		}
		set
		{
			if (enableLogging != value)
			{
				enableLogging = value;
				OnPropertyChanged("EnableLogging");
			}
		}
	}

	public bool LogToFile
	{
		get
		{
			return logToFile;
		}
		set
		{
			if (logToFile != value)
			{
				logToFile = value;
				OnPropertyChanged("LogToFile");
			}
		}
	}

	public int LogBufferSize
	{
		get
		{
			return logBufferSize;
		}
		set
		{
			if (logBufferSize != value)
			{
				logBufferSize = value;
				OnPropertyChanged("LogBufferSize");
			}
		}
	}

	public bool BackupDB
	{
		get
		{
			return backupDB;
		}
		set
		{
			if (backupDB != value)
			{
				backupDB = value;
				OnPropertyChanged("BackupDB");
			}
		}
	}

	public bool IgnoreWordlistOnHitDedupe
	{
		get
		{
			return ignoreWordlistOnHitsDedupe;
		}
		set
		{
			if (ignoreWordlistOnHitsDedupe != value)
			{
				ignoreWordlistOnHitsDedupe = value;
				OnPropertyChanged("IgnoreWordlistOnHitDedupe");
			}
		}
	}

	public int AutoSaveConfigTime
	{
		get
		{
			return autoSaveConfigTime;
		}
		set
		{
			if (autoSaveConfigTime != value)
			{
				autoSaveConfigTime = value;
				OnPropertyChanged("AutoSaveConfigTime");
			}
		}
	}

	public bool ScriptCompletion
	{
		get
		{
			return scriptCompletion;
		}
		set
		{
			if (scriptCompletion != value)
			{
				scriptCompletion = value;
				OnPropertyChanged("ScriptCompletion");
			}
		}
	}

	public bool AutoSaveConfigOnStacker
	{
		get
		{
			return autoSaveConfigOnStacker;
		}
		set
		{
			if (autoSaveConfigOnStacker != value)
			{
				autoSaveConfigOnStacker = value;
				OnPropertyChanged("AutoSaveConfigOnStacker");
			}
		}
	}

	public bool PeriodicAutoSaveEnabled
	{
		get
		{
			return periodicAutoSaveEnabled;
		}
		set
		{
			if (periodicAutoSaveEnabled != value)
			{
				periodicAutoSaveEnabled = value;
				OnPropertyChanged("PeriodicAutoSaveEnabled");
			}
		}
	}

	public bool LocalHTMLViewer
	{
		get
		{
			return localHTMLViewer;
		}
		set
		{
			if (localHTMLViewer != value)
			{
				localHTMLViewer = value;
				OnPropertyChanged("LocalHTMLViewer");
			}
		}
	}

	public bool EnableCookiesInBrowser
	{
		get
		{
			return enableCookiesInBrowser;
		}
		set
		{
			if (enableCookiesInBrowser != value)
			{
				enableCookiesInBrowser = value;
				OnPropertyChanged("EnableCookiesInBrowser");
			}
		}
	}

	public void Reset()
	{
		SBSettingsGeneral obj = new SBSettingsGeneral();
		foreach (PropertyInfo item in (IEnumerable<PropertyInfo>)new List<PropertyInfo>(typeof(SBSettingsGeneral).GetProperties()))
		{
			item.SetValue(this, item.GetValue(obj, null));
		}
	}
}
