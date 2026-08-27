using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace OpenBullet;

public class NotesWindow : Window, IComponentConnector
{
	private bool _canClose;

	public MainWindow MainWindow;

	internal Grid Root;

	internal Grid titleBar;

	internal Label titleLabel;

	internal Grid dragPanel;

	internal StackPanel quitPanel;

	internal Image quitImage;

	internal RichTextBox richTextBox;

	private bool _contentLoaded;

	public string SBUrl { get; set; }

	public bool DontShowMainWindow { get; set; }

	public NotesWindow()
	{
		InitializeComponent();
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
			_canClose = false;
			if (MainWindow == null)
			{
				new MainWindow().Show();
			}
			Close();
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
			Clipboard.SetText(SBUrl);
		}
		catch
		{
		}
	}

	private void Button_Click_1(object sender, RoutedEventArgs e)
	{
		try
		{
			if (MainWindow == null && !DontShowMainWindow)
			{
				MainWindow = new MainWindow();
			}
			MainWindow.Show();
			Close();
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
			Uri resourceLocator = new Uri("/SilverBullet;component/noteswindow.xaml", UriKind.Relative);
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
			Root = (Grid)target;
			break;
		case 2:
			titleBar = (Grid)target;
			break;
		case 3:
			titleLabel = (Label)target;
			titleLabel.MouseLeftButtonDown += titleLabel_MouseLeftButtonDown;
			break;
		case 4:
			dragPanel = (Grid)target;
			dragPanel.MouseDown += dragPanel_MouseDown;
			break;
		case 5:
			quitPanel = (StackPanel)target;
			quitPanel.MouseDown += quitPanel_MouseDown;
			quitPanel.MouseEnter += quitPanel_MouseEnter;
			quitPanel.MouseLeave += quitPanel_MouseLeave;
			quitPanel.MouseLeftButtonUp += quitPanel_MouseLeftButtonUp;
			break;
		case 6:
			quitImage = (Image)target;
			break;
		case 7:
			richTextBox = (RichTextBox)target;
			break;
		case 8:
			((Button)target).Click += Button_Click;
			break;
		case 9:
			((Button)target).Click += Button_Click_1;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
