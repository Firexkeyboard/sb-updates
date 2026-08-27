using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockSpeechToText : Page, IComponentConnector
{
	private BlockSpeechToText vm;

	internal ComboBox LangComboBox;

	private bool _contentLoaded;

	public PageBlockSpeechToText(BlockSpeechToText block)
	{
		InitializeComponent();
		vm = block;
		base.DataContext = vm;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblockspeechtotext.xaml", UriKind.Relative);
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
			LangComboBox = (ComboBox)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
