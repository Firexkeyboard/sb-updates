using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace OpenBullet.AvalonEdit;

public class TruncateLongLines : VisualLineElementGenerator
{
	private const int maxLength = 2000;

	private const string ellipsis = "...";

	private const int charactersAfterEllipsis = 100;

	public override int GetFirstInterestedOffset(int startOffset)
	{
		DocumentLine lastDocumentLine = CurrentContext.VisualLine.LastDocumentLine;
		if (lastDocumentLine.Length > 2000)
		{
			int num = lastDocumentLine.Offset + 2000 - 100 - "...".Length;
			if (startOffset <= num)
			{
				return num;
			}
		}
		return -1;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		return (VisualLineElement)new FormattedTextElement("...", CurrentContext.VisualLine.LastDocumentLine.EndOffset - offset - 100);
	}
}
