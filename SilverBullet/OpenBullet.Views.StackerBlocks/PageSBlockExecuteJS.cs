using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageSBlockExecuteJS : Page, IComponentConnector
{
	private SBlockExecuteJS vm;

	internal TextEditor javascriptCodeEditor;

	private bool _contentLoaded;

	public PageSBlockExecuteJS(SBlockExecuteJS block)
	{
		InitializeComponent();
		vm = block;
		base.DataContext = vm;
		javascriptCodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
		javascriptCodeEditor.ShowLineNumbers = true;
		javascriptCodeEditor.Text = vm.JavascriptCode;
	}

	private void javascriptCodeEditor_LostFocus(object sender, EventArgs e)
	{
		vm.JavascriptCode = javascriptCodeEditor.Text;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pagesblockexecutejs.xaml", UriKind.Relative);
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
			javascriptCodeEditor = (TextEditor)target;
			((UIElement)(object)javascriptCodeEditor).LostFocus += javascriptCodeEditor_LostFocus;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
