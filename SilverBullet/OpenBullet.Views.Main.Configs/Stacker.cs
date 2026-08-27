using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using System.Xml;
using EO.WebBrowser;
using EO.WebEngine;
using EO.Wpf;
using RuriLib.Models;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using MahApps.Metro.IconPacks;
using OpenBullet.Editor.CustomSearch;
using OpenBullet.ViewModels;
using OpenBullet.Views.CustomMessageBox;
using OpenBullet.Views.Dialogs;
using PluginFramework;
using RuriLib;
using RuriLib.LS;
using RuriLib.LS.LoliCode;
using RuriLib.Models;
using RuriLib.Runner;
using RuriLib.ViewModels;
using SilverBullet.Tesseract;

namespace OpenBullet.Views.Main.Configs;

public class Stacker : Page, IComponentConnector, IStyleConnector
{
	public delegate void SaveConfigEventHandler(object sender, EventArgs e);

	private Stopwatch timer;

	private StackerViewModel vm;

	private AbortableBackgroundWorker debugger = new AbortableBackgroundWorker();

	private XmlNodeList syntaxHelperItems;

	private XmlNodeList scriptCompletion;

	private TextEditor toolTipEditor;

	private CompletionWindow completionWindow;

	private ToolTip toolTip;

	private Task taskSwitchView;

	private Task startCompileTask;

	private LoliScriptCompletionData.BlockParameters blockParameters;

	private BrushConverter bc = new BrushConverter();

	private SearchTextEditor searchTextEditor;

	private OcrEngine _ocrEngine;

	private string _tempSrcKey;

	private int _logFlushOffset = 0;

	private Task taskResto;

	private DispatcherTimer _errorTimer;

	private DispatcherTimer _periodicSaveTimer;

	internal TabControl stackerTabControl;

	internal TabItem codeTab;

	internal TextEditor loliScriptEditor;

	internal Button stackButton;

	internal Button openDocButton;

	internal TabItem stackTab;

	internal TextBox labelTextbox;

	internal PackIconMaterial disOrEnableIcon;

	internal PackIconFontAwesome iconSave;

	internal ListBox stackListView;

	internal ScrollViewer blockInfoScrollViewer;

	internal System.Windows.Controls.Frame BlockInfo;

	internal Button loliScriptButton;

	internal Button startDebuggerButton;

	internal PackIconMaterial startDebuggerButtonIcon;

	internal TextBlock startDebuggerButtonLabel;

	internal ComboBox testDataTypeCombobox;

	internal Button nextStepButton;

	internal ComboBox proxyTypeCombobox;

	internal TabControl debuggerTabControl;

	internal RichTextBox dataRTB;

	internal Grid logGrid;

	internal TextEditor logRTB;

	internal Button searchButton;

	internal Image previousMatchButton;

	internal Image nextMatchButton;

	internal TabItem htmlViewTab;

	internal WebControl webControl;

	internal EO.Wpf.WebView webView;

	internal TextBox browserStatus;

	private bool _contentLoaded;

	public event SaveConfigEventHandler SaveConfig;

	protected virtual void OnSaveConfig()
	{
		this.SaveConfig?.Invoke(this, EventArgs.Empty);
		try
		{
			LogEntry val = SB.Logger.Entries.LastOrDefault();
			if (val == null || (!val.LogString.StartsWith("Failed to save the config. Reason:") && (int)val.LogLevel != 2))
			{
				((Control)(object)iconSave).Foreground = bc.ConvertFrom("#FF5DF5A7") as Brush;
				RestoreForegroundIconSave();
			}
			else
			{
				((Control)(object)iconSave).Foreground = bc.ConvertFrom("#FFF5645D") as Brush;
				RestoreForegroundIconSave();
			}
		}
		catch
		{
			try
			{
				((Control)(object)iconSave).Foreground = Brushes.White;
			}
			catch
			{
			}
		}
	}

	private void RestoreForegroundIconSave()
	{
		try
		{
			try
			{
				taskResto?.Dispose();
			}
			catch
			{
			}
			taskResto = Task.Run(async delegate
			{
				await Task.Delay(1099);
				base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
				{
					((Control)(object)iconSave).Foreground = Brushes.White;
				});
			});
		}
		catch
		{
		}
	}

	public Stacker()
	{
		vm = SB.Stacker;
		base.DataContext = vm;
		InitializeComponent();
		loliScriptEditor.ShowLineNumbers = true;
		((Control)(object)loliScriptEditor.TextArea).Foreground = new SolidColorBrush(Colors.Gainsboro);
		loliScriptEditor.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Colors.DodgerBlue);
		using (XmlReader xmlReader = XmlReader.Create("LSHighlighting.xshd"))
		{
			loliScriptEditor.SyntaxHighlighting = HighlightingLoader.Load(xmlReader, (IHighlightingDefinitionReferenceResolver)(object)HighlightingManager.Instance);
		}
		XmlDocument xmlDocument = new XmlDocument();
		((UIElement)(object)loliScriptEditor).KeyDown += loliScriptEditor_KeyDown;
		((UIElement)(object)loliScriptEditor).KeyUp += LoliScriptEditor_KeyUp;
		loliScriptEditor.TextArea.TextEntered += TextArea_TextEntered;
		loliScriptEditor.TextArea.TextEntering += TextArea_TextEntering;
		loliScriptEditor.TextArea.KeyDown += TextArea_KeyDown;
		try
		{
			xmlDocument.Load("SyntaxHelper.xml");
			XmlNode xmlNode = xmlDocument.DocumentElement.SelectSingleNode("/doc");
			syntaxHelperItems = xmlNode.ChildNodes;
		}
		catch
		{
		}
		try
		{
			xmlDocument.Load("ScriptCompletion.xml");
			scriptCompletion = xmlDocument.DocumentElement.SelectSingleNode("/Keywords").ChildNodes;
		}
		catch
		{
		}
		toolTipEditor = new TextEditor();
		((Control)(object)toolTipEditor.TextArea).Foreground = Utils.GetBrush("ForegroundMain");
		((Control)(object)toolTipEditor).Background = new SolidColorBrush(Color.FromArgb(22, 22, 22, 50));
		toolTipEditor.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Colors.DodgerBlue);
		((Control)(object)toolTipEditor).FontSize = 11.0;
		toolTipEditor.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
		toolTipEditor.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
		using (XmlReader xmlReader2 = XmlReader.Create("LSHighlighting.xshd"))
		{
			toolTipEditor.SyntaxHighlighting = HighlightingLoader.Load(xmlReader2, (IHighlightingDefinitionReferenceResolver)(object)HighlightingManager.Instance);
		}
		toolTip = new ToolTip
		{
			Placement = PlacementMode.Relative,
			PlacementTarget = (UIElement)(object)loliScriptEditor
		};
		toolTip.Content = toolTipEditor;
		((FrameworkElement)(object)loliScriptEditor).ToolTip = toolTip;
		vm.LS = new LoliScript(SB.ConfigManager.CurrentConfig.Config.Script);
		loliScriptEditor.Text = vm.LS.Script;
		vm.ScriptCompletion = SB.SBSettings.General.ScriptCompletion;
		_errorTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
		_errorTimer.Tick += ErrorTimer_Tick;

		// Periodic auto-save: silently persists the config every 30 s.
		// Only runs when the user has enabled it in Settings → General → "Periodic auto-save".
		// Self-stops when this Stacker instance is no longer the active page.
		_periodicSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
		_periodicSaveTimer.Tick += (s, e) =>
		{
			try
			{
				if (SB.MainWindow.ConfigsPage.StackerPage != this)
				{
					_periodicSaveTimer.Stop();
					return;
				}
				if (!SB.SBSettings.General.PeriodicAutoSaveEnabled) return;
				if (vm?.Config?.Remote == true || vm?.Config == null) return;
				SB.MainWindow.ConfigsPage.ConfigManagerPage.SaveConfig();
			}
			catch { }
		};
		if (SB.SBSettings.General.PeriodicAutoSaveEnabled)
			_periodicSaveTimer.Start();

		// Auto-detect LoliCode on load: parse as blocks (same as non-LC configs) but
		// remember the original LC text so clicking "Code" restores it instead of LS.
		if (LoliCodeParser.IsLoliCode(vm.LS.Script ?? ""))
		{
			_isInLoliCodeMode = true;
			_lastModeWasLoliCode = true;
			_savedLoliCodeScript = vm.LS.Script;
			this.Loaded += (s, e) => LoadLoliCodeAsBlocks(_savedLoliCodeScript);
		}
		else if (!SB.SBSettings.General.DisplayLoliScriptOnLoad)
		{
			stackButton_Click(this, null);
		}
		logRTB.TextArea.TextView.LinkTextUnderline = false;
		logRTB.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Colors.DodgerBlue);
		logRTB.Options.EnableEmailHyperlinks = false;
		logRTB.Options.EnableImeSupport = false;
		logRTB.Options.CutCopyWholeLine = false;
		logRTB.Options.EnableTextDragDrop = false;
		logRTB.Options.EnableVirtualSpace = false;
		logRTB.Options.ShowTabs = false;
		((FrameworkElement)(object)logRTB.TextArea.TextView).Triggers.Clear();
		logRTB.Options.EnableRectangularSelection = false;
		searchTextEditor = SearchTextEditor.Install(logRTB);
		string[] names = Enum.GetNames(typeof(ProxyType));
		foreach (string text in names)
		{
			if (text != "Chain")
			{
				proxyTypeCombobox.Items.Add(text);
			}
		}
		proxyTypeCombobox.SelectedIndex = 0;
		foreach (string wordlistTypeName in SB.Settings.Environment.GetWordlistTypeNames())
		{
			testDataTypeCombobox.Items.Add(wordlistTypeName);
		}
		testDataTypeCombobox.SelectedIndex = 0;
		((BackgroundWorker)(object)debugger).WorkerSupportsCancellation = true;
		debugger.Status = (WorkerStatus)0;
		((BackgroundWorker)(object)debugger).DoWork += DebuggerCheck;
		((BackgroundWorker)(object)debugger).RunWorkerCompleted += debuggerCompleted;
		SaveConfig += SB.MainWindow.ConfigsPage.ConfigManagerPage.OnSaveConfig;
		try
		{
			BrowserOptions defaultBrowserOptions = new BrowserOptions
			{
				EnableWebSecurity = false,
				AllowJavaScriptCloseWindow = true,
				AllowJavaScriptAccessClipboard = false,
				AllowZooming = true
			};
			EngineOptions.Default.SetDefaultBrowserOptions((WebViewOptions)(object)defaultBrowserOptions);
		}
		catch
		{
		}
		SetBrowserStatus("Initialized!");
		vm.Stack.CollectionChanged += Stack_CollectionChanged;
		base.Loaded += (s, e) => SetupScriptModeUI();
	}

	private void Stack_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.NewItems != null)
		{
			foreach (StackerBlockViewModel newItem in e.NewItems)
			{
				newItem.Page.LostFocus += delegate
				{
					AutoSaveConfig();
				};
			}
		}
		AutoSaveConfig();
	}

	private void TextArea_KeyDown(object sender, KeyEventArgs e)
	{
		try
		{
			if (e.Key == System.Windows.Input.Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.Control && vm.ScriptCompletion)
			{
				InvokeCompletionWindow(GetDataList(string.Empty), Brushes.White);
				e.Handled = true;
			}
		}
		catch
		{
		}
	}

	private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
	{
		try
		{
			if (vm.ScriptCompletion && completionWindow == null && (e.Text == "\n" || e.Text == " "))
			{
				InvokeCompletionWindow(GetDataList(e.Text), Brushes.White);
			}
		}
		catch (Exception)
		{
		}
	}

	private IEnumerable<Tuple<string, string>> GetDataList(string text)
	{
		var list = new List<Tuple<string, string>>
		{
			Tuple.Create("REQUEST", "Make an HTTP/HTTPS request"),
			Tuple.Create("PARSE", "Parse data from a source string"),
			Tuple.Create("FUNCTION", "Execute a built-in function"),
			Tuple.Create("KEYCHECK", "Check success/fail/custom conditions"),
			Tuple.Create("UTILITY", "Utility operations block"),
			Tuple.Create("TCP", "TCP connection block"),
			Tuple.Create("WEBSOCKET", "WebSocket connection block"),
			Tuple.Create("IF", "Conditional IF block"),
			Tuple.Create("ELSE", "Else branch of IF"),
			Tuple.Create("ENDIF", "End of IF block"),
			Tuple.Create("WHILE", "WHILE loop"),
			Tuple.Create("ENDWHILE", "End of WHILE loop"),
			Tuple.Create("TRY", "Try block — catch errors"),
			Tuple.Create("CATCH", "Catch block for TRY errors"),
			Tuple.Create("ENDTRY", "End of TRY block"),
			Tuple.Create("FOREACH", "Iterate each item in a list"),
			Tuple.Create("ENDFOREACH", "End of FOREACH loop"),
			Tuple.Create("JUMP", "Jump to a line label"),
			Tuple.Create("BEGIN SCRIPT", "Begin inline script block"),
			Tuple.Create("END SCRIPT", "End inline script block"),
		};
		if (scriptCompletion != null)
		{
			foreach (XmlNode node in scriptCompletion)
			{
				try
				{
					string kw = node.Attributes?["name"]?.Value;
					string desc = node.Attributes?["desc"]?.Value ?? "";
					if (!string.IsNullOrEmpty(kw) && list.All(t => t.Item1 != kw))
						list.Add(Tuple.Create(kw, desc));
				}
				catch { }
			}
		}
		return list;
	}

	private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
	{
		if (vm.ScriptCompletion && e.Text.Length > 0 && completionWindow != null && char.IsLetter(e.Text[0]))
		{
			completionWindow.CompletionList.RequestInsertion((EventArgs)e);
		}
	}

	private void InvokeCompletionWindow(IEnumerable<Tuple<string, string>> scriptAutoCompleteList, Brush foreground)
	{
		if (scriptAutoCompleteList == null || !scriptAutoCompleteList.Any())
			return;
		this.completionWindow = new CompletionWindow(loliScriptEditor.TextArea);
		CompletionListBox listBox = this.completionWindow.CompletionList.ListBox;
		Brush brush = (((Control)(object)this.completionWindow.CompletionList).Background = Brushes.Black);
		Brush background = (((Control)(object)listBox).Background = brush);
		((Control)(object)this.completionWindow).Background = background;
		CompletionList completionList = this.completionWindow.CompletionList;
		background = (((Control)(object)this.completionWindow.CompletionList.ListBox).Foreground = foreground);
		((Control)(object)completionList).Foreground = background;
		IList<ICompletionData> completionData = this.completionWindow.CompletionList.CompletionData;
		foreach (Tuple<string, string> scriptAutoComplete in scriptAutoCompleteList)
		{
			completionData.Add((ICompletionData)new LoliScriptCompletionData(scriptAutoComplete.Item1, scriptAutoComplete.Item2));
		}
		((Window)(object)this.completionWindow).Show();
		((Window)(object)this.completionWindow).Closed += delegate
		{
			this.completionWindow = null;
		};
	}

	private void ClearDebuggerLog(object sender, EventArgs e)
	{
		if (SB.SBSettings.General.SendDebuggerLogToNotepadPlus)
		{
			try
			{
				NotepadPlusExtensions.Clear();
			}
			catch
			{
			}
		}
		(logRTB.Tag as MultiLineColorizer)?.Clear();
		logRTB.Clear();
	}

	private void Image_MouseEnter(object sender, MouseEventArgs e)
	{
		try
		{
			DependencyObject child = VisualTreeHelper.GetChild((Grid)e.OriginalSource, 0);
			DependencyObject obj = ((child is PackIconBase) ? child : null);
			((FrameworkElement)obj).Width = 27.5;
			((FrameworkElement)obj).Height = 27.5;
		}
		catch (InvalidCastException)
		{
		}
	}

	private void Image_MouseLeave(object sender, MouseEventArgs e)
	{
		try
		{
			DependencyObject child = VisualTreeHelper.GetChild((Grid)e.OriginalSource, 0);
			DependencyObject obj = ((child is PackIconBase) ? child : null);
			((FrameworkElement)obj).Width = 24.0;
			((FrameworkElement)obj).Height = 24.0;
		}
		catch (InvalidCastException)
		{
		}
	}

	private void AddBlock_MouseDown(object sender, MouseButtonEventArgs e)
	{
		new MainDialog(new DialogAddBlock(this), "Add Block").ShowDialog();
	}

	public void AddBlock(BlockBase block)
	{
		int num = ((vm.CurrentBlockIndex != -1) ? ((vm.Stack.Count > 0) ? (vm.CurrentBlockIndex + 1) : 0) : vm.Stack.Count);
		SB.Logger.LogInfo(Components.Stacker, $"Added a block of type {((object)block).GetType()} in position {num}");
		vm.AddBlock(block, num);
	}

	private void RemoveBlock_MouseDown(object sender, MouseButtonEventArgs e)
	{
		foreach (StackerBlockViewModel selectedBlock in vm.SelectedBlocks)
		{
			vm.Stack.Remove(selectedBlock);
		}
		vm.CurrentBlock = null;
		BlockInfo.Content = null;
		vm.UpdateHeights();
	}

	private void DisableBlock_MouseDown(object sender, MouseButtonEventArgs e)
	{
		foreach (StackerBlockViewModel selectedBlock in vm.SelectedBlocks)
		{
			selectedBlock.Disable();
		}
	}

	private void CloneBlock_MouseDown(object sender, MouseButtonEventArgs e)
	{
		vm.ConvertKeychains();
		foreach (StackerBlockViewModel selectedBlock in vm.SelectedBlocks)
		{
			vm.AddBlock(IOManager.CloneBlock(selectedBlock.Block), vm.Stack.IndexOf(selectedBlock) + 1);
		}
	}

	private void MoveUpBlock_MouseDown(object sender, MouseButtonEventArgs e)
	{
		foreach (StackerBlockViewModel selectedBlock in vm.SelectedBlocks)
		{
			vm.MoveBlockUp(selectedBlock);
		}
	}

	private void MoveDownBlock_MouseDown(object sender, MouseButtonEventArgs e)
	{
		foreach (StackerBlockViewModel item in vm.SelectedBlocks.AsEnumerable().Reverse())
		{
			vm.MoveBlockDown(item);
		}
	}

	private void SaveConfig_MouseDown(object sender, MouseButtonEventArgs e)
	{
		OnSaveConfig();
	}

	private void Page_KeyDown(object sender, KeyEventArgs e)
	{
		if (Keyboard.Modifiers != ModifierKeys.Control)
		{
			return;
		}
		switch (e.Key)
		{
		case System.Windows.Input.Key.Z:
			if (vm.LastDeletedBlock != null)
			{
				vm.AddBlock(vm.LastDeletedBlock, vm.LastDeletedIndex);
				SB.Logger.LogInfo(Components.Stacker, $"Readded block of type {((object)vm.LastDeletedBlock).GetType()} in position {vm.LastDeletedIndex}");
				vm.LastDeletedBlock = null;
			}
			else
			{
				SB.Logger.LogError(Components.Stacker, "Nothing to undo");
			}
			break;
		case System.Windows.Input.Key.C:
			if (SB.SBSettings.General.DisableCopyPasteBlocks)
			{
				break;
			}
			try
			{
				Clipboard.SetText(IOManager.SerializeBlocks(vm.SelectedBlocks.Select((StackerBlockViewModel b) => b.Block).ToList()));
				break;
			}
			catch
			{
				SB.Logger.LogError(Components.Stacker, "Exception while copying blocks");
				break;
			}
		case System.Windows.Input.Key.V:
			if (SB.SBSettings.General.DisableCopyPasteBlocks)
			{
				break;
			}
			try
			{
				foreach (BlockBase item in IOManager.DeserializeBlocks(Clipboard.GetText()))
				{
					vm.AddBlock(item);
				}
				break;
			}
			catch
			{
				SB.Logger.LogError(Components.Stacker, "Exception while pasting blocks");
				break;
			}
		case System.Windows.Input.Key.S:
			vm.LS.Script = loliScriptEditor.Text;
			OnSaveConfig();
			break;
		}
	}

	private void startDebuggerButton_Click(object sender, RoutedEventArgs e)
	{
		AutoSaveConfig();
		WorkerStatus status = debugger.Status;
		switch ((int)status)
		{
		case 0:
			if (vm.View == StackerView.Blocks)
			{
				vm.LS.FromBlocks(vm.GetList());
			}
			else
			{
				vm.LS.Script = loliScriptEditor.Text;
			}
			if (debuggerTabControl.SelectedIndex == 1)
			{
				((UIElement)(object)logRTB).Focus();
			}
			vm.ControlsEnabled = false;
			if (!SB.SBSettings.General.PersistDebuggerLog)
			{
				ClearDebuggerLog(null, null);
			}
			dataRTB.Document.Blocks.Clear();
			if (!((BackgroundWorker)(object)debugger).IsBusy)
			{
				((BackgroundWorker)(object)debugger).RunWorkerAsync();
				SB.Logger.LogInfo(Components.Stacker, "Started the debugger");
			}
			else
			{
				SB.Logger.LogError(Components.Stacker, "Cannot start the debugger (busy)");
			}
			startDebuggerButtonLabel.Text = "Abort";
			startDebuggerButtonLabel.Margin = new Thickness(2.0, 0.0, 0.0, 0.0);
			startDebuggerButtonIcon.Kind = (PackIconMaterialKind)5531;
			((FrameworkElement)(object)startDebuggerButtonIcon).Height = 10.0;
			debugger.Status = (WorkerStatus)1;
			break;
		case 1:
			if (((BackgroundWorker)(object)debugger).IsBusy)
			{
				((BackgroundWorker)(object)debugger).CancelAsync();
				SB.Logger.LogInfo(Components.Stacker, "Sent Cancellation Request to the debugger");
			}
			startDebuggerButtonLabel.Text = "Force";
			startDebuggerButtonLabel.Margin = new Thickness(2.0, 0.0, 0.0, 0.0);
			startDebuggerButtonIcon.Kind = (PackIconMaterialKind)5531;
			((FrameworkElement)(object)startDebuggerButtonIcon).Height = 10.0;
			debugger.Status = (WorkerStatus)2;
			break;
		case 2:
			debugger.Abort();
			SB.Logger.LogInfo(Components.Stacker, "Hard aborted the debugger");
			startDebuggerButtonLabel.Text = "Start";
			startDebuggerButtonLabel.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
			startDebuggerButtonIcon.Kind = (PackIconMaterialKind)4606;
			((FrameworkElement)(object)startDebuggerButtonIcon).Height = 13.0;
			debugger.Status = (WorkerStatus)0;
			vm.ControlsEnabled = true;
			break;
		}
	}

	private void DebuggerCheck(object sender, DoWorkEventArgs e)
	{
		if (vm.BotData != null && vm.BotData.BrowserOpen)
		{
			SB.Logger.LogInfo(Components.Stacker, "Quitting the previously opened browser");
			vm.BotData.Driver.Quit();
			SB.Logger.LogInfo(Components.Stacker, "Quitted correctly");
		}
		SB.Logger.LogInfo(Components.Stacker, "Converting Observables");
		vm.ConvertKeychains();
		SB.Logger.LogInfo(Components.Stacker, "Initializing the request data");
		CProxy val = null;
		if (vm.TestProxy.StartsWith("("))
		{
			try
			{
				val = new CProxy().Parse(vm.TestProxy, (ProxyType)0, "", "");
			}
			catch
			{
				SB.Logger.LogError(Components.Stacker, "Invalid Proxy Syntax", prompt: true);
			}
		}
		else
		{
			string text = string.Empty;
			string username = string.Empty;
			string password = string.Empty;
			if (vm.TestProxy.Contains(":"))
			{
				string[] array = vm.TestProxy.Split(':');
				text = array[0] + ":" + array[1];
				try
				{
					username = array[2];
				}
				catch
				{
				}
				try
				{
					password = array[3];
				}
				catch
				{
				}
			}
			val = new CProxy(text, vm.ProxyType);
			val.Username = username;
			val.Password = password;
		}
		CData val2 = new CData(vm.TestData, SB.Settings.Environment.GetWordlistType(vm.TestDataType));
		try
		{
			OcrEngine ocrEngine = _ocrEngine;
			if (ocrEngine != null)
			{
				ocrEngine.DisposeEngines();
			}
		}
		catch
		{
		}
		StackerViewModel stackerViewModel = vm;
		BotData val3 = new BotData(SB.Settings.RLSettings, vm.Config.Config.Settings, val2, val, vm.UseProxy, new Random(), 1, true)
		{
			BotsAmount = 1
		};
		OcrEngine obj5 = _ocrEngine;
		if (obj5 == null)
		{
			OcrEngine val4 = new OcrEngine();
			OcrEngine val5 = val4;
			_ocrEngine = val4;
			obj5 = val5;
		}
		val3.OcrEngine = obj5;
		val3.Worker = debugger;
		stackerViewModel.BotData = val3;
		vm.LS.Reset();
		base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
		{
			browserStatus.Text = "Idle";
		});
		foreach (CustomInput input in vm.BotData.ConfigSettings.CustomInputs)
		{
			SB.Logger.LogInfo(Components.Stacker, "Asking for user input: " + input.Description);
			Application.Current.Dispatcher.Invoke(delegate
			{
				new MainDialog(new DialogCustomInput(vm, input.VariableName, input.Description), "Custom Input").ShowDialog();
			});
		}
		SB.Logger.LogInfo(Components.Stacker, "Setting the first block as the current block");
		string text2 = (vm.UseProxy ? "ENABLED" : "DISABLED");
		vm.BotData.LogBuffer.Add(new LogEntry($"===== DEBUGGER STARTED FOR CONFIG {vm.Config.Name} WITH DATA {vm.TestData} AND PROXY {vm.TestProxy} ({vm.ProxyType}) {text2} ====={Environment.NewLine}", Colors.White));
		timer = new Stopwatch();
		timer.Start();
		if (vm.Config.Config.Settings.AlwaysOpen)
		{
			SB.Logger.LogInfo(Components.Stacker, "Opening the Browser");
			SBlockBrowserAction.OpenBrowser(vm.BotData, "");
		}
		if (vm.SBS)
		{
			vm.SBSClear = true;
			do
			{
				Thread.Sleep(100);
				if (((BackgroundWorker)(object)debugger).CancellationPending)
				{
					SB.Logger.LogInfo(Components.Stacker, "Found cancellation pending, aborting debugger");
					return;
				}
				if (vm.SBSClear)
				{
					vm.SBSEnabled = false;
					Process();
					SB.Logger.LogInfo(Components.Stacker, $"Block processed in SBS mode, can proceed: {vm.LS.CanProceed}");
					vm.SBSEnabled = true;
					vm.SBSClear = false;
				}
			}
			while (vm.LS.CanProceed);
		}
		else
		{
			do
			{
				if (((BackgroundWorker)(object)debugger).CancellationPending)
				{
					SB.Logger.LogInfo(Components.Stacker, "Found cancellation pending, aborting debugger");
					return;
				}
				Process();
				// Break immediately on terminal status so PrintLogBuffer is not
				// called again from offset 0, which would re-display all log entries.
				var _s = vm.BotData.Status;
				if ((_s == BotStatus.CUSTOM && !vm.BotData.ConfigSettings.ContinueOnCustom) ||
				    (_s != BotStatus.NONE && _s != BotStatus.SUCCESS && _s != BotStatus.CUSTOM))
					break;
			}
			while (vm.LS.CanProceed);
		}
		if (vm.Config.Config.Settings.AlwaysQuit || (vm.Config.Config.Settings.QuitOnBanRetry && ((int)vm.BotData.Status == 4 || (int)vm.BotData.Status == 5)))
		{
			try
			{
				vm.BotData.Driver.Quit();
				vm.BotData.BrowserOpen = false;
				SB.Logger.LogInfo(Components.Stacker, "Successfully quit the browser");
			}
			catch (Exception ex)
			{
				SB.Logger.LogError(Components.Stacker, "Cannot quit the browser - " + ex.Message);
			}
		}
	}

	private void Process()
	{
		_logFlushOffset = 0;
		vm.BotData.OnFlush = FlushPartialLog;
		try
		{
			vm.LS.TakeStep(vm.BotData);
			SB.Logger.LogInfo(Components.Stacker, "Processed " + BlockBase.TruncatePretty(vm.LS.CurrentLine, 20));
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.Stacker, "Processing of line " + BlockBase.TruncatePretty(vm.LS.CurrentLine, 20) + " failed, exception: " + ex.Message);
		}
		vm.BotData.OnFlush = null;
		PrintBotData();
		PrintLogBuffer();
		DisplayHTML();
	}

	private void FlushPartialLog()
	{
		int from = _logFlushOffset;
		int to = vm.BotData.LogBuffer.Count;
		if (from >= to) return;
		for (int i = from; i < to; i++)
		{
			LogEntry entry = vm.BotData.LogBuffer[i];
			if (!SB.SBSettings.General.DisableDebuggerLog)
			{
				base.Dispatcher.BeginInvoke(DispatcherPriority.Input, (ThreadStart)delegate
				{
					logRTB.AppendTextToEditor(entry.LogString, entry.LogColor);
					logRTB.TextArea.Caret.BringCaretToView();
					logRTB.ScrollToLine(logRTB.LineCount);
				});
			}
			if (SB.SBSettings.General.SendDebuggerLogToNotepadPlus)
				NotepadPlusExtensions.ShowText(entry.LogString + Environment.NewLine);
		}
		_logFlushOffset = to;
	}

	private void PrintLogBuffer()
	{
		int from = _logFlushOffset;
		_logFlushOffset = 0;
		if (vm.BotData.LogBuffer.Count == 0 || vm.BotData.LogBuffer.Count <= from)
		{
			return;
		}
		for (int i = from; i < vm.BotData.LogBuffer.Count; i++)
		{
			LogEntry entry = vm.BotData.LogBuffer[i];
			if (!SB.SBSettings.General.DisableDebuggerLog)
			{
				base.Dispatcher.BeginInvoke(DispatcherPriority.Input, (ThreadStart)delegate
				{
					logRTB.AppendTextToEditor(entry.LogString, entry.LogColor);
					logRTB.TextArea.Caret.BringCaretToView();
					logRTB.ScrollToLine(logRTB.LineCount);
				});
			}
			if (SB.SBSettings.General.SendDebuggerLogToNotepadPlus)
			{
				NotepadPlusExtensions.ShowText(entry.LogString + Environment.NewLine);
			}
		}
	}

	private void PrintBotData()
	{
		Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (ThreadStart)delegate
		{
			dataRTB.Document.Blocks.Clear();
			dataRTB.AppendText(Environment.NewLine);
			dataRTB.AppendText("BOT STATUS: " + vm.BotData.StatusString + Environment.NewLine, Colors.White);
			dataRTB.AppendText("VARIABLES:" + Environment.NewLine, Colors.Yellow);
			if (SB.SBSettings.General.DisplayCapturesLast)
			{
				foreach (CVar item in vm.BotData.Variables.All.Where((CVar v) => !v.Hidden && !v.IsCapture))
				{
					dataRTB.AppendText(item.Name + $" ({(item.Type == CVar.VarType.List ? "ListOfStrings" : item.Type.ToString())}) = " + ((object)item).ToString() + Environment.NewLine, Colors.Yellow);
				}
				{
					foreach (CVar item2 in vm.BotData.Variables.All.Where((CVar v) => !v.Hidden && v.IsCapture))
					{
						dataRTB.AppendText(item2.Name + $" ({(item2.Type == CVar.VarType.List ? "ListOfStrings" : item2.Type.ToString())}) = " + ((object)item2).ToString() + Environment.NewLine, Colors.Tomato);
					}
					return;
				}
			}
			foreach (CVar item3 in vm.BotData.Variables.All.Where((CVar v) => !v.Hidden))
			{
				dataRTB.AppendText(item3.Name + $" ({(item3.Type == CVar.VarType.List ? "ListOfStrings" : item3.Type.ToString())}) = " + ((object)item3).ToString() + Environment.NewLine, item3.IsCapture ? Colors.Tomato : Colors.Yellow);
			}
		});
	}

	private void DisplayHTML()
	{
		if (SB.SBSettings.General.DisableHTMLView)
		{
			return;
		}
		base.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (ThreadStart)delegate
		{
			string _srcKey = vm.BotData.ResponseSource?.Length.ToString() + (vm.BotData.ResponseSource?.Length > 0 ? vm.BotData.ResponseSource.Substring(0, Math.Min(128, vm.BotData.ResponseSource.Length)) : "");
			if (!string.IsNullOrEmpty(vm.BotData.ResponseSource) && _srcKey != _tempSrcKey)
			{
				if (SB.SBSettings.General.LocalHTMLViewer)
				{
					EO.Wpf.WebView obj = webView;
					if (obj != null)
					{
						((EO.Wpf.WebView)obj).LoadHtml(vm.BotData.ResponseSource);
					}
					_tempSrcKey = _srcKey;
				}
				else
				{
					if (SB.SBSettings.General.EnableCookiesInBrowser)
					{
						string host = new Uri(vm.BotData.Address).Host;
						foreach (KeyValuePair<string, string> cookie in vm.BotData.Cookies)
						{
							try
							{
								((EO.Wpf.WebView)webView).Engine.CookieManager.SetCookie(vm.BotData.Address, new Cookie(cookie.Key, cookie.Value)
								{
									Domain = host,
									Path = "/"
								});
							}
							catch
							{
							}
						}
						foreach (KeyValuePair<string, string> item in (Dictionary<string, string>)(object)vm.BotData.GlobalCookies)
						{
							try
							{
								((EO.Wpf.WebView)webView).Engine.CookieManager.SetCookie(vm.BotData.Address, new Cookie(item.Key, item.Value)
								{
									Domain = host,
									Path = "/"
								});
							}
							catch
							{
							}
						}
					}
					if (!vm.BotData.IsImage)
					{
						EO.Wpf.WebView obj4 = webView;
						if (obj4 != null)
						{
							((EO.Wpf.WebView)obj4).LoadHtml(vm.BotData.ResponseSource, vm.BotData.Address);
						}
					}
					else
					{
						EO.Wpf.WebView obj5 = webView;
						if (obj5 != null)
						{
							((EO.Wpf.WebView)obj5).LoadUrl(vm.BotData.Address);
						}
					}
					_tempSrcKey = _srcKey;
				}
			}
		});
	}

	public void HideScriptErrors(WebBrowser wb, bool hide)
	{
		FieldInfo field = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			return;
		}
		object value = field.GetValue(wb);
		if (value == null)
		{
			wb.Loaded += delegate
			{
				HideScriptErrors(wb, hide);
			};
		}
		else
		{
			value.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, value, new object[1] { hide });
		}
	}

	private void WebView_BeforeNavigate(object sender, BeforeNavigateEventArgs e)
	{
		SetBrowserStatus("Navigating...");
		if (((CancelEventArgs)(object)e).Cancel)
		{
			SetBrowserStatus("Navigation cancelled");
		}
	}

	private void WebView_LoadCompleted(object sender, LoadCompletedEventArgs e)
	{
		SetBrowserStatus($"Navigation completed ({e.HttpStatusCode})");
	}

	private void debuggerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		timer.Stop();
		debugger.Status = (WorkerStatus)0;
		startDebuggerButtonLabel.Text = "Start";
		startDebuggerButtonLabel.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
		startDebuggerButtonIcon.Kind = (PackIconMaterialKind)4606;
		((FrameworkElement)(object)startDebuggerButtonIcon).Height = 13.0;
		vm.SBSEnabled = false;
		vm.ControlsEnabled = true;
		vm.BotData.LogBuffer.Clear();
		if (!vm.BotData.Data.IsValid)
		{
			vm.BotData.LogBuffer.Add(new LogEntry("WARNING: The test input data did not respect the validity regex for the selected wordlist type!", Colors.Tomato));
		}
		if (!vm.BotData.Data.RespectsRules(vm.Config.Config.Settings.DataRules.ToList()))
		{
			vm.BotData.LogBuffer.Add(new LogEntry("WARNING: The test input data did not respect the data rules of this config!", Colors.Tomato));
		}
		vm.BotData.LogBuffer.Add(new LogEntry($"===== DEBUGGER ENDED AFTER {(double)timer.ElapsedMilliseconds / 1000.0} SECOND(S) WITH STATUS: {vm.BotData.StatusString} =====", Colors.White));
		PrintLogBuffer();
		SB.Logger.LogInfo(Components.Stacker, "Debugger completed");
	}

	private void nextStepButton_Click(object sender, RoutedEventArgs e)
	{
		vm.SBSClear = true;
	}

	private void proxyTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.ProxyType = (ProxyType)proxyTypeCombobox.SelectedIndex;
	}

	private void searchButton_Click(object sender, RoutedEventArgs e)
	{
		SB.Logger.LogInfo(Components.Stacker, "Seaching for " + vm.SearchString);
		logRTB.TextArea.ClearSelection();
		if (vm.SearchString == string.Empty)
		{
			vm.TotalSearchMatches = 0;
			vm.CurrentSearchMatch = 0;
			return;
		}
		logRTB.SelectionStart = 0;
		searchTextEditor.SearchPattern = vm.SearchString;
		searchTextEditor.UpdateSearch();
		searchTextEditor.DoSearch(changeSelection: true);
		vm.TotalSearchMatches = searchTextEditor.Count;
		SB.Logger.LogInfo(Components.Stacker, $"Found {vm.TotalSearchMatches} matches");
		if (vm.TotalSearchMatches > 0)
		{
			vm.CurrentSearchMatch = 1;
		}
	}

	public static List<int> AllIndexesOf(string str, string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			throw new ArgumentException("the string to find may not be empty", "value");
		}
		List<int> list = new List<int>();
		int startIndex = 0;
		while (true)
		{
			startIndex = str.IndexOf(value, startIndex, StringComparison.InvariantCultureIgnoreCase);
			if (startIndex == -1)
			{
				break;
			}
			list.Add(startIndex);
			startIndex += value.Length;
		}
		return list;
	}

	private void previousMatchButton_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (vm.TotalSearchMatches != 0)
		{
			if (vm.CurrentSearchMatch == 1)
			{
				vm.CurrentSearchMatch = vm.TotalSearchMatches;
			}
			else
			{
				vm.CurrentSearchMatch--;
			}
			searchTextEditor.FindPrevious();
		}
	}

	private void nextMatchButton_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (vm.TotalSearchMatches != 0)
		{
			if (vm.CurrentSearchMatch == vm.TotalSearchMatches)
			{
				vm.CurrentSearchMatch = 1;
			}
			else
			{
				vm.CurrentSearchMatch++;
			}
			searchTextEditor.FindNext();
		}
	}

	private void labelTextbox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (vm.CurrentBlock != null)
		{
			vm.CurrentBlock.Block.Label = labelTextbox.Text;
		}
	}

	public static TextPointer GetTextPointAt(TextPointer from, int pos)
	{
		TextPointer textPointer = from;
		int num = 0;
		while (num < pos && textPointer != null)
		{
			if (textPointer.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text || textPointer.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.None)
			{
				num++;
			}
			if (textPointer.GetPositionAtOffset(1, LogicalDirection.Forward) == null)
			{
				return textPointer;
			}
			textPointer = textPointer.GetPositionAtOffset(1, LogicalDirection.Forward);
		}
		return textPointer;
	}

	private void testDataTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (testDataTypeCombobox.SelectedItem == null)
		{
			testDataTypeCombobox.SelectedIndex = testDataTypeCombobox.Items.IndexOf(vm.TestDataType);
		}
		else
		{
			vm.TestDataType = (string)testDataTypeCombobox.SelectedItem;
		}
	}

	private void blockClicked(object sender, RoutedEventArgs e)
	{
		ToggleButton toggleButton = sender as ToggleButton;
		StackerBlockViewModel blockById = vm.GetBlockById((int)toggleButton.Tag);
		if (Keyboard.Modifiers == ModifierKeys.Control)
		{
			blockById.Selected = !blockById.Selected;
		}
		else
		{
			vm.DeselectAll();
			blockById.Selected = true;
		}
		try
		{
			blockInfoScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		}
		catch
		{
		}
		StackerBlockViewModel stackerBlockViewModel2 = (vm.CurrentBlock = vm.SelectedBlocks.LastOrDefault());
		if (stackerBlockViewModel2 != null)
		{
			if (vm.CurrentBlock.Page != null)
			{
				BlockInfo.Content = vm.CurrentBlock.Page;
			}
			Keyboard.ClearFocus();
			if (vm.CurrentBlock.Page.Title == "PageBlockKeycheck")
			{
				blockInfoScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
			}
			labelTextbox.Text = vm.CurrentBlock.Block.Label;
		}
	}

	public void SetScript()
	{
		if (vm.View == StackerView.Blocks)
		{
			vm.LS.FromBlocks(vm.GetList());
		}
		else
		{
			vm.LS.Script = loliScriptEditor.Text;
		}
		vm.Config.Config.Script = vm.LS.Script;
	}

	private void loliScriptButton_Click(object sender, RoutedEventArgs e)
	{
		// Capture whether we're coming from blocks view before the view changes
		bool wasInBlocksView = vm.View == StackerView.Blocks;

		// On first entry: detect the original format BEFORE FromBlocks overwrites vm.LS.Script
		if (!_modeUIInjected)
		{
			string storedScript = vm.LS.Script ?? "";
			if (LoliCodeParser.IsLoliCode(storedScript))
			{
				// Config is LoliCode — load it directly, don't convert via FromBlocks
				loliScriptEditor.Text = storedScript;
				vm.View = StackerView.LoliScript;
				stackerTabControl.SelectedIndex = 0;
				InjectModeButtons();
				_isInLoliCodeMode = true;
				_lastModeWasLoliCode = true;
				UpdateScriptModeButton();
				return;
			}
			_lastModeWasLoliCode = false;
		}

		vm.View = StackerView.LoliScript;
		stackerTabControl.SelectedIndex = 0;
		InjectModeButtons();

		if (_lastModeWasLoliCode && _savedLoliCodeScript != null)
		{
			if (wasInBlocksView)
			{
				// User changed blocks in visual Stacker — regenerate LoliCode from current blocks
				// so that property edits (DateFormat etc.) are reflected in the code view.
				vm.LS.FromBlocks(vm.GetList());
				var _lcBlocks = vm.LS.ToBlocks();
				string _freshLc = RuriLib.LS.LoliCode.LoliCodeSerializer.BlocksToLoliCode(_lcBlocks);
				_freshLc = RuriLib.LS.LoliCode.LoliCodeSerializer.InjectMissingUsings(_freshLc);
				_savedLoliCodeScript = _freshLc;
				_loliCodeAfterConversion = _freshLc;
				loliScriptEditor.Text = _freshLc;
			}
			else
			{
				// Not coming from Blocks view — restore the exact LoliCode as saved
				loliScriptEditor.Text = _savedLoliCodeScript;
			}
			_isInLoliCodeMode = true;
		}
		else
		{
			// Sync current block values → vm.LS.Script so the editor reflects what the user typed
			if (wasInBlocksView) vm.LS.FromBlocks(vm.GetList());
			loliScriptEditor.Text = vm.LS.Script;
			_isInLoliCodeMode = false;
		}
		UpdateScriptModeButton();
	}

	private void stackButton_Click(object sender, RoutedEventArgs e)
	{
		// Always sync editor → vm.LS.Script before converting to blocks.
		// This ensures the latest editor text is used even if LostFocus didn't fire.
		if (vm.View != StackerView.Blocks)
		{
			_lastModeWasLoliCode = _isInLoliCodeMode;

			// LoliCode can't go to blocks directly — convert to LoliScript first
			if (_isInLoliCodeMode)
			{
				_savedLoliCodeScript = loliScriptEditor.Text; // preserve exact LC before conversion

				// Preserve the LS/LC save state so that the IF/ELSE restoration still works
				// after the user returns from Blocks (ConvertEditorToLoliScript clears them).
				string lsBeforeLC   = _savedLoliScriptBeforeLC;
				string lcAfterConv  = _loliCodeAfterConversion;

				ConvertEditorToLoliScript();
				if (_isInLoliCodeMode) return; // conversion failed, abort

				// Restore so clicking LS button after Code→Blocks→Code still has the original LS
				_savedLoliScriptBeforeLC = lsBeforeLC;
				_loliCodeAfterConversion = lcAfterConv;
			}
			else
			{
				_savedLoliCodeScript = null; // not in LC mode, clear any stale save
			}

			vm.LS.Script = loliScriptEditor.Text;
		}

		List<BlockBase> blocks = null;
		Action action = async delegate
		{
			_isLoadingBlocks = true;
			try
			{
				blocks = vm.LS.ToBlocks();
			}
			catch (Exception ex)
			{
				_isLoadingBlocks = false;
				Exception ex2 = ex;
				await base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
				{
					MessageBox.Show("Error while converting to blocks, please check the syntax!\n" + ex2.Message);
				});
				vm.View = StackerView.LoliScript;
				return;
			}
			try
			{
				await base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
				{
					vm.ClearBlocks();
				});
			}
			catch (Exception)
			{
			}
			// Add all blocks in a single Dispatcher call so AutoSave can't fire mid-load.
			await base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
			{
				foreach (var block in blocks)
					vm.AddBlock(block);
			});
			vm.CurrentBlock = null;
			base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
			{
				BlockInfo.Content = null;
			});
			vm.View = StackerView.Blocks;
			await base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
			{
				stackerTabControl.SelectedIndex = 1;
			});
			_isLoadingBlocks = false;
		};
		try
		{
			if (vm.View != 0)
			{
				AutoSaveConfig();
			}
			taskSwitchView?.Dispose();
			if (sender is Button)
			{
				stackerTabControl.BlurApply(0.0, 5.0, TimeSpan.FromSeconds(0.1));
				stackerTabControl.IsEnabled = false;
				taskSwitchView = Task.Run(action).ContinueWith(delegate
				{
					base.Dispatcher.Invoke(delegate
					{
						stackerTabControl.BlurDisable(TimeSpan.FromSeconds(0.1));
						stackerTabControl.IsEnabled = true;
					});
				});
			}
			else
			{
				taskSwitchView = Task.Run(action);
				taskSwitchView.Wait();
				taskSwitchView.Dispose();
			}
		}
		catch
		{
			stackerTabControl.IsEnabled = true;
			stackerTabControl.BlurDisable(TimeSpan.FromSeconds(0.1));
		}
	}

	private void loliScriptEditor_LostFocus(object sender, RoutedEventArgs e)
	{
		vm.LS.Script = loliScriptEditor.Text;
		toolTip.IsOpen = false;
		AutoSaveConfig();
		UpdateScriptModeButton();
	}

	// ─── Script-mode toggle (LoliCode ↔ LoliScript) ────────────────────────────

	// Two buttons injected inside the code tab header bar (replacing "LoliScript Code" label)
	private Button _btnModeLS;
	private Button _btnModeLC;
	private SolidColorBrush _fgBrushLS;
	private SolidColorBrush _fgBrushLC;
	private SolidColorBrush _bgBrushLS;
	private SolidColorBrush _bgBrushLC;
	private DropShadowEffect _glowLS;
	private DropShadowEffect _glowLC;
	private Border _underlineLS;
	private Border _underlineLC;

	private static T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate = null) where T : DependencyObject
	{
		if (parent == null) return null;
		int n = VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < n; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is T t && (predicate == null || predicate(t))) return t;
			var found = FindVisualChild(child, predicate);
			if (found != null) return found;
		}
		return null;
	}

	private bool _modeUIInjected = false;
	private bool _lastModeWasLoliCode = false;
	private bool _isInLoliCodeMode = false;
	private string _savedLoliCodeScript = null;      // original LC text saved when going to Blocks
	private string _savedLoliScriptBeforeLC = null;  // original LS text saved before converting to LC
	private string _loliCodeAfterConversion = null;  // LC text right after LS→LC conversion (change detection)

	// Load a LoliCode script as visual blocks (same starting view as LoliScript configs).
	// _savedLoliCodeScript + _lastModeWasLoliCode stay set so clicking "Code" restores LC.
	private void LoadLoliCodeAsBlocks(string loliCode)
	{
		try
		{
			var segs   = LoliCodeParser.Parse(loliCode ?? "");
			var blocks = LoliCodeSerializer.SegmentsToBlocks(segs);

			vm.ClearBlocks();
			for (int i = 0; i < blocks.Count; i++)
				vm.AddBlock(blocks[i], i);

			vm.CurrentBlock = null;
			try { BlockInfo.Content = null; } catch { }
			vm.View = StackerView.Blocks;
			try { stackerTabControl.SelectedIndex = 1; } catch { }

			_isInLoliCodeMode = false; // in blocks view now; restored to true when user clicks Code
		}
		catch
		{
			// Fallback: stay in LoliCode editor view so nothing is lost
		}
	}

	private void SetupScriptModeUI()
	{
		// Always rename the bottom-left button (safe even if codeTab isn't rendered yet)
		try { loliScriptButton.Content = "</> SWITCH TO CODE"; } catch { }

		// Always set up the right-click context menu on the code editor
		try
		{
			var cm = new System.Windows.Controls.ContextMenu();
			var miToLC = new System.Windows.Controls.MenuItem { Header = "⇄  Convert to LoliCode (OB2)" };
			miToLC.Click += (s, e) => ConvertEditorToLoliCode();
			var miToLS = new System.Windows.Controls.MenuItem { Header = "⇄  Convert to LoliScript" };
			miToLS.Click += (s, e) => ConvertEditorToLoliScript();
			cm.Items.Add(miToLC);
			cm.Items.Add(miToLS);
			((FrameworkElement)(object)loliScriptEditor).ContextMenu = cm;
		}
		catch { }

		// Inject the visible mode-toggle buttons (deferred until the code tab is rendered)
		InjectModeButtons();
	}

	private void InjectModeButtons()
	{
		if (_modeUIInjected) return;
		try
		{
			// BorderBrush brushes for the animated highlight border (all 4 sides)
			_bgBrushLS = new SolidColorBrush(Color.FromArgb(0, 0, 175, 230));
			_bgBrushLC = new SolidColorBrush(Color.FromArgb(0, 0, 175, 230));

			// Buttons use the same style as the rest of the UI
			_btnModeLS = new Button
			{
				Content = "LoliScript",
				Padding = new Thickness(22, 6, 22, 6),
				Margin = new Thickness(0),
				VerticalAlignment = VerticalAlignment.Center,
				FontSize = 13,
			};
			_btnModeLC = new Button
			{
				Content = "LoliCode",
				Padding = new Thickness(22, 6, 22, 6),
				Margin = new Thickness(0),
				VerticalAlignment = VerticalAlignment.Center,
				FontSize = 13,
			};
			try { _btnModeLS.Style = ((FrameworkElement)(object)loliScriptButton).Style; } catch { }
			try { _btnModeLC.Style = ((FrameworkElement)(object)loliScriptButton).Style; } catch { }
			_btnModeLS.Click += (s, e) => ConvertEditorToLoliScript();
			_btnModeLC.Click += (s, e) => ConvertEditorToLoliCode();

			// Strategy A: replace the "LoliScript Code" TextBlock in the visual tree
			if (TryReplaceCodeLabelVisual() || TryReplaceCodeLabelLogical())
			{
				_modeUIInjected = true;
				UpdateScriptModeButton();
				return;
			}

			// Strategy B: insert a bar above the editor
			var editorLogicalParent = ((FrameworkElement)(object)loliScriptEditor).Parent as Panel;
			if (editorLogicalParent != null)
			{
				var modeBar = new Border
				{
					HorizontalAlignment = HorizontalAlignment.Stretch,
					Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)),
					Padding = new Thickness(0, 4, 0, 4),
					Child = BuildModePill(),
				};

				int edIdx = editorLogicalParent.Children.IndexOf((UIElement)(object)loliScriptEditor);
				editorLogicalParent.Children.Insert(edIdx < 0 ? 0 : edIdx, modeBar);
				if (editorLogicalParent is DockPanel)
					DockPanel.SetDock(modeBar, Dock.Top);

				_modeUIInjected = true;
				UpdateScriptModeButton();
			}
		}
		catch { }
	}

	private Border BuildModePill()
	{
		// Wrapper Borders provide the animated highlight outline on all 4 sides
		_underlineLS = new Border
		{
			BorderThickness = new Thickness(1.5),
			BorderBrush = _bgBrushLS,
			CornerRadius = new CornerRadius(2),
			Margin = new Thickness(0, 0, 4, 0),
			Child = _btnModeLS,
		};
		_underlineLC = new Border
		{
			BorderThickness = new Thickness(1.5),
			BorderBrush = _bgBrushLC,
			CornerRadius = new CornerRadius(2),
			Child = _btnModeLC,
		};
		var inner = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
		inner.Children.Add(_underlineLS);
		inner.Children.Add(_underlineLC);
		return new Border
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(6, 0, 6, 0),
			Child = inner,
		};
	}

	private bool TryReplaceCodeLabelVisual()
	{
		try
		{
			var tb = FindVisualChild<TextBlock>((DependencyObject)(object)codeTab,
				t => t.Text != null && t.Text.Contains("LoliScript") && t.Text.Contains("Code"));
			if (tb == null) return false;
			return ReplaceElementWithModePanel((UIElement)(object)tb);
		}
		catch { return false; }
	}

	private bool TryReplaceCodeLabelLogical()
	{
		try
		{
			// Walk logical tree for TextBlock or Label containing "LoliScript Code"
			var tb = FindLogicalChild<TextBlock>((DependencyObject)(object)codeTab,
				t => t.Text != null && t.Text.Contains("LoliScript") && t.Text.Contains("Code"));
			if (tb != null) return ReplaceElementWithModePanel((UIElement)(object)tb);

			var lbl = FindLogicalChild<Label>((DependencyObject)(object)codeTab,
				l => l.Content is string s && s.Contains("LoliScript") && s.Contains("Code"));
			if (lbl != null) return ReplaceElementWithModePanel((UIElement)(object)lbl);
		}
		catch { }
		return false;
	}

	private bool ReplaceElementWithModePanel(UIElement element)
	{
		var pill = BuildModePill();

		if (((FrameworkElement)element).Parent is Panel panel)
		{
			bool isDock = panel is DockPanel;
			Dock savedDock = isDock ? DockPanel.GetDock(element) : Dock.Left;
			int idx = panel.Children.IndexOf(element);
			panel.Children.RemoveAt(idx);
			panel.Children.Insert(idx, pill);
			if (isDock) DockPanel.SetDock(pill, savedDock);

			// Hide any icon siblings (book icon, PackIcon, Image, etc.)
			foreach (UIElement child in panel.Children)
			{
				if (child == pill) continue;
				string typeName = child.GetType().Name;
				if (child is System.Windows.Controls.Image ||
				    typeName.StartsWith("PackIcon") ||
				    typeName.Contains("Icon"))
					child.Visibility = Visibility.Collapsed;
			}
			return true;
		}
		return false;
	}

	private static T FindLogicalChild<T>(DependencyObject parent, Func<T, bool> predicate = null) where T : DependencyObject
	{
		if (parent == null) return null;
		foreach (object child in LogicalTreeHelper.GetChildren(parent))
		{
			if (child is DependencyObject dep)
			{
				if (dep is T t && (predicate == null || predicate(t))) return t;
				var found = FindLogicalChild(dep, predicate);
				if (found != null) return found;
			}
		}
		return null;
	}

	private void UpdateScriptModeButton()
	{
		if (_btnModeLS == null || _btnModeLC == null) return;
		try
		{
			bool isLC = _isInLoliCodeMode;
			var dur  = new Duration(TimeSpan.FromMilliseconds(220));
			var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

			// Fade opacity: active = full, inactive = dimmed
			_btnModeLS.BeginAnimation(UIElement.OpacityProperty,
				new DoubleAnimation(isLC ? 0.42 : 1.0, dur) { EasingFunction = ease });
			_btnModeLC.BeginAnimation(UIElement.OpacityProperty,
				new DoubleAnimation(isLC ? 1.0 : 0.42, dur) { EasingFunction = ease });

			// Animate border highlight on all 4 sides: active = cyan, inactive = transparent
			var activeCyan = Color.FromRgb(0, 175, 230);
			var clearCyan  = Color.FromArgb(0, 0, 175, 230);
			if (_bgBrushLS != null)
			{
				_bgBrushLS.BeginAnimation(SolidColorBrush.ColorProperty,
					new ColorAnimation(isLC ? clearCyan : activeCyan, dur) { EasingFunction = ease });
				_bgBrushLC.BeginAnimation(SolidColorBrush.ColorProperty,
					new ColorAnimation(isLC ? activeCyan : clearCyan, dur) { EasingFunction = ease });
			}

			_btnModeLS.FontWeight = isLC ? FontWeights.Normal : FontWeights.Bold;
			_btnModeLC.FontWeight = isLC ? FontWeights.Bold   : FontWeights.Normal;
			_btnModeLC.ToolTip = isLC ? "Active — click to switch to LoliScript" : "Click to switch to LoliCode";
			_btnModeLS.ToolTip = isLC ? "Click to switch to LoliScript" : "Active — click to switch to LoliCode";
		}
		catch { }
	}

	private void ConvertEditorToLoliCode()
	{
		try
		{
			string current = loliScriptEditor.Text;
			if (!_isInLoliCodeMode)
			{
				if (LoliCodeParser.IsLoliCode(current))
				{
					// The editor already contains LoliCode (user pasted LC into the LS tab).
					// Don't save it as _savedLoliScriptBeforeLC — it is not LoliScript,
					// so restoring it later would put LoliCode back in the LS editor.
					// Setting null forces ConvertEditorToLoliScript to do a real LC→LS conversion.
					_savedLoliScriptBeforeLC = null;
					_loliCodeAfterConversion = current;
				}
				else
				{
					_savedLoliScriptBeforeLC = current; // preserve LS before converting so IF/ELSE/ENDIF can round-trip
					vm.LS.Script = current;
					var blocks = vm.LS.ToBlocks();
					string loliCode = LoliCodeSerializer.BlocksToLoliCode(blocks);
					loliCode = LoliCodeSerializer.InjectMissingUsings(loliCode);
					vm.LS.Script = loliCode;
					loliScriptEditor.Text = loliCode;
					// Read back from editor after it has normalized line endings,
					// so the comparison in ConvertEditorToLoliScript is always consistent.
					_loliCodeAfterConversion = loliScriptEditor.Text;
				}
			}
			_isInLoliCodeMode = true;
			UpdateScriptModeButton();
		}
		catch (Exception ex)
		{
			System.Windows.MessageBox.Show(
				"Error converting to LoliCode:\n" + ex.Message,
				"Conversion Error", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
	}

	private void ConvertEditorToLoliScript()
	{
		try
		{
			string current = loliScriptEditor.Text;
			if (_isInLoliCodeMode)
			{
				// If the user did NOT edit anything in LC mode, restore the original LS.
				// This preserves IF/ELSE/ENDIF syntax which can't survive a LS→LC→LS round-trip.
				// If the user DID edit in LC mode, do a normal conversion so their changes are kept.
				bool userEditedLC = _loliCodeAfterConversion == null ||
									current.Trim() != _loliCodeAfterConversion.Trim();

				if (!userEditedLC && _savedLoliScriptBeforeLC != null)
				{
					vm.LS.Script = _savedLoliScriptBeforeLC;
					loliScriptEditor.Text = _savedLoliScriptBeforeLC;
				}
				else
				{
					vm.LS.Script = current;
					var blocks = vm.LS.ToBlocks();

					var sb = new System.Text.StringBuilder();
					foreach (var block in blocks)
					{
						string ls = block.ToLS().TrimEnd();
						if (!string.IsNullOrWhiteSpace(ls))
							sb.AppendLine(ls).AppendLine();
					}
					string loliScript = sb.ToString().TrimEnd();

					vm.LS.Script = loliScript;
					loliScriptEditor.Text = loliScript;
				}
				_savedLoliScriptBeforeLC = null;
				_loliCodeAfterConversion = null;
			}
			_isInLoliCodeMode = false;
			UpdateScriptModeButton();
		}
		catch (Exception ex)
		{
			System.Windows.MessageBox.Show(
				"Error converting to LoliScript:\n" + ex.Message,
				"Conversion Error", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
	}

	// ─── End script-mode toggle ──────────────────────────────────────────────

	private void loliScriptEditor_KeyDown(object sender, KeyEventArgs e)
	{
		if (Keyboard.Modifiers == ModifierKeys.Control)
		{
			if (e.Key == System.Windows.Input.Key.S)
			{
				vm.LS.Script = loliScriptEditor.Text;
				OnSaveConfig();
			}
			else if (e.Key == System.Windows.Input.Key.F)
			{
				Button_Click(null, null);
			}
		}
		if (SB.SBSettings.General.AutoSaveConfigOnStacker && vm.LS.Script != loliScriptEditor.Text)
		{
			vm.LS.Script = loliScriptEditor.Text;
			OnSaveConfig();
		}
		if (SB.SBSettings.General.DisableSyntaxHelper)
		{
			return;
		}
		DocumentLine val = loliScriptEditor.Document.GetLineByOffset(loliScriptEditor.CaretOffset);
		string text = loliScriptEditor.Document.GetText(val.Offset, val.Length);
		while (text.StartsWith(" ") || text.StartsWith("\t"))
		{
			try
			{
				val = val.PreviousLine;
				text = loliScriptEditor.Document.GetText(val.Offset, val.Length);
			}
			catch
			{
				break;
			}
		}
		if (BlockParser.IsBlock(text))
		{
			string blockType = BlockParser.GetBlockType(text);
			Rect rect = loliScriptEditor.TextArea.Caret.CalculateCaretRectangle();
			toolTip.HorizontalOffset = rect.Right;
			toolTip.VerticalOffset = rect.Bottom;
			XmlNode xmlNode = null;
			for (int i = 0; i < syntaxHelperItems.Count; i++)
			{
				if (syntaxHelperItems[i].Attributes["name"].Value.ToUpper() == blockType.ToUpper())
				{
					xmlNode = syntaxHelperItems[i];
					break;
				}
			}
			if (xmlNode != null)
			{
				toolTipEditor.Text = xmlNode.InnerText;
			}
		}
		else
		{
			toolTip.IsOpen = false;
		}
	}

	private void LoliScriptEditor_KeyUp(object sender, KeyEventArgs e)
	{
		try
		{
			if (SB.SBSettings.General.AutoSaveConfigOnStacker && vm.LS.Script != loliScriptEditor.Text)
			{
				vm.LS.Script = loliScriptEditor.Text;
				OnSaveConfig();
			}
		}
		catch
		{
		}
	}

	private void openDocButton_Click(object sender, RoutedEventArgs e)
	{
		new MainDialog(new DialogLSDoc(), "LoliScript Documentation").Show();
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			FindReplaceDialog.ShowForFind(loliScriptEditor);
		}
		catch
		{
		}
	}

	private void loliScriptEditor_KeyUp(object sender, KeyEventArgs e)
	{
	}

	private void debuggerTabControl_KeyDown(object sender, KeyEventArgs e)
	{
		try
		{
			if (e.SystemKey == System.Windows.Input.Key.F10)
			{
				nextStepButton_Click(null, e);
			}
		}
		catch
		{
		}
	}

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		try
		{
			if ((e.OriginalSource as TextBox).Text == string.Empty)
			{
				searchButton_Click(sender, e);
			}
		}
		catch
		{
		}
	}

	private void TextBox_KeyDown(object sender, KeyEventArgs e)
	{
		try
		{
			if (e.Key == System.Windows.Input.Key.Return)
			{
				searchButton_Click(sender, e);
			}
			else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
			{
				if (e.Key == System.Windows.Input.Key.Next)
				{
					nextMatchButton_MouseDown(sender, null);
				}
				else if (e.Key == System.Windows.Input.Key.Prior)
				{
					previousMatchButton_MouseDown(sender, null);
				}
			}
		}
		catch
		{
		}
	}

	private void MenuItem_Click(object sender, RoutedEventArgs e)
	{
		ClearDebuggerLog(sender, e);
	}

	private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
	{
	}

	private void SetBrowserStatus(string status)
	{
		base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate { browserStatus.Text = status; });
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
	}

	private void MenuItem_Click_1(object sender, RoutedEventArgs e)
	{
		try
		{
			NotepadExtensions.ShowText(logRTB.Text, "Log");
		}
		catch (Exception ex)
		{
			SB.Logger.LogError(Components.Stacker, ex.Message, prompt: true);
		}
	}

	private void loliScriptEditor_TextChanged(object sender, EventArgs e)
	{
		AutoSaveConfig();
		_errorTimer?.Stop();
		_errorTimer?.Start();
	}

	private void ErrorTimer_Tick(object sender, EventArgs e)
	{
		_errorTimer.Stop();
		ValidateLoliScript();
	}

	private void ValidateLoliScript()
	{
		// In LoliCode mode the editor contains C# control flow, not LoliScript keywords.
		// Running LS validation on it would always report "clean" (no keywords → no depth errors),
		// giving a false green border. Skip entirely and reset the border.
		if (_isInLoliCodeMode) { ResetEditorBorder(); return; }
		try
		{
			string script = loliScriptEditor.Text;
			if (string.IsNullOrWhiteSpace(script))
			{
				ResetEditorBorder();
				return;
			}
			int ifDepth = 0, whileDepth = 0, tryDepth = 0, forDepth = 0;
			string errorMsg = null;
			foreach (string rawLine in script.Split('\n'))
			{
				string line = rawLine.Trim();
				if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("#"))
					continue;
				if (line.StartsWith("IF ") || line == "IF") ifDepth++;
				else if (line == "ENDIF") { ifDepth--; if (ifDepth < 0) { errorMsg = "ENDIF without matching IF"; break; } }
				else if (line.StartsWith("WHILE ") || line == "WHILE") whileDepth++;
				else if (line == "ENDWHILE") { whileDepth--; if (whileDepth < 0) { errorMsg = "ENDWHILE without matching WHILE"; break; } }
				else if (line == "TRY") tryDepth++;
				else if (line == "ENDTRY") { tryDepth--; if (tryDepth < 0) { errorMsg = "ENDTRY without matching TRY"; break; } }
				else if (line.StartsWith("FOREACH ")) forDepth++;
				else if (line == "ENDFOREACH") { forDepth--; if (forDepth < 0) { errorMsg = "ENDFOREACH without matching FOREACH"; break; } }
			}
			if (errorMsg == null)
			{
				if (ifDepth > 0) errorMsg = $"Unclosed IF ({ifDepth} missing ENDIF)";
				else if (whileDepth > 0) errorMsg = $"Unclosed WHILE ({whileDepth} missing ENDWHILE)";
				else if (tryDepth > 0) errorMsg = $"Unclosed TRY ({tryDepth} missing ENDTRY)";
				else if (forDepth > 0) errorMsg = $"Unclosed FOREACH ({forDepth} missing ENDFOREACH)";
			}
			if (errorMsg != null)
			{
				((Control)(object)loliScriptEditor).BorderBrush = new SolidColorBrush(Color.FromRgb(220, 50, 50));
				((Control)(object)loliScriptEditor).BorderThickness = new Thickness(1.5);
				((FrameworkElement)(object)loliScriptEditor).ToolTip = "Syntax error: " + errorMsg;
			}
			else
			{
				((Control)(object)loliScriptEditor).BorderBrush = new SolidColorBrush(Color.FromRgb(50, 200, 80));
				((Control)(object)loliScriptEditor).BorderThickness = new Thickness(1);
				((FrameworkElement)(object)loliScriptEditor).ToolTip = toolTip;
			}
		}
		catch { ResetEditorBorder(); }
	}

	private void ResetEditorBorder()
	{
		try
		{
			((Control)(object)loliScriptEditor).BorderBrush = null;
			((FrameworkElement)(object)loliScriptEditor).ToolTip = toolTip;
		}
		catch { }
	}

	private void Compile_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			stackerTabControl.BlurApply(0.0, 5.0, TimeSpan.FromSeconds(0.1));
			stackerTabControl.IsEnabled = false;
			try
			{
				startCompileTask?.Dispose();
			}
			catch
			{
			}
			startCompileTask = Task.Run((Action)StartCompile).ContinueWith(delegate
			{
				base.Dispatcher.Invoke(delegate
				{
					stackerTabControl.BlurDisable(TimeSpan.FromSeconds(0.1));
					stackerTabControl.IsEnabled = true;
				});
			});
		}
		catch (Exception ex)
		{
			CustomMsgBox.ShowError(ex.Message);
		}
	}

	private void StartCompile()
	{
		try
		{
			ConfigViewModel currentConfig = SB.MainWindow.ConfigsPage.CurrentConfig;
			if (string.IsNullOrWhiteSpace(currentConfig.Config.Script))
			{
				base.Dispatcher.Invoke(delegate
				{
					CustomMsgBox.ShowError("Script is empty!!");
				});
				return;
			}
			if (currentConfig.Config.BlocksAmount == 0)
			{
				base.Dispatcher.Invoke(delegate
				{
					CustomMsgBox.ShowError("Blocks amount is zero!!");
				});
				return;
			}
			ConfigSettings settings = currentConfig.Config.Settings;
			if ((from i in settings.HitInfoFormat.Split('{')
				where i.Contains(".")
				select i).Any((string i) => !i.StartsWith("hit.")))
			{
				base.Dispatcher.Invoke(delegate
				{
					CustomMsgBox.ShowError("Hit information format is invalid");
				});
				return;
			}
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentConfig.FileName);
			string text = "Compiled";
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			if (!Directory.Exists(text + "\\bin"))
			{
				Directory.CreateDirectory(text + "\\bin");
			}
			ScriptCompiler compiler = new ScriptCompiler((ScriptCompiler.CodeProviderLanguage)0)
			{
				Output = text + "\\bin\\" + fileNameWithoutExtension + ".exe",
				Title = settings.Title,
				IconPath = settings.IconPath,
				Message = settings.Message,
				MessageColor = settings.MessageColor.ConvertToString(),
				AuthorColor = settings.AuthorColor.ConvertToString(),
				BotsColor = settings.BotsColor.ConvertToString(),
				CPMColor = settings.CPMColor.ConvertToString(),
				CustomColor = settings.CustomColor.ConvertToString(),
				CustomInputColor = settings.CustomInputColor.ConvertToString(),
				FailsColor = settings.FailsColor.ConvertToString(),
				HitsColor = settings.HitsColor.ConvertToString(),
				OcrRateColor = settings.OcrRateColor.ConvertToString(),
				ProgressColor = settings.ProgressColor.ConvertToString(),
				ProxiesColor = settings.ProxiesColor.ConvertToString(),
				RetriesColor = settings.RetriesColor.ConvertToString(),
				ToCheckColor = settings.ToCheckColor.ConvertToString(),
				WordlistColor = settings.WordlistColor.ConvertToString(),
				SvbConfig = IOManager.SerializeConfig(currentConfig.Config),
				Config = currentConfig.Config,
				HitInformationFormat = settings.HitInfoFormat,
				LicenseSource = (string.IsNullOrWhiteSpace(settings.LicenseSource) ? string.Empty : File.ReadAllText(settings.LicenseSource))
			};
			try
			{
				compiler.AddOption("/optimize");
				compiler.AddReferences(new string[6] { "System.dll", "System.Drawing.dll", "System.Core.dll", "mscorlib.dll", "System.Linq.dll", "System.Collections.dll" });
				compiler.AddReferences((from d in Directory.GetFiles("bin", "*.dll")
					where !d.Contains("MahApps.Metro") && !d.Contains("Xceed.") && !d.Contains("MaterialDesign") && !d.Contains("WPFToolkit") && !d.Contains("ControlzEx.dll") && !d.Contains("ICSharpCode.AvalonEdit") && !d.Contains("CefSharp.Wpf")
					select d).ToArray());
				string[] requiredPlugins2 = settings.RequiredPlugins;
				if (requiredPlugins2 != null && requiredPlugins2.Length != 0 && compiler.Supports(GeneratorSupport.Resources))
				{
					compiler.InjectPluginLoader();
					string[] plugins = Directory.GetFiles(SB.pluginsFolder, "*.dll");
					if (plugins.Length == 0)
					{
						base.Dispatcher.Invoke(delegate
						{
							CustomMsgBox.ShowError("Required plugins not found!");
						});
						return;
					}
					List<string> reqPlugins = new List<string>();
					settings.RequiredPlugins.Distinct().ToList().ForEach(delegate(string requiredPlugins)
					{
						string text2 = SB.PluginNames.ToList()[SB.BlockPlugins.IndexOf(SB.BlockPlugins.First((IBlockPlugin p) => p.Name == requiredPlugins))];
						reqPlugins.Add(text2);
						string[] array = plugins;
						foreach (string text3 in array)
						{
							if (Path.GetFileNameWithoutExtension(text3) == text2)
							{
								reqPlugins.Remove(text2);
								compiler.AddEmbeddedResource(text3);
							}
						}
					});
					if (reqPlugins.Count > 0)
					{
						base.Dispatcher.Invoke(delegate
						{
							CustomMsgBox.ShowError("\"" + string.Join(", ", reqPlugins) + "\" Plugin(s) not found!");
						});
						return;
					}
				}
				(string, bool) result = compiler.GetResult(compiler.Execute());
				if (result.Item2)
				{
					base.Dispatcher.Invoke(delegate
					{
						CustomMsgBox.ShowError(result.Item1);
					});
					return;
				}
				compiler.CopyReferencesAndDependencies();
				compiler.CopySettings();
				compiler.CreateRunner(fileNameWithoutExtension, currentConfig.Config);
				base.Dispatcher.Invoke(delegate
				{
					CustomMsgBox.Show(result.Item1);
				});
			}
			finally
			{
				if (compiler != null)
				{
					((IDisposable)compiler).Dispose();
				}
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			base.Dispatcher.Invoke(delegate
			{
				CustomMsgBox.ShowError(ex3.Message);
			});
		}
	}

	// True while blocks are being loaded into the Stacker (async dispatch in progress).
	// Prevents AutoSave from writing a partial script to disk during block loading.
	private volatile bool _isLoadingBlocks = false;

	public void AutoSaveConfig()
	{
		// Skip saves while blocks are being dispatched to the UI — the list is partial.
		if (_isLoadingBlocks) return;
		// Also skip while the tab control is disabled (Button-path block loading).
		try { if (stackerTabControl?.IsEnabled == false) return; } catch { }
		if (!SB.MainWindow.ConfigsPage.ConfigManagerPage.CheckSaved() && SB.SBSettings.General.AutoSaveConfigOnStacker)
			OnSaveConfig();
	}

	public void StartPeriodicSaveTimer()
	{
		try { if (_periodicSaveTimer != null && !_periodicSaveTimer.IsEnabled) _periodicSaveTimer.Start(); }
		catch { }
	}

	public void StopPeriodicSaveTimer()
	{
		try { _periodicSaveTimer?.Stop(); }
		catch { }
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/configs/stacker.xaml", UriKind.Relative);
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
			((Stacker)target).KeyDown += Page_KeyDown;
			((Stacker)target).Loaded += Page_Loaded;
			break;
		case 2:
			stackerTabControl = (TabControl)target;
			break;
		case 3:
			codeTab = (TabItem)target;
			break;
		case 4:
			((Button)target).Click += Button_Click;
			break;
		case 5:
			((Button)target).Click += Compile_Click;
			break;
		case 6:
			loliScriptEditor = (TextEditor)target;
			((UIElement)(object)loliScriptEditor).LostFocus += loliScriptEditor_LostFocus;
			loliScriptEditor.TextChanged += loliScriptEditor_TextChanged;
			break;
		case 7:
			stackButton = (Button)target;
			stackButton.Click += stackButton_Click;
			break;
		case 8:
			openDocButton = (Button)target;
			openDocButton.Click += openDocButton_Click;
			break;
		case 9:
			stackTab = (TabItem)target;
			break;
		case 10:
			labelTextbox = (TextBox)target;
			labelTextbox.TextChanged += labelTextbox_TextChanged;
			break;
		case 11:
			((Grid)target).MouseDown += AddBlock_MouseDown;
			((Grid)target).MouseEnter += Image_MouseEnter;
			((Grid)target).MouseLeave += Image_MouseLeave;
			break;
		case 12:
			((Grid)target).MouseDown += RemoveBlock_MouseDown;
			((Grid)target).MouseEnter += Image_MouseEnter;
			((Grid)target).MouseLeave += Image_MouseLeave;
			break;
		case 13:
			((Grid)target).MouseDown += DisableBlock_MouseDown;
			((Grid)target).MouseEnter += Image_MouseEnter;
			((Grid)target).MouseLeave += Image_MouseLeave;
			break;
		case 14:
			disOrEnableIcon = (PackIconMaterial)target;
			break;
		case 15:
			((Grid)target).MouseDown += CloneBlock_MouseDown;
			((Grid)target).MouseEnter += Image_MouseEnter;
			((Grid)target).MouseLeave += Image_MouseLeave;
			break;
		case 16:
			((Grid)target).MouseDown += MoveUpBlock_MouseDown;
			((Grid)target).MouseEnter += Image_MouseEnter;
			((Grid)target).MouseLeave += Image_MouseLeave;
			break;
		case 17:
			((Grid)target).MouseDown += MoveDownBlock_MouseDown;
			((Grid)target).MouseEnter += Image_MouseEnter;
			((Grid)target).MouseLeave += Image_MouseLeave;
			break;
		case 18:
			((Grid)target).MouseDown += SaveConfig_MouseDown;
			((Grid)target).MouseEnter += Image_MouseEnter;
			((Grid)target).MouseLeave += Image_MouseLeave;
			break;
		case 19:
			iconSave = (PackIconFontAwesome)target;
			break;
		case 20:
			stackListView = (ListBox)target;
			break;
		case 22:
			blockInfoScrollViewer = (ScrollViewer)target;
			break;
		case 23:
			BlockInfo = (System.Windows.Controls.Frame)target;
			break;
		case 24:
			loliScriptButton = (Button)target;
			loliScriptButton.Click += loliScriptButton_Click;
			break;
		case 25:
			startDebuggerButton = (Button)target;
			startDebuggerButton.Click += startDebuggerButton_Click;
			break;
		case 26:
			startDebuggerButtonIcon = (PackIconMaterial)target;
			break;
		case 27:
			startDebuggerButtonLabel = (TextBlock)target;
			break;
		case 28:
			testDataTypeCombobox = (ComboBox)target;
			testDataTypeCombobox.SelectionChanged += testDataTypeCombobox_SelectionChanged;
			break;
		case 29:
			nextStepButton = (Button)target;
			nextStepButton.Click += nextStepButton_Click;
			break;
		case 30:
			proxyTypeCombobox = (ComboBox)target;
			proxyTypeCombobox.SelectionChanged += proxyTypeCombobox_SelectionChanged;
			break;
		case 31:
			debuggerTabControl = (TabControl)target;
			debuggerTabControl.KeyDown += debuggerTabControl_KeyDown;
			break;
		case 32:
			dataRTB = (RichTextBox)target;
			break;
		case 33:
			logGrid = (Grid)target;
			break;
		case 34:
			logRTB = (TextEditor)target;
			break;
		case 35:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem_Click;
			break;
		case 36:
			((System.Windows.Controls.MenuItem)target).Click += MenuItem_Click_1;
			break;
		case 37:
			((TextBox)target).KeyDown += TextBox_KeyDown;
			((TextBox)target).TextChanged += TextBox_TextChanged;
			break;
		case 38:
			searchButton = (Button)target;
			searchButton.Click += searchButton_Click;
			break;
		case 39:
			previousMatchButton = (Image)target;
			previousMatchButton.MouseDown += previousMatchButton_MouseDown;
			break;
		case 40:
			nextMatchButton = (Image)target;
			nextMatchButton.MouseDown += nextMatchButton_MouseDown;
			break;
		case 41:
			htmlViewTab = (TabItem)target;
			break;
		case 42:
			((Grid)target).MouseDown += Grid_MouseDown;
			break;
		case 43:
			webControl = (WebControl)target;
			break;
		case 44:
			webView = (EO.Wpf.WebView)target;
			((EO.Wpf.WebView)webView).BeforeNavigate += new BeforeNavigateHandler(WebView_BeforeNavigate);
			((EO.Wpf.WebView)webView).LoadCompleted += new LoadCompletedEventHandler(WebView_LoadCompleted);
			break;
		case 45:
			browserStatus = (TextBox)target;
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
		if (connectionId == 21)
		{
			((ToggleButton)target).Click += blockClicked;
		}
	}
}
