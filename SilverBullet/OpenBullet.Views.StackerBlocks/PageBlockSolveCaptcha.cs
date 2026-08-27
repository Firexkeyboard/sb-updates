using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CaptchaSharp.Enums;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockSolveCaptcha : Page, IComponentConnector
{
	private BlockSolveCaptcha vm;

	internal ComboBox captchaTypeCombobox;

	internal TabControl captchaTypeTabControl;

	internal ComboBox textLanguageGroupCombobox;

	internal ComboBox textLanguageCombobox;

	internal ComboBox imageLanguageGroupCombobox;

	internal ComboBox imageLanguageCombobox;

	internal ComboBox charSetCombobox;

	private bool _contentLoaded;

	public PageBlockSolveCaptcha(BlockSolveCaptcha block)
	{
		InitializeComponent();
		vm = block;
		base.DataContext = vm;
		string[] names = Enum.GetNames(typeof(CaptchaType));
		foreach (string newItem in names)
		{
			captchaTypeCombobox.Items.Add(newItem);
		}
		captchaTypeCombobox.SelectedIndex = (int)vm.Type;
		names = Enum.GetNames(typeof(CaptchaLanguageGroup));
		foreach (string newItem2 in names)
		{
			textLanguageGroupCombobox.Items.Add(newItem2);
			imageLanguageGroupCombobox.Items.Add(newItem2);
		}
		textLanguageGroupCombobox.SelectedIndex = (int)vm.LanguageGroup;
		imageLanguageGroupCombobox.SelectedIndex = (int)vm.LanguageGroup;
		names = Enum.GetNames(typeof(CaptchaLanguage));
		foreach (string newItem3 in names)
		{
			textLanguageCombobox.Items.Add(newItem3);
			imageLanguageCombobox.Items.Add(newItem3);
		}
		textLanguageCombobox.SelectedIndex = (int)vm.Language;
		imageLanguageCombobox.SelectedIndex = (int)vm.Language;
		names = Enum.GetNames(typeof(CharacterSet));
		foreach (string newItem4 in names)
		{
			charSetCombobox.Items.Add(newItem4);
		}
		charSetCombobox.SelectedIndex = (int)vm.CharSet;
	}

	private void captchaTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.Type = (CaptchaType)((ComboBox)e.OriginalSource).SelectedIndex;
		Dictionary<CaptchaType, int> dictionary = new Dictionary<CaptchaType, int>
		{
			{
				(CaptchaType)0,
				0
			},
			{
				(CaptchaType)1,
				1
			},
			{
				(CaptchaType)3,
				2
			},
			{
				(CaptchaType)4,
				3
			},
			{
				(CaptchaType)2,
				4
			},
			{
				(CaptchaType)5,
				5
			},
			{
				(CaptchaType)6,
				6
			},
			{
				(CaptchaType)7,
				7
			},
			{
				(CaptchaType)8,
				8
			}
		};
		captchaTypeTabControl.SelectedIndex = dictionary[vm.Type];
	}

	private void languageGroupCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.LanguageGroup = (CaptchaLanguageGroup)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void languageCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.Language = (CaptchaLanguage)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void charSetCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.CharSet = (CharacterSet)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblocksolvecaptcha.xaml", UriKind.Relative);
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
			captchaTypeCombobox = (ComboBox)target;
			captchaTypeCombobox.SelectionChanged += captchaTypeCombobox_SelectionChanged;
			break;
		case 2:
			captchaTypeTabControl = (TabControl)target;
			break;
		case 3:
			textLanguageGroupCombobox = (ComboBox)target;
			textLanguageGroupCombobox.SelectionChanged += languageGroupCombobox_SelectionChanged;
			break;
		case 4:
			textLanguageCombobox = (ComboBox)target;
			textLanguageCombobox.SelectionChanged += languageCombobox_SelectionChanged;
			break;
		case 5:
			imageLanguageGroupCombobox = (ComboBox)target;
			imageLanguageGroupCombobox.SelectionChanged += languageGroupCombobox_SelectionChanged;
			break;
		case 6:
			imageLanguageCombobox = (ComboBox)target;
			imageLanguageCombobox.SelectionChanged += languageCombobox_SelectionChanged;
			break;
		case 7:
			charSetCombobox = (ComboBox)target;
			charSetCombobox.SelectionChanged += charSetCombobox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
