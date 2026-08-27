using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockParse : Page, IComponentConnector
{
	private BlockParse vm;

	internal CheckBox captureBox;

	internal RadioButton LRRadio;

	internal RadioButton CSSRadio;

	internal RadioButton JSONRadio;

	internal RadioButton REGEXRadio;

	internal TabControl typeTabControl;

	internal RichTextBox LRRTB;

	private bool _contentLoaded;

	public PageBlockParse(BlockParse block)
	{
		InitializeComponent();
		vm = block;
		base.DataContext = vm;
		}

	private void LRRadio_Checked(object sender, RoutedEventArgs e)
	{
		typeTabControl.SelectedIndex = 0;
	}

	private void CSSRadio_Checked(object sender, RoutedEventArgs e)
	{
		typeTabControl.SelectedIndex = 1;
	}

	private void JSONRadio_Checked(object sender, RoutedEventArgs e)
	{
		typeTabControl.SelectedIndex = 2;
	}

	private void REGEXRadio_Checked(object sender, RoutedEventArgs e)
	{
		typeTabControl.SelectedIndex = 3;
	}

	private void LRRTB_KeyUp(object sender, KeyEventArgs e)
	{
		try
		{
			if (e.Key != Key.LeftShift)
			{
				return;
			}
			int length = new TextRange(LRRTB.Document.ContentStart, LRRTB.Selection.Start).Text.Length;
			int length2 = LRRTB.Selection.Text.Length;
			int num = length + length2 - 1;
			string text = "";
			string text2 = "";
			int num2 = length;
			while (num2 != 0)
			{
				text = LRRTB.GetText()[num2 - 1] + text;
				num2--;
				if (BlockFunction.CountStringOccurrences(LRRTB.GetText(), text) <= 1)
				{
					break;
				}
			}
			num2 = num;
			while (num2 != LRRTB.GetText().Length - 1)
			{
				text2 += LRRTB.GetText()[num2 + 1];
				num2++;
				if (BlockFunction.CountStringOccurrences(LRRTB.GetText(), text2) <= 1)
				{
					break;
				}
			}
			vm.LeftString = text;
			vm.RightString = text2;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString());
		}
	}

	private void CaptureBox_Click(object sender, RoutedEventArgs e)
	{
		if (vm.IsCapture)
		{
			vm.CreateEmpty = false;
		}
		else
		{
			vm.CreateEmpty = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblockparse.xaml", UriKind.Relative);
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
			captureBox = (CheckBox)target;
			captureBox.Click += CaptureBox_Click;
			break;
		case 2:
			LRRadio = (RadioButton)target;
			LRRadio.Checked += LRRadio_Checked;
			break;
		case 3:
			CSSRadio = (RadioButton)target;
			CSSRadio.Checked += CSSRadio_Checked;
			break;
		case 4:
			JSONRadio = (RadioButton)target;
			JSONRadio.Checked += JSONRadio_Checked;
			break;
		case 5:
			REGEXRadio = (RadioButton)target;
			REGEXRadio.Checked += REGEXRadio_Checked;
			break;
		case 6:
			typeTabControl = (TabControl)target;
			break;
		case 7:
			LRRTB = (RichTextBox)target;
			LRRTB.KeyUp += LRRTB_KeyUp;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
