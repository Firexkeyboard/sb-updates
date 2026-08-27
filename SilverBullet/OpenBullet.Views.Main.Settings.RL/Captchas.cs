using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CaptchaSharp.Exceptions;
using RuriLib;
using RuriLib.Enums;
using RuriLib.Functions.Captchas;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main.Settings.RL;

public class Captchas : Page, IComponentConnector
{
	internal ComboBox currentServiceCombobox;

	internal TabControl captchaServiceTabControl;

	internal Button checkBalanceButton;

	internal Label balanceLabel;

	private bool _contentLoaded;

	public Captchas()
	{
		InitializeComponent();
		base.DataContext = SB.Settings.RLSettings.Captchas;
		string[] names = Enum.GetNames(typeof(CaptchaServiceType));
		foreach (string newItem in names)
		{
			currentServiceCombobox.Items.Add(newItem);
		}
		currentServiceCombobox.SelectedIndex = (int)SB.Settings.RLSettings.Captchas.CurrentService;
	}

	private void currentServiceCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		SB.Settings.RLSettings.Captchas.CurrentService = (CaptchaServiceType)currentServiceCombobox.SelectedIndex;
		Dictionary<CaptchaServiceType, int> dictionary = new Dictionary<CaptchaServiceType, int>
		{
			{
				(CaptchaServiceType)0,
				0
			},
			{
				(CaptchaServiceType)1,
				1
			},
			{
				(CaptchaServiceType)2,
				2
			},
			{
				(CaptchaServiceType)6,
				2
			},
			{
				(CaptchaServiceType)3,
				3
			},
			{
				(CaptchaServiceType)4,
				4
			},
			{
				(CaptchaServiceType)5,
				5
			},
			{
				(CaptchaServiceType)7,
				6
			},
			{
				(CaptchaServiceType)8,
				7
			},
			{
				(CaptchaServiceType)9,
				8
			},
			{
				(CaptchaServiceType)10,
				9
			},
			{
				(CaptchaServiceType)11,
				10
			},
			{
				(CaptchaServiceType)12,
				11
			}
		};
		captchaServiceTabControl.SelectedIndex = dictionary[SB.Settings.RLSettings.Captchas.CurrentService];
	}

	private async void checkBalanceButton_Click(object sender, RoutedEventArgs e)
	{
		IOManager.SaveSettings<RLSettingsViewModel>(SB.rlSettingsFile, SB.Settings.RLSettings);
		try
		{
			decimal num = await RuriLib.Functions.Captchas.Captchas.GetService(SB.Settings.RLSettings.Captchas).GetBalanceAsync(default(CancellationToken));
			balanceLabel.Content = num;
			balanceLabel.Foreground = ((num > 0m) ? Utils.GetBrush("ForegroundGood") : Utils.GetBrush("ForegroundBad"));
		}
		catch (BadAuthenticationException)
		{
			balanceLabel.Content = "WRONG TOKEN / CREDENTIALS";
			balanceLabel.Foreground = Utils.GetBrush("ForegroundBad");
		}
		catch
		{
			balanceLabel.Content = "AN ERROR OCCURRED";
			balanceLabel.Foreground = Utils.GetBrush("ForegroundBad");
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/settings/rurilib/captchas.xaml", UriKind.Relative);
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
			currentServiceCombobox = (ComboBox)target;
			currentServiceCombobox.SelectionChanged += currentServiceCombobox_SelectionChanged;
			break;
		case 2:
			captchaServiceTabControl = (TabControl)target;
			break;
		case 3:
			checkBalanceButton = (Button)target;
			checkBalanceButton.Click += checkBalanceButton_Click;
			break;
		case 4:
			balanceLabel = (Label)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
