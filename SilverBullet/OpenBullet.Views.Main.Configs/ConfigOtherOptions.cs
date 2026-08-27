using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using OpenBullet.Views.Main.Configs.OtherOptions;

namespace OpenBullet.Views.Main.Configs;

public class ConfigOtherOptions : Page, IComponentConnector
{
	public General GeneralPage = new General();

	public Requests RequestsPage = new Requests();

	public Proxies ProxiesPage = new Proxies();

	public Inputs InputsPage = new Inputs();

	public Data DataPage = new Data();

	public Selenium SeleniumPage = new Selenium();

	public Compile CompilePage = new Compile();

	internal StackPanel topMenu;

	internal Label menuOptionGeneral;

	internal Label menuOptionRequests;

	internal Label menuOptionProxies;

	internal Label menuOptionInput;

	internal Label menuOptionData;

	internal Label menuOptionSelenium;

	internal Label menuOptionBuildExe;

	internal Frame Main;

	private bool _contentLoaded;

	public ConfigOtherOptions()
	{
		InitializeComponent();
		menuOptionGeneral_MouseDown(this, null);
	}

	private void menuOptionGeneral_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = GeneralPage;
		menuOptionSelected(menuOptionGeneral);
	}

	private void menuOptionRequests_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = RequestsPage;
		menuOptionSelected(menuOptionRequests);
	}

	private void menuOptionProxies_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = ProxiesPage;
		menuOptionSelected(menuOptionProxies);
	}

	private void menuOptionInput_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = InputsPage;
		menuOptionSelected(menuOptionInput);
	}

	private void menuOptionData_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = DataPage;
		menuOptionSelected(menuOptionData);
	}

	private void menuOptionSelenium_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = SeleniumPage;
		menuOptionSelected(menuOptionSelenium);
	}

	private void menuOptionCompile_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = CompilePage;
		menuOptionSelected(sender);
	}

	private void menuOptionSelected(object sender)
	{
		foreach (Label child in topMenu.Children)
		{
			try
			{
				child.Foreground = Utils.GetBrush("ForegroundMain");
			}
			catch
			{
			}
		}
		((Label)sender).Foreground = Utils.GetBrush("ForegroundGood");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/configs/configotheroptions.xaml", UriKind.Relative);
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
			menuOptionRequests = (Label)target;
			menuOptionRequests.MouseDown += menuOptionRequests_MouseDown;
			break;
		case 4:
			menuOptionProxies = (Label)target;
			menuOptionProxies.MouseDown += menuOptionProxies_MouseDown;
			break;
		case 5:
			menuOptionInput = (Label)target;
			menuOptionInput.MouseDown += menuOptionInput_MouseDown;
			break;
		case 6:
			menuOptionData = (Label)target;
			menuOptionData.MouseDown += menuOptionData_MouseDown;
			break;
		case 7:
			menuOptionSelenium = (Label)target;
			menuOptionSelenium.MouseDown += menuOptionSelenium_MouseDown;
			break;
		case 8:
			menuOptionBuildExe = (Label)target;
			menuOptionBuildExe.MouseDown += menuOptionCompile_MouseDown;
			break;
		case 9:
			Main = (Frame)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
