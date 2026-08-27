using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.UserControls.Filters;

public class UserControlEnumBox : UserControl, IComponentConnector
{
	public const string ControlName = "Enum";

	internal Label label;

	internal ComboBox EnumComboBox;

	private bool _contentLoaded;

	public string TEnumName { get; set; }

	public event EventHandler SetFilter;

	public UserControlEnumBox()
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
			this.SetFilter?.Invoke(new string[1] { EnumComboBox.SelectedItem.ToString() }, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None)
			{
				Source = "Enum"
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/filters/usercontrolenumbox.xaml", UriKind.Relative);
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
			((UserControlEnumBox)target).Initialized += UserControl_Initialized;
			break;
		case 2:
			label = (Label)target;
			break;
		case 3:
			EnumComboBox = (ComboBox)target;
			EnumComboBox.SelectionChanged += EnumComboBox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
