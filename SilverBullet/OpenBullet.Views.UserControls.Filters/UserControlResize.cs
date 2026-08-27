using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlResize : UserControl, IComponentConnector
{
	public const string ControlName = "Resize";

	internal TextBox WidthTextBox;

	internal TextBox HeightTextBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlResize()
	{
		InitializeComponent();
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "Resize";
		this.SetFilter?.Invoke(new string[2] { WidthTextBox.Text, HeightTextBox.Text }, e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolresize.xaml", UriKind.Relative);
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
		default:
			_contentLoaded = true;
			break;
		}
	}
}
