using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using OpenBullet.Repositories;
using OpenBullet.Views.Main.Configs;
using RuriLib.Functions.Files;
using RuriLib.ViewModels;

namespace OpenBullet;

public class DialogNewConfig : Page, IComponentConnector
{
	internal TextBox nameTextbox;

	internal ComboBox categoryCombobox;

	internal TextBox authorTextbox;

	internal Button acceptButton;

	private bool _contentLoaded;

	public object Caller { get; set; }

	public DialogNewConfig(object caller)
	{
		InitializeComponent();
		Caller = caller;
		authorTextbox.Text = SB.SBSettings.General.DefaultAuthor;
		nameTextbox.Focus();
		categoryCombobox.Items.Add(ConfigRepository.defaultCategory);
		foreach (object item in (IEnumerable)(from c in SB.ConfigManager.ConfigsCollection
			select c.Category into category
			where category != ConfigRepository.defaultCategory
			select category).Distinct())
		{
			categoryCombobox.Items.Add(item);
		}
		categoryCombobox.SelectedIndex = 0;
	}

	private void acceptButton_Click(object sender, RoutedEventArgs e)
	{
		if (Caller.GetType() == typeof(ConfigManager))
		{
			if (nameTextbox.Text.Trim() == string.Empty)
			{
				MessageBox.Show("The name cannot be blank");
				return;
			}
			if (nameTextbox.Text != Files.MakeValidFileName(nameTextbox.Text, true))
			{
				MessageBox.Show("The name contains invalid characters");
				return;
			}
			if (string.IsNullOrWhiteSpace(categoryCombobox.Text))
			{
				categoryCombobox.Text = ConfigRepository.defaultCategory;
			}
			else if (categoryCombobox.Text != Files.MakeValidFileName(categoryCombobox.Text, true))
			{
				MessageBox.Show("The category contains invalid characters");
				return;
			}
			try
			{
				((ConfigManager)Caller).CreateConfig(nameTextbox.Text, categoryCombobox.Text, authorTextbox.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}
		}
		((MainDialog)base.Parent).Close();
	}

	private void textbox_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			acceptButton_Click(this, new RoutedEventArgs());
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialognewconfig.xaml", UriKind.Relative);
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
			nameTextbox = (TextBox)target;
			nameTextbox.KeyDown += textbox_KeyDown;
			break;
		case 2:
			categoryCombobox = (ComboBox)target;
			categoryCombobox.KeyDown += textbox_KeyDown;
			break;
		case 3:
			authorTextbox = (TextBox)target;
			authorTextbox.KeyDown += textbox_KeyDown;
			break;
		case 4:
			acceptButton = (Button)target;
			acceptButton.Click += acceptButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
