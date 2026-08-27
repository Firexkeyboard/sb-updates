using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockLSCode : Page, IComponentConnector
{
	private BlockLSCode block;

	internal DockPanel dockParent;

	internal TextEditor scriptEditor;

	private bool _contentLoaded;

	public PageBlockLSCode(BlockLSCode block)
	{
		InitializeComponent();
		this.block = block;
		base.DataContext = this.block;
		scriptEditor.Text = block.Script;
		scriptEditor.ShowLineNumbers = true;
		((Control)(object)scriptEditor.TextArea).Foreground = new SolidColorBrush(Colors.Gainsboro);
		scriptEditor.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Colors.DodgerBlue);
		using XmlReader xmlReader = XmlReader.Create("LSHighlighting.xshd");
		scriptEditor.SyntaxHighlighting = HighlightingLoader.Load(xmlReader, (IHighlightingDefinitionReferenceResolver)(object)HighlightingManager.Instance);
	}

	private void openDocButton_Click(object sender, RoutedEventArgs e)
	{
		new MainDialog(new DialogLSDoc(), "LoliScript Documentation").Show();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblocklscode.xaml", UriKind.Relative);
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
			dockParent = (DockPanel)target;
			break;
		case 2:
			scriptEditor = (TextEditor)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
