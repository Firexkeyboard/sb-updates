using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using OpenBullet.Views.Main.Settings.OpenBullet;

namespace OpenBullet.Views.Main.Settings;

public class OBSettings : Page, IComponentConnector
{
	private General GeneralPage = new General();

	private Sounds SoundsPage = new Sounds();

	private Sources SourcesPage = new Sources();

	private Themes ThemesPage = new Themes();

	internal StackPanel topMenu;

	internal Label menuOptionGeneral;

	internal Label menuOptionSounds;

	internal Label menuOptionSources;

	internal Label menuOptionThemes;

	internal Frame Main;

	internal Button saveButton;

	internal Button resetButton;

	private bool _contentLoaded;

	public OBSettings()
	{
		InitializeComponent();
		menuOptionGeneral_MouseDown(this, null);
	}

	private void menuOptionGeneral_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = GeneralPage;
		menuOptionSelected(menuOptionGeneral);
	}

	private void menuOptionSounds_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = SoundsPage;
		menuOptionSelected(menuOptionSounds);
	}

	private void menuOptionSources_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = SourcesPage;
		menuOptionSelected(menuOptionSources);
	}

	private void menuOptionThemes_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = ThemesPage;
		menuOptionSelected(menuOptionThemes);
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
		((Label)sender).Foreground = Utils.GetBrush("ForegroundGood");
	}

	private void saveButton_Click(object sender, RoutedEventArgs e)
	{
		SBIOManager.SaveSettings(SB.obSettingsFile, SB.SBSettings);
	}

	private void resetButton_Click(object sender, RoutedEventArgs e)
	{
		if (MessageBox.Show("Are you sure you want to reset all your OpenBullet settings?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
		{
			SB.SBSettings.Reset();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/settings/obsettings.xaml", UriKind.Relative);
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
			menuOptionGeneral = (Label)target;
			menuOptionGeneral.MouseDown += menuOptionGeneral_MouseDown;
			break;
		case 3:
			menuOptionSounds = (Label)target;
			menuOptionSounds.MouseDown += menuOptionSounds_MouseDown;
			break;
		case 4:
			menuOptionSources = (Label)target;
			menuOptionSources.MouseDown += menuOptionSources_MouseDown;
			break;
		case 5:
			menuOptionThemes = (Label)target;
			menuOptionThemes.MouseDown += menuOptionThemes_MouseDown;
			break;
		case 6:
			Main = (Frame)target;
			break;
		case 7:
			saveButton = (Button)target;
			saveButton.Click += saveButton_Click;
			break;
		case 8:
			resetButton = (Button)target;
			resetButton.Click += resetButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
