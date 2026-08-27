using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using AngleSharp.Text;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.MetaData;
using Microsoft.Win32;
using OpenBullet.Models;
using OpenBullet.Views.Main.Configs;
using OpenBullet.Views.UserControls.Filters;
using RuriLib;
using Tesseract;
using Xceed.Wpf.Toolkit.Primitives;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockOcr : System.Windows.Controls.Page, IComponentConnector
{
	private BlockOcr vm;

	private int lastSelectedIndex = -1;

	internal System.Windows.Controls.ComboBox LangComboBox;

	internal System.Windows.Controls.ComboBox EngineModeComboBox;

	internal System.Windows.Controls.ComboBox PageSegModeComboBox;

	internal System.Windows.Controls.ComboBox pixelFormatComboBox;

	internal System.Windows.Controls.ComboBox secProtoComboBox;

	internal System.Windows.Controls.ComboBox filterComboBox;

	internal System.Windows.Controls.Button btnAddFilter;

	internal System.Windows.Controls.ListBox filterLB;

	internal System.Windows.Controls.Button btnfilterClone;

	internal System.Windows.Controls.Button btnfilterUp;

	internal System.Windows.Controls.Button btnfilterDown;

	internal System.Windows.Controls.Button btnfilterRemove;

	internal System.Windows.Controls.Button btnfilterClear;

	internal System.Windows.Controls.GroupBox filterGroupBox;

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

	internal UserControlFastNlMeansDenoisingColored controlFastNlMeansDenoisingColored;

	internal UserControlCvtColor controlCvtColor;

	internal UserControlResolution controlResolution;

	internal System.Windows.Controls.RichTextBox customHeadersRTB;

	private bool _contentLoaded;

	public PageBlockOcr(BlockOcr block)
	{
		InitializeComponent();
		vm = block;
		base.DataContext = vm;
		customHeadersRTB.AppendText(vm.GetCustomHeaders());
		try
		{
			string[] array = vm.GetFilters().Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				filterLB.Items.Add(text.Trim());
			}
		}
		catch
		{
		}
		vm.PixelFormats.Add("Default");
		vm.PixelFormats.AddRange(Enum.GetNames(typeof(PixelFormat)));
		Enum.GetNames(typeof(EngineMode)).ToList().ForEach(delegate(string e)
		{
			EngineModeComboBox.Items.Add(e);
		});
		Enum.GetNames(typeof(PageSegMode)).ToList().ForEach(delegate(string p)
		{
			PageSegModeComboBox.Items.Add(p);
		});
		InitFilterControls();
		SetItemToComboBox();
	}

	private void InitFilterControls()
	{
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		LoadTessData();
		LoadFilters();
		SetItemToComboBox();
	}

	private void Page_Initialized(object sender, EventArgs e)
	{
	}

	private void customHeadersRTB_LostFocus(object sender, RoutedEventArgs e)
	{
		vm.SetCustomHeaders(customHeadersRTB.Lines());
	}

	private void filterLB_LostFocus(object sender, RoutedEventArgs e)
	{
		try
		{
			vm.SetFilters(filterLB.Items.OfType<string>().ToArray());
		}
		catch
		{
		}
	}

	private void btnAddFilter_Click(object sender, RoutedEventArgs e)
	{
		filterLB.Items.Add(filterComboBox.Text + ": ");
		filterLB.SelectedIndex = filterLB.Items.Count - 1;
		filterLB_LostFocus(null, null);
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
				if (fileInfo.Name.Contains(".") && !vm.Languages.Contains(fileInfo.Name.Split('.')[0]))
				{
					vm.Languages.Add(fileInfo.Name.Split('.')[0]);
				}
			}
			try
			{
				vm.Languages = new ObservableCollection<string>(vm.Languages.Distinct());
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

	private void LoadFilters()
	{
		try
		{
			foreach (var processor in vm.Processors)
			{
				if (!filterComboBox.Items.Contains(processor.Item1))
				{
					filterComboBox.Items.Add(processor.Item1);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private void SetItemToComboBox()
	{
		try
		{
			LangComboBox.SelectedIndex = LangComboBox.Items.IndexOf(vm.OcrLang);
			EngineModeComboBox.SelectedIndex = EngineModeComboBox.Items.IndexOf(vm.Engine);
			PageSegModeComboBox.SelectedIndex = PageSegModeComboBox.Items.IndexOf(vm.PageSeg);
		}
		catch
		{
		}
	}

	private void btnfilterUp_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			int selectedIndex = filterLB.SelectedIndex;
			if (selectedIndex > 0)
			{
				object insertItem = filterLB.Items[selectedIndex];
				filterLB.Items.RemoveAt(selectedIndex);
				filterLB.Items.Insert(selectedIndex - 1, insertItem);
				filterLB.SelectedIndex = selectedIndex - 1;
				vm.SetFilters(filterLB.Items.OfType<string>().ToArray());
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
				object insertItem = filterLB.Items[selectedIndex];
				filterLB.Items.RemoveAt(selectedIndex);
				filterLB.Items.Insert(selectedIndex + 1, insertItem);
				filterLB.SelectedIndex = selectedIndex + 1;
				filterLB_LostFocus(null, null);
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
				filterLB.Items.RemoveAt(selectedIndex);
				filterTabControl.SelectedIndex = -1;
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
			if (filterLB.Items.Count != 0)
			{
				filterLB.Items.Add(filterLB.SelectedItem.ToString());
			}
		}
		catch
		{
		}
	}

	private void btnfilterClear_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (System.Windows.Forms.MessageBox.Show("Do you want to clear the list of filters?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.No)
			{
				filterLB.Items.Clear();
				vm.SetFilters(filterLB.Items.OfType<string>().ToArray());
			}
		}
		catch
		{
		}
	}

	private void filterLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			int num = (lastSelectedIndex = filterLB.SelectedIndex);
			if (num != -1)
			{
				string text = filterLB.Items[num++].ToString();
				if (!text.Contains(":"))
				{
					text += ": ";
				}
				string text2 = text.Split(':')[0].Trim();
				if (num > -1)
				{
					filterGroupBox.Visibility = Visibility.Visible;
					filterGroupBox.Header = text2;
				}
				switch (text2.ToLower())
				{
				case "binarization":
				case "entropycrop":
					SetInInput(--num, new string[1] { "0" }, "Threshold");
					break;
				case "contrastex":
				case "brightness":
				case "saturation":
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
				case "median":
					SetInInput(--num, new string[1] { "0" }, "ksize");
					break;
				case "coloralpha":
				case "vignette":
				case "tint":
				case "backgroundcolor":
					SetInInput(--num, new string[1] { "0,0,0" }, "Color(R,G,B)", color: true);
					break;
				case "gaussianblur":
				case "gaussiansharpen":
					SetInInput(--num, new string[1] { "0" }, "Size");
					break;
				case "alignment":
					SetInInput(--num, new string[1] { "4" }, "Alignment size(must be a power of two)");
					break;
				case "rotate":
					SetInInput(--num, new string[1] { "0" }, "Degrees");
					break;
				case "constrain":
					SetSize(--num);
					break;
				case "halftone":
					SetInInputBoolean(--num, "False", "Comic Mode");
					break;
				case "blur":
					SetSize(--num);
					break;
				case "edge":
				case "roundedcorners":
					SetInInput(--num, new string[1] { "0" }, "Radius");
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
				case "fastnlmeansdenoisingcolored":
					SetFastNlMeansDenoisingColored(--num, new string[2] { "3", "3" });
					break;
				case "resolution":
					SetResolution(--num, new string[3] { "0", "0", "Inch" });
					break;
				default:
					filterTabControl.SelectedIndex = -1;
					filterGroupBox.Visibility = Visibility.Collapsed;
					break;
				}
			}
		}
		catch (Exception)
		{
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

	private void SetFastNlMeansDenoisingColored(int index, string[] defValues)
	{
		filterTabControl.SelectIndexByHeaderName("FastNlMeansDenoisingColored");
		string text = GetFilterValues(index, defValues)[0];
		string text2 = GetFilterValues(index, defValues)[1];
		SetTextInTextBox(controlFastNlMeansDenoisingColored.StrengthTextBox, text);
		SetTextInTextBox(controlFastNlMeansDenoisingColored.ColorStrengthTextBox, text2);
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
			filterLB.Items[index] = array[0] + ": " + text2;
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
		string val = string.Empty;
		defaultValues.ToList().ForEach(delegate(string dv)
		{
			if (!dv.EndsWith("|"))
			{
				val = val + dv + "|";
			}
			else if (dv != "|")
			{
				val += dv;
			}
		});
		return val.Trim().TrimEnd('|').Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries);
	}

	private void SetCaretIndexAndSelect(System.Windows.Controls.TextBox textBox, string defVal = "0", int index = 1)
	{
		try
		{
			if (textBox.Text == defVal)
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

	private void MenuItem_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
			{
				Filter = "Text|*.txt"
			};
			if (openFileDialog.ShowDialog() == true)
			{
				File.ReadAllLines(openFileDialog.FileName).ToList().ForEach(delegate(string f)
				{
					filterLB.Items.Add(f);
				});
				filterLB_LostFocus(null, null);
			}
		}
		catch
		{
		}
	}

	private void MenuItem_Click_1(object sender, RoutedEventArgs e)
	{
		try
		{
			System.Windows.Clipboard.SetText(((object)vm.Filters[filterLB.SelectedIndex]).ToString());
		}
		catch
		{
		}
	}

	private void MenuItem_Click_2(object sender, RoutedEventArgs e)
	{
		try
		{
			string text = System.Windows.Clipboard.GetText();
			string item = text;
			if (text.Contains(":"))
			{
				item = text.Split(':')[0].Trim();
			}
			if (filterComboBox.Items.Contains(item))
			{
				filterLB.Items.Add(text);
			}
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

	private void MenuItem_Click_3(object sender, RoutedEventArgs e)
	{
		try
		{
			string item = filterLB.SelectedItem.ToString();
			object dataContext = new ConfigOcrSettings(sendFilter: true).DataContext;
			((ConfigSettings)((dataContext is ConfigSettings) ? dataContext : null)).FilterList.Add(item);
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblockocr.xaml", UriKind.Relative);
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
			((PageBlockOcr)target).Initialized += Page_Initialized;
			((PageBlockOcr)target).Loaded += Page_Loaded;
			break;
		case 2:
			LangComboBox = (System.Windows.Controls.ComboBox)target;
			break;
		case 3:
			EngineModeComboBox = (System.Windows.Controls.ComboBox)target;
			break;
		case 4:
			PageSegModeComboBox = (System.Windows.Controls.ComboBox)target;
			break;
		case 5:
			pixelFormatComboBox = (System.Windows.Controls.ComboBox)target;
			break;
		case 6:
			secProtoComboBox = (System.Windows.Controls.ComboBox)target;
			break;
		case 7:
			filterComboBox = (System.Windows.Controls.ComboBox)target;
			break;
		case 8:
			btnAddFilter = (System.Windows.Controls.Button)target;
			btnAddFilter.Click += btnAddFilter_Click;
			break;
		case 9:
			filterLB = (System.Windows.Controls.ListBox)target;
			filterLB.LostFocus += filterLB_LostFocus;
			filterLB.MouseDoubleClick += filterLB_MouseDoubleClick;
			filterLB.SelectionChanged += filterLB_SelectionChanged;
			break;
		case 10:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem_Click_1;
			break;
		case 11:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem_Click_2;
			break;
		case 12:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem_Click;
			break;
		case 13:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem_Click_3;
			break;
		case 14:
			btnfilterClone = (System.Windows.Controls.Button)target;
			btnfilterClone.Click += btnfilterClone_Click;
			break;
		case 15:
			btnfilterUp = (System.Windows.Controls.Button)target;
			btnfilterUp.Click += btnfilterUp_Click;
			break;
		case 16:
			btnfilterDown = (System.Windows.Controls.Button)target;
			btnfilterDown.Click += btnfilterDown_Click;
			break;
		case 17:
			btnfilterRemove = (System.Windows.Controls.Button)target;
			btnfilterRemove.Click += btnfilterRemove_Click;
			break;
		case 18:
			btnfilterClear = (System.Windows.Controls.Button)target;
			btnfilterClear.Click += btnfilterClear_Click;
			break;
		case 19:
			filterGroupBox = (System.Windows.Controls.GroupBox)target;
			break;
		case 20:
			filterTabControl = (System.Windows.Controls.TabControl)target;
			break;
		case 21:
			emptyTab = (TabItem)target;
			break;
		case 22:
			resizeControl = (UserControlResize)target;
			break;
		case 23:
			inputControl = (UserControlInput)target;
			break;
		case 24:
			blurControl = (UserControlBlur)target;
			break;
		case 25:
			controlInputTextAndBool = (UserControlInputTextAndBoolean)target;
			break;
		case 26:
			controlThreshold = (UserControlThreshold)target;
			break;
		case 27:
			controlAdaptiveThreshold = (UserControlAdaptiveThreshold)target;
			break;
		case 28:
			controlCropLayer = (UserControlCropLayer)target;
			break;
		case 29:
			controlEnumBox = (UserControlEnumBox)target;
			break;
		case 30:
			controlInputTextAndEnum = (UserControlInputTextAndEnum)target;
			break;
		case 31:
			controlMorphology = (UserControlMorphology)target;
			break;
		case 32:
			controlReplaceColor = (UserControlReplaceColor)target;
			break;
		case 33:
			controlFastNlMeansDenoisingColored = (UserControlFastNlMeansDenoisingColored)target;
			break;
		case 34:
			controlCvtColor = (UserControlCvtColor)target;
			break;
		case 35:
			controlResolution = (UserControlResolution)target;
			break;
		case 36:
			customHeadersRTB = (System.Windows.Controls.RichTextBox)target;
			customHeadersRTB.LostFocus += customHeadersRTB_LostFocus;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
