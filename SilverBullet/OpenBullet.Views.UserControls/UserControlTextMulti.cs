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

public class UserControlTextMulti : UserControl, IControl, IComponentConnector
{
	private ViewModelBase viewModel;

	internal TextBox valueTextbox;

	private bool _contentLoaded;

	public UserControlTextMulti(string[] defaultValue, bool readOnly = false, ViewModelBase viewModel = null)
	{
		InitializeComponent();
		base.DataContext = this;
		valueTextbox.IsReadOnly = readOnly;
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
		return valueTextbox.Text.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.None);
	}

	public void SetValue(dynamic value)
	{
		string[] value2 = (string[])value;
		valueTextbox.Text = string.Join(Environment.NewLine, value2);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/usercontroltextmulti.xaml", UriKind.Relative);
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
			valueTextbox = (TextBox)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
