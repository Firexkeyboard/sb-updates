using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using ImageProcessor.Imaging.MetaData;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlResolution : UserControl, IComponentConnector
{
	public const string ControlName = "Resolution";

	internal IntegerUpDown HorizontalNumeric;

	internal IntegerUpDown VerticalNumeric;

	internal ComboBox UnitComboBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlResolution()
	{
		InitializeComponent();
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "Resolution";
		this.SetFilter?.Invoke(new string[3]
		{
			(((UpDownBase<int?>)(object)HorizontalNumeric).Value ?? 0).ToString(),
			(((UpDownBase<int?>)(object)VerticalNumeric).Value ?? 0).ToString(),
			UnitComboBox.SelectedItem.ToString()
		}, e);
	}

	private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		this.SetFilter?.Invoke(new string[3]
		{
			(((UpDownBase<int?>)(object)HorizontalNumeric).Value ?? 0).ToString(),
			(((UpDownBase<int?>)(object)VerticalNumeric).Value ?? 0).ToString(),
			UnitComboBox.SelectedItem.ToString()
		}, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
		{
			Source = "Resolution"
		});
	}

	private void UserControl_Initialized(object sender, EventArgs e)
	{
		try
		{
			Enum.GetNames(typeof(PropertyTagResolutionUnit)).ToList().ForEach(delegate(string p)
			{
				UnitComboBox.Items.Add(p);
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolresolution.xaml", UriKind.Relative);
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
			((UserControlResolution)target).Initialized += UserControl_Initialized;
			break;
		case 2:
			HorizontalNumeric = (IntegerUpDown)target;
			((UIElement)(object)HorizontalNumeric).AddHandler(TextBoxBase.TextChangedEvent, (Delegate)new TextChangedEventHandler(TextBox_TextChanged));
			break;
		case 3:
			VerticalNumeric = (IntegerUpDown)target;
			((UIElement)(object)VerticalNumeric).AddHandler(TextBoxBase.TextChangedEvent, (Delegate)new TextChangedEventHandler(TextBox_TextChanged));
			break;
		case 4:
			UnitComboBox = (ComboBox)target;
			UnitComboBox.SelectionChanged += ComboBox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
