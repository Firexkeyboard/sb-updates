using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageSBlockBrowserAction : Page, IComponentConnector
{
	private SBlockBrowserAction vm;

	public Dictionary<string, string> infoDic = new Dictionary<string, string>
	{
		{ "Open", "Opens the browser assigned to the current bot. This will be disregarded if the browser is already opened." },
		{ "Close", "Closes the current tab without disposing of the webdriver (not recommended)." },
		{ "Quit", "Quits all the tabs and windows and disposes of the webdriver (recommended)." },
		{ "SendKeys", "Sends the keys directly to the browser as if you were typing them on your keyboard. You can use variables and <TAB> <ENTER> and <BACKSPACE> separated by || e.g. <USER>||<TAB>||<PASS>||<TAB>||<ENTER>. If you need other keys look here https://github.com/SeleniumHQ/selenium/blob/master/dotnet/src/webdriver/Keys.cs" },
		{ "MoveMouse", "Not Implemented Yet" },
		{ "Screenshot", "Takes a screenshot of the visible part of the page." },
		{ "Scroll", "Scrolls down by the specified amount in the input field." },
		{ "SwitchToTab", "Switches to the tab with the index in the input field." },
		{ "DOMtoSOURCE", "Puts the full DOM into the <SOURCE> fixed variable." },
		{ "GetCookies", "Sends all cookies from selenium to the cookie jar used in normal requests." },
		{ "SetCookies", "Sets all cookies from the cookie jar to selenium. You need to put the domain of the website in the input field (e.g. example.com)." }
	};

	internal ComboBox actionCombobox;

	internal DockPanel panelSUserAgent;

	internal TextBlock functionInfoTextblock;

	private bool _contentLoaded;

	public PageSBlockBrowserAction(SBlockBrowserAction block)
	{
		InitializeComponent();
		vm = block;
		base.DataContext = vm;
		string[] names = Enum.GetNames(typeof(BrowserAction));
		foreach (string newItem in names)
		{
			actionCombobox.Items.Add(newItem);
		}
		actionCombobox.SelectedIndex = (int)vm.Action;
	}

	private void actionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.Action = (BrowserAction)((ComboBox)e.OriginalSource).SelectedIndex;
		if ((int)vm.Action == 0)
		{
			panelSUserAgent.Visibility = Visibility.Visible;
		}
		else
		{
			panelSUserAgent.Visibility = Visibility.Collapsed;
		}
		try
		{
			TextBlock textBlock = functionInfoTextblock;
			Dictionary<string, string> dictionary = infoDic;
			BrowserAction action = vm.Action;
			textBlock.Text = dictionary[action.ToString()];
		}
		catch
		{
			functionInfoTextblock.Text = "No additional information available for this function";
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pagesblockbrowseraction.xaml", UriKind.Relative);
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
			actionCombobox = (ComboBox)target;
			actionCombobox.SelectionChanged += actionCombobox_SelectionChanged;
			break;
		case 2:
			panelSUserAgent = (DockPanel)target;
			break;
		case 3:
			functionInfoTextblock = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
