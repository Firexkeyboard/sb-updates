using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RuriLib;
using RuriLib.Functions.Conditions;
using RuriLib.Functions.Conversions;
using System.Windows.Media;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockUtility : Page, IComponentConnector
{
	private BlockUtility block;

	internal ComboBox groupCombobox;

	internal TabControl groupTabControl;

	internal TabItem emptyTab;

	internal TabItem listTab;

	internal ComboBox listActionCombobox;

	internal TabControl listActionTabControl;

	internal TabItem emptyTab2;

	internal TabItem joinTab;

	internal TabItem sortTab;

	internal TabItem zipTab;

	internal TabItem addTab;

	internal TabItem removeTab;

	internal TabItem removeValuesTab;

	internal ComboBox removeComparerCombobox;

	internal TabItem varTab;

	internal ComboBox varActionCombobox;

	internal TabControl varActionTabControl;

	internal TabItem emptyTab3;

	internal TabItem splitTab;

	internal TabItem conversionTab;

	internal ComboBox conversionFromCombobox;

	internal ComboBox conversionToCombobox;

	internal TabItem fileTab;

	internal ComboBox fileActionCombobox;

	internal TabItem folderTab;

	internal ComboBox folderActionCombobox;

	// Programmatic group content — reuses emptyTab (index 0) as a dynamic holder.
	private ScrollViewer convExtGroupPanel;
	internal ComboBox convExtActionCombobox;
	internal TextBox convExtInputTextBox;
	internal StackPanel convExtReadableSizePanel;
	internal CheckBox convExtOutputBitsCheckBox;
	internal CheckBox convExtBinaryUnitCheckBox;
	internal TextBox convExtDecimalPlacesTextBox;
	private StackPanel convEncodingPanel;
	internal ComboBox convExtFromCombobox;
	internal ComboBox convExtToCombobox;
	private StackPanel convByteStringPanel;
	internal ComboBox byteStringEncCombobox;

	private ScrollViewer miscGroupPanel;
	internal ComboBox miscActionCombobox;

	private ScrollViewer imagesGroupPanel;
	internal ComboBox imagesActionCombobox;
	internal TextBox imagesSvgWidthTextBox;
	internal TextBox imagesSvgHeightTextBox;

	private bool _contentLoaded;

	public PageBlockUtility(BlockUtility block)
	{
		InitializeComponent();
		this.block = block;
		base.DataContext = this.block;
		// Wire DropDownClosed so the tab switch happens AFTER the popup is physically closed.
		groupCombobox.DropDownClosed += groupCombobox_DropDownClosed;
		string[] names = Enum.GetNames(typeof(UtilityGroup));
		foreach (string newItem in names)
		{
			groupCombobox.Items.Add(newItem);
		}
		groupCombobox.SelectedIndex = (int)block.Group;
		names = Enum.GetNames(typeof(ListAction));
		foreach (string newItem2 in names)
		{
			listActionCombobox.Items.Add(newItem2);
		}
		listActionCombobox.SelectedIndex = (int)block.ListAction;
		names = Enum.GetNames(typeof(VarAction));
		foreach (string newItem3 in names)
		{
			varActionCombobox.Items.Add(newItem3);
		}
		varActionCombobox.SelectedIndex = (int)block.VarAction;
		names = Enum.GetNames(typeof(Encoding));
		foreach (string newItem4 in names)
		{
			conversionFromCombobox.Items.Add(newItem4);
		}
		conversionFromCombobox.SelectedIndex = (int)block.ConversionFrom;
		names = Enum.GetNames(typeof(Encoding));
		foreach (string newItem5 in names)
		{
			conversionToCombobox.Items.Add(newItem5);
		}
		conversionToCombobox.SelectedIndex = (int)block.ConversionTo;
		names = Enum.GetNames(typeof(FileAction));
		foreach (string newItem6 in names)
		{
			fileActionCombobox.Items.Add(newItem6);
		}
		fileActionCombobox.SelectedIndex = (int)block.FileAction;
		names = Enum.GetNames(typeof(FolderAction));
		foreach (string newItem7 in names)
		{
			folderActionCombobox.Items.Add(newItem7);
		}
		folderActionCombobox.SelectedIndex = (int)block.FolderAction;
		names = Enum.GetNames(typeof(Comparer));
		foreach (string newItem8 in names)
		{
			removeComparerCombobox.Items.Add(newItem8);
		}
		removeComparerCombobox.SelectedIndex = (int)block.ListElementComparer;
		// ── Build ConversionExt panel (NOT added to groupTabControl) ────────────
		// Reutilizamos emptyTab (índice 0) como contenedor dinámico para evitar que
		// añadir TabItems extra al TabControl corrompa el popup de Telegram.
		{
			var convExtScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
			var convExtPanel = new StackPanel { Margin = new Thickness(5) };

			convExtPanel.Children.Add(new TextBlock { Text = "Conversion Mode:", Margin = new Thickness(0, 4, 0, 2) });
			convExtActionCombobox = new ComboBox { Margin = new Thickness(0, 0, 0, 6) };
			foreach (string name in Enum.GetNames(typeof(ConversionAction)))
				convExtActionCombobox.Items.Add(name);
			convExtActionCombobox.SelectedIndex = (int)block.ConversionAct;
			convExtActionCombobox.SelectionChanged += convExtActionCombobox_SelectionChanged;
			convExtPanel.Children.Add(convExtActionCombobox);

			// From/To dropdowns — visible only for Encoding mode
			convEncodingPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
			convEncodingPanel.Children.Add(new TextBlock { Text = "From:", Margin = new Thickness(0, 4, 0, 2) });
			convExtFromCombobox = new ComboBox { Margin = new Thickness(0, 0, 0, 6) };
			foreach (string name in Enum.GetNames(typeof(Encoding)))
				convExtFromCombobox.Items.Add(name);
			convExtFromCombobox.SelectedIndex = (int)block.ConversionFrom;
			convExtFromCombobox.SelectionChanged += (s, e) => block.ConversionFrom = (Encoding)((ComboBox)e.OriginalSource).SelectedIndex;
			convEncodingPanel.Children.Add(convExtFromCombobox);
			convEncodingPanel.Children.Add(new TextBlock { Text = "To:", Margin = new Thickness(0, 4, 0, 2) });
			convExtToCombobox = new ComboBox { Margin = new Thickness(0, 0, 0, 6) };
			foreach (string name in Enum.GetNames(typeof(Encoding)))
				convExtToCombobox.Items.Add(name);
			convExtToCombobox.SelectedIndex = (int)block.ConversionTo;
			convExtToCombobox.SelectionChanged += (s, e) => block.ConversionTo = (Encoding)((ComboBox)e.OriginalSource).SelectedIndex;
			convEncodingPanel.Children.Add(convExtToCombobox);
			convExtPanel.Children.Add(convEncodingPanel);

			convExtPanel.Children.Add(new TextBlock { Text = "Input:", Margin = new Thickness(0, 4, 0, 2) });
			convExtInputTextBox = new TextBox { Text = block.InputString ?? "", TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Height = 60 };
			convExtInputTextBox.LostFocus += (s, e) => block.InputString = convExtInputTextBox.Text;
			convExtPanel.Children.Add(convExtInputTextBox);

			convExtReadableSizePanel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
			convExtOutputBitsCheckBox = new CheckBox { Content = "Output Bits", IsChecked = block.ReadableSizeOutputBits, Margin = new Thickness(0, 2, 0, 2) };
			convExtOutputBitsCheckBox.Checked   += (s, e) => block.ReadableSizeOutputBits = true;
			convExtOutputBitsCheckBox.Unchecked += (s, e) => block.ReadableSizeOutputBits = false;
			convExtReadableSizePanel.Children.Add(convExtOutputBitsCheckBox);
			convExtBinaryUnitCheckBox = new CheckBox { Content = "Binary Unit (KiB/MiB)", IsChecked = block.ReadableSizeBinaryUnit, Margin = new Thickness(0, 2, 0, 2) };
			convExtBinaryUnitCheckBox.Checked   += (s, e) => block.ReadableSizeBinaryUnit = true;
			convExtBinaryUnitCheckBox.Unchecked += (s, e) => block.ReadableSizeBinaryUnit = false;
			convExtReadableSizePanel.Children.Add(convExtBinaryUnitCheckBox);
			convExtReadableSizePanel.Children.Add(new TextBlock { Text = "Decimal Places:", Margin = new Thickness(0, 4, 0, 2) });
			convExtDecimalPlacesTextBox = new TextBox { Text = block.ReadableSizeDecimalPlaces ?? "2" };
			convExtDecimalPlacesTextBox.LostFocus += (s, e) => block.ReadableSizeDecimalPlaces = convExtDecimalPlacesTextBox.Text;
			convExtReadableSizePanel.Children.Add(convExtDecimalPlacesTextBox);
			convExtPanel.Children.Add(convExtReadableSizePanel);

			// Encoding dropdown — visible only for BytesToString / StringToBytes
			convByteStringPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
			convByteStringPanel.Children.Add(new TextBlock { Text = "Encoding:", Margin = new Thickness(0, 4, 0, 2) });
			byteStringEncCombobox = new ComboBox { Margin = new Thickness(0, 0, 0, 6) };
			foreach (string enc in new[] { "UTF8", "ASCII", "Unicode", "UTF32", "Latin1" })
				byteStringEncCombobox.Items.Add(enc);
			byteStringEncCombobox.SelectedIndex = 0;
			byteStringEncCombobox.SelectedItem = block.ByteStringEncoding ?? "UTF8";
			byteStringEncCombobox.SelectionChanged += (s, e) =>
				block.ByteStringEncoding = byteStringEncCombobox.SelectedItem?.ToString() ?? "UTF8";
			convByteStringPanel.Children.Add(byteStringEncCombobox);
			convExtPanel.Children.Add(convByteStringPanel);

			convExtScroll.Content = convExtPanel;
			convExtGroupPanel = convExtScroll;
			UpdateConvExtPanels(block.ConversionAct);
		}

		// ── Build Misc panel (NOT added to groupTabControl) ───────────────────
		{
			var miscScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
			var miscPanel = new StackPanel { Margin = new Thickness(5) };
			miscPanel.Children.Add(new TextBlock { Text = "Action:", Margin = new Thickness(0, 4, 0, 2) });
			miscActionCombobox = new ComboBox { Margin = new Thickness(0, 0, 0, 6) };
			foreach (string name in Enum.GetNames(typeof(MiscAction)))
				miscActionCombobox.Items.Add(name);
			miscActionCombobox.SelectedIndex = (int)block.MiscAction;
			miscActionCombobox.SelectionChanged += miscActionCombobox_SelectionChanged;
			miscPanel.Children.Add(miscActionCombobox);
			miscScroll.Content = miscPanel;
			miscGroupPanel = miscScroll;
		}

		// ── Build Images panel (NOT added to groupTabControl) ─────────────────
		{
			var imagesScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
			var imagesPanel = new StackPanel { Margin = new Thickness(5) };
			imagesPanel.Children.Add(new TextBlock { Text = "Action:", Margin = new Thickness(0, 4, 0, 2) });
			imagesActionCombobox = new ComboBox { Margin = new Thickness(0, 0, 0, 6) };
			foreach (string name in Enum.GetNames(typeof(ImageAction)))
				imagesActionCombobox.Items.Add(name);
			imagesActionCombobox.SelectedIndex = (int)block.ImageAct;
			imagesPanel.Children.Add(imagesActionCombobox);
			imagesPanel.Children.Add(new TextBlock { Text = "SVG Input:", Margin = new Thickness(0, 4, 0, 2) });
			var imagesSvgInputTextBox = new TextBox { Text = block.InputString ?? "", TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Height = 80 };
			imagesSvgInputTextBox.LostFocus += (s, e) => block.InputString = imagesSvgInputTextBox.Text;
			imagesPanel.Children.Add(imagesSvgInputTextBox);
			imagesPanel.Children.Add(new TextBlock { Text = "Width:", Margin = new Thickness(0, 4, 0, 2) });
			imagesSvgWidthTextBox = new TextBox { Text = block.ImageSvgWidth ?? "300" };
			imagesSvgWidthTextBox.LostFocus += (s, e) => block.ImageSvgWidth = imagesSvgWidthTextBox.Text;
			imagesPanel.Children.Add(imagesSvgWidthTextBox);
			imagesPanel.Children.Add(new TextBlock { Text = "Height:", Margin = new Thickness(0, 4, 0, 2) });
			imagesSvgHeightTextBox = new TextBox { Text = block.ImageSvgHeight ?? "150" };
			imagesSvgHeightTextBox.LostFocus += (s, e) => block.ImageSvgHeight = imagesSvgHeightTextBox.Text;
			imagesPanel.Children.Add(imagesSvgHeightTextBox);
			imagesScroll.Content = imagesPanel;
			imagesGroupPanel = imagesScroll;
		}
		// Todos los paneles están construidos — establecer el tab inicial.
		SwitchToGroupTab(block.Group);
	}

	private void SwitchToGroupTab(UtilityGroup group)
	{
		// Programmatic groups (Misc, Images, ConversionExt) reuse emptyTab (index 0)
		// as a dynamic content holder so we never modify groupTabControl.Items.
		// This keeps the BAML tab indices stable and avoids corrupting Telegram's popup.
		switch ((int)group)
		{
		case 0: groupTabControl.SelectedIndex = 1; break;
		case 1: groupTabControl.SelectedIndex = 2; break;
		case 2:
			emptyTab.Content = convExtGroupPanel;
			groupTabControl.SelectedIndex = 0;
			break;
		case 3: groupTabControl.SelectedIndex = 4; break;
		case 4: groupTabControl.SelectedIndex = 5; break;
		case 5:
			emptyTab.Content = miscGroupPanel;
			groupTabControl.SelectedIndex = 0;
			break;
		case 6:
			emptyTab.Content = imagesGroupPanel;
			groupTabControl.SelectedIndex = 0;
			break;
		default: groupTabControl.SelectedIndex = 0; break;
		}
	}

	// Fires after the dropdown popup is physically closed — safe to switch tabs here.
	private void groupCombobox_DropDownClosed(object sender, EventArgs e)
	{
		SwitchToGroupTab(block.Group);
	}

	private void groupCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count == 0) return;
		block.Group = (UtilityGroup)groupCombobox.SelectedIndex;
		// Switch the tab immediately only if the dropdown is already closed (keyboard navigation).
		// When the dropdown IS open, DropDownClosed will switch the tab after it physically closes.
		if (!groupCombobox.IsDropDownOpen)
			SwitchToGroupTab(block.Group);
	}

	private void miscActionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		block.MiscAction = (MiscAction)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void convExtActionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		block.ConversionAct = (ConversionAction)((ComboBox)e.OriginalSource).SelectedIndex;
		UpdateConvExtPanels(block.ConversionAct);
	}

	private void UpdateConvExtPanels(ConversionAction action)
	{
		if (convEncodingPanel != null)
			convEncodingPanel.Visibility = action == ConversionAction.Encoding
				? Visibility.Visible : Visibility.Collapsed;
		if (convExtReadableSizePanel != null)
			convExtReadableSizePanel.Visibility = action == ConversionAction.ReadableSize
				? Visibility.Visible : Visibility.Collapsed;
		bool needsEncoding = action == ConversionAction.BytesToString || action == ConversionAction.StringToBytes;
		if (convByteStringPanel != null)
			convByteStringPanel.Visibility = needsEncoding ? Visibility.Visible : Visibility.Collapsed;
	}

	private void listActionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		block.ListAction = (ListAction)((ComboBox)e.OriginalSource).SelectedIndex;
		ListAction listAction = block.ListAction;
		switch ((int)listAction - 2)
		{
		default:
			listActionTabControl.SelectedIndex = 0;
			break;
		case 0:
			listActionTabControl.SelectedIndex = 1;
			break;
		case 1:
			listActionTabControl.SelectedIndex = 2;
			break;
		case 2:
		case 3:
			listActionTabControl.SelectedIndex = 3;
			break;
		case 4:
			listActionTabControl.SelectedIndex = 3;
			break;
		case 5:
			listActionTabControl.SelectedIndex = 4;
			break;
		case 6:
			listActionTabControl.SelectedIndex = 5;
			break;
		case 7:
			listActionTabControl.SelectedIndex = 6;
			break;
		}
	}

	private void conversionFromCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		block.ConversionFrom = (Encoding)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void conversionToCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		block.ConversionTo = (Encoding)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void fileActionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		block.FileAction = (FileAction)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void folderActionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		block.FolderAction = (FolderAction)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void removeComparerCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		block.ListElementComparer = (Comparer)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void varActionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		block.VarAction = (VarAction)((ComboBox)e.OriginalSource).SelectedIndex;
		if ((int)block.VarAction != 0)
		{
			varActionTabControl.SelectedIndex = 0;
		}
		else
		{
			varActionTabControl.SelectedIndex = 1;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblockutility.xaml", UriKind.Relative);
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
			groupCombobox = (ComboBox)target;
			groupCombobox.SelectionChanged += groupCombobox_SelectionChanged;
			break;
		case 2:
			groupTabControl = (TabControl)target;
			break;
		case 3:
			emptyTab = (TabItem)target;
			break;
		case 4:
			listTab = (TabItem)target;
			break;
		case 5:
			listActionCombobox = (ComboBox)target;
			listActionCombobox.SelectionChanged += listActionCombobox_SelectionChanged;
			break;
		case 6:
			listActionTabControl = (TabControl)target;
			break;
		case 7:
			emptyTab2 = (TabItem)target;
			break;
		case 8:
			joinTab = (TabItem)target;
			break;
		case 9:
			sortTab = (TabItem)target;
			break;
		case 10:
			zipTab = (TabItem)target;
			break;
		case 11:
			addTab = (TabItem)target;
			break;
		case 12:
			removeTab = (TabItem)target;
			break;
		case 13:
			removeValuesTab = (TabItem)target;
			break;
		case 14:
			removeComparerCombobox = (ComboBox)target;
			removeComparerCombobox.SelectionChanged += removeComparerCombobox_SelectionChanged;
			break;
		case 15:
			varTab = (TabItem)target;
			break;
		case 16:
			varActionCombobox = (ComboBox)target;
			varActionCombobox.SelectionChanged += varActionCombobox_SelectionChanged;
			break;
		case 17:
			varActionTabControl = (TabControl)target;
			break;
		case 18:
			emptyTab3 = (TabItem)target;
			break;
		case 19:
			splitTab = (TabItem)target;
			break;
		case 20:
			conversionTab = (TabItem)target;
			break;
		case 21:
			conversionFromCombobox = (ComboBox)target;
			conversionFromCombobox.SelectionChanged += conversionFromCombobox_SelectionChanged;
			break;
		case 22:
			conversionToCombobox = (ComboBox)target;
			conversionToCombobox.SelectionChanged += conversionToCombobox_SelectionChanged;
			break;
		case 23:
			fileTab = (TabItem)target;
			break;
		case 24:
			fileActionCombobox = (ComboBox)target;
			fileActionCombobox.SelectionChanged += fileActionCombobox_SelectionChanged;
			break;
		case 25:
			folderTab = (TabItem)target;
			break;
		case 26:
			folderActionCombobox = (ComboBox)target;
			folderActionCombobox.SelectionChanged += folderActionCombobox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
