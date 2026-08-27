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

public class UserControlMorphology : UserControl, IComponentConnector
{
	public const string ControlName = "Morphology";

	internal ComboBox MorphMethodComboBox;

	internal ComboBox MorphShapesComboBox;

	internal TextBox SizeWidthTextBox;

	internal TextBox SizeHeightTextBox;

	internal TextBox IterationsTextBox;

	internal ComboBox BorderTypeComboBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlMorphology()
	{
		InitializeComponent();
	}

	private void UserControl_Initialized(object sender, EventArgs e)
	{
		try
		{
			Enum.GetNames(typeof(MorphTypes)).ToList().ForEach(delegate(string m)
			{
				MorphMethodComboBox.Items.Add(m);
			});
		}
		catch
		{
		}
		try
		{
			Enum.GetNames(typeof(BorderTypes)).ToList().ForEach(delegate(string c)
			{
				BorderTypeComboBox.Items.Add(c);
			});
		}
		catch
		{
		}
		try
		{
			MorphShapesComboBox.Items.Add("Null");
		}
		catch
		{
		}
		try
		{
			Enum.GetNames(typeof(MorphShapes)).ToList().ForEach(delegate(string m)
			{
				MorphShapesComboBox.Items.Add(m);
			});
		}
		catch
		{
		}
	}

	private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		this.SetFilter?.Invoke(new string[6]
		{
			MorphMethodComboBox.SelectedItem.ToString(),
			IterationsTextBox.Text,
			BorderTypeComboBox.SelectedItem.ToString(),
			MorphShapesComboBox.SelectedItem.ToString(),
			SizeWidthTextBox.Text,
			SizeHeightTextBox.Text
		}, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
		{
			Source = "Morphology"
		});
	}

	private void IterationsTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "Morphology";
		this.SetFilter?.Invoke(new string[6]
		{
			MorphMethodComboBox.SelectedItem.ToString(),
			IterationsTextBox.Text,
			BorderTypeComboBox.SelectedItem.ToString(),
			MorphShapesComboBox.SelectedItem.ToString(),
			SizeWidthTextBox.Text,
			SizeHeightTextBox.Text
		}, e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolmorphology.xaml", UriKind.Relative);
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
			((UserControlMorphology)target).Initialized += UserControl_Initialized;
			break;
		case 2:
			MorphMethodComboBox = (ComboBox)target;
			MorphMethodComboBox.SelectionChanged += ComboBox_SelectionChanged;
			break;
		case 3:
			MorphShapesComboBox = (ComboBox)target;
			MorphShapesComboBox.SelectionChanged += ComboBox_SelectionChanged;
			break;
		case 4:
			SizeWidthTextBox = (TextBox)target;
			SizeWidthTextBox.TextChanged += IterationsTextBox_TextChanged;
			break;
		case 5:
			SizeHeightTextBox = (TextBox)target;
			SizeHeightTextBox.TextChanged += IterationsTextBox_TextChanged;
			break;
		case 6:
			IterationsTextBox = (TextBox)target;
			IterationsTextBox.TextChanged += IterationsTextBox_TextChanged;
			break;
		case 7:
			BorderTypeComboBox = (ComboBox)target;
			BorderTypeComboBox.SelectionChanged += ComboBox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
