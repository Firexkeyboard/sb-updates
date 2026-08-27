using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using OpenBullet.Models;
using OpenBullet.ViewModels;
using RuriLib;

namespace OpenBullet.Views.Main;

public class RunnerManager : Page, IComponentConnector, IStyleConnector
{
	public delegate void StartRunnerEventHandler(object sender, EventArgs e);

	private RunnerManagerViewModel vm;

	internal TextBlock helpMessageLabel;

	internal TextBlock dlCount;

	internal TextBlock mostDownloads;

	internal Button addRunnerButton;

	internal Button startAllRunnersButton;

	internal Button stopAllRunnersButton;

	internal Button removeAllRunnersButton;

	internal ItemsControl runnersControl;

	private bool _contentLoaded;

	private bool DelegateCalled { get; set; }

	public event StartRunnerEventHandler StartRunner;

	protected virtual void OnStartRunner()
	{
		this.StartRunner?.Invoke(this, EventArgs.Empty);
	}

	public RunnerManager()
	{
		vm = SB.RunnerManager;
		base.DataContext = vm;
		InitializeComponent();
		base.Loaded += delegate
		{
			if (vm.RunnersCollection.Count > 0)
			{
				helpMessageLabel.Visibility = Visibility.Collapsed;
				dlCount.Visibility = Visibility.Collapsed;
				mostDownloads.Visibility = Visibility.Collapsed;
				return;
			}
			dlCount.Visibility = Visibility.Collapsed;
			mostDownloads.Visibility = Visibility.Collapsed;
			string json = string.Empty;
			try
			{
				Task.Run(delegate
				{
					using var http = new HttpClient();
					http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:87.0) Gecko/20100101 Firefox/87.0");
					json = http.GetStringAsync("https://api.github.com/repos/mohamm4dx/SilverBullet/releases").GetAwaiter().GetResult();
					SBRelease[] array = IOManager.DeserializeObject<SBRelease[]>(json);
					if (array != null && array.Length != 0)
					{
						SBRelease currentRelease = array.FirstOrDefault((SBRelease r) => r.Ver.ToString() == "1.1.4");
						SBRelease mostDlRelease = array.OrderByDescending((SBRelease r) => r.Assets[0].download_count).FirstOrDefault();
						if (currentRelease != null)
						{
							base.Dispatcher.Invoke(() => dlCount.Visibility = Visibility.Visible);
							base.Dispatcher.Invoke(() => dlCount.Text = $"Download Count From Github: {currentRelease.Assets[0].download_count}");
							if (mostDlRelease.Assets[0].download_count == currentRelease.Assets[0].download_count)
							{
								base.Dispatcher.Invoke(() => mostDownloads.Text = "Most Downloads For This Version");
							}
							else
							{
								base.Dispatcher.Invoke(() => mostDownloads.Text = $"Most Downloads For {mostDlRelease.Ver} Version is {mostDlRelease.Assets[0].download_count} Downloads");
							}
							base.Dispatcher.Invoke(() => mostDownloads.Visibility = Visibility.Visible);
						}
					}
				});
			}
			catch
			{
			}
		};
	}

	private void addRunnerButton_Click(object sender, RoutedEventArgs e)
	{
		vm.Create();
		helpMessageLabel.Visibility = Visibility.Collapsed;
		dlCount.Visibility = Visibility.Collapsed;
		mostDownloads.Visibility = Visibility.Collapsed;
	}

	private void removeRunnerButton_Click(object sender, RoutedEventArgs e)
	{
		int id = (int)((Button)e.OriginalSource).Tag;
		if ((int)vm.Get(id).ViewModel.Master.Status != 0)
		{
			MessageBox.Show("The Runner is active! Please stop it before removing it.");
		}
		else
		{
			vm.Remove(id);
		}
	}

	private void startRunnerButton_Click(object sender, RoutedEventArgs e)
	{
		int id = (int)((Button)e.OriginalSource).Tag;
		RunnerInstance runnerInstance = vm.Get(id);
		StartRunner += runnerInstance.View.OnStartRunner;
		OnStartRunner();
		StartRunner -= runnerInstance.View.OnStartRunner;
	}

	private void runnerInstanceGrid_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (DelegateCalled)
		{
			DelegateCalled = false;
		}
		else if (sender.GetType() == typeof(Grid))
		{
			int id = (int)(sender as Grid).Tag;
			SB.MainWindow.ShowRunner(vm.Get(id).View);
		}
	}

	private static T FindParent<T>(DependencyObject child) where T : DependencyObject
	{
		DependencyObject parent = VisualTreeHelper.GetParent(child);
		if (parent == null)
		{
			return null;
		}
		if (parent is T result)
		{
			return result;
		}
		return FindParent<T>(parent);
	}

	private void selectConfig_MouseDown(object sender, MouseButtonEventArgs e)
	{
		int id = (int)FindParent<Grid>(sender as DependencyObject).Tag;
		RunnerInstance runnerInstance = SB.MainWindow.RunnerManagerPage.vm.Get(id);
		if (!runnerInstance.ViewModel.Busy)
		{
			DelegateCalled = true;
			new MainDialog(new DialogSelectConfig(runnerInstance.View), "Select Config").ShowDialog();
		}
	}

	private void selectWordlist_MouseDown(object sender, MouseButtonEventArgs e)
	{
		int id = (int)FindParent<Grid>(sender as DependencyObject).Tag;
		RunnerInstance runnerInstance = SB.MainWindow.RunnerManagerPage.vm.Get(id);
		if (!runnerInstance.ViewModel.Busy)
		{
			DelegateCalled = true;
			new MainDialog(new DialogSelectWordlist(runnerInstance.View), "Select Wordlist").ShowDialog();
		}
	}

	private void selectProxies_MouseDown(object sender, MouseButtonEventArgs e)
	{
		int id = (int)FindParent<Grid>(sender as DependencyObject).Tag;
		RunnerInstance runnerInstance = SB.MainWindow.RunnerManagerPage.vm.Get(id);
		if (!runnerInstance.ViewModel.Busy)
		{
			DelegateCalled = true;
			new MainDialog(new DialogSetProxies(runnerInstance.ViewModel), "Set Proxies").ShowDialog();
		}
	}

	private void selectBots_MouseDown(object sender, MouseButtonEventArgs e)
	{
		int id = (int)FindParent<Grid>(sender as DependencyObject).Tag;
		RunnerInstance runnerInstance = SB.MainWindow.RunnerManagerPage.vm.Get(id);
		if (!runnerInstance.ViewModel.Busy)
		{
			DelegateCalled = true;
			new MainDialog(new DialogSelectBots(runnerInstance.ViewModel, runnerInstance.ViewModel.BotsAmount), "Select Bots Number").ShowDialog();
		}
	}

	private void stopAllRunnersButton_Click(object sender, RoutedEventArgs e)
	{
		foreach (RunnerInstance item in vm.RunnersCollection.Where((RunnerInstance r) => r.ViewModel.Busy))
		{
			StartRunner += item.View.OnStartRunner;
			OnStartRunner();
			StartRunner -= item.View.OnStartRunner;
		}
	}

	private void removeAllRunnersButton_Click(object sender, RoutedEventArgs e)
	{
		if (MessageBox.Show("Are you sure you want to remove all Runners?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
		{
			List<RunnerInstance> list = vm.RunnersCollection.Where((RunnerInstance r) => !r.ViewModel.Busy).ToList();
			for (int num = list.Count - 1; num >= 0; num--)
			{
				vm.RunnersCollection.Remove(list[num]);
			}
		}
	}

	private void startAllRunnersButton_Click(object sender, RoutedEventArgs e)
	{
		foreach (RunnerInstance item in vm.RunnersCollection.Where((RunnerInstance r) => !r.ViewModel.Busy))
		{
			StartRunner += item.View.OnStartRunner;
			OnStartRunner();
			StartRunner -= item.View.OnStartRunner;
		}
	}

	private void LabelCustom_MouseEnter(object sender, MouseEventArgs e)
	{
		try
		{
			int id = (int)FindParent<Grid>(sender as DependencyObject).Tag;
			SB.MainWindow.RunnerManagerPage.vm.Get(id).View.LabelCustom_MouseEnter(sender, e);
		}
		catch
		{
		}
	}

	private void LabelCustom_MouseLeave(object sender, MouseEventArgs e)
	{
		try
		{
			(e.OriginalSource as Label).ToolTip = null;
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/runnermanager.xaml", UriKind.Relative);
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
			helpMessageLabel = (TextBlock)target;
			break;
		case 2:
			dlCount = (TextBlock)target;
			break;
		case 3:
			mostDownloads = (TextBlock)target;
			break;
		case 4:
			addRunnerButton = (Button)target;
			addRunnerButton.Click += addRunnerButton_Click;
			break;
		case 5:
			startAllRunnersButton = (Button)target;
			startAllRunnersButton.Click += startAllRunnersButton_Click;
			break;
		case 6:
			stopAllRunnersButton = (Button)target;
			stopAllRunnersButton.Click += stopAllRunnersButton_Click;
			break;
		case 7:
			removeAllRunnersButton = (Button)target;
			removeAllRunnersButton.Click += removeAllRunnersButton_Click;
			break;
		case 8:
			runnersControl = (ItemsControl)target;
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
		switch (connectionId)
		{
		case 9:
			((Grid)target).MouseDown += runnerInstanceGrid_MouseDown;
			break;
		case 10:
			((Label)target).MouseDown += selectConfig_MouseDown;
			break;
		case 11:
			((Label)target).MouseDown += selectConfig_MouseDown;
			break;
		case 12:
			((Label)target).MouseDown += selectWordlist_MouseDown;
			break;
		case 13:
			((Label)target).MouseDown += selectWordlist_MouseDown;
			break;
		case 14:
			((Label)target).MouseDown += selectBots_MouseDown;
			break;
		case 15:
			((Label)target).MouseDown += selectBots_MouseDown;
			break;
		case 16:
			((Label)target).MouseDown += selectProxies_MouseDown;
			break;
		case 17:
			((Label)target).MouseDown += selectProxies_MouseDown;
			break;
		case 18:
			((Label)target).MouseEnter += LabelCustom_MouseEnter;
			((Label)target).MouseLeave += LabelCustom_MouseLeave;
			break;
		case 19:
			((Button)target).Click += startRunnerButton_Click;
			break;
		case 20:
			((Button)target).Click += removeRunnerButton_Click;
			break;
		}
	}
}
