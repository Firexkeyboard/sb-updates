using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using OpenBullet.Views.Main.Settings.RL;
using RuriLib;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main.Settings;

public class RLSettings : Page, IComponentConnector
{
	private General GeneralPage = new General();

	private Proxies ProxiesPage = new Proxies();

	private Captchas CaptchasPage = new Captchas();

	private Selenium SeleniumPage = new Selenium();

	private Ocr OcrPage = new Ocr();

	private CefSharp cefSharpPage = new CefSharp();

	internal StackPanel topMenu;

	internal Label menuOptionGeneral;

	internal Label menuOptionProxies;

	internal Label menuOptionCaptchas;

	internal Label menuOptionSelenium;

	internal Label menuOptionOcr;

	internal Label menuOptionCefSharpBrw;

	internal Frame Main;

	internal Button saveButton;

	internal Button resetButton;

	private bool _contentLoaded;

	public RLSettings()
	{
		InitializeComponent();
		menuOptionGeneral_MouseDown(this, null);
	}

	private void menuOptionGeneral_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = GeneralPage;
		menuOptionSelected(menuOptionGeneral);
	}

	private void menuOptionProxies_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = ProxiesPage;
		menuOptionSelected(menuOptionProxies);
	}

	private void menuOptionCaptchas_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = CaptchasPage;
		menuOptionSelected(menuOptionCaptchas);
	}

	private void menuOptionSelenium_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = SeleniumPage;
		menuOptionSelected(menuOptionSelenium);
	}

	private void menuOptionOcr_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = OcrPage;
		menuOptionSelected(menuOptionOcr);
	}

	private void menuOptionCefSharpBrw_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = cefSharpPage;
		menuOptionSelected(sender);
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
		IOManager.SaveSettings<RLSettingsViewModel>(SB.rlSettingsFile, SB.Settings.RLSettings);
	}

	private void resetButton_Click(object sender, RoutedEventArgs e)
	{
		if (MessageBox.Show("Are you sure you want to reset all your RuriLib settings?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
		{
			SB.Settings.RLSettings.Reset();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/settings/rlsettings.xaml", UriKind.Relative);
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
			menuOptionProxies = (Label)target;
			menuOptionProxies.MouseDown += menuOptionProxies_MouseDown;
			break;
		case 4:
			menuOptionCaptchas = (Label)target;
			menuOptionCaptchas.MouseDown += menuOptionCaptchas_MouseDown;
			break;
		case 5:
			menuOptionSelenium = (Label)target;
			menuOptionSelenium.MouseDown += menuOptionSelenium_MouseDown;
			break;
		case 6:
			menuOptionOcr = (Label)target;
			menuOptionOcr.MouseDown += menuOptionOcr_MouseDown;
			break;
		case 7:
			menuOptionCefSharpBrw = (Label)target;
			menuOptionCefSharpBrw.MouseDown += menuOptionCefSharpBrw_MouseDown;
			break;
		case 8:
			Main = (Frame)target;
			break;
		case 9:
			saveButton = (Button)target;
			saveButton.Click += saveButton_Click;
			break;
		case 10:
			resetButton = (Button)target;
			resetButton.Click += resetButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
