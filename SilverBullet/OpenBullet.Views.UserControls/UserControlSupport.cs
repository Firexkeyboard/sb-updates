using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenBullet.Views.UserControls;

public class UserControlSupport : UserControl, INotifyPropertyChanged, IComponentConnector
{
	private bool _mouseDown;

	private Stretch _stretch = Stretch.Fill;

	private string supportName;

	private SolidColorBrush backgroundButton;

	private string _url;

	internal Grid mainGrid;

	private ImageBrush imageBrush;

	internal Border border;

	private bool _contentLoaded;

	public Stretch Stretch
	{
		get
		{
			return _stretch;
		}
		set
		{
			if (_stretch != value && !object.Equals(_stretch, value))
			{
				_stretch = value;
				RaisePropertyChanged("Stretch");
			}
		}
	}

	public string SupportName
	{
		get
		{
			return supportName;
		}
		set
		{
			if (!string.Equals(supportName, value, StringComparison.Ordinal))
			{
				supportName = value;
				RaisePropertyChanged("SupportName");
			}
		}
	}

	public SolidColorBrush BackgroundButton
	{
		get
		{
			return backgroundButton;
		}
		set
		{
			if (!object.Equals(backgroundButton, value))
			{
				backgroundButton = value;
				RaisePropertyChanged("BackgroundButton");
			}
		}
	}

	public string Url
	{
		get
		{
			return _url;
		}
		set
		{
			if (!string.Equals(_url, value, StringComparison.Ordinal))
			{
				_url = value;
				RaisePropertyChanged("Url");
			}
		}
	}

	public event RoutedEventHandler Click;

	public event PropertyChangedEventHandler PropertyChanged;

	public UserControlSupport()
	{
		InitializeComponent();
		base.DataContext = this;
	}

	public void SetImage(Uri imgSource)
	{
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.UriSource = imgSource;
		bitmapImage.EndInit();
		imageBrush.ImageSource = bitmapImage;
	}

	private void Border_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_mouseDown)
		{
			this.Click?.Invoke(this, e);
		}
		_mouseDown = false;
	}

	private void Border_PreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
	}

	private void Border_MouseLeave(object sender, MouseEventArgs e)
	{
		_mouseDown = false;
	}

	private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Process.Start(Url);
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/usercontrolsupport.xaml", UriKind.Relative);
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
			mainGrid = (Grid)target;
			mainGrid.MouseDown += Border_PreviewMouseDown;
			mainGrid.MouseLeave += Border_MouseLeave;
			mainGrid.MouseLeftButtonUp += Border_PreviewMouseLeftButtonUp;
			break;
		case 2:
			imageBrush = (ImageBrush)target;
			break;
		case 3:
			border = (Border)target;
			break;
		case 4:
			((Button)target).Click += Button_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
