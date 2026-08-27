using System;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CustomSearch;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;

namespace OpenBullet.Editor.CustomSearch;

public class SearchTextEditor
{
	private TextArea textArea;

	private SearchInputHandler handler;

	private TextDocument currentDocument;

	private SearchResultBackgroundRenderer renderer;

	private TextBox searchTextBox;

	private ICSharpCode.AvalonEdit.CustomSearch.ISearchStrategy strategy;

	public int Count
	{
		get
		{
			if (renderer != null)
			{
				return renderer.CurrentResults.Count;
			}
			return 0;
		}
	}

	public string SearchPattern { get; set; }

	public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged;

	public void UpdateSearch()
	{
		strategy = ICSharpCode.AvalonEdit.CustomSearch.SearchStrategyFactory.Create(SearchPattern ?? "", ignoreCase: true, matchWholeWords: false, ICSharpCode.AvalonEdit.CustomSearch.SearchMode.Normal);
		OnSearchOptionsChanged(new SearchOptionsChangedEventArgs(SearchPattern, matchCase: true, useRegex: false, wholeWords: false));
		DoSearch(changeSelection: true);
	}

	private SearchTextEditor()
	{
	}

	public static SearchTextEditor Install(TextEditor editor)
	{
		if (editor == null)
		{
			throw new ArgumentNullException("editor");
		}
		return Install(editor.TextArea);
	}

	public static SearchTextEditor Install(TextArea textArea)
	{
		if (textArea == null)
		{
			throw new ArgumentNullException("textArea");
		}
		SearchTextEditor searchTextEditor = new SearchTextEditor();
		searchTextEditor.AttachInternal(textArea);
		return searchTextEditor;
	}

	private void AttachInternal(TextArea textArea)
	{
		this.textArea = textArea;
		renderer = new SearchResultBackgroundRenderer();
		currentDocument = textArea.Document;
		if (currentDocument != null)
		{
			currentDocument.TextChanged += textArea_Document_TextChanged;
		}
		textArea.DocumentChanged += textArea_DocumentChanged;
	}

	private void textArea_DocumentChanged(object sender, EventArgs e)
	{
		if (currentDocument != null)
		{
			currentDocument.TextChanged -= textArea_Document_TextChanged;
		}
		currentDocument = textArea.Document;
		if (currentDocument != null)
		{
			currentDocument.TextChanged += textArea_Document_TextChanged;
			DoSearch(changeSelection: false);
		}
	}

	private void textArea_Document_TextChanged(object sender, EventArgs e)
	{
		DoSearch(changeSelection: false);
	}

	public void Reactivate()
	{
		if (searchTextBox != null)
		{
			searchTextBox.Focus();
			searchTextBox.SelectAll();
		}
	}

	public void FindNext()
	{
		SearchResult searchResult = renderer.CurrentResults.FindFirstSegmentWithStartAfter(textArea.Caret.Offset + 1);
		if (searchResult == null)
		{
			searchResult = renderer.CurrentResults.FirstSegment;
		}
		if (searchResult != null)
		{
			SelectResult(searchResult);
		}
	}

	public void FindPrevious()
	{
		SearchResult searchResult = renderer.CurrentResults.FindFirstSegmentWithStartAfter(textArea.Caret.Offset);
		if (searchResult != null)
		{
			searchResult = renderer.CurrentResults.GetPreviousSegment(searchResult);
		}
		if (searchResult == null)
		{
			searchResult = renderer.CurrentResults.LastSegment;
		}
		if (searchResult != null)
		{
			SelectResult(searchResult);
		}
	}

	public void DoSearch(bool changeSelection)
	{
		renderer.CurrentResults.Clear();
		if (!string.IsNullOrEmpty(SearchPattern))
		{
			int offset = textArea.Caret.Offset;
			if (changeSelection)
			{
				textArea.ClearSelection();
			}
			foreach (SearchResult item in strategy.FindAll((ITextSource)(object)textArea.Document, 0, textArea.Document.TextLength))
			{
				if (changeSelection && ((TextSegment)item).StartOffset >= offset)
				{
					SelectResult(item);
					changeSelection = false;
				}
				renderer.CurrentResults.Add(item);
			}
		}
		textArea.TextView.InvalidateLayer((KnownLayer)1);
	}

	private void SelectResult(SearchResult result)
	{
		textArea.Caret.Offset = ((TextSegment)result).StartOffset;
		textArea.Selection = Selection.Create(textArea, ((TextSegment)result).StartOffset, ((TextSegment)result).EndOffset);
		textArea.Caret.BringCaretToView();
		textArea.Caret.Show();
	}

	protected virtual void OnSearchOptionsChanged(SearchOptionsChangedEventArgs e)
	{
		this.SearchOptionsChanged?.Invoke(this, e);
	}
}
