using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;

public static class WPFRichTextBoxExtensions
{
	public static void AppendText(this RichTextBox box, string text, System.Windows.Media.Color color)
	{
		box.SelectionStart = box.TextLength;
		box.SelectionLength = 0;
		box.SelectionColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
		box.AppendText(text);
		box.SelectionColor = box.ForeColor;
		box.AppendText(Environment.NewLine);
	}

	public static void AppendTextToEditor(this TextEditor editor, string text, System.Windows.Media.Color color)
	{
		editor.TextArea.ClearSelection();

		// Get or create the single shared MultiLineColorizer.
		// Using one transformer with a dictionary instead of one LineColorizer per line
		// drops rendering cost from O(n²) to O(1) per visible line.
		var colorizer = editor.Tag as MultiLineColorizer;
		if (colorizer == null)
		{
			colorizer = new MultiLineColorizer();
			editor.TextArea.TextView.LineTransformers.Add((IVisualLineTransformer)(object)colorizer);
			editor.Tag = colorizer;
		}

		colorizer.Set(editor.LineCount, new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B)));
		editor.AppendText(text);
		editor.AppendText(Environment.NewLine);
	}
}
