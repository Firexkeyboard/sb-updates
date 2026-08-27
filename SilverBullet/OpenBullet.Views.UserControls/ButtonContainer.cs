using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace OpenBullet.Views.UserControls;

public class ButtonContainer : UserControl, IComponentConnector
{
	internal Button SubmitButton;

	private bool _contentLoaded;

	public string Text { get; set; }

	private string MethodName { get; set; }

	private PluginControl PluginControl { get; set; }

	public ButtonContainer(string text, string methodName, PluginControl pluginControl)
	{
		InitializeComponent();
		base.DataContext = this;
		Text = text;
		MethodName = methodName;
		PluginControl = pluginControl;
	}

	private void SubmitButton_Click(object sender, RoutedEventArgs e)
	{
		PluginControl.RunMethod(MethodName);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/buttoncontainer.xaml", UriKind.Relative);
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
			SubmitButton = (Button)target;
			SubmitButton.Click += SubmitButton_Click;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
