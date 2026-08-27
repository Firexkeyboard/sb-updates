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

public class RequiresPage : Page, IComponentConnector
{
	private Requires[] requires;

	private BrushConverter brushConverter = new BrushConverter();

	internal Label waitingLabel;

	internal WrapPanel wrapPanel;

	private bool _contentLoaded;

	public RequiresPage()
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
				data = http.GetStringAsync("https://raw.githubusercontent.com/mohamm4dx/SilverBullet/master/OpenBullet/Requires.json").GetAwaiter().GetResult();
			}).ContinueWith(delegate
			{
				if (string.IsNullOrWhiteSpace(data))
				{
					base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
					{
						waitingLabel.Visibility = Visibility.Visible;
						waitingLabel.Content = "ERROR (CHECK YOUR NETWORK CONNECTION)";
					});
				}
				else
				{
					base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
					{
						waitingLabel.Visibility = Visibility.Collapsed;
					});
				}
				requires = IOManager.DeserializeObject<Requires[]>(data);
				try
				{
					SetRequirements();
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
			waitingLabel.Content = "ERROR (CHECK YOUR NETWORK CONNECTION)";
		}
	}

	private async void SetRequirements()
	{
		if (requires == null || requires.Length == 0)
		{
			return;
		}
		int i;
		for (i = 0; i < requires.Length; i++)
		{
			try
			{
				await base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
				{
					UserControlSupport uc = new UserControlSupport
					{
						Width = requires[i].Width,
						Height = requires[i].Height,
						SupportName = requires[i].Name,
						Margin = new Thickness(0.0, 0.0, 8.0, 8.0),
						BackgroundButton = (SolidColorBrush)brushConverter.ConvertFrom(requires[i].Color),
						Url = requires[i].Address,
						border = 
						{
							ToolTip = requires[i].ToolTip
						}
					};
					uc.SetImage(new Uri(requires[i].Image));
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/requirespage.xaml", UriKind.Relative);
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
			((RequiresPage)target).Loaded += Page_Loaded;
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
