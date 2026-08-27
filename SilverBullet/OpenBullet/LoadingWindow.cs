using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AngleSharp.Text;

namespace OpenBullet;

public class LoadingWindow : Window, IComponentConnector
{
	private bool? checkUpdate;

	private MainWindow mainWindow;

	private NotesWindow notesWindow;

	private bool _canClose;

	private bool showMainWindow = true;

	private const string discoard = "https://discord.gg/8jFtRs";

	private CancellationTokenSource cancellationToken;

	private Task task;

	internal Grid Root;

	internal Grid titleBar;

	internal Label titleLabel;

	internal Grid dragPanel;

	internal StackPanel quitPanel;

	internal Image quitImage;

	internal TextBlock Wait;

	internal CheckBox checkBoxUpdate;

	private bool _contentLoaded;

	public LoadingWindow()
	{
		InitializeComponent();
		((Storyboard)FindResource("WaitStoryboard")).Begin();
	}

	private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		DragMove();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		CheckBox_Click(null, null);
		try
		{
			task = Task.Run(delegate
			{
				CheckForUpdate();
			}).ContinueWith(delegate
			{
			}, (cancellationToken = new CancellationTokenSource()).Token);
		}
		catch
		{
			new MainWindow().Show();
			Close();
		}
	}

	private void CheckForUpdate()
	{
		Task.Delay(200);
		if (checkUpdate != true)
		{
			Task.Delay(2000);
			showMainWindow = true;
			Window_Closing(null, null);
		}
		else
		{
			cancellationToken.Token.ThrowIfCancellationRequested();
		}
	}

	private void CheckBox_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (!checkUpdate.HasValue)
			{
				if (File.Exists("Settings\\Update.txt"))
				{
					checkUpdate = StringExtensions.ToBoolean(File.ReadAllText("Settings\\Update.txt"), false);
					checkBoxUpdate.IsChecked = checkUpdate;
					return;
				}
				checkUpdate = true;
			}
			try
			{
				checkUpdate = checkBoxUpdate.IsChecked == true;
			}
			catch (NullReferenceException)
			{
				checkUpdate = true;
			}
			if (File.Exists("Settings\\Update.txt"))
			{
				using (File.CreateText("Settings\\Update.txt"))
				{
				}
			}
			File.WriteAllText("Settings\\Update.txt", checkUpdate.ToString());
		}
		catch
		{
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		try
		{
			if (mainWindow == null && notesWindow == null && showMainWindow)
			{
				Hide();
				mainWindow = new MainWindow();
				mainWindow.Show();
				showMainWindow = false;
				if (e == null)
				{
					Close();
				}
			}
		}
		catch (InvalidOperationException)
		{
			try
			{
				base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
				{
					mainWindow = new MainWindow();
					mainWindow.Show();
					showMainWindow = false;
					if (e == null)
					{
						Close();
					}
				});
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private void dragPanel_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left)
		{
			DragMove();
		}
	}

	private void titleLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		DragMove();
	}

	private void quitPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_canClose)
		{
			Environment.Exit(0);
		}
	}

	private void quitPanel_MouseLeave(object sender, MouseEventArgs e)
	{
		quitPanel.Background = new SolidColorBrush(Colors.Transparent);
		_canClose = false;
	}

	private void quitPanel_MouseDown(object sender, MouseButtonEventArgs e)
	{
		_canClose = true;
	}

	private void quitPanel_MouseEnter(object sender, MouseEventArgs e)
	{
		quitPanel.Background = new SolidColorBrush(Colors.DarkRed);
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			try
			{
				task.Dispose();
			}
			catch
			{
			}
			try
			{
				cancellationToken.Cancel(throwOnFirstException: true);
			}
			catch
			{
			}
			try
			{
				cancellationToken.Dispose();
			}
			catch
			{
			}
			mainWindow = new MainWindow();
			if (notesWindow != null)
			{
				notesWindow.MainWindow = mainWindow;
			}
			mainWindow.Show();
			Close();
		}
		catch
		{
		}
	}

	private void Button_Click_1(object sender, RoutedEventArgs e)
	{
		try
		{
			Process.Start("https://discord.gg/8jFtRs");
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
			Uri resourceLocator = new Uri("/SilverBullet;component/loadingwindow.xaml", UriKind.Relative);
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
			((LoadingWindow)target).Closing += Window_Closing;
			((LoadingWindow)target).Loaded += Window_Loaded;
			break;
		case 2:
			Root = (Grid)target;
			break;
		case 3:
			titleBar = (Grid)target;
			break;
		case 4:
			titleLabel = (Label)target;
			titleLabel.MouseLeftButtonDown += titleLabel_MouseLeftButtonDown;
			break;
		case 5:
			dragPanel = (Grid)target;
			dragPanel.MouseDown += dragPanel_MouseDown;
			break;
		case 6:
			quitPanel = (StackPanel)target;
			quitPanel.MouseDown += quitPanel_MouseDown;
			quitPanel.MouseEnter += quitPanel_MouseEnter;
			quitPanel.MouseLeave += quitPanel_MouseLeave;
			quitPanel.MouseLeftButtonUp += quitPanel_MouseLeftButtonUp;
			break;
		case 7:
			quitImage = (Image)target;
			break;
		case 8:
			Wait = (TextBlock)target;
			break;
		case 9:
			checkBoxUpdate = (CheckBox)target;
			checkBoxUpdate.Click += CheckBox_Click;
			break;
		case 10:
			((Button)target).Click += Button_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
