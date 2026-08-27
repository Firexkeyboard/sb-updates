using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.Main.Tools;

public class SeleniumTools : Page, IComponentConnector
{
	internal Button killChromedriversButton;

	internal Button killGeckodriversButton;

	internal Button deleteChromeCacheFoldersButton;

	internal Button deleteFirefoxCacheFoldersButton;

	internal Button killChromesButton;

	internal Button killFirefoxesButton;

	private bool _contentLoaded;

	public SeleniumTools()
	{
		InitializeComponent();
	}

	private void killChromedriversButton_Click(object sender, RoutedEventArgs e)
	{
		Process.Start("cmd.exe", "/C C:\\Windows\\System32\\taskkill.exe /F /IM chromedriver.exe /T");
	}

	private void killGeckodriversButton_Click(object sender, RoutedEventArgs e)
	{
		Process.Start("cmd.exe", "/C C:\\Windows\\System32\\taskkill.exe /F /IM geckodriver.exe /T");
	}

	private void deleteChromeCacheFoldersButton_Click(object sender, RoutedEventArgs e)
	{
		string[] directories = Directory.GetDirectories(Environment.ExpandEnvironmentVariables("%userprofile%") + "\\AppData\\Local\\Temp");
		foreach (string text in directories)
		{
			if (text.Contains("scopeddir") || text.Contains("chromeurlfetcher") || text.Contains("chromeBITS"))
			{
				try
				{
					Directory.Delete(text, recursive: true);
				}
				catch
				{
				}
			}
		}
	}

	private void deleteFirefoxCacheFoldersButton_Click(object sender, RoutedEventArgs e)
	{
		string[] directories = Directory.GetDirectories(Environment.ExpandEnvironmentVariables("%userprofile%") + "\\AppData\\Local\\Temp");
		foreach (string text in directories)
		{
			if (text.Contains("rust_mozprofile"))
			{
				try
				{
					Directory.Delete(text, recursive: true);
				}
				catch
				{
				}
			}
		}
	}

	private void killChromesButton_Click(object sender, RoutedEventArgs e)
	{
		Process.Start("cmd.exe", "/C C:\\Windows\\System32\\taskkill.exe /F /IM chrome.exe /T");
	}

	private void killFirefoxesButton_Click(object sender, RoutedEventArgs e)
	{
		Process.Start("cmd.exe", "/C C:\\Windows\\System32\\taskkill.exe /F /IM firefox.exe /T");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/tools/seleniumtools.xaml", UriKind.Relative);
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
			killChromedriversButton = (Button)target;
			killChromedriversButton.Click += killChromedriversButton_Click;
			break;
		case 2:
			killGeckodriversButton = (Button)target;
			killGeckodriversButton.Click += killGeckodriversButton_Click;
			break;
		case 3:
			deleteChromeCacheFoldersButton = (Button)target;
			deleteChromeCacheFoldersButton.Click += deleteChromeCacheFoldersButton_Click;
			break;
		case 4:
			deleteFirefoxCacheFoldersButton = (Button)target;
			deleteFirefoxCacheFoldersButton.Click += deleteFirefoxCacheFoldersButton_Click;
			break;
		case 5:
			killChromesButton = (Button)target;
			killChromesButton.Click += killChromesButton_Click;
			break;
		case 6:
			killFirefoxesButton = (Button)target;
			killFirefoxesButton.Click += killFirefoxesButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
