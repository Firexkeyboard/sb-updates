using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using OpenBullet.Plugins;
using RuriLib.Models;

namespace OpenBullet.Views.UserControls;

public class UserControlWordlist : UserControl, IControl, IComponentConnector
{
	internal TextBox WordlistName;

	internal Button Choose;

	private bool _contentLoaded;

	public Wordlist Wordlist { get; set; }

	public UserControlWordlist()
	{
		InitializeComponent();
		base.DataContext = this;
	}

	public dynamic GetValue()
	{
		return Wordlist;
	}

	public void SetValue(dynamic value)
	{
		Wordlist = (Wordlist)value;
	}

	private void Choose_Click(object sender, RoutedEventArgs e)
	{
		new MainDialog(new DialogSelectWordlist(this), "Select a Wordlist").ShowDialog();
		if (Wordlist != null)
		{
			WordlistName.Text = Wordlist.Name;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/usercontrols/usercontrolwordlist.xaml", UriKind.Relative);
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
			WordlistName = (TextBox)target;
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
