using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using MahApps.Metro.IconPacks;

namespace OpenBullet.Views.CustomMessageBox;

public class CustomMsgBoxWindow : Window, IDisposable, IComponentConnector
{
	internal Grid TitleBar;

	internal new Path Icon;

	internal Label TitleLabel;

	internal Grid dragPanel;

	internal Button btnQuit;

	internal TextBlock Message;

	internal PackIconMaterial MsgIcon;

	internal Button BtnOk;

	internal Button BtnCancel;

	private bool _contentLoaded;

	public MessageBoxResult Result { get; set; }

	public CustomMsgBoxWindow()
	{
		InitializeComponent();
		Result = MessageBoxResult.Cancel;
	}

	private void BtnOk_Click(object sender, RoutedEventArgs e)
	{
		Result = MessageBoxResult.OK;
		Close();
	}

	private void BtnCancel_Click(object sender, RoutedEventArgs e)
	{
		Result = MessageBoxResult.Cancel;
		Close();
	}

	public void Dispose()
	{
		Close();
	}

	private void BtnCopyMessage_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(Message.Text);
		}
		catch
		{
		}
	}

	private void titleBar_MouseDown(object sender, MouseButtonEventArgs e)
	{
	}

	private void dragPanel_MouseDown(object sender, MouseButtonEventArgs e)
	{
		DragMove();
	}

	private void btnQuit_Click(object sender, RoutedEventArgs e)
	{
		Dispose();
	}

	private void btnQuit_MouseEnter(object sender, MouseEventArgs e)
	{
		Button button = btnQuit;
		Brush borderBrush = (btnQuit.Background = Brushes.DarkRed);
		button.BorderBrush = borderBrush;
	}

	private void btnQuit_MouseLeave(object sender, MouseEventArgs e)
	{
		Button button = btnQuit;
		Brush borderBrush = (btnQuit.Background = Brushes.Transparent);
		button.BorderBrush = borderBrush;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/custommessagebox/custommsgboxwindow.xaml", UriKind.Relative);
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
			TitleBar = (Grid)target;
			TitleBar.MouseDown += titleBar_MouseDown;
			break;
		case 2:
			Icon = (Path)target;
			break;
		case 3:
			TitleLabel = (Label)target;
			break;
		case 4:
			dragPanel = (Grid)target;
			dragPanel.MouseDown += dragPanel_MouseDown;
			break;
		case 5:
			btnQuit = (Button)target;
			btnQuit.Click += btnQuit_Click;
			btnQuit.MouseEnter += btnQuit_MouseEnter;
			btnQuit.MouseLeave += btnQuit_MouseLeave;
			break;
		case 6:
			Message = (TextBlock)target;
			break;
		case 7:
			MsgIcon = (PackIconMaterial)target;
			break;
		case 8:
			BtnOk = (Button)target;
			BtnOk.Click += BtnOk_Click;
			break;
		case 9:
			BtnCancel = (Button)target;
			BtnCancel.Click += BtnCancel_Click;
			break;
		case 10:
			((Button)target).Click += BtnCopyMessage_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
