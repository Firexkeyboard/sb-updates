using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using OpenBullet.Plugins;
using PluginFramework;
using RuriLib.Interfaces;
using RuriLib.ViewModels;

namespace OpenBullet.Views.UserControls;

public class PluginControl : UserControl, IComponentConnector
{
	private bool _contentLoaded;

	public IPlugin Plugin { get; set; }

	public ObservableCollection<UserControl> Controls { get; set; } = new ObservableCollection<UserControl>();

	private List<PropertyInfo> ValidProperties { get; set; } = new List<PropertyInfo>();

	public PluginControl(Type type, IApplication app, bool supportsPropertyChanged = false)
	{
		InitializeComponent();
		base.DataContext = this;
		object[] args = new object[0];
		if (type.GetConstructors().Any((ConstructorInfo c) => c.GetParameters().Any((ParameterInfo p) => p.ParameterType == typeof(IApplication))))
		{
			args = new object[1] { app };
		}
		object obj = Activator.CreateInstance(type, args);
		Plugin = (IPlugin)((obj is IPlugin) ? obj : null);
		foreach (PropertyInfo item in from p in type.GetProperties()
			where Check.InputProperty(p)
			select p)
		{
			ValidProperties.Add(item);
			Controls.Add(Build.InputField(Plugin, item, supportsPropertyChanged ? Plugin as ViewModelBase : null));
		}
		foreach (MethodInfo item2 in from m in type.GetMethods()
			where Check.Method(Plugin, m)
			select m)
		{
			Controls.Add(Build.Button(item2, this));
		}
	}

	public void RunMethod(string methodName)
	{
		foreach (PropertyInfo property in ValidProperties)
		{
			property.SetValue(Plugin, (from c in Controls
				where c is UserControlContainer
				select c as UserControlContainer).First((UserControlContainer c) => c.PropertyName == property.Name).GetValue());
		}
		MethodInfo method = ((object)Plugin).GetType().GetMethod(methodName);
		ParameterInfo[] parameters = method.GetParameters();
		object[] parameters2 = new object[0];
		if (parameters.Length == 1 && parameters.First().ParameterType == typeof(IApplication))
		{
			parameters2 = new object[1] { SB.App };
		}
		method.Invoke(Plugin, parameters2);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/plugincontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		_contentLoaded = true;
	}
}
