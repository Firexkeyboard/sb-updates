using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace OpenBullet;

public class SplashWindow : Window, IComponentConnector
{
	internal Image quitImage;

	internal Button agreeButton;

	private bool _contentLoaded;

	public SplashWindow()
	{
		InitializeComponent();
	}

	private void agreeButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void WindowMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left)
		{
			DragMove();
		}
	}

	private void quitImage_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Environment.Exit(0);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/splashwindow.xaml", UriKind.Relative);
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
			((SplashWindow)target).MouseDown += WindowMouseDown;
			break;
		case 2:
			quitImage = (Image)target;
			quitImage.MouseDown += quitImage_MouseDown;
			break;
		case 3:
			agreeButton = (Button)target;
			agreeButton.Click += agreeButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
