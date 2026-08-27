using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows.Data;
using OpenBullet.Models;
using OpenBullet.Repositories;
using PluginFramework;
using RuriLib;
using RuriLib.Functions.Formats;
using RuriLib.Interfaces;
using RuriLib.LS;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class ConfigManagerViewModel : ViewModelBase, IConfigManager
{
	private ConfigRepository _diskRepos;

	private ObservableCollection<ConfigViewModel> configsCollection;

	private int _savedHash;

	private Config hoveredConfig;

	private ConfigViewModel currentConfig;

	private string searchString = "";

	public ObservableCollection<ConfigViewModel> ConfigsCollection
	{
		get
		{
			return configsCollection;
		}
		private set
		{
			if (!object.Equals(configsCollection, value))
			{
				configsCollection = value;
				OnPropertyChanged("Configs");
				OnPropertyChanged("Total");
				OnPropertyChanged("ConfigsCollection");
			}
		}
	}

	public IEnumerable<ConfigViewModel> Configs => ConfigsCollection;

	public int Total => ConfigsCollection.Count;

	public int SavedHash
	{
		get
		{
			return _savedHash;
		}
		set
		{
			if (_savedHash != value)
			{
				_savedHash = value;
				OnPropertyChanged("SavedHash");
			}
		}
	}

	public Config HoveredConfig
	{
		get
		{
			return hoveredConfig;
		}
		set
		{
			if (!object.Equals(hoveredConfig, value))
			{
				hoveredConfig = value;
				OnPropertyChanged("HoveredConfig");
			}
		}
	}

	public ConfigViewModel CurrentConfig
	{
		get
		{
			return currentConfig;
		}
		set
		{
			if (!object.Equals(currentConfig, value))
			{
				currentConfig = value;
				OnPropertyChanged("CurrentConfig");
				OnPropertyChanged("CurrentConfigName");
			}
		}
	}

	public string CurrentConfigName
	{
		get
		{
			if (CurrentConfig != null)
			{
				return CurrentConfig.Name;
			}
			return "None";
		}
	}

	public string SearchString
	{
		get
		{
			return searchString;
		}
		set
		{
			if (!string.Equals(searchString, value, StringComparison.Ordinal))
			{
				searchString = value;
				OnPropertyChanged("SearchString");
				CollectionViewSource.GetDefaultView(ConfigsCollection).Refresh();
				OnPropertyChanged("Total");
			}
		}
	}

	public ConfigManagerViewModel()
	{
		_diskRepos = new ConfigRepository(SB.configFolder);
		Rescan();
	}

	public IEnumerable<string> GetRequiredPlugins(ConfigViewModel config)
	{
		return (from IBlockPlugin p in new LoliScript(config.Config.Script).ToBlocks().OnlyPlugins()
			select p.Name).Distinct();
	}

	public void HookFilters()
	{
		((CollectionView)CollectionViewSource.GetDefaultView(ConfigsCollection)).Filter = ConfigsFilter;
	}

	private bool ConfigsFilter(object item)
	{
		return ((ConfigViewModel)((item is ConfigViewModel) ? item : null)).Name.ToLower().Contains(searchString.ToLower());
	}

	public List<ConfigViewModel> GetConfigsFromDisk(bool sort = false, bool reverse = false)
	{
		List<ConfigViewModel> list = _diskRepos.Get().ToList();
		if (sort)
		{
			list.Sort((ConfigViewModel m1, ConfigViewModel m2) => m1.Config.Settings.LastModified.CompareTo(m2.Config.Settings.LastModified));
			if (reverse)
			{
				list.Reverse();
			}
		}
		return list;
	}

	public IEnumerable<ConfigViewModel> GetConfigsFromSources()
	{
		List<ConfigViewModel> list = new List<ConfigViewModel>();
		foreach (Source source in SB.SBSettings.Sources.Sources)
		{
			try
			{
				list.AddRange(PullSource(source));
			}
			catch (Exception ex)
			{
				SB.Logger.LogError(Components.ConfigManager, "Error with API " + source.ApiUrl + "\r\nReason: " + ex.Message, prompt: true);
			}
		}
		return list;
	}

	public IEnumerable<ConfigViewModel> PullSource(Source source)
	{
		using var http = new HttpClient();
		List<ConfigViewModel> list = new List<ConfigViewModel>();
		switch (source.Auth)
		{
		case Source.AuthMode.ApiKey:
			http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", source.ApiKey);
			break;
		case Source.AuthMode.UserPass:
		{
			string text = Encode.ToBase64(source.Username + ":" + source.Password);
			http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Basic " + text);
			break;
		}
		}
		var response = http.GetAsync(source.ApiUrl).GetAwaiter().GetResult();
		byte[] array = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
		string text2 = response.Headers.TryGetValues("Result", out var resultVals) ? string.Join(",", resultVals) : null;
		if (text2 != null && text2 == "Error")
		{
			throw new Exception("The server says: " + Encoding.ASCII.GetString(array));
		}
		try
		{
			ZipArchive val = new ZipArchive((Stream)new MemoryStream(array), (ZipArchiveMode)0);
			try
			{
				foreach (ZipArchiveEntry entry in val.Entries)
				{
					string text3 = Path.GetDirectoryName(entry.FullName).Replace("\\", " - ");
					string text4 = ((text3 == string.Empty) ? "Remote" : ("Remote - " + text3));
					using Stream stream = entry.Open();
					using TextReader textReader = new StreamReader(stream);
					Config val2 = IOManager.DeserializeConfig(textReader.ReadToEnd());
					list.Add(new ConfigViewModel(string.Empty, "", text4, val2, true));
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch
		{
		}
		return list;
	}

	public void Add(ConfigViewModel config)
	{
		_diskRepos.Add(config);
		ConfigsCollection.Add(config);
	}

	public void Rescan()
	{
		ConfigsCollection = new ObservableCollection<ConfigViewModel>(GetConfigsFromDisk(sort: true, reverse: true).Concat(GetConfigsFromSources()));
		HookFilters();
		OnPropertyChanged("Total");
	}

	public void Update(ConfigViewModel config)
	{
		_diskRepos.Update(config);
	}

	public void SaveCurrent()
	{
		Update(CurrentConfig);
	}

	public void Remove(ConfigViewModel config)
	{
		_diskRepos.Remove(config);
		ConfigsCollection.Remove(config);
	}

	public void Remove(IEnumerable<ConfigViewModel> configs)
	{
		ConfigViewModel[] array = configs.ToArray();
		ConfigViewModel[] array2 = array;
		foreach (ConfigViewModel item in array2)
		{
			ConfigsCollection.Remove(item);
		}
		_diskRepos.Remove((IEnumerable<ConfigViewModel>)array);
	}
}
