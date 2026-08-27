using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using OpenBullet.Plugins;
using RuriLib.ViewModels;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;

namespace OpenBullet.Views.UserControls;

public class UserControlNumeric : UserControl, IControl, IComponentConnector
{
	private ViewModelBase viewModel;

	internal IntegerUpDown valueNumeric;

	private bool _contentLoaded;

	public int Minimum { get; set; }

	public int Maximum { get; set; }

	public UserControlNumeric(int defaultValue, int minimum, int maximum, ViewModelBase viewModel = null)
	{
		InitializeComponent();
		base.DataContext = this;
		Minimum = minimum;
		Maximum = maximum;
		SetValue(defaultValue);
		this.viewModel = viewModel;
		if (viewModel != null)
		{
			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}
	}

	private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		SetValue(((object)viewModel).GetType().GetProperty(e.PropertyName).GetValue(viewModel));
	}

	public dynamic GetValue()
	{
		return ((UpDownBase<int?>)(object)valueNumeric).Value;
	}

	public void SetValue(dynamic value)
	{
		((UpDownBase<int?>)(object)valueNumeric).Value = (int)value;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/usercontrolnumeric.xaml", UriKind.Relative);
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
			valueNumeric = (IntegerUpDown)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
