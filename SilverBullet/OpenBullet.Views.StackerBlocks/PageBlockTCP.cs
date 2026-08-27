using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockTCP : Page, IComponentConnector
{
	private BlockTCP block;

	internal ComboBox tcpCommandCombobox;

	internal TabControl commandTabControl;

	internal TabItem emptyTab;

	internal TabItem connectTab;

	internal TabItem sendTab;

	private bool _contentLoaded;

	public PageBlockTCP(BlockTCP block)
	{
		InitializeComponent();
		this.block = block;
		base.DataContext = this.block;
		string[] names = Enum.GetNames(typeof(TCPCommand));
		foreach (string newItem in names)
		{
			tcpCommandCombobox.Items.Add(newItem);
		}
		tcpCommandCombobox.SelectedIndex = (int)block.TCPCommand;
	}

	private void tcpCommandCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		block.TCPCommand = (TCPCommand)((ComboBox)e.OriginalSource).SelectedIndex;
		TCPCommand tCPCommand = block.TCPCommand;
		if ((int)tCPCommand != 0)
		{
			if ((int)tCPCommand != 2)
			{
				commandTabControl.SelectedIndex = 0;
			}
			else
			{
				commandTabControl.SelectedIndex = 2;
			}
		}
		else
		{
			commandTabControl.SelectedIndex = 1;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblocktcp.xaml", UriKind.Relative);
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
			tcpCommandCombobox = (ComboBox)target;
			tcpCommandCombobox.SelectionChanged += tcpCommandCombobox_SelectionChanged;
			break;
		case 2:
			commandTabControl = (TabControl)target;
			break;
		case 3:
			emptyTab = (TabItem)target;
			break;
		case 4:
			connectTab = (TabItem)target;
			break;
		case 5:
			sendTab = (TabItem)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
