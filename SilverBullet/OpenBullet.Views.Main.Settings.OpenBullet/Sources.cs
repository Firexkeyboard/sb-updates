using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using OpenBullet.Models;
using OpenBullet.ViewModels;

namespace OpenBullet.Views.Main.Settings.OpenBullet;

public class Sources : Page, IComponentConnector, IStyleConnector
{
	private OBSettingsSources vm;

	private Random rand = new Random();

	internal Button addSourceButton;

	internal Button clearSourcesButton;

	internal ItemsControl sourcesControl;

	private bool _contentLoaded;

	public Sources()
	{
		vm = SB.SBSettings.Sources;
		base.DataContext = vm;
		InitializeComponent();
	}

	private void authTypeCombobox_Loaded(object sender, RoutedEventArgs e)
	{
		Source sourceById = vm.GetSourceById((int)(sender as ComboBox).Tag);
		if (!sourceById.AuthInitialized)
		{
			sourceById.AuthInitialized = true;
			string[] names = Enum.GetNames(typeof(Source.AuthMode));
			foreach (string newItem in names)
			{
				(sender as ComboBox).Items.Add(newItem);
			}
			(sender as ComboBox).SelectedIndex = (int)sourceById.Auth;
		}
	}

	private void authTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.GetSourceById((int)(sender as ComboBox).Tag).Auth = (Source.AuthMode)(sender as ComboBox).SelectedIndex;
	}

	private void removeSourceButton_Click(object sender, RoutedEventArgs e)
	{
		vm.RemoveSourceById((int)(sender as Button).Tag);
	}

	private void clearSourcesButton_Click(object sender, RoutedEventArgs e)
	{
		vm.Sources.Clear();
	}

	private void addSourceButton_Click(object sender, RoutedEventArgs e)
	{
		vm.Sources.Add(new Source(rand.Next()));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/settings/openbullet/sources.xaml", UriKind.Relative);
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
			addSourceButton = (Button)target;
			addSourceButton.Click += addSourceButton_Click;
			break;
		case 2:
			clearSourcesButton = (Button)target;
			clearSourcesButton.Click += clearSourcesButton_Click;
			break;
		case 3:
			sourcesControl = (ItemsControl)target;
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
		case 4:
			((ComboBox)target).Loaded += authTypeCombobox_Loaded;
			((ComboBox)target).SelectionChanged += authTypeCombobox_SelectionChanged;
			break;
		case 5:
			((Button)target).Click += removeSourceButton_Click;
			break;
		}
	}
}
