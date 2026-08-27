using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace RuriLib.LS;

public class LoliScriptCompletionData : ICompletionData
{
	public class BlockParameters
	{
		public static BlockParameters Create()
		{
			return new BlockParameters();
		}
	}

	public ImageSource Image => null;

	public string Text { get; private set; }

	public object Content
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Description.ToString()))
			{
				return Text;
			}
			return Text + " (" + Description?.ToString() + ")";
		}
	}

	public object Description { get; private set; }

	public double Priority => 0.0;

	public LoliScriptCompletionData(string text, string description)
	{
		Text = text;
		Description = description;
	}

	public void Complete(TextArea textArea, ISegment completionSegment, EventArgs e)
	{
		textArea.Document.Replace(completionSegment, Text);
	}
}
