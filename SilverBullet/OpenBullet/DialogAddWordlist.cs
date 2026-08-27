using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Win32;
using OpenBullet.Views.Main;
using RuriLib.Models;

namespace OpenBullet;

public class DialogAddWordlist : Page, IComponentConnector
{
	private List<SubWordlist> SubWordlists = new List<SubWordlist>();

	internal TextBox locationTextbox;

	internal TextBox locationsSubWordlistsTextbox;

	internal Image loadSubWordlistIco;

	internal Label subTypeLabel;

	internal ComboBox subTypeComboBox;

	internal Button selectSubButton;

	internal TextBox nameTextbox;

	internal ComboBox typeCombobox;

	internal TextBox purposeTextbox;

	internal Button acceptButton;

	private bool _contentLoaded;

	public object Caller { get; set; }

	public DialogAddWordlist(object caller)
	{
		InitializeComponent();
		Caller = caller;
		foreach (string wordlistTypeName in SB.Settings.Environment.GetWordlistTypeNames())
		{
			typeCombobox.Items.Add(wordlistTypeName);
		}
		SB.Settings.Environment.GetWordlistTypeNames().ForEach(delegate(string wt)
		{
			subTypeComboBox.Items.Add(wt);
		});
		typeCombobox.SelectedIndex = 0;
	}

	private void acceptButton_Click(object sender, RoutedEventArgs e)
	{
		if (Caller.GetType() == typeof(WordlistManager))
		{
			if (nameTextbox.Text.Trim() == string.Empty)
			{
				MessageBox.Show("The name cannot be blank");
				return;
			}
			if (string.IsNullOrWhiteSpace(locationTextbox.Text))
			{
				MessageBox.Show("Please select a wordlist file");
				return;
			}
			string text = locationTextbox.Text;
			string currentDirectory = Directory.GetCurrentDirectory();
			if (text.StartsWith(currentDirectory))
			{
				text = text.Substring(currentDirectory.Length + 1);
			}
			((WordlistManager)Caller).AddWordlist(new Wordlist(nameTextbox.Text, text, typeCombobox.Text, purposeTextbox.Text, true, false, SubWordlists.ToArray()));
		}
		((MainDialog)base.Parent).Close();
	}

	private void Image_MouseDown(object sender, MouseButtonEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Wordlist files | *.txt";
		openFileDialog.FilterIndex = 1;
		if (openFileDialog.ShowDialog() == false)
		{
			return;
		}
		locationTextbox.Text = openFileDialog.FileName;
		nameTextbox.Text = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
		try
		{
			string text = File.ReadLines(openFileDialog.FileName).First(l => !string.IsNullOrWhiteSpace(l));
			typeCombobox.Text = SB.Settings.Environment.RecognizeWordlistType(text);
		}
		catch
		{
		}
	}

	private void Image_MouseDown_1(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(locationTextbox.Text))
			{
				MessageBox.Show("Please select main wordlist!", "NOTICE", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Sub Wordlist files | *.txt";
			openFileDialog.FilterIndex = 1;
			if (openFileDialog.ShowDialog() == false)
			{
				return;
			}
			locationsSubWordlistsTextbox.Text = openFileDialog.FileName;
			try
			{
				string text = File.ReadLines(openFileDialog.FileName).First(l => !string.IsNullOrWhiteSpace(l));
				subTypeComboBox.Text = SB.Settings.Environment.RecognizeWordlistType(text);
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private void selectSubButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(locationTextbox.Text))
			{
				MessageBox.Show("Please select main wordlist!", "NOTICE", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}
			if (string.IsNullOrWhiteSpace(locationsSubWordlistsTextbox.Text))
			{
				MessageBox.Show("Please select sub wordlist!", "NOTICE", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}
			string text = locationsSubWordlistsTextbox.Text;
			string currentDirectory = Directory.GetCurrentDirectory();
			if (text.StartsWith(currentDirectory))
			{
				text = text.Substring(currentDirectory.Length + 1);
			}
			SubWordlist val = new SubWordlist(nameTextbox.Text, text, subTypeComboBox.Text, purposeTextbox.Text, true, false);
			SubWordlists.Add(val);
			MessageBox.Show($"Added!\nTotal: {val.Total}\nSubWordlist count: {SubWordlists.Count}");
		}
		catch
		{
		}
	}

	private void CheckBox_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Button button = selectSubButton;
			Label label = subTypeLabel;
			ComboBox comboBox = subTypeComboBox;
			TextBox textBox = locationsSubWordlistsTextbox;
			bool flag = (loadSubWordlistIco.IsEnabled = (sender as CheckBox).IsChecked == true);
			bool flag3 = (textBox.IsEnabled = flag);
			bool flag5 = (comboBox.IsEnabled = flag3);
			bool isEnabled = (label.IsEnabled = flag5);
			button.IsEnabled = isEnabled;
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialogaddwordlist.xaml", UriKind.Relative);
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
			locationTextbox = (TextBox)target;
			break;
		case 2:
			((Image)target).MouseDown += Image_MouseDown;
			break;
		case 3:
			((CheckBox)target).Click += CheckBox_Click;
			break;
		case 4:
			locationsSubWordlistsTextbox = (TextBox)target;
			break;
		case 5:
			loadSubWordlistIco = (Image)target;
			loadSubWordlistIco.MouseDown += Image_MouseDown_1;
			break;
		case 6:
			subTypeLabel = (Label)target;
			break;
		case 7:
			subTypeComboBox = (ComboBox)target;
			break;
		case 8:
			selectSubButton = (Button)target;
			selectSubButton.Click += selectSubButton_Click;
			break;
		case 9:
			nameTextbox = (TextBox)target;
			break;
		case 10:
			typeCombobox = (ComboBox)target;
			break;
		case 11:
			purposeTextbox = (TextBox)target;
			break;
		case 12:
			acceptButton = (Button)target;
			acceptButton.Click += acceptButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
