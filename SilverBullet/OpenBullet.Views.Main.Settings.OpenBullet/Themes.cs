using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;

namespace OpenBullet.Views.Main.Settings.OpenBullet;

public class Themes : Page, IComponentConnector
{
	internal ColorPicker BackgroundMain;

	internal ColorPicker BackgroundSecondary;

	internal ColorPicker ForegroundMain;

	internal ColorPicker ForegroundGood;

	internal ColorPicker ForegroundBad;

	internal ColorPicker ForegroundCustom;

	internal ColorPicker ForegroundRetry;

	internal ColorPicker ForegroundToCheck;

	internal ColorPicker ForegroundOcrRate;

	internal ColorPicker ForegroundMenuSelected;

	internal Button resetButton;

	internal CheckBox useImagesCheckbox;

	internal IntegerUpDown backgroundImageOpacityUpDown;

	internal Image loadBackgroundImage;

	internal Image backgroundImagePreview;

	internal Image loadBackgroundLogo;

	internal Image backgroundLogoPreview;

	private bool _contentLoaded;

	public Themes()
	{
		InitializeComponent();
		base.DataContext = SB.SBSettings.Themes;
		SetColors();
		SetColorPreviews();
		SetImagePreviews();
		SB.MainWindow.AllowsTransparency = SB.SBSettings.Themes.AllowTransparency;
		base.Loaded += Themes_Loaded;
	}

	private void Themes_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			Panel parent = VisualTreeHelper.GetParent(resetButton) as Panel;
			if (parent != null)
			{
				Button lightBtn = new Button
				{
					Content = "Light Preset",
					Margin = new Thickness(5, 0, 0, 0),
					Padding = new Thickness(8, 4, 8, 4),
					Style = resetButton.Style
				};
				lightBtn.Click += (s, ev) => ApplyLightPreset();
				int idx = parent.Children.IndexOf(resetButton);
				parent.Children.Insert(idx + 1, lightBtn);
			}
		}
		catch { }
	}

	private void ApplyLightPreset()
	{
		SB.SBSettings.Themes.BackgroundMain = "#f5f5f5";
		SB.SBSettings.Themes.BackgroundSecondary = "#e8e8e8";
		SB.SBSettings.Themes.ForegroundMain = "#212121";
		SB.SBSettings.Themes.ForegroundGood = "#1b5e20";
		SB.SBSettings.Themes.ForegroundBad = "#b71c1c";
		SB.SBSettings.Themes.ForegroundCustom = "#e65100";
		SB.SBSettings.Themes.ForegroundRetry = "#f57f17";
		SB.SBSettings.Themes.ForegroundToCheck = "#006064";
		SB.SBSettings.Themes.ForegroundMenuSelected = "#0d47a1";
		SB.SBSettings.Themes.ForegroundOcrRate = "#4a148c";
		SetColors();
		SetColorPreviews();
	}

	public void SetColors()
	{
		SetAppColor("BackgroundMain", SB.SBSettings.Themes.BackgroundMain);
		SetAppColor("BackgroundSecondary", SB.SBSettings.Themes.BackgroundSecondary);
		SetAppColor("ForegroundMain", SB.SBSettings.Themes.ForegroundMain);
		SetAppColor("ForegroundGood", SB.SBSettings.Themes.ForegroundGood);
		SetAppColor("ForegroundBad", SB.SBSettings.Themes.ForegroundBad);
		SetAppColor("ForegroundCustom", SB.SBSettings.Themes.ForegroundCustom);
		SetAppColor("ForegroundRetry", SB.SBSettings.Themes.ForegroundRetry);
		SetAppColor("ForegroundToCheck", SB.SBSettings.Themes.ForegroundToCheck);
		SetAppColor("ForegroundMenuSelected", SB.SBSettings.Themes.ForegroundMenuSelected);
		SetAppColor("ForegroundOcrRate", SB.SBSettings.Themes.ForegroundOcrRate);
		SB.MainWindow.SetStyle();
	}

	private void SetColorPreviews()
	{
		BackgroundMain.SelectedColor = GetAppColor("BackgroundMain");
		BackgroundSecondary.SelectedColor = GetAppColor("BackgroundSecondary");
		ForegroundMain.SelectedColor = GetAppColor("ForegroundMain");
		ForegroundGood.SelectedColor = GetAppColor("ForegroundGood");
		ForegroundBad.SelectedColor = GetAppColor("ForegroundBad");
		ForegroundCustom.SelectedColor = GetAppColor("ForegroundCustom");
		ForegroundRetry.SelectedColor = GetAppColor("ForegroundRetry");
		ForegroundToCheck.SelectedColor = GetAppColor("ForegroundToCheck");
		ForegroundOcrRate.SelectedColor = GetAppColor("ForegroundOcrRate");
		ForegroundMenuSelected.SelectedColor = GetAppColor("ForegroundMenuSelected");
	}

	public void SetAppColor(string resourceName, string color)
	{
		Application.Current.Resources[resourceName] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
	}

	public Color GetAppColor(string resourceName)
	{
		return ((SolidColorBrush)Application.Current.Resources[resourceName]).Color;
	}

	private void SetImagePreviews()
	{
		try
		{
			backgroundImagePreview.Source = GetImageBrush(SB.SBSettings.Themes.BackgroundImage);
			backgroundLogoPreview.Source = GetImageBrush(SB.SBSettings.Themes.BackgroundLogo);
		}
		catch
		{
		}
	}

	private BitmapImage GetImageBrush(string file)
	{
		try
		{
			if (File.Exists(file))
			{
				return new BitmapImage(new Uri(file));
			}
			return new BitmapImage(new Uri("pack://application:,,,/" + Assembly.GetExecutingAssembly().GetName().Name + ";component/Images/Themes/empty.png", UriKind.Absolute));
		}
		catch
		{
			return null;
		}
	}

	private void resetButton_Click(object sender, RoutedEventArgs e)
	{
		SB.SBSettings.Themes.BackgroundMain = "#222";
		SB.SBSettings.Themes.BackgroundSecondary = "#111";
		SB.SBSettings.Themes.ForegroundMain = "#dcdcdc";
		SB.SBSettings.Themes.ForegroundGood = "#adff2f";
		SB.SBSettings.Themes.ForegroundBad = "#ff6347";
		SB.SBSettings.Themes.ForegroundCustom = "#ff8c00";
		SB.SBSettings.Themes.ForegroundRetry = "#ffff00";
		SB.SBSettings.Themes.ForegroundToCheck = "#7fffd4";
		SB.SBSettings.Themes.ForegroundMenuSelected = "#1e90ff";
		SB.SBSettings.Themes.ForegroundOcrRate = "#ff8cc6ff";
		SetColors();
		SetColorPreviews();
		SetImagePreviews();
	}

	private void loadBackgroundImage_MouseDown(object sender, MouseButtonEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "BMP|*.bmp|GIF|*.gif|JPG|*.jpg;*.jpeg|PNG|*.png|TIFF|*.tif;*.tiff|All Graphics Types|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff";
		openFileDialog.FilterIndex = 4;
		openFileDialog.ShowDialog();
		SB.SBSettings.Themes.BackgroundImage = openFileDialog.FileName;
		SetColors();
		SetImagePreviews();
	}

	private void loadBackgroundLogo_MouseDown(object sender, MouseButtonEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "BMP|*.bmp|GIF|*.gif|JPG|*.jpg;*.jpeg|PNG|*.png|TIFF|*.tif;*.tiff|All Graphics Types|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff";
		openFileDialog.FilterIndex = 4;
		openFileDialog.ShowDialog();
		SB.SBSettings.Themes.BackgroundLogo = openFileDialog.FileName;
		SetColors();
		SetImagePreviews();
	}

	private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
	{
		if (e.NewValue.HasValue)
		{
			((object)SB.SBSettings.Themes).GetType().GetProperty(((FrameworkElement)(ColorPicker)sender).Name.ToString()).SetValue(SB.SBSettings.Themes, ColorToHtml(e.NewValue.Value), null);
		}
		SetColors();
	}

	private string ColorToHtml(Color color)
	{
		return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
	}

	private void useImagesCheckbox_Checked(object sender, RoutedEventArgs e)
	{
		SetColors();
	}

	private void useImagesCheckbox_Unchecked(object sender, RoutedEventArgs e)
	{
		SetColors();
	}

	private void backgroundImageOpacityUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
	{
		SB.MainWindow.SetStyle();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/settings/openbullet/themes.xaml", UriKind.Relative);
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
			BackgroundMain = (ColorPicker)target;
			BackgroundMain.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 2:
			BackgroundSecondary = (ColorPicker)target;
			BackgroundSecondary.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 3:
			ForegroundMain = (ColorPicker)target;
			ForegroundMain.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 4:
			ForegroundGood = (ColorPicker)target;
			ForegroundGood.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 5:
			ForegroundBad = (ColorPicker)target;
			ForegroundBad.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 6:
			ForegroundCustom = (ColorPicker)target;
			ForegroundCustom.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 7:
			ForegroundRetry = (ColorPicker)target;
			ForegroundRetry.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 8:
			ForegroundToCheck = (ColorPicker)target;
			ForegroundToCheck.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 9:
			ForegroundOcrRate = (ColorPicker)target;
			ForegroundOcrRate.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 10:
			ForegroundMenuSelected = (ColorPicker)target;
			ForegroundMenuSelected.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 11:
			resetButton = (Button)target;
			resetButton.Click += resetButton_Click;
			break;
		case 12:
			useImagesCheckbox = (CheckBox)target;
			useImagesCheckbox.Checked += useImagesCheckbox_Checked;
			useImagesCheckbox.Unchecked += useImagesCheckbox_Unchecked;
			break;
		case 13:
			backgroundImageOpacityUpDown = (IntegerUpDown)target;
			((UpDownBase<int?>)(object)backgroundImageOpacityUpDown).ValueChanged += backgroundImageOpacityUpDown_ValueChanged;
			break;
		case 14:
			loadBackgroundImage = (Image)target;
			loadBackgroundImage.MouseDown += loadBackgroundImage_MouseDown;
			break;
		case 15:
			backgroundImagePreview = (Image)target;
			break;
		case 16:
			loadBackgroundLogo = (Image)target;
			loadBackgroundLogo.MouseDown += loadBackgroundLogo_MouseDown;
			break;
		case 17:
			backgroundLogoPreview = (Image)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
