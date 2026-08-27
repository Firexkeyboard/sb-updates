using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ImageProcessor.Imaging;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlCropLayer : UserControl, IComponentConnector
{
	public const string ControlName = "CropLayer";

	internal TextBox LeftTextBox;

	internal TextBox TopTextBox;

	internal TextBox RightTextBox;

	internal TextBox BottomTextBox;

	internal ComboBox CropModeBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlCropLayer()
	{
		InitializeComponent();
	}

	private void TextBoxCL_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "CropLayer";
		this.SetFilter?.Invoke(new string[5]
		{
			LeftTextBox.Text,
			TopTextBox.Text,
			RightTextBox.Text,
			BottomTextBox.Text,
			CropModeBox.SelectedItem.ToString()
		}, e);
	}

	private void CropModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		this.SetFilter?.Invoke(new string[5]
		{
			LeftTextBox.Text,
			TopTextBox.Text,
			RightTextBox.Text,
			BottomTextBox.Text,
			CropModeBox.SelectedItem.ToString()
		}, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
		{
			Source = "CropLayer"
		});
	}

	private void UserControl_Initialized(object sender, EventArgs e)
	{
		try
		{
			Enum.GetNames(typeof(CropMode)).ToList().ForEach(delegate(string c)
			{
				CropModeBox.Items.Add(c);
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolcroplayer.xaml", UriKind.Relative);
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
			((UserControlCropLayer)target).Initialized += UserControl_Initialized;
			break;
		case 2:
			LeftTextBox = (TextBox)target;
			LeftTextBox.TextChanged += TextBoxCL_TextChanged;
			break;
		case 3:
			TopTextBox = (TextBox)target;
			TopTextBox.TextChanged += TextBoxCL_TextChanged;
			break;
		case 4:
			RightTextBox = (TextBox)target;
			RightTextBox.TextChanged += TextBoxCL_TextChanged;
			break;
		case 5:
			BottomTextBox = (TextBox)target;
			BottomTextBox.TextChanged += TextBoxCL_TextChanged;
			break;
		case 6:
			CropModeBox = (ComboBox)target;
			CropModeBox.SelectionChanged += CropModeBox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
