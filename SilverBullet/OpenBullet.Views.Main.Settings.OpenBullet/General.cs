using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace OpenBullet.Views.Main.Settings.OpenBullet;

public class General : Page, IComponentConnector
{
	private bool _contentLoaded;

	public General()
	{
		InitializeComponent();
		base.DataContext = SB.SBSettings.General;
		this.Loaded += General_Loaded;
	}

	private void General_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			// Find the CheckBox that is bound to AutoSaveConfigOnStacker in the visual tree.
			// Insert a new "Periodic auto-save" checkbox right after it.
			var existingCb = FindCheckBoxByBindingPath(this, "AutoSaveConfigOnStacker");
			if (existingCb?.Parent is Panel panel)
			{
				// Avoid adding duplicate if Loaded fires more than once
				foreach (UIElement child in panel.Children)
					if (child is CheckBox cb2 && cb2.Tag is string t && t == "PeriodicAutoSave") return;

				int idx = panel.Children.IndexOf(existingCb);

				var newCb = new CheckBox
				{
					Tag     = "PeriodicAutoSave",
					Content = "Periodic auto-save (every 30s) — auto-saves the open config periodically",
					Margin  = existingCb.Margin,
					Padding = existingCb.Padding,
					Style   = existingCb.Style,
				};
				// Copy foreground/font from the existing checkbox so it matches the theme
				if (existingCb.Foreground != null)
					newCb.Foreground = existingCb.Foreground;

				var binding = new Binding("PeriodicAutoSaveEnabled")
				{
					Mode   = BindingMode.TwoWay,
					Source = SB.SBSettings.General,
				};
				newCb.SetBinding(CheckBox.IsCheckedProperty, binding);

				// Wire checkbox → timer: start/stop the Stacker's periodic save timer when toggled
				newCb.Checked   += (s2, _) => ApplyPeriodicSaveTimerState(true);
				newCb.Unchecked += (s2, _) => ApplyPeriodicSaveTimerState(false);

				panel.Children.Insert(idx + 1, newCb);
			}
		}
		catch { }
	}

	private static void ApplyPeriodicSaveTimerState(bool enabled)
	{
		try
		{
			var stacker = SB.MainWindow?.ConfigsPage?.StackerPage;
			if (stacker == null) return;
			if (enabled)
				stacker.StartPeriodicSaveTimer();
			else
				stacker.StopPeriodicSaveTimer();
		}
		catch { }
	}

	// Walk the visual tree looking for the first CheckBox whose IsChecked binding path matches.
	private static CheckBox FindCheckBoxByBindingPath(DependencyObject root, string bindingPath)
	{
		if (root == null) return null;
		if (root is CheckBox cb)
		{
			var expr = cb.GetBindingExpression(CheckBox.IsCheckedProperty);
			if (expr?.ParentBinding?.Path?.Path == bindingPath) return cb;
		}
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var result = FindCheckBoxByBindingPath(VisualTreeHelper.GetChild(root, i), bindingPath);
			if (result != null) return result;
		}
		return null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/settings/openbullet/general.xaml", UriKind.Relative);
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
