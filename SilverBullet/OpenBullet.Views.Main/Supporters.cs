using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using OpenBullet.Views.UserControls;
using RuriLib;
using RuriLib.Models;

namespace OpenBullet.Views.Main;

public class Supporters : Page, IComponentConnector
{
	private SupportersModel[] supporters;

	private BrushConverter brushConverter = new BrushConverter();

	internal Label waitingLabel;

	internal WrapPanel wrapPanel;

	private bool _contentLoaded;

	public Supporters()
	{
		InitializeComponent();
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			if (wrapPanel.Children.Count <= 0)
			{
				waitingLabel.Visibility = Visibility.Visible;
			}
			else
			{
				waitingLabel.Visibility = Visibility.Collapsed;
			}
			string data = string.Empty;
			using (Task.Run(delegate
			{
				using var http = new HttpClient();
				http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:87.0) Gecko/20100101 Firefox/87.0");
				data = http.GetStringAsync("https://raw.githubusercontent.com/mohamm4dx/SilverBullet/master/OpenBullet/Supporters.json").GetAwaiter().GetResult();
			}).ContinueWith(delegate
			{
				if (string.IsNullOrWhiteSpace(data))
				{
					base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
					{
						waitingLabel.Visibility = Visibility.Visible;
						waitingLabel.Content = "ERROR";
					});
				}
				else
				{
					base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
					{
						waitingLabel.Visibility = Visibility.Collapsed;
					});
				}
				supporters = IOManager.DeserializeObject<SupportersModel[]>(data);
				base.Dispatcher.Invoke(delegate
				{
					SB.MainWindow.SilverZonePage.vm.SupportersBadge = ((supporters.Length > 99) ? "999+" : supporters.Length.ToString());
					int num = supporters.Length + int.Parse(SB.MainWindow.SilverZonePage.vm.VerifiedMarketBadge.Replace("+", ""));
					SB.MainWindow.silverZoneBadged.Badge = ((num > 99) ? "99+" : num.ToString());
				});
				try
				{
					SetSupporters();
				}
				catch
				{
				}
			}))
			{
			}
		}
		catch (InvalidOperationException)
		{
		}
		catch (NullReferenceException)
		{
		}
		catch (Exception)
		{
			waitingLabel.Visibility = Visibility.Visible;
			waitingLabel.Content = "ERROR";
		}
	}

	private async void SetSupporters()
	{
		if (supporters == null || supporters.Length == 0)
		{
			return;
		}
		int i;
		for (i = 0; i < supporters.Length; i++)
		{
			try
			{
				await base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
				{
					UserControlSupport uc = new UserControlSupport
					{
						Width = 200.0,
						Height = 200.0,
						SupportName = supporters[i].Name,
						Margin = new Thickness(0.0, 0.0, 8.0, 8.0),
						BackgroundButton = (SolidColorBrush)brushConverter.ConvertFrom(supporters[i].Color),
						Url = supporters[i].Address
					};
					uc.SetImage(new Uri(supporters[i].Logo));
					if (!wrapPanel.Children.OfType<UserControlSupport>().Any((UserControlSupport u) => u.Url == uc.Url))
					{
						wrapPanel.Children.Add(uc);
					}
				});
			}
			catch
			{
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/supporters.xaml", UriKind.Relative);
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
			((Supporters)target).Loaded += Page_Loaded;
			break;
		case 2:
			waitingLabel = (Label)target;
			break;
		case 3:
			wrapPanel = (WrapPanel)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
