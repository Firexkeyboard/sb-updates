using System.Collections.Generic;
using System.Reflection;

namespace RuriLib.ViewModels;

public class SettingsSelenium : ViewModelBase
{
	private BrowserType browser;

	private bool headless;

	private string firefoxBinaryLocation = "C:\\Program Files (x86)\\Mozilla Firefox\\firefox.exe";

	private string chromeBinaryLocation = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe";

	private List<string> chromeExtensions = new List<string>();

	private bool drawMouseMovement = true;

	private bool fastStart = true;

	private bool disableAutomation;

	private int pageLoadTimeout = 60;

	public BrowserType Browser
	{
		get
		{
			return browser;
		}
		set
		{
			browser = value;
			OnPropertyChanged("Browser");
		}
	}

	public bool Headless
	{
		get
		{
			return headless;
		}
		set
		{
			headless = value;
			OnPropertyChanged("Headless");
		}
	}

	public string FirefoxBinaryLocation
	{
		get
		{
			return firefoxBinaryLocation;
		}
		set
		{
			firefoxBinaryLocation = value;
			OnPropertyChanged("FirefoxBinaryLocation");
		}
	}

	public string ChromeBinaryLocation
	{
		get
		{
			return chromeBinaryLocation;
		}
		set
		{
			chromeBinaryLocation = value;
			OnPropertyChanged("ChromeBinaryLocation");
		}
	}

	public List<string> ChromeExtensions
	{
		get
		{
			return chromeExtensions;
		}
		set
		{
			chromeExtensions = value;
			OnPropertyChanged("ChromeExtensions");
		}
	}

	public bool DrawMouseMovement
	{
		get
		{
			return drawMouseMovement;
		}
		set
		{
			drawMouseMovement = value;
			OnPropertyChanged("DrawMouseMovement");
		}
	}

	public bool FastStart
	{
		get
		{
			return fastStart;
		}
		set
		{
			fastStart = value;
			OnPropertyChanged("FastStart");
		}
	}

	public bool DisableAutomation
	{
		get
		{
			return disableAutomation;
		}
		set
		{
			disableAutomation = value;
			OnPropertyChanged("DisableAutomation");
		}
	}

	public int PageLoadTimeout
	{
		get
		{
			return pageLoadTimeout;
		}
		set
		{
			pageLoadTimeout = value;
			OnPropertyChanged("PageLoadTimeout");
		}
	}

	public void Reset()
	{
		SettingsSelenium obj = new SettingsSelenium();
		foreach (PropertyInfo item in (IEnumerable<PropertyInfo>)new List<PropertyInfo>(typeof(SettingsSelenium).GetProperties()))
		{
			item.SetValue(this, item.GetValue(obj, null));
		}
	}
}
