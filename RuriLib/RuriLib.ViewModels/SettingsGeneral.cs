using System.Collections.Generic;
using System.Reflection;

namespace RuriLib.ViewModels;

public class SettingsGeneral : ViewModelBase
{
	private int _waitTime = 0;

	private int _requestsTimeout = 10;

	private int _maxHits;

	private BotsDisplayMode _botsDisplayMode = BotsDisplayMode.Everything;

	private bool _enableBotLog;

	private bool saveLastSource;

	private bool sendToCheckOnAbort;

	private bool webhookEnabled;

	private string webhookURL = "";

	private string webhookUser = "Undefined";

	private bool removeDuplicatewordlist;

	private bool disableBotsListView;

	public int WaitTime
	{
		get
		{
			return _waitTime;
		}
		set
		{
			_waitTime = value;
			OnPropertyChanged("WaitTime");
		}
	}

	public int RequestTimeout
	{
		get
		{
			return _requestsTimeout;
		}
		set
		{
			_requestsTimeout = value;
			OnPropertyChanged("RequestTimeout");
		}
	}

	public int MaxHits
	{
		get
		{
			return _maxHits;
		}
		set
		{
			_maxHits = value;
			OnPropertyChanged("MaxHits");
		}
	}

	public BotsDisplayMode BotsDisplayMode
	{
		get
		{
			return _botsDisplayMode;
		}
		set
		{
			_botsDisplayMode = value;
			OnPropertyChanged("BotsDisplayMode");
		}
	}

	public bool EnableBotLog
	{
		get
		{
			return _enableBotLog;
		}
		set
		{
			_enableBotLog = value;
			OnPropertyChanged("EnableBotLog");
		}
	}

	public bool SaveLastSource
	{
		get
		{
			return saveLastSource;
		}
		set
		{
			saveLastSource = value;
			OnPropertyChanged("SaveLastSource");
		}
	}

	public bool SendToCheckOnAbort
	{
		get
		{
			return sendToCheckOnAbort;
		}
		set
		{
			sendToCheckOnAbort = value;
			OnPropertyChanged("SendToCheckOnAbort");
		}
	}

	public bool WebhookEnabled
	{
		get
		{
			return webhookEnabled;
		}
		set
		{
			webhookEnabled = value;
			OnPropertyChanged("WebhookEnabled");
		}
	}

	public string WebhookURL
	{
		get
		{
			return webhookURL;
		}
		set
		{
			webhookURL = value;
			OnPropertyChanged("WebhookURL");
		}
	}

	public string WebhookUser
	{
		get
		{
			return webhookUser;
		}
		set
		{
			webhookUser = value;
			OnPropertyChanged("WebhookUser");
		}
	}

	public bool RemoveDuplicateWordlist
	{
		get
		{
			return removeDuplicatewordlist;
		}
		set
		{
			removeDuplicatewordlist = value;
			OnPropertyChanged("RemoveDuplicateWordlist");
		}
	}

	public bool DisableBotsListView
	{
		get
		{
			return disableBotsListView;
		}
		set
		{
			disableBotsListView = value;
			OnPropertyChanged("DisableBotsListView");
		}
	}

	public void Reset()
	{
		SettingsGeneral obj = new SettingsGeneral();
		foreach (PropertyInfo item in (IEnumerable<PropertyInfo>)new List<PropertyInfo>(typeof(SettingsGeneral).GetProperties()))
		{
			item.SetValue(this, item.GetValue(obj, null));
		}
	}
}
