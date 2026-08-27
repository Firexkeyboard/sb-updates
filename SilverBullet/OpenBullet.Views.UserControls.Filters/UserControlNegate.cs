using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlNegate : UserControl, IComponentConnector
{
	public const string ControlName = "Negate";

	internal CheckBox CheckBoxOnlyGrayscale;

	internal ComboBox ChannelsComboBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlNegate()
	{
		InitializeComponent();
	}

	private void UserControl_Initialized(object sender, EventArgs e)
	{
	}

	private void ChannelsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ChannelsComboBox.Items.Count != 0)
		{
			this.SetFilter?.Invoke(new string[2]
			{
				(CheckBoxOnlyGrayscale.IsChecked == true).ToString(),
				ChannelsComboBox.SelectedItem.ToString()
			}, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
			{
				Source = "Negate"
			});
		}
	}

	private void CheckBoxOnlyGrayscale_Click(object sender, RoutedEventArgs e)
	{
		this.SetFilter?.Invoke(new string[2]
		{
			(CheckBoxOnlyGrayscale.IsChecked == true).ToString(),
			ChannelsComboBox.SelectedItem.ToString()
		}, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
		{
			Source = "Negate"
		});
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolnegate.xaml", UriKind.Relative);
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
			((UserControlNegate)target).Initialized += UserControl_Initialized;
			break;
		case 2:
			CheckBoxOnlyGrayscale = (CheckBox)target;
			CheckBoxOnlyGrayscale.Click += CheckBoxOnlyGrayscale_Click;
			break;
		case 3:
			ChannelsComboBox = (ComboBox)target;
			ChannelsComboBox.SelectionChanged += ChannelsComboBox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
