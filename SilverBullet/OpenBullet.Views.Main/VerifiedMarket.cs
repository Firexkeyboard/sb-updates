using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using OpenBullet.Views.UserControls;
using RuriLib;
using RuriLib.Models;

namespace OpenBullet.Views.Main;

public class VerifiedMarket : Page, IComponentConnector
{
	private Market[] markets;

	private ObservableCollection<UserControlMarket> marketCollection = new ObservableCollection<UserControlMarket>();

	internal DockPanel searchBoxDockPanel;

	internal TextBox serachTextBox;

	internal Label waitingLabel;

	internal ItemsControl itemsControl;

	private bool _contentLoaded;

	public VerifiedMarket()
	{
		InitializeComponent();
		itemsControl.ItemsSource = marketCollection;
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			if (marketCollection.Count <= 0)
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
				data = http.GetStringAsync("https://raw.githubusercontent.com/mohamm4dx/SilverBullet/master/OpenBullet/VerifiedMarket.json").GetAwaiter().GetResult();
			}).ContinueWith(delegate
			{
				if (string.IsNullOrWhiteSpace(data))
				{
					base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
					{
						waitingLabel.Visibility = Visibility.Visible;
						searchBoxDockPanel.Visibility = Visibility.Collapsed;
						waitingLabel.Content = "ERROR";
					});
				}
				else
				{
					base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
					{
						waitingLabel.Visibility = Visibility.Collapsed;
						searchBoxDockPanel.Visibility = Visibility.Visible;
					});
				}
				markets = IOManager.DeserializeObject<Market[]>(data);
				base.Dispatcher.Invoke(delegate
				{
					SB.MainWindow.SilverZonePage.vm.VerifiedMarketBadge = ((markets.Length > 99) ? "99+" : markets.Length.ToString());
					int num = markets.Length + int.Parse(SB.MainWindow.SilverZonePage.vm.SupportersBadge.Replace("+", ""));
					SB.MainWindow.silverZoneBadged.Badge = ((num > 99) ? "99+" : num.ToString());
				});
				try
				{
					SetMarkets();
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
			searchBoxDockPanel.Visibility = Visibility.Collapsed;
			waitingLabel.Content = "ERROR";
		}
	}

	private async void SetMarkets()
	{
		if (markets == null || markets.Length == 0)
		{
			return;
		}
		int i;
		for (i = 0; i < markets.Length; i++)
		{
			try
			{
				await base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
				{
					UserControlMarket uc = new UserControlMarket
					{
						Date = markets[i].Date,
						Category = markets[i].Category,
						Seller = markets[i].Seller,
						Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
						ContentMarket = markets[i].Content
					};
					uc.SetContent(uc.ContentMarket);
					uc.SetIcon(new Uri(markets[i].Icon));
					if (!marketCollection.Any((UserControlMarket u) => u.Seller == uc.Seller && u.Date == uc.Date && u.ContentMarket == uc.ContentMarket))
					{
						marketCollection.Add(uc);
					}
				});
			}
			catch
			{
			}
		}
	}

	private void TextBox_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Return)
		{
			itemsControl.ItemsSource = marketCollection.Where((UserControlMarket m) => m.ContentMarket.ToLower().Contains(serachTextBox.Text.ToLower()));
		}
	}

	private void serachTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (serachTextBox.Text.Length == 0)
		{
			itemsControl.ItemsSource = marketCollection;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/verifiedmarket.xaml", UriKind.Relative);
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
			((VerifiedMarket)target).Loaded += Page_Loaded;
			break;
		case 2:
			searchBoxDockPanel = (DockPanel)target;
			break;
		case 3:
			serachTextBox = (TextBox)target;
			serachTextBox.TextChanged += serachTextBox_TextChanged;
			serachTextBox.KeyDown += TextBox_KeyDown;
			break;
		case 4:
			waitingLabel = (Label)target;
			break;
		case 5:
			itemsControl = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
