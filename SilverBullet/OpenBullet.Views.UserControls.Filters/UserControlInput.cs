using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlInput : UserControl, IComponentConnector
{
	public enum InputType
	{
		Text,
		Boolean
	}

	public const string ControlName = "Input";

	internal Label label;

	internal TextBox InputTextBox;

	internal ComboBox InputComboBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlInput()
	{
		InitializeComponent();
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "Input";
		this.SetFilter?.Invoke(new string[1] { (sender as TextBox).Text }, e);
	}

	private void UserControl_Initialized(object sender, EventArgs e)
	{
		SetInputType(InputType.Text);
	}

	private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ComboBox comboBox = sender as ComboBox;
		if (comboBox.Visibility == Visibility.Visible)
		{
			this.SetFilter?.Invoke(new string[1] { (comboBox.SelectedItem as ComboBoxItem).Content.ToString() }, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
			{
				Source = "Input"
			});
		}
	}

	internal void SetInputType(InputType inputType)
	{
		switch (inputType)
		{
		case InputType.Text:
			InputComboBox.Visibility = Visibility.Collapsed;
			InputTextBox.Visibility = Visibility.Visible;
			break;
		case InputType.Boolean:
			InputTextBox.Visibility = Visibility.Collapsed;
			InputComboBox.Visibility = Visibility.Visible;
			break;
		default:
			InputTextBox.Visibility = Visibility.Visible;
			InputComboBox.Visibility = Visibility.Collapsed;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolinput.xaml", UriKind.Relative);
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
			((UserControlInput)target).Initialized += UserControl_Initialized;
			break;
		case 2:
			label = (Label)target;
			break;
		case 3:
			InputTextBox = (TextBox)target;
			InputTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 4:
			InputComboBox = (ComboBox)target;
			InputComboBox.SelectionChanged += ComboBox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
