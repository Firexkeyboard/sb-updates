using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace OpenBullet;

public class DialogLSDoc : Page, IComponentConnector
{
	private XmlNode main;

	private XmlNode currentSection;

	private XmlNode currentItem;

	private XmlNodeList sections;

	private XmlNodeList items;

	internal ComboBox sectionComboBox;

	internal StackPanel menuPanel;

	internal Label titleLabel;

	internal TextEditor contentDisplay;

	private bool _contentLoaded;

	public DialogLSDoc()
	{
		InitializeComponent();
		((Control)(object)contentDisplay.TextArea).Foreground = new SolidColorBrush(Colors.Gainsboro);
		using (XmlReader xmlReader = XmlReader.Create("LSHighlighting.xshd"))
		{
			contentDisplay.SyntaxHighlighting = HighlightingLoader.Load(xmlReader, (IHighlightingDefinitionReferenceResolver)(object)HighlightingManager.Instance);
		}
		XmlDocument xmlDocument = new XmlDocument();
		try
		{
			xmlDocument.Load("LSDoc.xml");
		}
		catch
		{
			MessageBox.Show("No documentation file found!");
			return;
		}
		main = xmlDocument.DocumentElement.SelectSingleNode("/doc");
		sectionComboBox.Items.Clear();
		sections = main.ChildNodes;
		foreach (XmlNode section in sections)
		{
			sectionComboBox.Items.Add(section.Attributes["name"].Value);
		}
		sectionComboBox.SelectedIndex = 0;
		currentSection = sections[0];
		SwitchPage();
	}

	private void sectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			currentSection = sections.Item(((ComboBox)e.OriginalSource).SelectedIndex);
			SwitchPage();
		}
		catch
		{
		}
	}

	private void SwitchPage()
	{
		items = currentSection.ChildNodes;
		menuPanel.Children.Clear();
		foreach (XmlNode item in items)
		{
			Label label = new Label();
			label.Content = item.Attributes["name"].Value;
			label.FontWeight = FontWeights.Bold;
			label.MouseDown += menuItem_Clicked;
			menuPanel.Children.Add(label);
		}
	}

	private void menuItem_Clicked(object sender, MouseButtonEventArgs e)
	{
		try
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (items[i].Attributes["name"].Value == ((TextBlock)e.OriginalSource).Text)
				{
					currentItem = items[i];
					break;
				}
			}
			DisplayContent();
		}
		catch
		{
		}
	}

	private void DisplayContent()
	{
		titleLabel.Content = currentItem.Attributes["name"];
		contentDisplay.Text = currentItem.InnerText;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/dialogs/dialoglsdoc.xaml", UriKind.Relative);
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
			sectionComboBox = (ComboBox)target;
			sectionComboBox.SelectionChanged += sectionComboBox_SelectionChanged;
			break;
		case 2:
			menuPanel = (StackPanel)target;
			break;
		case 3:
			titleLabel = (Label)target;
			break;
		case 4:
			contentDisplay = (TextEditor)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
