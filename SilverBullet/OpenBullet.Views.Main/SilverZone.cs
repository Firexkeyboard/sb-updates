using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using MaterialDesignThemes.Wpf;
using OpenBullet.ViewModels;

namespace OpenBullet.Views.Main;

public class SilverZone : Page, IComponentConnector
{
	private Supporters supportersPage = new Supporters();

	private VerifiedMarket verifiedMarketPage = new VerifiedMarket();

	public SilverZoneViewModel vm;

	internal StackPanel topMenu;

	internal Label menuOptionSupporters;

	internal Label menuOptionVerifiedMarket;

	internal Frame Main;

	private bool _contentLoaded;

	public SilverZone(SilverZoneViewModel viewModel = null)
	{
		InitializeComponent();
		SilverZoneViewModel obj = viewModel ?? new SilverZoneViewModel();
		SilverZoneViewModel dataContext = obj;
		vm = obj;
		base.DataContext = dataContext;
		menuOptionSupporters_MouseDown(this, null);
	}

	private void menuOptionSupporters_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = supportersPage;
		menuOptionSelected(menuOptionSupporters);
	}

	private void menuOptionVerifiedMarket_MouseDown(object sender, MouseButtonEventArgs e)
	{
		Main.Content = verifiedMarketPage;
		menuOptionSelected(menuOptionVerifiedMarket);
	}

	private void menuOptionSelected(object sender)
	{
		foreach (object child in topMenu.Children)
		{
			try
			{
				Badged val = (Badged)((child is Badged) ? child : null);
				Label label = ((val == null) ? ((Label)child) : (((ContentControl)(object)val).Content as Label));
				label.Foreground = Utils.GetBrush("ForegroundMain");
			}
			catch
			{
			}
		}
		((Label)sender).Foreground = Utils.GetBrush("ForegroundGood");
	}

	public int GetBadge()
	{
		int count;
		int count2;
		using (var http = new HttpClient())
		{
			http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:87.0) Gecko/20100101 Firefox/87.0");
			count = Regex.Matches(http.GetStringAsync("https://raw.githubusercontent.com/mohamm4dx/SilverBullet/master/OpenBullet/Supporters.json").GetAwaiter().GetResult(), "\"Name\":\"").Count;
			count2 = Regex.Matches(http.GetStringAsync("https://raw.githubusercontent.com/mohamm4dx/SilverBullet/master/OpenBullet/VerifiedMarket.json").GetAwaiter().GetResult(), "\"Content\":\"").Count;
		}
		if (vm != null)
		{
			vm.SupportersBadge = ((count > 99) ? "99+" : count.ToString());
			vm.VerifiedMarketBadge = ((count2 > 99) ? "99+" : count2.ToString());
		}
		return count + count2;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/silverzone.xaml", UriKind.Relative);
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
			topMenu = (StackPanel)target;
			break;
		case 2:
			menuOptionSupporters = (Label)target;
			menuOptionSupporters.MouseDown += menuOptionSupporters_MouseDown;
			break;
		case 3:
			menuOptionVerifiedMarket = (Label)target;
			menuOptionVerifiedMarket.MouseDown += menuOptionVerifiedMarket_MouseDown;
			break;
		case 4:
			Main = (Frame)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
