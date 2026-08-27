using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CaptchaSharp.Enums;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockReportCaptcha : Page, IComponentConnector
{
	private BlockReportCaptcha vm;

	internal ComboBox captchaTypeCombobox;

	private bool _contentLoaded;

	public PageBlockReportCaptcha(BlockReportCaptcha block)
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
	}

	private void captchaTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.Type = (CaptchaType)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblockreportcaptcha.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			captchaTypeCombobox = (ComboBox)target;
			captchaTypeCombobox.SelectionChanged += captchaTypeCombobox_SelectionChanged;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
