using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main.Settings.RL;

public class Selenium : Page, IComponentConnector
{
	internal ComboBox browserTypeCombobox;

	internal TextBox extensionsBox;

	private bool _contentLoaded;

	public Selenium()
	{
		InitializeComponent();
		base.DataContext = SB.Settings.RLSettings.Selenium;
		string[] names = Enum.GetNames(typeof(BrowserType));
		foreach (string newItem in names)
		{
			browserTypeCombobox.Items.Add(newItem);
		}
		browserTypeCombobox.SelectedIndex = (int)SB.Settings.RLSettings.Selenium.Browser;
		foreach (string chromeExtension in SB.Settings.RLSettings.Selenium.ChromeExtensions)
		{
			extensionsBox.AppendText(chromeExtension + Environment.NewLine);
		}
	}

	private void browserTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		SB.Settings.RLSettings.Selenium.Browser = (BrowserType)browserTypeCombobox.SelectedIndex;
	}

	private void extensionsBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		SB.Settings.RLSettings.Selenium.ChromeExtensions = extensionsBox.Text.Split(new string[1] { Environment.NewLine }, StringSplitOptions.None).ToList();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/settings/rurilib/selenium.xaml", UriKind.Relative);
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
			browserTypeCombobox = (ComboBox)target;
			browserTypeCombobox.SelectionChanged += browserTypeCombobox_SelectionChanged;
			break;
		case 2:
			extensionsBox = (TextBox)target;
			extensionsBox.TextChanged += extensionsBox_TextChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
