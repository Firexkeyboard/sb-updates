using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main.Settings.RL;

public class General : Page, IComponentConnector
{
	internal ComboBox botsDisplayModeCombobox;

	private bool _contentLoaded;

	public General()
	{
		InitializeComponent();
		base.DataContext = SB.Settings.RLSettings.General;
		string[] names = Enum.GetNames(typeof(BotsDisplayMode));
		foreach (string newItem in names)
		{
			botsDisplayModeCombobox.Items.Add(newItem);
		}
		botsDisplayModeCombobox.SelectedIndex = (int)SB.Settings.RLSettings.General.BotsDisplayMode;
	}

	private void botsDisplayModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		SB.Settings.RLSettings.General.BotsDisplayMode = (BotsDisplayMode)botsDisplayModeCombobox.SelectedIndex;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/settings/rurilib/general.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			botsDisplayModeCombobox = (ComboBox)target;
			botsDisplayModeCombobox.SelectionChanged += botsDisplayModeCombobox_SelectionChanged;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
