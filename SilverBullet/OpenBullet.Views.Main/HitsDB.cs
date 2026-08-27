using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Win32;
using OpenBullet.ViewModels;
using OpenBullet.Views.Main.Runner;
using RuriLib;
using RuriLib.Models;
using RuriLib.Runner;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main;

public class HitsDB : Page, IComponentConnector, IStyleConnector
{
	private HitsDBViewModel vm;

	private GridViewColumnHeader listViewSortCol;

	private SortAdorner listViewSortAdorner;

	private Func<Hit, string> mappingCapture = (Hit hit) => hit.Data + " | " + hit.CapturedString;

	private Func<Hit, string> mappingFull = (Hit hit) => "Data = " + hit.Data + " | Type = " + hit.Type + " | Config = " + hit.ConfigName + " | Wordlist = " + hit.WordlistName + " | Proxy = " + hit.Proxy + " | Date = " + hit.Date.ToLongDateString() + " | CapturedData = " + hit.CapturedString;

	internal Button removeDuplicatesButton;

	internal Button deleteFilteredButton;

	internal Button purgeButton;

	internal ComboBox configFilterCombobox;

	internal ComboBox typeFilterCombobox;

	internal ListView hitsListView;

	internal TextBox searchBar;

	internal Button searchButton;

	private bool _contentLoaded;

	private IEnumerable<Hit> Selected => hitsListView.SelectedItems.Cast<Hit>();

	public HitsDB()
	{
		vm = SB.HitsDB;
		base.DataContext = vm;
		InitializeComponent();
		vm.RefreshList();
		foreach (string item in new List<string> { "SUCCESS", "NONE" }.Concat(SB.Settings.Environment.GetCustomKeychainNames()))
		{
			if (!typeFilterCombobox.Items.Contains(item))
			{
				typeFilterCombobox.Items.Add(item);
			}
		}
		typeFilterCombobox.SelectedIndex = 0;
		configFilterCombobox.Items.Add(HitsDBViewModel.defaultFilter);
		foreach (string item2 in vm.ConfigsList.OrderBy((string c) => c))
		{
			configFilterCombobox.Items.Add(item2);
		}
		configFilterCombobox.SelectedIndex = 0;
		ContextMenu obj = (ContextMenu)base.Resources["ItemContextMenu"];
		MenuItem menuItem = (MenuItem)obj.Items[0];
		MenuItem menuItem2 = (MenuItem)obj.Items[1];
		foreach (ExportFormat exportFormat in SB.Settings.Environment.ExportFormats)
		{
			MenuItem menuItem3 = new MenuItem();
			menuItem3.Header = exportFormat.Format;
			menuItem3.Click += copySelectedCustom_Click;
			((MenuItem)menuItem.Items[4]).Items.Add(menuItem3);
		}
		foreach (ExportFormat exportFormat2 in SB.Settings.Environment.ExportFormats)
		{
			MenuItem menuItem4 = new MenuItem();
			menuItem4.Header = exportFormat2.Format;
			menuItem4.Click += saveSelectedCustom_Click;
			((MenuItem)menuItem2.Items[3]).Items.Add(menuItem4);
		}
	}

	public void AddConfigToFilter(string name)
	{
		if (!configFilterCombobox.Items.Cast<string>().Any((string i) => i == name))
		{
			configFilterCombobox.Items.Add(name);
		}
	}

	private void configFilterCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.ConfigFilter = (string)configFilterCombobox.SelectedValue;
		SB.Logger.LogInfo(Components.HitsDB, $"Changed config filter to {vm.ConfigFilter}, found {vm.Total} hits");
	}

	private void typeFilterCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.TypeFilter = typeFilterCombobox.SelectedItem.ToString();
		SB.Logger.LogInfo(Components.HitsDB, $"Changed type filter to {vm.TypeFilter}, found {vm.Total} hits");
	}

	private void typeFilterCombobox_TextChanged(object sender, TextChangedEventArgs e)
	{
		try
		{
			vm.TypeFilter = typeFilterCombobox.Text;
			SB.Logger.LogInfo(Components.HitsDB, $"Changed type filter to {vm.TypeFilter}, found {vm.Total} hits");
		}
		catch
		{
		}
	}

	private void purgeButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogWarning(Components.HitsDB, "Purge selected, prompting warning");
		if (MessageBox.Show("This will purge the WHOLE Hits DB, are you sure you want to continue?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
		{
			SB.Logger.LogInfo(Components.HitsDB, "Purge initiated");
			vm.RemoveAll();
			SB.Logger.LogInfo(Components.HitsDB, "Purge finished");
		}
		else
		{
			SB.Logger.LogInfo(Components.HitsDB, "Purge dismissed");
		}
		try
		{
			List<string> configsList = vm.ConfigsList;
			if (configsList != null && configsList.Count >= 2 && vm.Hits.Count() == 0)
			{
				configFilterCombobox.Items.Clear();
				configFilterCombobox.Items.Add("All");
			}
		}
		catch
		{
		}
	}

	private void listViewColumnHeader_Click(object sender, RoutedEventArgs e)
	{
		GridViewColumnHeader gridViewColumnHeader = sender as GridViewColumnHeader;
		string propertyName = gridViewColumnHeader.Tag.ToString();
		if (listViewSortCol != null)
		{
			AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
			hitsListView.Items.SortDescriptions.Clear();
		}
		ListSortDirection listSortDirection = ListSortDirection.Ascending;
		if (listViewSortCol == gridViewColumnHeader && listViewSortAdorner.Direction == listSortDirection)
		{
			listSortDirection = ListSortDirection.Descending;
		}
		listViewSortCol = gridViewColumnHeader;
		listViewSortAdorner = new SortAdorner(listViewSortCol, listSortDirection);
		AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
		hitsListView.Items.SortDescriptions.Add(new SortDescription(propertyName, listSortDirection));
	}

	private void ListViewItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
	}

	private string GetSaveFile()
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "TXT files | *.txt";
		saveFileDialog.FilterIndex = 1;
		saveFileDialog.ShowDialog();
		return saveFileDialog.FileName;
	}

	private void copySelectedData_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Selected.CopyToClipboard((Hit hit) => hit.Data);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.HitsDB, "Exception while copying hits - " + ex.Message);
		}
	}

	private void copySelectedCapture_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Selected.CopyToClipboard(mappingCapture);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.HitsDB, "Exception while copying hits - " + ex.Message);
		}
	}

	private void copySelectedFull_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Selected.CopyToClipboard(mappingFull);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.HitsDB, "Exception while copying hits - " + ex.Message);
		}
	}

	private void copySelectedCustom_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Selected.CopyToClipboard((Hit hit) => hit.ToFormattedString(StringExtensions.Unescape((sender as MenuItem).Header.ToString(), false)));
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.HitsDB, "Exception while copying hits - " + ex.Message);
		}
	}

	private void saveSelectedData_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Selected.SaveToFile(GetSaveFile(), (Hit hit) => hit.Data);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.HitsDB, "Exception while saving hits - " + ex.Message);
		}
	}

	private void saveSelectedCapture_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Selected.SaveToFile(GetSaveFile(), mappingCapture);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.HitsDB, "Exception while saving hits - " + ex.Message);
		}
	}

	private void saveSelectedFull_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Selected.SaveToFile(GetSaveFile(), mappingFull);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.HitsDB, "Exception while saving hits - " + ex.Message);
		}
	}

	private void saveSelectedCustom_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Selected.SaveToFile(GetSaveFile(), (Hit hit) => hit.ToFormattedString(StringExtensions.Unescape((sender as MenuItem).Header.ToString(), false)));
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.HitsDB, "Exception while copying hits - " + ex.Message);
		}
	}

	private void selectAll_Click(object sender, RoutedEventArgs e)
	{
		hitsListView.SelectAll();
	}

	private void copySelectedProxy_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(((Hit)hitsListView.SelectedItem).Proxy);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.HitsDB, "Failed to copy selected proxy - " + ex.Message);
		}
	}

	private void searchButton_Click(object sender, RoutedEventArgs e)
	{
		vm.SearchString = searchBar.Text;
		SB.Logger.LogInfo(Components.HitsDB, "Changed capture filter to '" + vm.SearchString + $"', found {vm.Total} hits");
	}

	private void sendToRecheck_Click(object sender, RoutedEventArgs e)
	{
		if (hitsListView.SelectedItems.Count == 0)
		{
			SB.Logger.LogError(Components.HitsDB, "No hits selected!", prompt: true);
			return;
		}
		Hit first = (Hit)hitsListView.SelectedItem;
		Wordlist wordlist = new Wordlist("Recheck-" + first.ConfigName, "NULL", SB.Settings.Environment.RecognizeWordlistType(first.Data), "", true, true, (SubWordlist[])null);
		RunnerManagerViewModel runnerManager = SB.RunnerManager;
		runnerManager.Create();
		OpenBullet.Views.Main.Runner.Runner view = runnerManager.RunnersCollection.Last().View;
		RunnerViewModel viewModel = runnerManager.RunnersCollection.Last().ViewModel;
		SB.MainWindow.ShowRunner(view);
		viewModel.SetWordlist(wordlist);
		viewModel.DataPool = new DataPool((IEnumerable<string>)(from Hit h in hitsListView.SelectedItems
			select h.Data).ToList(), (List<string[]>)null, false, false);
		try
		{
			Config config = SB.ConfigManager.ConfigsCollection.First((ConfigViewModel c) => c.Name == first.ConfigName).Config;
			viewModel.SetConfig(config, false);
			viewModel.BotsAmount = Math.Min(config.Settings.SuggestedBots, hitsListView.SelectedItems.Count);
		}
		catch
		{
		}
		SB.MainWindow.menuOptionRunner_Click(this, null);
	}

	private void deleteSelected_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogInfo(Components.HitsDB, $"Deleting {hitsListView.SelectedItems.Count} hits");
		vm.Remove(Selected);
		SB.Logger.LogInfo(Components.HitsDB, "Succesfully sent the delete query and refreshed the list");
	}

	private void removeDuplicatesButton_Click(object sender, RoutedEventArgs e)
	{
		vm.DeleteDuplicates();
		SB.Logger.LogInfo(Components.HitsDB, "Deleted duplicate hits");
	}

	private void deleteFilteredButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogWarning(Components.HitsDB, "Delete filtered selected, prompting warning");
		if (MessageBox.Show("This will delete all the hits that are currently being displayed, are you sure you want to continue?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
		{
			vm.DeleteFiltered();
			SB.Logger.LogInfo(Components.HitsDB, "Deleted filtered hits");
		}
	}

	private void searchBar_KeyDown(object sender, KeyEventArgs e)
	{
		try
		{
			if (e.Key == System.Windows.Input.Key.Return)
			{
				searchButton_Click(sender, e);
			}
		}
		catch
		{
		}
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			List<string> list = new List<string>();
			list.AddRange(from v in vm.Hits
				select v.Type into cType
				group cType by cType into g
				select g.Key);
			foreach (string item in list.Distinct().ToList().Concat(SB.Settings.Environment.GetCustomKeychainNames()))
			{
				if (!typeFilterCombobox.Items.Contains(item))
				{
					typeFilterCombobox.Items.Add(item);
				}
			}
		}
		catch
		{
		}
		try
		{
			List<string> configsList = vm.ConfigsList;
			if (configsList != null && configsList.Count >= 2 && vm.Hits.Count() == 0)
			{
				configFilterCombobox.Items.Clear();
				configFilterCombobox.Items.Add("All");
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/hitsdb.xaml", UriKind.Relative);
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
			((HitsDB)target).Loaded += Page_Loaded;
			break;
		case 2:
			((MenuItem)target).Click += copySelectedData_Click;
			break;
		case 3:
			((MenuItem)target).Click += copySelectedProxy_Click;
			break;
		case 4:
			((MenuItem)target).Click += copySelectedCapture_Click;
			break;
		case 5:
			((MenuItem)target).Click += copySelectedFull_Click;
			break;
		case 6:
			((MenuItem)target).Click += saveSelectedData_Click;
			break;
		case 7:
			((MenuItem)target).Click += saveSelectedCapture_Click;
			break;
		case 8:
			((MenuItem)target).Click += saveSelectedFull_Click;
			break;
		case 9:
			((MenuItem)target).Click += deleteSelected_Click;
			break;
		case 10:
			((MenuItem)target).Click += selectAll_Click;
			break;
		case 11:
			((MenuItem)target).Click += sendToRecheck_Click;
			break;
		case 12:
			removeDuplicatesButton = (Button)target;
			removeDuplicatesButton.Click += removeDuplicatesButton_Click;
			break;
		case 13:
			deleteFilteredButton = (Button)target;
			deleteFilteredButton.Click += deleteFilteredButton_Click;
			break;
		case 14:
			purgeButton = (Button)target;
			purgeButton.Click += purgeButton_Click;
			break;
		case 15:
			configFilterCombobox = (ComboBox)target;
			configFilterCombobox.SelectionChanged += configFilterCombobox_SelectionChanged;
			break;
		case 16:
			typeFilterCombobox = (ComboBox)target;
			typeFilterCombobox.SelectionChanged += typeFilterCombobox_SelectionChanged;
			break;
		case 17:
			hitsListView = (ListView)target;
			break;
		case 19:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 20:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 21:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 22:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 23:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 24:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 25:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 26:
			searchBar = (TextBox)target;
			searchBar.KeyDown += searchBar_KeyDown;
			break;
		case 27:
			searchButton = (Button)target;
			searchButton.Click += searchButton_Click;
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
		if (connectionId == 18)
		{
			EventSetter eventSetter = new EventSetter();
			eventSetter.Event = UIElement.MouseRightButtonDownEvent;
			eventSetter.Handler = new MouseButtonEventHandler(ListViewItem_MouseRightButtonDown);
			((Style)target).Setters.Add(eventSetter);
		}
	}
}
