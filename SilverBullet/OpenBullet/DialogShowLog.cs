using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using RuriLib;

namespace OpenBullet;

public class DialogShowLog : Page, IComponentConnector
{
	public FullLogViewModel vm = new FullLogViewModel();

	internal System.Windows.Forms.RichTextBox logRTB;

	internal System.Windows.Controls.Button searchButton;

	internal System.Windows.Controls.Image previousMatchButton;

	internal System.Windows.Controls.Image nextMatchButton;

	private bool _contentLoaded;

	public DialogShowLog(List<LogEntry> log)
	{
		InitializeComponent();
		base.DataContext = vm;
		logRTB.Font = new Font("Consolas", 10f);
		logRTB.BackColor = Color.FromArgb(22, 22, 22);
		foreach (LogEntry item in log)
		{
			logRTB.AppendText(item.LogString + Environment.NewLine, item.LogColor);
		}
	}

	private void searchButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogInfo(Components.Stacker, "Seaching for " + vm.SearchString);
		logRTB.SelectAll();
		logRTB.SelectionBackColor = Color.FromArgb(22, 22, 22);
		logRTB.DeselectAll();
		if (vm.SearchString == string.Empty)
		{
			return;
		}
		int selectionStart = logRTB.SelectionStart;
		int num = 0;
		vm.Indexes.Clear();
		int num2;
		while ((num2 = logRTB.Text.IndexOf(vm.SearchString, num, StringComparison.InvariantCultureIgnoreCase)) != -1)
		{
			logRTB.Select(num2, vm.SearchString.Length);
			logRTB.SelectionColor = Color.White;
			logRTB.SelectionBackColor = Color.Navy;
			num = num2 + vm.SearchString.Length;
			vm.Indexes.Add(num);
			if (vm.Indexes.Count == 1)
			{
				logRTB.ScrollToCaret();
			}
		}
		vm.UpdateTotalSearchMatches();
		logRTB.SelectionStart = selectionStart;
		logRTB.SelectionLength = 0;
		logRTB.SelectionColor = Color.Black;
		SB.Logger.LogInfo(Components.Stacker, $"Found {vm.Indexes.Count} matches", prompt: true);
		if (vm.Indexes.Count > 0)
		{
			vm.CurrentSearchMatch = 1;
		}
	}

	private void previousMatchButton_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (vm.TotalSearchMatches != 0)
		{
			if (vm.CurrentSearchMatch == 1)
			{
				vm.CurrentSearchMatch = vm.Indexes.Count;
			}
			else
			{
				vm.CurrentSearchMatch--;
			}
			logRTB.DeselectAll();
			logRTB.Select(vm.Indexes[vm.CurrentSearchMatch - 1], 0);
			logRTB.ScrollToCaret();
		}
	}

	private void nextMatchButton_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (vm.TotalSearchMatches != 0)
		{
			if (vm.CurrentSearchMatch == vm.Indexes.Count)
			{
				vm.CurrentSearchMatch = 1;
			}
			else
			{
				vm.CurrentSearchMatch++;
			}
			logRTB.DeselectAll();
			logRTB.Select(vm.Indexes[vm.CurrentSearchMatch - 1], 0);
			logRTB.ScrollToCaret();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialogshowlog.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			logRTB = (System.Windows.Forms.RichTextBox)target;
			break;
		case 2:
			searchButton = (System.Windows.Controls.Button)target;
			searchButton.Click += searchButton_Click;
			break;
		case 3:
			previousMatchButton = (System.Windows.Controls.Image)target;
			previousMatchButton.MouseDown += previousMatchButton_MouseDown;
			break;
		case 4:
			nextMatchButton = (System.Windows.Controls.Image)target;
			nextMatchButton.MouseDown += nextMatchButton_MouseDown;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
