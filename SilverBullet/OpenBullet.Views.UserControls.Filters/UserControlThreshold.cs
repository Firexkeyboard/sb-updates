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

public class UserControlThreshold : UserControl, IComponentConnector
{
	public const string ControlName = "Threshold";

	internal TextBox ThreshTextBox;

	internal TextBox MaxValueTextBox;

	internal ComboBox ThresholdTypeComboBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlThreshold()
	{
		InitializeComponent();
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "Threshold";
		this.SetFilter?.Invoke(new string[3]
		{
			ThreshTextBox.Text,
			MaxValueTextBox.Text,
			ThresholdTypeComboBox.SelectedItem.ToString()
		}, e);
	}

	private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		this.SetFilter?.Invoke(new string[3]
		{
			ThreshTextBox.Text,
			MaxValueTextBox.Text,
			ThresholdTypeComboBox.SelectedItem.ToString()
		}, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
		{
			Source = "Threshold"
		});
	}

	private void UserControl_Initialized(object sender, EventArgs e)
	{
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

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolthreshold.xaml", UriKind.Relative);
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
			((UserControlThreshold)target).Initialized += UserControl_Initialized;
			break;
		case 2:
			ThreshTextBox = (TextBox)target;
			ThreshTextBox.TextChanged += TextBox_TextChanged;
			break;
		case 3:
			MaxValueTextBox = (TextBox)target;
			break;
		case 4:
			ThresholdTypeComboBox = (ComboBox)target;
			ThresholdTypeComboBox.SelectionChanged += ComboBox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
