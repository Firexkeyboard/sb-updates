using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using OpenBullet.ViewModels;
using OpenBullet.Views.Main.Runner;
using OpenBullet.Views.UserControls;
using RuriLib;
using RuriLib.Models;
using RuriLib.Runner;

namespace OpenBullet;

public class DialogSelectWordlist : Page, IComponentConnector, IStyleConnector
{
	private GridViewColumnHeader listViewSortCol;

	private SortAdorner listViewSortAdorner;

	private WordlistManagerViewModel vm;

	private Task myTask;

	internal System.Windows.Controls.TextBox filterTextbox;

	internal System.Windows.Controls.Button searchButton;

	internal System.Windows.Controls.ListView wordlistsListView;

	internal System.Windows.Controls.TextBox pathTextBox;

	internal System.Windows.Controls.CheckBox addToWordlistsCheckBox;

	internal System.Windows.Controls.CheckBox removeDupCheckBox;

	internal System.Windows.Controls.Button selectButton;

	private bool _contentLoaded;

	private object Caller { get; set; }

	public DialogSelectWordlist(object caller)
	{
		Caller = caller;
		vm = SB.WordlistManager;
		base.DataContext = vm;
		InitializeComponent();
	}

	private void selectButton_Click(object sender, RoutedEventArgs e)
	{
		if (Caller.GetType() == typeof(Runner) && wordlistsListView.SelectedItems.Count > 1)
		{
			Runner runner = (Runner)Caller;
			object dataContext = runner.DataContext;
			RunnerViewModel val = (RunnerViewModel)((dataContext is RunnerViewModel) ? dataContext : null);
			List<string> list = new List<string>();
			string text = string.Empty;
			foreach (Wordlist selectedItem in wordlistsListView.SelectedItems)
			{
				Wordlist val2 = selectedItem;
				if (val.DataPool == null)
				{
					val.DataPool = new DataPool(val2.Path);
				}
				text = text + val2.Name + " & ";
				string[] array = File.ReadAllLines(val2.Path);
				if (val2.RemoveDup)
				{
					array = array.Distinct().ToArray();
				}
				list.AddRange(array);
			}
			if (list.Count > 0)
			{
				val.DataPool.List = list;
				val.DataPool.Size = list.Count;
			}
			Wordlist val3 = (Wordlist)wordlistsListView.SelectedItem;
			Wordlist val4 = new Wordlist(val3.Name, val3.Path, val3.Type, val3.Purpose, true, true, val3.SubWordlists);
			val4.Total = list.Count;
			val4.Name = text.Trim().TrimEnd('&') + " [Multiple]";
			val4.RemoveDup = false;
			runner.SetWordlist(val4);
		}
		else if (Caller.GetType() == typeof(Runner))
		{
			((Runner)Caller).SetWordlist((Wordlist)wordlistsListView.SelectedItem);
		}
		else if (Caller.GetType() == typeof(UserControlWordlist))
		{
			((UserControlWordlist)Caller).Wordlist = (Wordlist)wordlistsListView.SelectedItem;
		}
		((MainDialog)base.Parent).Close();
	}

	private void listViewColumnHeader_Click(object sender, RoutedEventArgs e)
	{
		GridViewColumnHeader gridViewColumnHeader = sender as GridViewColumnHeader;
		string propertyName = gridViewColumnHeader.Tag.ToString();
		if (listViewSortCol != null)
		{
			AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
			wordlistsListView.Items.SortDescriptions.Clear();
		}
		ListSortDirection listSortDirection = ListSortDirection.Ascending;
		if (listViewSortCol == gridViewColumnHeader && listViewSortAdorner.Direction == listSortDirection)
		{
			listSortDirection = ListSortDirection.Descending;
		}
		listViewSortCol = gridViewColumnHeader;
		listViewSortAdorner = new SortAdorner(listViewSortCol, listSortDirection);
		AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
		wordlistsListView.Items.SortDescriptions.Add(new SortDescription(propertyName, listSortDirection));
	}

	private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		selectButton_Click(this, null);
	}

	private void ListViewItem_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Return)
		{
			selectButton_Click(this, null);
		}
	}

	private void importWordlistButton_MouseDown(object sender, MouseButtonEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Wordlist file | *.txt";
		openFileDialog.FilterIndex = 1;
		if (openFileDialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		pathTextBox.Text = openFileDialog.FileName;
		if (addToWordlistsCheckBox.IsChecked == true)
		{
			try
			{
				Wordlist val = WordlistManagerViewModel.FileToWordlist(openFileDialog.FileName);
				val.RemoveDup = removeDupCheckBox.IsChecked == true;
				vm.Add(val);
				return;
			}
			catch (Exception ex)
			{
				SB.Logger.Log(ex.Message, (LogLevel)2, prompt: true);
				return;
			}
		}
		try
		{
			Wordlist val2 = WordlistManagerViewModel.FileToWordlist(openFileDialog.FileName);
			val2.RemoveDup = removeDupCheckBox.IsChecked == true;
			((Runner)Caller).SetWordlist(val2);
			((MainDialog)base.Parent).Close();
		}
		catch
		{
		}
	}

	private void searchButton_Click(object sender, RoutedEventArgs e)
	{
		vm.SearchString = filterTextbox.Text;
	}

	private void filterTextbox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Return)
		{
			searchButton_Click(this, null);
		}
	}

	private void wordlistsListView_DragEnter(object sender, System.Windows.DragEventArgs e)
	{
		if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
		{
			e.Effects = System.Windows.DragDropEffects.Copy;
		}
	}

	private void wordlistsListView_Drop(object sender, System.Windows.DragEventArgs e)
	{
		try
		{
			try
			{
				myTask?.Dispose();
			}
			catch
			{
			}
			if (addToWordlistsCheckBox.IsChecked == true)
			{
				myTask = Task.Run(delegate
				{
					foreach (string item in ((string[])e.Data.GetData(System.Windows.DataFormats.FileDrop)).Where((string w) => w.EndsWith(".txt")))
					{
						try
						{
							Wordlist wordlist = WordlistManagerViewModel.FileToWordlist(item);
							base.Dispatcher.Invoke(() => wordlist.RemoveDup = removeDupCheckBox.IsChecked == true);
							base.Dispatcher.Invoke(delegate
							{
								vm.Add(wordlist);
							});
						}
						catch
						{
						}
					}
				});
			}
			else
			{
				string text = ((string[])e.Data.GetData(System.Windows.DataFormats.FileDrop))[0];
				if (text.EndsWith(".txt") && File.Exists(text))
				{
					Wordlist val = WordlistManagerViewModel.FileToWordlist(text);
					val.RemoveDup = removeDupCheckBox.IsChecked == true;
					((Runner)Caller).SetWordlist(val);
					((MainDialog)base.Parent).Close();
				}
			}
		}
		catch (Exception ex)
		{
			SB.Logger.Log(ex.Message, (LogLevel)2, prompt: true);
		}
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		((MainDialog)base.Parent).Closing += delegate
		{
			try
			{
				myTask?.Dispose();
			}
			catch
			{
			}
		};
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialogselectwordlist.xaml", UriKind.Relative);
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
			((DialogSelectWordlist)target).Loaded += Page_Loaded;
			break;
		case 2:
			filterTextbox = (System.Windows.Controls.TextBox)target;
			filterTextbox.KeyDown += filterTextbox_KeyDown;
			break;
		case 3:
			searchButton = (System.Windows.Controls.Button)target;
			searchButton.Click += searchButton_Click;
			break;
		case 4:
			wordlistsListView = (System.Windows.Controls.ListView)target;
			wordlistsListView.DragEnter += wordlistsListView_DragEnter;
			wordlistsListView.Drop += wordlistsListView_Drop;
			break;
		case 6:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 7:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 8:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 9:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 10:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 11:
			pathTextBox = (System.Windows.Controls.TextBox)target;
			break;
		case 12:
			((Image)target).MouseDown += importWordlistButton_MouseDown;
			break;
		case 13:
			addToWordlistsCheckBox = (System.Windows.Controls.CheckBox)target;
			break;
		case 14:
			removeDupCheckBox = (System.Windows.Controls.CheckBox)target;
			break;
		case 15:
			selectButton = (System.Windows.Controls.Button)target;
			selectButton.Click += selectButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 5)
		{
			EventSetter eventSetter = new EventSetter();
			eventSetter.Event = System.Windows.Controls.Control.MouseDoubleClickEvent;
			eventSetter.Handler = new MouseButtonEventHandler(ListViewItem_MouseDoubleClick);
			((Style)target).Setters.Add(eventSetter);
			eventSetter = new EventSetter();
			eventSetter.Event = UIElement.KeyDownEvent;
			eventSetter.Handler = new System.Windows.Input.KeyEventHandler(ListViewItem_KeyDown);
			((Style)target).Setters.Add(eventSetter);
		}
	}
}
