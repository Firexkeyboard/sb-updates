using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockWebSocket : Page, IComponentConnector
{
	private BlockWebSocket vm;

	internal ComboBox wsCommandCombobox;

	internal TabControl wsCommandTabControl;

	internal TabItem emptyTab;

	internal TabItem connectTab;

	internal CheckBox chbCredentials;

	internal RichTextBox customCookiesRTB;

	internal TabItem sendTab;

	private bool _contentLoaded;

	public PageBlockWebSocket(BlockWebSocket block)
	{
		InitializeComponent();
		base.DataContext = (vm = block);
		customCookiesRTB.AppendText(vm.GetCustomHeaders());
	}

	private void wsCommandCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (vm == null)
		{
			return;
		}
		vm.Command = (WSCommand)((ComboBox)e.OriginalSource).SelectedIndex;
		WSCommand command = vm.Command;
		if ((int)command != 0)
		{
			if ((int)command != 2)
			{
				wsCommandTabControl.SelectedIndex = 0;
			}
			else
			{
				wsCommandTabControl.SelectedIndex = 2;
			}
		}
		else
		{
			wsCommandTabControl.SelectedIndex = 1;
		}
	}

	private void customCookiesRTB_LostFocus(object sender, RoutedEventArgs e)
	{
		try
		{
			vm.SetCustomCookies(customCookiesRTB.Lines());
		}
		catch
		{
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblockwebsocket.xaml", UriKind.Relative);
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
			wsCommandCombobox = (ComboBox)target;
			wsCommandCombobox.SelectionChanged += wsCommandCombobox_SelectionChanged;
			break;
		case 2:
			wsCommandTabControl = (TabControl)target;
			break;
		case 3:
			emptyTab = (TabItem)target;
			break;
		case 4:
			connectTab = (TabItem)target;
			break;
		case 5:
			chbCredentials = (CheckBox)target;
			break;
		case 6:
			customCookiesRTB = (RichTextBox)target;
			customCookiesRTB.LostFocus += customCookiesRTB_LostFocus;
			break;
		case 7:
			sendTab = (TabItem)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
