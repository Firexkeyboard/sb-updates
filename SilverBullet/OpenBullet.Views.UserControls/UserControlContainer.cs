using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using OpenBullet.Plugins;

namespace OpenBullet.Views.UserControls;

public class UserControlContainer : UserControl, IControl, IComponentConnector
{
	internal Grid Grid;

	private bool _contentLoaded;

	public string PropertyName { get; set; }

	public IControl UserControl { get; set; }

	public string Label { get; set; }

	public string Tooltip { get; set; }

	public UserControlContainer(string propertyName, IControl userControl, string label, string tooltip)
	{
		InitializeComponent();
		base.DataContext = this;
		PropertyName = propertyName;
		UserControl = userControl;
		(UserControl as UserControl).SetValue(Grid.ColumnProperty, 1);
		Grid.Children.Add(UserControl as UserControl);
		Label = label;
		Tooltip = tooltip;
	}

	public dynamic GetValue()
	{
		return UserControl.GetValue();
	}

	public void SetValue(dynamic value)
	{
		UserControl.SetValue(value);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/usercontrolcontainer.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			Grid = (Grid)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
