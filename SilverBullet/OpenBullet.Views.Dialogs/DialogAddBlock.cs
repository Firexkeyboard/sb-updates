using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using OpenBullet.Views.Main.Configs;
using PluginFramework;
using RuriLib;

namespace OpenBullet.Views.Dialogs;

public class DialogAddBlock : Page, IComponentConnector
{
	internal Label defaultSetLabel;

	internal Label pluginsSetLabel;

	internal TabControl blockSetTabControl;

	internal Button blockRequestButton;

	internal Button blockUtilityButton;

	internal Button blockKeycheckButton;

	internal Button blockParseButton;

	internal Button blockFunctionButton;

	internal Button blockSolveCaptchaButton;

	internal Button blockReportCaptchaButton;

	internal Button blockBypassCFButton;

	internal Button blockTCPButton;

	internal Button blockOcrButton;

	internal Button blockNavigateButton;

	internal Button blockBrowserActionButton;

	internal Button blockElementActionButton;

	internal Button blockExecuteJSButton;

	internal Grid pluginsGrid;

	private Label  allSetLabel;
	private int    allTabIndex = -1;
	private Button _backArrowBtn;
	private Action _backAction;

	private bool _contentLoaded;

	public object Caller { get; set; }

	public DialogAddBlock(object caller)
	{
		InitializeComponent();
		Caller = caller;
		for (int i = 0; i < SB.BlockPlugins.Count; i += 3)
		{
			pluginsGrid.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
		}
		for (int j = 0; j < SB.BlockPlugins.Count; j++)
		{
			IBlockPlugin val = SB.BlockPlugins[j];
			Button button = new Button
			{
				Background = val.LinearGradientBrush
			};
			if (val.LightForeground)
			{
				button.Foreground = new SolidColorBrush(Colors.Gainsboro);
			}
			button.Content = val.Name;
			button.SetValue(Grid.ColumnProperty, j % 3);
			button.SetValue(Grid.RowProperty, j / 3);
			button.Tag = ((object)val).GetType();
			button.Click += pluginButton_Click;
			pluginsGrid.Children.Add(button);
		}

		// ── Inject "ALL" label next to Default / Plugins ──────────────────────
		allSetLabel = new Label
		{
			Content           = "ALL",
			Cursor            = Cursors.Hand,
			Foreground        = Utils.GetBrush("ForegroundMain"),
			Padding           = defaultSetLabel.Padding,
			VerticalAlignment = VerticalAlignment.Center
		};
		allSetLabel.MouseDown += allSetLabel_MouseDown;

		if (defaultSetLabel.Parent is Panel headerPanel)
		{
			int defaultIdx = headerPanel.Children.IndexOf(defaultSetLabel);
			headerPanel.Children.Insert(defaultIdx < 0 ? 0 : defaultIdx, allSetLabel);
		}

		// Back button — injected into the dialog's root Grid at the header row,
		// same Y position as "Current set:", pinned to the right edge
		_backArrowBtn = MakeBackButton();
		_backArrowBtn.Click += (_, __) => _backAction?.Invoke();

		// blockSetTabControl.Parent is the same Grid that contains the header row
		if (blockSetTabControl.Parent is Grid dlgGrid)
		{
			int tabRow    = Grid.GetRow(blockSetTabControl);
			int headerRow = tabRow > 0 ? tabRow - 1 : 0;
			Grid.SetRow(_backArrowBtn, headerRow);
			Grid.SetColumnSpan(_backArrowBtn, Math.Max(1, dlgGrid.ColumnDefinitions.Count));
			dlgGrid.Children.Add(_backArrowBtn);
		}
		else if (defaultSetLabel.Parent is Panel hp2)
		{
			hp2.Children.Add(_backArrowBtn);
		}

		// ── Build ALL tab ──────────────────────────────────────────────────────
		var allTabItem = new TabItem { Header = "", Content = BuildAllTab(), Visibility = Visibility.Collapsed };
		allTabIndex = blockSetTabControl.Items.Add(allTabItem);

		allSetLabel_MouseDown(this, null);

		// Lock window to Default-tab size (the largest content) so switching tabs never clips anything.
		// Switch to Default momentarily to measure, then return to ALL.
		Loaded += (_, __) => Dispatcher.BeginInvoke(
			System.Windows.Threading.DispatcherPriority.Loaded,
			new Action(() =>
			{
				var win = Window.GetWindow(this);
				if (win == null) return;
				// Measure with Default tab (index 0) — it has the most content
				blockSetTabControl.SelectedIndex = 0;
				win.UpdateLayout();
				win.SizeToContent = SizeToContent.Manual;
				win.Height        = win.ActualHeight;
				win.Width         = win.ActualWidth;
				// Return to ALL tab without flicker (already measured and locked)
				allSetLabel_MouseDown(this, null);
			}));
	}

	private void pluginButton_Click(object sender, RoutedEventArgs e)
	{
		Type type = (e.OriginalSource as Button).Tag as Type;
		object obj = Activator.CreateInstance(type);
		SendBack((BlockBase)((obj is BlockBase) ? obj : null));
	}

	private void blockRequestButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockRequest());
	}

	private void blockUtilityButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockUtility());
	}

	private void blockKeycheckButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockKeycheck());
	}

	private void blockParseButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockParse());
	}

	private void blockFunctionButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockFunction());
	}

	private void blockSolveCaptchaButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockSolveCaptcha());
	}

	private void blockReportCaptchaButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockReportCaptcha());
	}

	private void blockBypassCFButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockBypassCF());
	}

	private void blockTCPButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockTCP());
	}

	private void blockOcrButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockOcr());
	}

	private void blockSpeechToTextButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockSpeechToText());
	}

	private void blockWS_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new BlockWebSocket());
	}

	private void blockNavigateButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new SBlockNavigate());
	}

	private void blockBrowserActionButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new SBlockBrowserAction());
	}

	private void blockElementActionButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new SBlockElementAction());
	}

	private void blockExecuteJSButton_Click(object sender, RoutedEventArgs e)
	{
		SendBack((BlockBase)new SBlockExecuteJS());
	}

	private void SendBack(BlockBase block)
	{
		if (Caller.GetType() == typeof(Stacker))
		{
			((Stacker)Caller).AddBlock(block);
		}
		((MainDialog)base.Parent).Close();
	}

	private void HideBackArrow() { if (_backArrowBtn != null) _backArrowBtn.Visibility = Visibility.Collapsed; }

	private static Button MakeBackButton()
	{
		var btn = new Button
		{
			Content             = "◄",
			Width               = 26,
			Height              = 18,
			Visibility          = Visibility.Collapsed,
			Foreground          = new SolidColorBrush(Colors.White),
			BorderThickness     = new Thickness(0),
			Padding             = new Thickness(0),
			FontSize            = 11,
			FontWeight          = FontWeights.Bold,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Center,
			Margin              = new Thickness(0, 0, 6, 0),
			ToolTip             = "Back",
		};

		// Rounded corners via ControlTemplate (not possible with default Button style alone)
		var border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.CornerRadiusProperty,  new CornerRadius(4));
		border.SetValue(Border.BackgroundProperty,    new SolidColorBrush(Color.FromRgb(0xBB, 0x1C, 0x1C)));
		border.SetValue(Border.BorderThicknessProperty, new Thickness(0));
		var cp = new FrameworkElementFactory(typeof(ContentPresenter));
		cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		cp.SetValue(ContentPresenter.VerticalAlignmentProperty,   VerticalAlignment.Center);
		border.AppendChild(cp);
		var tpl = new ControlTemplate(typeof(Button)) { VisualTree = border };
		btn.Template = tpl;

		return btn;
	}

	private void defaultSetLabel_MouseDown(object sender, MouseButtonEventArgs e)
	{
		defaultSetLabel.Foreground = Utils.GetBrush("ForegroundMenuSelected");
		pluginsSetLabel.Foreground  = Utils.GetBrush("ForegroundMain");
		if (allSetLabel != null) allSetLabel.Foreground = Utils.GetBrush("ForegroundMain");
		HideBackArrow();
		blockSetTabControl.SelectedIndex = 0;
	}

	private void pluginsSetLabel_MouseDown(object sender, MouseButtonEventArgs e)
	{
		defaultSetLabel.Foreground = Utils.GetBrush("ForegroundMain");
		pluginsSetLabel.Foreground  = Utils.GetBrush("ForegroundMenuSelected");
		if (allSetLabel != null) allSetLabel.Foreground = Utils.GetBrush("ForegroundMain");
		HideBackArrow();
		blockSetTabControl.SelectedIndex = 1;
	}

	private void allSetLabel_MouseDown(object sender, MouseButtonEventArgs e)
	{
		defaultSetLabel.Foreground = Utils.GetBrush("ForegroundMain");
		pluginsSetLabel.Foreground  = Utils.GetBrush("ForegroundMain");
		allSetLabel.Foreground      = Utils.GetBrush("ForegroundMenuSelected");
		HideBackArrow();
		blockSetTabControl.SelectedIndex = allTabIndex;
	}

	private void allBlockButton_Click(object sender, RoutedEventArgs e)
	{
		Type type = (e.OriginalSource as Button).Tag as Type;
		SendBack(Activator.CreateInstance(type) as BlockBase);
	}

	private static string GetBlockDisplayName(Type t)
	{
		try { return (Activator.CreateInstance(t) as BlockBase)?.Label ?? t.Name; }
		catch { return t.Name; }
	}

	// ── OB2-style category grid ────────────────────────────────────────────────

	private UIElement BuildAllTab()
	{
		var root = new Grid();

		// Category page — visible by default
		var catScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		var catGrid   = Make3ColGrid(margin: 4);
		catScroll.Content = catGrid;

		// Block page — shown when a category is clicked
		var blockScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Visibility = Visibility.Collapsed };
		var blockStack  = new StackPanel();
		blockScroll.Content = blockStack;

		var registered = new HashSet<Type>(SB.BlockMappings.Select(m => m.Item1));

		// Categories shown in the Add Block dialog
		var cats = new (string Name, Color Color, Type[] Types)[]
		{
			("Requests",   Color.FromRgb(0x4C, 0xAF, 0x50), new[] { typeof(BlockRequest), typeof(BlockBypassCF), typeof(BlockDns) }),
			("Parsing",    Color.FromRgb(0xD4, 0xB8, 0x20), new[] { typeof(BlockParse) }),
			("Conditions", Color.FromRgb(0x5B, 0x8E, 0xDB), new[] { typeof(BlockKeycheck) }),
			("Functions",  Color.FromRgb(0x96, 0xCC, 0x5A), new[] { typeof(BlockFunction) }),
			("Solvers",    Color.FromRgb(0xDA, 0xA5, 0x20), new[] { typeof(BlockCapMonster), typeof(BlockTurnstile), typeof(BlockAltcha), typeof(BlockFriendlyCaptcha), typeof(BlockRecaptchaV3), typeof(BlockRecaptchaV2Invisible), typeof(BlockRecaptchaV3Bypass), typeof(BlockAkmCookies), typeof(BlockDataDome), typeof(BlockCfClearance) }),
			("Selenium",   Color.FromRgb(0x6E, 0x8B, 0x3D), new[] { typeof(SBlockNavigate), typeof(SBlockBrowserAction), typeof(SBlockElementAction), typeof(SBlockExecuteJS) }),
			("Captchas",   Color.FromRgb(0x5B, 0xD5, 0xE0), new[] { typeof(BlockSolveCaptcha), typeof(BlockReportCaptcha) }),
			("Interop",    Color.FromRgb(0xC2, 0x7E, 0xA7), new[] { typeof(BlockTCP), typeof(BlockWebSocket) }),
			("Utility",    Color.FromRgb(0xE8, 0xC9, 0x9B), new[] { typeof(BlockUtility), typeof(BlockOcr), typeof(BlockSpeechToText) }),
		};

		int catCol = 0;
		foreach (var (catName, color, allTypes) in cats)
		{
			var types = allTypes.Where(t => registered.Contains(t)).ToArray();
			if (types.Length == 0) continue;

			var btn = MakeBtn(catName.ToUpperInvariant(), new SolidColorBrush(color));
			var captured = types;
			btn.Click += (_, __) => ShowBlockCategory(captured, catScroll, blockScroll, blockStack);
			GridAddCell(catGrid, btn, ref catCol);
		}

		if (SB.BlockPlugins.Count > 0)
		{
			var plugTypes = SB.BlockPlugins.Select(p => ((object)p).GetType()).ToArray();
			var btn = MakeBtn("PLUGINS", new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)));
			btn.Click += (_, __) => ShowBlockCategory(plugTypes, catScroll, blockScroll, blockStack);
			GridAddCell(catGrid, btn, ref catCol);
		}

		root.Children.Add(catScroll);
		root.Children.Add(blockScroll);
		return root;
	}

	// Builds a 3-column * / * / * Grid identical to pluginsGrid layout
	private static Grid Make3ColGrid(int margin = 2)
	{
		var g = new Grid { Margin = new Thickness(margin) };
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		return g;
	}

	// Adds an element to the next cell (tracks col externally)
	private static void GridAddCell(Grid g, UIElement el, ref int col)
	{
		if (col == 0)
			g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		int row = g.RowDefinitions.Count - 1;
		Grid.SetRow(el, row);
		Grid.SetColumn(el, col);
		g.Children.Add(el);
		col = (col + 1) % 3;
	}

	// Single button factory — no explicit Height/Font so the app theme controls size (matches Default tab)
	private static Button MakeBtn(string label, Brush bg) => new Button
	{
		Content    = label,
		Margin     = new Thickness(2),
		Background = bg,
	};

	private void ShowBlockCategory(Type[] types, UIElement catPage, UIElement blockPage, StackPanel blockStack)
	{
		catPage.Visibility   = Visibility.Collapsed;
		blockPage.Visibility = Visibility.Visible;

		blockStack.Children.Clear();

		if (_backArrowBtn != null)
		{
			_backArrowBtn.Visibility = Visibility.Visible;
			_backAction = () =>
			{
				blockPage.Visibility     = Visibility.Collapsed;
				catPage.Visibility       = Visibility.Visible;
				_backArrowBtn.Visibility = Visibility.Collapsed;
			};
		}

		var blockGrid = Make3ColGrid(margin: 4);
		int col = 0;
		foreach (var t in types)
		{
			var mapping = SB.BlockMappings.FirstOrDefault(m => m.Item1 == t);
			Brush brush  = mapping.Item3
			            ?? SB.BlockPlugins.FirstOrDefault(p => ((object)p).GetType() == t)?.LinearGradientBrush
			            ?? (Brush)new SolidColorBrush(Colors.DimGray);

			var btn = MakeBtn(GetBlockDisplayName(t).ToUpperInvariant(), brush);
			btn.Tag    = t;
			btn.Click += allBlockButton_Click;
			GridAddCell(blockGrid, btn, ref col);
		}
		blockStack.Children.Add(blockGrid);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialogaddblock.xaml", UriKind.Relative);
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
			defaultSetLabel = (Label)target;
			defaultSetLabel.MouseDown += defaultSetLabel_MouseDown;
			break;
		case 2:
			pluginsSetLabel = (Label)target;
			pluginsSetLabel.MouseDown += pluginsSetLabel_MouseDown;
			break;
		case 3:
			blockSetTabControl = (TabControl)target;
			break;
		case 4:
			blockRequestButton = (Button)target;
			blockRequestButton.Click += blockRequestButton_Click;
			break;
		case 5:
			blockUtilityButton = (Button)target;
			blockUtilityButton.Click += blockUtilityButton_Click;
			break;
		case 6:
			blockKeycheckButton = (Button)target;
			blockKeycheckButton.Click += blockKeycheckButton_Click;
			break;
		case 7:
			blockParseButton = (Button)target;
			blockParseButton.Click += blockParseButton_Click;
			break;
		case 8:
			blockFunctionButton = (Button)target;
			blockFunctionButton.Click += blockFunctionButton_Click;
			break;
		case 9:
			blockSolveCaptchaButton = (Button)target;
			blockSolveCaptchaButton.Click += blockSolveCaptchaButton_Click;
			break;
		case 10:
			blockReportCaptchaButton = (Button)target;
			blockReportCaptchaButton.Click += blockReportCaptchaButton_Click;
			break;
		case 11:
			blockBypassCFButton = (Button)target;
			blockBypassCFButton.Click += blockBypassCFButton_Click;
			break;
		case 12:
			blockTCPButton = (Button)target;
			blockTCPButton.Click += blockTCPButton_Click;
			break;
		case 13:
			blockOcrButton = (Button)target;
			blockOcrButton.Click += blockOcrButton_Click;
			break;
		case 14:
			((Button)target).Click += blockWS_Click;
			break;
		case 15:
			blockNavigateButton = (Button)target;
			blockNavigateButton.Click += blockNavigateButton_Click;
			break;
		case 16:
			blockBrowserActionButton = (Button)target;
			blockBrowserActionButton.Click += blockBrowserActionButton_Click;
			break;
		case 17:
			blockElementActionButton = (Button)target;
			blockElementActionButton.Click += blockElementActionButton_Click;
			break;
		case 18:
			blockExecuteJSButton = (Button)target;
			blockExecuteJSButton.Click += blockExecuteJSButton_Click;
			break;
		case 19:
			pluginsGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
