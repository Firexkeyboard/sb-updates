using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using MahApps.Metro.IconPacks;
using OpenBullet.ViewModels;
using RuriLib;
using RuriLib.Functions.Conditions;
using RuriLib.Models;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockKeycheck : Page, IComponentConnector, IStyleConnector
{
	public BlockKeycheckViewModel vm;

	private Random rand = new Random(1);

	internal PackIconMaterial addKeychainIcon;

	internal ScrollViewer keychainsScrollViewer;

	internal ItemsControl keychainsControl;

	private bool _contentLoaded;

	public PageBlockKeycheck(BlockKeycheck block)
	{
		InitializeComponent();
		vm = new BlockKeycheckViewModel(block);
		base.DataContext = vm;
	}

	private void addKeychainImage_MouseDown(object sender, MouseButtonEventArgs e)
	{
		vm.AddKeychain();
		try
		{
			keychainsScrollViewer.ScrollToEnd();
		}
		catch
		{
		}
	}

	private void keychainTypeCombobox_Loaded(object sender, RoutedEventArgs e)
	{
		KeychainViewModel keychainById = vm.GetKeychainById((int)((ComboBox)e.OriginalSource).Tag);
		if (!keychainById.TypeInitialized)
		{
			keychainById.TypeInitialized = true;
			string[] names = Enum.GetNames(typeof(KeyChain.KeychainType));
			foreach (string newItem in names)
			{
				((ComboBox)e.OriginalSource).Items.Add(newItem);
			}
			((ComboBox)e.OriginalSource).SelectedIndex = (int)keychainById.Type;
		}
	}

	private void keychainModeCombobox_Loaded(object sender, RoutedEventArgs e)
	{
		KeychainViewModel keychainById = vm.GetKeychainById((int)((ComboBox)e.OriginalSource).Tag);
		if (!keychainById.ModeInitialized)
		{
			keychainById.ModeInitialized = true;
			string[] names = Enum.GetNames(typeof(KeyChain.KeychainMode));
			foreach (string newItem in names)
			{
				((ComboBox)e.OriginalSource).Items.Add(newItem);
			}
			((ComboBox)e.OriginalSource).SelectedIndex = (int)keychainById.Mode;
		}
	}

	private void customTypeCombobox_Loaded(object sender, RoutedEventArgs e)
	{
		KeychainViewModel keychainById = vm.GetKeychainById((int)((ComboBox)e.OriginalSource).Tag);
		if (keychainById.CustomTypeInitialized)
		{
			return;
		}
		keychainById.CustomTypeInitialized = true;
		foreach (string customKeychainName in SB.Settings.Environment.GetCustomKeychainNames())
		{
			((ComboBox)e.OriginalSource).Items.Add(customKeychainName);
		}
		if (((ComboBox)e.OriginalSource).Items.IndexOf(keychainById.CustomType) > 0)
		{
			((ComboBox)e.OriginalSource).SelectedValue = keychainById.CustomType;
		}
		else
		{
			((ComboBox)e.OriginalSource).Text = keychainById.CustomType;
		}
	}

	private void keychainTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.GetKeychainById((int)((ComboBox)e.OriginalSource).Tag).Type = (KeyChain.KeychainType)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void keychainModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.GetKeychainById((int)((ComboBox)e.OriginalSource).Tag).Mode = (KeyChain.KeychainMode)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void customTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.GetKeychainById((int)((ComboBox)e.OriginalSource).Tag).CustomType = (string)(sender as ComboBox).SelectedItem;
	}

	private void removeKeychainImage_MouseDown(object sender, MouseButtonEventArgs e)
	{
		vm.RemoveKeychainById((int)((Image)e.OriginalSource).Tag);
	}

	private void conditionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		KeyFullId keyFullId = (KeyFullId)((ComboBox)e.OriginalSource).Tag;
		vm.GetKeychainById(keyFullId.ParentId).GetKeyById(keyFullId.KeyId).Comparer = (Comparer)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void conditionCombobox_Loaded(object sender, RoutedEventArgs e)
	{
		KeyFullId keyFullId = (KeyFullId)((ComboBox)e.OriginalSource).Tag;
		if (!keyFullId.ConditionInitialized)
		{
			keyFullId.ConditionInitialized = true;
			string[] names = Enum.GetNames(typeof(Comparer));
			foreach (string newItem in names)
			{
				((ComboBox)e.OriginalSource).Items.Add(newItem);
			}
			((ComboBox)e.OriginalSource).SelectedIndex = (int)vm.GetKeychainById(keyFullId.ParentId).GetKeyById(keyFullId.KeyId).Comparer;
		}
	}

	private void leftTermCombobox_Loaded(object sender, RoutedEventArgs e)
	{
		KeyFullId keyFullId = (KeyFullId)((ComboBox)e.OriginalSource).Tag;
		if (keyFullId.LeftTermInitialized)
		{
			return;
		}
		keyFullId.LeftTermInitialized = true;
		string[] array = new string[7] { "<SOURCE>", "<HEADERS(*)>", "<HEADERS{*}>", "<COOKIES(*)>", "<COOKIES{*}>", "<RESPONSECODE>", "<ADDRESS>" };
		foreach (string newItem in array)
		{
			((ComboBox)e.OriginalSource).Items.Add(newItem);
		}
		try
		{
			((ComboBox)e.OriginalSource).SelectedValue = vm.GetKeychainById(keyFullId.ParentId).GetKeyById(keyFullId.KeyId).LeftTerm;
		}
		catch
		{
			((ComboBox)e.OriginalSource).SelectedIndex = 0;
		}
	}

	private void addKeyImage_MouseDown(object sender, MouseButtonEventArgs e)
	{
		vm.GetKeychainById((int)((Image)e.OriginalSource).Tag).AddKey();
	}

	private void removeKeyImage_MouseDown(object sender, MouseButtonEventArgs e)
	{
		KeyFullId keyFullId = (KeyFullId)((Image)e.OriginalSource).Tag;
		vm.GetKeychainById(keyFullId.ParentId).RemoveKeyById(keyFullId.KeyId);
	}

	private void customTypeCombobox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ComboBox comboBox = sender as ComboBox;
		if (comboBox.Items.Count != 0)
		{
			int id = (int)comboBox.Tag;
			vm.GetKeychainById(id).CustomType = comboBox.Text;
		}
	}

	private void addKeychainImage_MouseEnter(object sender, MouseEventArgs e)
	{
		try
		{
			((FrameworkElement)(object)addKeychainIcon).Width = 16.5;
		}
		catch
		{
		}
	}

	private void addKeychainImage_MouseLeave(object sender, MouseEventArgs e)
	{
		try
		{
			((FrameworkElement)(object)addKeychainIcon).Width = 16.0;
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblockkeycheck.xaml", UriKind.Relative);
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
			((Grid)target).MouseDown += addKeychainImage_MouseDown;
			((Grid)target).MouseEnter += addKeychainImage_MouseEnter;
			((Grid)target).MouseLeave += addKeychainImage_MouseLeave;
			break;
		case 2:
			addKeychainIcon = (PackIconMaterial)target;
			break;
		case 3:
			keychainsScrollViewer = (ScrollViewer)target;
			break;
		case 4:
			keychainsControl = (ItemsControl)target;
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
		switch (connectionId)
		{
		case 5:
			((Image)target).MouseDown += removeKeychainImage_MouseDown;
			break;
		case 6:
			((ComboBox)target).Loaded += keychainTypeCombobox_Loaded;
			((ComboBox)target).SelectionChanged += keychainTypeCombobox_SelectionChanged;
			break;
		case 7:
			((ComboBox)target).Loaded += keychainModeCombobox_Loaded;
			((ComboBox)target).SelectionChanged += keychainModeCombobox_SelectionChanged;
			break;
		case 8:
			((ComboBox)target).Loaded += customTypeCombobox_Loaded;
			((ComboBox)target).SelectionChanged += customTypeCombobox_SelectionChanged;
			((ComboBox)target).AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(customTypeCombobox_TextChanged));
			break;
		case 9:
			((Image)target).MouseDown += addKeyImage_MouseDown;
			break;
		case 10:
			((Image)target).MouseDown += removeKeyImage_MouseDown;
			break;
		case 11:
			((ComboBox)target).Loaded += leftTermCombobox_Loaded;
			break;
		case 12:
			((ComboBox)target).Loaded += conditionCombobox_Loaded;
			((ComboBox)target).SelectionChanged += conditionCombobox_SelectionChanged;
			break;
		}
	}
}
