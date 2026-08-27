using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using AngleSharp.Text;
using RuriLib.Models;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.MetaData;
using Microsoft.Scripting.Utils;
using Microsoft.Win32;
using OpenBullet.Models;
using OpenBullet.Views.UserControls.Filters;
using RuriLib;
using RuriLib.Models;
using Tesseract;
using Xceed.Wpf.Toolkit.Primitives;

namespace OpenBullet.Views.Main.Configs;

public class ConfigOcrSettings : System.Windows.Controls.Page, IComponentConnector
{
	private BlockOcr blockOcr = new BlockOcr();

	private bool imageFromFile;

	private string path;

	private int lastSelectedIndex = -1;

	private bool clicked;

	internal System.Windows.Controls.TextBox OcrUrl;

	internal System.Windows.Controls.Button btnLoad;

	internal System.Windows.Controls.ComboBox langBox;

	internal System.Windows.Controls.CheckBox chbProxy;

	internal System.Windows.Controls.TextBox proxyTextbox;

	internal System.Windows.Controls.ComboBox proxyTypeCombobox;

	internal System.Windows.Controls.CheckBox chbisBase64;

	internal System.Windows.Controls.CheckBox chbAutoLoad;

	internal System.Windows.Controls.ComboBox engineComboBox;

	internal System.Windows.Controls.ComboBox pageSegComboBox;

	internal System.Windows.Controls.CheckBox chbEvaluateMath;

	internal System.Windows.Controls.ComboBox filterBox;

	internal ScrollViewer scrollFilterTabControl;

	internal System.Windows.Controls.TabControl filterTabControl;

	internal TabItem emptyTab;

	internal UserControlResize resizeControl;

	internal UserControlInput inputControl;

	internal UserControlBlur blurControl;

	internal UserControlInputTextAndBoolean controlInputTextAndBool;

	internal UserControlThreshold controlThreshold;

	internal UserControlAdaptiveThreshold controlAdaptiveThreshold;

	internal UserControlCropLayer controlCropLayer;

	internal UserControlEnumBox controlEnumBox;

	internal UserControlInputTextAndEnum controlInputTextAndEnum;

	internal UserControlMorphology controlMorphology;

	internal UserControlReplaceColor controlReplaceColor;

	internal UserControlCvtColor controlCvtColor;

	internal UserControlFastNlMeansDenoisingColored controlFastNlMeansDenoisingColored;

	internal UserControlResolution controlResolution;

	internal System.Windows.Controls.ListBox filterLB;

	internal System.Windows.Controls.Button btnfilterClone;

	internal System.Windows.Controls.Button btnfilterUp;

	internal System.Windows.Controls.Button btnfilterDown;

	internal System.Windows.Controls.Button btnfilterRemove;

	internal System.Windows.Controls.Button btnfilterClear;

	internal PictureBox OrigImage;

	internal System.Windows.Controls.Button btnApplyFilters;

	internal System.Windows.Controls.Button btnRefresh;

	internal System.Windows.Controls.Button btnSave;

	internal System.Windows.Controls.ComboBox sizeModeBox;

	internal TextBlock ocrRateTextblock;

	internal Border ProcImageBorder;

	internal PictureBox ProcImage;

	internal System.Windows.Controls.TextBox pixelInfo;

	internal System.Windows.Controls.TextBox resultOcrTextbox;

	private bool _contentLoaded;

	public ConfigOcrSettings(bool sendFilter = false)
	{
		InitializeComponent();
		base.DataContext = SB.MainWindow.ConfigsPage.CurrentConfig.Config.Settings;
		if (sendFilter)
		{
			return;
		}
		blockOcr.Processors.ForEach(delegate((string, string, Type) p)
		{
			filterBox.Items.Add(p.Item1);
		});
		OrigImage.SizeMode = PictureBoxSizeMode.Zoom;
		OrigImage.WaitOnLoad = true;
		ProcImage.SizeMode = PictureBoxSizeMode.Zoom;
		ProcImage.WaitOnLoad = true;
		LoadTessData();
		InitFilterControls();
		string[] names = Enum.GetNames(typeof(ProxyType));
		foreach (string text in names)
		{
			if (text != "Chain")
			{
				proxyTypeCombobox.Items.Add(text);
			}
		}
	}

	private void LoadTessData()
	{
		try
		{
			if (!Directory.Exists(".\\tessdata"))
			{
				Directory.CreateDirectory(".\\tessdata");
			}
			FileInfo[] files = new DirectoryInfo(".\\tessdata").GetFiles(".");
			foreach (FileInfo fileInfo in files)
			{
				if (fileInfo.Name.Contains(".") && !langBox.Items.Contains(fileInfo.Name.Split('.')[0]))
				{
					langBox.Items.Add(fileInfo.Name.Split('.')[0]);
				}
			}
			try
			{
				langBox.SelectedIndex = langBox.Items.IndexOf(blockOcr.OcrLang);
			}
			catch
			{
			}
		}
		catch (Exception)
		{
			System.Windows.Forms.MessageBox.Show("Missing folder \"tessdata\"! Please go make one and put your language files in it!", "NOTICE", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void btnLoad_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (!string.IsNullOrWhiteSpace(OcrUrl.Text))
			{
				CProxy val = null;
				if (chbProxy.IsChecked == true && chbProxy.IsEnabled)
				{
					val = blockOcr.CreateProxy(proxyTextbox.Text, proxyTypeCombobox.SelectedItem.ToString().ToEnum<ProxyType>((ProxyType)0));
				}
				Bitmap ocrImage = blockOcr.GetOcrImage(false, val);
				OrigImage.Image = ocrImage;
				ProcImage.Image = ocrImage.Clone() as Bitmap;
				imageFromFile = false;
			}
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.OcrTesting, ex.Message, prompt: true);
		}
	}

	private void btnfilterClear_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (SB.Logger.Log(Components.OcrTesting, (LogLevel)1, "Do you want to clear the list of filters?", prompt: true, 0, isCancelButtonVisible: true) != MessageBoxResult.Cancel)
			{
				GetSettings().FilterList.Clear();
				scrollFilterTabControl.Visibility = Visibility.Collapsed;
				ProcImage.Image = (Bitmap)OrigImage.Image.Clone();
				SetFilters();
			}
		}
		catch
		{
		}
	}

	private void btnfilterRemove_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			int selectedIndex = filterLB.SelectedIndex;
			if (selectedIndex > -1)
			{
				GetSettings().FilterList.RemoveAt(selectedIndex);
				if (filterLB.Items.Count == 0)
				{
					ProcImage.Image = (Bitmap)OrigImage.Image.Clone();
				}
				scrollFilterTabControl.Visibility = Visibility.Collapsed;
				SetFilters();
			}
		}
		catch
		{
			scrollFilterTabControl.Visibility = Visibility.Collapsed;
			SetFilters();
		}
	}

	private void chbAutoLoad_Click(object sender, RoutedEventArgs e)
	{
	}

	private void chbisBase64_Click(object sender, RoutedEventArgs e)
	{
		System.Windows.Controls.TextBox textBox = proxyTextbox;
		bool flag = (blockOcr.Base64 = chbisBase64.IsChecked == true);
		textBox.IsEnabled = !flag && chbProxy.IsEnabled;
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (OrigImage.Image != null)
			{
				Bitmap bitmap = LoadBmp();
				string[] ocr = blockOcr.GetOcr(bitmap, engineComboBox.SelectedItem.ToString().ToEnum<EngineMode>((EngineMode)3), pageSegComboBox.SelectedItem.ToString().ToEnum<PageSegMode>((PageSegMode)7), GetSettings().EvaluateMathOCR);
				ProcImage.Image = blockOcr.ProcessedImage;
				resultOcrTextbox.Text = string.Empty;
				ocr.ToList().ForEach(delegate(string o)
				{
					System.Windows.Controls.TextBox textBox = resultOcrTextbox;
					textBox.Text = textBox.Text + o + "\n";
				});
				resultOcrTextbox.Text = resultOcrTextbox.Text.TrimEnd('\n');
				ocrRateTextblock.Text = "OCR Rate: " + blockOcr.OcrRate + "%";
			}
		}
		catch (Exception ex)
		{
			System.Windows.MessageBox.Show(ex.Message, "ERROR");
		}
	}

	private Bitmap LoadBmp()
	{
		return (Bitmap)OrigImage.Image.Clone();
	}

	private void OcrUrl_TextChanged(object sender, TextChangedEventArgs e)
	{
		blockOcr.Url = OcrUrl.Text;
		if (chbAutoLoad.IsChecked == true)
		{
			btnLoad_Click(null, null);
		}
	}

	private void Button_Click_1(object sender, RoutedEventArgs e)
	{
		Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
		{
			Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png | All files (*.*)|*.*"
		};
		if (openFileDialog.ShowDialog() == true)
		{
			System.Drawing.Image image = System.Drawing.Image.FromFile(openFileDialog.FileName);
			OrigImage.Image = image;
			ProcImage.Image = image;
			imageFromFile = true;
			path = openFileDialog.FileName;
		}
	}

	private void btnRefresh_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			CProxy val = null;
			Bitmap bitmap;
			if (!imageFromFile)
			{
				if (chbProxy.IsChecked == true && chbProxy.IsEnabled)
				{
					val = blockOcr.CreateProxy(proxyTextbox.Text, proxyTypeCombobox.SelectedItem.ToString().ToEnum<ProxyType>((ProxyType)0));
				}
				bitmap = blockOcr.GetOcrImage(false, val);
			}
			else
			{
				bitmap = (Bitmap)System.Drawing.Image.FromFile(path);
			}
			OrigImage.Image = bitmap;
			Bitmap image = blockOcr.ApplyFilters(bitmap.Clone() as Bitmap, (BotData)null);
			ProcImage.Image = image;
		}
		catch (Exception ex)
		{
			System.Windows.Forms.MessageBox.Show(ex.Message, "NOTICE", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void Button_Click_2(object sender, RoutedEventArgs e)
	{
		try
		{
			GetSettings().FilterList.Add(filterBox.SelectedItem.ToString());
			filterLB.SelectedIndex = filterLB.Items.IndexOf(filterBox.SelectedItem.ToString());
			SetFilters();
		}
		catch (Exception)
		{
		}
	}

	private void SetFilters()
	{
		try
		{
			blockOcr.SetFilters(filterLB.Items.OfType<string>().ToArray());
		}
		catch
		{
		}
	}

	private void btnSave_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
			{
				Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png"
			};
			if (saveFileDialog.ShowDialog() == true)
			{
				ProcImage.Image.Save(saveFileDialog.FileName);
			}
		}
		catch
		{
		}
	}

	private void InitFilterControls()
	{
		try
		{
			Enum.GetNames(typeof(EngineMode)).ToList().ForEach(delegate(string e)
			{
				engineComboBox.Items.Add(e);
			});
		}
		catch
		{
		}
		try
		{
			Enum.GetNames(typeof(PageSegMode)).ToList().ForEach(delegate(string p)
			{
				pageSegComboBox.Items.Add(p);
			});
		}
		catch
		{
		}
	}

	private void filterLB_LostFocus(object sender, RoutedEventArgs e)
	{
		if (filterLB.Items.Count != 0)
		{
			blockOcr.SetFilters(filterLB.Items.OfType<string>().ToArray());
		}
	}

	private void filterLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			System.Windows.Controls.ListBox listBox = sender as System.Windows.Controls.ListBox;
			int num = (lastSelectedIndex = listBox.SelectedIndex);
			if (num != -1)
			{
				string text = listBox.Items[num++].ToString();
				if (!text.Contains(":"))
				{
					text += ": ";
				}
				string text2 = text.Split(':')[0].Trim();
				if (num > -1)
				{
					scrollFilterTabControl.Visibility = Visibility.Visible;
				}
				switch (text2.ToLower())
				{
				case "binarization":
				case "entropycrop":
					SetInInput(--num, new string[1] { "0" }, "Threshold");
					break;
				case "saturation":
				case "brightness":
				case "contrastex":
				case "contrast":
				case "alpha":
				case "scale":
					SetInInput(--num, new string[1] { "0" }, "Percentage");
					break;
				case "pixelate":
				case "resize":
					SetSize(--num);
					break;
				case "gamma":
				case "smooth":
				case "mean":
				case "colorthreshold":
				case "sharpenex":
				case "sharpen":
					SetInInput(--num, new string[1] { "0" });
					break;
				case "blur":
					SetSize(--num);
					break;
				case "constrain":
					SetSize(--num);
					break;
				case "vignette":
				case "tint":
				case "backgroundcolor":
					SetInInput(--num, new string[1] { "0,0,0" }, "Color(R,G,B)", color: true);
					break;
				case "gaussianblur":
				case "gaussiansharpen":
					SetInInput(--num, new string[1] { "0" }, "Size");
					break;
				case "rotate":
					SetInInput(--num, new string[1] { "0" }, "Degrees");
					break;
				case "halftone":
					SetInInputBoolean(--num, "False", "Comic Mode");
					break;
				case "edge":
				case "roundedcorners":
					SetInInput(--num, new string[1] { "0" }, "Radius");
					break;
				case "median":
					SetInInput(--num, new string[1] { "0" }, "ksize");
					break;
				case "crop":
					SetCropLayer(--num, new string[5]
					{
						controlCropLayer.LeftTextBox.Text,
						controlCropLayer.TopTextBox.Text,
						controlCropLayer.RightTextBox.Text,
						controlCropLayer.BottomTextBox.Text,
						"Percentage"
					});
					break;
				case "morphology":
					SetMorphology(--num);
					break;
				case "zoom":
					SetInInputTextAndBoolean(--num, "0", defBoolean: false, "Zoom Factor", "NearestNeighbor");
					break;
				case "hue":
					SetInInputTextAndBoolean(--num, "0", defBoolean: false, "Degrees", "Rotate (Any integer between 0 and 360)");
					break;
				case "adaptivethreshold":
					SetAdaptiveThreshold(--num, new string[5] { "1", "MeanC", "Binary", "1", "1" });
					break;
				case "threshold":
					SetThreshold(--num, new string[3] { "0", "255", "Binary" });
					break;
				case "replacecolor":
					SetReplaceColor(--num, new string[4] { "0,0,0", "|", "0,0,0", "0" });
					break;
				case "cvtcolor":
					SetControl(--num, "CvtColor", new ControlText<System.Windows.Controls.TextBox>[1]
					{
						new ControlText<System.Windows.Controls.TextBox>(controlCvtColor.dstCnTextBox, controlCvtColor.dstCnTextBox.Text)
					});
					break;
				case "alignment":
					SetInInput(--num, new string[1] { "4" }, "Alignment size(must be a power of two)");
					break;
				case "fastnlmeansdenoisingcolored":
					SetFastNlMeansDenoisingColored(--num, new string[2] { "3", "3" });
					break;
				case "resolution":
					SetResolution(--num, new string[3] { "0", "0", "Inch" });
					break;
				default:
					filterTabControl.SelectedIndex = -1;
					scrollFilterTabControl.Visibility = Visibility.Collapsed;
					break;
				}
			}
		}
		catch (IndexOutOfRangeException)
		{
		}
		catch (ArgumentOutOfRangeException)
		{
		}
		catch (ArgumentException)
		{
		}
		catch (Exception ex4)
		{
			System.Windows.MessageBox.Show(ex4.Message, "ERROR");
		}
	}

	private void SetInInput(int index, string[] defValues, string label = "Value", bool color = false)
	{
		inputControl.SetInputType(UserControlInput.InputType.Text);
		inputControl.label.Content = label + ":";
		filterTabControl.SelectIndexByHeaderName("Input");
		string empty = string.Empty;
		if (color)
		{
			string[] filterColors = GetFilterColors(index, defValues);
			empty = filterColors[0] + "," + filterColors[1] + "," + filterColors[2];
		}
		else
		{
			empty = GetFilterValue(index, defValues);
		}
		if (inputControl.InputTextBox.Text != empty)
		{
			inputControl.InputTextBox.Text = empty;
			SetCaretIndexAndSelect(inputControl.InputTextBox);
		}
	}

	private void SetEnum<TEnum>(int index, string defValue, string label = "Select")
	{
		if (controlEnumBox.EnumComboBox.Items.Count == 0 || controlEnumBox.TEnumName != typeof(TEnum).Name)
		{
			controlEnumBox.AddEnum<TEnum>();
		}
		filterTabControl.SelectIndexByHeaderName("Enum");
		controlEnumBox.label.Content = label + ":";
		string filterValue = GetFilterValue(index, new string[1] { defValue });
		controlEnumBox.EnumComboBox.SelectedItem = filterValue;
	}

	private void SetSize(int index)
	{
		filterTabControl.SelectIndexByHeaderName("Resize");
		string text = GetFilterValues(index, new string[2] { "0", "0" })[0];
		string text2 = GetFilterValues(index, new string[2] { "0", "0" })[1];
		if (!resizeControl.WidthTextBox.Text.Equals(text))
		{
			resizeControl.WidthTextBox.Text = text;
		}
		if (!resizeControl.HeightTextBox.Text.Equals(text2))
		{
			resizeControl.HeightTextBox.Text = text2;
		}
		SetCaretIndexAndSelect(resizeControl.WidthTextBox);
		SetCaretIndexAndSelect(resizeControl.HeightTextBox);
	}

	private void SetInputTextAndEnum<TEnum>(int index, string[] defValue, string labelInput = "Input", string labelSelect = "Select", bool reverse = false)
	{
		if (controlInputTextAndEnum.EnumComboBox.Items.Count == 0 || controlInputTextAndEnum.TEnumName != typeof(TEnum).Name)
		{
			controlInputTextAndEnum.AddEnum<TEnum>();
		}
		filterTabControl.SelectIndexByHeaderName("InputTextAndEnum");
		controlInputTextAndEnum.Reverse = reverse;
		controlInputTextAndEnum.labelInput.Content = labelInput + ":";
		controlInputTextAndEnum.labelSelect.Content = labelSelect + ":";
		string text = GetFilterValues(index, defValue)[reverse ? 1 : 0];
		string selectedItem = GetFilterValues(index, defValue)[(!reverse) ? 1u : 0u];
		controlInputTextAndEnum.EnumComboBox.SelectedItem = selectedItem;
		SetTextInTextBox(controlInputTextAndEnum.InputTextBox, text);
	}

	private void SetInInputBoolean(int index, string defValue, string label = "Value")
	{
		inputControl.SetInputType(UserControlInput.InputType.Boolean);
		inputControl.label.Content = label + ":";
		filterTabControl.SelectIndexByHeaderName("Input");
		inputControl.InputComboBox.SelectedIndex = (GetFilterValue(index, new string[1] { defValue }).ToBoolean() ? 1 : 0);
	}

	private void SetInInputTextAndBoolean(int index, string defValue, bool defBoolean, string labelVal = "Value", string labelBool = "Grayscale")
	{
		controlInputTextAndBool.label.Content = labelVal + ":";
		controlInputTextAndBool.CheckBox.Content = labelBool;
		filterTabControl.SelectIndexByHeaderName("InputTextAndBoolean");
		string text = GetFilterValues(index, new string[2]
		{
			defValue,
			defBoolean.ToString()
		})[0];
		SetTextInTextBox(controlInputTextAndBool.InputTextBox, text);
		controlInputTextAndBool.CheckBox.IsChecked = GetFilterValues(index, new string[2]
		{
			defValue,
			defBoolean.ToString()
		})[1].ToBoolean();
	}

	private void SetBlur(int index, string[] defValues)
	{
		filterTabControl.SelectIndexByHeaderName("Blur");
		string text = GetFilterValues(index, defValues)[0];
		string text2 = GetFilterValues(index, defValues)[1];
		string selectedItem = GetFilterValues(index, defValues)[2];
		SetTextInTextBox(blurControl.RadiusTextBox, text);
		SetTextInTextBox(blurControl.SigmaTextBox, text2);
		blurControl.ChannelsComboBox.SelectedItem = selectedItem;
		SetCaretIndexAndSelect(blurControl.RadiusTextBox);
		SetCaretIndexAndSelect(blurControl.SigmaTextBox);
	}

	private void SetThreshold(int index, string[] defValues)
	{
		filterTabControl.SelectIndexByHeaderName("Threshold");
		string text = GetFilterValues(index, defValues)[0];
		string text2 = GetFilterValues(index, defValues)[1];
		string selectedItem = GetFilterValues(index, defValues)[2];
		SetTextInTextBox(controlThreshold.ThreshTextBox, text);
		SetTextInTextBox(controlThreshold.MaxValueTextBox, text2);
		controlThreshold.ThresholdTypeComboBox.SelectedItem = selectedItem;
	}

	private void SetAdaptiveThreshold(int index, string[] defValues)
	{
		filterTabControl.SelectIndexByHeaderName("AdaptiveThreshold");
		string text = GetFilterValues(index, defValues)[0];
		string selectedItem = GetFilterValues(index, defValues)[1];
		string selectedItem2 = GetFilterValues(index, defValues)[2];
		string text2 = GetFilterValues(index, defValues)[3];
		string text3 = GetFilterValues(index, defValues)[4];
		SetTextInTextBox(controlAdaptiveThreshold.MaxValueTextBox, text);
		try
		{
			controlAdaptiveThreshold.AdaptiveMethodComboBox.SelectedItem = selectedItem;
		}
		catch
		{
		}
		try
		{
			controlAdaptiveThreshold.ThresholdTypeComboBox.SelectedItem = selectedItem2;
		}
		catch
		{
		}
		SetTextInTextBox(controlAdaptiveThreshold.BlockSizeTextBox, text2);
		SetTextInTextBox(controlAdaptiveThreshold.ConstantTextBox, text3);
	}

	private void SetFastNlMeansDenoisingColored(int index, string[] defValues)
	{
		filterTabControl.SelectIndexByHeaderName("FastNlMeansDenoisingColored");
		string text = GetFilterValues(index, defValues)[0];
		string text2 = GetFilterValues(index, defValues)[1];
		SetTextInTextBox(controlFastNlMeansDenoisingColored.StrengthTextBox, text);
		SetTextInTextBox(controlFastNlMeansDenoisingColored.ColorStrengthTextBox, text2);
	}

	private void SetReplaceColor(int index, string[] defValues)
	{
		filterTabControl.SelectIndexByHeaderName("ReplaceColor");
		if (!string.IsNullOrWhiteSpace(controlReplaceColor.TargetTextBox.Text) && !string.IsNullOrWhiteSpace(controlReplaceColor.ReplacementTextBox.Text))
		{
			try
			{
				string text = filterLB.Items[index].ToString();
				if (!text.Contains(":"))
				{
					text += ": ";
				}
				string[] array = text.Split(new char[1] { ':' }, 2)[1].Trim().Split(',');
				defValues = new string[8]
				{
					array[0],
					array[1],
					array[2],
					"|",
					array[3],
					array[4],
					array[5],
					array[6]
				};
			}
			catch
			{
			}
		}
		string text2 = defValues[0] + "," + defValues[1] + "," + defValues[2];
		string text3 = defValues[4] + "," + defValues[5] + "," + defValues[6];
		SetTextInTextBox(controlReplaceColor.TargetTextBox, text2);
		SetTextInTextBox(controlReplaceColor.ReplacementTextBox, text3);
		SetTextInTextBox(controlReplaceColor.FuzzinessTextBox, defValues[7]);
	}

	private void SetCropLayer(int index, string[] defValues)
	{
		filterTabControl.SelectIndexByHeaderName("CropLayer");
		string text = filterLB.SelectedItem.ToString();
		CropMode val = (CropMode)1;
		try
		{
			val = GetFilterValues(index, text.Split(','))[4].ToEnum<CropMode>((CropMode)1);
		}
		catch
		{
		}
		string[] filterValues = GetFilterValues(index, defValues);
		string text2 = filterValues[0];
		string text3 = filterValues[1];
		string text4 = filterValues[2];
		string text5 = filterValues[3];
		controlCropLayer.CropModeBox.SelectedItem = val;
		SetTextInTextBox(controlCropLayer.LeftTextBox, text2);
		SetTextInTextBox(controlCropLayer.TopTextBox, text3);
		SetTextInTextBox(controlCropLayer.RightTextBox, text4);
		SetTextInTextBox(controlCropLayer.BottomTextBox, text5);
		controlCropLayer.CropModeBox.SelectedItem = val;
	}

	private void SetMorphology(int index)
	{
		filterTabControl.SelectIndexByHeaderName("Morphology");
		string selectedItem = GetFilterValues(index, new string[1] { "Erode" })[0];
		string text = GetFilterValues(index, new string[3] { "Erode", "1", "Constant" })[1];
		string selectedItem2 = GetFilterValues(index, new string[3] { "Erode", "1", "Constant" })[2];
		string selectedItem3 = GetFilterValues(index, new string[4] { "Erode", "1", "Constant", "Null" })[3];
		string text2 = GetFilterValues(index, new string[5] { "Erode", "1", "Constant", "Null", "Null" })[4];
		string text3 = GetFilterValues(index, new string[6] { "Erode", "1", "Constant", "Null", "Null", "Null" })[5];
		try
		{
			controlMorphology.MorphMethodComboBox.SelectedItem = selectedItem;
		}
		catch
		{
		}
		try
		{
			controlMorphology.BorderTypeComboBox.SelectedItem = selectedItem2;
		}
		catch
		{
		}
		try
		{
			controlMorphology.MorphShapesComboBox.SelectedItem = selectedItem3;
		}
		catch
		{
		}
		SetTextInTextBox(controlMorphology.IterationsTextBox, text);
		SetTextInTextBox(controlMorphology.SizeWidthTextBox, text2);
		SetTextInTextBox(controlMorphology.SizeHeightTextBox, text3);
		SetCaretIndexAndSelect(controlMorphology.IterationsTextBox);
	}

	private void SetResolution(int index, string[] defValues)
	{
		filterTabControl.SelectIndexByHeaderName("Resolution");
		string s = GetFilterValues(index, defValues)[0];
		string s2 = GetFilterValues(index, defValues)[1];
		string text = GetFilterValues(index, defValues)[2];
		((UpDownBase<int?>)(object)controlResolution.HorizontalNumeric).Value = int.Parse(s);
		((UpDownBase<int?>)(object)controlResolution.VerticalNumeric).Value = int.Parse(s2);
		controlResolution.UnitComboBox.SelectedItem = text.ToEnum<PropertyTagResolutionUnit>((PropertyTagResolutionUnit)2);
	}

	private void SetControl(int index, string controlName, ControlText<System.Windows.Controls.TextBox>[] controls = null)
	{
		filterTabControl.SelectIndexByHeaderName(controlName);
		for (int i = 0; i < controls?.Length; i++)
		{
			SetTextInTextBox(controls[i].Control, controls[i].Text);
		}
	}

	private void SetTextInTextBox(System.Windows.Controls.TextBox textBox, string text)
	{
		if (textBox.Text != text)
		{
			textBox.Text = text;
		}
	}

	private void SetFilter(int index, string[] values)
	{
		try
		{
			string text = filterLB.Items[index].ToString();
			if (!text.Contains(":"))
			{
				text += ":";
			}
			string[] array = text.Split(new char[1] { ':' }, 2);
			string text2 = array[1];
			if (string.IsNullOrWhiteSpace(array[1]) && values.Length != 0)
			{
				for (int i = 0; i < values.Length; i++)
				{
					text2 = text2 + values[i] + ",";
				}
				text2 = text2.Trim().TrimEnd(',');
			}
			else if (values.Length != 0)
			{
				text2 = string.Empty;
				for (int j = 0; j < values.Length; j++)
				{
					text2 = text2 + values[j] + ",";
				}
				text2 = text2.Trim().TrimEnd(',');
			}
			GetSettings().FilterList[index] = array[0] + ": " + text2;
			filterLB.SelectedIndex = index;
		}
		catch
		{
		}
	}

	private string GetFilterValue(int index, string[] defaultValues, int parameterCount = 0)
	{
		string text = filterLB.Items[index].ToString();
		if (!text.Contains(":"))
		{
			text += ": ";
		}
		string text2 = text.Split(new char[1] { ':' }, 2)[1].Trim();
		string[] array;
		if (parameterCount > 0 && text2.Split(new char[1] { ',' }, parameterCount, StringSplitOptions.RemoveEmptyEntries).Length < parameterCount)
		{
			array = defaultValues;
			foreach (string text3 in array)
			{
				if (!text2.EndsWith(","))
				{
					text2 += ",";
				}
				text2 = text2 + text3 + ",";
			}
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2.Trim().TrimEnd(',');
		}
		array = defaultValues;
		foreach (string text4 in array)
		{
			text2 = text2 + text4 + ",";
		}
		return text2.Trim().TrimEnd(',');
	}

	private string[] GetFilterValues(int index, string[] defaultValues, char split = ',')
	{
		string text = filterLB.Items[index].ToString();
		if (!text.Contains(":"))
		{
			text += ": ";
		}
		string val = text.Split(new char[1] { ':' }, 2)[1].Trim();
		if (!string.IsNullOrWhiteSpace(val))
		{
			string[] array = val.Split(split);
			string text2 = string.Empty;
			for (int i = 0; i < defaultValues.Length; i++)
			{
				try
				{
					text2 = ((!string.IsNullOrEmpty(array[i])) ? (text2 + array[i] + split) : (text2 + defaultValues[i] + split));
				}
				catch
				{
					text2 = text2 + defaultValues[i] + split;
				}
			}
			return ((text2 == "") ? val : text2).Trim().TrimEnd(split).Split(split);
		}
		defaultValues.ToList().ForEach(delegate(string dv)
		{
			val = val + dv + split;
		});
		return val.Trim().TrimEnd(split).Split(split);
	}

	private string[] GetFilterColors(int index, string[] defaultValues)
	{
		string text = filterLB.Items[index].ToString();
		if (!text.Contains(":"))
		{
			text += ": ";
		}
		string[] array = text.Split(new char[1] { ':' }, 2)[1].Trim().Split(',');
		return new string[8]
		{
			array[0],
			array[1],
			array[2],
			"|",
			array[3],
			array[4],
			array[5],
			array[6]
		};
	}

	private void SetCaretIndexAndSelect(System.Windows.Controls.TextBox textBox, int index = 1)
	{
		try
		{
			if (textBox.Text == "0")
			{
				textBox.CaretIndex = index;
				textBox.SelectAll();
			}
		}
		catch
		{
		}
	}

	private void inputControl_SetFilter(object sender, EventArgs e)
	{
		if (filterTabControl.SelectedIndex == filterTabControl.GetIndexByItemName((e as TextChangedEventArgs).Source.ToString()))
		{
			SetFilter(lastSelectedIndex, sender as string[]);
			filterLB_LostFocus(null, null);
		}
	}

	private void btnfilterUp_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			int selectedIndex = filterLB.SelectedIndex;
			if (selectedIndex > 0)
			{
				string item = filterLB.Items[selectedIndex].ToString();
				ConfigSettings settings = GetSettings();
				settings.FilterList.RemoveAt(selectedIndex);
				settings.FilterList.Insert(selectedIndex - 1, item);
				filterLB.SelectedIndex = selectedIndex - 1;
				SetFilters();
			}
		}
		catch
		{
		}
	}

	private void btnfilterDown_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			int selectedIndex = filterLB.SelectedIndex;
			if (selectedIndex + 1 < filterLB.Items.Count)
			{
				string item = filterLB.Items[selectedIndex].ToString();
				ConfigSettings settings = GetSettings();
				settings.FilterList.RemoveAt(selectedIndex);
				settings.FilterList.Insert(selectedIndex + 1, item);
				filterLB.SelectedIndex = selectedIndex + 1;
				filterLB_LostFocus(null, null);
			}
		}
		catch
		{
		}
	}

	private void btnfilterClone_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (filterLB.Items.Count != 0 && lastSelectedIndex != -1)
			{
				GetSettings().FilterList.Add(filterLB.SelectedItem.ToString());
			}
		}
		catch
		{
		}
	}

	private void btnApplyFilters_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (OrigImage.Image != null)
			{
				Bitmap image = blockOcr.ApplyFilters(OrigImage.Image.Clone() as Bitmap, (BotData)null);
				ProcImage.Image = image;
			}
		}
		catch (Exception ex)
		{
			System.Windows.Forms.MessageBox.Show(ex.Message + "\nInnerException: " + ex.InnerException?.Message + "\n" + ex.InnerException?.InnerException?.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void MenuItem_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (filterLB.Items.Count == 0)
			{
				return;
			}
			Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
			{
				Filter = "Text|*.txt"
			};
			if (saveFileDialog.ShowDialog() != true)
			{
				return;
			}
			using StreamWriter streamWriter = new StreamWriter(saveFileDialog.FileName);
			foreach (object item in (IEnumerable)filterLB.Items)
			{
				streamWriter.WriteLine(item.ToString());
			}
		}
		catch
		{
		}
	}

	private void MenuItem1_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (filterLB.Items.Count == 0)
			{
				return;
			}
			string text = string.Empty;
			foreach (object selectedItem in filterLB.SelectedItems)
			{
				text += selectedItem;
				if (!selectedItem.Equals(filterLB.SelectedItems.OfType<string>().Last()))
				{
					text += "\n";
				}
			}
			System.Windows.Clipboard.SetText(text);
		}
		catch
		{
		}
	}

	private void MenuItem_Click_1(object sender, RoutedEventArgs e)
	{
		try
		{
			Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
			{
				Filter = "Text|*.txt"
			};
			if (openFileDialog.ShowDialog() == true)
			{
				string[] array = File.ReadAllLines(openFileDialog.FileName);
				CollectionUtils.AddRange<string>((IList<string>)GetSettings().FilterList, (IEnumerable<string>)array);
				if (array.Length != 0)
				{
					filterLB_LostFocus(null, null);
				}
			}
		}
		catch
		{
		}
	}

	private void sizeModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (ProcImage != null)
			{
				ProcImage.SizeMode = (PictureBoxSizeMode)Enum.Parse(typeof(PictureBoxSizeMode), (sizeModeBox.SelectedItem as ComboBoxItem).Content.ToString(), ignoreCase: true);
			}
		}
		catch
		{
		}
	}

	private ConfigSettings GetSettings()
	{
		object dataContext = base.DataContext;
		return (ConfigSettings)((dataContext is ConfigSettings) ? dataContext : null);
	}

	private void MenuItem_Click_2(object sender, RoutedEventArgs e)
	{
		try
		{
			ConfigSettings settings = GetSettings();
			int selectedIndex = filterLB.SelectedIndex;
			string text = settings.FilterList[selectedIndex];
			if (!text.StartsWith("!"))
			{
				text = "!" + text;
				settings.FilterList[selectedIndex] = text;
				filterTabControl.SelectedIndex = -1;
				scrollFilterTabControl.Visibility = Visibility.Collapsed;
				filterLB_LostFocus(null, null);
			}
		}
		catch
		{
		}
	}

	private void MenuItem_Click_3(object sender, RoutedEventArgs e)
	{
		try
		{
			ConfigSettings settings = GetSettings();
			int selectedIndex = filterLB.SelectedIndex;
			string text = settings.FilterList[selectedIndex];
			if (text.StartsWith("!"))
			{
				settings.FilterList[selectedIndex] = text.Remove(0, 1);
				filterLB_LostFocus(null, null);
			}
		}
		catch
		{
		}
	}

	private void langBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			blockOcr.OcrLang = langBox.SelectedItem.ToString();
		}
		catch
		{
		}
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			if (GetSettings().FilterList.Count > 0 && blockOcr.Filters.Count == 0)
			{
				filterLB_LostFocus(null, null);
			}
		}
		catch
		{
		}
		try
		{
			chbisBase64_Click(null, null);
		}
		catch
		{
		}
	}

	private void MenuItem_Click_4(object sender, RoutedEventArgs e)
	{
		try
		{
			try
			{
				string text = System.Windows.Clipboard.GetText();
				string item = text;
				if (text.Contains(":"))
				{
					item = text.Split(':')[0].Trim();
				}
				if (filterBox.Items.Contains(item))
				{
					GetSettings().FilterList.Add(text);
				}
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private void ProcImage_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
	{
		try
		{
			if (ProcImage.Image != null)
			{
				Color color = GetPixelInfo(e.X, e.Y);
				System.Windows.Clipboard.SetText(color.R + "," + color.G + "," + color.B);
			}
		}
		catch
		{
		}
	}

	private void ProcImage_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
	{
		try
		{
			if (ProcImage.Image != null && !clicked)
			{
				Color color = GetPixelInfo(e.X, e.Y);
				pixelInfo.Text = e.X + "," + e.Y + ": RGBA(" + color.R + "-" + color.G + "-" + color.B + "-" + color.A + "),Saturation(" + color.GetSaturation() + "),Brightness(" + color.GetBrightness() + ")";
			}
		}
		catch
		{
		}
	}

	private Color GetPixelInfo(int x, int y)
	{
		try
		{
			Bitmap bitmap = (Bitmap)ProcImage.Image;
			float num = (float)ProcImage.Width / (float)bitmap.Width;
			float num2 = (float)ProcImage.Height / (float)bitmap.Height;
			return bitmap.GetPixel((int)((float)x / num), (int)((float)y / num2));
		}
		catch (Exception ex)
		{
			if (!ex.Message.Contains("Parameter must be positive and < Height"))
			{
				SB.Logger.Log(ex.Message, (LogLevel)2, prompt: true);
			}
			return Color.Transparent;
		}
	}

	private void pixelInfo_MouseDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (e.ClickCount == 2)
			{
				System.Windows.Clipboard.SetText(pixelInfo.Text);
			}
		}
		catch
		{
		}
	}

	private void ProcImage_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
	{
		try
		{
			clicked = true;
		}
		catch
		{
		}
	}

	private void ProcImage_MouseLeave(object sender, EventArgs e)
	{
		try
		{
			clicked = false;
		}
		catch
		{
		}
	}

	private void filterLB_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (e.ChangedButton == MouseButton.Left && filterLB.Items.Count > 0 && filterLB.SelectedIndex > -1)
			{
				filterLB_SelectionChanged(filterLB, null);
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/configs/configocrsettings.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((ConfigOcrSettings)target).Loaded += Page_Loaded;
			break;
		case 2:
			OcrUrl = (System.Windows.Controls.TextBox)target;
			OcrUrl.TextChanged += OcrUrl_TextChanged;
			break;
		case 3:
			btnLoad = (System.Windows.Controls.Button)target;
			btnLoad.Click += btnLoad_Click;
			break;
		case 4:
			langBox = (System.Windows.Controls.ComboBox)target;
			langBox.SelectionChanged += langBox_SelectionChanged;
			break;
		case 5:
			chbProxy = (System.Windows.Controls.CheckBox)target;
			break;
		case 6:
			proxyTextbox = (System.Windows.Controls.TextBox)target;
			break;
		case 7:
			proxyTypeCombobox = (System.Windows.Controls.ComboBox)target;
			break;
		case 8:
			chbisBase64 = (System.Windows.Controls.CheckBox)target;
			chbisBase64.Click += chbisBase64_Click;
			break;
		case 9:
			chbAutoLoad = (System.Windows.Controls.CheckBox)target;
			chbAutoLoad.Click += chbAutoLoad_Click;
			break;
		case 10:
			engineComboBox = (System.Windows.Controls.ComboBox)target;
			break;
		case 11:
			pageSegComboBox = (System.Windows.Controls.ComboBox)target;
			break;
		case 12:
			chbEvaluateMath = (System.Windows.Controls.CheckBox)target;
			break;
		case 13:
			filterBox = (System.Windows.Controls.ComboBox)target;
			break;
		case 14:
			((System.Windows.Controls.Button)target).Click += Button_Click_2;
			break;
		case 15:
			scrollFilterTabControl = (ScrollViewer)target;
			break;
		case 16:
			filterTabControl = (System.Windows.Controls.TabControl)target;
			break;
		case 17:
			emptyTab = (TabItem)target;
			break;
		case 18:
			resizeControl = (UserControlResize)target;
			break;
		case 19:
			inputControl = (UserControlInput)target;
			break;
		case 20:
			blurControl = (UserControlBlur)target;
			break;
		case 21:
			controlInputTextAndBool = (UserControlInputTextAndBoolean)target;
			break;
		case 22:
			controlThreshold = (UserControlThreshold)target;
			break;
		case 23:
			controlAdaptiveThreshold = (UserControlAdaptiveThreshold)target;
			break;
		case 24:
			controlCropLayer = (UserControlCropLayer)target;
			break;
		case 25:
			controlEnumBox = (UserControlEnumBox)target;
			break;
		case 26:
			controlInputTextAndEnum = (UserControlInputTextAndEnum)target;
			break;
		case 27:
			controlMorphology = (UserControlMorphology)target;
			break;
		case 28:
			controlReplaceColor = (UserControlReplaceColor)target;
			break;
		case 29:
			controlCvtColor = (UserControlCvtColor)target;
			break;
		case 30:
			controlFastNlMeansDenoisingColored = (UserControlFastNlMeansDenoisingColored)target;
			break;
		case 31:
			controlResolution = (UserControlResolution)target;
			break;
		case 32:
			filterLB = (System.Windows.Controls.ListBox)target;
			filterLB.MouseDoubleClick += filterLB_MouseDoubleClick;
			filterLB.SelectionChanged += filterLB_SelectionChanged;
			break;
		case 33:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem1_Click;
			break;
		case 34:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem_Click_4;
			break;
		case 35:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem_Click_1;
			break;
		case 36:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem_Click;
			break;
		case 37:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem_Click_3;
			break;
		case 38:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem_Click_2;
			break;
		case 39:
			btnfilterClone = (System.Windows.Controls.Button)target;
			btnfilterClone.Click += btnfilterClone_Click;
			break;
		case 40:
			btnfilterUp = (System.Windows.Controls.Button)target;
			btnfilterUp.Click += btnfilterUp_Click;
			break;
		case 41:
			btnfilterDown = (System.Windows.Controls.Button)target;
			btnfilterDown.Click += btnfilterDown_Click;
			break;
		case 42:
			btnfilterRemove = (System.Windows.Controls.Button)target;
			btnfilterRemove.Click += btnfilterRemove_Click;
			break;
		case 43:
			btnfilterClear = (System.Windows.Controls.Button)target;
			btnfilterClear.Click += btnfilterClear_Click;
			break;
		case 44:
			OrigImage = (PictureBox)target;
			break;
		case 45:
			btnApplyFilters = (System.Windows.Controls.Button)target;
			btnApplyFilters.Click += btnApplyFilters_Click;
			break;
		case 46:
			btnRefresh = (System.Windows.Controls.Button)target;
			btnRefresh.Click += btnRefresh_Click;
			break;
		case 47:
			btnSave = (System.Windows.Controls.Button)target;
			btnSave.Click += btnSave_Click;
			break;
		case 48:
			((System.Windows.Controls.Button)target).Click += Button_Click_1;
			break;
		case 49:
			sizeModeBox = (System.Windows.Controls.ComboBox)target;
			sizeModeBox.SelectionChanged += sizeModeBox_SelectionChanged;
			break;
		case 50:
			ocrRateTextblock = (TextBlock)target;
			break;
		case 51:
			ProcImageBorder = (Border)target;
			break;
		case 52:
			ProcImage = (PictureBox)target;
			ProcImage.MouseDoubleClick += ProcImage_MouseDoubleClick;
			ProcImage.MouseDown += ProcImage_MouseDown;
			ProcImage.MouseLeave += ProcImage_MouseLeave;
			ProcImage.MouseMove += ProcImage_MouseMove;
			break;
		case 53:
			pixelInfo = (System.Windows.Controls.TextBox)target;
			pixelInfo.MouseDown += pixelInfo_MouseDown;
			break;
		case 54:
			resultOcrTextbox = (System.Windows.Controls.TextBox)target;
			break;
		case 55:
			((System.Windows.Controls.Button)target).Click += Button_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
