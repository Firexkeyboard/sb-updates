using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using OpenBullet.Views.UserControls;

namespace OpenBullet.Views.Main;

public class PluginsSection : Page, IComponentConnector
{
	private List<PluginControl> controls;

	internal StackPanel topMenu;

	internal ComboBox pluginSelector;

	internal Frame Main;

	private bool _contentLoaded;

	public PluginsSection(IEnumerable<PluginControl> controls)
	{
		InitializeComponent();
		this.controls = controls.ToList();
		foreach (PluginControl control in this.controls)
		{
			pluginSelector.Items.Add(control.Plugin.Name);
		}
		if (this.controls.Count > 0)
		{
			pluginSelector.SelectedIndex = 0;
		}
	}

	private void pluginSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		Main.Content = controls.First((PluginControl c) => c.Plugin.Name == (string)pluginSelector.SelectedValue);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/pluginssection.xaml", UriKind.Relative);
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
			topMenu = (StackPanel)target;
			break;
		case 2:
			pluginSelector = (ComboBox)target;
			pluginSelector.SelectionChanged += pluginSelector_SelectionChanged;
			break;
		case 3:
			Main = (Frame)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
