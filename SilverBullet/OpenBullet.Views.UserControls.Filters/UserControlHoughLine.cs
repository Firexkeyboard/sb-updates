using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlHoughLine : UserControl, IComponentConnector
{
	public const string ControlName = "HoughLine";

	internal TextBox WidthTextBox;

	internal TextBox HeightTextBox;

	internal TextBox ThresholdTextBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlHoughLine()
	{
		InitializeComponent();
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "HoughLine";
		this.SetFilter?.Invoke(new string[3] { WidthTextBox.Text, HeightTextBox.Text, ThresholdTextBox.Text }, e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolhoughline.xaml", UriKind.Relative);
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
			WidthTextBox = (TextBox)target;
			WidthTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 2:
			HeightTextBox = (TextBox)target;
			HeightTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 3:
			ThresholdTextBox = (TextBox)target;
			ThresholdTextBox.TextChanged += TextBox_TextChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
