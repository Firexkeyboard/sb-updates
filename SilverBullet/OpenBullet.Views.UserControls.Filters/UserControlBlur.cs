using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlBlur : UserControl, IComponentConnector
{
	public const string ControlName = "Blur";

	internal TextBox RadiusTextBox;

	internal TextBox SigmaTextBox;

	internal ComboBox ChannelsComboBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlBlur()
	{
		InitializeComponent();
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "Blur";
		this.SetFilter?.Invoke(new string[3]
		{
			RadiusTextBox.Text,
			SigmaTextBox.Text,
			ChannelsComboBox.SelectedItem.ToString()
		}, e);
	}

	private void ChannelsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ChannelsComboBox.Visibility == Visibility.Visible)
		{
			this.SetFilter?.Invoke(new string[3]
			{
				RadiusTextBox.Text,
				SigmaTextBox.Text,
				ChannelsComboBox.SelectedItem.ToString()
			}, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
			{
				Source = "Blur"
			});
		}
	}

	private void UserControl_Initialized(object sender, EventArgs e)
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolblur.xaml", UriKind.Relative);
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
			((UserControlBlur)target).Initialized += UserControl_Initialized;
			break;
		case 2:
			RadiusTextBox = (TextBox)target;
			RadiusTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 3:
			SigmaTextBox = (TextBox)target;
			SigmaTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 4:
			ChannelsComboBox = (ComboBox)target;
			ChannelsComboBox.SelectionChanged += ChannelsComboBox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
