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
using OpenBullet.Views.UserControls;
using PluginFramework;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class BlockPluginPage : Page, IComponentConnector
{
	private bool _contentLoaded;

	public IBlockPlugin BlockPlugin { get; set; }

	public BlockBase Block
	{
		get
		{
			IBlockPlugin blockPlugin = BlockPlugin;
			return (BlockBase)(object)((blockPlugin is BlockBase) ? blockPlugin : null);
		}
	}

	public ObservableCollection<UserControl> Controls { get; set; } = new ObservableCollection<UserControl>();

	private List<PropertyInfo> ValidProperties { get; set; } = new List<PropertyInfo>();

	public event EventHandler AutoSave;

	public BlockPluginPage(IBlockPlugin blockPlugin)
	{
		InitializeComponent();
		base.DataContext = this;
		BlockPlugin = blockPlugin;
		base.LostFocus += BlockPluginPage_LostFocus;
		foreach (PropertyInfo item in from p in ((object)BlockPlugin).GetType().GetProperties()
			where Check.InputProperty(p)
			select p)
		{
			ValidProperties.Add(item);
			UserControlContainer userControlContainer = Build.InputField(BlockPlugin, item);
			userControlContainer.LostFocus += InputField_LostFocus;
			Controls.Add(userControlContainer);
		}
	}

	private void BlockPluginPage_LostFocus(object sender, RoutedEventArgs e)
	{
		this.AutoSave?.Invoke(sender, e);
	}

	private void InputField_LostFocus(object sender, RoutedEventArgs e)
	{
		this.AutoSave?.Invoke(sender, e);
	}

	public void SetPropertyValues()
	{
		foreach (PropertyInfo property in ValidProperties)
		{
			dynamic value = (from c in Controls
				where c is UserControlContainer
				select c as UserControlContainer).First((UserControlContainer c) => c.PropertyName == property.Name).GetValue();
			property.SetValue(BlockPlugin, value);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/blockpluginpage.xaml", UriKind.Relative);
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
