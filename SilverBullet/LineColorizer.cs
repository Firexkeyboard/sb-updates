using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

internal class LineColorizer : DocumentColorizingTransformer
{
	public int LineNumber { get; set; }

	public Brush Brush { get; set; }

	public LineColorizer(int lineNumber, Brush brush)
	{
		LineNumber = lineNumber;
		Brush = brush;
	}

	protected override void ColorizeLine(DocumentLine line)
	{
		if (!line.IsDeleted && line.LineNumber == LineNumber)
		{
			ChangeLinePart(line.Offset, line.EndOffset, (Action<VisualLineElement>)ApplyChanges);
		}
	}

	private void ApplyChanges(VisualLineElement element)
	{
		element.TextRunProperties.SetForegroundBrush(Brush);
	}
}
