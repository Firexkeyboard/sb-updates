using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using RuriLib.Models;
using MahApps.Metro.IconPacks;
using Microsoft.Win32;
using OpenBullet.ViewModels;
using RuriLib;
using RuriLib.Models;
using RuriLib.Runner;
using RuriLib.ViewModels;
using Xceed.Wpf.Toolkit;

namespace OpenBullet.Views.Main;

public class ProxyManager : Page, IComponentConnector, IStyleConnector
{
	private ProxyManagerViewModel vm;

	private GridViewColumnHeader listViewSortCol;

	private SortAdorner listViewSortAdorner;

	private WorkerStatus Status;

	private CancellationTokenSource cts = new CancellationTokenSource();

	internal ProgressBar progressBar;

	internal Slider botsSlider;

	internal Button checkButton;

	internal PackIconIonicons iconCheckButton;

	internal TextBlock checkButtonLabel;

	internal ListView proxiesListView;

	internal Button importButton;

	internal Button exportButton;

	internal Button deleteButton;

	internal Button deleteAllButton;

	internal Button deleteNotWorkingButton;

	internal Button deleteDuplicatesButton;

	internal Button deleteUntestedButton;

	internal IntegerUpDown timeoutUpDown;

	private bool _contentLoaded;

	private IEnumerable<CProxy> Selected => proxiesListView.SelectedItems.Cast<CProxy>();

	public ProxyManager()
	{
		vm = SB.ProxyManager;
		base.DataContext = vm;
		InitializeComponent();
		botsSlider.Maximum = ProxyManagerViewModel.maximumBots;
		SetupListViewEnhancements(); // antes de RefreshList para que el style no esté sellado
		vm.RefreshList();
		vm.UpdateProperties();
	}

	private void SetupListViewEnhancements()
	{
		// 1) Recuperar el ContextMenu ANTES de reemplazar el style
		//    Puede estar en proxiesListView.ContextMenu o dentro del ItemContainerStyle como Setter
		ContextMenu menu = proxiesListView.ContextMenu;
		if (menu == null && proxiesListView.ItemContainerStyle != null)
		{
			foreach (var s in proxiesListView.ItemContainerStyle.Setters.OfType<Setter>())
			{
				if (s.Property == FrameworkElement.ContextMenuProperty && string.IsNullOrEmpty(s.TargetName))
				{
					menu = s.Value as ContextMenu;
					break;
				}
			}
		}

		// 2) Construir nuevo ItemContainerStyle con template propio (quita el cyan brillante)
		var itemStyle = new Style(typeof(ListViewItem));

		itemStyle.Setters.Add(new EventSetter(
			UIElement.MouseRightButtonDownEvent,
			new MouseButtonEventHandler(ListViewItem_MouseRightButtonDown)));

		// Re-incluir el ContextMenu en el nuevo style para que siga funcionando el clic derecho
		if (menu != null)
			itemStyle.Setters.Add(new Setter(FrameworkElement.ContextMenuProperty, menu));

		itemStyle.Setters.Add(new Setter(Control.TemplateProperty, BuildListViewItemTemplate()));

		// Proxies desactivadas → rojo con opacidad reducida
		var disabledTrigger = new DataTrigger { Binding = new Binding("Disabled"), Value = true };
		disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.50));
		disabledTrigger.Setters.Add(new Setter(Control.ForegroundProperty, System.Windows.Media.Brushes.IndianRed));
		itemStyle.Triggers.Add(disabledTrigger);

		proxiesListView.ItemContainerStyle = itemStyle;

		// 3) Si no había ContextMenu en ningún sitio, crear uno con las opciones de copia
		if (menu == null)
		{
			menu = new ContextMenu();
			var copyItem = new MenuItem { Header = "Copy Selected Proxies" };
			copyItem.Click += copySelectedProxies_Click;
			menu.Items.Add(copyItem);
			var copyFullItem = new MenuItem { Header = "Copy Selected Proxies (with type and credentials)" };
			copyFullItem.Click += copySelectedProxiesFull_Click;
			menu.Items.Add(copyFullItem);
			proxiesListView.ContextMenu = menu;
		}

		// 4) Añadir nuevas opciones al ContextMenu
		menu.Items.Add(new Separator());

		var checkSelected = new MenuItem { Header = "Check Selected" };
		checkSelected.Click += async (_, _) =>
		{
			var items = Selected.ToList();
			if (items.Count == 0) return;
			await RunCheckAsync(items);
		};
		menu.Items.Add(checkSelected);

		menu.Items.Add(new Separator());

		var disableItem = new MenuItem { Header = "Disable Selected" };
		disableItem.Click += (_, _) =>
		{
			foreach (var p in Selected.ToList()) { p.Disabled = true; vm.Update(p); }
		};
		menu.Items.Add(disableItem);

		var enableItem = new MenuItem { Header = "Enable Selected" };
		enableItem.Click += (_, _) =>
		{
			foreach (var p in Selected.ToList()) { p.Disabled = false; vm.Update(p); }
		};
		menu.Items.Add(enableItem);
	}

	private static ControlTemplate BuildListViewItemTemplate()
	{
		// Border exterior
		var bdFactory = new FrameworkElementFactory(typeof(Border));
		bdFactory.Name = "Bd";
		bdFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);
		bdFactory.SetValue(Border.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
		bdFactory.SetValue(Border.PaddingProperty, new Thickness(2, 1, 2, 1));
		bdFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);

		// GridViewRowPresenter: renderiza las columnas correctamente
		var rowPresenter = new FrameworkElementFactory(typeof(GridViewRowPresenter));
		rowPresenter.SetBinding(GridViewRowPresenter.ContentProperty,
			new Binding("Content") { RelativeSource = RelativeSource.TemplatedParent });
		rowPresenter.SetBinding(GridViewRowPresenter.ColumnsProperty,
			new Binding("View.Columns")
			{
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ListView), 1)
			});
		rowPresenter.SetBinding(FrameworkElement.HorizontalAlignmentProperty,
			new Binding("HorizontalContentAlignment") { RelativeSource = RelativeSource.TemplatedParent });
		rowPresenter.SetBinding(FrameworkElement.VerticalAlignmentProperty,
			new Binding("VerticalContentAlignment") { RelativeSource = RelativeSource.TemplatedParent });
		bdFactory.AppendChild(rowPresenter);

		var tmpl = new ControlTemplate(typeof(ListViewItem)) { VisualTree = bdFactory };

		// Solo hover — color oscuro/negro discreto, sin quedarse permanente
		var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
			new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(55, 55, 60)), "Bd"));
		tmpl.Triggers.Add(hoverTrigger);

		return tmpl;
	}

	private async void checkButton_Click(object sender, RoutedEventArgs e)
	{
		if ((int)Status != 0)
		{
			if ((int)Status != 1) return;
			try { cts.Cancel(); } catch { }
			try { cts.Dispose(); } catch { }
			return;
		}
		IEnumerable<CProxy> items = vm.OnlyUntested
			? vm.Proxies.Where(p => (int)p.Working == 2)
			: vm.Proxies;
		await RunCheckAsync(items);
	}

	private async Task RunCheckAsync(IEnumerable<CProxy> proxiesToCheck)
	{
		if ((int)Status != 0)
		{
			SB.Logger.LogWarning(Components.ProxyManager, "Checker is already running");
			return;
		}
		SB.Logger.LogInfo(Components.ProxyManager, "Disabling the UI and starting the checker");
		checkButtonLabel.Text = "ABORT";
		botsSlider.IsEnabled = false;
		Status = (WorkerStatus)1;
		if (!SB.Settings.ProxyManagerSettings.ProxySiteUrls.Contains(vm.TestSite))
			SB.Settings.ProxyManagerSettings.ProxySiteUrls.Add(vm.TestSite);
		if (!SB.Settings.ProxyManagerSettings.ProxyKeys.Contains(vm.SuccessKey))
			SB.Settings.ProxyManagerSettings.ProxyKeys.Add(vm.SuccessKey);
		IOManager.SaveSettings<ProxyManagerSettings>(SB.proxyManagerSettingsFile, SB.Settings.ProxyManagerSettings);
		progressBar.Value = 0.0;
		cts = new CancellationTokenSource();
		try
		{
			await Task.Run(async delegate
			{
				await vm.CheckAllAsync(proxiesToCheck, cts.Token, delegate(ProxyCheckResult<ProxyResult> check)
				{
					if (!cts.IsCancellationRequested)
					{
						ProxyResult result = check.result;
						CProxy proxy = result.proxy;
						proxy.LastChecked = DateTime.Now;
						if (check.success)
						{
							proxy.Working = result.working ? ProxyWorking.YES : ProxyWorking.NO;
							proxy.Ping = result.ping;
							proxy.Country = result.country;
							SB.Logger.LogInfo(Components.ProxyManager, $"[{DateTime.Now.ToLongTimeString()}] Check for proxy {proxy.Proxy} succeeded in {result.ping} milliseconds.");
						}
						else
						{
							proxy.Working = (ProxyWorking)1;
							proxy.Ping = 0;
							SB.Logger.LogError(Components.ProxyManager, $"[{DateTime.Now.ToLongTimeString()}] Check for proxy {proxy.Proxy} failed with error: {check.error}");
						}
						if ((int)proxy.Working == 0)
							lock (vm.workingLock) { vm.Working++; }
						else
							lock (vm.notWorkingLock) { vm.NotWorking++; }
						lock (vm.testedLock) { vm.Tested++; }
						vm.Update(proxy);
					}
				}, new Progress<float>(progress =>
				{
					Application.Current.Dispatcher.Invoke(() =>
					{
						progressBar.Value = progress;
						vm.UpdateProperties();
					});
				}));
			});
		}
		catch
		{
			SB.Logger.LogWarning(Components.ProxyManager, "Abort signal received");
		}
		finally
		{
			checkButtonLabel.Text = "CHECK";
			botsSlider.IsEnabled = true;
			Status = (WorkerStatus)0;
		}
	}

	public void AddProxies(IEnumerable<string> raw, ProxyType defaultType = 0, string defaultUsername = "", string defaultPassword = "")
	{
		SB.Logger.LogInfo(Components.ProxyManager, $"Adding {raw.Count()} {defaultType} proxies to the database");
		List<CProxy> list = new List<CProxy>();
		foreach (string item in raw.Where((string p) => !string.IsNullOrEmpty(p)).Distinct().ToList())
		{
			try
			{
				CProxy val = new CProxy().Parse(item, defaultType, defaultUsername, defaultPassword);
				if (!val.IsNumeric || val.IsValidNumeric)
				{
					list.Add(val);
				}
			}
			catch
			{
			}
		}
		vm.AddRange(list);
		vm.UpdateProperties();
	}

	private void botsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		vm.BotsAmount = (int)e.NewValue;
	}

	private void exportButton_Click(object sender, RoutedEventArgs e)
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "Text File |*.txt";
		saveFileDialog.Title = "Export proxies";
		saveFileDialog.ShowDialog();
		if (!(saveFileDialog.FileName != string.Empty))
		{
			return;
		}
		if (Selected.Count() > 0)
		{
			SB.Logger.LogInfo(Components.ProxyManager, $"Exporting {Selected.Count()} proxies");
			Selected.SaveToFile(saveFileDialog.FileName, (CProxy p) => p.Proxy);
		}
		else
		{
			System.Windows.MessageBox.Show("No proxies selected!");
			SB.Logger.LogWarning(Components.ProxyManager, "No proxies selected");
		}
	}

	private void deleteButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogInfo(Components.ProxyManager, $"Deleting {proxiesListView.SelectedItems.Count} proxies");
		vm.Remove(Selected);
		vm.UpdateProperties();
		SB.Logger.LogInfo(Components.ProxyManager, "Proxies deleted successfully");
	}

	private void deleteAllButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogWarning(Components.ProxyManager, "Purging all proxies");
		vm.RemoveAll();
		vm.UpdateProperties();
	}

	private void deleteNotWorkingButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogInfo(Components.ProxyManager, "Deleting all non working proxies");
		vm.RemoveNotWorking();
		vm.UpdateProperties();
	}

	private void importButton_Click(object sender, RoutedEventArgs e)
	{
		new MainDialog(new DialogAddProxies(this), "Import Proxies").ShowDialog();
	}

	private void deleteDuplicatesButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogInfo(Components.ProxyManager, "Deleting duplicate proxies");
		vm.RemoveDuplicates();
		vm.UpdateProperties();
	}

	private void DeleteUntestedButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogInfo(Components.ProxyManager, "Deleting all untested proxies");
		vm.RemoveUntested();
		vm.UpdateProperties();
	}

	private void ProxyListViewDrop(object sender, DragEventArgs e)
	{
		if (!e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			return;
		}
		string[] array = ((string[])e.Data.GetData(DataFormats.FileDrop)).Where((string x) => x.EndsWith(".txt")).ToArray();
		foreach (string text in array)
		{
			try
			{
				string[] raw = File.ReadAllLines(text);
				if (text.ToLower().Contains("http"))
				{
					AddProxies(raw, (ProxyType)0);
					continue;
				}
				if (text.ToLower().Contains("socks4a"))
				{
					AddProxies(raw, (ProxyType)2);
					continue;
				}
				if (text.ToLower().Contains("socks4"))
				{
					AddProxies(raw, (ProxyType)1);
					continue;
				}
				if (text.ToLower().Contains("socks5"))
				{
					AddProxies(raw, (ProxyType)3);
					continue;
				}
				SB.Logger.LogError(Components.ProxyManager, "Failed to parse proxies type from file name, defaulting to HTTP");
				AddProxies(raw, (ProxyType)0);
			}
			catch (Exception ex)
			{
				SB.Logger.LogError(Components.ProxyManager, "Failed to open file " + text + " - " + ex.Message);
			}
		}
	}

	private void copySelectedProxies_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Selected.CopyToClipboard((CProxy p) => p.Host + ":" + p.Port);
			SB.Logger.LogInfo(Components.ProxyManager, $"Copied {Selected.Count()} proxies");
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.ProxyManager, "Failed to copy proxies - " + ex.Message);
		}
	}

	private void copySelectedProxiesFull_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Selected.CopyToClipboard((CProxy p) => $"({p.Type}){p.Host}:{p.Port}" + (string.IsNullOrEmpty(p.Username) ? "" : (":" + p.Username + ":" + p.Password)));
			SB.Logger.LogInfo(Components.ProxyManager, $"Copied {Selected.Count()} proxies");
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.ProxyManager, "Failed to copy proxies - " + ex.Message);
		}
	}

	private void listViewColumnHeader_Click(object sender, RoutedEventArgs e)
	{
		GridViewColumnHeader gridViewColumnHeader = sender as GridViewColumnHeader;
		string propertyName = gridViewColumnHeader.Tag.ToString();
		if (listViewSortCol != null)
		{
			AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
			proxiesListView.Items.SortDescriptions.Clear();
		}
		ListSortDirection listSortDirection = ListSortDirection.Ascending;
		if (listViewSortCol == gridViewColumnHeader && listViewSortAdorner.Direction == listSortDirection)
		{
			listSortDirection = ListSortDirection.Descending;
		}
		listViewSortCol = gridViewColumnHeader;
		listViewSortAdorner = new SortAdorner(listViewSortCol, listSortDirection);
		AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
		proxiesListView.Items.SortDescriptions.Add(new SortDescription(propertyName, listSortDirection));
	}

	private void ListViewItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
	}

	private void AddProxySiteUrl_OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!SB.Settings.ProxyManagerSettings.ProxySiteUrls.Contains(vm.TestSite))
		{
			SB.Settings.ProxyManagerSettings.ProxySiteUrls.Add(vm.TestSite);
			IOManager.SaveSettings<ProxyManagerSettings>(SB.proxyManagerSettingsFile, SB.Settings.ProxyManagerSettings);
		}
	}

	private void RemoveProxySiteUrl_OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is Image { Parent: Grid parent } && parent.Children.Count == 2 && parent.Children[0] is TextBlock textBlock)
		{
			SB.Settings.ProxyManagerSettings.ProxySiteUrls.Remove(textBlock.Text);
			IOManager.SaveSettings<ProxyManagerSettings>(SB.proxyManagerSettingsFile, SB.Settings.ProxyManagerSettings);
		}
	}

	private void AddProxyKey_OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!SB.Settings.ProxyManagerSettings.ProxyKeys.Contains(vm.SuccessKey))
		{
			SB.Settings.ProxyManagerSettings.ProxyKeys.Add(vm.SuccessKey);
			IOManager.SaveSettings<ProxyManagerSettings>(SB.proxyManagerSettingsFile, SB.Settings.ProxyManagerSettings);
		}
	}

	private void RemoveProxyKey_OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is Image { Parent: Grid parent } && parent.Children.Count == 2 && parent.Children[0] is TextBlock textBlock)
		{
			SB.Settings.ProxyManagerSettings.ProxyKeys.Remove(textBlock.Text);
			IOManager.SaveSettings<ProxyManagerSettings>(SB.proxyManagerSettingsFile, SB.Settings.ProxyManagerSettings);
		}
	}

	private void Image_MouseEnter(object sender, MouseEventArgs e)
	{
		if (sender is Image image)
		{
			image.Width = 25.0;
			image.Height = 25.0;
		}
	}

	private void Image_MouseLeave(object sender, MouseEventArgs e)
	{
		if (sender is Image image)
		{
			image.Width = 20.0;
			image.Height = 20.0;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/proxymanager.xaml", UriKind.Relative);
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
			((MenuItem)target).Click += copySelectedProxies_Click;
			break;
		case 2:
			((MenuItem)target).Click += copySelectedProxiesFull_Click;
			break;
		case 3:
			progressBar = (ProgressBar)target;
			break;
		case 5:
			((Image)target).MouseDown += AddProxySiteUrl_OnMouseDown;
			((Image)target).MouseEnter += Image_MouseEnter;
			((Image)target).MouseLeave += Image_MouseLeave;
			break;
		case 6:
			botsSlider = (Slider)target;
			botsSlider.ValueChanged += botsSlider_ValueChanged;
			break;
		case 8:
			((Image)target).MouseDown += AddProxyKey_OnMouseDown;
			((Image)target).MouseEnter += Image_MouseEnter;
			((Image)target).MouseLeave += Image_MouseLeave;
			break;
		case 9:
			checkButton = (Button)target;
			checkButton.Click += checkButton_Click;
			break;
		case 10:
			iconCheckButton = (PackIconIonicons)target;
			break;
		case 11:
			checkButtonLabel = (TextBlock)target;
			break;
		case 12:
			proxiesListView = (ListView)target;
			proxiesListView.Drop += ProxyListViewDrop;
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
			importButton = (Button)target;
			importButton.Click += importButton_Click;
			break;
		case 25:
			exportButton = (Button)target;
			exportButton.Click += exportButton_Click;
			break;
		case 26:
			deleteButton = (Button)target;
			deleteButton.Click += deleteButton_Click;
			break;
		case 27:
			deleteAllButton = (Button)target;
			deleteAllButton.Click += deleteAllButton_Click;
			break;
		case 28:
			deleteNotWorkingButton = (Button)target;
			deleteNotWorkingButton.Click += deleteNotWorkingButton_Click;
			break;
		case 29:
			deleteDuplicatesButton = (Button)target;
			deleteDuplicatesButton.Click += deleteDuplicatesButton_Click;
			break;
		case 30:
			deleteUntestedButton = (Button)target;
			deleteUntestedButton.Click += DeleteUntestedButton_Click;
			break;
		case 31:
			timeoutUpDown = (IntegerUpDown)target;
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
		switch (connectionId)
		{
		case 4:
			((Image)target).MouseDown += RemoveProxySiteUrl_OnMouseDown;
			((Image)target).MouseEnter += Image_MouseEnter;
			((Image)target).MouseLeave += Image_MouseLeave;
			break;
		case 7:
			((Image)target).MouseDown += RemoveProxyKey_OnMouseDown;
			((Image)target).MouseEnter += Image_MouseEnter;
			((Image)target).MouseLeave += Image_MouseLeave;
			break;
		case 13:
		{
			EventSetter eventSetter = new EventSetter();
			eventSetter.Event = UIElement.MouseRightButtonDownEvent;
			eventSetter.Handler = new MouseButtonEventHandler(ListViewItem_MouseRightButtonDown);
			((Style)target).Setters.Add(eventSetter);
			break;
		}
		}
	}
}
