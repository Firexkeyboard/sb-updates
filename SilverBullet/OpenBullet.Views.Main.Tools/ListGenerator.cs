using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Win32;
using OpenBullet.ViewModels;
using RuriLib.Models;

namespace OpenBullet.Views.Main.Tools;

public class ListGenerator : Page, IComponentConnector
{
	private ListGeneratorViewModel vm = new ListGeneratorViewModel();

	private StreamWriter sw;

	private Random rand = new Random();

	internal Button clearButton;

	internal Button digitsButton;

	internal Button lowercaseButton;

	internal Button uppercaseButton;

	internal Button generateButton;

	private bool _contentLoaded;

	public ListGenerator()
	{
		InitializeComponent();
		base.DataContext = vm;
	}

	private void lowercaseButton_Click(object sender, RoutedEventArgs e)
	{
		vm.AllowedCharacters += "abcdefghijklmnopqrstuvwxyz";
	}

	private void uppercaseButton_Click(object sender, RoutedEventArgs e)
	{
		vm.AllowedCharacters += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	}

	private void digitsButton_Click(object sender, RoutedEventArgs e)
	{
		vm.AllowedCharacters += "0123456789";
	}

	private void clearButton_Click(object sender, RoutedEventArgs e)
	{
		vm.AllowedCharacters = "";
	}

	private void generateButton_Click(object sender, RoutedEventArgs e)
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "Text File |*.txt";
		saveFileDialog.Title = "Save Output List";
		saveFileDialog.ShowDialog();
		if (saveFileDialog.FileName != string.Empty)
		{
			sw = new StreamWriter(saveFileDialog.FileName);
			WriteCombinations(vm.Mask);
			sw.Close();
			sw.Dispose();
			if (vm.AutoImport)
			{
				Wordlist wordlist = new Wordlist("Generated" + rand.Next(), saveFileDialog.FileName, "Default", "", true, false, (SubWordlist[])null);
				SB.MainWindow.WordlistManagerPage.AddWordlist(wordlist);
			}
		}
	}

	private void WriteCombinations(string input)
	{
		if (input.Contains('*'))
		{
			string allowedCharacters = vm.AllowedCharacters;
			foreach (char c in allowedCharacters)
			{
				WriteCombinations(new Regex("\\*").Replace(input, c.ToString(), 1));
			}
		}
		else if ((vm.OnlyLuhn && Luhn(input.Split(':')[0])) || !vm.OnlyLuhn)
		{
			sw.WriteLine(input);
		}
	}

	private List<string> Generate(List<string> list)
	{
		if (list.Any((string s) => s.Contains('*')))
		{
			List<string> list2 = new List<string>();
			foreach (string item in list)
			{
				string allowedCharacters = vm.AllowedCharacters;
				foreach (char c in allowedCharacters)
				{
					list2.Add(new Regex("\\*").Replace(item, c.ToString(), 1));
				}
			}
			return Generate(list2);
		}
		return list;
	}

	public static bool Luhn(string digits)
	{
		if (digits.All(char.IsDigit))
		{
			return (from c in digits.Reverse()
				select c - 48).Select((int thisNum, int i) => (i % 2 != 0) ? (((thisNum *= 2) <= 9) ? thisNum : (thisNum - 9)) : thisNum).Sum() % 10 == 0;
		}
		return false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/tools/listgenerator.xaml", UriKind.Relative);
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
			clearButton = (Button)target;
			clearButton.Click += clearButton_Click;
			break;
		case 2:
			digitsButton = (Button)target;
			digitsButton.Click += digitsButton_Click;
			break;
		case 3:
			lowercaseButton = (Button)target;
			lowercaseButton.Click += lowercaseButton_Click;
			break;
		case 4:
			uppercaseButton = (Button)target;
			uppercaseButton.Click += uppercaseButton_Click;
			break;
		case 5:
			generateButton = (Button)target;
			generateButton.Click += generateButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
