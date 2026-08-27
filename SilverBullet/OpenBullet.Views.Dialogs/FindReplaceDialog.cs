using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace OpenBullet.Views.Dialogs;

public class FindReplaceDialog : Window, IComponentConnector
{
	private static string textToFind = "";

	private static bool caseSensitive = true;

	private static bool wholeWord = false;

	private static bool useRegex = false;

	private static bool useWildcards = false;

	private static bool searchUp = false;

	private TextEditor editor;

	private static FindReplaceDialog theDialog = null;

	internal TabControl tabMain;

	internal TextBox txtFind;

	internal TextBox txtFind2;

	internal TextBox txtReplace;

	internal CheckBox cbCaseSensitive;

	internal CheckBox cbWholeWord;

	internal CheckBox cbRegex;

	internal CheckBox cbWildcards;

	internal CheckBox cbSearchUp;

	private bool _contentLoaded;

	public FindReplaceDialog(TextEditor editor)
	{
		InitializeComponent();
		this.editor = editor;
		txtFind.Text = (txtFind2.Text = textToFind);
		cbCaseSensitive.IsChecked = caseSensitive;
		cbWholeWord.IsChecked = wholeWord;
		cbRegex.IsChecked = useRegex;
		cbWildcards.IsChecked = useWildcards;
		cbSearchUp.IsChecked = searchUp;
	}

	private void Window_Closed(object sender, EventArgs e)
	{
		textToFind = txtFind2.Text;
		caseSensitive = cbCaseSensitive.IsChecked == true;
		wholeWord = cbWholeWord.IsChecked == true;
		useRegex = cbRegex.IsChecked == true;
		useWildcards = cbWildcards.IsChecked == true;
		searchUp = cbSearchUp.IsChecked == true;
		theDialog = null;
	}

	private void FindNextClick(object sender, RoutedEventArgs e)
	{
		if (!FindNext(txtFind.Text))
		{
			SystemSounds.Beep.Play();
		}
	}

	private void FindNext2Click(object sender, RoutedEventArgs e)
	{
		if (!FindNext(txtFind2.Text))
		{
			SystemSounds.Beep.Play();
		}
	}

	private void ReplaceClick(object sender, RoutedEventArgs e)
	{
		Regex regEx = GetRegEx(txtFind2.Text);
		string text = editor.Text.Substring(editor.SelectionStart, editor.SelectionLength);
		Match match = regEx.Match(text);
		bool flag = false;
		if (match.Success && match.Index == 0 && match.Length == text.Length)
		{
			editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, txtReplace.Text);
			flag = true;
		}
		if (!FindNext(txtFind2.Text) && !flag)
		{
			SystemSounds.Beep.Play();
		}
	}

	private void ReplaceAllClick(object sender, RoutedEventArgs e)
	{
		if (MessageBox.Show("Are you sure you want to Replace All occurences of \"" + txtFind2.Text + "\" with \"" + txtReplace.Text + "\"?", "Replace All", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
		{
			return;
		}
		Regex regEx = GetRegEx(txtFind2.Text, leftToRight: true);
		int num = 0;
		editor.BeginChange();
		foreach (Match item in regEx.Matches(editor.Text))
		{
			editor.Document.Replace(num + item.Index, item.Length, txtReplace.Text);
			num += txtReplace.Text.Length - item.Length;
		}
		editor.EndChange();
	}

	private bool FindNext(string textToFind)
	{
		Regex regEx = GetRegEx(textToFind);
		int startat = (regEx.Options.HasFlag(RegexOptions.RightToLeft) ? editor.SelectionStart : (editor.SelectionStart + editor.SelectionLength));
		Match match = regEx.Match(editor.Text, startat);
		if (!match.Success)
		{
			match = ((!regEx.Options.HasFlag(RegexOptions.RightToLeft)) ? regEx.Match(editor.Text, 0) : regEx.Match(editor.Text, editor.Text.Length));
		}
		if (match.Success)
		{
			editor.Select(match.Index, match.Length);
			TextLocation location = editor.Document.GetLocation(match.Index);
			editor.ScrollTo(location.Line, location.Column);
		}
		return match.Success;
	}

	private Regex GetRegEx(string textToFind, bool leftToRight = false)
	{
		RegexOptions regexOptions = RegexOptions.None;
		if (cbSearchUp.IsChecked == true && !leftToRight)
		{
			regexOptions |= RegexOptions.RightToLeft;
		}
		if (cbCaseSensitive.IsChecked == false)
		{
			regexOptions |= RegexOptions.IgnoreCase;
		}
		if (cbRegex.IsChecked == true)
		{
			return new Regex(textToFind, regexOptions);
		}
		string text = Regex.Escape(textToFind);
		if (cbWildcards.IsChecked == true)
		{
			text = text.Replace("\\*", ".*").Replace("\\?", ".");
		}
		if (cbWholeWord.IsChecked == true)
		{
			text = "\\b" + text + "\\b";
		}
		return new Regex(text, regexOptions);
	}

	public static void ShowForReplace(TextEditor editor)
	{
		if (theDialog == null)
		{
			theDialog = new FindReplaceDialog(editor);
			theDialog.tabMain.SelectedIndex = 1;
			theDialog.Show();
			theDialog.Activate();
		}
		else
		{
			theDialog.tabMain.SelectedIndex = 1;
			theDialog.Activate();
		}
		if (!editor.TextArea.Selection.IsMultiline)
		{
			TextBox textBox = theDialog.txtFind;
			string text2 = (theDialog.txtFind2.Text = editor.TextArea.Selection.GetText());
			textBox.Text = text2;
			theDialog.txtFind.SelectAll();
			theDialog.txtFind2.SelectAll();
			theDialog.txtFind2.Focus();
		}
	}

	public static void ShowForFind(TextEditor editor)
	{
		if (theDialog == null)
		{
			theDialog = new FindReplaceDialog(editor);
			theDialog.tabMain.SelectedIndex = 0;
			theDialog.Show();
			theDialog.Activate();
		}
		else
		{
			theDialog.tabMain.SelectedIndex = 0;
			theDialog.Activate();
		}
	}

	private void Window_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			FindNextClick(null, null);
		}
		else if (e.Key == Key.Escape)
		{
			Close();
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			txtFind.Focus();
		}
		catch
		{
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/findreplacedialog.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((FindReplaceDialog)target).Closed += Window_Closed;
			((FindReplaceDialog)target).KeyDown += Window_KeyDown;
			((FindReplaceDialog)target).Loaded += Window_Loaded;
			break;
		case 2:
			tabMain = (TabControl)target;
			break;
		case 3:
			txtFind = (TextBox)target;
			break;
		case 4:
			((Button)target).Click += FindNextClick;
			break;
		case 5:
			txtFind2 = (TextBox)target;
			break;
		case 6:
			txtReplace = (TextBox)target;
			break;
		case 7:
			((Button)target).Click += FindNext2Click;
			break;
		case 8:
			((Button)target).Click += ReplaceClick;
			break;
		case 9:
			((Button)target).Click += ReplaceAllClick;
			break;
		case 10:
			cbCaseSensitive = (CheckBox)target;
			break;
		case 11:
			cbWholeWord = (CheckBox)target;
			break;
		case 12:
			cbRegex = (CheckBox)target;
			break;
		case 13:
			cbWildcards = (CheckBox)target;
			break;
		case 14:
			cbSearchUp = (CheckBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
