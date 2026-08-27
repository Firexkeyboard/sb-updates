using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockRecaptcha : Page, IComponentConnector
{
	private BlockRecaptcha vm;

	internal Button autoSiteKey;

	private bool _contentLoaded;

	public PageBlockRecaptcha(BlockRecaptcha block)
	{
		InitializeComponent();
		vm = block;
		base.DataContext = vm;
	}

	private void autoSiteKey_Click(object sender, RoutedEventArgs e)
	{
		if (vm.Url == string.Empty)
		{
			MessageBox.Show("You cannot use auto without setting a page where the reCaptcha is shown first!");
			return;
		}
		try
		{
			HttpWebRequest obj = (HttpWebRequest)WebRequest.Create(vm.Url);
			obj.AutomaticDecompression = DecompressionMethods.GZip;
			using HttpWebResponse httpWebResponse = (HttpWebResponse)obj.GetResponse();
			using Stream stream = httpWebResponse.GetResponseStream();
			using StreamReader streamReader = new StreamReader(stream);
			string input = streamReader.ReadToEnd();
			Regex regex = new Regex("[^A-Za-z\\d][A-Za-z\\d\\-]{40}[^A-Za-z\\d]");
			vm.SiteKey = regex.Match(input).Value.Replace("\"", "");
		}
		catch
		{
			MessageBox.Show("Auto failed. Do it manually");
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblockrecaptcha.xaml", UriKind.Relative);
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
			autoSiteKey = (Button)target;
			autoSiteKey.Click += autoSiteKey_Click;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
