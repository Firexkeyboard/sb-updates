using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LiteDB;
using MahApps.Metro.IconPacks;
using MaterialDesignThemes.Wpf;
using OpenBullet.Plugins;
using OpenBullet.ViewModels;
using OpenBullet.Views.Main;
using OpenBullet.Views.Main.Runner;
using OpenBullet.Views.Main.Settings;
using OpenBullet.Views.StackerBlocks;
using OpenBullet.Views.UserControls;
using PluginFramework;
using RuriLib;
using RuriLib.LS;
using RuriLib.LS.LoliCode;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace OpenBullet;

public class MainWindow : Window, IComponentConnector
{
	private enum ResizeDirection
	{
		Left = 1,
		Right,
		Top,
		TopLeft,
		TopRight,
		Bottom,
		BottomLeft,
		BottomRight
	}

	private Random rand = new Random();

	private int snowBuffer;

	private System.Windows.Point _startPosition;

	private bool _isResizing;

	private bool _canQuit;

	private Timer silverZoneTimer;

	private HwndSource _hwndSource;

	internal Grid Root;

	internal Grid titleBar;

	internal Label titleLabel;

	internal Grid dragPanel;

	internal Button buySbPro;

	internal Button updateButton;

	internal Button minimizeButton;

	internal PackIconFontAwesome minimizeImage;

	internal Button maximizeButton;

	internal PackIconFontAwesome maximizeImage;

	internal Button quitButton;

	internal StackPanel topMenu;

	internal Button menuOptionHome;

	internal Button menuOptionRunner;

	internal Button menuOptionProxyManager;

	internal Button menuOptionWordlistManager;

	internal Button menuOptionConfigCreator;

	internal Button menuOptionHitsDatabase;

	internal Button menuOptionTools;

	internal Button menuOptionPlugins;

	internal Button menuOptionSettings;

	internal Button menuOptionAbout;

	internal Badged silverZoneBadged;

	internal Button menuOptionSilverZone;

	internal Frame Main;

	internal System.Windows.Controls.Image gripImage;

	private bool _contentLoaded;

	public OpenBullet.Views.Main.HomePage HomePageView { get; set; }

	public RunnerManager RunnerManagerPage { get; set; }

	public Runner CurrentRunnerPage { get; set; }

	public ProxyManager ProxyManagerPage { get; set; }

	public WordlistManager WordlistManagerPage { get; set; }

	public ConfigsSection ConfigsPage { get; set; }

	public HitsDB HitsDBPage { get; set; }

	public Settings OBSettingsPage { get; set; }

	public ToolsSection ToolsPage { get; set; }

	public PluginsSection PluginsPage { get; set; }

	public Help AboutPage { get; set; }

	public Rectangle Bounds { get; private set; }

	public SilverZone SilverZonePage { get; private set; }

	public MainWindow()
	{
		SB.MainWindow = this;
		File.WriteAllText(SB.logFile, "");
		InitializeComponent();
		buySbPro.Visibility = Visibility.Collapsed;
		menuOptionPlugins.Visibility = Visibility.Collapsed;
		base.Title = $"SilverBullet X - {SB.Version} [Release]";
		var titleTb = new System.Windows.Controls.TextBlock { VerticalAlignment = VerticalAlignment.Center };
		titleTb.Inlines.Add(new System.Windows.Documents.Run("SilverBullet "));
		titleTb.Inlines.Add(new System.Windows.Documents.Run("X"));
		titleTb.Inlines.Add(new System.Windows.Documents.Run($" - {SB.Version} [Release]"));
		titleLabel.Content = titleTb;
		try
		{
			var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/app.ico"))?.Stream;
			if (iconStream != null)
			{
				var decoder = new System.Windows.Media.Imaging.IconBitmapDecoder(iconStream,
					System.Windows.Media.Imaging.BitmapCreateOptions.None,
					System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
				base.Icon = decoder.Frames.OrderByDescending(f => f.PixelWidth).FirstOrDefault();
			}
		}
		catch { }
		updateButton.Visibility = Visibility.Collapsed;
		AutoUpdater.UpdateAvailable += ShowUpdateBadge;
		AutoUpdater.CheckAndPrompt();
		// Pre-warm Roslyn so the first LoliCode execution does not pay the 3-5 second JIT cold-start.
		LoliCodeRunner.WarmUp();
		foreach (string item in new string[13]
		{
			"UserData",
			Path.Combine("UserData", "Captchas"),
			Path.Combine("UserData", "ChromeExtensions"),
			Path.Combine("UserData", "Configs"),
			Path.Combine("UserData", "DB"),
			Path.Combine("UserData", "Hits"),
			Path.Combine("UserData", "Plugins"),
			Path.Combine("UserData", "Screenshots"),
			Path.Combine("UserData", "Settings"),
			Path.Combine("UserData", "Sounds"),
			Path.Combine("UserData", "Wordlists"),
			"Js",
			"Compiled"
		}.Select((string f) => Path.Combine(Directory.GetCurrentDirectory(), f)))
		{
			if (!Directory.Exists(item))
			{
				Directory.CreateDirectory(item);
			}
		}
		try
		{
			SB.Settings.Environment = IOManager.ParseEnvironmentSettings(SB.envFile);
		}
		catch
		{
			SB.Logger.LogError(Components.Main, "Could not find / parse the Environment Settings file. Please fix the issue and try again.", prompt: true);
			Environment.Exit(0);
		}
		if (SB.Settings.Environment.WordlistTypes.Count == 0 || SB.Settings.Environment.CustomKeychains.Count == 0)
		{
			SB.Logger.LogError(Components.Main, "At least one WordlistType and one CustomKeychain must be defined in the Environment Settings file.", prompt: true);
			Environment.Exit(0);
		}
		SB.Settings.RLSettings = new RLSettingsViewModel();
		SB.Settings.ProxyManagerSettings = new ProxyManagerSettings();
		SB.SBSettings = new SBSettingsViewModel();
		if (!File.Exists(SB.rlSettingsFile))
		{
			MessageBox.Show("RuriLib Settings file not found, generating a default one");
			SB.Logger.LogWarning(Components.Main, "RuriLib Settings file not found, generating a default one");
			IOManager.SaveSettings<RLSettingsViewModel>(SB.rlSettingsFile, SB.Settings.RLSettings);
			SB.Logger.LogInfo(Components.Main, "Created the default RuriLib Settings file " + SB.rlSettingsFile);
		}
		else
		{
			SB.Settings.RLSettings = IOManager.LoadSettings<RLSettingsViewModel>(SB.rlSettingsFile);
			SB.Logger.LogInfo(Components.Main, "Loaded the existing RuriLib Settings file");
		}
		if (!File.Exists(SB.proxyManagerSettingsFile))
		{
			SB.Logger.LogWarning(Components.Main, "Proxy manager Settings file not found, generating a default one");
			SB.Settings.ProxyManagerSettings.ProxySiteUrls.Add(SB.defaultProxySiteUrl);
			SB.Settings.ProxyManagerSettings.ActiveProxySiteUrl = SB.defaultProxySiteUrl;
			SB.Settings.ProxyManagerSettings.ProxyKeys.Add(SB.defaultProxyKey);
			SB.Settings.ProxyManagerSettings.ActiveProxyKey = SB.defaultProxyKey;
			IOManager.SaveSettings<ProxyManagerSettings>(SB.proxyManagerSettingsFile, SB.Settings.ProxyManagerSettings);
			SB.Logger.LogInfo(Components.Main, "Created the default proxy manager Settings file " + SB.proxyManagerSettingsFile);
		}
		else
		{
			SB.Settings.ProxyManagerSettings = IOManager.LoadSettings<ProxyManagerSettings>(SB.proxyManagerSettingsFile);
			SB.Logger.LogInfo(Components.Main, "Loaded the existing proxy manager Settings file");
		}
		if (!File.Exists(SB.obSettingsFile))
		{
			MessageBox.Show("SilverBullet Settings file not found, generating a default one");
			SB.Logger.LogWarning(Components.Main, "SilverBullet Settings file not found, generating a default one");
			SBIOManager.SaveSettings(SB.obSettingsFile, SB.SBSettings);
			SB.Logger.LogInfo(Components.Main, "Created the default SilverBullet Settings file " + SB.obSettingsFile);
		}
		else
		{
			SB.SBSettings = SBIOManager.LoadSettings(SB.obSettingsFile);
			SB.Logger.LogInfo(Components.Main, "Loaded the existing SilverBullet Settings file");
		}
		try
		{
			if (SB.SBSettings.General.BackupDB && (!File.Exists(SB.dataBaseBackupFile) || (File.Exists(SB.dataBaseBackupFile) && (DateTime.Now - File.GetCreationTime(SB.dataBaseBackupFile)).TotalDays > 1.0)))
			{
				LiteDatabase val = new LiteDatabase(SB.dataBaseFile, (BsonMapper)null);
				try
				{
					val.GetCollection<CProxy>("proxies", (BsonAutoId)10);
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
				File.Delete(SB.dataBaseBackupFile);
				File.Copy(SB.dataBaseFile, SB.dataBaseBackupFile);
				SB.Logger.LogInfo(Components.Main, "Backed up the DB");
			}
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.Main, "Could not backup the DB: " + ex.Message);
		}
		base.Topmost = SB.SBSettings.General.AlwaysOnTop;
		(IEnumerable<PluginControl>, IEnumerable<IBlockPlugin>, IEnumerable<string>) tuple;
		try
		{
			tuple = Loader.LoadPlugins(SB.pluginsFolder);
		}
		catch (Exception ex2)
		{
			MessageBox.Show("Error in load plugins\n" + ex2.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			Environment.Exit(0);
			throw;
		}
		SB.BlockPlugins = tuple.Item2.ToList();
		SB.PluginNames = tuple.Item3;
		SB.BlockMappings = new List<(Type, Type, LinearGradientBrush)>
		{
			(typeof(BlockBypassCF), typeof(PageBlockBypassCF), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.DarkSalmon)),
			(typeof(BlockImageCaptcha), typeof(PageBlockCaptcha), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.DarkOrange)),
			(typeof(BlockReportCaptcha), typeof(PageBlockReportCaptcha), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.DarkOrange)),
			(typeof(BlockFunction), typeof(PageBlockFunction), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.YellowGreen)),
			(typeof(BlockKeycheck), typeof(PageBlockKeycheck), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.DodgerBlue)),
			(typeof(BlockLSCode), typeof(PageBlockLSCode), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.White)),
			(typeof(BlockParse), typeof(PageBlockParse), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.Gold)),
			(typeof(BlockRecaptcha), typeof(PageBlockRecaptcha), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.Turquoise)),
			(typeof(BlockSolveCaptcha), typeof(PageBlockSolveCaptcha), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.Turquoise)),
			(typeof(BlockTurnstile),    typeof(PageBlockTurnstile),    LinearGradientBrushExtensions.GetLinearGradientBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x8C, 0x00))),
			(typeof(BlockCapMonster),   typeof(PageBlockCapMonster),   new System.Windows.Media.LinearGradientBrush(new System.Windows.Media.GradientStopCollection { new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x00, 0x80, 0xFF), 0.0), new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x00, 0x50, 0xCC), 1.0) }, new System.Windows.Point(0, 0.5), new System.Windows.Point(1, 0.5))),
			(typeof(BlockRecaptchaV3),  typeof(PageBlockRecaptchaV3),  LinearGradientBrushExtensions.GetLinearGradientBrush(System.Windows.Media.Color.FromRgb(0x42, 0x85, 0xF4))),
			(typeof(BlockRecaptchaV2Invisible), typeof(PageBlockRecaptchaV2Invisible), LinearGradientBrushExtensions.GetLinearGradientBrush(System.Windows.Media.Color.FromRgb(0x0F, 0x9D, 0x58))),
			(typeof(BlockCfClearance),  typeof(PageBlockCfClearance),  LinearGradientBrushExtensions.GetLinearGradientBrush(System.Windows.Media.Color.FromRgb(0xF3, 0x80, 0x20))),
			(typeof(BlockAkmCookies),   typeof(PageBlockAkmCookies),   LinearGradientBrushExtensions.GetLinearGradientBrush(System.Windows.Media.Color.FromRgb(0x00, 0xB0, 0xB9))),
			(typeof(BlockDataDome),     typeof(PageBlockDataDome),     LinearGradientBrushExtensions.GetLinearGradientBrush(System.Windows.Media.Color.FromRgb(0x08, 0xD8, 0xCA))),
			(typeof(BlockAltcha),    typeof(PageBlockAltcha),    new System.Windows.Media.LinearGradientBrush(new System.Windows.Media.GradientStopCollection { new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x66, 0x66, 0xFF), 0.0), new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x24, 0x24, 0xEB), 1.0) }, new System.Windows.Point(0, 0.5), new System.Windows.Point(1, 0.5))),
			(typeof(BlockRecaptchaV3Bypass), typeof(PageBlockRecaptchaV3Bypass), new System.Windows.Media.LinearGradientBrush(new System.Windows.Media.GradientStopCollection { new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0xFF, 0x66, 0x00), 0.0), new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0x00, 0xCC, 0x44), 1.0) }, new System.Windows.Point(0, 0.5), new System.Windows.Point(1, 0.5))),
			(typeof(BlockFriendlyCaptcha), typeof(PageBlockFriendlyCaptcha), new System.Windows.Media.LinearGradientBrush(new System.Windows.Media.GradientStopCollection { new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0xFA, 0x81, 0x00), 0.0), new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0xF2, 0xA8, 0x08), 1.0) }, new System.Windows.Point(0, 0.5), new System.Windows.Point(1, 0.5))),
			(typeof(BlockRequest), typeof(PageBlockRequest), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.LimeGreen)),
			(typeof(BlockTCP), typeof(PageBlockTCP), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.MediumPurple)),
			(typeof(BlockDns), typeof(PageBlockDns), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.CornflowerBlue)),
			(typeof(BlockOcr), typeof(PageBlockOcr), LinearGradientBrushExtensions.GetLinearGradientBrush(System.Windows.Media.Color.FromRgb(230, 230, 230))),
			(typeof(BlockWebSocket), typeof(PageBlockWebSocket), LinearGradientBrushExtensions.GetLinearGradientBrush(System.Windows.Media.Color.FromRgb(245, 180, 0))),
			(typeof(BlockSpeechToText), typeof(PageBlockSpeechToText), LinearGradientBrushExtensions.GetLinearGradientBrush(System.Windows.Media.Color.FromRgb(164, 198, 197))),
			(typeof(BlockUtility), typeof(PageBlockUtility), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.Wheat)),
			(typeof(SBlockBrowserAction), typeof(PageSBlockBrowserAction), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.Green)),
			(typeof(SBlockElementAction), typeof(PageSBlockElementAction), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.Firebrick)),
			(typeof(SBlockExecuteJS), typeof(PageSBlockExecuteJS), LinearGradientBrushExtensions.GetLinearGradientBrush(System.Windows.Media.Color.FromRgb(60, 193, 226))),
			(typeof(SBlockNavigate), typeof(PageSBlockNavigate), LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.RoyalBlue))
		};
		foreach (IBlockPlugin item2 in tuple.Item2)
		{
			try
			{
				SB.BlockMappings.Add((((object)item2).GetType(), typeof(BlockPluginPage), item2.LinearGradientBrush));
				BlockParser.BlockMappings.Add(item2.Name, ((object)item2).GetType());
				SB.Logger.LogInfo(Components.Main, "Initialized " + item2.Name + " block plugin");
			}
			catch (Exception ex3)
			{
				SB.Logger.LogError(Components.Main, ex3.Message + "\n" + item2.Name + ".dll", prompt: true);
				Environment.Exit(0);
			}
		}
		// ── Home page + menu button ────────────────────────────────────────────
		HomePageView = new OpenBullet.Views.Main.HomePage();

		// Build the Home button to match the existing menu buttons' style,
		// then insert it at position 0 so it appears left of Runner.
		menuOptionHome = BuildHomeMenuButton();
		topMenu.Children.Insert(0, menuOptionHome);

		SB.RunnerManager = new RunnerManagerViewModel();
		SB.ProxyManager = new ProxyManagerViewModel();
		SB.WordlistManager = new WordlistManagerViewModel();
		SB.ConfigManager = new ConfigManagerViewModel();
		SB.HitsDB = new HitsDBViewModel();
		RunnerManagerPage = new RunnerManager();
		if (SB.SBSettings.General.AutoCreateRunner & !SB.RunnerManager.RestoreSession())
		{
			SB.RunnerManager.Create();
			CurrentRunnerPage = SB.RunnerManager.RunnersCollection.FirstOrDefault().View;
		}
		SB.Logger.LogInfo(Components.Main, "Initialized RunnerManager");
		ProxyManagerPage = new ProxyManager();
		SB.Logger.LogInfo(Components.Main, "Initialized ProxyManager");
		WordlistManagerPage = new WordlistManager();
		SB.Logger.LogInfo(Components.Main, "Initialized WordlistManager");
		ConfigsPage = new ConfigsSection();
		SB.Logger.LogInfo(Components.Main, "Initialized ConfigManager");
		HitsDBPage = new HitsDB();
		SB.Logger.LogInfo(Components.Main, "Initialized HitsDB");
		OBSettingsPage = new Settings();
		SB.Logger.LogInfo(Components.Main, "Initialized Settings");
		ToolsPage = new ToolsSection();
		SB.Logger.LogInfo(Components.Main, "Initialized Tools");
		PluginsPage = new PluginsSection(tuple.Item1);
		SB.Logger.LogInfo(Components.Main, "Initialized Plugins");
		OBSettingsPage.SetPluginsPage(PluginsPage);
		AboutPage = new Help();
		SilverZonePage = new SilverZone();
		menuOptionHome_Click(this, null);
		int startingWidth = SB.SBSettings.General.StartingWidth;
		int startingHeight = SB.SBSettings.General.StartingHeight;
		if (startingWidth > 800)
		{
			base.Width = startingWidth;
		}
		if (startingHeight > 600)
		{
			base.Height = startingHeight;
		}
		base.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		if (SB.SBSettings.Themes.EnableSnow)
		{
			base.Loaded += MainWindow_Loaded;
		}
	}

	private void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		DispatcherTimer dispatcherTimer = new DispatcherTimer();
		dispatcherTimer.Interval = TimeSpan.FromMilliseconds(10000 / SB.SBSettings.Themes.SnowAmount);
		dispatcherTimer.Tick += delegate
		{
			Snow();
		};
		dispatcherTimer.Start();
	}

	private void Snow()
	{
		if (snowBuffer >= 100)
		{
			for (int i = 0; i < Root.Children.Count; i++)
			{
				if (Root.Children[i].GetType() == typeof(Snowflake))
				{
					Root.Children.RemoveAt(i);
					break;
				}
			}
		}
		int num = rand.Next(-500, (int)Root.ActualWidth - 100);
		int num2 = -100;
		int num3 = rand.Next(5, 15);
		Snowflake snowflake = new Snowflake
		{
			Width = num3,
			Height = num3,
			RenderTransform = new TranslateTransform
			{
				X = num,
				Y = num2
			},
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			IsHitTestVisible = false
		};
		Grid.SetColumn(snowflake, 1);
		Grid.SetRow(snowflake, 2);
		Root.Children.Add(snowflake);
		TimeSpan timeSpan = TimeSpan.FromSeconds(rand.Next(1, 4));
		num += rand.Next(100, 500);
		DoubleAnimation animation = new DoubleAnimation
		{
			To = num,
			Duration = timeSpan
		};
		snowflake.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation);
		num2 = (int)Root.ActualHeight + 200;
		DoubleAnimation animation2 = new DoubleAnimation
		{
			To = num2,
			Duration = timeSpan
		};
		snowflake.RenderTransform.BeginAnimation(TranslateTransform.YProperty, animation2);
		snowBuffer++;
	}

	public void ShowRunnerManager()
	{
		CurrentRunnerPage = null;
		Main.Content = RunnerManagerPage;
	}

	public void ShowRunner(Runner page)
	{
		CurrentRunnerPage = page;
		Main.Content = page;
	}

	// ── Home button builder ────────────────────────────────────────────────
	private Button BuildHomeMenuButton()
	{
		// Copy the style from the Runner button so it looks identical to the other
		// menu items (icon + label, theme-aware highlight etc.).
		var icon = new MahApps.Metro.IconPacks.PackIconFontAwesome
		{
			Kind              = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.HomeSolid,
			Width             = 20,
			Height            = 20,
			VerticalAlignment = VerticalAlignment.Center,
			Margin            = new Thickness(0, 0, 6, 0),
		};

		var label = new TextBlock
		{
			Text              = "Home",
			VerticalAlignment = VerticalAlignment.Center,
		};

		var sp = new StackPanel
		{
			Orientation       = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
		};
		sp.Children.Add(icon);
		sp.Children.Add(label);

		var btn = new Button { Content = sp };
		// Inherit the same visual style as the other menu buttons
		btn.Style = menuOptionRunner.Style;
		btn.Click += menuOptionHome_Click;
		return btn;
	}

	public void menuOptionHome_Click(object sender, RoutedEventArgs e)
	{
		Main.Content = HomePageView;
		menuOptionSelected(menuOptionHome);
	}

	// ── Runner ─────────────────────────────────────────────────────────────
	public void menuOptionRunner_Click(object sender, RoutedEventArgs e)
	{
		if (CurrentRunnerPage == null)
		{
			Main.Content = RunnerManagerPage;
		}
		else
		{
			Main.Content = CurrentRunnerPage;
		}
		menuOptionSelected(menuOptionRunner);
	}

	private void menuOptionProxyManager_Click(object sender, RoutedEventArgs e)
	{
		Main.Content = ProxyManagerPage;
		menuOptionSelected(menuOptionProxyManager);
	}

	private void menuOptionWordlistManager_Click(object sender, RoutedEventArgs e)
	{
		Main.Content = WordlistManagerPage;
		menuOptionSelected(menuOptionWordlistManager);
	}

	private void menuOptionConfigCreator_Click(object sender, RoutedEventArgs e)
	{
		Main.Content = ConfigsPage;
		menuOptionSelected(menuOptionConfigCreator);
	}

	private void menuOptionHitsDatabase_Click(object sender, RoutedEventArgs e)
	{
		Main.Content = HitsDBPage;
		menuOptionSelected(menuOptionHitsDatabase);
	}

	private void menuOptionTools_Click(object sender, RoutedEventArgs e)
	{
		Main.Content = ToolsPage;
		menuOptionSelected(menuOptionTools);
	}

	private void menuOptionPlugins_Click(object sender, RoutedEventArgs e)
	{
		Main.Content = PluginsPage;
		menuOptionSelected(menuOptionPlugins);
	}

	private void menuOptionSettings_Click(object sender, RoutedEventArgs e)
	{
		Main.Content = OBSettingsPage;
		menuOptionSelected(menuOptionSettings);
	}

	private void menuOptionAbout_Click(object sender, RoutedEventArgs e)
	{
		Main.Content = AboutPage;
		menuOptionSelected(menuOptionAbout);
	}

	private void menuOptioSilverZone_Click(object sender, RoutedEventArgs e)
	{
		Main.Content = SilverZonePage;
		menuOptionSelected(menuOptionSilverZone);
	}

	private void ShowUpdateBadge(string tagName)
	{
		// Style the update button as a glowing green badge visible in the title bar
		updateButton.Content = $"⬆ {tagName}";
		updateButton.ToolTip = $"New version {tagName} available — click to download and install";
		updateButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0xD6, 0x6B));
		updateButton.Foreground = new SolidColorBrush(Colors.White);
		updateButton.BorderThickness = new Thickness(0);
		updateButton.Padding = new Thickness(10, 4, 10, 4);
		updateButton.FontWeight = FontWeights.Bold;
		updateButton.FontSize = 12;
		updateButton.Cursor = Cursors.Hand;

		// Pulsing glow animation
		var glowColor = System.Windows.Media.Color.FromRgb(0x1A, 0xD6, 0x6B);
		var dimColor  = System.Windows.Media.Color.FromRgb(0x0F, 0x8A, 0x44);
		var anim = new ColorAnimation
		{
			From           = glowColor,
			To             = dimColor,
			Duration       = new Duration(TimeSpan.FromSeconds(0.9)),
			AutoReverse    = true,
			RepeatBehavior = RepeatBehavior.Forever
		};
		var brush = new SolidColorBrush(glowColor);
		updateButton.Background = brush;
		brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);

		updateButton.Visibility = Visibility.Visible;
	}

	internal void UpdateButton_Click(object sender, RoutedEventArgs e)
	{
		updateButton.IsEnabled = false;
		updateButton.Content   = "Downloading...";
		Task.Run(() => AutoUpdater.ApplyUpdateAsync());
	}

	private void menuOptionSelected(object sender)
	{
		foreach (object child in topMenu.Children)
		{
			try
			{
				Badged val = (Badged)((child is Badged) ? child : null);
				Button button = ((val == null) ? ((Button)child) : (((ContentControl)(object)val).Content as Button));
				button.Foreground = Utils.GetBrush("ForegroundMain");
			}
			catch
			{
			}
		}
		((Button)sender).Foreground = Utils.GetBrush("ForegroundMenuSelected");
	}

	private void IconDiscord_MouseDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			Process.Start(new ProcessStartInfo("https://discord.gg/NbFmFg2vY9") { UseShellExecute = true });
		}
		catch { }
	}

	private void IconTelegram_MouseDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			Process.Start(new ProcessStartInfo("https://t.me/venomsiteoficial") { UseShellExecute = true });
		}
		catch { }
	}

	private void quitButton_Click(object sender, RoutedEventArgs e)
	{
		if (CheckOnQuit())
		{
			try
			{
				NotepadPlusExtensions.Close();
			}
			catch
			{
			}
			Environment.Exit(0);
		}
	}

	private void quitButton_MouseEnter(object sender, MouseEventArgs e)
	{
		quitButton.Background = new SolidColorBrush(Colors.DarkRed);
	}

	private void quitButton_MouseLeave(object sender, MouseEventArgs e)
	{
		quitButton.Background = new SolidColorBrush(Colors.Transparent);
	}

	private bool CheckOnQuit()
	{
		int num = SB.RunnerManager.RunnersCollection.Count((RunnerInstance r) => r.ViewModel.Busy);
		if (!SB.SBSettings.General.DisableQuitWarning || num > 0)
		{
			SB.Logger.LogWarning(Components.Main, "Prompting quit confirmation");
			if (num == 0)
			{
				if (MessageBox.Show("Are you sure you want to quit?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
				{
					return false;
				}
			}
			else if (MessageBox.Show($"There are {num} active runners. Are you sure you want to quit?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
			{
				return false;
			}
		}
		if (!SB.SBSettings.General.DisableNotSavedWarning && !SB.MainWindow.ConfigsPage.ConfigManagerPage.CheckSaved())
		{
			SB.Logger.LogWarning(Components.Main, "Config not saved, prompting quit confirmation");
			if (MessageBox.Show("The Config in Stacker wasn't saved.\nAre you sure you want to quit?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
			{
				return false;
			}
		}
		SB.Logger.LogInfo(Components.Main, "Saving RunnerManager session to the database");
		SB.RunnerManager.SaveSession();
		SB.Logger.LogInfo(Components.Main, "Quit sequence initiated");
		return true;
	}

	private void maximizeButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (base.WindowState == WindowState.Normal)
			{
				WpfScreen screenFrom = WpfScreen.GetScreenFrom(this);
				base.MaxHeight = screenFrom.WorkingArea.Height;
				base.MaxWidth = screenFrom.WorkingArea.Width;
				base.WindowState = WindowState.Maximized;
			}
			else
			{
				base.WindowState = WindowState.Normal;
			}
		}
		catch
		{
		}
	}

	private void maximizeButton_MouseEnter(object sender, MouseEventArgs e)
	{
		maximizeButton.Background = new SolidColorBrush(Colors.DimGray);
	}

	private void maximizeButton_MouseLeave(object sender, MouseEventArgs e)
	{
		maximizeButton.Background = new SolidColorBrush(Colors.Transparent);
	}

	private void minimizeButton_Click(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void minimizeButton_MouseEnter(object sender, MouseEventArgs e)
	{
		minimizeButton.Background = new SolidColorBrush(Colors.DimGray);
	}

	private void minimizeButton_MouseLeave(object sender, MouseEventArgs e)
	{
		minimizeButton.Background = new SolidColorBrush(Colors.Transparent);
	}

	private void titleBar_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
		{
			maximizeButton_Click(sender, e);
		}
	}

	private void dragPanel_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left)
		{
			WindowDrag(sender, e);
		}
	}

	private void gripImage_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (Mouse.Capture(gripImage))
		{
			_isResizing = true;
			_startPosition = Mouse.GetPosition(this);
		}
	}

	private void gripImage_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (_isResizing)
		{
			System.Windows.Point position = Mouse.GetPosition(this);
			double num = position.X - _startPosition.X;
			double num2 = position.Y - _startPosition.Y;
			base.Width += num;
			base.Height += num2;
			_startPosition = position;
		}
	}

	private void gripImage_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isResizing)
		{
			_isResizing = false;
			Mouse.Capture(null);
		}
	}

	private void screenshotImage_MouseDown(object sender, MouseButtonEventArgs e)
	{
		BitmapSource bitmapSource = CopyScreen((int)base.Width, (int)base.Height, (int)base.Top, (int)base.Left);
		Clipboard.SetImage(bitmapSource);
		GetBitmap(bitmapSource).Save("screenshot.jpg", ImageFormat.Jpeg);
		SB.Logger.LogInfo(Components.Main, "Acquired screenshot");
	}

	private static BitmapSource CopyScreen(int width, int height, int top, int left)
	{
		using Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.CopyFromScreen(left, top, 0, 0, bitmap.Size);
		return Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
	}

	private static Bitmap GetBitmap(BitmapSource source)
	{
		Bitmap bitmap = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
		BitmapData bitmapData = bitmap.LockBits(new Rectangle(System.Drawing.Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
		source.CopyPixels(Int32Rect.Empty, bitmapData.Scan0, bitmapData.Height * bitmapData.Stride, bitmapData.Stride);
		bitmap.UnlockBits(bitmapData);
		return bitmap;
	}

	private void logImage_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (SB.LogWindow == null)
		{
			SB.LogWindow = new LogWindow();
			SB.LogWindow.Show();
		}
		else
		{
			SB.LogWindow.Show();
		}
	}

	public void SetStyle()
	{
		try
		{
			SolidColorBrush brush = Utils.GetBrush("BackgroundMain");
			if (!SB.SBSettings.Themes.UseImage)
			{
				base.Background = brush;
				Main.Background = brush;
				return;
			}
			if (File.Exists(SB.SBSettings.Themes.BackgroundImage))
			{
				ImageBrush imageBrush = new ImageBrush(new BitmapImage(new Uri(SB.SBSettings.Themes.BackgroundImage)));
				imageBrush.Opacity = (double)SB.SBSettings.Themes.BackgroundImageOpacity / 100.0;
				base.Background = imageBrush;
			}
			else
			{
				base.Background = brush;
			}
			if (File.Exists(SB.SBSettings.Themes.BackgroundLogo))
			{
				ImageBrush imageBrush2 = new ImageBrush(new BitmapImage(new Uri(SB.SBSettings.Themes.BackgroundLogo)));
				imageBrush2.AlignmentX = AlignmentX.Center;
				imageBrush2.AlignmentY = AlignmentY.Center;
				imageBrush2.Stretch = Stretch.None;
				imageBrush2.Opacity = (double)SB.SBSettings.Themes.BackgroundImageOpacity / 100.0;
				Main.Background = imageBrush2;
			}
			else
			{
				Main.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/" + Assembly.GetExecutingAssembly().GetName().Name + ";component/Images/Themes/empty.png", UriKind.Absolute)));
			}
		}
		catch
		{
		}
	}

	private void Window_SourceInitialized(object sender, EventArgs e)
	{
		try
		{
			silverZoneTimer = new Timer(delegate
			{
				try
				{
					int badge = SilverZonePage.GetBadge();
					base.Dispatcher.Invoke(() => silverZoneBadged.Badge = ((badge > 99) ? "99+" : badge.ToString()));
				}
				catch
				{
				}
			}, null, TimeSpan.Zero, TimeSpan.FromMinutes(30.0));
		}
		catch
		{
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	public static extern bool ReleaseCapture();

	private void WindowDrag(object sender, MouseButtonEventArgs e)
	{
		ReleaseCapture();
		SendMessage(new WindowInteropHelper(this).Handle, 161u, (IntPtr)2, (IntPtr)0);
	}

	private void ResizeWindow(ResizeDirection direction)
	{
		SendMessage(_hwndSource.Handle, 274u, (IntPtr)(int)(61440 + direction), IntPtr.Zero);
	}

	private void BuySbPro_Click(object sender, RoutedEventArgs e)
	{
		MainDialog mainDialog = new MainDialog(new DialogSilverBulletPro(), "Buy SilverBullet Pro");
		mainDialog.ResizeMode = ResizeMode.CanResizeWithGrip;
		mainDialog.SizeToContent = SizeToContent.Manual;
		mainDialog.Width = 700.0;
		mainDialog.Height = 450.0;
		mainDialog.ShowDialog();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/mainwindow.xaml", UriKind.Relative);
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
			((MainWindow)target).SourceInitialized += Window_SourceInitialized;
			break;
		case 2:
			Root = (Grid)target;
			break;
		case 3:
			titleBar = (Grid)target;
			titleBar.MouseDown += titleBar_MouseDown;
			break;
		case 4:
			titleLabel = (Label)target;
			break;
		case 5:
			dragPanel = (Grid)target;
			dragPanel.MouseDown += dragPanel_MouseDown;
			break;
		case 6:
			buySbPro = (Button)target;
			buySbPro.Click += BuySbPro_Click;
			break;
		case 7:
			updateButton = (Button)target;
			updateButton.Click += UpdateButton_Click;
			break;
		case 8:
			minimizeButton = (Button)target;
			minimizeButton.Click += minimizeButton_Click;
			minimizeButton.MouseEnter += minimizeButton_MouseEnter;
			minimizeButton.MouseLeave += minimizeButton_MouseLeave;
			break;
		case 9:
			minimizeImage = (PackIconFontAwesome)target;
			break;
		case 10:
			maximizeButton = (Button)target;
			maximizeButton.Click += maximizeButton_Click;
			maximizeButton.MouseEnter += maximizeButton_MouseEnter;
			maximizeButton.MouseLeave += maximizeButton_MouseLeave;
			break;
		case 11:
			maximizeImage = (PackIconFontAwesome)target;
			break;
		case 12:
			quitButton = (Button)target;
			quitButton.Click += quitButton_Click;
			quitButton.MouseEnter += quitButton_MouseEnter;
			quitButton.MouseLeave += quitButton_MouseLeave;
			break;
		case 13:
			topMenu = (StackPanel)target;
			break;
		case 14:
			menuOptionRunner = (Button)target;
			menuOptionRunner.Click += menuOptionRunner_Click;
			break;
		case 15:
			menuOptionProxyManager = (Button)target;
			menuOptionProxyManager.Click += menuOptionProxyManager_Click;
			break;
		case 16:
			menuOptionWordlistManager = (Button)target;
			menuOptionWordlistManager.Click += menuOptionWordlistManager_Click;
			break;
		case 17:
			menuOptionConfigCreator = (Button)target;
			menuOptionConfigCreator.Click += menuOptionConfigCreator_Click;
			break;
		case 18:
			menuOptionHitsDatabase = (Button)target;
			menuOptionHitsDatabase.Click += menuOptionHitsDatabase_Click;
			break;
		case 19:
			menuOptionTools = (Button)target;
			menuOptionTools.Click += menuOptionTools_Click;
			break;
		case 20:
			menuOptionPlugins = (Button)target;
			menuOptionPlugins.Click += menuOptionPlugins_Click;
			break;
		case 21:
			menuOptionSettings = (Button)target;
			menuOptionSettings.Click += menuOptionSettings_Click;
			break;
		case 22:
			menuOptionAbout = (Button)target;
			menuOptionAbout.Click += menuOptionAbout_Click;
			break;
		case 23:
			silverZoneBadged = (Badged)target;
			((UIElement)target).Visibility = Visibility.Collapsed;
			break;
		case 24:
			menuOptionSilverZone = (Button)target;
			menuOptionSilverZone.Click += menuOptioSilverZone_Click;
			break;
		case 25:
			((Grid)target).MouseDown += logImage_MouseDown;
			break;
		case 26:
			((Grid)target).MouseDown += screenshotImage_MouseDown;
			break;
		case 27:
			((Grid)target).MouseDown += IconDiscord_MouseDown;
			break;
		case 28:
			((Grid)target).MouseDown += IconTelegram_MouseDown;
			break;
		case 29:
			Main = (Frame)target;
			break;
		case 30:
			gripImage = (System.Windows.Controls.Image)target;
			gripImage.PreviewMouseLeftButtonDown += gripImage_PreviewMouseLeftButtonDown;
			gripImage.PreviewMouseLeftButtonUp += gripImage_PreviewMouseLeftButtonUp;
			gripImage.PreviewMouseMove += gripImage_PreviewMouseMove;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
