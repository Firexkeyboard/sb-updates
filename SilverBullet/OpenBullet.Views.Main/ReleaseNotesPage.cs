using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace OpenBullet.Views.Main;

public class ReleaseNotesPage : Page, IComponentConnector
{
	internal RichTextBox richTextBox;

	private bool _contentLoaded;

	public string App => "Silver Bullet 1.1.4";

	public ReleaseNotesPage()
	{
		InitializeComponent();
		base.DataContext = this;
	}

	private void AppendNote(string[] notes)
	{
		foreach (string text in notes)
		{
			Bold bold = new Bold(new Run("• "));
			bold.SetResourceReference(TextElement.ForegroundProperty, "ForegroundMain");
			Paragraph paragraph = new Paragraph(bold);
			paragraph.SetResourceReference(TextElement.ForegroundProperty, "ForegroundMain");
			paragraph.Inlines.Add(new Run(text));
			richTextBox.Document.Blocks.Add(paragraph);
		}
		Paragraph paragraph2 = new Paragraph(new Bold(new Run("========================")));
		paragraph2.SetResourceReference(TextElement.ForegroundProperty, "ForegroundMain");
		richTextBox.Document.Blocks.Add(paragraph2);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/releasenotespage.xaml", UriKind.Relative);
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
			richTextBox = (RichTextBox)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
