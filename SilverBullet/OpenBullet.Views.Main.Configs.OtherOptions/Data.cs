using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib;
using RuriLib.Models;

namespace OpenBullet.Views.Main.Configs.OtherOptions;

public class Data : Page, IComponentConnector, IStyleConnector
{
	private ConfigSettings vm;

	private Random rand = new Random();

	internal ComboBox allowedWordlist1Combobox;

	internal ComboBox allowedWordlist2Combobox;

	internal Button addRuleButton;

	internal Button clearRulesButton;

	internal ItemsControl rulesControl;

	private bool _contentLoaded;

	public Data()
	{
		vm = SB.ConfigManager.CurrentConfig.Config.Settings;
		base.DataContext = vm;
		InitializeComponent();
		allowedWordlist1Combobox.Items.Add("");
		foreach (string wordlistTypeName in SB.Settings.Environment.GetWordlistTypeNames())
		{
			allowedWordlist1Combobox.Items.Add(wordlistTypeName);
		}
		try
		{
			allowedWordlist1Combobox.Text = vm.AllowedWordlist1;
		}
		catch
		{
			allowedWordlist1Combobox.SelectedIndex = 0;
		}
		allowedWordlist2Combobox.Items.Add("");
		foreach (string wordlistTypeName2 in SB.Settings.Environment.GetWordlistTypeNames())
		{
			allowedWordlist2Combobox.Items.Add(wordlistTypeName2);
		}
		try
		{
			allowedWordlist2Combobox.Text = vm.AllowedWordlist2;
		}
		catch
		{
			allowedWordlist2Combobox.SelectedIndex = 0;
		}
	}

	private void clearRulesButton_Click(object sender, RoutedEventArgs e)
	{
		vm.DataRules.Clear();
	}

	private void addRuleButton_Click(object sender, RoutedEventArgs e)
	{
		vm.DataRules.Add(new DataRule(rand.Next()));
	}

	private void removeRuleButton_Click(object sender, RoutedEventArgs e)
	{
		vm.RemoveDataRuleById((int)(sender as Button).Tag);
	}

	private void allowedWordlist1Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.AllowedWordlist1 = (string)allowedWordlist1Combobox.SelectedValue;
	}

	private void allowedWordlist2Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.AllowedWordlist2 = (string)allowedWordlist2Combobox.SelectedValue;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/configs/otheroptions/data.xaml", UriKind.Relative);
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
			allowedWordlist1Combobox = (ComboBox)target;
			allowedWordlist1Combobox.SelectionChanged += allowedWordlist1Combobox_SelectionChanged;
			break;
		case 2:
			allowedWordlist2Combobox = (ComboBox)target;
			allowedWordlist2Combobox.SelectionChanged += allowedWordlist2Combobox_SelectionChanged;
			break;
		case 3:
			addRuleButton = (Button)target;
			addRuleButton.Click += addRuleButton_Click;
			break;
		case 4:
			clearRulesButton = (Button)target;
			clearRulesButton.Click += clearRulesButton_Click;
			break;
		case 5:
			rulesControl = (ItemsControl)target;
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
		if (connectionId == 6)
		{
			((Button)target).Click += removeRuleButton_Click;
		}
	}
}
