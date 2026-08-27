using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using OpenBullet.ViewModels;
using OpenBullet.Views.Main.Runner;
using OpenBullet.Views.UserControls;
using RuriLib;
using RuriLib.ViewModels;

namespace OpenBullet;

public class DialogSelectConfig : Page, IComponentConnector, IStyleConnector
{
	private GridViewColumnHeader listViewSortCol;

	private SortAdorner listViewSortAdorner;

	private ConfigManagerViewModel vm;

	internal TextBox filterTextbox;

	internal Button searchButton;

	internal ListView configsListView;

	internal Button refreshButton;

	internal Button selectButton;

	private bool _contentLoaded;

	private object Caller { get; set; }

	public DialogSelectConfig(object caller)
	{
		Caller = caller;
		vm = SB.ConfigManager;
		base.DataContext = vm;
		InitializeComponent();
	}

	private void selectButton_Click(object sender, RoutedEventArgs e)
	{
		if (configsListView.SelectedItems.Count == 0)
		{
			return;
		}
		if (Caller.GetType() == typeof(Runner))
		{
			Config config = ((ConfigViewModel)configsListView.SelectedItem).Config;
			Runner runner = Caller as Runner;
			if (SB.SBSettings.General.LiveConfigUpdates)
			{
				runner.SetConfig(config);
			}
			else
			{
				runner.SetConfig(IOManager.CloneConfig(config));
			}
		}
		else if (Caller.GetType() == typeof(UserControlConfig))
		{
			((UserControlConfig)Caller).Config = (ConfigViewModel)configsListView.SelectedItem;
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
			configsListView.Items.SortDescriptions.Clear();
		}
		ListSortDirection listSortDirection = ListSortDirection.Ascending;
		if (listViewSortCol == gridViewColumnHeader && listViewSortAdorner.Direction == listSortDirection)
		{
			listSortDirection = ListSortDirection.Descending;
		}
		listViewSortCol = gridViewColumnHeader;
		listViewSortAdorner = new SortAdorner(listViewSortCol, listSortDirection);
		AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
		configsListView.Items.SortDescriptions.Add(new SortDescription(propertyName, listSortDirection));
	}

	private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		selectButton_Click(this, null);
	}

	private void ListViewItem_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			selectButton_Click(this, null);
		}
	}

	private void searchButton_Click(object sender, RoutedEventArgs e)
	{
		vm.SearchString = filterTextbox.Text;
	}

	private void filterTextbox_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			searchButton_Click(this, null);
		}
	}

	private void refreshButton_Click(object sender, RoutedEventArgs e)
	{
		vm.Rescan();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialogselectconfig.xaml", UriKind.Relative);
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
			filterTextbox = (TextBox)target;
			filterTextbox.KeyDown += filterTextbox_KeyDown;
			break;
		case 2:
			searchButton = (Button)target;
			searchButton.Click += searchButton_Click;
			break;
		case 3:
			configsListView = (ListView)target;
			break;
		case 5:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
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
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 12:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 13:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 14:
			refreshButton = (Button)target;
			refreshButton.Click += refreshButton_Click;
			break;
		case 15:
			selectButton = (Button)target;
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
		if (connectionId == 4)
		{
			EventSetter eventSetter = new EventSetter();
			eventSetter.Event = Control.MouseDoubleClickEvent;
			eventSetter.Handler = new MouseButtonEventHandler(ListViewItem_MouseDoubleClick);
			((Style)target).Setters.Add(eventSetter);
			eventSetter = new EventSetter();
			eventSetter.Event = UIElement.KeyDownEvent;
			eventSetter.Handler = new KeyEventHandler(ListViewItem_KeyDown);
			((Style)target).Setters.Add(eventSetter);
		}
	}
}
