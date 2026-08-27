using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Win32;
using OpenBullet.Plugins;

namespace OpenBullet.Views.UserControls;

public class UserControlFilePicker : UserControl, IControl, IComponentConnector
{
	internal TextBox Location;

	internal Button Choose;

	private bool _contentLoaded;

	public string Filter { get; set; }

	public UserControlFilePicker(string location, string filter)
	{
		InitializeComponent();
		base.DataContext = this;
		Filter = filter;
		SetValue(location);
	}

	public dynamic GetValue()
	{
		return Location.Text;
	}

	public void SetValue(dynamic value)
	{
		Location.Text = (string)value;
	}

	private void Choose_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = Filter;
		bool? flag = openFileDialog.ShowDialog();
		if (flag.HasValue && flag.Value)
		{
			SetValue(openFileDialog.FileName);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/usercontrolfilepicker.xaml", UriKind.Relative);
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
			Location = (TextBox)target;
			break;
		case 2:
			Choose = (Button)target;
			Choose.Click += Choose_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
