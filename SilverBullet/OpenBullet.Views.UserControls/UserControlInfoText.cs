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

public class UserControlInfoText : UserControl, IControl, IComponentConnector
{
	private ViewModelBase viewModel;

	internal Label valueLabel;

	private bool _contentLoaded;

	public UserControlInfoText(string defaultValue, ViewModelBase viewModel = null)
	{
		InitializeComponent();
		base.DataContext = this;
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
		return valueLabel.Content;
	}

	public void SetValue(dynamic value)
	{
		valueLabel.Content = (string)value;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/usercontrolinfotext.xaml", UriKind.Relative);
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
			valueLabel = (Label)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
