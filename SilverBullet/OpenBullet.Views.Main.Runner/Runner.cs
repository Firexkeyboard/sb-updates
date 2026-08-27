using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using MahApps.Metro.IconPacks;
using OpenBullet.ViewModels;
using PluginFramework;
using RuriLib;
using RuriLib.Functions.Files;
using RuriLib.Models;
using RuriLib.Runner;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main.Runner;

public class Runner : Page, IComponentConnector, IStyleConnector
{
	private RunnerViewModel vm;

	private SoundPlayer hitPlayer;

	private SoundPlayer reloadPlayer;

	// Bug 6 fix: use int with Interlocked for thread-safe lock (plain bool is not atomic)
	private int _soundLock = 0;

	internal TextBox labelStartingPoint;

	internal Slider startingPointSlider;

	internal Slider botsSlider;

	internal RadioButton proxyDefRadio;

	internal RadioButton proxyYesRadio;

	internal RadioButton proxyNoRadio;

	internal Button startButton;

	internal PackIconMaterial startButtonIcon;

	internal TextBlock startButtonLabel;

	internal Grid rightGrid;

	internal ListView validListView;

	internal Button hitsFilterButton;

	internal Button customFilterButton;

	internal Button toCheckFilterButton;

	internal Label daysLabel;

	internal Label hoursLabel;

	internal Label minutesLabel;

	internal Label secondsLabel;

	internal Label timeLeft;

	internal ListView botsListView;

	internal Grid bottomLeftGrid;

	internal Button selectConfigButton;

	internal Button selectWordlistButton;

	internal RichTextBox logBox;

	internal Button showManagerButton;

	private bool _contentLoaded;

	public Runner(RunnerViewModel vm)
	{
		this.vm = vm;
		base.DataContext = vm;
		InitializeComponent();
		vm.MessageArrived += LogRunnerData;
		vm.WorkerStatusChanged += LogWorkerStatus;
		vm.WorkerStatusChanged += ProcessStatusChange;
		vm.FoundHit += PlayHitSound;
		vm.FoundHit += RegisterHit;
		vm.ReloadProxies += PlayReloadSound;
		vm.ReloadProxies += LoadProxiesFromManager;
		vm.DispatchAction += ExecuteAction;
		vm.SaveProgress += SaveProgressToDB;
		vm.AskCustomInputs += InitCustomInputs;
		if (SB.SBSettings.General.ChangeRunnerInterface)
		{
			SB.Logger.LogInfo(Components.About, "Changed the Runner interface");
			Grid.SetColumn(rightGrid, 0);
			Grid.SetRow(rightGrid, 2);
			Grid.SetColumn(bottomLeftGrid, 2);
			Grid.SetRow(bottomLeftGrid, 0);
		}
		logBox.AppendText("", Colors.White);
		logBox.AppendText("Runner initialized succesfully!" + Environment.NewLine, Utils.GetColor("ForegroundMain"));
	}

	private void LogRunnerData(IRunnerMessaging sender, LogLevel level, string message, bool prompt, int timeout)
	{
		SB.Logger.Log(Components.Runner, level, message, prompt, timeout);
	}

	private void LogWorkerStatus(IRunnerMessaging sender)
	{
		RunnerViewModel runner = (RunnerViewModel)(object)((sender is RunnerViewModel) ? sender : null);
		WorkerStatus status = runner.Master.Status;
		switch ((int)status)
		{
		case 1:
			base.Dispatcher.Invoke(delegate
			{
				logBox.AppendText($"Started Running Config {runner.ConfigName} with Wordlist {runner.WordlistName} at {DateTime.Now}.{Environment.NewLine}", Utils.GetColor("ForegroundGood"));
			});
			break;
		case 2:
			base.Dispatcher.Invoke(delegate
			{
				logBox.AppendText($"Sent Abort Request at {DateTime.Now}.{Environment.NewLine}", Utils.GetColor("ForegroundCustom"));
			});
			break;
		case 0:
			base.Dispatcher.Invoke(delegate
			{
				logBox.AppendText($"Aborted Runner at {DateTime.Now}.{Environment.NewLine}", Utils.GetColor("ForegroundBad"));
			});
			break;
		}
	}

	private void ExecuteAction(IRunnerMessaging sender, Action action)
	{
		Application.Current.Dispatcher.Invoke(action);
	}

	private void RegisterHit(IRunnerMessaging sender, Hit hit)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			if (vm.Config != null && vm.Config.Settings.SaveHitsToTextFile)
			{
				try
				{
					SB.Logger.LogInfo(Components.Runner, "Adding " + hit.Type + " hit " + hit.Data + " to the text file");
					string text = Path.Combine("UserData\\Hits", Files.MakeValidFileName(vm.Config.Settings.Name, true));
					if (!Directory.Exists(text))
					{
						Directory.CreateDirectory(text);
					}
					string text2 = Path.Combine(text, hit.Type + ".txt");
					lock (FileLocker.GetLock(text2))
					{
						File.AppendAllText(text2, hit.Data + " | " + hit.CapturedString + Environment.NewLine);
						return;
					}
				}
				catch (Exception ex)
				{
					SB.Logger.LogError(Components.Runner, "Failed to add " + hit.Type + " hit " + hit.Data + " to the text file - " + ex.Message);
					return;
				}
			}
			try
			{
				SB.Logger.LogInfo(Components.Runner, "Adding " + hit.Type + " hit " + hit.Data + " to the DB");
				SB.HitsDB.Add(hit);
				SB.MainWindow.HitsDBPage.AddConfigToFilter(vm.ConfigName);
			}
			catch (Exception ex2)
			{
				SB.Logger.LogError(Components.Runner, "Failed to add " + hit.Type + " hit " + hit.Data + " to the DB - " + ex2.Message);
			}
		});
	}

	private void PlayHitSound(IRunnerMessaging sender, Hit hit)
	{
		if (!SB.SBSettings.Sounds.EnableSounds || !(hit.Type == "SUCCESS"))
		{
			return;
		}
		// Bug 6 fix: Interlocked.CompareExchange atomically claims the lock — no two threads play at once
		if (Interlocked.CompareExchange(ref _soundLock, 1, 0) != 0)
			return; // another bot is already playing; skip silently
		try
		{
			hitPlayer.Play();
		}
		catch { }
		finally
		{
			Interlocked.Exchange(ref _soundLock, 0);
		}
	}

	private void PlayReloadSound(IRunnerMessaging sender)
	{
		if (!SB.SBSettings.Sounds.EnableSounds)
		{
			return;
		}
		if (Interlocked.CompareExchange(ref _soundLock, 1, 0) != 0)
			return;
		try
		{
			reloadPlayer.Play();
		}
		catch { }
		finally
		{
			Interlocked.Exchange(ref _soundLock, 0);
		}
	}

	private void LoadProxiesFromManager(IRunnerMessaging sender)
	{
		List<CProxy> list = SB.ProxyManager.ProxiesCollection.Where(p => !p.Disabled).ToList();
		List<CProxy> list2 = (vm.Config.Settings.OnlySocks ? list.Where((CProxy x) => (int)x.Type > 0).ToList() : ((!vm.Config.Settings.OnlySsl) ? list : list.Where((CProxy x) => (int)x.Type == 0).ToList()));
		vm.ProxyPool = new ProxyPool(list2, SB.Settings.RLSettings.Proxies.ShuffleOnStart);
	}

	private void ProcessStatusChange(IRunnerMessaging sender)
	{
		if ((int)vm.Master.Status == 0)
		{
			SaveRecord();
			base.Dispatcher.Invoke(delegate
			{
				startButtonLabel.Text = "START";
				startButtonIcon.Kind = (PackIconMaterialKind)4612;
			});
		}
	}

	private void SaveProgressToDB(IRunnerMessaging sender)
	{
		SaveRecord();
	}

	private void InitCustomInputs(IRunnerMessaging sender)
	{
		Application.Current.Dispatcher.Invoke(delegate
		{
			vm.CustomInputs = new List<KeyValuePair<string, string>>();
			foreach (CustomInput customInput in vm.Config.Settings.CustomInputs)
			{
				SB.Logger.LogInfo(Components.Runner, "Asking for input " + customInput.Description);
				new MainDialog(new DialogCustomInput(vm, customInput.VariableName, customInput.Description), "Custom Input").ShowDialog();
			}
			vm.CustomInputsInitialized = true;
		});
	}

	public void OnStartRunner(object sender, EventArgs e)
	{
		startButton_Click(this, new RoutedEventArgs());
	}

	public void startButton_Click(object sender, RoutedEventArgs e)
	{
		WorkerStatus status = vm.Master.Status;
		switch ((int)status)
		{
		case 0:
			try
			{
				SBIOManager.CheckRequiredPlugins(SB.BlockPlugins.Select((IBlockPlugin b) => b.Name), vm.Config);
			}
			catch (Exception ex)
			{
				SB.Logger.LogError(Components.Runner, ex.Message, prompt: true);
				break;
			}
			SetupSoundPlayers();
			ThreadPool.SetMinThreads(vm.BotsAmount * 2 + 1, vm.BotsAmount * 2 + 1);
			ServicePointManager.DefaultConnectionLimit = 10000;
			startButtonLabel.Text = "STOP";
			startButtonIcon.Kind = (PackIconMaterialKind)5533;
			if (SB.Settings.RLSettings.General.DisableBotsListView)
			{
				botsListView.ItemsSource = null;
			}
			else
			{
				botsListView.SetBinding(ItemsControl.ItemsSourceProperty, new Binding
				{
					Source = vm.Bots
				});
			}
			vm.Start();
			break;
		case 1:
			vm.Stop();
			startButtonLabel.Text = "HARD ABORT";
			startButtonIcon.Kind = (PackIconMaterialKind)5533;
			break;
		case 2:
			vm.ForceStop();
			startButtonLabel.Text = "START";
			startButtonIcon.Kind = (PackIconMaterialKind)4612;
			SaveRecord();
			break;
		}
	}

	private void SetupSoundPlayers()
	{
		string text = "UserData/Sounds/" + SB.SBSettings.Sounds.OnHitSound;
		string text2 = "UserData/Sounds/" + SB.SBSettings.Sounds.OnReloadSound;
		if (File.Exists(text))
		{
			hitPlayer = new SoundPlayer(text);
		}
		if (File.Exists(text2))
		{
			reloadPlayer = new SoundPlayer(text2);
		}
		SB.Logger.LogInfo(Components.Runner, "Set up sound players");
	}

	public void SetConfig(Config config)
	{
		vm.SetConfig(config, SB.SBSettings.General.RecommendedBots);
		RetrieveRecord();
	}

	public void SetWordlist(Wordlist wordlist)
	{
		vm.SetWordlist(wordlist);
		RetrieveRecord();
	}

	private void selectConfigButton_Click(object sender, RoutedEventArgs e)
	{
		new MainDialog(new DialogSelectConfig(this), "Select Config").ShowDialog();
	}

	private void selectWordlistButton_Click(object sender, RoutedEventArgs e)
	{
		new MainDialog(new DialogSelectWordlist(this), "Select Wordlist").ShowDialog();
	}

	private void selectpProxylistButton_Click(object sender, RoutedEventArgs e)
	{
	}

	private void hitsFilterButton_Click(object sender, RoutedEventArgs e)
	{
		vm.ResultsFilter = (BotStatus)2;
		SB.Logger.LogInfo(Components.Runner, $"Changed valid filter to {vm.ResultsFilter}");
		RefreshListView();
	}

	private void customFilterButton_Click(object sender, RoutedEventArgs e)
	{
		vm.ResultsFilter = (BotStatus)6;
		SB.Logger.LogInfo(Components.Runner, $"Changed valid filter to {vm.ResultsFilter}");
		RefreshListView();
	}

	private void toCheckFilterButton_Click(object sender, RoutedEventArgs e)
	{
		vm.ResultsFilter = (BotStatus)0;
		SB.Logger.LogInfo(Components.Runner, $"Changed valid filter to {vm.ResultsFilter}");
		RefreshListView();
	}

	private void RefreshListView()
	{
		validListView.ItemsSource = vm.EmptyList;
		BotStatus resultsFilter = vm.ResultsFilter;
		if ((int)resultsFilter != 0)
		{
			if ((int)resultsFilter != 2)
			{
				if ((int)resultsFilter == 6)
				{
					validListView.ItemsSource = vm.CustomList;
				}
			}
			else
			{
				validListView.ItemsSource = vm.HitsList;
			}
		}
		else
		{
			validListView.ItemsSource = vm.ToCheckList;
		}
	}

	private ListView GetCurrentListView()
	{
		return validListView;
	}

	private void showManagerButton_Click(object sender, RoutedEventArgs e)
	{
		SB.MainWindow.ShowRunnerManager();
	}

	private void ListViewItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
	}

	private void showHTML_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			File.WriteAllText("source.html", ((ValidData)GetCurrentListView().SelectedItem).Source);
			Process.Start("source.html");
			SB.Logger.LogInfo(Components.Runner, "Saved the html to source.html and opened it with the default viewer");
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.Runner, "Couldn't show the HTML - " + ex.Message, prompt: true);
		}
	}

	private void showLog_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			new MainDialog(new DialogShowLog(((ValidData)GetCurrentListView().SelectedItem).Log), "Complete Log").Show();
			SB.Logger.LogInfo(Components.Runner, "Opened the log for the hit " + ((ValidData)GetCurrentListView().SelectedItem).Data);
		}
		catch
		{
			MessageBox.Show("FAILED");
		}
	}

	private void copySelectedData_Click(object sender, RoutedEventArgs e)
	{
		string text = "";
		try
		{
			foreach (ValidData selectedItem in GetCurrentListView().SelectedItems)
			{
				ValidData val = selectedItem;
				text = text + val.Data + Environment.NewLine;
			}
			SB.Logger.LogInfo(Components.Runner, $"Copied {GetCurrentListView().SelectedItems.Count} data");
			Clipboard.SetText(text);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.Runner, "Exception while copying data - " + ex.Message);
		}
	}

	private void copySelectedCaptureOnly_Click(object sender, RoutedEventArgs e)
	{
		string text = "";
		try
		{
			foreach (ValidData selectedItem in GetCurrentListView().SelectedItems)
			{
				ValidData val = selectedItem;
				text = text + val.CapturedData + Environment.NewLine;
			}
			SB.Logger.LogInfo(Components.Runner, $"Copied {GetCurrentListView().SelectedItems.Count} data");
			Clipboard.SetText(text);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.Runner, "Exception while copying data - " + ex.Message);
		}
	}

	private void copySelectedCapture_Click(object sender, RoutedEventArgs e)
	{
		string text = "";
		try
		{
			foreach (ValidData selectedItem in GetCurrentListView().SelectedItems)
			{
				ValidData val = selectedItem;
				text = text + val.Data + " | " + val.CapturedData + Environment.NewLine;
			}
			SB.Logger.LogInfo(Components.Runner, $"Copied {GetCurrentListView().SelectedItems.Count} data");
			Clipboard.SetText(text);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.Runner, "Exception while copying data - " + ex.Message);
		}
	}

	private void selectAll_Click(object sender, RoutedEventArgs e)
	{
		GetCurrentListView().SelectAll();
	}

	private void copySelectedProxy_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(((ValidData)GetCurrentListView().SelectedItem).Proxy);
			SB.Logger.LogInfo(Components.Runner, "Copied the proxy " + ((ValidData)GetCurrentListView().SelectedItem).Proxy);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.Runner, "Couldn't copy the proxy for the selected hit - " + ex.Message);
		}
	}

	private void sendToDebugger_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			StackerViewModel stacker = SB.Stacker;
			object selectedItem = GetCurrentListView().SelectedItem;
			ValidData val = (ValidData)((selectedItem is ValidData) ? selectedItem : null);
			stacker.TestData = val.Data;
			stacker.TestProxy = val.Proxy;
			stacker.ProxyType = val.ProxyType;
			SB.Logger.LogInfo(Components.Runner, "Sent to the debugger");
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.Runner, "Could not send data and proxy to the debugger - " + ex.Message);
		}
	}

	private void SaveRecord()
	{
		SB.RunnerManager.SaveRecord(vm.Config, vm.Wordlist, vm.TestedCount + vm.StartingPoint);
	}

	private void RetrieveRecord()
	{
		vm.StartingPoint = SB.RunnerManager.RetrieveRecord(vm.Config, vm.Wordlist);
	}

	public void LabelCustom_MouseEnter(object sender, MouseEventArgs e)
	{
		try
		{
			if (vm.CustomCount == 0)
			{
				return;
			}
			Label label = e.OriginalSource as Label;
			string toolTip = string.Empty;
			var enumerable = from v in vm.CustomList
				select v.Type into cType
				group cType by cType into g
				let customCount = g.Count()
				orderby customCount descending
				select new
				{
					Count = customCount,
					Name = g.Key
				};
			if (enumerable != null && enumerable.Count() > 0)
			{
				enumerable.ToList().ForEach(ct =>
				{
					toolTip += $"{ct.Name}:{ct.Count}\n";
				});
				toolTip = toolTip.TrimEnd('\n');
				label.ToolTip = new ToolTip
				{
					Content = toolTip
				};
			}
		}
		catch
		{
		}
	}

	public void LabelCustom_MouseLeave(object sender, MouseEventArgs e)
	{
		try
		{
			(e.OriginalSource as Label).ToolTip = null;
		}
		catch
		{
		}
	}

	private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		try
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
			if (!e.Handled)
			{
				TextBox textBox = (TextBox)sender;
				string text = textBox.Text;
				if (textBox.SelectedText != string.Empty)
				{
					text = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectedText.Length);
				}
				int num = int.Parse(text + e.Text);
				e.Handled = !((double)num <= botsSlider.Maximum) || !((double)num > botsSlider.Minimum - 1.0);
			}
		}
		catch
		{
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/runner/runner.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((MenuItem)target).Click += copySelectedData_Click;
			break;
		case 2:
			((MenuItem)target).Click += copySelectedCaptureOnly_Click;
			break;
		case 3:
			((MenuItem)target).Click += copySelectedProxy_Click;
			break;
		case 4:
			((MenuItem)target).Click += copySelectedCapture_Click;
			break;
		case 5:
			((MenuItem)target).Click += sendToDebugger_Click;
			break;
		case 6:
			((MenuItem)target).Click += selectAll_Click;
			break;
		case 7:
			((MenuItem)target).Click += showHTML_Click;
			break;
		case 8:
			((MenuItem)target).Click += showLog_Click;
			break;
		case 9:
			labelStartingPoint = (TextBox)target;
			break;
		case 10:
			startingPointSlider = (Slider)target;
			break;
		case 11:
			((TextBox)target).PreviewTextInput += TextBox_PreviewTextInput;
			break;
		case 12:
			botsSlider = (Slider)target;
			break;
		case 13:
			proxyDefRadio = (RadioButton)target;
			break;
		case 14:
			proxyYesRadio = (RadioButton)target;
			break;
		case 15:
			proxyNoRadio = (RadioButton)target;
			break;
		case 16:
			startButton = (Button)target;
			startButton.Click += startButton_Click;
			break;
		case 17:
			startButtonIcon = (PackIconMaterial)target;
			break;
		case 18:
			startButtonLabel = (TextBlock)target;
			break;
		case 19:
			rightGrid = (Grid)target;
			break;
		case 20:
			validListView = (ListView)target;
			break;
		case 22:
			hitsFilterButton = (Button)target;
			hitsFilterButton.Click += hitsFilterButton_Click;
			break;
		case 23:
			customFilterButton = (Button)target;
			customFilterButton.Click += customFilterButton_Click;
			break;
		case 24:
			toCheckFilterButton = (Button)target;
			toCheckFilterButton.Click += toCheckFilterButton_Click;
			break;
		case 25:
			daysLabel = (Label)target;
			break;
		case 26:
			hoursLabel = (Label)target;
			break;
		case 27:
			minutesLabel = (Label)target;
			break;
		case 28:
			secondsLabel = (Label)target;
			break;
		case 29:
			timeLeft = (Label)target;
			break;
		case 30:
			botsListView = (ListView)target;
			break;
		case 31:
			bottomLeftGrid = (Grid)target;
			break;
		case 32:
			selectConfigButton = (Button)target;
			selectConfigButton.Click += selectConfigButton_Click;
			break;
		case 33:
			selectWordlistButton = (Button)target;
			selectWordlistButton.Click += selectWordlistButton_Click;
			break;
		case 34:
			logBox = (RichTextBox)target;
			break;
		case 35:
			showManagerButton = (Button)target;
			showManagerButton.Click += showManagerButton_Click;
			break;
		case 36:
			((Label)target).MouseEnter += LabelCustom_MouseEnter;
			((Label)target).MouseLeave += LabelCustom_MouseLeave;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 21)
		{
			EventSetter eventSetter = new EventSetter();
			eventSetter.Event = UIElement.MouseRightButtonDownEvent;
			eventSetter.Handler = new MouseButtonEventHandler(ListViewItem_MouseRightButtonDown);
			((Style)target).Setters.Add(eventSetter);
		}
	}
}
