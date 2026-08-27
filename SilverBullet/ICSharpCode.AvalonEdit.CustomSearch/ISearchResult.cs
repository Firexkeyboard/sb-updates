using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.CustomSearch;

public interface ISearchResult : ISegment
{
	string ReplaceWith(string replacement);
}
