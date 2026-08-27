using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using LiteDB;
using OpenBullet.Repositories;
using RuriLib;
using RuriLib.Interfaces;
using RuriLib.Models;
using RuriLib.Runner;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class RunnerManagerViewModel : ViewModelBase, IRunnerManager
{
	private LiteDBRepository<RunnerSessionData> _repo;

	private ObservableCollection<RunnerInstance> _runnersCollection = new ObservableCollection<RunnerInstance>();

	private Random rand = new Random();

	public ObservableCollection<RunnerInstance> RunnersCollection
	{
		get
		{
			return _runnersCollection;
		}
		set
		{
			if (!object.Equals(_runnersCollection, value))
			{
				_runnersCollection = value;
				OnPropertyChanged("Runners");
				OnPropertyChanged("RunnersCollection");
			}
		}
	}

	public IEnumerable<IRunner> Runners => (IEnumerable<IRunner>)RunnersCollection.Select((RunnerInstance i) => i.ViewModel);

	public RunnerManagerViewModel()
	{
		_repo = new LiteDBRepository<RunnerSessionData>(SB.dataBaseFile, "runners");
	}

	public RunnerInstance Get(int id)
	{
		return RunnersCollection.Where((RunnerInstance r) => r.Id == id).First();
	}

	public IRunner Create()
	{
		RunnerInstance runnerInstance = new RunnerInstance(rand.Next());
		runnerInstance.ViewModel.ConfigChanged += OnRunnerSessionChanged;
		runnerInstance.ViewModel.WordlistChanged += OnRunnerSessionChanged;
		RunnersCollection.Add(runnerInstance);
		return (IRunner)(object)runnerInstance.ViewModel;
	}

	public void Remove(IRunner runner)
	{
		RunnersCollection.Remove(RunnersCollection.First((RunnerInstance r) => r.ViewModel == runner));
	}

	public void Remove(int id)
	{
		RunnersCollection.Remove(Get(id));
	}

	public void RemoveAll()
	{
		RunnersCollection.Clear();
	}

	public void OnRunnerSessionChanged(IRunnerMessaging obj)
	{
		SaveSession();
	}

	public void SaveSession()
	{
		_repo.RemoveAll();
		_repo.Add(RunnersCollection.Select((RunnerInstance r) => r.ViewModel).Select((Func<RunnerViewModel, RunnerSessionData>)((RunnerViewModel r) => new RunnerSessionData
		{
			Bots = r.BotsAmount,
			Config = ((r.Config != null) ? r.ConfigName : ""),
			Wordlist = ((r.Wordlist != null) ? r.Wordlist.Path : ""),
			ProxyMode = r.ProxyMode
		})));
	}

	public bool RestoreSession()
	{
		RunnerSessionData[] array = _repo.Get().ToArray();
		if (array.Length == 0)
		{
			return false;
		}
		RunnerSessionData[] array2 = array;
		foreach (RunnerSessionData r in array2)
		{
			try
			{
				IRunner obj = Create();
				obj.BotsAmount = r.Bots;
				obj.ProxyMode = r.ProxyMode;
				ConfigViewModel val = SB.ConfigManager.Configs.FirstOrDefault((ConfigViewModel c) => c.Name == r.Config) ?? throw new Exception("The Config " + r.Config + " was not found in the ConfigManager");
				obj.SetConfig(val.Config, false);
				Wordlist val2 = SB.WordlistManager.Wordlists.FirstOrDefault((Wordlist w) => w.Path == r.Wordlist);
				if (val2 == null)
				{
					if (!File.Exists(r.Wordlist))
					{
						throw new Exception("The Wordlist " + r.Wordlist + " was not found in the WordlistManager or on Disk");
					}
					val2 = WordlistManagerViewModel.FileToWordlist(r.Wordlist);
				}
				obj.SetWordlist(val2);
				obj.StartingPoint = RetrieveRecord(val.Config, val2);
			}
			catch (Exception ex)
			{
				SB.Logger.LogError(Components.RunnerManager, ex.Message);
			}
		}
		return true;
	}

	public int RetrieveRecord(Config config, Wordlist wordlist)
	{
		if (wordlist == null || config == null)
		{
			return 1;
		}
		LiteDatabase val = new LiteDatabase(SB.dataBaseFile, (BsonMapper)null);
		try
		{
			Record val2 = val.GetCollection<Record>("records", (BsonAutoId)10).FindOne((Expression<Func<Record, bool>>)((Record r) => r.ConfigName == config.Settings.Name && r.WordlistLocation == wordlist.Path));
			if (val2 != null)
			{
				return val2.Checkpoint;
			}
			return 1;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void SaveRecord(Config config, Wordlist wordlist, int progress)
	{
		if (config == null || wordlist == null)
		{
			return;
		}
		LiteDatabase val = new LiteDatabase("filename=" + SB.dataBaseFile + "; Connection=Shared;", (BsonMapper)null);
		try
		{
			ILiteCollection<Record> collection = val.GetCollection<Record>("records", (BsonAutoId)10);
			Record val2 = new Record(config.Settings.Name, wordlist.Path, progress);
			collection.DeleteMany((Expression<Func<Record, bool>>)((Record r) => r.ConfigName == config.Settings.Name && r.WordlistLocation == wordlist.Path));
			collection.Insert(val2);
			collection = null;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}
}
