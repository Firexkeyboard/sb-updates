using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace OpenBullet;

public static class RichTextBoxExtensions
{
	public static void AppendText(this RichTextBox box, string text, Color color)
	{
		TextRange textRange = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
		textRange.Text = text;
		textRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
	}

	public static string[] Lines(this RichTextBox box)
	{
		return new TextRange(box.Document.ContentStart, box.Document.ContentEnd).Text.Split(new string[1] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
	}

	public static string GetText(this RichTextBox box)
	{
		return new TextRange(box.Document.ContentStart, box.Document.ContentEnd).Text;
	}

	public static string GetTextFromLines(this RichTextBox box)
	{
		return box.Lines().Aggregate((string current, string next) => current + next);
	}

	public static string Select(this RichTextBox rtb, int offset, int length, Color color)
	{
		TextSelection selection = rtb.Selection;
		TextPointer contentStart = rtb.Document.ContentStart;
		TextPointer textPointAt = GetTextPointAt(contentStart, offset);
		TextPointer textPointAt2 = GetTextPointAt(contentStart, offset + length);
		selection.Select(textPointAt, textPointAt2);
		selection.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(color));
		return rtb.Selection.Text;
	}

	public static TextPointer GetTextPointAt(TextPointer from, int pos)
	{
		TextPointer textPointer = from;
		int num = 0;
		while (num < pos && textPointer != null)
		{
			if (textPointer.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text || textPointer.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.None)
			{
				num++;
			}
			if (textPointer.GetPositionAtOffset(1, LogicalDirection.Forward) == null)
			{
				return textPointer;
			}
			textPointer = textPointer.GetPositionAtOffset(1, LogicalDirection.Forward);
		}
		return textPointer;
	}
}
