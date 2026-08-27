using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using RuriLib.Models;

namespace OpenBullet.Views.Dialogs;

public class DialogEditWordlist : Page, IComponentConnector
{
	internal System.Windows.Controls.TextBox wordlistName;

	internal System.Windows.Controls.TextBox wordlistPath;

	internal System.Windows.Controls.ComboBox wordlistType;

	internal System.Windows.Controls.TextBox wordlistPurpose;

	private bool _contentLoaded;

	public Wordlist WordList { get; private set; }

	public DialogResult DialogResult { get; private set; }

	public DialogEditWordlist(Wordlist wordlist)
	{
		WordList = wordlist;
		InitializeComponent();
		foreach (string wordlistTypeName in SB.Settings.Environment.GetWordlistTypeNames())
		{
			wordlistType.Items.Add(wordlistTypeName);
		}
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			wordlistName.Text = WordList.Name;
			wordlistPath.Text = WordList.Path;
			wordlistPurpose.Text = WordList.Purpose;
			wordlistType.SelectedItem = WordList.Type;
		}
		catch
		{
		}
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			DialogResult = DialogResult.Cancel;
			((MainDialog)base.Parent).Close();
		}
		catch
		{
		}
	}

	private void Button_Click_1(object sender, RoutedEventArgs e)
	{
		try
		{
			DialogResult = DialogResult.OK;
			WordList.Name = wordlistName.Text;
			WordList.Path = wordlistPath.Text;
			WordList.Purpose = wordlistPurpose.Text;
			WordList.Type = wordlistType.SelectedItem.ToString();
			((MainDialog)base.Parent).Close();
		}
		catch
		{
		}
	}

	private void Page_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		try
		{
			if (e.Key == System.Windows.Input.Key.Return)
			{
				Button_Click_1(null, null);
			}
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialogeditwordlist.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			((DialogEditWordlist)target).KeyDown += Page_KeyDown;
			((DialogEditWordlist)target).Loaded += Page_Loaded;
			break;
		case 2:
			wordlistName = (System.Windows.Controls.TextBox)target;
			break;
		case 3:
			wordlistPath = (System.Windows.Controls.TextBox)target;
			break;
		case 4:
			wordlistType = (System.Windows.Controls.ComboBox)target;
			break;
		case 5:
			wordlistPurpose = (System.Windows.Controls.TextBox)target;
			break;
		case 6:
			((System.Windows.Controls.Button)target).Click += Button_Click_1;
			break;
		case 7:
			((System.Windows.Controls.Button)target).Click += Button_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
