using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using OpenBullet.ViewModels;
using OpenBullet.Views.Dialogs;
using RuriLib;
using RuriLib.Models;

namespace OpenBullet.Views.Main;

public class WordlistManager : Page, IComponentConnector
{
	private WordlistManagerViewModel vm;

	private GridViewColumnHeader listViewSortCol;

	private SortAdorner listViewSortAdorner;

	internal System.Windows.Controls.Button addButton;

	internal System.Windows.Controls.Button editButton;

	internal System.Windows.Controls.Button deleteButton;

	internal System.Windows.Controls.Button deleteAllButton;

	internal System.Windows.Controls.Button deleteNotFoundWordlistsButton;

	internal System.Windows.Controls.TextBox filterTextbox;

	internal System.Windows.Controls.Button searchButton;

	internal System.Windows.Controls.ListView wordlistListView;

	private bool _contentLoaded;

	// ═══════════════════════════ FOLDER NAVIGATION ═══════════════════════════

	private string currentFolder = null;
	private WrapPanel folderTilesPanel;
	private ScrollViewer _tilesScrollView;
	private bool _foldersExpanded = false;
	private Border _tilesBorder;
	private System.Windows.Shapes.Path _toggleChevron;
	private StackPanel breadcrumbPanel;
	private TextBlock breadcrumbLabel;
	private readonly List<string> _virtualFolders = new();

	public WordlistManager()
	{
		vm = SB.WordlistManager;
		base.DataContext = vm;
		InitializeComponent();
		InitFolderNav();
	}

	public void AddWordlist(Wordlist wordlist)
	{
		try
		{
			vm.Add(wordlist);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.WordlistManager, ex.Message);
		}
	}

	private void addButton_Click(object sender, RoutedEventArgs e)
	{
		new MainDialog(new DialogAddWordlist(this), "Add Wordlist").ShowDialog();
	}

	private void deleteButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogInfo(Components.WordlistManager, $"Deleting {wordlistListView.SelectedItems.Count} references from the DB");
		foreach (Wordlist item in wordlistListView.SelectedItems.Cast<Wordlist>().ToList())
		{
			vm.Remove(item);
		}
		SB.Logger.LogInfo(Components.WordlistManager, "Successfully deleted the wordlist references from the DB");
	}

	private void deleteAllButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogWarning(Components.WordlistManager, "Purge selected, prompting warning");
		if (System.Windows.MessageBox.Show("This will purge the WHOLE Wordlists DB, are you sure you want to continue?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
		{
			SB.Logger.LogInfo(Components.WordlistManager, "Purge initiated");
			vm.RemoveAll();
			SB.Logger.LogInfo(Components.WordlistManager, "Purge finished");
		}
		else
		{
			SB.Logger.LogInfo(Components.WordlistManager, "Purge dismissed");
		}
	}

	private void deleteNotFoundWordlistsButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogWarning(Components.WordlistManager, "Deleting wordlists with missing files.");
		vm.DeleteNotFound();
	}

	private void searchButton_Click(object sender, RoutedEventArgs e)
	{
		vm.SearchString = filterTextbox.Text;
		ApplyListFilter();
	}

	private void filterTextbox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Return)
		{
			searchButton_Click(this, null);
		}
	}

	private void listViewColumnHeader_Click(object sender, RoutedEventArgs e)
	{
		GridViewColumnHeader gridViewColumnHeader = sender as GridViewColumnHeader;
		string propertyName = gridViewColumnHeader.Tag.ToString();
		if (listViewSortCol != null)
		{
			AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
			wordlistListView.Items.SortDescriptions.Clear();
		}
		ListSortDirection listSortDirection = ListSortDirection.Ascending;
		if (listViewSortCol == gridViewColumnHeader && listViewSortAdorner.Direction == listSortDirection)
		{
			listSortDirection = ListSortDirection.Descending;
		}
		listViewSortCol = gridViewColumnHeader;
		listViewSortAdorner = new SortAdorner(listViewSortCol, listSortDirection);
		AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
		wordlistListView.Items.SortDescriptions.Add(new SortDescription(propertyName, listSortDirection));
	}

	private void wordlistListViewDrop(object sender, System.Windows.DragEventArgs e)
	{
		if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
		{
			return;
		}
		string[] array = ((string[])e.Data.GetData(System.Windows.DataFormats.FileDrop)).Where((string x) => x.EndsWith(".txt")).ToArray();
		foreach (string text in array)
		{
			try
			{
				string text2 = text;
				string currentDirectory = Directory.GetCurrentDirectory();
				if (text2.StartsWith(currentDirectory))
				{
					text2 = text2.Substring(currentDirectory.Length + 1);
				}
				Wordlist val = new Wordlist(Path.GetFileNameWithoutExtension(text), text2, SB.Settings.Environment.WordlistTypes.First().Name, "", true, false, (SubWordlist[])null);
				string text3 = File.ReadLines(val.Path).First((string l) => !string.IsNullOrWhiteSpace(l));
				val.Type = SB.Settings.Environment.RecognizeWordlistType(text3);
				AddWordlist(val);
			}
			catch
			{
			}
		}
	}

	private void editButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (wordlistListView.SelectedIndex != -1 && wordlistListView.SelectedItem != null)
			{
				DialogEditWordlist dialogEditWordlist = new DialogEditWordlist((Wordlist)wordlistListView.SelectedItem);
				new MainDialog(dialogEditWordlist, "Edit Wordlist").ShowDialog();
				if (dialogEditWordlist.DialogResult == DialogResult.OK)
				{
					vm.Update(dialogEditWordlist.WordList);
					vm.RefreshList();
				}
			}
		}
		catch (Exception ex)
		{
			SB.Logger.Log(ex.Message, (LogLevel)2, prompt: true);
		}
	}

	// ═══════════════════════ FOLDER NAV IMPLEMENTATION ═══════════════════════

	private void InitFolderNav()
	{
		// Attach "Move to Folder" to the ListView context menu
		if (wordlistListView.ContextMenu != null)
		{
			wordlistListView.ContextMenu.Items.Add(new Separator());
			var moveItem = new MenuItem { Header = "📁  Move to Folder…" };
			moveItem.Click += MoveToFolder_Click;
			wordlistListView.ContextMenu.Items.Add(moveItem);
		}

		InjectFolderPanel();
		RefreshFolderView();

		// Instant refresh on add/remove
		void Subscribe()
		{
			if (vm.WordlistsCollection == null) return;
			vm.WordlistsCollection.CollectionChanged += (s, e) =>
				base.Dispatcher.InvokeAsync(() => RefreshFolderView());
		}
		Subscribe();

		vm.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == "WordlistsCollection")
			{
				Subscribe();
				base.Dispatcher.InvokeAsync(() => RefreshFolderView());
			}
			else if (e.PropertyName == "SearchString")
				base.Dispatcher.InvokeAsync(() => ApplyListFilter());
		};
	}

	private void InjectFolderPanel()
	{
		// ── Build breadcrumb bar ─────────────────────────────────────────────
		breadcrumbPanel = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal,
			Background = new SolidColorBrush(Color.FromRgb(22, 22, 22)),
			Visibility = Visibility.Collapsed
		};
		var backBtn = new System.Windows.Controls.Button
		{
			Content = "← Back",
			Margin = new Thickness(8, 5, 6, 5),
			Padding = new Thickness(14, 4, 14, 4),
			Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0),
			Cursor = System.Windows.Input.Cursors.Hand,
			FontSize = 12
		};
		backBtn.Click += (s, e) => { currentFolder = null; RefreshFolderView(); };
		breadcrumbLabel = new TextBlock
		{
			Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(8, 0, 0, 0),
			FontSize = 12.5
		};
		breadcrumbPanel.Children.Add(backBtn);
		breadcrumbPanel.Children.Add(breadcrumbLabel);

		// ── Build folder tiles strip ─────────────────────────────────────────
		var tilesHost = new Border
		{
			Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
			BorderBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
			BorderThickness = new Thickness(0, 0, 0, 1)
		};

		var headerRow = new Grid { Height = 24 };
		headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var folderLabel = new TextBlock
		{
			Text = "FOLDERS",
			Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
			FontSize = 9.5,
			FontWeight = FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(10, 0, 0, 0)
		};
		Grid.SetColumn(folderLabel, 0);
		headerRow.Children.Add(folderLabel);

		_toggleChevron = new System.Windows.Shapes.Path
		{
			Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
			StrokeThickness = 2.0,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round,
			StrokeLineJoin = PenLineJoin.Round,
			Data = Geometry.Parse("M 2,1 L 7,5 L 12,1"),
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SnapsToDevicePixels = true,
			UseLayoutRounding = true
		};
		var chevronTemplate = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.Button));
		chevronTemplate.VisualTree = new System.Windows.FrameworkElementFactory(typeof(ContentPresenter));
		var toggleBtn = new System.Windows.Controls.Button
		{
			Content = _toggleChevron,
			Template = chevronTemplate,
			Width = 28,
			Height = 20,
			Margin = new Thickness(0, 0, 6, 0),
			Background = Brushes.Transparent,
			Cursor = System.Windows.Input.Cursors.Hand,
			VerticalAlignment = VerticalAlignment.Center,
			ToolTip = "Ocultar / mostrar carpetas"
		};
		toggleBtn.MouseEnter += (s, e) => _toggleChevron.Stroke = new SolidColorBrush(Color.FromRgb(200, 200, 200));
		toggleBtn.MouseLeave += (s, e) => _toggleChevron.Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 120));
		toggleBtn.Click += ToggleFoldersPanel;
		Grid.SetColumn(toggleBtn, 2);
		headerRow.Children.Add(toggleBtn);

		_tilesScrollView = new ScrollViewer
		{
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Visibility = Visibility.Collapsed
		};
		folderTilesPanel = new WrapPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal,
			Margin = new Thickness(6, 6, 6, 7)
		};
		_tilesScrollView.Content = folderTilesPanel;

		var tilesStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical };
		tilesStack.Children.Add(headerRow);
		tilesStack.Children.Add(_tilesScrollView);
		tilesHost.Child = tilesStack;
		_tilesBorder = tilesHost;

		var ctx = new ContextMenu();
		var hideItem = new MenuItem { Header = "Ocultar panel de carpetas" };
		hideItem.Click += (s, e) => { if (_tilesBorder != null) _tilesBorder.Visibility = Visibility.Collapsed; };
		ctx.Items.Add(hideItem);
		tilesHost.ContextMenu = ctx;

		// ── Inject: between the toolbar and the ListView ─────────────────────
		// Try to find the ListView's parent and insert our panels there so
		// the toolbar buttons remain at the top, FOLDERS appears below them.
		if (!TryInjectIntoListParent(tilesHost))
		{
			// Fallback: wrap this.Content in an outer Grid
			var originalContent = this.Content as UIElement;
			var root = new Grid();
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			Grid.SetRow(breadcrumbPanel, 0);
			root.Children.Add(breadcrumbPanel);
			Grid.SetRow(tilesHost, 1);
			root.Children.Add(tilesHost);
			this.Content = root;
			Grid.SetRow(originalContent, 2);
			root.Children.Add(originalContent);
		}
	}

	private bool TryInjectIntoListParent(Border tilesHost)
	{
		if (wordlistListView == null) return false;

		// ── Grid parent (most common for Pages built from XAML) ─────────────
		if (wordlistListView.Parent is Grid grid)
		{
			int listRow  = Grid.GetRow(wordlistListView);
			int listCol  = Grid.GetColumn(wordlistListView);
			int listSpan = Grid.GetColumnSpan(wordlistListView);
			if (listSpan < 1) listSpan = 1;

			// Shift every sibling at or below the ListView's row down by 2
			foreach (UIElement child in grid.Children)
			{
				if (child == wordlistListView) continue;
				if (Grid.GetRow(child) >= listRow)
					Grid.SetRow(child, Grid.GetRow(child) + 2);
			}

			// Insert 2 new Auto rows at listRow position
			grid.RowDefinitions.Insert(listRow,     new RowDefinition { Height = GridLength.Auto });
			grid.RowDefinitions.Insert(listRow + 1, new RowDefinition { Height = GridLength.Auto });

			// Place breadcrumb + tilesHost in the new rows
			Grid.SetRow(breadcrumbPanel, listRow);
			Grid.SetColumn(breadcrumbPanel, listCol);
			Grid.SetColumnSpan(breadcrumbPanel, listSpan);
			grid.Children.Add(breadcrumbPanel);

			Grid.SetRow(tilesHost, listRow + 1);
			Grid.SetColumn(tilesHost, listCol);
			Grid.SetColumnSpan(tilesHost, listSpan);
			grid.Children.Add(tilesHost);

			// Move ListView down 2 rows
			Grid.SetRow(wordlistListView, listRow + 2);
			return true;
		}

		// ── DockPanel parent ─────────────────────────────────────────────────
		if (wordlistListView.Parent is DockPanel dock)
		{
			int idx = dock.Children.IndexOf(wordlistListView);
			dock.Children.Remove(wordlistListView);
			DockPanel.SetDock(breadcrumbPanel, Dock.Top);
			DockPanel.SetDock(tilesHost, Dock.Top);
			dock.Children.Insert(idx, tilesHost);
			dock.Children.Insert(idx, breadcrumbPanel);
			dock.Children.Add(wordlistListView);
			return true;
		}

		// ── Generic Panel parent (StackPanel, WrapPanel…) ────────────────────
		if (wordlistListView.Parent is System.Windows.Controls.Panel panel)
		{
			int idx = panel.Children.IndexOf(wordlistListView);
			panel.Children.Insert(idx, tilesHost);
			panel.Children.Insert(idx, breadcrumbPanel);
			return true;
		}

		return false;
	}

	private void ToggleFoldersPanel(object sender, RoutedEventArgs e)
	{
		_foldersExpanded = !_foldersExpanded;
		if (_tilesScrollView != null)
			_tilesScrollView.Visibility = _foldersExpanded ? Visibility.Visible : Visibility.Collapsed;
		if (_toggleChevron != null)
			_toggleChevron.Data = Geometry.Parse(
				_foldersExpanded ? "M 2,5 L 7,1 L 12,5" : "M 2,1 L 7,5 L 12,1");
	}

	private void RefreshFolderView()
	{
		if (folderTilesPanel == null) return;
		folderTilesPanel.Children.Clear();

		if (currentFolder == null)
		{
			breadcrumbPanel.Visibility = Visibility.Collapsed;

			// Collect categories from loaded wordlists
			var categories = vm.WordlistsCollection
				.Where(w => !string.IsNullOrEmpty(w.Category))
				.Select(w => w.Category)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			// Merge in session-created virtual folders (may be empty)
			foreach (var vf in _virtualFolders) categories.Add(vf);

			foreach (var folder in categories.OrderBy(f => f))
			{
				int cnt = vm.WordlistsCollection.Count(w =>
					string.Equals(w.Category, folder, StringComparison.OrdinalIgnoreCase));
				folderTilesPanel.Children.Add(BuildFolderTile(folder, cnt));
			}

			folderTilesPanel.Children.Add(BuildNewFolderTile());
		}
		else
		{
			breadcrumbPanel.Visibility = Visibility.Visible;
			breadcrumbLabel.Text = "📁  Wordlists  /  " + currentFolder;
		}

		ApplyListFilter();
	}

	private void ApplyListFilter()
	{
		if (vm?.WordlistsCollection == null) return;
		IEnumerable<Wordlist> items = vm.WordlistsCollection;

		if (currentFolder == null)
			items = items.Where(w => string.IsNullOrEmpty(w.Category));
		else
			items = items.Where(w => string.Equals(w.Category, currentFolder, StringComparison.OrdinalIgnoreCase));

		if (!string.IsNullOrEmpty(vm.SearchString))
			items = items.Where(w => w.Name.IndexOf(vm.SearchString, StringComparison.OrdinalIgnoreCase) >= 0);

		wordlistListView.ItemsSource = items.ToList();
	}

	private Border BuildFolderTile(string name, int count)
	{
		var tile = new Border
		{
			Width = 128,
			Height = 74,
			Margin = new Thickness(4, 3, 4, 3),
			Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
			BorderBrush = new SolidColorBrush(Color.FromRgb(52, 52, 52)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Cursor = System.Windows.Input.Cursors.Hand
		};
		tile.MouseEnter += (s, e) =>
		{
			tile.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
			tile.BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90));
		};
		tile.MouseLeave += (s, e) =>
		{
			tile.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
			tile.BorderBrush = new SolidColorBrush(Color.FromRgb(52, 52, 52));
		};
		tile.MouseLeftButtonUp += (s, e) => { currentFolder = name; RefreshFolderView(); };

		var ctx = new ContextMenu();
		var renItem = new MenuItem { Header = "✏️  Rename Folder" };
		renItem.Click += (s, e) => RenameFolder(name);
		var delItem = new MenuItem { Header = "🗑  Delete Folder" };
		delItem.Click += (s, e) => DeleteFolder(name);
		ctx.Items.Add(renItem);
		ctx.Items.Add(new Separator());
		ctx.Items.Add(delItem);
		tile.ContextMenu = ctx;

		var stack = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Vertical,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		var icon = new MahApps.Metro.IconPacks.PackIconMaterial
		{
			Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.FolderOutline,
			Width = 28,
			Height = 28,
			Foreground = new SolidColorBrush(Color.FromRgb(91, 155, 213)),
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			Margin = new Thickness(0, 0, 0, 5)
		};

		var nameText = new TextBlock
		{
			Text = name.Length > 14 ? name.Substring(0, 13) + "…" : name,
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			FontSize = 11,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			TextAlignment = TextAlignment.Center
		};

		var cntText = new TextBlock
		{
			Text = count + " wordlist" + (count == 1 ? "" : "s"),
			Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
			FontSize = 9.5,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			Margin = new Thickness(0, 2, 0, 0)
		};

		stack.Children.Add(icon);
		stack.Children.Add(nameText);
		stack.Children.Add(cntText);
		tile.Child = stack;
		return tile;
	}

	private Border BuildNewFolderTile()
	{
		var tile = new Border
		{
			Width = 128,
			Height = 74,
			Margin = new Thickness(4, 3, 4, 3),
			Background = new SolidColorBrush(Color.FromRgb(22, 22, 22)),
			BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 46)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Cursor = System.Windows.Input.Cursors.Hand
		};
		tile.MouseEnter += (s, e) =>
			tile.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
		tile.MouseLeave += (s, e) =>
			tile.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 46));
		tile.MouseLeftButtonUp += (s, e) => CreateNewFolder();

		var stack = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Vertical,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		stack.Children.Add(new TextBlock
		{
			Text = "+",
			Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
			FontSize = 26,
			FontWeight = FontWeights.Light,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			Margin = new Thickness(0, 0, 0, 2)
		});
		stack.Children.Add(new TextBlock
		{
			Text = "New Folder",
			Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
			FontSize = 11,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center
		});
		tile.Child = stack;
		return tile;
	}

	private void CreateNewFolder()
	{
		string name = ShowInputDialog("New Folder", "Folder name:");
		if (string.IsNullOrWhiteSpace(name)) return;
		name = name.Trim();
		if (!_virtualFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
			_virtualFolders.Add(name);
		RefreshFolderView();
	}

	private void RenameFolder(string oldName)
	{
		string newName = ShowInputDialog("Rename Folder", "New name:");
		if (string.IsNullOrWhiteSpace(newName) || string.Equals(newName, oldName, StringComparison.OrdinalIgnoreCase)) return;
		newName = newName.Trim();
		foreach (var w in vm.WordlistsCollection.Where(w =>
			string.Equals(w.Category, oldName, StringComparison.OrdinalIgnoreCase)).ToList())
		{
			w.Category = newName;
			vm.Update(w);
		}
		if (_virtualFolders.Remove(oldName)) _virtualFolders.Add(newName);
		if (currentFolder == oldName) currentFolder = newName;
		RefreshFolderView();
	}

	private void DeleteFolder(string name)
	{
		int cnt = vm.WordlistsCollection.Count(w =>
			string.Equals(w.Category, name, StringComparison.OrdinalIgnoreCase));
		string msg = cnt > 0
			? $"Folder '{name}' has {cnt} wordlist(s) — they will be moved to root. Continue?"
			: $"Delete empty folder '{name}'?";
		if (System.Windows.MessageBox.Show(msg, "Delete Folder", MessageBoxButton.YesNo, MessageBoxImage.Warning)
			!= MessageBoxResult.Yes) return;

		foreach (var w in vm.WordlistsCollection.Where(w =>
			string.Equals(w.Category, name, StringComparison.OrdinalIgnoreCase)).ToList())
		{
			w.Category = "";
			vm.Update(w);
		}
		_virtualFolders.Remove(name);
		if (currentFolder == name) currentFolder = null;
		RefreshFolderView();
	}

	private void MoveToFolder_Click(object sender, RoutedEventArgs e)
	{
		if (wordlistListView.SelectedItem is not Wordlist wordlist) return;

		var folders = vm.WordlistsCollection
			.Where(w => !string.IsNullOrEmpty(w.Category))
			.Select(w => w.Category)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		foreach (var vf in _virtualFolders)
			if (!folders.Contains(vf, StringComparer.OrdinalIgnoreCase)) folders.Add(vf);
		folders = folders.OrderBy(f => f).ToList();

		ShowMoveFolderDialog(wordlist, folders);
	}

	private void ShowMoveFolderDialog(Wordlist wordlist, List<string> folders)
	{
		var dlg = new Window
		{
			Title = "Move to Folder",
			Width = 320,
			SizeToContent = SizeToContent.Height,
			WindowStartupLocation = WindowStartupLocation.CenterScreen,
			Background = new SolidColorBrush(Color.FromRgb(18, 19, 32)),
			ResizeMode = ResizeMode.NoResize
		};

		var sp = new StackPanel { Margin = new Thickness(14) };
		sp.Children.Add(new TextBlock
		{
			Text = $"Move \"{wordlist.Name}\" to:",
			Foreground = Brushes.White,
			Margin = new Thickness(0, 0, 0, 10),
			FontWeight = FontWeights.SemiBold
		});

		var lb = new System.Windows.Controls.ListBox
		{
			Background = new SolidColorBrush(Color.FromRgb(28, 30, 50)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(60, 65, 110)),
			MaxHeight = 280,
			Margin = new Thickness(0, 0, 0, 12)
		};
		lb.Items.Add(new ListBoxItem
		{
			Content = "📁  (Root — no folder)",
			Tag = ""
		});
		foreach (var f in folders)
			lb.Items.Add(new ListBoxItem { Content = "📁  " + f, Tag = f });

		// Pre-select current folder
		foreach (ListBoxItem item in lb.Items)
		{
			if (string.Equals(item.Tag?.ToString(), wordlist.Category ?? "", StringComparison.OrdinalIgnoreCase))
			{
				item.IsSelected = true;
				break;
			}
		}

		sp.Children.Add(lb);

		var btnRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
		var cancelBtn = new System.Windows.Controls.Button
		{
			Content = "Cancel",
			Width = 70,
			Padding = new Thickness(0, 6, 0, 6),
			Margin = new Thickness(0, 0, 8, 0),
			Background = new SolidColorBrush(Color.FromRgb(50, 52, 70)),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0)
		};
		cancelBtn.Click += (s, e) => dlg.Close();
		var okBtn = new System.Windows.Controls.Button
		{
			Content = "Move",
			Width = 70,
			Padding = new Thickness(0, 6, 0, 6),
			Background = new SolidColorBrush(Color.FromRgb(65, 75, 160)),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0)
		};
		okBtn.Click += (s, e) =>
		{
			if (lb.SelectedItem is ListBoxItem sel)
			{
				MoveWordlistToFolder(wordlist, sel.Tag?.ToString() ?? "");
				dlg.Close();
			}
		};
		btnRow.Children.Add(cancelBtn);
		btnRow.Children.Add(okBtn);
		sp.Children.Add(btnRow);
		dlg.Content = sp;
		dlg.ShowDialog();
	}

	private void MoveWordlistToFolder(Wordlist wordlist, string targetFolder)
	{
		wordlist.Category = targetFolder ?? "";
		vm.Update(wordlist);
		RefreshFolderView();
	}

	private string ShowInputDialog(string title, string prompt)
	{
		var dlg = new Window
		{
			Title = title,
			Width = 340,
			SizeToContent = SizeToContent.Height,
			WindowStartupLocation = WindowStartupLocation.CenterScreen,
			Background = new SolidColorBrush(Color.FromRgb(18, 19, 32)),
			ResizeMode = ResizeMode.NoResize
		};
		var sp = new StackPanel { Margin = new Thickness(16) };
		sp.Children.Add(new TextBlock
		{
			Text = prompt,
			Foreground = Brushes.White,
			Margin = new Thickness(0, 0, 0, 8)
		});
		var tb = new System.Windows.Controls.TextBox
		{
			Background = new SolidColorBrush(Color.FromRgb(28, 30, 50)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(70, 75, 120)),
			Padding = new Thickness(6),
			Margin = new Thickness(0, 0, 0, 12)
		};
		tb.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Return) dlg.DialogResult = true; };
		sp.Children.Add(tb);
		var okBtn = new System.Windows.Controls.Button
		{
			Content = "Create",
			HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
			Width = 80,
			Padding = new Thickness(0, 6, 0, 6),
			Background = new SolidColorBrush(Color.FromRgb(65, 75, 160)),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0)
		};
		okBtn.Click += (s, e) => dlg.DialogResult = true;
		sp.Children.Add(okBtn);
		dlg.Content = sp;
		tb.Focus();
		return dlg.ShowDialog() == true ? tb.Text : null;
	}

	// ═════════════════════════════════════════════════════════════════════════

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/wordlistmanager.xaml", UriKind.Relative);
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
			addButton = (System.Windows.Controls.Button)target;
			addButton.Click += addButton_Click;
			break;
		case 2:
			editButton = (System.Windows.Controls.Button)target;
			editButton.Click += editButton_Click;
			break;
		case 3:
			deleteButton = (System.Windows.Controls.Button)target;
			deleteButton.Click += deleteButton_Click;
			break;
		case 4:
			deleteAllButton = (System.Windows.Controls.Button)target;
			deleteAllButton.Click += deleteAllButton_Click;
			break;
		case 5:
			deleteNotFoundWordlistsButton = (System.Windows.Controls.Button)target;
			deleteNotFoundWordlistsButton.Click += deleteNotFoundWordlistsButton_Click;
			break;
		case 6:
			filterTextbox = (System.Windows.Controls.TextBox)target;
			filterTextbox.KeyDown += filterTextbox_KeyDown;
			break;
		case 7:
			searchButton = (System.Windows.Controls.Button)target;
			searchButton.Click += searchButton_Click;
			break;
		case 8:
			wordlistListView = (System.Windows.Controls.ListView)target;
			wordlistListView.Drop += wordlistListViewDrop;
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
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
