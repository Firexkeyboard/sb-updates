using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlInputTextAndEnum : UserControl, IComponentConnector
{
	public const string ControlName = "InputTextAndEnum";

	internal Label labelInput;

	internal TextBox InputTextBox;

	internal Label labelSelect;

	internal ComboBox EnumComboBox;

	private bool _contentLoaded;

	public bool Reverse { get; set; }

	public string TEnumName { get; set; }

	public event EventHandler SetFilter;

	public UserControlInputTextAndEnum()
	{
		InitializeComponent();
	}

	public void AddEnum<TEnum>()
	{
		EnumComboBox.Items.Clear();
		Type typeFromHandle = typeof(TEnum);
		Enum.GetNames(typeFromHandle).ToList().ForEach(delegate(string e)
		{
			EnumComboBox.Items.Add(e);
		});
		TEnumName = typeFromHandle.Name;
	}

	private void UserControl_Initialized(object sender, EventArgs e)
	{
	}

	private void EnumComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (EnumComboBox.Items.Count != 0)
		{
			this.SetFilter?.Invoke(GetInputs(), new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
			{
				Source = "InputTextAndEnum"
			});
		}
	}

	private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		e.Source = "InputTextAndEnum";
		this.SetFilter?.Invoke(GetInputs(), e);
	}

	private string[] GetInputs()
	{
		if (!Reverse)
		{
			return new string[2]
			{
				InputTextBox.Text,
				EnumComboBox.SelectedItem.ToString()
			};
		}
		return new string[2]
		{
			EnumComboBox.SelectedItem.ToString(),
			InputTextBox.Text
		};
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolinputtextandenum.xaml", UriKind.Relative);
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
			((UserControlInputTextAndEnum)target).Initialized += UserControl_Initialized;
			break;
		case 2:
			labelInput = (Label)target;
			break;
		case 3:
			InputTextBox = (TextBox)target;
			InputTextBox.TextChanged += InputTextBox_TextChanged;
			break;
		case 4:
			labelSelect = (Label)target;
			break;
		case 5:
			EnumComboBox = (ComboBox)target;
			EnumComboBox.SelectionChanged += EnumComboBox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
