using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using OpenCvSharp;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlAdaptiveThreshold : UserControl, IComponentConnector
{
	public const string ControlName = "AdaptiveThreshold";

	internal TextBox MaxValueTextBox;

	internal ComboBox AdaptiveMethodComboBox;

	internal ComboBox ThresholdTypeComboBox;

	internal TextBox BlockSizeTextBox;

	internal TextBox ConstantTextBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlAdaptiveThreshold()
	{
		InitializeComponent();
	}

	private void UserControl_Initialized(object sender, EventArgs e)
	{
		try
		{
			Enum.GetNames(typeof(AdaptiveThresholdTypes)).ToList().ForEach(delegate(string a)
			{
				AdaptiveMethodComboBox.Items.Add(a);
			});
		}
		catch
		{
		}
		try
		{
			Enum.GetNames(typeof(ThresholdTypes)).ToList().ForEach(delegate(string t)
			{
				ThresholdTypeComboBox.Items.Add(t);
			});
		}
		catch
		{
		}
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "AdaptiveThreshold";
		this.SetFilter?.Invoke(new string[5]
		{
			MaxValueTextBox.Text,
			AdaptiveMethodComboBox.SelectedItem.ToString(),
			ThresholdTypeComboBox.SelectedItem.ToString(),
			BlockSizeTextBox.Text,
			ConstantTextBox.Text
		}, e);
	}

	private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		this.SetFilter?.Invoke(new string[5]
		{
			MaxValueTextBox.Text,
			AdaptiveMethodComboBox.SelectedItem.ToString(),
			ThresholdTypeComboBox.SelectedItem.ToString(),
			BlockSizeTextBox.Text,
			ConstantTextBox.Text
		}, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
		{
			Source = "AdaptiveThreshold"
		});
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontroladaptivethreshold.xaml", UriKind.Relative);
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
			((UserControlAdaptiveThreshold)target).Initialized += UserControl_Initialized;
			break;
		case 2:
			MaxValueTextBox = (TextBox)target;
			MaxValueTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 3:
			AdaptiveMethodComboBox = (ComboBox)target;
			AdaptiveMethodComboBox.SelectionChanged += ComboBox_SelectionChanged;
			break;
		case 4:
			ThresholdTypeComboBox = (ComboBox)target;
			ThresholdTypeComboBox.SelectionChanged += ComboBox_SelectionChanged;
			break;
		case 5:
			BlockSizeTextBox = (TextBox)target;
			BlockSizeTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 6:
			ConstantTextBox = (TextBox)target;
			ConstantTextBox.TextChanged += TextBox_TextChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
