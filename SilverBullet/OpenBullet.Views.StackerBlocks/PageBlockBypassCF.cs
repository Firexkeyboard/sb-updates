using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib;
using RuriLib.Functions.Requests;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockBypassCF : Page, IComponentConnector
{
	private BlockBypassCF vm;

	internal ComboBox securityProtocolCombobox;

	private bool _contentLoaded;

	public PageBlockBypassCF(BlockBypassCF block)
	{
		InitializeComponent();
		vm = block;
		base.DataContext = vm;
		string[] names = Enum.GetNames(typeof(SecurityProtocol));
		foreach (string newItem in names)
		{
			securityProtocolCombobox.Items.Add(newItem);
		}
		securityProtocolCombobox.SelectedIndex = (int)vm.SecurityProtocol;
	}

	private void securityProtocolCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.SecurityProtocol = (SecurityProtocol)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblockbypasscf.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			securityProtocolCombobox = (ComboBox)target;
			securityProtocolCombobox.SelectionChanged += securityProtocolCombobox_SelectionChanged;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
