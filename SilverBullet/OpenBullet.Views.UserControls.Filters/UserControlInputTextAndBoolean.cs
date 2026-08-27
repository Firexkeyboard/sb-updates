using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlInputTextAndBoolean : UserControl, IComponentConnector
{
	public const string ControlName = "InputTextAndBoolean";

	internal Label label;

	internal TextBox InputTextBox;

	internal CheckBox CheckBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlInputTextAndBoolean()
	{
		InitializeComponent();
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "InputTextAndBoolean";
		this.SetFilter?.Invoke(new string[2]
		{
			InputTextBox.Text,
			(CheckBox.IsChecked == true).ToString()
		}, e);
	}

	private void CheckBox_Click(object sender, RoutedEventArgs e)
	{
		this.SetFilter?.Invoke(new string[2]
		{
			InputTextBox.Text,
			(CheckBox.IsChecked == true).ToString()
		}, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
		{
			Source = "InputTextAndBoolean"
		});
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolinputtextandboolean.xaml", UriKind.Relative);
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
			label = (Label)target;
			break;
		case 2:
			InputTextBox = (TextBox)target;
			InputTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 3:
			CheckBox = (CheckBox)target;
			CheckBox.Click += CheckBox_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
