using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using OpenBullet.Plugins;
using RuriLib.ViewModels;

namespace OpenBullet.Views.UserControls;

public class UserControlDropdown : UserControl, IControl, IComponentConnector
{
	private ViewModelBase viewModel;

	internal ComboBox valueDropdown;

	private bool _contentLoaded;

	public UserControlDropdown(string value, string[] options, ViewModelBase viewModel = null)
	{
		InitializeComponent();
		base.DataContext = this;
		foreach (string newItem in options)
		{
			valueDropdown.Items.Add(newItem);
		}
		SetValue(value);
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
		return valueDropdown.SelectedValue;
	}

	public void SetValue(dynamic value)
	{
		valueDropdown.SelectedValue = (string)value;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/usercontroldropdown.xaml", UriKind.Relative);
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
			valueDropdown = (ComboBox)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
