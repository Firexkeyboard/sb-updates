using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using OpenBullet.ViewModels;
using RuriLib.Models;
using RuriLib.Runner;

namespace OpenBullet;

public class DialogCustomInput : Page, IComponentConnector
{
	internal TextBox questionTextbox;

	internal TextBox answerTextbox;

	internal Button acceptButton;

	private bool _contentLoaded;

	private object Caller { get; set; }

	private string VariableName { get; set; }

	public DialogCustomInput(object caller, string variableName, string question)
	{
		InitializeComponent();
		Caller = caller;
		VariableName = variableName;
		questionTextbox.Text = question;
		answerTextbox.Focus();
	}

	private void acceptButton_Click(object sender, RoutedEventArgs e)
	{
		if (Caller.GetType() == typeof(StackerViewModel))
		{
			((StackerViewModel)Caller).BotData.Variables.Set(new CVar(VariableName, answerTextbox.Text, false, false));
		}
		else if (Caller.GetType() == typeof(RunnerViewModel))
		{
			((RunnerViewModel)Caller).CustomInputs.Add(new KeyValuePair<string, string>(VariableName, answerTextbox.Text));
		}
		((MainDialog)base.Parent).Close();
	}

	private void answerTextbox_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Return)
		{
			acceptButton_Click(sender, null);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialogcustominput.xaml", UriKind.Relative);
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
			questionTextbox = (TextBox)target;
			break;
		case 2:
			answerTextbox = (TextBox)target;
			answerTextbox.KeyDown += answerTextbox_KeyDown;
			break;
		case 3:
			acceptButton = (Button)target;
			acceptButton.Click += acceptButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
