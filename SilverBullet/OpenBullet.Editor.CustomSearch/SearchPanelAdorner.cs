using System.Windows;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Editing;

namespace OpenBullet.Editor.CustomSearch;

internal class SearchPanelAdorner : Adorner
{
	private SearchTextEditor panel;

	protected override int VisualChildrenCount => 1;

	public SearchPanelAdorner(TextArea textArea, SearchTextEditor panel)
		: base((UIElement)(object)textArea)
	{
		this.panel = panel;
	}
}
