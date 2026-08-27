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

public class UserControlCvtColor : UserControl, IComponentConnector
{
	public const string ControlName = "CvtColor";

	internal ComboBox CodeComboBox;

	internal TextBox dstCnTextBox;

	private bool _contentLoaded;

	public event EventHandler SetFilter;

	public UserControlCvtColor()
	{
		InitializeComponent();
		Init();
	}

	private void Init()
	{
		try
		{
			Enum.GetNames(typeof(ColorConversionCodes)).ToList().ForEach(delegate(string c)
			{
				CodeComboBox.Items.Add(c);
			});
		}
		catch
		{
		}
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "CvtColor";
		this.SetFilter?.Invoke(new string[2]
		{
			CodeComboBox.SelectedItem.ToString(),
			dstCnTextBox.Text
		}, e);
	}

	private void CodeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (CodeComboBox.Items.Count != 0)
		{
			this.SetFilter?.Invoke(new string[2]
			{
				CodeComboBox.SelectedItem.ToString(),
				dstCnTextBox.Text
			}, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
			{
				Source = "CvtColor"
			});
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolcvtcolor.xaml", UriKind.Relative);
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
			CodeComboBox = (ComboBox)target;
			CodeComboBox.SelectionChanged += CodeComboBox_SelectionChanged;
			break;
		case 2:
			dstCnTextBox = (TextBox)target;
			dstCnTextBox.TextChanged += TextBox_TextChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
