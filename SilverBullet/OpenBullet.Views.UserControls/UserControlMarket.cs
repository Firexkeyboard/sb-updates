using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using AngleSharp.Text;
using RuriLib;

namespace OpenBullet.Views.UserControls;

public class UserControlMarket : UserControl, IComponentConnector
{
	private BrushConverter converter = new BrushConverter();

	private Regex regexUrl = new Regex("\\b(?:https?://|www\\.)\\S+\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	internal Image icon;

	internal StackPanel contentPanel;

	private bool _contentLoaded;

	public string Seller { get; set; }

	public string Date { get; set; }

	public string Category { get; set; }

	public string ContentMarket { get; set; }

	public UserControlMarket()
	{
		InitializeComponent();
		base.DataContext = this;
	}

	public void SetIcon(Uri imgSource)
	{
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.UriSource = imgSource;
		bitmapImage.EndInit();
		icon.Source = bitmapImage;
	}

	public void SetContent(string content)
	{
		string[] array = content.Split('\n');
		if (array == null || content.Length == 0)
		{
			array = new string[1] { content };
		}
		string[] array2 = array;
		foreach (string text in array2)
		{
			string text2 = text;
			DockPanel dockPanel = new DockPanel();
			contentPanel.Children.Add(dockPanel);
			MatchCollection matchCollection = regexUrl.Matches(text2);
			for (int j = 0; j < matchCollection.Count; j++)
			{
				string value = matchCollection[j].Value;
				string untilOrEmpty = RuriLib.StringExtensions.GetUntilOrEmpty(text2, value);
				text2 = text2.ReplaceFirst(untilOrEmpty + value, string.Empty);
				TextBlock element = CreateHyperLink(value, untilOrEmpty);
				dockPanel.Children.Add(element);
			}
			if (!string.IsNullOrWhiteSpace(text2))
			{
				dockPanel.Children.Add(CreateTextBlock(text2));
			}
			else if (string.IsNullOrEmpty(text))
			{
				dockPanel.Children.Add(CreateTextBlock(text));
			}
		}
	}

	private TextBlock CreateHyperLink(string url, string text)
	{
		TextBlock obj = new TextBlock
		{
			Text = text,
			Margin = new Thickness(0.0, 0.0, 3.0, 0.0),
			FontSize = 13.5
		};
		Uri uri = new Uri(url);
		Hyperlink hyperlink = new Hyperlink(new Run(uri.Host.Contains("t.me") ? uri.AbsolutePath.Replace("/", "") : (uri.Host + uri.AbsolutePath)))
		{
			Cursor = Cursors.Hand,
			NavigateUri = uri,
			Foreground = (converter.ConvertFrom("#FF3CE6EC") as SolidColorBrush)
		};
		hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
		obj.Inlines.Add(hyperlink);
		return obj;
	}

	private TextBlock CreateTextBlock(string text)
	{
		return new TextBlock
		{
			Text = text
		};
	}

	private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
	{
		try
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/usercontrolmarket.xaml", UriKind.Relative);
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
			icon = (Image)target;
			break;
		case 2:
			contentPanel = (StackPanel)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
