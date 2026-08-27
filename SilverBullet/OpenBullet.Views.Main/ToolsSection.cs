using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using OpenBullet.Views.Main.Tools;

namespace OpenBullet.Views.Main;

public class ToolsSection : Page, IComponentConnector
{
	private ListGenerator ListGenerator;

	private SeleniumTools SeleniumTools;

	private TessDataDownloads tessDataDL;

	private WordlistTools wordlistTools;

	internal StackPanel topMenu;

	internal Label menuOptionListGenerator;

	internal Label menuOptionSeleniumTools;

	internal Label menuOptionWordlist;

	internal Label menuTessData;

	internal Frame Main;

	private bool _contentLoaded;

	public ToolsSection()
	{
		InitializeComponent();
		ListGenerator = new ListGenerator();
		SeleniumTools = new SeleniumTools();
		wordlistTools = new WordlistTools();
		tessDataDL = new TessDataDownloads();

		// Move Wordlist Tools to first position in the tab bar
		if (topMenu != null && menuOptionWordlist != null)
		{
			topMenu.Children.Remove(menuOptionWordlist);
			topMenu.Children.Insert(0, menuOptionWordlist);
		}

		menuOptionWordlist_MouseDown(menuOptionWordlist, null);
	}

	private void menuOptionListGenerator_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = ListGenerator;
		menuOptionSelected(menuOptionListGenerator);
	}

	private void menuOptionSeleniumUtilities_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = SeleniumTools;
		menuOptionSelected(menuOptionSeleniumTools);
	}

	private void menuTessData_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = tessDataDL;
		menuOptionSelected(menuTessData);
	}

	private void menuOptionWordlist_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = wordlistTools;
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
		((Label)sender).Foreground = Utils.GetBrush("ForegroundCustom");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/toolssection.xaml", UriKind.Relative);
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
			menuOptionListGenerator = (Label)target;
			menuOptionListGenerator.MouseDown += menuOptionListGenerator_MouseDown;
			break;
		case 3:
			menuOptionSeleniumTools = (Label)target;
			menuOptionSeleniumTools.MouseDown += menuOptionSeleniumUtilities_MouseDown;
			break;
		case 4:
			menuOptionWordlist = (Label)target;
			menuOptionWordlist.MouseDown += menuOptionWordlist_MouseDown;
			break;
		case 5:
			menuTessData = (Label)target;
			menuTessData.MouseDown += menuTessData_MouseDown;
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
