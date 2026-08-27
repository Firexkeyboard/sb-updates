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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using OpenBullet.Repositories;
using OpenBullet.ViewModels;
using PluginFramework;
using RuriLib;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main.Configs;

public class ConfigManager : Page, IComponentConnector, IStyleConnector
{
	private ConfigManagerViewModel vm;

	private GridViewColumnHeader listViewSortCol;

	private SortAdorner listViewSortAdorner;

	internal Button newConfigButton;

	internal Button loadConfigButton;

	internal Button saveConfigButton;

	internal Button deleteConfigsButton;

	internal TextBox filterTextbox;

	internal Button searchButton;

	internal Button openConfigFolderButton;

	internal Button rescanConfigsButton;

	internal ListView configsListView;

	internal MenuItem pasteConfig;

	private bool _contentLoaded;

	private IEnumerable<ConfigViewModel> Selected => configsListView.SelectedItems.Cast<ConfigViewModel>();

	public void OnSaveConfig(object sender, EventArgs e)
	{
		saveConfigButton_Click(this, new RoutedEventArgs());
	}

	public ConfigManager()
	{
		vm = SB.ConfigManager;
		base.DataContext = vm;
		InitializeComponent();
		InitFolderNav();
	}

	public bool CheckSaved()
	{
		Stacker stackerPage = SB.MainWindow.ConfigsPage.StackerPage;
		if (vm.CurrentConfig == null || SB.SBSettings.General.DisableNotSavedWarning || stackerPage == null)
		{
			return true;
		}
		stackerPage.SetScript();
		ConfigViewModel config = SB.Stacker.Config;
		if (config == null)
		{
			return true;
		}
		return vm.SavedHash == config.Config.Script.GetHashCode();
	}

	public void SaveState()
	{
		ConfigViewModel config = SB.Stacker.Config;
		if (config != null)
		{
			vm.SavedHash = config.Config.Script.GetHashCode();
		}
	}

	private void loadConfigButton_Click(object sender, RoutedEventArgs e)
	{
		if (!CheckSaved())
		{
			SB.Logger.LogWarning(Components.Stacker, "Config not saved, prompting quit confirmation");
			if (MessageBox.Show("The Config in Stacker wasn't saved.\nAre you sure you want to load another config?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
			{
				return;
			}
		}
		object selectedItem = configsListView.SelectedItem;
		LoadConfig((ConfigViewModel)((selectedItem is ConfigViewModel) ? selectedItem : null));
	}

	private void saveConfigButton_Click(object sender, RoutedEventArgs e)
	{
		SaveConfig();
	}

	private void deleteConfigsButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogWarning(Components.ConfigManager, "Deletion initiated, prompting warning");
		if (MessageBox.Show("This will delete the physical files from your disk! Are you sure you want to continue?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
		{
			vm.Remove(Selected);
			SB.Logger.LogInfo(Components.ConfigManager, "Deletion completed");
		}
		else
		{
			SB.Logger.LogInfo(Components.ConfigManager, "Deletion cancelled");
		}
	}

	private void rescanConfigsButton_Click(object sender, RoutedEventArgs e)
	{
		vm.Rescan();
	}

	private void newConfigButton_Click(object sender, RoutedEventArgs e)
	{
		if (!CheckSaved())
		{
			SB.Logger.LogWarning(Components.Stacker, "Config not saved, prompting quit confirmation");
			if (MessageBox.Show("The Config in Stacker wasn't saved.\r\nAre you sure you want to create a new config?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
			{
				return;
			}
		}
		new MainDialog(new DialogNewConfig(this), "New Config").ShowDialog();
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

	private void openConfigFolderButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			string targetDir = string.IsNullOrEmpty(currentFolder)
				? Path.Combine(Directory.GetCurrentDirectory(), SB.configFolder)
				: Path.Combine(Directory.GetCurrentDirectory(), SB.configFolder, currentFolder);
			if (!Directory.Exists(targetDir))
				Directory.CreateDirectory(targetDir);
			Process.Start(new ProcessStartInfo { FileName = targetDir, UseShellExecute = true });
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.ConfigManager, "Could not open folder: " + ex.Message, prompt: true);
		}
	}

	public void SaveConfig()
	{
		if (vm.CurrentConfig == null || SB.MainWindow.ConfigsPage.StackerPage == null || SB.MainWindow.ConfigsPage.OtherOptionsPage == null)
		{
			SB.Logger.LogError(Components.ConfigManager, "No config eligible for saving!", prompt: true);
			return;
		}
		if (vm.CurrentConfig.Remote)
		{
			SB.Logger.LogError(Components.ConfigManager, "The config was pulled from a remote source and cannot be saved!", prompt: true);
			return;
		}
		if (vm.CurrentConfigName == string.Empty)
		{
			SB.Logger.LogError(Components.ConfigManager, "Empty config name, cannot save", prompt: true);
			return;
		}
		StackerViewModel stacker = SB.Stacker;
		stacker.ConvertKeychains();
		stacker.ConvertPlugins();
		if (stacker.View == StackerView.Blocks)
		{
			stacker.LS.FromBlocks(stacker.GetList());
		}
		vm.CurrentConfig.Config.Script = stacker.LS.Script;
		SB.Logger.LogInfo(Components.ConfigManager, "Saving config " + vm.CurrentConfigName);
		vm.CurrentConfig.Config.Settings.LastModified = DateTime.Now;
		vm.CurrentConfig.Config.Settings.Version = "1.1.4 [SB]";
		vm.CurrentConfig.Config.Settings.RequiredPlugins = vm.GetRequiredPlugins(vm.CurrentConfig).ToArray();
		SB.Logger.LogInfo(Components.ConfigManager, "Converted the unbinded observables and set the Last Modified date");
		try
		{
			vm.SaveCurrent();
			SaveState();
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.ConfigManager, "Failed to save the config. Reason: " + ex.Message, prompt: true);
		}
	}

	// Silently save the config that is currently open in the Stacker.
	// Called before replacing the Stacker so no edits are ever silently lost.
	private void AutoSaveCurrent()
	{
		try
		{
			var stacker = SB.MainWindow.ConfigsPage.StackerPage;
			if (vm.CurrentConfig == null || stacker == null || vm.CurrentConfig.Remote) return;
			stacker.SetScript();
			vm.CurrentConfig.Config.Settings.LastModified = DateTime.Now;
			vm.SaveCurrent();
			SaveState();
		}
		catch { }
	}

	public void LoadConfig(ConfigViewModel config)
	{
		// Always persist the currently-open config before replacing the Stacker.
		// This prevents data loss when the user switches configs without pressing Ctrl+S.
		AutoSaveCurrent();

		if (config == null)
		{
			SB.Logger.LogError(Components.ConfigManager, "The config to load cannot be null", prompt: true);
			return;
		}
		try
		{
			SBIOManager.CheckRequiredPlugins(SB.BlockPlugins.Select((IBlockPlugin b) => b.Name), config.Config);
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.ConfigManager, ex.Message, prompt: true);
			return;
		}
		vm.CurrentConfig = config;
		if (vm.CurrentConfig.Remote)
		{
			SB.Logger.LogError(Components.ConfigManager, "The config was pulled from a remote source and cannot be edited!", prompt: true);
			vm.CurrentConfig = null;
			return;
		}
		SB.Logger.LogInfo(Components.ConfigManager, "Loading config: " + vm.CurrentConfig.Name);
		SB.MainWindow.ConfigsPage.menuOptionStacker.IsEnabled = true;
		SB.MainWindow.ConfigsPage.menuOptionOtherOptions.IsEnabled = true;
		StackerViewModel stackerViewModel = new StackerViewModel(vm.CurrentConfig);
		if (SB.MainWindow.ConfigsPage.StackerPage != null)
		{
			stackerViewModel.TestData = SB.Stacker.TestData;
			stackerViewModel.TestProxy = SB.Stacker.TestProxy;
			stackerViewModel.ProxyType = SB.Stacker.ProxyType;
		}
		SB.Stacker = stackerViewModel;
		SB.MainWindow.ConfigsPage.StackerPage = new Stacker();
		SB.Logger.LogInfo(Components.ConfigManager, "Created and assigned a new Stacker instance");
		SB.MainWindow.ConfigsPage.OtherOptionsPage = new ConfigOtherOptions();
		SB.Logger.LogInfo(Components.ConfigManager, "Created and assigned a new Other Options instance");
		SB.MainWindow.ConfigsPage.ConfigOcrSettings = new ConfigOcrSettings();
		SB.Logger.LogInfo(Components.ConfigManager, "Created and assigned a new Ocr Testing instance");
		SB.MainWindow.ConfigsPage.menuOptionStacker_MouseDown(this, null);
		SB.MainWindow.ConfigsPage.StackerPage.SetScript();
		SaveState();
	}

	public void CreateConfig(string name, string category, string author)
	{
		// Persist any unsaved changes before replacing the Stacker with a new config.
		AutoSaveCurrent();

		// If the user is inside a folder, save the new config there
		string effectiveCategory = !string.IsNullOrEmpty(currentFolder) ? currentFolder : category;
		ConfigSettings val = new ConfigSettings();
		val.Name = name;
		val.Author = author;
		ConfigViewModel val2 = new ConfigViewModel(string.Empty, name, effectiveCategory, new Config(val, string.Empty), false);
		vm.Add(val2);
		vm.CurrentConfig = val2;
		StackerViewModel stackerViewModel = new StackerViewModel(vm.CurrentConfig);
		if (SB.MainWindow.ConfigsPage.StackerPage != null)
		{
			stackerViewModel.TestData = SB.Stacker.TestData;
			stackerViewModel.TestProxy = SB.Stacker.TestProxy;
			stackerViewModel.ProxyType = SB.Stacker.ProxyType;
		}
		SB.Stacker = stackerViewModel;
		SB.MainWindow.ConfigsPage.StackerPage = new Stacker();
		SB.Logger.LogInfo(Components.ConfigManager, "Created and assigned a new Stacker instance");
		SB.MainWindow.ConfigsPage.OtherOptionsPage = new ConfigOtherOptions();
		SB.Logger.LogInfo(Components.ConfigManager, "Created and assigned a new Other Options instance");
		SB.MainWindow.ConfigsPage.menuOptionStacker_MouseDown(this, null);
	}

	private void configsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			vm.HoveredConfig = ((ConfigViewModel)configsListView.SelectedItem).Config;
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
		loadConfigButton_Click(this, new RoutedEventArgs());
	}

	private void pasteConfig_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			IDataObject dataObject = Clipboard.GetDataObject();
			if (dataObject.GetDataPresent(DataFormats.FileDrop))
			{
				string path = (dataObject.GetData(DataFormats.FileDrop) as string[]).FirstOrDefault();
				PasteConfig(path);
			}
		}
		catch (Exception ex)
		{
			SB.Logger.Log(ex.Message, (LogLevel)2, prompt: true);
		}
	}

	private void PasteConfig(string path)
	{
		string extension = Path.GetExtension(path);
		if (path == null || !(extension == ".svb"))
		{
			switch (extension)
			{
			default:
				return;
			case ".loli":
			case ".loliX":
			case ".anom":
			case ".opk":
				break;
			}
		}
		if (!File.Exists(path))
		{
			return;
		}

		// Paste into active subfolder if one is open, otherwise root
		string destFolder = string.IsNullOrEmpty(currentFolder)
			? Path.Combine(Directory.GetCurrentDirectory(), SB.configFolder)
			: Path.Combine(Directory.GetCurrentDirectory(), SB.configFolder, currentFolder);
		if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

		string fileName = Path.GetFileName(path);
		string destFile = Path.Combine(destFolder, fileName);

		if (File.Exists(destFile))
		{
			if (MessageBox.Show("The File you want to copy already exists. Do you want to replace it?", "File exists [" + Path.GetFileNameWithoutExtension(path) + "]", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				if (path != destFile) File.Delete(destFile);
				File.Copy(path, destFile, path == destFile);
				rescanConfigsButton_Click(null, null);
			}
			else
			{
				string renamed = PathExtensions.Rename(destFile);
				File.Copy(path, renamed, overwrite: false);
				rescanConfigsButton_Click(null, null);
			}
			return;
		}
		File.Copy(path, destFile, overwrite: false);
		if (extension != ".loliX")
		{
			var fi = new FileInfo(destFile);
			if (fi.Exists && fi.Extension != ".svb")
				fi.MoveTo(Path.Combine(fi.Directory.FullName, Path.GetFileNameWithoutExtension(fi.Name) + ".svb"));
		}
		rescanConfigsButton_Click(null, null);
	}

	private void ListViewItem_KeyDown(object sender, KeyEventArgs e)
	{
		try
		{
			if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.V)
			{
				pasteConfig_Click(sender, e);
			}
		}
		catch
		{
		}
	}

	private void filterTextbox_TextChanged(object sender, TextChangedEventArgs e)
	{
		try
		{
			if (string.IsNullOrEmpty(filterTextbox.Text))
			{
				searchButton_Click(sender, null);
			}
		}
		catch
		{
		}
	}

	private void configsListView_DragEnter(object sender, DragEventArgs e)
	{
		try
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Copy;
			}
		}
		catch
		{
		}
	}

	private void configsListView_Drop(object sender, DragEventArgs e)
	{
		try
		{
			string[] locations = (string[])e.Data.GetData(DataFormats.FileDrop);
			Task.Run(delegate
			{
				for (int i = 0; i < locations.Length; i++)
				{
					try
					{
						string loc = locations[i];
						if ((loc.EndsWith(".svb") || loc.EndsWith(".loli") || loc.EndsWith(".anom") || loc.EndsWith(".loliX")) && File.Exists(loc))
						{
							base.Dispatcher.Invoke(delegate
							{
								PasteConfig(loc);
							});
						}
					}
					catch
					{
					}
				}
			});
		}
		catch
		{
		}
	}

	// ═══════════════════════════ FOLDER NAVIGATION ═══════════════════════════

	private string currentFolder = null;
	private WrapPanel folderTilesPanel;
	private StackPanel breadcrumbPanel;
	private TextBlock breadcrumbLabel;
	private bool _foldersExpanded = false;
	private ScrollViewer _tilesScrollView;
	private System.Windows.Shapes.Path _toggleChevron;
	private Border _tilesBorder;

	private void InitFolderNav()
	{
		// Add "Move to Folder" to context menu
		if (configsListView.ContextMenu != null)
		{
			configsListView.ContextMenu.Items.Add(new Separator());
			var moveItem = new MenuItem { Header = "📁  Move to Folder…" };
			moveItem.Click += MoveToFolder_Click;
			configsListView.ContextMenu.Items.Add(moveItem);
		}

		// Add FileType column to the GridView (after Name, index 1)
		if (configsListView.View is GridView gv)
		{
			var header = new GridViewColumnHeader { Content = "Type", Tag = "FileType" };
			header.Click += listViewColumnHeader_Click;
			var typeCol = new GridViewColumn
			{
				Header               = header,
				DisplayMemberBinding = new System.Windows.Data.Binding("FileType"),
				Width                = 80
			};
			gv.Columns.Insert(1, typeCol);
		}

		// Inject folder panel above the ListView
		InjectFolderPanel();
		RefreshFolderView();

		// Subscribe to individual item add/remove (delete, drag-drop, etc.) → instant refresh
		void SubscribeCollectionChanged()
		{
			if (vm.ConfigsCollection == null) return;
			vm.ConfigsCollection.CollectionChanged += (s, e) =>
				base.Dispatcher.InvokeAsync(() => RefreshFolderView());
		}
		SubscribeCollectionChanged();

		// When Rescan() replaces the whole collection, re-subscribe to the new one
		vm.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == "ConfigsCollection")
			{
				SubscribeCollectionChanged();
				base.Dispatcher.InvokeAsync(() => RefreshFolderView());
			}
			else if (e.PropertyName == "SearchString")
				base.Dispatcher.InvokeAsync(() => RefreshFolderView());
		};
	}

	private void ApplyListFilter()
	{
		if (vm?.ConfigsCollection == null) return;
		IEnumerable<ConfigViewModel> items = vm.ConfigsCollection;
		if (currentFolder == null)
			items = items.Where(c => string.IsNullOrEmpty(c.Category) || c.Category == ConfigRepository.defaultCategory);
		else
			items = items.Where(c => string.Equals(c.Category, currentFolder, StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrEmpty(vm.SearchString))
			items = items.Where(c => c.Name.IndexOf(vm.SearchString, StringComparison.OrdinalIgnoreCase) >= 0);
		configsListView.ItemsSource = items.ToList();
	}

	private void InjectFolderPanel()
	{
		var originalContent = this.Content as UIElement;
		var root = new Grid();
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		// ── Breadcrumb bar (hidden at root) ──────────────────────────────────
		breadcrumbPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Background = new SolidColorBrush(Color.FromRgb(22, 22, 22)),
			Visibility = Visibility.Collapsed
		};

		var backBtn = new Button
		{
			Content = "← Back",
			Margin = new Thickness(8, 5, 6, 5),
			Padding = new Thickness(14, 4, 14, 4),
			Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0),
			Cursor = Cursors.Hand,
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
		Grid.SetRow(breadcrumbPanel, 0);
		root.Children.Add(breadcrumbPanel);

		// ── Folder tiles strip ───────────────────────────────────────────────
		var tilesHost = new Border
		{
			Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
			BorderBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
			BorderThickness = new Thickness(0, 0, 0, 1)
		};

		// Header: subtle "FOLDERS" label + clean crisp toggle chevron
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

		// Crisp Path chevron — no blur, no button chrome
		_toggleChevron = new System.Windows.Shapes.Path
		{
			Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
			StrokeThickness = 2.0,
			StrokeStartLineCap = System.Windows.Media.PenLineCap.Round,
			StrokeEndLineCap = System.Windows.Media.PenLineCap.Round,
			StrokeLineJoin = System.Windows.Media.PenLineJoin.Round,
			Data = System.Windows.Media.Geometry.Parse("M 2,5 L 7,1 L 12,5"),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SnapsToDevicePixels = true,
			UseLayoutRounding = true
		};
		// Chromeless button wrapper
		var chevronTemplate = new System.Windows.Controls.ControlTemplate(typeof(Button));
		chevronTemplate.VisualTree = new System.Windows.FrameworkElementFactory(typeof(ContentPresenter));
		var toggleBtn = new Button
		{
			Content = _toggleChevron,
			Template = chevronTemplate,
			Width = 28,
			Height = 20,
			Margin = new Thickness(0, 0, 6, 0),
			Background = Brushes.Transparent,
			Cursor = Cursors.Hand,
			VerticalAlignment = VerticalAlignment.Center,
			ToolTip = "Show / hide folders"
		};
		toggleBtn.MouseEnter += (s, e) => _toggleChevron.Stroke = new SolidColorBrush(Color.FromRgb(200, 200, 200));
		toggleBtn.MouseLeave += (s, e) => _toggleChevron.Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 120));
		toggleBtn.Click += ToggleFoldersPanel;
		Grid.SetColumn(toggleBtn, 2);
		headerRow.Children.Add(toggleBtn);

		_tilesScrollView = new ScrollViewer
		{
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		folderTilesPanel = new WrapPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(6, 6, 6, 7)
		};
		_tilesScrollView.Content = folderTilesPanel;

		var tilesStack = new StackPanel { Orientation = Orientation.Vertical };
		tilesStack.Children.Add(headerRow);
		tilesStack.Children.Add(_tilesScrollView);
		tilesHost.Child = tilesStack;

		// Apply initial collapsed state
		_tilesBorder = tilesHost;
		_tilesScrollView.Visibility = Visibility.Collapsed;
		if (_toggleChevron != null)
			_toggleChevron.Data = System.Windows.Media.Geometry.Parse("M 2,1 L 7,5 L 12,1");

		// Right-click on FOLDERS bar → hide entire panel until restart
		var foldersCtx = new ContextMenu();
		var hideItem = new MenuItem { Header = "Hide folder panel" };
		hideItem.Click += (s, e) => { if (_tilesBorder != null) _tilesBorder.Visibility = Visibility.Collapsed; };
		foldersCtx.Items.Add(hideItem);
		tilesHost.ContextMenu = foldersCtx;

		Grid.SetRow(tilesHost, 1);
		root.Children.Add(tilesHost);

		// ── Original BAML content ────────────────────────────────────────────
		this.Content = root;
		Grid.SetRow(originalContent, 2);
		root.Children.Add(originalContent);
	}

	private void ToggleFoldersPanel(object sender, System.Windows.RoutedEventArgs e)
	{
		_foldersExpanded = !_foldersExpanded;
		if (_tilesScrollView != null)
			_tilesScrollView.Visibility = _foldersExpanded ? Visibility.Visible : Visibility.Collapsed;
		if (_toggleChevron != null)
			_toggleChevron.Data = System.Windows.Media.Geometry.Parse(
				_foldersExpanded ? "M 2,5 L 7,1 L 12,5" : "M 2,1 L 7,5 L 12,1");
	}
	private void RefreshFolderView()
	{
		if (folderTilesPanel == null) return;
		folderTilesPanel.Children.Clear();

		if (currentFolder == null)
		{
			breadcrumbPanel.Visibility = Visibility.Collapsed;

			// Folders from configs already loaded
			var configCategories = vm.ConfigsCollection
				.Where(c => !string.IsNullOrEmpty(c.Category) && c.Category != ConfigRepository.defaultCategory)
				.Select(c => c.Category)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			// Also include physical subdirectories (even empty ones)
			string configRoot = System.IO.Path.Combine(Directory.GetCurrentDirectory(), SB.configFolder);
			if (Directory.Exists(configRoot))
			{
				foreach (var dir in Directory.GetDirectories(configRoot))
					configCategories.Add(System.IO.Path.GetFileName(dir));
			}

			foreach (var folder in configCategories.OrderBy(f => f))
			{
				int cnt = vm.ConfigsCollection.Count(c =>
					string.Equals(c.Category, folder, StringComparison.OrdinalIgnoreCase));
				folderTilesPanel.Children.Add(BuildFolderTile(folder, cnt));
			}

			folderTilesPanel.Children.Add(BuildNewFolderTile());
		}
		else
		{
			breadcrumbPanel.Visibility = Visibility.Visible;
			breadcrumbLabel.Text = "📁  Configs  /  " + currentFolder;
		}

		ApplyListFilter();
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
			Cursor = Cursors.Hand
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
		var delItem = new MenuItem { Header = "🗑  Delete Folder" };
		delItem.Click += (s, e) => DeleteFolder(name);
		ctx.Items.Add(delItem);
		tile.ContextMenu = ctx;

		var stack = new StackPanel
		{
			Orientation = Orientation.Vertical,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		var icon = new MahApps.Metro.IconPacks.PackIconMaterial
		{
			Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Folder,
			Width = 28, Height = 28,
			Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0, 0, 0, 5)
		};

		var nameText = new TextBlock
		{
			Text = name.Length > 14 ? name.Substring(0, 13) + "…" : name,
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			FontSize = 11,
			HorizontalAlignment = HorizontalAlignment.Center,
			TextAlignment = TextAlignment.Center
		};

		var cntText = new TextBlock
		{
			Text = count + " config" + (count == 1 ? "" : "s"),
			Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
			FontSize = 9.5,
			HorizontalAlignment = HorizontalAlignment.Center,
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
			Cursor = Cursors.Hand
		};

		tile.MouseEnter += (s, e) =>
			tile.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
		tile.MouseLeave += (s, e) =>
			tile.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 46));
		tile.MouseLeftButtonUp += (s, e) => CreateNewFolder();

		var stack = new StackPanel
		{
			Orientation = Orientation.Vertical,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		stack.Children.Add(new TextBlock
		{
			Text = "+",
			Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
			FontSize = 26,
			FontWeight = FontWeights.Light,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0, 0, 0, 2)
		});
		stack.Children.Add(new TextBlock
		{
			Text = "New Folder",
			Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
			FontSize = 11,
			HorizontalAlignment = HorizontalAlignment.Center
		});

		tile.Child = stack;
		return tile;
	}

	private void CreateNewFolder()
	{
		string folderName = ShowInputDialog("New Folder", "Folder name:");
		if (string.IsNullOrWhiteSpace(folderName)) return;
		folderName = folderName.Trim();
		string path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), SB.configFolder, folderName);
		try
		{
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);
			vm.Rescan();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Could not create folder: " + ex.Message);
		}
	}

	private void DeleteFolder(string name)
	{
		int cnt = vm.ConfigsCollection.Count(c => c.Category == name);
		string msg = cnt > 0
			? $"Folder '{name}' has {cnt} config(s) — all will be deleted. Continue?"
			: $"Delete empty folder '{name}'?";
		if (MessageBox.Show(msg, "Delete Folder", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
			return;
		try
		{
			string path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), SB.configFolder, name);
			if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
			if (currentFolder == name) currentFolder = null;
			vm.Rescan();
		}
		catch (Exception ex) { MessageBox.Show("Could not delete folder: " + ex.Message); }
	}

	private void MoveToFolder_Click(object sender, RoutedEventArgs e)
	{
		if (configsListView.SelectedItem is not ConfigViewModel config) return;

		var folders = vm.ConfigsCollection
			.Where(c => !string.IsNullOrEmpty(c.Category) && c.Category != ConfigRepository.defaultCategory)
			.Select(c => c.Category)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(f => f)
			.ToList();

		ShowMoveFolderDialog(config, folders);
	}

	private void ShowMoveFolderDialog(ConfigViewModel config, List<string> folders)
	{
		var dlg = new Window
		{
			Title = "Move to Folder",
			Width = 300,
			SizeToContent = SizeToContent.Height,
			WindowStartupLocation = WindowStartupLocation.CenterScreen,
			Background = new SolidColorBrush(Color.FromRgb(18, 19, 32)),
			ResizeMode = ResizeMode.NoResize
		};

		var sp = new StackPanel { Margin = new Thickness(16) };
		sp.Children.Add(new TextBlock
		{
			Text = "Move  \"" + config.Name + "\"  to:",
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0, 0, 0, 10),
			TextWrapping = TextWrapping.Wrap
		});

		var lb = new ListBox
		{
			Background = new SolidColorBrush(Color.FromRgb(22, 24, 40)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(55, 60, 100)),
			Margin = new Thickness(0, 0, 0, 12),
			MaxHeight = 200
		};
		lb.Items.Add(new ListBoxItem { Content = "📁  (Root — no folder)", Tag = ConfigRepository.defaultCategory });
		foreach (var f in folders)
			lb.Items.Add(new ListBoxItem { Content = "📁  " + f, Tag = f });
		lb.SelectedIndex = 0;
		sp.Children.Add(lb);

		var moveBtn = new Button
		{
			Content = "Move",
			HorizontalAlignment = HorizontalAlignment.Right,
			Width = 80,
			Padding = new Thickness(0, 6, 0, 6),
			Background = new SolidColorBrush(Color.FromRgb(65, 75, 160)),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0)
		};
		moveBtn.Click += (s, e) => dlg.DialogResult = true;
		sp.Children.Add(moveBtn);

		dlg.Content = sp;
		if (dlg.ShowDialog() == true && lb.SelectedItem is ListBoxItem sel)
			MoveConfigToFolder(config, sel.Tag?.ToString() ?? ConfigRepository.defaultCategory);
	}

	private void MoveConfigToFolder(ConfigViewModel config, string targetFolder)
	{
		try
		{
			string oldPath = config.Path;
			string newDir = targetFolder == ConfigRepository.defaultCategory
				? System.IO.Path.Combine(Directory.GetCurrentDirectory(), SB.configFolder)
				: System.IO.Path.Combine(Directory.GetCurrentDirectory(), SB.configFolder, targetFolder);

			if (!Directory.Exists(newDir)) Directory.CreateDirectory(newDir);

			string newPath = System.IO.Path.Combine(newDir, System.IO.Path.GetFileName(oldPath));
			if (File.Exists(oldPath) && oldPath != newPath) File.Move(oldPath, newPath);

			vm.Rescan();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Could not move config: " + ex.Message);
			vm.Rescan();
		}
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
		var tb = new TextBox
		{
			Background = new SolidColorBrush(Color.FromRgb(28, 30, 50)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(70, 75, 120)),
			Padding = new Thickness(6),
			Margin = new Thickness(0, 0, 0, 12)
		};
		tb.KeyDown += (s, e) => { if (e.Key == Key.Return) dlg.DialogResult = true; };
		sp.Children.Add(tb);
		var okBtn = new Button
		{
			Content = "Create",
			HorizontalAlignment = HorizontalAlignment.Right,
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/configs/configmanager.xaml", UriKind.Relative);
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
			newConfigButton = (Button)target;
			newConfigButton.Click += newConfigButton_Click;
			break;
		case 2:
			loadConfigButton = (Button)target;
			loadConfigButton.Click += loadConfigButton_Click;
			break;
		case 3:
			saveConfigButton = (Button)target;
			saveConfigButton.Click += saveConfigButton_Click;
			break;
		case 4:
			deleteConfigsButton = (Button)target;
			deleteConfigsButton.Click += deleteConfigsButton_Click;
			break;
		case 5:
			filterTextbox = (TextBox)target;
			filterTextbox.KeyDown += filterTextbox_KeyDown;
			filterTextbox.TextChanged += filterTextbox_TextChanged;
			break;
		case 6:
			searchButton = (Button)target;
			searchButton.Click += searchButton_Click;
			break;
		case 7:
			openConfigFolderButton = (Button)target;
			openConfigFolderButton.Click += openConfigFolderButton_Click;
			break;
		case 8:
			rescanConfigsButton = (Button)target;
			rescanConfigsButton.Click += rescanConfigsButton_Click;
			break;
		case 9:
			configsListView = (ListView)target;
			configsListView.DragEnter += configsListView_DragEnter;
			configsListView.Drop += configsListView_Drop;
			configsListView.SelectionChanged += configsListView_SelectionChanged;
			break;
		case 10:
			pasteConfig = (MenuItem)target;
			pasteConfig.Click += pasteConfig_Click;
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
		case 15:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 16:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 17:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 18:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 19:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
			break;
		case 20:
			((GridViewColumnHeader)target).Click += listViewColumnHeader_Click;
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
		if (connectionId == 11)
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
