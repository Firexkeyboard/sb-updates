using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using OpenBullet.Plugins;
using RuriLib.ViewModels;

namespace OpenBullet.Views.UserControls;

public class UserControlConfig : UserControl, IControl, IComponentConnector
{
	internal TextBox ConfigName;

	internal Button Choose;

	private bool _contentLoaded;

	public ConfigViewModel Config { get; set; }

	public UserControlConfig()
	{
		InitializeComponent();
		base.DataContext = this;
	}

	public dynamic GetValue()
	{
		return Config;
	}

	public void SetValue(dynamic value)
	{
		Config = (ConfigViewModel)value;
	}

	private void Choose_Click(object sender, RoutedEventArgs e)
	{
		new MainDialog(new DialogSelectConfig(this), "Select a Config").ShowDialog();
		if (Config != null)
		{
			ConfigName.Text = Config.Name;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/usercontrolconfig.xaml", UriKind.Relative);
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
			ConfigName = (TextBox)target;
			break;
		case 2:
			Choose = (Button)target;
			Choose.Click += Choose_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
