using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Net.Http;
using Newtonsoft.Json;
using RuriLib.Interfaces;
using RuriLib.LS;
using RuriLib.LS.LoliCode;
using RuriLib.Models;
using ExProxyType = RuriLib.Models.ProxyType;
using RuriLib.Models.Stats;
using RuriLib.SB.JS;
using RuriLib.ViewModels;
using SilverBullet.Tesseract;

namespace RuriLib.Runner;

public class RunnerViewModel : ViewModelBase, IRunnerMessaging, IRunner
{
	private static readonly HttpClient _webhookClient = new HttpClient();

	private Random random;

	private object randomLocker = new object();

	private int botsAmount = 1;

	private int startingPoint = 1;

	private int cpm;

	private decimal _balance;

	private string usage = "0/0";

	private ProxyMode proxyMode;

	public OcrEngine ocrEngine;

	public JsEngine jsEngine;

	private float ocrRate;

	private int retryCount;

	private Timer usageTimer;

	private RLSettingsViewModel Settings { get; set; }

	private EnvironmentSettings Env { get; set; }

	public AbortableBackgroundWorker Master { get; set; } = new AbortableBackgroundWorker();

	public WorkerStatus WorkerStatus => Master.Status;

	public ObservableCollection<RunnerBotViewModel> Bots { get; } = new ObservableCollection<RunnerBotViewModel>();

	public bool Busy => Master.Status != WorkerStatus.Idle;

	public bool ControlsEnabled => !Busy;

	public int BotsAmount
	{
		get
		{
			return botsAmount;
		}
		set
		{
			botsAmount = value;
			OnPropertyChanged("BotsAmount");
		}
	}

	public int StartingPoint
	{
		get
		{
			return startingPoint;
		}
		set
		{
			startingPoint = value;
			OnPropertyChanged("StartingPoint");
			OnPropertyChanged("Progress");
		}
	}

	public int ProgressCount => TestedCount + StartingPoint - 1;

	public int Progress
	{
		get
		{
			int a = 0;
			try
			{
				double num = TestedCount + StartingPoint - 1;
				double num2 = DataPool.Size;
				a = (int)(num / num2 * 100.0);
			}
			catch
			{
			}
			return Clamp(a, 0, 100);
		}
	}

	public int CPM
	{
		get
		{
			if (TestedCount == 0)
			{
				cpm = 0;
				return cpm;
			}
			// Devolver valor cacheado si fue calculado hace menos de 1 segundo
			if (!IsCPMLocked && (DateTime.Now - _lastCpmCalc).TotalSeconds < 1.0)
			{
				return cpm;
			}
			if (IsCPMLocked)
			{
				return cpm;
			}
			try
			{
				int num = 0;
				IsCPMLocked = true;
				DateTime now = DateTime.Now;

				// FailedList requiere lock porque mÃºltiples bots escriben concurrentemente
				lock (_failedListLock)
				{
					int num2 = FailedList.Count - 1;
					while (num2 >= 0 && !((now - FailedList[num2].Time).TotalSeconds > 60.0))
					{
						num++;
						num2--;
					}
				}
				int num3 = HitsList.Count - 1;
				while (num3 >= 0 && !((now - HitsList[num3].Time).TotalSeconds > 60.0))
				{
					num++;
					num3--;
				}
				int num4 = CustomList.Count - 1;
				while (num4 >= 0 && !((now - CustomList[num4].Time).TotalSeconds > 60.0))
				{
					num++;
					num4--;
				}
				int num5 = ToCheckList.Count - 1;
				while (num5 >= 0 && !((now - ToCheckList[num5].Time).TotalSeconds > 60.0))
				{
					num++;
					num5--;
				}
				cpm = num;
				_lastCpmCalc = now;
				IsCPMLocked = false;
			}
			catch
			{
			}
			finally
			{
				IsCPMLocked = false;
			}
			return cpm;
		}
	}

	public decimal Balance
	{
		get
		{
			return _balance;
		}
		set
		{
			_balance = value;
			OnPropertyChanged("BalanceString");
		}
	}

	public string BalanceString => $"${_balance}";

	public string Usage
	{
		get
		{
			return usage;
		}
		set
		{
			usage = value;
			OnPropertyChanged("Usage");
		}
	}

	public ProxyMode ProxyMode
	{
		get
		{
			return proxyMode;
		}
		set
		{
			proxyMode = value;
			OnPropertyChanged("ProxyMode");
		}
	}

	public bool UseProxies
	{
		get
		{
			if (Config != null)
			{
				if (!Config.Settings.NeedsProxies || ProxyMode != 0)
				{
					return ProxyMode == ProxyMode.On;
				}
				return true;
			}
			return true;
		}
	}

	public Config Config { get; private set; }

	public string ConfigName
	{
		get
		{
			if (Config != null)
			{
				return Config.Settings.Name;
			}
			return "None";
		}
	}

	public Wordlist Wordlist { get; private set; }

	public string WordlistName
	{
		get
		{
			if (Wordlist != null)
			{
				return Wordlist.Name;
			}
			return "None";
		}
	}

	public long WordlistSize
	{
		get
		{
			if (Wordlist != null)
			{
				return Wordlist.Total;
			}
			return 0L;
		}
	}

	public ProxyPool ProxyPool { get; set; } = new ProxyPool(new List<CProxy>());

	public int TotalProxiesCount => ProxyPool.Proxies.Count;

	public int AliveProxiesCount => ProxyPool.Alive.Count;

	public int AvailableProxiesCount => ProxyPool.Available.Count;

	public int BannedProxiesCount => ProxyPool.Banned.Count;

	public int BadProxiesCount => ProxyPool.Bad.Count;

	public DataPool DataPool { get; set; }

	public long DataSize
	{
		get
		{
			if (DataPool != null)
			{
				return DataPool.Size;
			}
			return 0L;
		}
	}

	public List<KeyValuePair<string, string>> CustomInputs { get; set; } = new List<KeyValuePair<string, string>>();

	public VariableList GlobalVariables { get; set; } = new VariableList();

	public Dictionary<string, string> GlobalCookies { get; set; } = new Dictionary<string, string>();

	public Action<BotData> CustomAction { get; set; }

	public List<ValidData> FailedList { get; set; } = new List<ValidData>();

	public ObservableCollection<ValidData> HitsList { get; set; } = new ObservableCollection<ValidData>();

	public ObservableCollection<ValidData> CustomList { get; set; } = new ObservableCollection<ValidData>();

	public ObservableCollection<ValidData> ToCheckList { get; set; } = new ObservableCollection<ValidData>();

	public ObservableCollection<ValidData> EmptyList { get; set; } = new ObservableCollection<ValidData>();

	public IEnumerable<ValidData> Checked => from h in new IEnumerable<ValidData>[3] { HitsList, CustomList, ToCheckList }.SelectMany((IEnumerable<ValidData> h) => h)
		orderby h.Time
		select h;

	public BotStatus ResultsFilter { get; set; } = BotStatus.SUCCESS;

	public int FailCount => FailedList.Count;

	public int HitCount => HitsList.Count;

	public int CustomCount => CustomList.Count;

	public int ToCheckCount => ToCheckList.Count;

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

	public int RetryCount
	{
		get
		{
			return retryCount;
		}
		set
		{
			retryCount = value;
			OnPropertyChanged("RetryCount");
		}
	}

	public int TestedCount => FailCount + HitCount + CustomCount + ToCheckCount;

	public RunnerStats Stats => new RunnerStats(new RunnerStatsData(TestedCount, HitCount, CustomCount, FailCount, RetryCount, ToCheckCount), new RunnerStatsProxies(TotalProxiesCount, AliveProxiesCount, BannedProxiesCount, BadProxiesCount), CPM, Balance);

	// Bug 3 fix: use int + Interlocked instead of property bool (eliminates TOCTOU race between bot threads)
	private int _isReloadingProxies = 0;

	private bool IsCPMLocked { get; set; }

	private bool NoProxyWarningSent { get; set; }

	// Bug 2 fix: lock for shared GlobalCookies dictionary
	private readonly object _globalCookiesLock = new object();

	// Bug 4 fix: fast thread-safe lookup of bot VMs by Id (avoids iterating ObservableCollection from bot threads)
	private readonly ConcurrentDictionary<int, RunnerBotViewModel> _botById = new ConcurrentDictionary<int, RunnerBotViewModel>();

	// Protege FailedList contra escrituras concurrentes de mÃºltiples bots
	private readonly object _failedListLock = new object();

	// Fix 6: señal que despierta el loop de despacho en cuanto un bot termina
	private readonly ManualResetEventSlim _botFreeEvent = new ManualResetEventSlim(false);

	// CachÃ© de CPM: evita recalcular las 4 listas en cada llamada
	private DateTime _lastCpmCalc = DateTime.MinValue;

	public bool CustomInputsInitialized { get; set; }

	private Stopwatch Timer { get; set; } = new Stopwatch();

	public string TimerDays => Timer.Elapsed.Days.ToString();

	public string TimerHours => Timer.Elapsed.Hours.ToString("D2");

	public string TimerMinutes => Timer.Elapsed.Minutes.ToString("D2");

	public string TimerSeconds => Timer.Elapsed.Seconds.ToString("D2");

	public string TimeLeft
	{
		get
		{
			if (Wordlist == null)
			{
				return "Unknown time left";
			}
			long num = Wordlist.Total - StartingPoint - TestedCount;
			if (CPM == 0)
			{
				return "+inf";
			}
			long num2 = num / CPM * 60;
			string arg = "seconds";
			if (num2 > 60)
			{
				num2 /= 60;
				arg = "minutes";
			}
			if (num2 > 60)
			{
				num2 /= 60;
				arg = "hours";
			}
			if (num2 > 24)
			{
				num2 /= 24;
				arg = "days";
			}
			return $"{num2} {arg} left";
		}
	}

	public event Action<IRunnerMessaging, LogLevel, string, bool, int> MessageArrived;

	public event Action<IRunnerMessaging> WorkerStatusChanged;

	public event Action<IRunnerMessaging, Hit> FoundHit;

	public event Action<IRunnerMessaging> ReloadProxies;

	public event Action<IRunnerMessaging, Action> DispatchAction;

	public event Action<IRunnerMessaging> SaveProgress;

	public event Action<IRunnerMessaging> AskCustomInputs;

	public event Action<IRunnerMessaging> ConfigChanged;

	public event Action<IRunnerMessaging> WordlistChanged;

	public RunnerViewModel(EnvironmentSettings environment, RLSettingsViewModel settings, Random random = null)
	{
		Env = environment;
		Settings = settings;
		this.random = ((random != null) ? random : new Random());
		OnPropertyChanged("Busy");
		OnPropertyChanged("ControlsEnabled");
	}

	public void SetConfig(Config config, bool setRecommended)
	{
		Config = config;
		if (setRecommended)
		{
			BotsAmount = Clamp(config.Settings.SuggestedBots, 1, 200);
		}
		OnPropertyChanged("ConfigName");
		RaiseConfigChanged();
	}

	public void SetWordlist(Wordlist wordlist)
	{
		Wordlist = wordlist;
		OnPropertyChanged("WordlistName");
		OnPropertyChanged("WordlistSize");
		RaiseWordlistChanged();
	}

	public void Start()
	{
		RaiseMessageArrived(LogLevel.Info, "Setting up the background worker");
		Master = new AbortableBackgroundWorker();
		Master.DoWork += Run;
		Master.RunWorkerCompleted += RunCompleted;
		Master.WorkerSupportsCancellation = true;
		Timer.Reset();
		Timer.Start();
		if (!Master.IsBusy)
		{
			Master.Status = WorkerStatus.Running;
			OnPropertyChanged("Busy");
			OnPropertyChanged("ControlsEnabled");
			RaiseWorkerStatusChanged();
			Master.RunWorkerAsync();
			RaiseMessageArrived(LogLevel.Info, "Started the Master Worker");
		}
		else
		{
			RaiseMessageArrived(LogLevel.Error, "Cannot start the Background Worker (busy)");
		}
	}

	public void Stop()
	{
		try
		{
			if (Config.OcrNeeded)
			{
				ocrEngine?.StopEngines();
			}
		}
		catch
		{
		}
		if (Master.IsBusy)
		{
			Master.CancelAsync();
		}
		RaiseMessageArrived(LogLevel.Info, "Sent cancellation request to Master Worker");
		Master.Status = WorkerStatus.Stopping;
		OnPropertyChanged("Busy");
		OnPropertyChanged("ControlsEnabled");
		RaiseWorkerStatusChanged();
		usageTimer?.Dispose();
		try
		{
			if (Config.OcrNeeded)
			{
				ocrEngine?.DisposeEngines();
			}
		}
		catch
		{
		}
		try
		{
			if (Config.JsNeeded)
			{
				jsEngine?.DisposeEngines();
			}
		}
		catch
		{
		}
	}

	public void ForceStop()
	{
		ocrEngine?.StopEngines();
		AbortAllBots();
		Master.Abort();
		RaiseMessageArrived(LogLevel.Info, "Hard Aborted the Master Worker");
		Timer.Stop();
		usageTimer?.Dispose();
		Master.Status = WorkerStatus.Idle;
		OnPropertyChanged("Busy");
		OnPropertyChanged("ControlsEnabled");
		RaiseWorkerStatusChanged();
		// NOTE: StartingPoint is updated in RunCompleted to avoid double-increment.
		// On .NET 8 Abort falls back to CancelAsync, so RunCompleted always fires.
		ocrEngine?.DisposeEngines();
		jsEngine?.DisposeEngines();
	}

	private void Run(object sender, DoWorkEventArgs e)
	{
		if (CustomAction != null)
		{
			Config = new Config(new ConfigSettings(), "");
		}
		if (Config == null)
		{
			throw new Exception("No Config loaded!");
		}
		if (Wordlist == null)
		{
			throw new Exception("No Wordlist loaded!");
		}
		if (!Wordlist.Temporary)
		{
			List<string[]> sublists = new List<string[]>();
			if (Wordlist.HasSubWordlist)
			{
				Wordlist.SubWordlists.ToList().ForEach(delegate(SubWordlist swl)
				{
					try
					{
						if (File.Exists(swl.Path))
						{
							sublists.Add(File.ReadAllLines(swl.Path));
						}
					}
					catch
					{
					}
				});
			}
			DataPool = new DataPool(File.ReadLines(Wordlist.Path), sublists, Settings.General.RemoveDuplicateWordlist, Wordlist.RemoveDup);
			if (Wordlist.Total != DataPool.Size)
			{
				Wordlist = new Wordlist(Wordlist.Name, Wordlist.Path, Wordlist.Type, Wordlist.Purpose, countLines: true, Wordlist.Temporary, Wordlist.SubWordlists);
			}
			Wordlist.Total = DataPool.Size;
			OnPropertyChanged("WordlistSize");
		}
		RaiseMessageArrived(LogLevel.Info, $"Loaded {DataPool.Size} lines");
		RaiseMessageArrived(LogLevel.Info, $"Using Proxies: {UseProxies}");
		if (DataPool.Size == 0L)
		{
			throw new Exception("No data to process!");
		}
		if (StartingPoint > DataPool.Size)
		{
			throw new Exception("Illegal Starting Point!");
		}
		if (CustomAction == null && Config.BlocksAmount == 0 && !LoliCodeParser.IsLoliCode(Config.Script))
		{
			throw new Exception("The Config has zero blocks!");
		}
		NoProxyWarningSent = false;
		CustomInputsInitialized = false;
		RetryCount = 0;
		Usage = "0/0";
		OcrRate = 0f;
		FailedList.Clear();
		GlobalVariables = new VariableList();
		GlobalCookies = new Dictionary<string, string>();
		ocrEngine?.DisposeEngines();
		jsEngine?.DisposeEngines();
		if (ocrEngine == null && Config.OcrNeeded)
		{
			ocrEngine = new OcrEngine();
		}
		if (jsEngine == null && Config.JsNeeded)
		{
			jsEngine = new JsEngine();
		}
		RaiseDispatchAction(delegate
		{
			HitsList.Clear();
			CustomList.Clear();
			ToCheckList.Clear();
		});
		if (UseProxies)
		{
			LoadProxies();
			if (TotalProxiesCount == 0)
			{
				throw new Exception("Zero proxies available!");
			}
		}
		usageTimer = new Timer(delegate
		{
			if (ShouldStop())
			{
				return;
			}
			try
			{
				Usage = RuriLib.Models.Usage.Get()?.ToString();
			}
			catch
			{
			}
		}, null, TimeSpan.Zero, TimeSpan.FromSeconds(0.8));
		if (Config.Settings.CustomInputs.Count > 0)
		{
			RaiseAskCustomInputs();
			while (!CustomInputsInitialized)
			{
				Thread.Sleep(100);
			}
		}
		RaiseDispatchAction(delegate
		{
			Bots.Clear();
		});
		_botById.Clear();
		for (int i = 1; i <= BotsAmount; i++)
		{
			RaiseMessageArrived(LogLevel.Info, $"Creating bot {i}");
			RunnerBotViewModel bot = new RunnerBotViewModel(i);
			if (Settings.General.BotsDisplayMode == BotsDisplayMode.None)
			{
				bot.Status = "Bots Display is Disabled in Settings";
			}
			bot.Worker.DoWork += RunBot;
			bot.Worker.RunWorkerCompleted += (s, _) => _botFreeEvent.Set();
			_botById[bot.Id] = bot;
			RaiseDispatchAction(delegate
			{
				Bots.Add(bot);
			});
		}
		foreach (string item in DataPool.List.Skip(StartingPoint - 1))
		{
			if (Master.CancellationPending)
			{
				AbortAllBots();
				return;
			}
			CData cData = new CData(item, Env.GetWordlistType(Wordlist.Type));
			if (!cData.IsValid || !cData.RespectsRules(Config.Settings.DataRules.ToList()))
			{
				lock (_failedListLock) { FailedList.Add(new ValidData(item, 0f, "", ProxyType.Http, BotStatus.FAIL, "", "", "", null)); }
				continue;
			}
			UpdateTimer();
			UpdateStats();
			UpdateCPM();
			if (BotsAmount != Bots.Count)
			{
				RaiseMessageArrived(LogLevel.Info, $"Bots Number was changed from {Bots.Count} to {BotsAmount}");
				if (BotsAmount > Bots.Count)
				{
					for (int j = Bots.Count + 1; j <= BotsAmount; j++)
					{
						RaiseMessageArrived(LogLevel.Info, $"Creating bot {j}");
						try
						{
							RunnerBotViewModel bot2 = new RunnerBotViewModel(j);
							if (Settings.General.BotsDisplayMode == BotsDisplayMode.None)
							{
								bot2.Status = "Bots Display is Disabled in Settings";
							}
							bot2.Worker.DoWork += RunBot;
							bot2.Worker.RunWorkerCompleted += (s, _) => _botFreeEvent.Set();
							_botById[bot2.Id] = bot2;
							RaiseDispatchAction(delegate
							{
								Bots.Add(bot2);
							});
						}
						catch (Exception ex)
						{
							RaiseMessageArrived(LogLevel.Error, $"Error while creating bot {j}: " + ex.Message);
						}
					}
				}
				else
				{
					int k;
					for (k = Bots.Count - 1; k >= BotsAmount; k--)
					{
						RaiseMessageArrived(LogLevel.Info, $"Removing bot {k}");
						try
						{
							RunnerBotViewModel runnerBotViewModel = Bots[k];
							if (runnerBotViewModel.IsDriverOpen)
							{
								runnerBotViewModel.Driver.Quit();
							}
							runnerBotViewModel.Worker.CancelAsync();
							runnerBotViewModel.Worker.Abort();
							RaiseDispatchAction(delegate
							{
								Bots.RemoveAt(k);
							});
						}
						catch (Exception ex2)
						{
							RaiseMessageArrived(LogLevel.Error, $"Error while creating bot {k}: " + ex2.Message);
						}
					}
				}
			}
			bool flag = false;
			while (!flag)
			{
				if (Settings.General.MaxHits != 0 && HitCount >= Settings.General.MaxHits)
				{
					AbortAllBots();
					return;
				}
				if (Master.CancellationPending)
				{
					AbortAllBots();
					return;
				}
				if (Timer.IsRunning && (int)Timer.Elapsed.TotalSeconds != 0)
				{
					if ((int)Timer.Elapsed.TotalSeconds % 120 == 0)
					{
						RaiseSaveProgress();
					}
					if (Settings.Proxies.ReloadInterval > 0 && (int)Timer.Elapsed.TotalSeconds % (Settings.Proxies.ReloadInterval * 60) == 0)
					{
						LoadProxies();
					}
				}
				if (Config.Settings.MaxCPM > 0 && cpm >= Config.Settings.MaxCPM)
				{
					UpdateCPM();
				}
				else
				{
					foreach (RunnerBotViewModel bot3 in Bots)
					{
						if (!bot3.Worker.IsBusy)
						{
							bot3.Worker.RunWorkerAsync(item);
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					UpdateTimer();
					UpdateStats();
					_botFreeEvent.Wait(25);
					_botFreeEvent.Reset();
				}
			}
		}
		var _drainSw = System.Diagnostics.Stopwatch.StartNew();
		while (Bots.Select((RunnerBotViewModel b) => b.Worker).Any((AbortableBackgroundWorker w) => w.IsBusy) && !Master.CancellationPending)
		{
			if (_drainSw.ElapsedMilliseconds >= 500)
			{
				UpdateStats();
				UpdateCPM();
				_drainSw.Restart();
			}
			Thread.Sleep(25);
		}
	}

	private void RunCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		if (Settings.General.SendToCheckOnAbort)
		{
			foreach (RunnerBotViewModel item2 in Bots.Where((RunnerBotViewModel b) => b.Worker.IsBusy))
			{
				ValidData item = new ValidData(item2.Data, 0f, item2.Proxy, ProxyType.Http, BotStatus.NONE, "NONE", "", "", new List<LogEntry>());
				ToCheckList.Add(item);
				UpdateStats();
				Hit hit = new Hit(item2.Data, new VariableList(), item2.Proxy, "NONE", ConfigName, WordlistName);
				RaiseFoundHit(hit);
			}
		}
		if (e.Error != null)
		{
			RaiseMessageArrived(LogLevel.Error, "The Master Worker has encountered an error: " + e.Error.Message, prompt: true);
		}
		Master.Status = WorkerStatus.Idle;
		OnPropertyChanged("Busy");
		OnPropertyChanged("ControlsEnabled");
		RaiseWorkerStatusChanged();
		Timer.Stop();
		usageTimer?.Dispose();
		AbortAllBots();
		StartingPoint += TestedCount;
		ocrEngine?.DisposeEngines();
		jsEngine?.DisposeEngines();
	}

	private void RunBot(object sender, DoWorkEventArgs e)
	{
		AbortableBackgroundWorker senderABW = (AbortableBackgroundWorker)sender;
		// Bug 4 fix: use ConcurrentDictionary instead of iterating ObservableCollection from a bot thread
		if (!_botById.TryGetValue(senderABW.Id, out RunnerBotViewModel runnerBotViewModel))
			return;
		runnerBotViewModel.Status = "INITIALIZING...";
		string text2 = (runnerBotViewModel.Data = (string)e.Argument);
		try
		{
			List<string[]> sublists = DataPool.Sublists;
			if (sublists != null && sublists.Count > 0)
			{
				Tuple<string, bool> tuple = new Tuple<string, bool>(string.Empty, item2: true);
				do
				{
					tuple = GetDataFromSubWordlist();
				}
				while (string.IsNullOrEmpty(tuple.Item1) && tuple.Item2);
				text2 = (runnerBotViewModel.Data = text2 + tuple.Item1);
			}
		}
		catch
		{
		}
		CData cData = new CData(text2, Env.GetWordlistType(Wordlist.Type));
		RaiseMessageArrived(LogLevel.Info, $"[{runnerBotViewModel.Id}] started with data " + runnerBotViewModel.Data);
		try
		{
			CProxy cProxy;
			BotData botData;
			ValidData validData;
			string type;
			VariableList variableList;
			while (true)
			{
				if (senderABW.CancellationPending || ShouldStop())
				{
					RaiseMessageArrived(LogLevel.Info, $"[{runnerBotViewModel.Id}] Cancellation pending, aborting");
					return;
				}
				cProxy = null;
				if (UseProxies)
				{
					cProxy = ProxyPool.GetProxy(Settings.Proxies.ConcurrentUse, Config.Settings.MaxProxyUses, Settings.Proxies.NeverBan);
					if (cProxy == null && ProxyPool.Alive.Count > 0)
					{
						// Proxies exist but are BUSY — share them concurrently so all bots stay active
						cProxy = ProxyPool.GetProxy(true, Config.Settings.MaxProxyUses, Settings.Proxies.NeverBan);
					}
					if (cProxy == null)
					{
						// All proxies truly exhausted (BAD/BANNED) — reload or abort
						if (Settings.Proxies.Reload)
						{
							if (Interlocked.CompareExchange(ref _isReloadingProxies, 1, 0) != 0)
							{
								Thread.Sleep(100);
								continue;
							}
							try { LoadProxies(); }
							finally { Interlocked.Exchange(ref _isReloadingProxies, 0); }
							continue; // retry GetProxy after reload
						}
						else if (!ShouldStop())
						{
							if (!NoProxyWarningSent)
							{
								RaiseMessageArrived(LogLevel.Error, "No more proxies and no Unban All option selected OR the config has a max proxy use! Aborting", prompt: true, 2);
								NoProxyWarningSent = true;
							}
							Master.CancelAsync();
							return;
						}
						continue;
					}
					runnerBotViewModel.Proxy = cProxy.Proxy;
				}
				// Clone proxy with sticky session ID so all requests in this bot run use the same exit IP
				CProxy effectiveProxy = cProxy;
				if (cProxy != null && Config.Settings.StickyProxy && !string.IsNullOrEmpty(cProxy.Username))
				{
					var sid = Guid.NewGuid().ToString("N").Substring(0, 8);
					var suffix = !string.IsNullOrEmpty(Config.Settings.StickyProxySuffix)
						? Config.Settings.StickyProxySuffix
						: "-sessid-{0}";
					effectiveProxy = new CProxy
					{
						Proxy     = cProxy.Proxy,
						Username  = cProxy.Username + string.Format(suffix, sid),
						Password  = cProxy.Password,
						Type      = cProxy.Type,
						Clearance = cProxy.Clearance,
						Cfduid    = cProxy.Cfduid
					};
				}
				string text4 = ((cProxy == null) ? "NONE" : $"{cProxy.Proxy} ({cProxy.Type})");
				botData = null;
				lock (randomLocker)
				{
					botData = new BotData(Settings, Config.Settings, cData, effectiveProxy, UseProxies, random, runnerBotViewModel.Id, isDebug: false)
					{
						BotsAmount = BotsAmount,
						OcrEngine = ocrEngine,
						JsEngine = jsEngine,
						Worker = Master
					};
				}
				botData.Driver = runnerBotViewModel.Driver;
				botData.BrowserOpen = runnerBotViewModel.IsDriverOpen;
				List<LogEntry> list = new List<LogEntry>();
				foreach (KeyValuePair<string, string> customInput in CustomInputs)
				{
					botData.Variables.Set(new CVar(customInput.Key, customInput.Value));
				}
				botData.GlobalVariables = GlobalVariables;
				// Bug 2 fix: lock GlobalCookies while copying to bot (prevents concurrent replace crash)
				lock (_globalCookiesLock)
				{
					foreach (KeyValuePair<string, string> globalCookie in GlobalCookies)
					{
						botData.Cookies[globalCookie.Key] = globalCookie.Value;
					}
				}
				if (botData.UseProxies && botData.Proxy != null && botData.Proxy.Clearance != string.Empty && !Settings.Proxies.AlwaysGetClearance)
				{
					botData.Cookies["cf_clearance"] = botData.Proxy.Clearance;
					botData.Cookies["__cfduid"] = botData.Proxy.Cfduid;
				}
				list.Add(new LogEntry($"===== LOG FOR BOT #{runnerBotViewModel.Id} WITH DATA {botData.Data.Data} AND PROXY {text4} ====={Environment.NewLine}", Colors.White));
				if (CustomAction != null)
				{
					RaiseMessageArrived(LogLevel.Info, $"[{runnerBotViewModel.Id}] Executing custom action");
					try
					{
						CustomAction(botData);
					}
					catch (Exception ex)
					{
						RaiseMessageArrived(LogLevel.Info, $"[{runnerBotViewModel.Id}] CUSTOM ACTION EXCEPTION: {ex.ToString()}");
						throw;
					}
				}
				else
				{
					LoliScript loliScript = new LoliScript(Config.Script);
					loliScript.Reset();
					if (!Settings.General.EnableBotLog)
					{
						list.Add(new LogEntry("The Bot Logging is disabled in General Settings", Colors.Tomato));
					}
					if (Config.Settings.AlwaysOpen)
					{
						SBlockBrowserAction.OpenBrowser(botData);
					}
					do
					{
						if (senderABW.CancellationPending || ShouldStop())
						{
							RaiseMessageArrived(LogLevel.Info, $"[{runnerBotViewModel.Id}] Cancellation pending, aborting");
							return;
						}
						if (Settings.General.BotsDisplayMode == BotsDisplayMode.Everything)
						{
							runnerBotViewModel.Status = "<<< PROCESSING BLOCK: " + loliScript.NextBlock + " >>>";
						}
						try
						{
							loliScript.TakeStep(botData);
						}
						catch (BlockProcessingException ex2)
						{
							if (Settings.General.BotsDisplayMode == BotsDisplayMode.Everything)
							{
								runnerBotViewModel.Status = "<<< ERROR IN BLOCK: " + loliScript.NextBlock + " >>>";
							}
							RaiseMessageArrived(LogLevel.Error, $"[{runnerBotViewModel.Id}][{runnerBotViewModel.Data}][{text4}] ERROR in block {loliScript.NextBlock} | Exception: {ex2.Message}");
							Thread.Sleep(200);
						}
						catch (Exception ex3)
						{
							if (Settings.General.BotsDisplayMode == BotsDisplayMode.Everything)
							{
								runnerBotViewModel.Status = "<<< SCRIPT ERROR >>>";
							}
							RaiseMessageArrived(LogLevel.Error, $"[{runnerBotViewModel.Id}][{runnerBotViewModel.Data}][{text4}] ERROR in the script | Exception: {ex3.Message}");
							Thread.Sleep(200);
						}
						if (botData.LogBuffer.Count > 0)
						{
							list.AddRange(botData.LogBuffer);
							list.Add(new LogEntry("", Colors.White));
						}
					}
					while (loliScript.CanProceed);
				}
				list.Add(new LogEntry("===== BOT TERMINATED WITH RESULT: " + botData.StatusString + " =====", Colors.White));
				if (Settings.General.BotsDisplayMode != 0)
				{
					runnerBotViewModel.Status = "<<< FINISHED WITH RESULT: " + botData.StatusString + " >>>";
				}
				RaiseMessageArrived(LogLevel.Info, $"[{runnerBotViewModel.Id}][{runnerBotViewModel.Data}][{text4}] Ended with result {botData.StatusString}");
				if (Config.Settings.AlwaysQuit || (Config.Settings.QuitOnBanRetry && (botData.Status == BotStatus.BAN || botData.Status == BotStatus.RETRY)))
				{
					try
					{
						botData.Driver.Quit();
						botData.BrowserOpen = false;
					}
					catch
					{
					}
				}
				runnerBotViewModel.Driver = botData.Driver;
				runnerBotViewModel.IsDriverOpen = botData.BrowserOpen;
				if (UseProxies)
				{
					cProxy.Status = Status.AVAILABLE;
					cProxy.Uses++;
					cProxy.Hooked--;
				}
				Balance = botData.Balance;
				// Bug 2 fix: lock GlobalCookies while writing back from bot (prevents concurrent replace + enumeration crash)
				lock (_globalCookiesLock)
				{
					GlobalCookies = new Dictionary<string, string>(botData.GlobalCookies);
				}
				validData = null;
				type = botData.Status.ToString();
				variableList = new VariableList(botData.Variables.All.Where((CVar v) => v.IsCapture && !v.Hidden).ToList());
				switch (botData.Status)
				{
				case BotStatus.SUCCESS:
					OcrRate = botData.OcrRate;
					validData = new ValidData(botData.Data.Data, botData.OcrRate, (botData.Proxy == null) ? "" : botData.Proxy.Proxy, (botData.Proxy != null) ? botData.Proxy.Type : ProxyType.Http, botData.Status, "HIT", variableList.ToCaptureString(), Settings.General.SaveLastSource ? botData.ResponseSource : "", list);
					RaiseDispatchAction(delegate
					{
						HitsList.Add(validData);
					});
					if (UseProxies && !Settings.Proxies.NeverBan && Config.Settings.BanProxyAfterGoodStatus)
					{
						cProxy.Status = Status.BANNED;
						cProxy.LastUsed = DateTime.Now;
						RetryCount++;
					}
					break;
				case BotStatus.FAIL:
					OcrRate = botData.OcrRate;
					lock (_failedListLock) { FailedList.Add(new ValidData("", botData.OcrRate, "", ProxyType.Http, botData.Status, "", "", "", null)); }
					break;
				case BotStatus.CUSTOM:
					OcrRate = botData.OcrRate;
					type = botData.CustomStatus;
					validData = new ValidData(botData.Data.Data, botData.OcrRate, (botData.Proxy == null) ? "" : botData.Proxy.Proxy, (botData.Proxy != null) ? botData.Proxy.Type : ProxyType.Http, botData.Status, type, variableList.ToCaptureString(), Settings.General.SaveLastSource ? botData.ResponseSource : "", list);
					RaiseDispatchAction(delegate
					{
						CustomList.Add(validData);
					});
					if (UseProxies && !Settings.Proxies.NeverBan && Config.Settings.BanProxyAfterGoodStatus)
					{
						cProxy.Status = Status.BANNED;
						cProxy.LastUsed = DateTime.Now;
						RetryCount++;
					}
					break;
				case BotStatus.BAN:
					OcrRate = botData.OcrRate;
					if (UseProxies && !Settings.Proxies.NeverBan)
					{
						cProxy.Status = Status.BANNED;
						cProxy.LastUsed = DateTime.Now;
						RetryCount++;
					}
					if (ShouldTriggerEvasion(cData.Retries))
					{
						cData.Retries++;
						continue;
					}
					RaiseMessageArrived(LogLevel.Warning, $"[{runnerBotViewModel.Id}][{runnerBotViewModel.Data}] Maximum retries exceeded");
					botData.Status = BotStatus.NONE;
					type = botData.Status.ToString();
					goto case BotStatus.NONE;
				case BotStatus.ERROR:
					RetryCount++;
					OcrRate = botData.OcrRate;
					if (UseProxies)
					{
						cProxy.Status = Status.BAD;
					}
					if (ShouldTriggerEvasion(cData.Retries))
					{
						cData.Retries++;
						continue;
					}
					RaiseMessageArrived(LogLevel.Warning, $"[{runnerBotViewModel.Id}][{runnerBotViewModel.Data}] Max ERROR retries exceeded, discarding line");
					lock (_failedListLock) { FailedList.Add(new ValidData(botData.Data.Data, botData.OcrRate, "", ProxyType.Http, BotStatus.FAIL, "", "", "", null)); }
					break;
				case BotStatus.RETRY:
					RetryCount++;
					OcrRate = botData.OcrRate;
					if (ShouldTriggerEvasion(cData.Retries))
					{
						cData.Retries++;
						continue;
					}
					RaiseMessageArrived(LogLevel.Warning, $"[{runnerBotViewModel.Id}][{runnerBotViewModel.Data}] Max RETRY retries exceeded, discarding line");
					lock (_failedListLock) { FailedList.Add(new ValidData(botData.Data.Data, botData.OcrRate, "", ProxyType.Http, BotStatus.FAIL, "", "", "", null)); }
					break;
				case BotStatus.NONE:
					OcrRate = botData.OcrRate;
					validData = new ValidData(botData.Data.Data, botData.OcrRate, (botData.Proxy == null) ? "" : botData.Proxy.Proxy, (botData.Proxy != null) ? botData.Proxy.Type : ProxyType.Http, botData.Status, "TOCHK", variableList.ToCaptureString(), Settings.General.SaveLastSource ? botData.ResponseSource : "", list);
					RaiseDispatchAction(delegate
					{
						ToCheckList.Add(validData);
					});
					if (UseProxies && !Settings.Proxies.NeverBan && Config.Settings.BanProxyAfterGoodStatus)
					{
						cProxy.Status = Status.BANNED;
						cProxy.LastUsed = DateTime.Now;
						RetryCount++;
					}
					break;
				}
				break;
			}
			if (validData != null)
			{
				Hit hit = new Hit(botData.Data.Data, variableList, (cProxy == null) ? "" : cProxy.Proxy, type, ConfigName, WordlistName);
				RaiseFoundHit(hit);
			}
			if (Settings.General.WebhookEnabled && (botData.Status == BotStatus.SUCCESS || botData.Status == BotStatus.CUSTOM))
			{
				try
				{
					string str = JsonConvert.SerializeObject(new WebhookFormat(text2, type, variableList.ToCaptureString(), DateTime.Now, Config.Settings.Name, Config.Settings.Author, Settings.General.WebhookUser));
					_ = _webhookClient.PostAsync(Settings.General.WebhookURL, new StringContent(str, System.Text.Encoding.UTF8, "application/json"));
				}
				catch
				{
					RaiseMessageArrived(LogLevel.Error, "Could not register the hit to webhook " + Settings.General.WebhookURL);
				}
			}
			if (Settings.General.WaitTime > 0)
			{
				Thread.Sleep(Settings.General.WaitTime);
			}
		}
		catch (Exception ex4)
		{
			RaiseMessageArrived(LogLevel.Error, $"[{runnerBotViewModel.Id}] Check exception on data " + text2 + " - " + ex4.Message);
		}
	}

	private bool ShouldStop()
	{
		if (!Master.CancellationPending)
		{
			return Master.Status != WorkerStatus.Running;
		}
		return true;
	}

	private Tuple<string, bool> GetDataFromSubWordlist()
	{
		try
		{
			string separator = Env.GetWordlistType(Wordlist.SubWordlists[0].Type).Separator;
			string text = separator;
			for (int i = 0; i < DataPool.Sublists.Count; i++)
			{
				text = text + DataPool.Sublists[i].Skip(StartingPoint - 1).First() + separator;
				var tmp = DataPool.Sublists[i].ToList();
				tmp.RemoveAt(StartingPoint - 1);
				DataPool.Sublists[i] = tmp.ToArray();
			}
			text = text.Trim().TrimEnd(separator[0]);
			return new Tuple<string, bool>(text, item2: false);
		}
		catch (Exception)
		{
			return new Tuple<string, bool>(string.Empty, item2: true);
		}
	}

	private void UpIndex(ref int index, bool ItsOver_StartOver = true)
	{
		try
		{
			index++;
		}
		catch (ArgumentException)
		{
			if (ItsOver_StartOver)
			{
				index = 0;
			}
		}
		catch (IndexOutOfRangeException)
		{
			if (ItsOver_StartOver)
			{
				index = 0;
			}
		}
	}

	public void UpdateTimer()
	{
		OnPropertyChanged("TimerDays");
		OnPropertyChanged("TimerHours");
		OnPropertyChanged("TimerMinutes");
		OnPropertyChanged("TimerSeconds");
		OnPropertyChanged("TimeLeft");
	}

	public void UpdateStats()
	{
		OnPropertyChanged("HitsList");
		OnPropertyChanged("ToCheckList");
		OnPropertyChanged("TestedCount");
		OnPropertyChanged("ProgressCount");
		OnPropertyChanged("FailCount");
		OnPropertyChanged("HitCount");
		OnPropertyChanged("CustomCount");
		OnPropertyChanged("ToCheckCount");
		OnPropertyChanged("Balance");
		OnPropertyChanged("Progress");
		OnPropertyChanged("TotalProxiesCount");
		OnPropertyChanged("AvailableProxiesCount");
		OnPropertyChanged("AliveProxiesCount");
		OnPropertyChanged("BannedProxiesCount");
		OnPropertyChanged("BadProxiesCount");
	}

	public void UpdateCPM()
	{
		OnPropertyChanged("CPM");
	}

	private void RaiseMessageArrived(LogLevel level, string message, bool prompt = false, int timeout = 0)
	{
		this.MessageArrived?.Invoke(this, level, message, prompt, timeout);
	}

	private void RaiseWorkerStatusChanged()
	{
		OnPropertyChanged("WorkerStatus");
		this.WorkerStatusChanged?.Invoke(this);
	}

	private void RaiseFoundHit(Hit hit)
	{
		this.FoundHit?.Invoke(this, hit);
	}

	private void RaiseReloadingProxies()
	{
		this.ReloadProxies?.Invoke(this);
	}

	private void RaiseDispatchAction(Action action)
	{
		this.DispatchAction?.Invoke(this, action);
	}

	private void RaiseSaveProgress()
	{
		this.SaveProgress?.Invoke(this);
	}

	private void RaiseAskCustomInputs()
	{
		this.AskCustomInputs?.Invoke(this);
	}

	private void RaiseConfigChanged()
	{
		this.ConfigChanged?.Invoke(this);
	}

	private void RaiseWordlistChanged()
	{
		this.WordlistChanged?.Invoke(this);
	}

	private void LoadProxies()
	{
		RaiseMessageArrived(LogLevel.Info, $"Loading proxies from {Settings.Proxies.ReloadSource}");
		switch (Settings.Proxies.ReloadSource)
		{
		case ProxyReloadSource.Manager:
			RaiseReloadingProxies();
			Thread.Sleep(100);
			break;
		case ProxyReloadSource.Remote:
		{
			List<CProxy> proxies = new List<CProxy>();
			object proxiesLock = new object();
			Parallel.ForEach(Settings.Proxies.RemoteProxySources.Where((RemoteProxySource s) => s.Active), delegate(RemoteProxySource s)
			{
				try
				{
					var batch = GetProxiesFromRemoteSource(s.Url, s.Type, s.Pattern, s.Output);
					lock (proxiesLock) { proxies.AddRange(batch); }
				}
				catch (Exception ex2)
				{
					RaiseMessageArrived(LogLevel.Error, $"Could not contact the reload API {s.Url} for {s.Type} proxies - {ex2.Message}", prompt: true, 5);
				}
			});
			ProxyPool = new ProxyPool(proxies, Settings.Proxies.ShuffleOnStart);
			break;
		}
		case ProxyReloadSource.File:
			try
			{
				ProxyPool = new ProxyPool(GetProxiesFromFile(Settings.Proxies.ReloadPath, Settings.Proxies.ReloadType), Settings.Proxies.ShuffleOnStart);
			}
			catch (Exception ex)
			{
				RaiseMessageArrived(LogLevel.Error, "Could not read the proxies from file " + Settings.Proxies.ReloadPath + " - " + ex.Message, prompt: true);
			}
			break;
		}
		RaiseMessageArrived(LogLevel.Info, $"Loaded {TotalProxiesCount} proxies");
	}

	public static List<CProxy> GetProxiesFromRemoteSource(string url, ExProxyType type, string pattern, string output)
	{
		List<CProxy> list = new List<CProxy>();
		using var _proxyFetchClient = new HttpClient();
		_proxyFetchClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36 OPR/52.0.2871.64");
		_proxyFetchClient.Timeout = System.TimeSpan.FromSeconds(5);
		string input = _proxyFetchClient.GetStringAsync(url).GetAwaiter().GetResult();
		try
		{
			foreach (Match item in Regex.Matches(input, pattern))
			{
				string text = output;
				for (int i = 0; i < item.Groups.Count; i++)
				{
					text = text.Replace("[" + i + "]", item.Groups[i].Value);
				}
				list.Add(new CProxy(text, type));
			}
		}
		catch
		{
		}
		return list;
	}

	public static async Task<RemoteProxySourceResult> GetProxiesFromRemoteSourceAsync(string url, ExProxyType type, string pattern, string output)
	{
		List<CProxy> proxies = new List<CProxy>();
		try
		{
			using var _proxyFetchClientAsync = new HttpClient();
			_proxyFetchClientAsync.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36 OPR/52.0.2871.64");
			string _proxyFetchResult = await _proxyFetchClientAsync.GetStringAsync(url);
			foreach (Match item in Regex.Matches(_proxyFetchResult, pattern))
			{
				string text = output;
				for (int i = 0; i < item.Groups.Count; i++)
				{
					text = text.Replace("[" + i + "]", item.Groups[i].Value);
				}
				proxies.Add(new CProxy(text, type));
			}
		}
		catch (Exception ex)
		{
			return new RemoteProxySourceResult
			{
				Successful = false,
				Error = ex.Message,
				Url = url,
				Proxies = new List<CProxy>()
			};
		}
		return new RemoteProxySourceResult
		{
			Successful = true,
			Error = "",
			Url = url,
			Proxies = proxies
		};
	}

	public static List<CProxy> GetProxiesFromFile(string fileName, ExProxyType type)
	{
		string[] source = File.ReadAllLines(fileName);
		List<CProxy> list = new List<CProxy>();
		list.AddRange(source.Select((string l) => new CProxy(l, type)));
		return list;
	}

	private bool ShouldTriggerEvasion(int retries)
	{
		int num = ((Config.Settings.BanLoopEvasionOverride == -1) ? Settings.Proxies.BanLoopEvasion : Config.Settings.BanLoopEvasionOverride);
		// 0 = disabled: never trigger evasion (old code returned true here, causing infinite loop)
		if (num <= 0) return false;
		return retries < num;
	}

	private void AbortAllBots()
	{
		foreach (RunnerBotViewModel bot in Bots)
		{
			try
			{
				bot.Worker.CancelAsync();
			}
			catch (Exception ex)
			{
				RaiseMessageArrived(LogLevel.Error, $"Error while deleting bot {bot.Id}: " + ex.Message);
			}
		}
		QuitAllDrivers();
	}

	private void QuitAllDrivers()
	{
		foreach (RunnerBotViewModel bot in Bots)
		{
			try
			{
				if (bot.IsDriverOpen)
				{
					bot.Driver.Quit();
				}
			}
			catch (Exception ex)
			{
				RaiseMessageArrived(LogLevel.Error, $"Error while quitting driver {bot.Id}: " + ex.Message);
			}
		}
	}

	public int Clamp(int a, int min, int max)
	{
		if (a < min)
		{
			return min;
		}
		if (a > max)
		{
			return max;
		}
		return a;
	}
}

