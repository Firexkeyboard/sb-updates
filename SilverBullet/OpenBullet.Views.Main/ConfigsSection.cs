using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using OpenBullet.Views.Main.Configs;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main;

public class ConfigsSection : Page, IComponentConnector
{
	public ConfigManager ConfigManagerPage;

	public Stacker StackerPage;

	public ConfigOtherOptions OtherOptionsPage;

	public ConfigOcrSettings ConfigOcrSettings;

	internal StackPanel topMenu;

	internal Label menuOptionManager;

	internal Label menuOptionStacker;

	internal Label menuOptionOtherOptions;

	internal Label menuOptionOCRSettings;

	internal Frame Main;

	private bool _contentLoaded;

	public ConfigViewModel CurrentConfig => SB.ConfigManager.CurrentConfig;

	public ConfigsSection()
	{
		InitializeComponent();
		ConfigManagerPage = new ConfigManager();
		SB.Logger.LogInfo(Components.ConfigManager, "Initialized Manager Page");
		menuOptionManager_MouseDown(this, null);
	}

	private void menuOptionManager_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = ConfigManagerPage;
		menuOptionSelected(menuOptionManager);
	}

	public void menuOptionStacker_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (CurrentConfig != null && StackerPage != null)
		{
			Main.Content = StackerPage;
			menuOptionSelected(menuOptionStacker);
		}
		else
		{
			SB.Logger.LogError(Components.ConfigManager, "Cannot switch to stacker since no config is loaded or the loaded config isn't public");
		}
	}

	private void menuOptionOtherOptions_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (CurrentConfig != null)
		{
			if (OtherOptionsPage == null)
			{
				OtherOptionsPage = new ConfigOtherOptions();
			}
			Main.Content = OtherOptionsPage;
			menuOptionSelected(menuOptionOtherOptions);
		}
		else
		{
			SB.Logger.LogError(Components.ConfigManager, "Cannot switch to other options since no config is loaded");
		}
	}

	private void menuOptionOCRSettings_MouseDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (CurrentConfig != null)
			{
				if (ConfigOcrSettings == null)
				{
					ConfigOcrSettings = new ConfigOcrSettings();
				}
				Main.Content = ConfigOcrSettings;
				menuOptionSelected(menuOptionOCRSettings);
			}
			else
			{
				SB.Logger.LogError(Components.ConfigManager, "Cannot switch to other options since no config is loaded");
			}
		}
		catch
		{
		}
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

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/configssection.xaml", UriKind.Relative);
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
			menuOptionManager = (Label)target;
			menuOptionManager.MouseDown += menuOptionManager_MouseDown;
			break;
		case 3:
			menuOptionStacker = (Label)target;
			menuOptionStacker.MouseDown += menuOptionStacker_MouseDown;
			break;
		case 4:
			menuOptionOtherOptions = (Label)target;
			menuOptionOtherOptions.MouseDown += menuOptionOtherOptions_MouseDown;
			break;
		case 5:
			menuOptionOCRSettings = (Label)target;
			menuOptionOCRSettings.MouseDown += menuOptionOCRSettings_MouseDown;
			break;
		case 6:
			Main = (Frame)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
