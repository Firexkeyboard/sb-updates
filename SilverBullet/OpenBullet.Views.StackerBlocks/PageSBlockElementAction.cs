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

public class PageSBlockElementAction : Page, IComponentConnector
{
	private SBlockElementAction vm;

	public Dictionary<string, string> infoDic = new Dictionary<string, string>
	{
		{ "Clear", "Clears the text in the targeted input element." },
		{ "SendKeys", "Sends the text to the targeted input element." },
		{ "Submit", "Submits the targeted form." },
		{ "GetText", "Gets the innerHTML of the targeted element." },
		{ "GetAttribute", "From the targeted element, gets the attribute with the name specified in the input field." },
		{ "Screenshot", "Takes a screenshot of the targeted element, you can use this to screenshot a gift captcha and send it to be solved later with a Captcha Block." },
		{ "WaitForElement", "Waits until the element specified exists or not exists. You can set the timeout in seconds through the input field [Default Timeout = 10s]." }
	};

	internal ComboBox locatorCombobox;

	internal ComboBox actionCombobox;

	internal TextBlock functionInfoTextblock;

	private bool _contentLoaded;

	public PageSBlockElementAction(SBlockElementAction block)
	{
		InitializeComponent();
		vm = block;
		base.DataContext = vm;
		string[] names = Enum.GetNames(typeof(ElementLocator));
		foreach (string newItem in names)
		{
			locatorCombobox.Items.Add(newItem);
		}
		locatorCombobox.SelectedIndex = (int)vm.Locator;
		names = Enum.GetNames(typeof(ElementAction));
		foreach (string newItem2 in names)
		{
			actionCombobox.Items.Add(newItem2);
		}
		actionCombobox.SelectedIndex = (int)vm.Action;
	}

	private void locatorCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.Locator = (ElementLocator)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void actionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.Action = (ElementAction)((ComboBox)e.OriginalSource).SelectedIndex;
		try
		{
			TextBlock textBlock = functionInfoTextblock;
			Dictionary<string, string> dictionary = infoDic;
			ElementAction action = vm.Action;
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pagesblockelementaction.xaml", UriKind.Relative);
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
			locatorCombobox = (ComboBox)target;
			locatorCombobox.SelectionChanged += locatorCombobox_SelectionChanged;
			break;
		case 2:
			actionCombobox = (ComboBox)target;
			actionCombobox.SelectionChanged += actionCombobox_SelectionChanged;
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
