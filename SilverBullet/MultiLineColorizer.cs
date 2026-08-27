using System.Collections.Generic;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

// Single shared transformer that colors lines by number from a dictionary.
// Replaces the old approach of adding one LineColorizer per line,
// which caused O(n) transformers → O(n²) render cost when scrolling large logs.
internal sealed class MultiLineColorizer : DocumentColorizingTransformer
{
    private readonly Dictionary<int, Brush> _colors = new Dictionary<int, Brush>();

    public void Set(int lineNumber, Brush brush) => _colors[lineNumber] = brush;
    public void Clear() => _colors.Clear();

    protected override void ColorizeLine(DocumentLine line)
    {
        if (!line.IsDeleted && _colors.TryGetValue(line.LineNumber, out Brush b))
            ChangeLinePart(line.Offset, line.EndOffset, el => el.TextRunProperties.SetForegroundBrush(b));
    }
}
