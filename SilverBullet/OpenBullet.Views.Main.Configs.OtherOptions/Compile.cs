using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Win32;
using RuriLib;
using Xceed.Wpf.Toolkit;

namespace OpenBullet.Views.Main.Configs.OtherOptions;

public class Compile : Page, IComponentConnector
{
	private ConfigSettings vm;

	internal ColorPicker MessageColor;

	internal Label hitInfoFormatHint;

	internal ColorPicker AuthorColor;

	internal ColorPicker WordlistColor;

	internal ColorPicker BotsColor;

	internal ColorPicker CustomInputColor;

	internal ColorPicker CPMColor;

	internal ColorPicker ProgressColor;

	internal ColorPicker HitsColor;

	internal ColorPicker CustomColor;

	internal ColorPicker ToCheckColor;

	internal ColorPicker FailsColor;

	internal ColorPicker RetriesColor;

	internal ColorPicker OcrRateColor;

	internal ColorPicker ProxiesColor;

	internal Label compilerVersion;

	private bool _contentLoaded;

	public Compile()
	{
		InitializeComponent();
		vm = SB.ConfigManager.CurrentConfig.Config.Settings;
		base.DataContext = vm;
		vm.Title = Path.GetFileNameWithoutExtension(SB.MainWindow.ConfigsPage.CurrentConfig.FileName);
		vm.IconPath = "Icon\\svbfile.ico";
		compilerVersion.Content = "1.1";
		SetColors();
	}

	private void SetColors()
	{
		MessageColor.SelectedColor = vm.MessageColor;
		AuthorColor.SelectedColor = vm.AuthorColor;
		WordlistColor.SelectedColor = vm.WordlistColor;
		BotsColor.SelectedColor = vm.BotsColor;
		CustomInputColor.SelectedColor = vm.CustomInputColor;
		CPMColor.SelectedColor = vm.CPMColor;
		ProgressColor.SelectedColor = vm.ProgressColor;
		HitsColor.SelectedColor = vm.HitsColor;
		CustomInputColor.SelectedColor = vm.CustomInputColor;
		ToCheckColor.SelectedColor = vm.ToCheckColor;
		FailsColor.SelectedColor = vm.FailsColor;
		RetriesColor.SelectedColor = vm.RetriesColor;
		OcrRateColor.SelectedColor = vm.OcrRateColor;
		ProxiesColor.SelectedColor = vm.ProxiesColor;
	}

	private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
	{
		if (e.NewValue.HasValue)
		{
			((object)vm).GetType().GetProperty(((FrameworkElement)(ColorPicker)sender).Name.ToString()).SetValue(vm, e.NewValue.Value, null);
		}
	}

	private void SelectIcon_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Icon | *.ico"
			};
			if (openFileDialog.ShowDialog() == true && Path.GetExtension(openFileDialog.FileName) == ".ico")
			{
				vm.IconPath = openFileDialog.FileName;
			}
		}
		catch
		{
		}
	}

	private void IconPath_TextChanged(object sender, TextChangedEventArgs e)
	{
		TextBox textBox = sender as TextBox;
		if (File.Exists(textBox.Text) && Path.GetExtension(textBox.Text) == ".ico")
		{
			vm.IconPath = textBox.Text;
		}
	}

	private void Message_TextChanged(object sender, TextChangedEventArgs e)
	{
		vm.Message = (sender as TextBox).Text;
	}

	private void HitInfoFormatTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		hitInfoFormatHint.Visibility = (((sender as TextBox).Text.Length != 0) ? Visibility.Hidden : Visibility.Visible);
	}

	private void SelectLicSource_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "License Source (*.cs)|*.cs|License Source (*.txt)|*.txt"
			};
			if (openFileDialog.ShowDialog() == true && Path.GetExtension(openFileDialog.FileName) == ".cs")
			{
				vm.LicenseSource = openFileDialog.FileName;
			}
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/configs/otheroptions/compile.xaml", UriKind.Relative);
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
			((TextBox)target).TextChanged += IconPath_TextChanged;
			break;
		case 2:
			((Button)target).Click += SelectIcon_Click;
			break;
		case 3:
			((TextBox)target).TextChanged += Message_TextChanged;
			break;
		case 4:
			MessageColor = (ColorPicker)target;
			break;
		case 5:
			((Button)target).Click += SelectLicSource_Click;
			break;
		case 6:
			hitInfoFormatHint = (Label)target;
			break;
		case 7:
			((TextBox)target).TextChanged += HitInfoFormatTextBox_TextChanged;
			break;
		case 8:
			AuthorColor = (ColorPicker)target;
			AuthorColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 9:
			WordlistColor = (ColorPicker)target;
			WordlistColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 10:
			BotsColor = (ColorPicker)target;
			BotsColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 11:
			CustomInputColor = (ColorPicker)target;
			CustomInputColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 12:
			CPMColor = (ColorPicker)target;
			CPMColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 13:
			ProgressColor = (ColorPicker)target;
			ProgressColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 14:
			HitsColor = (ColorPicker)target;
			HitsColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 15:
			CustomColor = (ColorPicker)target;
			CustomColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 16:
			ToCheckColor = (ColorPicker)target;
			ToCheckColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 17:
			FailsColor = (ColorPicker)target;
			FailsColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 18:
			RetriesColor = (ColorPicker)target;
			RetriesColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 19:
			OcrRateColor = (ColorPicker)target;
			OcrRateColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 20:
			ProxiesColor = (ColorPicker)target;
			ProxiesColor.SelectedColorChanged += ColorPicker_SelectedColorChanged;
			break;
		case 21:
			compilerVersion = (Label)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
