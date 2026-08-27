using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using RuriLib;

namespace OpenBullet;

public class LogWindow : Window, IComponentConnector, IStyleConnector
{
	internal ListView logListView;

	internal Button copyAllButton;

	internal Button clearButton;

	internal TextBox searchBar;

	internal Button searchButton;

	private bool _contentLoaded;

	public LogWindow()
	{
		InitializeComponent();
		base.DataContext = SB.Logger;
		base.Closing += LogWindowClosing;
	}

	private void LogWindowClosing(object sender, CancelEventArgs e)
	{
		SB.LogWindow = null;
	}

	private void copyClick(object sender, RoutedEventArgs e)
	{
		string text = "";
		foreach (LogEntry selectedItem in logListView.SelectedItems)
		{
			LogEntry val = selectedItem;
			text = text + $"[{val.LogTime}] ({val.LogLevel}) {val.LogComponent} - " + val.LogString + Environment.NewLine;
		}
		Clipboard.SetText(text);
	}

	private void copyAllButton_Click(object sender, RoutedEventArgs e)
	{
		string text = "";
		foreach (LogEntry item in (IEnumerable)logListView.Items)
		{
			LogEntry val = item;
			text = text + $"[{val.LogTime}] ({val.LogLevel}) {val.LogComponent} - " + val.LogString + Environment.NewLine;
		}
		Clipboard.SetText(text);
	}

	private void ListViewItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
	}

	private void clearButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			SB.Logger.EntriesCollection.Clear();
		}
		catch
		{
		}
	}

	private void searchButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.Refresh();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/logwindow.xaml", UriKind.Relative);
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
			((MenuItem)target).Click += copyClick;
			break;
		case 2:
			logListView = (ListView)target;
			break;
		case 4:
			copyAllButton = (Button)target;
			copyAllButton.Click += copyAllButton_Click;
			break;
		case 5:
			clearButton = (Button)target;
			clearButton.Click += clearButton_Click;
			break;
		case 6:
			searchBar = (TextBox)target;
			break;
		case 7:
			searchButton = (Button)target;
			searchButton.Click += searchButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 3)
		{
			EventSetter eventSetter = new EventSetter();
			eventSetter.Event = UIElement.MouseRightButtonDownEvent;
			eventSetter.Handler = new MouseButtonEventHandler(ListViewItem_MouseRightButtonDown);
			((Style)target).Setters.Add(eventSetter);
		}
	}
}
