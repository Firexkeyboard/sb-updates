using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlFastNlMeansDenoisingColored : UserControl, IComponentConnector
{
	public const string ControlName = "FastNlMeansDenoisingColored";

	internal TextBox StrengthTextBox;

	internal TextBox ColorStrengthTextBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlFastNlMeansDenoisingColored()
	{
		InitializeComponent();
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "FastNlMeansDenoisingColored";
		this.SetFilter?.Invoke(new string[2] { StrengthTextBox.Text, ColorStrengthTextBox.Text }, e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolfastnlmeansdenoisingcolored.xaml", UriKind.Relative);
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
			StrengthTextBox = (TextBox)target;
			StrengthTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 2:
			ColorStrengthTextBox = (TextBox)target;
			ColorStrengthTextBox.TextChanged += TextBox_TextChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
