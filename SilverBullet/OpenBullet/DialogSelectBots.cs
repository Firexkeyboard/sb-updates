using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using RuriLib.Runner;

namespace OpenBullet;

public class DialogSelectBots : Page, IComponentConnector
{
	private const int Maximum = 400;

	private const int Minimum = 1;

	internal TextBox botsNumberTextbox;

	internal Button selectButton;

	private bool _contentLoaded;

	public object Caller { get; set; }

	public DialogSelectBots(object caller, int initial = 1)
	{
		InitializeComponent();
		Caller = caller;
		botsNumberTextbox.Text = initial.ToString();
	}

	private void selectButton_Click(object sender, RoutedEventArgs e)
	{
		int result = 1;
		int.TryParse(botsNumberTextbox.Text, out result);
		if (Caller.GetType() == typeof(RunnerViewModel))
		{
			object caller = Caller;
			((RunnerViewModel)((caller is RunnerViewModel) ? caller : null)).BotsAmount = result;
		}
		((MainDialog)base.Parent).Close();
	}

	private void botsNumberTextbox_KeyDown(object sender, KeyEventArgs e)
	{
		try
		{
			if (e.Key == Key.Return)
			{
				selectButton_Click(null, null);
			}
		}
		catch
		{
		}
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			botsNumberTextbox.CaretIndex = botsNumberTextbox.Text.Length;
			botsNumberTextbox.Focus();
		}
		catch
		{
		}
	}

	private void botsNumberTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		try
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
			if (!e.Handled)
			{
				TextBox textBox = (TextBox)sender;
				string text = textBox.Text;
				if (textBox.SelectedText != string.Empty)
				{
					text = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectedText.Length);
				}
				int num = int.Parse(text + e.Text);
				e.Handled = num > 400 || num <= 0;
			}
		}
		catch
		{
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialogselectbots.xaml", UriKind.Relative);
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
			((DialogSelectBots)target).Loaded += Page_Loaded;
			break;
		case 2:
			botsNumberTextbox = (TextBox)target;
			botsNumberTextbox.KeyDown += botsNumberTextbox_KeyDown;
			botsNumberTextbox.PreviewTextInput += botsNumberTextbox_PreviewTextInput;
			break;
		case 3:
			selectButton = (Button)target;
			selectButton.Click += selectButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
