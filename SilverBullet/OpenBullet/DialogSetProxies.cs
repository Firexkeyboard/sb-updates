using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib.Runner;

namespace OpenBullet;

public class DialogSetProxies : Page, IComponentConnector
{
	internal RadioButton proxiesDefaultRadio;

	internal RadioButton proxiesOnRadio;

	internal RadioButton proxiesOffRadio;

	internal Button selectButton;

	private bool _contentLoaded;

	public object Caller { get; set; }

	public DialogSetProxies(object caller)
	{
		InitializeComponent();
		Caller = caller;
		proxiesDefaultRadio.IsChecked = true;
	}

	private void selectButton_Click(object sender, RoutedEventArgs e)
	{
		ProxyMode proxyMode = (ProxyMode)0;
		if (proxiesDefaultRadio.IsChecked.Value)
		{
			proxyMode = (ProxyMode)0;
		}
		else if (proxiesOnRadio.IsChecked.Value)
		{
			proxyMode = (ProxyMode)1;
		}
		else if (proxiesOffRadio.IsChecked.Value)
		{
			proxyMode = (ProxyMode)2;
		}
		if (Caller.GetType() == typeof(RunnerViewModel))
		{
			object caller = Caller;
			((RunnerViewModel)((caller is RunnerViewModel) ? caller : null)).ProxyMode = proxyMode;
		}
		((MainDialog)base.Parent).Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialogsetproxies.xaml", UriKind.Relative);
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
			proxiesDefaultRadio = (RadioButton)target;
			break;
		case 2:
			proxiesOnRadio = (RadioButton)target;
			break;
		case 3:
			proxiesOffRadio = (RadioButton)target;
			break;
		case 4:
			selectButton = (Button)target;
			selectButton.Click += selectButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
