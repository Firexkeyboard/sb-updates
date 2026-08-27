using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using OpenBullet.ViewModels;
using PluginFramework;
using RuriLib;
using RuriLib.Interfaces;

namespace OpenBullet;

public static class SB
{
	public const string Version = "0.3.2";

	public const string CompilerVersion = "1.1";

	public static List<(Type, Type, LinearGradientBrush)> BlockMappings = new List<(Type, Type, LinearGradientBrush)>();

	public static List<IBlockPlugin> BlockPlugins;

	public static IEnumerable<string> PluginNames;

	public static readonly string userDataFolder = "UserData";

	public static readonly string dataBaseFile = "UserData/DB/OpenBullet.db";

	public static readonly string dataBaseBackupFile = "UserData/DB/OpenBullet-BackupCopy.db";

	public static readonly string obSettingsFile = "UserData/Settings/OBSettings.json";

	public static readonly string rlSettingsFile = "UserData/Settings/RLSettings.json";

	public static readonly string proxyManagerSettingsFile = "UserData/Settings/ProxyManagerSettings.json";

	public static readonly string envFile = "UserData/Settings/Environment.ini";

	public static readonly string licenseFile = "UserData/Settings/License.txt";

	public static readonly string logFile = "UserData/Log.txt";

	public static readonly string configFolder = "UserData/Configs";

	public static readonly string pluginsFolder = "UserData/Plugins";

	public static readonly string defaultProxySiteUrl = "https://google.com";

	public static readonly string defaultProxyKey = "title>Google";

	public static IApplication App
	{
		get
		{
			SilverBulletApp silverBulletApp = default(SilverBulletApp);
			silverBulletApp.RunnerManager = (IRunnerManager)(object)RunnerManager;
			silverBulletApp.ProxyManager = (IProxyManager)(object)ProxyManager;
			silverBulletApp.ProxyChecker = (IProxyChecker)(object)ProxyManager;
			silverBulletApp.WordlistManager = (IWordlistManager)(object)WordlistManager;
			silverBulletApp.ConfigManager = (IConfigManager)(object)ConfigManager;
			silverBulletApp.HitsDB = (IHitsDB)(object)HitsDB;
			silverBulletApp.Settings = (ISettings)(object)Settings;
			silverBulletApp.Logger = (ILogger)(object)Logger;
			silverBulletApp.Alerter = (IAlerter)(object)Alerter;
			return (IApplication)(object)silverBulletApp;
		}
	}

	public static IEnumerable<BlockBase> BlockPluginsAsBase => BlockPlugins.Cast<BlockBase>();

	public static MainWindow MainWindow { get; set; }

	public static LogWindow LogWindow { get; set; }

	public static RunnerManagerViewModel RunnerManager { get; set; }

	public static ProxyManagerViewModel ProxyManager { get; set; }

	public static WordlistManagerViewModel WordlistManager { get; set; }

	public static ConfigManagerViewModel ConfigManager { get; set; }

	public static StackerViewModel Stacker { get; set; }

	public static HitsDBViewModel HitsDB { get; set; }

	public static Alerter Alerter { get; set; } = new Alerter();

	public static LoggerViewModel Logger { get; set; } = new LoggerViewModel();

	public static GlobalSettings Settings { get; set; } = new GlobalSettings();

	public static SBSettingsViewModel SBSettings { get; set; }
}
