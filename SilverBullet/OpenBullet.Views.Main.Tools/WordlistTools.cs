using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Win32;
using RuriLib;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;

namespace OpenBullet.Views.Main.Tools;

public class WordlistTools : Page, IComponentConnector
{
	// ── BAML connects (kept for compatibility) ───────────────────────────────
	private string recognizeWordlistType;
	private string wordlistName;
	private List<string> wordList = new List<string>();
	internal TextBox locTextBox;
	internal TextBlock removedDup;
	internal TextBox splitTextBox;
	internal IntegerUpDown splitIndex;
	internal TextBlock splited;
	internal TextBox currentSepTextBox;
	internal TextBox newSepTextBox;
	internal TextBlock changed;
	internal TextBlock loaded;
	private bool _contentLoaded;

	// ── CREDENTIAL AUDITOR STATE ─────────────────────────────────────────────
	private TextBox     _urlBox;
	private TextBox     _pathBox;
	private TextBox     _logBox;
	private TextBox     _minPassBox;
	private ProgressBar _progressBar;
	private TextBlock   _progressTb;
	private TextBlock   _foundTb;
	private TextBlock   _errorsTb;
	private Button      _runBtn;
	private Button      _stopBtn;
	private Button      _openBtn;
	private string      _lastResultDir;
	private CancellationTokenSource _cts;

	// ── DEDUP STATE ──────────────────────────────────────────────────────────
	private TextBox   _dedupPathBox;
	private TextBlock _dedupResultTb;
	private Button    _dedupBtn;

	private string _fmtSelected = "user";
	private string _sepSelected = ":";
	private readonly Dictionary<string, Border>    _fmtBorders = new();
	private readonly Dictionary<string, TextBlock> _fmtTexts   = new();
	private readonly Dictionary<string, Border>    _sepBorders = new();
	private readonly Dictionary<string, TextBlock> _sepTexts   = new();

	// ── COLORS ───────────────────────────────────────────────────────────────
	private static SolidColorBrush C(byte r, byte g, byte b) =>
		new SolidColorBrush(Color.FromRgb(r, g, b));

	private static readonly SolidColorBrush Orange     = C(230, 100,  0);
	private static readonly SolidColorBrush OrangeDim  = C(70,  34,   4);
	private static readonly SolidColorBrush OrangeBrd  = C(200,  85,  5);
	private static readonly SolidColorBrush TagBg      = C(42,  42,  42);
	private static readonly SolidColorBrush TagBrd     = C(62,  62,  62);
	private static readonly SolidColorBrush TextMain   = C(230, 230, 230);
	private static readonly SolidColorBrush TextMuted  = C(150, 150, 150);
	private static readonly SolidColorBrush InputBg    = C(22,  22,  22);
	private static readonly SolidColorBrush InputBrd   = C(52,  52,  52);
	private static readonly SolidColorBrush PageBg     = C(15,  15,  15);
	private static readonly SolidColorBrush CardBg     = C(24,  24,  24);
	private static readonly SolidColorBrush CardBrd    = C(38,  38,  38);
	private static readonly SolidColorBrush LogBg      = C(16,  16,  16);
	private static readonly SolidColorBrush Green      = C(72,  200, 90);
	private static readonly SolidColorBrush Amber      = C(215, 145, 50);
	private static readonly SolidColorBrush Transparent = new SolidColorBrush(Colors.Transparent);

	// ── CONSTRUCTOR ──────────────────────────────────────────────────────────
	public WordlistTools()
	{
		InitializeComponent();
		try { BuildUI(); } catch { }
	}

	// ── FULL PAGE BUILD ──────────────────────────────────────────────────────
	private void BuildUI()
	{
		this.Content    = null;
		this.Background = Brushes.Transparent;

		var scroll = new ScrollViewer
		{
			VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Background = Brushes.Transparent
		};
		this.Content = scroll;

		var page = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(20, 18, 20, 20) };
		scroll.Content = page;

		// Resize handler: log box grows proportionally with the page height
		this.SizeChanged += (s, e) =>
		{
			if (_logBox == null) return;
			_logBox.Height = Math.Max(70, this.ActualHeight * 0.20);
		};

		// ════════════════════════════════════════════════════════════════════
		// SECTION 1 — CREDENTIAL AUDITOR
		// ════════════════════════════════════════════════════════════════════
		var c1 = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 20) };
		page.Children.Add(c1);

		// ── Title row ────────────────────────────────────────────────────────
		var titleRow = new DockPanel { Margin = new Thickness(0, 0, 0, 16), LastChildFill = false };
		var auditTitle = new TextBlock
		{
			Text = "CREDENTIAL AUDITOR", Foreground = Brushes.White, FontSize = 12,
			FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center
		};
		DockPanel.SetDock(auditTitle, Dock.Left);
		titleRow.Children.Add(auditTitle);

		var ulpInner = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
		ulpInner.Children.Add(new System.Windows.Shapes.Path
		{
			Data = Geometry.Parse("M9.78,18.65 L10.06,14.42 L17.74,7.5 C18.08,7.19 17.67,7.04 17.22,7.31 L7.74,13.3 L3.64,12 C2.76,11.75 2.75,11.14 3.84,10.7 L19.81,4.54 C20.54,4.21 21.24,4.72 20.96,5.84 L18.24,18.65 C18.05,19.56 17.5,19.78 16.74,19.36 L12.6,16.3 L10.61,18.23 C10.38,18.46 10.19,18.65 9.78,18.65 Z"),
			Fill = Brushes.White, Stretch = Stretch.Uniform, Width = 14, Height = 14,
			Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center
		});
		ulpInner.Children.Add(new StackPanel
		{
			Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center,
			Children =
			{
				new TextBlock { Text = "JOIN FREE ULP",         FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = Brushes.White },
				new TextBlock { Text = "@fenixulpsearcher_bot", FontSize = 8,    Foreground = new SolidColorBrush(Color.FromRgb(160, 210, 255)), Margin = new Thickness(0, 1, 0, 0) },
			}
		});
		var ulpBanner = new Border
		{
			Background = new SolidColorBrush(Color.FromRgb(15, 110, 200)),
			BorderBrush = new SolidColorBrush(Color.FromRgb(40, 150, 255)),
			BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5),
			Padding = new Thickness(10, 5, 10, 5), Cursor = Cursors.Hand, Child = ulpInner,
			Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(30, 130, 255), BlurRadius = 10, ShadowDepth = 0, Opacity = 0.4 }
		};
		ulpBanner.MouseLeftButtonUp += (s, e) => { try { Process.Start(new ProcessStartInfo("https://t.me/fenixulpsearcher_bot") { UseShellExecute = true }); } catch { } };
		ulpBanner.MouseEnter += (s, e) => { ulpBanner.Background = new SolidColorBrush(Color.FromRgb(25, 135, 225)); };
		ulpBanner.MouseLeave += (s, e) => { ulpBanner.Background = new SolidColorBrush(Color.FromRgb(15, 110, 200)); };
		DockPanel.SetDock(ulpBanner, Dock.Right);
		titleRow.Children.Add(ulpBanner);
		c1.Children.Add(titleRow);

		// ── URL ──────────────────────────────────────────────────────────────
		c1.Children.Add(FieldLabel("Target URLs  —  one or more, separated by comma"));
		_urlBox = InputBox();
		c1.Children.Add(_urlBox);

		// ── Input Path ────────────────────────────────────────────────────────
		c1.Children.Add(FieldLabel("Input Path  —  file or folder with logs"));
		var pathRow = new Grid { Margin = new Thickness(0, 0, 0, 16) };
		pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_pathBox = new TextBox
		{
			Background = InputBg, Foreground = TextMain, BorderBrush = InputBrd, BorderThickness = new Thickness(1),
			Padding = new Thickness(10, 7, 10, 7), FontSize = 11.5, CaretBrush = Brushes.White,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_pathBox, 0); pathRow.Children.Add(_pathBox);
		var btnFile   = SmallBtn("📄  File");
		var btnFolder = SmallBtn("📂  Folder");
		btnFile.Margin = new Thickness(6, 0, 0, 0); btnFolder.Margin = new Thickness(5, 0, 0, 0);
		btnFile.Click += (s, e) => { var d = new OpenFileDialog { Filter = "Text files|*.txt|All files|*.*" }; if (d.ShowDialog() == true) _pathBox.Text = d.FileName; };
		btnFolder.Click += (s, e) => { var d = new System.Windows.Forms.FolderBrowserDialog(); if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) _pathBox.Text = d.SelectedPath; };
		Grid.SetColumn(btnFile, 1); Grid.SetColumn(btnFolder, 2);
		pathRow.Children.Add(btnFile); pathRow.Children.Add(btnFolder);
		c1.Children.Add(pathRow);

		// ── Output Format ─────────────────────────────────────────────────────
		c1.Children.Add(FieldLabel("Output Format"));
		c1.Children.Add(MakeTagRow(_fmtBorders, _fmtTexts, ref _fmtSelected,
			("user : password", "user"), ("email : password", "email"),
			("phone : password", "phone"), ("ID : password", "id"), ("login : password", "login")));

		// ── Separator + Min pass ──────────────────────────────────────────────
		var optRow = new Grid { Margin = new Thickness(0, 14, 0, 16) };
		optRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		optRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24, GridUnitType.Pixel) });
		optRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var sepBlock = new StackPanel();
		sepBlock.Children.Add(FieldLabel("Separator in output file"));
		sepBlock.Children.Add(MakeTagRow(_sepBorders, _sepTexts, ref _sepSelected, ("Colon  (:)", ":"), ("Semicolon  (;)", ";")));
		Grid.SetColumn(sepBlock, 0); optRow.Children.Add(sepBlock);
		var minPassBlock = new StackPanel();
		minPassBlock.Children.Add(FieldLabel("Min. pass length"));
		_minPassBox = new TextBox
		{
			Text = "4", Background = InputBg, Foreground = TextMain, BorderBrush = InputBrd,
			BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6), FontSize = 12,
			CaretBrush = Brushes.White, VerticalContentAlignment = VerticalAlignment.Center,
			Width = 56, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 6, 0, 0)
		};
		minPassBlock.Children.Add(_minPassBox);
		Grid.SetColumn(minPassBlock, 2); optRow.Children.Add(minPassBlock);
		c1.Children.Add(optRow);

		// ── Action buttons ────────────────────────────────────────────────────
		var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
		_runBtn = new Button
		{
			Content = "▶   Run Auditor", Background = C(55, 55, 55), Foreground = Brushes.White,
			BorderBrush = C(90, 90, 90), BorderThickness = new Thickness(1),
			Padding = new Thickness(22, 9, 22, 9),
			FontSize = 12, FontWeight = FontWeights.SemiBold, Cursor = Cursors.Hand
		};
		_runBtn.Click += RunAudit; btnRow.Children.Add(_runBtn);
		_stopBtn = new Button
		{
			Content = "■   Stop", Background = C(55, 12, 12), Foreground = C(210, 100, 100),
			BorderBrush = C(95, 22, 22), BorderThickness = new Thickness(1),
			Padding = new Thickness(16, 9, 16, 9), FontSize = 12, Cursor = Cursors.Hand,
			Margin = new Thickness(8, 0, 0, 0), IsEnabled = false
		};
		_stopBtn.Click += (s, e) => _cts?.Cancel(); btnRow.Children.Add(_stopBtn);
		c1.Children.Add(btnRow);

		// ── Progress (hidden) ─────────────────────────────────────────────────
		var progRow = new Grid { Margin = new Thickness(0, 0, 0, 10), Visibility = Visibility.Collapsed };
		progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_progressBar = new ProgressBar
		{
			Minimum = 0, Maximum = 100, Value = 0, Height = 5, Foreground = Orange,
			Background = C(40, 40, 40), BorderThickness = new Thickness(0), VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_progressBar, 0); progRow.Children.Add(_progressBar);
		_progressTb = new TextBlock { Foreground = TextMuted, FontSize = 10, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(_progressTb, 1); progRow.Children.Add(_progressTb);
		_progressBar.Tag = progRow;
		c1.Children.Add(progRow);

		// ── Log (height set dynamically via SizeChanged) ──────────────────────
		_logBox = new TextBox
		{
			Background                    = LogBg,
			Foreground                    = C(155, 162, 200),
			BorderBrush                   = CardBrd,
			BorderThickness               = new Thickness(1),
			FontFamily                    = new FontFamily("Consolas"),
			FontSize                      = 10.5,
			IsReadOnly                    = true,
			TextWrapping                  = TextWrapping.NoWrap,
			VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			Height                        = 90,
			Padding                       = new Thickness(10),
			Margin                        = new Thickness(0, 0, 0, 12)
		};
		c1.Children.Add(_logBox);

		// ── Status bar ────────────────────────────────────────────────────────
		var statusRow = new Grid();
		statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_foundTb = new TextBlock { Text = "Found: 0", Foreground = Green, FontSize = 11, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(_foundTb, 0); statusRow.Children.Add(_foundTb);
		var statusDivider = new TextBlock { Text = "   │   ", Foreground = C(65, 65, 65), VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(statusDivider, 1); statusRow.Children.Add(statusDivider);
		_errorsTb = new TextBlock { Text = "Errors: 0", Foreground = Amber, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(_errorsTb, 2); statusRow.Children.Add(_errorsTb);
		_openBtn = new Button
		{
			Content = "📁  Open Results", Background = C(36, 36, 36), Foreground = C(180, 180, 180),
			BorderBrush = CardBrd, BorderThickness = new Thickness(1), Padding = new Thickness(14, 5, 14, 5),
			FontSize = 11, Cursor = Cursors.Hand, IsEnabled = false
		};
		_openBtn.Click += (s, e) => { if (_lastResultDir != null && Directory.Exists(_lastResultDir)) Process.Start(new ProcessStartInfo("explorer.exe", _lastResultDir) { UseShellExecute = true }); };
		Grid.SetColumn(_openBtn, 3); statusRow.Children.Add(_openBtn);
		c1.Children.Add(statusRow);

		// Add a thin separator between the two sections
		page.Children.Add(new Border
		{
			Height = 1, Background = C(55, 55, 55), Margin = new Thickness(0, 0, 0, 20)
		});

		// ════════════════════════════════════════════════════════════════════
		// SECTION 2 — REMOVE DUPLICATES
		// ════════════════════════════════════════════════════════════════════
		var c2 = new StackPanel { Orientation = Orientation.Vertical };
		page.Children.Add(c2);

		var dedupTitleRow = new DockPanel { Margin = new Thickness(0, 0, 0, 16), LastChildFill = true };
		var dedupTitle2 = new TextBlock
		{
			Text = "REMOVE DUPLICATES", Foreground = Brushes.White, FontSize = 12,
			FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center
		};
		DockPanel.SetDock(dedupTitle2, Dock.Left);
		dedupTitleRow.Children.Add(dedupTitle2);
		dedupTitleRow.Children.Add(new TextBlock
		{
			Text = "Remove duplicate lines from any text file", Foreground = TextMuted, FontSize = 10,
			VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(12, 0, 0, 0)
		});
		c2.Children.Add(dedupTitleRow);

		c2.Children.Add(FieldLabel("File path"));
		var dedupPathRow = new Grid { Margin = new Thickness(0, 0, 0, 14) };
		dedupPathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		dedupPathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_dedupPathBox = new TextBox
		{
			Background = InputBg, Foreground = TextMain, BorderBrush = InputBrd, BorderThickness = new Thickness(1),
			Padding = new Thickness(10, 7, 10, 7), FontSize = 11.5, CaretBrush = Brushes.White,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_dedupPathBox, 0); dedupPathRow.Children.Add(_dedupPathBox);
		var dedupBrowse = SmallBtn("📄  Browse");
		dedupBrowse.Margin = new Thickness(6, 0, 0, 0);
		dedupBrowse.Click += (s, e) => { var d = new OpenFileDialog { Filter = "Text files|*.txt|All files|*.*" }; if (d.ShowDialog() == true) _dedupPathBox.Text = d.FileName; };
		Grid.SetColumn(dedupBrowse, 1); dedupPathRow.Children.Add(dedupBrowse);
		c2.Children.Add(dedupPathRow);

		var dedupBtnRow = new DockPanel();
		_dedupBtn = new Button
		{
			Content = "⊘   Remove Duplicates", Background = C(55, 55, 55), Foreground = Brushes.White,
			BorderBrush = C(90, 90, 90), BorderThickness = new Thickness(1),
			Padding = new Thickness(22, 9, 22, 9),
			FontSize = 12, FontWeight = FontWeights.SemiBold, Cursor = Cursors.Hand
		};
		_dedupBtn.Click += RunDedup;
		DockPanel.SetDock(_dedupBtn, Dock.Left); dedupBtnRow.Children.Add(_dedupBtn);
		_dedupResultTb = new TextBlock
		{
			Foreground = TextMuted, FontSize = 11, VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(16, 0, 0, 0)
		};
		dedupBtnRow.Children.Add(_dedupResultTb);
		c2.Children.Add(dedupBtnRow);
	}


	// ── TAG GROUP (replaces RadioButtons) ────────────────────────────────────
	private WrapPanel MakeTagRow(
		Dictionary<string, Border>    borders,
		Dictionary<string, TextBlock> texts,
		ref string selected,
		params (string label, string val)[] opts)
	{
		string initial = opts[0].val;
		selected = initial;
		var panel = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };

		foreach (var (label, val) in opts)
		{
			var tb = new TextBlock { Text = label, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
			var b  = new Border
			{
				Padding         = new Thickness(14, 6, 14, 6),
				CornerRadius    = new CornerRadius(5),
				Cursor          = Cursors.Hand,
				Margin          = new Thickness(0, 0, 6, 0),
				Child           = tb
			};
			borders[val] = b;
			texts[val]   = tb;
			panel.Children.Add(b);
		}

		// Apply initial active style
		ApplyTagStyles(borders, texts, initial);

		// Wire click handlers
		foreach (var (_, val) in opts)
		{
			string capturedVal       = val;
			borders[val].MouseLeftButtonUp += (s, e) =>
			{
				// Update the captured ref via the field directly
				if (borders == _fmtBorders) _fmtSelected = capturedVal;
				else                         _sepSelected = capturedVal;
				ApplyTagStyles(borders, texts, capturedVal);
			};
		}

		return panel;
	}

	private void ApplyTagStyles(
		Dictionary<string, Border>    borders,
		Dictionary<string, TextBlock> texts,
		string active)
	{
		foreach (var (val, b) in borders)
		{
			bool on = val == active;
			b.Background      = on ? C(58, 58, 58) : TagBg;
			b.BorderBrush     = on ? C(160, 160, 160) : TagBrd;
			b.BorderThickness = new Thickness(1);
			texts[val].Foreground = on ? Brushes.White : TextMuted;
			texts[val].FontWeight = on ? FontWeights.SemiBold : FontWeights.Normal;
		}
	}

	// ── UI HELPERS ───────────────────────────────────────────────────────────
	private static TextBlock FieldLabel(string text) => new TextBlock
	{
		Text       = text,
		Foreground = TextMuted,
		FontSize   = 10,
		Margin     = new Thickness(0, 0, 0, 5)
	};

	private static TextBox InputBox(string placeholder = "") => new TextBox
	{
		Background               = InputBg,
		Foreground               = TextMain,
		BorderBrush              = InputBrd,
		BorderThickness          = new Thickness(1),
		Padding                  = new Thickness(10, 7, 10, 7),
		FontSize                 = 11.5,
		CaretBrush               = Brushes.White,
		VerticalContentAlignment = VerticalAlignment.Center,
		Margin                   = new Thickness(0, 0, 0, 16)
	};

	private static Button SmallBtn(string text) => new Button
	{
		Content         = text,
		Background      = C(45, 45, 45),
		Foreground      = C(210, 210, 210),
		BorderBrush     = C(70, 70, 70),
		BorderThickness = new Thickness(1),
		Padding         = new Thickness(12, 7, 12, 7),
		FontSize        = 11,
		Cursor          = Cursors.Hand,
		VerticalAlignment = VerticalAlignment.Center
	};

	// ── RUN AUDIT ────────────────────────────────────────────────────────────
	private static string NormalizeUrl(string raw)
	{
		string u = raw
			.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
			.Replace("http://",  "", StringComparison.OrdinalIgnoreCase)
			.TrimStart('/').TrimEnd('/');
		if (u.StartsWith("www.", StringComparison.OrdinalIgnoreCase) && u.Length > 4)
			u = u[4..];
		return u;
	}

	private static string SafeFileName(string url)
	{
		string s = Regex.Replace(
			url.Replace("://", "_").Replace("/", "_").Replace("\\", "_"),
			@"[^a-zA-Z0-9._-]+", "_");
		return s.Length > 180 ? s[..180] : s;
	}

	private async void RunAudit(object sender, RoutedEventArgs e)
	{
		// ── 1. Parse & normalize URLs (comma-separated) ───────────────────────
		string[] urls = _urlBox.Text
			.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(u => NormalizeUrl(u.Trim()))
			.Where(u => u.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		if (urls.Length == 0)
		{
			AppendLog("Target URL is empty.", "ERROR");
			return;
		}

		string path = _pathBox.Text.Trim().Trim('"');
		if (!File.Exists(path) && !Directory.Exists(path))
		{
			AppendLog($"Path not found: {path}", "ERROR");
			return;
		}

		// ── 2. Min password length ────────────────────────────────────────────
		int minPassLen = int.TryParse(_minPassBox.Text.Trim(), out int mp) && mp >= 1 ? mp : 4;

		string fmt = _fmtSelected;
		string sep = _sepSelected;

		_logBox.Clear();
		_foundTb.Text      = "Found: 0";
		_errorsTb.Text     = "Error lines: 0";
		_runBtn.IsEnabled  = false;
		_stopBtn.IsEnabled = true;
		_openBtn.IsEnabled = false;

		_cts?.Cancel();
		_cts = new CancellationTokenSource();
		var ct = _cts.Token;

		AppendLog($"URLs: {string.Join("  |  ", urls)}  ·  Format: {fmt}  ·  Sep: '{sep}'  ·  Min pass: {minPassLen}", "INFO");

		int found = 0, errCount = 0;
		string resultDir = null;

		var progRow = (Grid)_progressBar.Tag;
		progRow.Visibility = Visibility.Visible;
		_progressBar.Value = 0;
		_progressTb.Text   = "0 / 0";

		var skipTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{ "http", "https", "ftp", "ftps", "URL", "LOGIN", "USER", "PASS",
			  "PASSWORD", "PROXY", "STATUS", "SUCCESS", "FAIL", "CAPTURE" };

		await Task.Run(() =>
		{
			try
			{
				var rxEmail    = new Regex(@"([a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,})\s*[:;]\s*([^\s|]+)", RegexOptions.Compiled);
				var rxId       = new Regex(@"(?<!\w)(\d{8}[a-zA-Z]|[XYZxyz]\d{7}[a-zA-Z]|[A-Z]\d{8})(?!\w)\s*[:;]\s*([^\s|]+)", RegexOptions.Compiled);
				var rxPhone    = new Regex(@"(?<![a-zA-Z@])(\+?\d{7,15})(?![a-zA-Z@\d])\s*[:;]\s*([^\s|]+)", RegexOptions.Compiled);
				var rxUser     = new Regex(@"([^\s:;|/][^\s:;|]*)\s*[:;]\s*([^\s|]+)", RegexOptions.Compiled);
				var rxTriple   = new Regex(@"([^\s:;]+)\s*[:;]\s*([^\s:;]+)\s*[:;]\s*([^\s|]+)", RegexOptions.Compiled);
				var rxCatchAll = new Regex(@"([^\s:;|]+)\s*[:;]\s*([^\s|]+)", RegexOptions.Compiled);
				Regex rx = fmt switch { "email" => rxEmail, "id" => rxId, "phone" => rxPhone, _ => rxUser };

				// One HashSet per URL
				var perUrl = urls.ToDictionary(
					u => u,
					_ => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
					StringComparer.OrdinalIgnoreCase);
				var errors = new List<string>();

				string[] files = File.Exists(path)
					? new[] { path }
					: Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToArray();

				AppendLog($"Scanning {files.Length} file(s)…", "INFO");
				Dispatcher.InvokeAsync(() => { _progressBar.Maximum = files.Length; _progressTb.Text = $"0 / {files.Length}"; });

				for (int fi = 0; fi < files.Length; fi++)
				{
					if (ct.IsCancellationRequested) break;
					string file = files[fi];

					int fCap = fi + 1;
					Dispatcher.InvokeAsync(() => { _progressBar.Value = fCap; _progressTb.Text = $"{fCap} / {files.Length}"; });

					if (fi == 0 || fi == files.Length - 1 || (fi + 1) % 100 == 0)
						AppendLog($"[{fi + 1}/{files.Length}]  {Path.GetFileName(file)}", "INFO");

					try
					{
						string[] fileLines;
						try { fileLines = File.ReadAllLines(file, new UTF8Encoding(false, true)); }
						catch (DecoderFallbackException)
						{
							fileLines = File.ReadAllLines(file, Encoding.Default);
							AppendLog($"  ↳ encoding fallback (ANSI)  {Path.GetFileName(file)}", "INFO");
						}

						int lineNo = 0;
						foreach (string rawLine in fileLines)
						{
							if (ct.IsCancellationRequested) break;
							lineNo++;

							// Find which URL this line belongs to (first match wins)
							string mu = null;
							foreach (string u in urls)
								if (rawLine.Contains(u, StringComparison.OrdinalIgnoreCase)) { mu = u; break; }
							if (mu == null) continue;

							var tgt  = perUrl[mu];
							string line = rawLine.Trim();
							bool hit = false;

							if (fmt == "user")
							{
								foreach (Match m in rxTriple.Matches(line))
								{
									string g1 = m.Groups[1].Value.Trim(), g2 = m.Groups[2].Value.Trim(), g3 = m.Groups[3].Value.Trim();
									if (g1.Contains(mu, StringComparison.OrdinalIgnoreCase))
									{
										if (g2.Contains('/') || g2.Contains('@') || skipTokens.Contains(g2)) continue;
										tgt.Add($"{g2}{sep}{g3}"); hit = true;
									}
									else if (g2.Contains(mu, StringComparison.OrdinalIgnoreCase))
									{
										int ci = g3.IndexOfAny(new[] { ':', ';' });
										if (ci > 0) { string u2 = g3[..ci].Trim(), p2 = g3[(ci+1)..].Trim();
											if (!u2.Contains('/') && !u2.Contains('@') && !skipTokens.Contains(u2) && p2.Length > 0)
											{ tgt.Add($"{u2}{sep}{p2}"); hit = true; } }
									}
								}
								if (!hit) foreach (Match m in rxUser.Matches(line))
								{
									string u = m.Groups[1].Value.Trim(), p = m.Groups[2].Value.Trim();
									if (u.Contains('@') || u.Contains('/') || skipTokens.Contains(u)) continue;
									if (mu.Contains(u, StringComparison.OrdinalIgnoreCase)) continue;
									if (p.StartsWith("//") || p.Contains("://")) continue;
									tgt.Add($"{u}{sep}{p}"); hit = true;
								}
							}
							else if (fmt == "login")
							{
								foreach (Match m in rxTriple.Matches(line))
								{
									string g1 = m.Groups[1].Value.Trim(), g2 = m.Groups[2].Value.Trim(), g3 = m.Groups[3].Value.Trim();
									if (g1.Contains(mu, StringComparison.OrdinalIgnoreCase))
									{ if (!skipTokens.Contains(g2)) { tgt.Add($"{g2}{sep}{g3}"); hit = true; } }
									else if (g2.Contains(mu, StringComparison.OrdinalIgnoreCase))
									{
										int ci = g3.IndexOfAny(new[] { ':', ';' });
										if (ci > 0) { string u2 = g3[..ci].Trim(), p2 = g3[(ci+1)..].Trim();
											if (!skipTokens.Contains(u2) && p2.Length > 0) { tgt.Add($"{u2}{sep}{p2}"); hit = true; } }
									}
								}
								if (!hit) foreach (Match m in rxCatchAll.Matches(line))
								{
									string u = m.Groups[1].Value.Trim(), p = m.Groups[2].Value.Trim();
									if (skipTokens.Contains(u)) continue;
									if (mu.Contains(u, StringComparison.OrdinalIgnoreCase)) continue;
									if (p.StartsWith("//") || p.Contains("://")) continue;
									tgt.Add($"{u}{sep}{p}"); hit = true;
								}
							}
							else
							{
								foreach (Match m in rx.Matches(line))
								{ tgt.Add($"{m.Groups[1].Value.Trim()}{sep}{m.Groups[2].Value.Trim()}"); hit = true; }
							}

							if (!hit) errors.Add($"{file} | line {lineNo} | {line}");
						}
					}
					catch { }
				}

				if (ct.IsCancellationRequested) { AppendLog("Stopped.", "WARN"); return; }

				string stamp = DateTime.Now.ToString("[dd.MM.yy] [HH.mm.ss]");
				resultDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WordlistAuditResults", stamp);
				Directory.CreateDirectory(resultDir);

				// Save one file per URL
				int totalFound = 0;
				foreach (var (urlKey, credSet) in perUrl)
				{
					var final = credSet
						.Where(c => { int idx = c.IndexOfAny(new[]{':',';'}); return idx >= 0 && (c.Length - idx - 1) >= minPassLen; })
						.OrderBy(x => x).ToList();

					int sk = credSet.Count - final.Count;
					if (sk > 0) AppendLog($"[{urlKey}] Filtered {sk} short-password credential(s)", "INFO");

					if (final.Count == 0) { AppendLog($"No matches for: {urlKey}", "WARN"); continue; }

					string fn = SafeFileName(urlKey) + ".txt";
					File.WriteAllLines(Path.Combine(resultDir, fn), final);
					AppendLog($"✔  [{urlKey}]  {final.Count} credentials  →  {fn}", "OK");
					totalFound += final.Count;
				}

				found    = totalFound;
				errCount = errors.Count;

				if (errors.Count > 0)
				{
					string errDir = Path.Combine(resultDir, "ErrorLines");
					Directory.CreateDirectory(errDir);
					File.WriteAllLines(Path.Combine(errDir, "ErrorLines.txt"), errors);
					AppendLog($"⚠  {errors.Count} unmatched lines → ErrorLines/", "WARN");
				}

				if (totalFound == 0) resultDir = null;
			}
			catch (Exception ex) { AppendLog($"Error: {ex.Message}", "ERROR"); }
		}, ct);

		Dispatcher.InvokeAsync(() =>
		{
			_runBtn.IsEnabled  = true;
			_stopBtn.IsEnabled = false;
			_foundTb.Text      = $"Found: {found}";
			_errorsTb.Text     = $"Error lines: {errCount}";
			if (resultDir != null) { _lastResultDir = resultDir; _openBtn.IsEnabled = true; }
			var pRow = (Grid)_progressBar.Tag;
			pRow.Visibility  = Visibility.Collapsed;
			_progressBar.Value = 0;
			_progressTb.Text   = "";
		});
	}

	private void AppendLog(string msg, string type = "INFO")
	{
		Dispatcher.InvokeAsync(() =>
		{
			string ts = DateTime.Now.ToString("HH:mm:ss");
			_logBox.AppendText($"[{ts}]  [{type,-7}]  {msg}\n");
			_logBox.ScrollToEnd();
		});
	}

	// ── REMOVE DUPLICATES ─────────────────────────────────────────────────────
	private async void RunDedup(object sender, RoutedEventArgs e)
	{
		string path = _dedupPathBox.Text.Trim().Trim('"');

		if (!File.Exists(path))
		{
			_dedupResultTb.Foreground = C(210, 80, 80);
			_dedupResultTb.Text = "File not found.";
			return;
		}

		_dedupBtn.IsEnabled       = false;
		_dedupResultTb.Foreground = TextMuted;
		_dedupResultTb.Text       = "Working…";

		int    total   = 0;
		int    removed = 0;
		bool   success = false;
		string errMsg  = null;

		await Task.Run(() =>
		{
			try
			{
				// Read preserving original encoding — same fallback as the auditor
				Encoding fileEnc;
				string[] lines;
				try
				{
					lines   = File.ReadAllLines(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
					fileEnc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false); // no BOM
				}
				catch (DecoderFallbackException)
				{
					fileEnc = Encoding.Default;
					lines   = File.ReadAllLines(path, fileEnc);
				}

				total = lines.Length;
				var seen   = new HashSet<string>(StringComparer.Ordinal);
				var unique = new List<string>(total);
				foreach (string ln in lines)
					if (seen.Add(ln)) unique.Add(ln);

				removed = total - unique.Count;

				// Write back in the SAME encoding that was read
				File.WriteAllLines(path, unique, fileEnc);
				success = true;
			}
			catch (Exception ex) { errMsg = ex.Message; }
		});

		_dedupBtn.IsEnabled = true;

		if (!success)
		{
			_dedupResultTb.Foreground = C(210, 80, 80);
			_dedupResultTb.Text       = $"Error: {errMsg}";
			return;
		}

		if (removed == 0)
		{
			_dedupResultTb.Foreground = TextMuted;
			_dedupResultTb.Text       = $"No duplicates found  ({total:N0} lines already unique)";
		}
		else
		{
			_dedupResultTb.Foreground = Green;
			_dedupResultTb.Text       = $"Removed {removed:N0} duplicates  →  {total - removed:N0} unique lines kept";
		}
	}

	// ══════════════════════════════════════════════════════════════════════════
	//  BAML WIRING (unchanged — connects XAML IDs to fields)
	// ══════════════════════════════════════════════════════════════════════════
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/tools/wordlisttools.xaml", UriKind.Relative);
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
		case 1:  locTextBox        = (TextBox)target; break;
		case 2:  ((Button)target).Click += (s,e) => {}; break;
		case 3:  ((Button)target).Click += (s,e) => {}; break;
		case 4:  removedDup        = (TextBlock)target; break;
		case 5:  ((Button)target).Click += (s,e) => {}; break;
		case 6:  splitTextBox      = (TextBox)target; break;
		case 7:  splitIndex        = (IntegerUpDown)target; break;
		case 8:  splited           = (TextBlock)target; break;
		case 9:  ((Button)target).Click += (s,e) => {}; break;
		case 10: currentSepTextBox = (TextBox)target; break;
		case 11: newSepTextBox     = (TextBox)target; break;
		case 12: changed           = (TextBlock)target; break;
		case 13: loaded            = (TextBlock)target; break;
		default: _contentLoaded    = true; break;
		}
	}
}
