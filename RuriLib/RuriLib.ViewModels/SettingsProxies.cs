using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using RuriLib.Models;
using ExProxyType = RuriLib.Models.ProxyType;

namespace RuriLib.ViewModels;

public class SettingsProxies : ViewModelBase
{
	private bool concurrentUse;

	private bool neverBan;

	private int banLoopEvasion = 100;

	private bool shuffleOnStart;

	private bool reload = true;

	private ProxyReloadSource reloadSource;

	private string reloadPath = "";

	private ExProxyType reloadType;

	private int reloadInterval;

	private bool alwaysGetClearance;

	private string[] globalBanKeys = new string[0];

	private string[] globalRetryKeys = new string[0];

	public bool ConcurrentUse
	{
		get
		{
			return concurrentUse;
		}
		set
		{
			concurrentUse = value;
			OnPropertyChanged("ConcurrentUse");
		}
	}

	public bool NeverBan
	{
		get
		{
			return neverBan;
		}
		set
		{
			neverBan = value;
			OnPropertyChanged("NeverBan");
		}
	}

	public int BanLoopEvasion
	{
		get
		{
			return banLoopEvasion;
		}
		set
		{
			banLoopEvasion = value;
			OnPropertyChanged("BanLoopEvasion");
		}
	}

	public bool ShuffleOnStart
	{
		get
		{
			return shuffleOnStart;
		}
		set
		{
			shuffleOnStart = value;
			OnPropertyChanged("ShuffleOnStart");
		}
	}

	public bool Reload
	{
		get
		{
			return reload;
		}
		set
		{
			reload = value;
			OnPropertyChanged("Reload");
		}
	}

	public ProxyReloadSource ReloadSource
	{
		get
		{
			return reloadSource;
		}
		set
		{
			reloadSource = value;
			OnPropertyChanged("ReloadSource");
		}
	}

	public string ReloadPath
	{
		get
		{
			return reloadPath;
		}
		set
		{
			reloadPath = value;
			OnPropertyChanged("ReloadPath");
		}
	}

	public ExProxyType ReloadType
	{
		get
		{
			return reloadType;
		}
		set
		{
			reloadType = value;
			OnPropertyChanged("ReloadType");
		}
	}

	public int ReloadInterval
	{
		get
		{
			return reloadInterval;
		}
		set
		{
			reloadInterval = value;
			OnPropertyChanged("ReloadInterval");
		}
	}

	public bool AlwaysGetClearance
	{
		get
		{
			return alwaysGetClearance;
		}
		set
		{
			alwaysGetClearance = value;
			OnPropertyChanged("AlwaysGetClearance");
		}
	}

	public string[] GlobalBanKeys
	{
		get
		{
			return globalBanKeys;
		}
		set
		{
			globalBanKeys = value;
			OnPropertyChanged("GlobalBanKeys");
		}
	}

	public string[] GlobalRetryKeys
	{
		get
		{
			return globalRetryKeys;
		}
		set
		{
			globalRetryKeys = value;
			OnPropertyChanged("GlobalRetryKeys");
		}
	}

	public ObservableCollection<RemoteProxySource> RemoteProxySources { get; set; } = new ObservableCollection<RemoteProxySource>();

	public void RemoveRemoteProxySourceById(int id)
	{
		RemoteProxySources.Remove(GetRemoteProxySourceById(id));
	}

	public RemoteProxySource GetRemoteProxySourceById(int id)
	{
		return RemoteProxySources.FirstOrDefault((RemoteProxySource s) => s.Id == id);
	}

	public void Reset()
	{
		SettingsProxies obj = new SettingsProxies();
		foreach (PropertyInfo item in (IEnumerable<PropertyInfo>)new List<PropertyInfo>(typeof(SettingsProxies).GetProperties()))
		{
			item.SetValue(this, item.GetValue(obj, null));
		}
	}
}


