using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using RuriLib.Models;
using RuriLib.Runner;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main.Settings.RL;

public class Proxies : Page, IComponentConnector, IStyleConnector
{
	private SettingsProxies vm;

	private Random rand = new Random();

	internal RichTextBox globalBanKeysTextbox;

	internal RichTextBox globalRetryKeysTextbox;

	internal ComboBox reloadSourceCombobox;

	internal Button addRemoteProxySourceButton;

	internal Button clearRemoteProxySourcesButton;

	internal Button testRemoteProxySourcesButton;

	internal TabControl reloadTabControl;

	internal TabItem emptyTab;

	internal TabItem fileTab;

	internal ComboBox reloadTypeCombobox;

	internal TabItem remoteTab;

	internal ItemsControl remoteProxySourcesControl;

	private bool _contentLoaded;

	public Proxies()
	{
		vm = SB.Settings.RLSettings.Proxies;
		base.DataContext = vm;
		InitializeComponent();
		string[] names = Enum.GetNames(typeof(ProxyType));
		foreach (string text in names)
		{
			if (text != "Chain")
			{
				reloadTypeCombobox.Items.Add(text);
			}
		}
		reloadTypeCombobox.SelectedIndex = (int)SB.Settings.RLSettings.Proxies.ReloadType;
		names = Enum.GetNames(typeof(ProxyReloadSource));
		foreach (string newItem in names)
		{
			reloadSourceCombobox.Items.Add(newItem);
		}
		reloadSourceCombobox.SelectedIndex = (int)SB.Settings.RLSettings.Proxies.ReloadSource;
		globalBanKeysTextbox.AppendText(string.Join(Environment.NewLine, SB.Settings.RLSettings.Proxies.GlobalBanKeys), Colors.White);
		globalRetryKeysTextbox.AppendText(string.Join(Environment.NewLine, SB.Settings.RLSettings.Proxies.GlobalRetryKeys), Colors.White);
	}

	private void globalBanKeysTextbox_TextChanged(object sender, TextChangedEventArgs e)
	{
		SB.Settings.RLSettings.Proxies.GlobalBanKeys = globalBanKeysTextbox.Lines();
	}

	private void globalRetryKeysTextbox_TextChanged(object sender, TextChangedEventArgs e)
	{
		SB.Settings.RLSettings.Proxies.GlobalRetryKeys = globalRetryKeysTextbox.Lines();
	}

	private void reloadSourceCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		SB.Settings.RLSettings.Proxies.ReloadSource = (ProxyReloadSource)reloadSourceCombobox.SelectedIndex;
		ProxyReloadSource reloadSource = SB.Settings.RLSettings.Proxies.ReloadSource;
		switch ((int)reloadSource)
		{
		case 0:
			reloadTabControl.SelectedIndex = 0;
			addRemoteProxySourceButton.Visibility = Visibility.Collapsed;
			clearRemoteProxySourcesButton.Visibility = Visibility.Collapsed;
			testRemoteProxySourcesButton.Visibility = Visibility.Collapsed;
			break;
		case 1:
			reloadTabControl.SelectedIndex = 1;
			addRemoteProxySourceButton.Visibility = Visibility.Collapsed;
			clearRemoteProxySourcesButton.Visibility = Visibility.Collapsed;
			testRemoteProxySourcesButton.Visibility = Visibility.Collapsed;
			break;
		case 2:
			reloadTabControl.SelectedIndex = 2;
			addRemoteProxySourceButton.Visibility = Visibility.Visible;
			clearRemoteProxySourcesButton.Visibility = Visibility.Visible;
			testRemoteProxySourcesButton.Visibility = Visibility.Visible;
			break;
		}
	}

	private void reloadTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		SB.Settings.RLSettings.Proxies.ReloadType = (ProxyType)reloadTypeCombobox.SelectedIndex;
	}

	private void remoteProxyTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.GetRemoteProxySourceById((int)(sender as ComboBox).Tag).Type = (ProxyType)(sender as ComboBox).SelectedIndex;
	}

	private void remoteProxyTypeCombobox_Loaded(object sender, RoutedEventArgs e)
	{
		RemoteProxySource remoteProxySourceById = vm.GetRemoteProxySourceById((int)(sender as ComboBox).Tag);
		if (remoteProxySourceById.TypeInitialized)
		{
			return;
		}
		remoteProxySourceById.TypeInitialized = true;
		string[] names = Enum.GetNames(typeof(ProxyType));
		foreach (string text in names)
		{
			if (text != "Chain")
			{
				(sender as ComboBox).Items.Add(text);
			}
		}
		(sender as ComboBox).SelectedIndex = (int)remoteProxySourceById.Type;
	}

	private void removeRemoteProxySourceButton_Click(object sender, RoutedEventArgs e)
	{
		vm.RemoveRemoteProxySourceById((int)(sender as Button).Tag);
	}

	private void addRemoteProxySourceButton_Click(object sender, RoutedEventArgs e)
	{
		vm.RemoteProxySources.Add(new RemoteProxySource(rand.Next()));
	}

	private void clearRemoteProxySourcesButton_Click(object sender, RoutedEventArgs e)
	{
		if (MessageBox.Show("Are you sure?", "Warning", MessageBoxButton.YesNo) != MessageBoxResult.No)
		{
			vm.RemoteProxySources.Clear();
		}
	}

	private async void TestRemoteProxySourcesButton_Click(object sender, RoutedEventArgs e)
	{
		List<string> prompt = new List<string> { "Results:" };
		RemoteProxySourceResult[] array = await Task.WhenAll((from s in vm.RemoteProxySources
			where s.Active
			select RunnerViewModel.GetProxiesFromRemoteSourceAsync(s.Url, s.Type, s.Pattern, s.Output)).ToList());
		foreach (RemoteProxySourceResult val in array)
		{
			if (val.Successful)
			{
				string arg = "NONE";
				if (val.Proxies.Count > 0)
				{
					arg = val.Proxies.First().Proxy;
				}
				prompt.Add($"[SUCCESS] {val.Url} - Got {val.Proxies.Count} proxies (first: {arg})");
			}
			else
			{
				prompt.Add("[FAILURE] " + val.Url + " - " + val.Error);
			}
		}
		MessageBox.Show(string.Join(Environment.NewLine, prompt));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/settings/rurilib/proxies.xaml", UriKind.Relative);
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
			globalBanKeysTextbox = (RichTextBox)target;
			globalBanKeysTextbox.TextChanged += globalBanKeysTextbox_TextChanged;
			break;
		case 2:
			globalRetryKeysTextbox = (RichTextBox)target;
			globalRetryKeysTextbox.TextChanged += globalRetryKeysTextbox_TextChanged;
			break;
		case 3:
			reloadSourceCombobox = (ComboBox)target;
			reloadSourceCombobox.SelectionChanged += reloadSourceCombobox_SelectionChanged;
			break;
		case 4:
			addRemoteProxySourceButton = (Button)target;
			addRemoteProxySourceButton.Click += addRemoteProxySourceButton_Click;
			break;
		case 5:
			clearRemoteProxySourcesButton = (Button)target;
			clearRemoteProxySourcesButton.Click += clearRemoteProxySourcesButton_Click;
			break;
		case 6:
			testRemoteProxySourcesButton = (Button)target;
			testRemoteProxySourcesButton.Click += TestRemoteProxySourcesButton_Click;
			break;
		case 7:
			reloadTabControl = (TabControl)target;
			break;
		case 8:
			emptyTab = (TabItem)target;
			break;
		case 9:
			fileTab = (TabItem)target;
			break;
		case 10:
			reloadTypeCombobox = (ComboBox)target;
			reloadTypeCombobox.SelectionChanged += reloadTypeCombobox_SelectionChanged;
			break;
		case 11:
			remoteTab = (TabItem)target;
			break;
		case 12:
			remoteProxySourcesControl = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 13:
			((ComboBox)target).Loaded += remoteProxyTypeCombobox_Loaded;
			((ComboBox)target).SelectionChanged += remoteProxyTypeCombobox_SelectionChanged;
			break;
		case 14:
			((Button)target).Click += removeRemoteProxySourceButton_Click;
			break;
		}
	}
}
