using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib.ViewModels;

namespace OpenBullet.Views.Main.Settings.RL;

public class Ocr : Page, IComponentConnector
{
	private SettingsOcr vm;

	internal TextBox varName;

	internal TextBox varValue;

	internal ComboBox varValueType;

	internal ListBox variableLB;

	internal Button btnVariableAdd;

	internal Button btnVariableUp;

	internal Button btnVariableDown;

	internal Button btnVariableRemove;

	internal Button btnVariableClear;

	private bool _contentLoaded;

	public Ocr()
	{
		InitializeComponent();
		base.DataContext = (vm = SB.Settings.RLSettings.Ocr);
		Enum.GetNames(typeof(VariableValueType)).ToList().ForEach(delegate(string vt)
		{
			varValueType.Items.Add(vt);
		});
	}

	private void btnVariableAdd_Click(object sender, RoutedEventArgs e)
	{
		vm.VariableList.Add(new TesseractVariable
		{
			Name = varName.Text,
			Value = varValue.Text,
			ValueType = (VariableValueType)Enum.Parse(typeof(VariableValueType), varValueType.SelectedItem.ToString(), ignoreCase: true)
		});
	}

	private void btnVariableUp_Click(object sender, RoutedEventArgs e)
	{
		int selectedIndex = variableLB.SelectedIndex;
		if (selectedIndex > 0)
		{
			object obj = variableLB.Items[selectedIndex];
			vm.VariableList.RemoveAt(selectedIndex);
			vm.VariableList.Insert(selectedIndex - 1, (TesseractVariable)obj);
			variableLB.SelectedIndex = selectedIndex - 1;
		}
	}

	private void btnVariableDown_Click(object sender, RoutedEventArgs e)
	{
		int selectedIndex = variableLB.SelectedIndex;
		if (selectedIndex + 1 < variableLB.Items.Count)
		{
			object obj = variableLB.Items[selectedIndex];
			vm.VariableList.RemoveAt(selectedIndex);
			vm.VariableList.Insert(selectedIndex + 1, (TesseractVariable)obj);
			variableLB.SelectedIndex = selectedIndex + 1;
		}
	}

	private void btnVariableRemove_Click(object sender, RoutedEventArgs e)
	{
		if (variableLB.SelectedIndex != -1)
		{
			vm.VariableList.RemoveAt(variableLB.SelectedIndex);
		}
	}

	private void btnVariableClear_Click(object sender, RoutedEventArgs e)
	{
		vm.VariableList.Clear();
	}

	private void MenuItem_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			TesseractVariable val = vm.VariableList[variableLB.SelectedIndex];
			string[] obj = new string[5] { val.Name, ":", val.Value, ":", null };
			VariableValueType valueType = val.ValueType;
			obj[4] = valueType.ToString();
			Clipboard.SetText(string.Concat(obj));
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/settings/rurilib/ocr.xaml", UriKind.Relative);
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
			varName = (TextBox)target;
			break;
		case 2:
			varValue = (TextBox)target;
			break;
		case 3:
			varValueType = (ComboBox)target;
			break;
		case 4:
			variableLB = (ListBox)target;
			break;
		case 5:
			((MenuItem)target).Click += MenuItem_Click;
			break;
		case 6:
			btnVariableAdd = (Button)target;
			btnVariableAdd.Click += btnVariableAdd_Click;
			break;
		case 7:
			btnVariableUp = (Button)target;
			btnVariableUp.Click += btnVariableUp_Click;
			break;
		case 8:
			btnVariableDown = (Button)target;
			btnVariableDown.Click += btnVariableDown_Click;
			break;
		case 9:
			btnVariableRemove = (Button)target;
			btnVariableRemove.Click += btnVariableRemove_Click;
			break;
		case 10:
			btnVariableClear = (Button)target;
			btnVariableClear.Click += btnVariableClear_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
