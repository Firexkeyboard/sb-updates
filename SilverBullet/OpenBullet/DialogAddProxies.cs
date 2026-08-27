using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Net.Http;
using Microsoft.Win32;
using RuriLib.Models;
using OpenBullet.Views.Main;

namespace OpenBullet;

public class DialogAddProxies : Page, IComponentConnector
{
	internal Label fileMode;

	internal Label pasteMode;

	internal Label apiMode;

	internal TabControl modeTabControl;

	internal TabItem fileTab;

	internal ListBox locationListBox;

	internal Image loadProxiesButton;

	internal TabItem pasteTab;

	internal TextBox proxiesBox;

	internal TabItem apiTab;

	internal TextBox urlTextbox;

	internal TextBlock advancedWarning;

	internal ComboBox proxyTypeCombobox;

	internal TextBox usernameTextbox;

	internal TextBox passwordTextbox;

	internal Button acceptButton;

	private bool _contentLoaded;

	private Label clipboardMode;
	private Label scraperMode;
	private TextBox scraperUrlBox;
	private TextBox scraperPatternBox;
	private List<Label> _allModeLabels;

	public object Caller { get; set; }

	public DialogAddProxies(object caller)
	{
		InitializeComponent();
		Caller = caller;
		string[] names = Enum.GetNames(typeof(ProxyType));
		foreach (string text in names)
		{
			if (text != "Chain")
				proxyTypeCombobox.Items.Add(text);
		}
		proxyTypeCombobox.SelectedIndex = 0;
		SetupAdditionalModes();

	}

	private void SetupAdditionalModes()
	{
		if (fileMode.Parent is not Panel modeParent) return;

		// Clipboard label — same style as existing labels, no padding changes
		clipboardMode = new Label
		{
			Content = "Clipboard",
			Foreground = Utils.GetBrush("ForegroundMain"),
			Cursor = Cursors.Hand,
			Style = fileMode.Style,
			Padding = fileMode.Padding,
			Margin = fileMode.Margin,
			FontSize = fileMode.FontSize,
			FontFamily = fileMode.FontFamily,
			FontWeight = fileMode.FontWeight,
			VerticalAlignment = fileMode.VerticalAlignment
		};
		modeParent.Children.Add(clipboardMode);

		// Scraper label
		scraperMode = new Label
		{
			Content = "Scraper",
			Foreground = Utils.GetBrush("ForegroundMain"),
			Cursor = Cursors.Hand,
			Style = fileMode.Style,
			Padding = fileMode.Padding,
			Margin = fileMode.Margin,
			FontSize = fileMode.FontSize,
			FontFamily = fileMode.FontFamily,
			FontWeight = fileMode.FontWeight,
			VerticalAlignment = fileMode.VerticalAlignment
		};
		modeParent.Children.Add(scraperMode);

		// Wrap the mode label row in a horizontal ScrollViewer
		WrapInHorizontalScrollViewer(modeParent);

		// Rewire ALL mode handlers to unified switcher so all labels reset correctly
		_allModeLabels = new List<Label> { fileMode, pasteMode, apiMode, clipboardMode, scraperMode };
		fileMode.MouseDown    -= FileMode_MouseDown;
		pasteMode.MouseDown   -= PasteMode_MouseDown;
		apiMode.MouseDown     -= ApiMode_MouseDown;
		fileMode.MouseDown    += (_, __) => SwitchMode(0);
		pasteMode.MouseDown   += (_, __) => SwitchMode(1);
		apiMode.MouseDown     += (_, __) => SwitchMode(2);
		clipboardMode.MouseDown += (_, __) => SwitchMode(3);
		scraperMode.MouseDown   += (_, __) => SwitchMode(4);

		// Clipboard tab
		var clipTab = new TabItem { Style = fileTab.Style };
		var clipPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
		clipPanel.Children.Add(new TextBlock
		{
			Text = "Proxies will be imported from your clipboard.\nCopy your proxy list first, then click ACCEPT.",
			Foreground = Utils.GetBrush("ForegroundMain"),
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0, 0, 0, 6)
		});
		clipTab.Content = clipPanel;
		modeTabControl.Items.Add(clipTab);

		// Scraper tab
		var scraperTab = new TabItem { Style = fileTab.Style };
		var scraperPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

		scraperPanel.Children.Add(new TextBlock
		{
			Text = "Target URL:",
			Foreground = Utils.GetBrush("ForegroundMain"),
			Margin = new Thickness(0, 0, 0, 2)
		});
		scraperUrlBox = new TextBox
		{
			Background = Utils.GetBrush("BackgroundMain"),
			Foreground = Utils.GetBrush("ForegroundMain"),
			BorderBrush = Utils.GetBrush("ForegroundMain"),
			Margin = new Thickness(0, 0, 0, 8),
			Padding = new Thickness(4, 2, 4, 2)
		};
		scraperPanel.Children.Add(scraperUrlBox);

		scraperPanel.Children.Add(new TextBlock
		{
			Text = "Regex (group 1 = proxy, leave default to extract IP:Port):",
			Foreground = Utils.GetBrush("ForegroundMain"),
			Margin = new Thickness(0, 0, 0, 2)
		});
		scraperPatternBox = new TextBox
		{
			Text = @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:\d{2,5})",
			Background = Utils.GetBrush("BackgroundMain"),
			Foreground = Utils.GetBrush("ForegroundMain"),
			BorderBrush = Utils.GetBrush("ForegroundMain"),
			Margin = new Thickness(0, 0, 0, 4),
			Padding = new Thickness(4, 2, 4, 2)
		};
		scraperPanel.Children.Add(scraperPatternBox);

		scraperPanel.Children.Add(new TextBlock
		{
			Text = "Works on any site: free proxy lists, HTML tables, JSON, etc.",
			Foreground = Utils.GetBrush("ForegroundMain"),
			TextWrapping = TextWrapping.Wrap,
			FontSize = 10,
			Opacity = 0.7
		});

		scraperTab.Content = scraperPanel;
		modeTabControl.Items.Add(scraperTab);

		// Replace the TabControl template: Grid wrapper + explicit bindings so ALL tabs render.
		// ContentSource="SelectedContent" alone fails on file tab (ListBox needs size constraints).
		// The Grid wrapper provides proper stretch constraints to the ContentPresenter.
		modeTabControl.Template = BuildNoHeaderTemplate();
		modeTabControl.Loaded += (_, _) =>
		{
			modeTabControl.Template = BuildNoHeaderTemplate();
			// Defer the index reset to after the first full render pass
			modeTabControl.Dispatcher.BeginInvoke(
				System.Windows.Threading.DispatcherPriority.Background,
				new Action(() =>
				{
					modeTabControl.SelectedIndex = -1;
					modeTabControl.SelectedIndex = 0;
				}));
		};
	}

	private static ControlTemplate BuildNoHeaderTemplate()
	{
		// Grid wrapper → provides stretch size context to the ContentPresenter.
		// Explicit TemplatedParent bindings → reliable content display on all tabs.
		var gridFactory = new FrameworkElementFactory(typeof(Grid));

		var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
		cpFactory.Name = "PART_SelectedContentHost";
		cpFactory.SetBinding(ContentPresenter.ContentProperty,
			new System.Windows.Data.Binding("SelectedContent")
			{ RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
		cpFactory.SetBinding(ContentPresenter.ContentTemplateProperty,
			new System.Windows.Data.Binding("SelectedContentTemplate")
			{ RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
		cpFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0));
		cpFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
		cpFactory.SetValue(FrameworkElement.VerticalAlignmentProperty,   VerticalAlignment.Stretch);

		gridFactory.AppendChild(cpFactory);
		return new ControlTemplate(typeof(TabControl)) { VisualTree = gridFactory };
	}

	private void WrapInHorizontalScrollViewer(Panel panel)
	{
		// Reset any fixed height the BAML panel may have — the gray gap comes from it
		panel.Height    = double.NaN;
		panel.MinHeight = 0;

		var sv = new ScrollViewer
		{
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility   = ScrollBarVisibility.Disabled,
			Margin             = new Thickness(0),
			Padding            = new Thickness(0),
			BorderThickness    = new Thickness(0),
			Background         = System.Windows.Media.Brushes.Transparent,
			Height             = double.NaN,            // auto height from content
			VerticalAlignment  = VerticalAlignment.Top, // don't stretch to fill Grid row
		};

		// Thin scrollbar (8px) and semi-transparent
		sv.Resources[SystemParameters.HorizontalScrollBarHeightKey] = 8.0;
		var scrollBarStyle = new Style(typeof(System.Windows.Controls.Primitives.ScrollBar));
		scrollBarStyle.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
		sv.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = scrollBarStyle;

		switch (panel.Parent)
		{
			case Panel p:
				int i = p.Children.IndexOf(panel);
				p.Children.Remove(panel);
				sv.Content = panel;
				p.Children.Insert(i, sv);
				break;
			case ContentControl cc:
				cc.Content = null;
				sv.Content = panel;
				cc.Content = sv;
				break;
			case Border b:
				b.Child = null;
				sv.Content = panel;
				b.Child = sv;
				break;
			case Decorator d:
				d.Child = null;
				sv.Content = panel;
				d.Child = sv;
				break;
		}

		// Kill any top margin on the tab control so nothing sits between scrollbar and content
		modeTabControl.Margin = new Thickness(
			modeTabControl.Margin.Left,
			0,
			modeTabControl.Margin.Right,
			modeTabControl.Margin.Bottom);
	}

	private void SwitchMode(int index)
	{
		foreach (var lbl in _allModeLabels)
			lbl.Foreground = Utils.GetBrush("ForegroundMain");
		_allModeLabels[index].Foreground = Utils.GetBrush("ForegroundMenuSelected");
		modeTabControl.SelectedIndex = index;
	}

	private void loadProxiesButton_MouseDown(object sender, MouseButtonEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = "Proxy files|*.txt;*.csv;*.list;*.dat|All files|*.*",
			FilterIndex = 1,
			Multiselect = true
		};
		if (openFileDialog.ShowDialog() == true)
		{
			for (int i = 0; i < openFileDialog.FileNames.Length; i++)
			{
				locationListBox.Items.Add(openFileDialog.FileNames[i]);
			}
		}
	}

	private void acceptButton_Click(object sender, RoutedEventArgs e)
	{
		string[] array = locationListBox.Items.OfType<string>().ToArray();
		List<string> list = new List<string>();
		try
		{
			switch (modeTabControl.SelectedIndex)
			{
			case 0:
				if (array.Length == 0)
				{
					SB.Logger.LogError(Components.ProxyManager, "No file specified!", prompt: true);
					return;
				}
				foreach (string filePath in array)
				{
					SB.Logger.LogInfo(Components.ProxyManager, "Loading from file " + filePath);
					list.AddRange(File.ReadAllLines(filePath));
				}
				break;
			case 1:
				if (string.IsNullOrWhiteSpace(proxiesBox.Text))
				{
					SB.Logger.LogError(Components.ProxyManager, "The box is empty!", prompt: true);
					return;
				}
				list.AddRange(proxiesBox.Text.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None));
				break;
			case 2:
				if (string.IsNullOrWhiteSpace(urlTextbox.Text))
				{
					SB.Logger.LogError(Components.ProxyManager, "No URL specified!", prompt: true);
					return;
				}
				// Support multiple URLs, one per line
				var urls = urlTextbox.Text.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
				using (var _dlgHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
				{
					foreach (var url in urls.Select(u => u.Trim()).Where(u => u.Length > 0))
					{
						try
						{
							SB.Logger.LogInfo(Components.ProxyManager, "Fetching proxies from " + url);
							var resp = _dlgHttp.GetAsync(url).GetAwaiter().GetResult();
							string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
							list.AddRange(body.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None));
						}
						catch (Exception urlEx)
						{
							SB.Logger.LogError(Components.ProxyManager, $"Failed to fetch {url}: {urlEx.Message}");
						}
					}
				}
				if (list.Count == 0)
				{
					SB.Logger.LogError(Components.ProxyManager, "No proxies retrieved from any URL!", prompt: true);
					return;
				}
				break;
			case 3: // Clipboard
				string clipText = Clipboard.GetText();
				if (string.IsNullOrWhiteSpace(clipText))
				{
					SB.Logger.LogError(Components.ProxyManager, "Clipboard is empty!", prompt: true);
					return;
				}
				list.AddRange(clipText.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None));
				SB.Logger.LogInfo(Components.ProxyManager, $"Read {list.Count} lines from clipboard");
				break;
			case 4: // Scraper
				if (string.IsNullOrWhiteSpace(scraperUrlBox?.Text))
				{
					SB.Logger.LogError(Components.ProxyManager, "No URL specified!", prompt: true);
					return;
				}
				if (string.IsNullOrWhiteSpace(scraperPatternBox?.Text))
				{
					SB.Logger.LogError(Components.ProxyManager, "No regex pattern specified!", prompt: true);
					return;
				}
				try
				{
					using var scraperHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
					var scraperResp = scraperHttp.GetAsync(scraperUrlBox.Text).GetAwaiter().GetResult();
					string html = scraperResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
					var regex = new Regex(scraperPatternBox.Text);
					foreach (Match m in regex.Matches(html))
					{
						string match = m.Groups.Count > 1 ? m.Groups[1].Value : m.Groups[0].Value;
						if (!string.IsNullOrWhiteSpace(match))
							list.Add(match.Trim());
					}
					if (list.Count == 0)
					{
						SB.Logger.LogError(Components.ProxyManager, "Regex matched 0 proxies on the page!", prompt: true);
						return;
					}
					SB.Logger.LogInfo(Components.ProxyManager, $"Scraper extracted {list.Count} proxies");
				}
				catch (Exception scraperEx)
				{
					SB.Logger.LogError(Components.ProxyManager, "Scraper error: " + scraperEx.Message);
					return;
				}
				break;
			}
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.ProxyManager, "There was an error: " + ex.Message);
			return;
		}
		// Trim whitespace from each line before parsing
		list = list.Select(l => l.Trim()).ToList();
		if (Caller.GetType() == typeof(ProxyManager))
		{
			((ProxyManager)Caller).AddProxies(list, (ProxyType)Enum.Parse(typeof(ProxyType), proxyTypeCombobox.Text), usernameTextbox.Text, passwordTextbox.Text);
		}
		((MainDialog)base.Parent).Close();
	}

	private void FileMode_MouseDown(object sender, MouseButtonEventArgs e)
	{
		fileMode.Foreground = Utils.GetBrush("ForegroundMenuSelected");
		pasteMode.Foreground = Utils.GetBrush("ForegroundMain");
		apiMode.Foreground = Utils.GetBrush("ForegroundMain");
		modeTabControl.SelectedIndex = 0;
	}

	private void PasteMode_MouseDown(object sender, MouseButtonEventArgs e)
	{
		fileMode.Foreground = Utils.GetBrush("ForegroundMain");
		pasteMode.Foreground = Utils.GetBrush("ForegroundMenuSelected");
		apiMode.Foreground = Utils.GetBrush("ForegroundMain");
		modeTabControl.SelectedIndex = 1;
	}

	private void ApiMode_MouseDown(object sender, MouseButtonEventArgs e)
	{
		fileMode.Foreground = Utils.GetBrush("ForegroundMain");
		pasteMode.Foreground = Utils.GetBrush("ForegroundMain");
		apiMode.Foreground = Utils.GetBrush("ForegroundMenuSelected");
		modeTabControl.SelectedIndex = 2;
	}

	private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			locationListBox.Items.RemoveAt(locationListBox.SelectedIndex);
		}
		catch
		{
		}
	}

	private void Grid_MouseDown_1(object sender, MouseButtonEventArgs e)
	{
		try
		{
			locationListBox.Items.Clear();
		}
		catch
		{
		}
	}

	private void locationListBox_DragEnter(object sender, DragEventArgs e)
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

	private void locationListBox_Drop(object sender, DragEventArgs e)
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
						// Accept same extensions as the file dialog
						if (File.Exists(loc) && (loc.EndsWith(".txt") || loc.EndsWith(".csv")
							|| loc.EndsWith(".list") || loc.EndsWith(".dat")))
						{
							base.Dispatcher.Invoke(() => locationListBox.Items.Add(loc));
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

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialogaddproxies.xaml", UriKind.Relative);
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
			fileMode = (Label)target;
			fileMode.MouseDown += FileMode_MouseDown;
			break;
		case 2:
			pasteMode = (Label)target;
			pasteMode.MouseDown += PasteMode_MouseDown;
			break;
		case 3:
			apiMode = (Label)target;
			apiMode.MouseDown += ApiMode_MouseDown;
			break;
		case 4:
			modeTabControl = (TabControl)target;
			break;
		case 5:
			fileTab = (TabItem)target;
			break;
		case 6:
			locationListBox = (ListBox)target;
			locationListBox.DragEnter += locationListBox_DragEnter;
			locationListBox.Drop += locationListBox_Drop;
			break;
		case 7:
			loadProxiesButton = (Image)target;
			loadProxiesButton.MouseDown += loadProxiesButton_MouseDown;
			break;
		case 8:
			((Grid)target).MouseDown += Grid_MouseDown;
			break;
		case 9:
			((Grid)target).MouseDown += Grid_MouseDown_1;
			break;
		case 10:
			pasteTab = (TabItem)target;
			break;
		case 11:
			proxiesBox = (TextBox)target;
			break;
		case 12:
			apiTab = (TabItem)target;
			break;
		case 13:
			urlTextbox = (TextBox)target;
			break;
		case 14:
			advancedWarning = (TextBlock)target;
			break;
		case 15:
			proxyTypeCombobox = (ComboBox)target;
			break;
		case 16:
			usernameTextbox = (TextBox)target;
			break;
		case 17:
			passwordTextbox = (TextBox)target;
			break;
		case 18:
			acceptButton = (Button)target;
			acceptButton.Click += acceptButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
