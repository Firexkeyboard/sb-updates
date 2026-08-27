using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main.Configs.OtherOptions;

public class Inputs : Page, IComponentConnector, IStyleConnector
{
	private ConfigSettings vm;

	private Random rand = new Random();

	internal Button addInputButton;

	internal Button clearInputsButton;

	internal ItemsControl inputsControl;

	private bool _contentLoaded;

	public Inputs()
	{
		vm = SB.ConfigManager.CurrentConfig.Config.Settings;
		base.DataContext = vm;
		InitializeComponent();
	}

	private void clearInputsButton_Click(object sender, RoutedEventArgs e)
	{
		vm.CustomInputs.Clear();
	}

	private void addInputButton_Click(object sender, RoutedEventArgs e)
	{
		vm.CustomInputs.Add(new CustomInput(rand.Next()));
	}

	private void removeInputButton_Click(object sender, RoutedEventArgs e)
	{
		vm.RemoveCustomInputById((int)(sender as Button).Tag);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/configs/otheroptions/inputs.xaml", UriKind.Relative);
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
			addInputButton = (Button)target;
			addInputButton.Click += addInputButton_Click;
			break;
		case 2:
			clearInputsButton = (Button)target;
			clearInputsButton.Click += clearInputsButton_Click;
			break;
		case 3:
			inputsControl = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 4)
		{
			((Button)target).Click += removeInputButton_Click;
		}
	}
}
