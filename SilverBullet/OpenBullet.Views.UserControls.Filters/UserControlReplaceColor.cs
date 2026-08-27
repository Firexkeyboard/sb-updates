using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlReplaceColor : UserControl, IComponentConnector
{
	public const string ControlName = "ReplaceColor";

	internal TextBox TargetTextBox;

	internal TextBox ReplacementTextBox;

	internal TextBox FuzzinessTextBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlReplaceColor()
	{
		InitializeComponent();
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "ReplaceColor";
		this.SetFilter?.Invoke(new string[3] { TargetTextBox.Text, ReplacementTextBox.Text, FuzzinessTextBox.Text }, e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolreplacecolor.xaml", UriKind.Relative);
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
			TargetTextBox = (TextBox)target;
			TargetTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 2:
			ReplacementTextBox = (TextBox)target;
			ReplacementTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 3:
			FuzzinessTextBox = (TextBox)target;
			FuzzinessTextBox.TextChanged += TextBox_TextChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
