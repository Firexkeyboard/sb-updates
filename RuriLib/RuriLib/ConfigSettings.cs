using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using Newtonsoft.Json;
using RuriLib.Functions.Requests;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace RuriLib;

public class ConfigSettings : ViewModelBase
{
	private string name = "";

	private int suggestedBots = 1;

	private int maxCPM;

	private DateTime lastModified = DateTime.Now;

	private string additionalInfo = "";

	private string[] requiredPlugins = new string[0];

	private string author = "";

	private string version = "1.2.2";

	private bool saveEmptyCaptures;

	private bool continueOnCustom;

	private bool saveHitsToTextFile;

	private bool ignoreResponseErrors;

	private int _maxRedirects = 8;

	private bool needsProxies;

	private bool onlySocks;

	private bool onlySsl;

	private int maxProxyUses;

	private bool banProxyAfterGoodStatus;

	private int banLoopEvasionOverride = -1;

	private bool stickyProxy;

	private string stickyProxySuffix = "-sessid-{0}";

	private bool encodeData;

	private string allowedWordlist1 = "";

	private string allowedWordlist2 = "";

	private string captchaUrl = "";

	private bool isBase64;

	private ObservableCollection<string> filterList = new ObservableCollection<string>();

	private bool evaluateMathOCR;

	private SecurityProtocol securityProtocol;

	private bool forceHeadless;

	private bool alwaysOpen;

	private bool alwaysQuit;

	private bool quitOnBanRetry;

	private bool acceptInsecureCertificates = true;

	private bool disableNotifications;

	private bool disableImageLoading;

	private bool defaultProfileDirectory;

	private string customUserAgent = "";

	private bool randomUA;

	private string customCMDArgs = "";

	private string title;

	private string iconPath;

	private string licenseSource;

	private string message;

	private Color messageColor = Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private string hitInfoFormat = "[{hit.Type}][{hit.Proxy}] {hit.Data} - [{hit.CapturedString}]";

	private Color authorColor = Color.FromRgb(byte.MaxValue, 178, 102);

	private Color wordlistColor = Color.FromRgb(181, 194, 225);

	private Color botsColor = Color.FromRgb(168, byte.MaxValue, byte.MaxValue);

	private Color customInputColor = Color.FromRgb(214, 199, 199);

	private Color cpmColor = Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private Color progressColor = Color.FromRgb(173, 147, 227);

	private Color hitsColor = Color.FromRgb(102, byte.MaxValue, 102);

	private Color customColor = Color.FromRgb(byte.MaxValue, 178, 102);

	private Color toCheckColor = Color.FromRgb(127, byte.MaxValue, 212);

	private Color failsColor = Color.FromRgb(byte.MaxValue, 51, 51);

	private Color retriesColor = Color.FromRgb(byte.MaxValue, byte.MaxValue, 153);

	private Color ocrRateColor = Color.FromRgb(70, 152, 253);

	private Color proxiesColor = Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue);

	public string Name
	{
		get
		{
			return name;
		}
		set
		{
			name = value;
			OnPropertyChanged("Name");
		}
	}

	public int SuggestedBots
	{
		get
		{
			return suggestedBots;
		}
		set
		{
			suggestedBots = value;
			OnPropertyChanged("SuggestedBots");
		}
	}

	public int MaxCPM
	{
		get
		{
			return maxCPM;
		}
		set
		{
			maxCPM = value;
			OnPropertyChanged("MaxCPM");
		}
	}

	public DateTime LastModified
	{
		get
		{
			return lastModified;
		}
		set
		{
			lastModified = value;
			OnPropertyChanged("LastModified");
		}
	}

	public string AdditionalInfo
	{
		get
		{
			return additionalInfo;
		}
		set
		{
			additionalInfo = value;
			OnPropertyChanged("AdditionalInfo");
		}
	}

	public string[] RequiredPlugins
	{
		get
		{
			return requiredPlugins;
		}
		set
		{
			requiredPlugins = value;
			OnPropertyChanged("RequiredPlugins");
			OnPropertyChanged("RequiredPluginsString");
		}
	}

	[JsonIgnore]
	public string RequiredPluginsString => string.Join(", ", RequiredPlugins);

	public string Author
	{
		get
		{
			return author;
		}
		set
		{
			author = value;
			OnPropertyChanged("Author");
		}
	}

	public string Version
	{
		get
		{
			return version;
		}
		set
		{
			version = value;
			OnPropertyChanged("Version");
		}
	}

	public bool SaveEmptyCaptures
	{
		get
		{
			return saveEmptyCaptures;
		}
		set
		{
			saveEmptyCaptures = value;
			OnPropertyChanged("SaveEmptyCaptures");
		}
	}

	public bool ContinueOnCustom
	{
		get
		{
			return continueOnCustom;
		}
		set
		{
			continueOnCustom = value;
			OnPropertyChanged("ContinueOnCustom");
		}
	}

	public bool SaveHitsToTextFile
	{
		get
		{
			return saveHitsToTextFile;
		}
		set
		{
			saveHitsToTextFile = value;
			OnPropertyChanged("SaveHitsToTextFile");
		}
	}

	public bool IgnoreResponseErrors
	{
		get
		{
			return ignoreResponseErrors;
		}
		set
		{
			ignoreResponseErrors = value;
			OnPropertyChanged("IgnoreResponseErrors");
		}
	}

	public int MaxRedirects
	{
		get
		{
			return _maxRedirects;
		}
		set
		{
			_maxRedirects = value;
			OnPropertyChanged("MaxRedirects");
		}
	}

	public bool NeedsProxies
	{
		get
		{
			return needsProxies;
		}
		set
		{
			needsProxies = value;
			OnPropertyChanged("NeedsProxies");
		}
	}

	public bool OnlySocks
	{
		get
		{
			return onlySocks;
		}
		set
		{
			onlySocks = value;
			OnPropertyChanged("OnlySocks");
		}
	}

	public bool OnlySsl
	{
		get
		{
			return onlySsl;
		}
		set
		{
			onlySsl = value;
			OnPropertyChanged("OnlySsl");
		}
	}

	public int MaxProxyUses
	{
		get
		{
			return maxProxyUses;
		}
		set
		{
			maxProxyUses = value;
			OnPropertyChanged("MaxProxyUses");
		}
	}

	public bool BanProxyAfterGoodStatus
	{
		get
		{
			return banProxyAfterGoodStatus;
		}
		set
		{
			banProxyAfterGoodStatus = value;
			OnPropertyChanged("BanProxyAfterGoodStatus");
		}
	}

	public int BanLoopEvasionOverride
	{
		get
		{
			return banLoopEvasionOverride;
		}
		set
		{
			banLoopEvasionOverride = value;
			OnPropertyChanged("BanLoopEvasionOverride");
		}
	}

	public bool StickyProxy
	{
		get
		{
			return stickyProxy;
		}
		set
		{
			stickyProxy = value;
			OnPropertyChanged("StickyProxy");
		}
	}

	public string StickyProxySuffix
	{
		get
		{
			return stickyProxySuffix;
		}
		set
		{
			stickyProxySuffix = value;
			OnPropertyChanged("StickyProxySuffix");
		}
	}

	public bool EncodeData
	{
		get
		{
			return encodeData;
		}
		set
		{
			encodeData = value;
			OnPropertyChanged("EncodeData");
		}
	}

	public string AllowedWordlist1
	{
		get
		{
			return allowedWordlist1;
		}
		set
		{
			allowedWordlist1 = value;
			OnPropertyChanged("AllowedWordlist1");
		}
	}

	public string AllowedWordlist2
	{
		get
		{
			return allowedWordlist2;
		}
		set
		{
			allowedWordlist2 = value;
			OnPropertyChanged("AllowedWordlist2");
		}
	}

	public ObservableCollection<DataRule> DataRules { get; set; } = new ObservableCollection<DataRule>();

	public ObservableCollection<CustomInput> CustomInputs { get; set; } = new ObservableCollection<CustomInput>();

	public string CaptchaUrl
	{
		get
		{
			return captchaUrl;
		}
		set
		{
			captchaUrl = value;
			OnPropertyChanged("CaptchaUrl");
		}
	}

	public bool IsBase64
	{
		get
		{
			return isBase64;
		}
		set
		{
			isBase64 = value;
			OnPropertyChanged("IsBase64");
		}
	}

	public ObservableCollection<string> FilterList
	{
		get
		{
			return filterList;
		}
		set
		{
			filterList = value;
			OnPropertyChanged("FilterList");
		}
	}

	public bool EvaluateMathOCR
	{
		get
		{
			return evaluateMathOCR;
		}
		set
		{
			evaluateMathOCR = value;
			OnPropertyChanged("EvaluateMathOCR");
		}
	}

	public SecurityProtocol SecurityProtocol
	{
		get
		{
			return securityProtocol;
		}
		set
		{
			securityProtocol = value;
			OnPropertyChanged("SecurityProtocol");
		}
	}

	public bool ForceHeadless
	{
		get
		{
			return forceHeadless;
		}
		set
		{
			forceHeadless = value;
			OnPropertyChanged("ForceHeadless");
		}
	}

	public bool AlwaysOpen
	{
		get
		{
			return alwaysOpen;
		}
		set
		{
			alwaysOpen = value;
			OnPropertyChanged("AlwaysOpen");
		}
	}

	public bool AlwaysQuit
	{
		get
		{
			return alwaysQuit;
		}
		set
		{
			alwaysQuit = value;
			OnPropertyChanged("AlwaysQuit");
		}
	}

	public bool QuitOnBanRetry
	{
		get
		{
			return quitOnBanRetry;
		}
		set
		{
			quitOnBanRetry = value;
			OnPropertyChanged("QuitOnBanRetry");
		}
	}

	public bool AcceptInsecureCertificates
	{
		get
		{
			return acceptInsecureCertificates;
		}
		set
		{
			acceptInsecureCertificates = value;
			OnPropertyChanged("AcceptInsecureCertificates");
		}
	}

	public bool DisableNotifications
	{
		get
		{
			return disableNotifications;
		}
		set
		{
			disableNotifications = value;
			OnPropertyChanged("DisableNotifications");
		}
	}

	public bool DisableImageLoading
	{
		get
		{
			return disableImageLoading;
		}
		set
		{
			disableImageLoading = value;
			OnPropertyChanged("DisableImageLoading");
		}
	}

	public bool DefaultProfileDirectory
	{
		get
		{
			return defaultProfileDirectory;
		}
		set
		{
			defaultProfileDirectory = value;
			OnPropertyChanged("DefaultProfileDirectory");
		}
	}

	public string CustomUserAgent
	{
		get
		{
			return customUserAgent;
		}
		set
		{
			customUserAgent = value;
			OnPropertyChanged("CustomUserAgent");
		}
	}

	public bool RandomUA
	{
		get
		{
			return randomUA;
		}
		set
		{
			randomUA = value;
			OnPropertyChanged("RandomUA");
		}
	}

	public string CustomCMDArgs
	{
		get
		{
			return customCMDArgs;
		}
		set
		{
			customCMDArgs = value;
			OnPropertyChanged("CustomCMDArgs");
		}
	}

	public string Title
	{
		get
		{
			return title;
		}
		set
		{
			title = value;
			OnPropertyChanged("Title");
		}
	}

	public string IconPath
	{
		get
		{
			return iconPath;
		}
		set
		{
			iconPath = value;
			OnPropertyChanged("IconPath");
		}
	}

	public string LicenseSource
	{
		get
		{
			return licenseSource;
		}
		set
		{
			licenseSource = value;
			OnPropertyChanged("LicenseSource");
		}
	}

	public string Message
	{
		get
		{
			return message;
		}
		set
		{
			message = value;
			OnPropertyChanged("Message");
		}
	}

	public Color MessageColor
	{
		get
		{
			return messageColor;
		}
		set
		{
			messageColor = value;
			OnPropertyChanged("MessageColor");
		}
	}

	public string HitInfoFormat
	{
		get
		{
			return hitInfoFormat;
		}
		set
		{
			hitInfoFormat = value;
			OnPropertyChanged("HitInfoFormat");
		}
	}

	public Color AuthorColor
	{
		get
		{
			return authorColor;
		}
		set
		{
			authorColor = value;
			OnPropertyChanged("AuthorColor");
		}
	}

	public Color WordlistColor
	{
		get
		{
			return wordlistColor;
		}
		set
		{
			wordlistColor = value;
			OnPropertyChanged("WordlistColor");
		}
	}

	public Color BotsColor
	{
		get
		{
			return botsColor;
		}
		set
		{
			botsColor = value;
			OnPropertyChanged("BotsColor");
		}
	}

	public Color CustomInputColor
	{
		get
		{
			return customInputColor;
		}
		set
		{
			customInputColor = value;
			OnPropertyChanged("CustomInputColor");
		}
	}

	public Color CPMColor
	{
		get
		{
			return cpmColor;
		}
		set
		{
			cpmColor = value;
			OnPropertyChanged("CPMColor");
		}
	}

	public Color ProgressColor
	{
		get
		{
			return progressColor;
		}
		set
		{
			progressColor = value;
			OnPropertyChanged("ProgressColor");
		}
	}

	public Color HitsColor
	{
		get
		{
			return hitsColor;
		}
		set
		{
			hitsColor = value;
			OnPropertyChanged("HitsColor");
		}
	}

	public Color CustomColor
	{
		get
		{
			return customColor;
		}
		set
		{
			customColor = value;
			OnPropertyChanged("CustomColor");
		}
	}

	public Color ToCheckColor
	{
		get
		{
			return toCheckColor;
		}
		set
		{
			toCheckColor = value;
			OnPropertyChanged("ToCheckColor");
		}
	}

	public Color FailsColor
	{
		get
		{
			return failsColor;
		}
		set
		{
			failsColor = value;
			OnPropertyChanged("FailsColor");
		}
	}

	public Color RetriesColor
	{
		get
		{
			return retriesColor;
		}
		set
		{
			retriesColor = value;
			OnPropertyChanged("RetriesColor");
		}
	}

	public Color OcrRateColor
	{
		get
		{
			return ocrRateColor;
		}
		set
		{
			ocrRateColor = value;
			OnPropertyChanged("OcrRateColor");
		}
	}

	public Color ProxiesColor
	{
		get
		{
			return proxiesColor;
		}
		set
		{
			proxiesColor = value;
			OnPropertyChanged("ProxiesColor");
		}
	}

	public void RemoveDataRuleById(int id)
	{
		DataRules.Remove(GetDataRuleById(id));
	}

	public DataRule GetDataRuleById(int id)
	{
		return DataRules.FirstOrDefault((DataRule r) => r.Id == id);
	}

	public void RemoveCustomInputById(int id)
	{
		CustomInputs.Remove(GetCustomInputById(id));
	}

	public CustomInput GetCustomInputById(int id)
	{
		return CustomInputs.FirstOrDefault((CustomInput c) => c.Id == id);
	}
}
