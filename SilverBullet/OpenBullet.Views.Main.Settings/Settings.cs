using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using MahApps.Metro.IconPacks;
using OpenBullet.Views.Main;
using RuriLib;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main.Settings;

public class Settings : Page, IComponentConnector
{
	private OBSettings OBSettingsPage = new OBSettings();

	private RLSettings RLSettingsPage = new RLSettings();

	private PluginsSection _pluginsPage;

	private Label _menuOptionPlugins;

	internal StackPanel topMenu;

	internal Label menuOptionRuriLib;

	internal Label menuOptionOpenBullet;

	internal Frame Main;

	private bool _contentLoaded;

	public Settings()
	{
		InitializeComponent();
		menuOptionRuriLib_MouseDown(this, null);
	}

	private void menuOptionRuriLib_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = RLSettingsPage;
		menuOptionSelected(menuOptionRuriLib);
	}

	private void menuOptionOpenBullet_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = OBSettingsPage;
		menuOptionSelected(menuOptionOpenBullet);
	}

	private void menuOptionSelected(object sender)
	{
		foreach (object child in topMenu.Children)
		{
			try
			{
				((Label)child).Foreground = Utils.GetBrush("ForegroundMain");
			}
			catch
			{
			}
		}
		((Label)sender).Foreground = Utils.GetBrush("ForegroundCustom");
	}

	public void SetPluginsPage(PluginsSection page)
	{
		_pluginsPage = page;

		_menuOptionPlugins = new Label { Style = menuOptionRuriLib.Style };

		var sp = new StackPanel { Orientation = Orientation.Horizontal };
		sp.Children.Add(new PackIconMaterial
		{
			Kind = PackIconMaterialKind.Puzzle,
			Width = 15,
			Height = 15,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0, 0, 4, 0)
		});
		sp.Children.Add(new TextBlock
		{
			Text = "Plugins",
			VerticalAlignment = VerticalAlignment.Center
		});
		_menuOptionPlugins.Content = sp;
		_menuOptionPlugins.MouseDown += MenuOptionPlugins_MouseDown;
		topMenu.Children.Add(_menuOptionPlugins);
	}

	private void MenuOptionPlugins_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (_pluginsPage == null) return;
		Main.Content = _pluginsPage;
		menuOptionSelected(_menuOptionPlugins);
	}

	private void saveButton_Click(object sender, RoutedEventArgs e)
	{
		IOManager.SaveSettings<RLSettingsViewModel>(SB.rlSettingsFile, SB.Settings.RLSettings);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/settings.xaml", UriKind.Relative);
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
			menuOptionRuriLib = (Label)target;
			menuOptionRuriLib.MouseDown += menuOptionRuriLib_MouseDown;
			break;
		case 3:
			menuOptionOpenBullet = (Label)target;
			menuOptionOpenBullet.MouseDown += menuOptionOpenBullet_MouseDown;
			break;
		case 4:
			Main = (Frame)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
