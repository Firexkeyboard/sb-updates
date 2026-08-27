using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using RuriLib;
using RuriLib.Functions.Requests;
using System.Linq;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockRequest : Page, IComponentConnector
{
	private BlockRequest vm;

	private Task analyzeTask;

	internal Path analyzeIcon;

	internal ComboBox methodCombobox;

	internal ComboBox protocolVersionComboBox;

	internal ComboBox securityProtocolCombobox;

	internal ComboBox requestTypeCombobox;

	internal TabControl requestTypeTabControl;

	internal TabItem emptyTab;

	internal TabItem basicAuthTab;

	internal TabItem standardTab;

	internal ComboBox contentTypeCombobox;

	internal TabItem multipartTab;

	internal RichTextBox multipartContentsRTB;

	internal TabItem rawTab;

	internal RichTextBox customCookiesRTB;

	internal RichTextBox customHeadersRTB;

	internal Expander expander;

	internal ComboBox responseTypeCombobox;

	internal TabControl responseTypeTabControl;

	internal TabItem emptyTab2;

	internal TabItem fileTab;

	internal TabItem base64Tab;

	private bool _contentLoaded;

	// ── Advanced options controls (built in code, not BAML) ──────────────────
	private ComboBox httpLibraryCombobox;
	private CheckBox ignoreCertCheckBox;
	private CheckBox alwaysSendCheckBox;
	private TextBox codePagesTextBox;
	private TextBox timeoutTextBox;
	private TextBox retryCountTextBox;
	private TextBox retryDelayTextBox;
	private CheckBox saveCookiesCheckBox;
	private CheckBox loadCookiesCheckBox;
	private CheckBox customCipherCheckBox;
	private TextBox cipherSuitesTextBox;
	private ComboBox curlProfileComboBox;
	private Grid curlProfileRow;

	public PageBlockRequest(BlockRequest block)
	{
		InitializeComponent();
		vm = block;
		base.DataContext = vm;
		string[] names = Enum.GetNames(typeof(HttpMethod));
		foreach (string newItem in names)
		{
			methodCombobox.Items.Add(newItem);
		}
		methodCombobox.SelectedIndex = (int)vm.Method;
		names = Enum.GetNames(typeof(RequestType));
		foreach (string newItem2 in names)
		{
			requestTypeCombobox.Items.Add(newItem2);
		}
		requestTypeCombobox.SelectedIndex = (int)vm.RequestType;
		names = Enum.GetNames(typeof(ResponseType));
		foreach (string newItem3 in names)
		{
			responseTypeCombobox.Items.Add(newItem3);
		}
		responseTypeCombobox.SelectedIndex = (int)vm.ResponseType;
		customCookiesRTB.AppendText(vm.GetCustomCookies());
		customHeadersRTB.AppendText(vm.GetCustomHeaders());
		multipartContentsRTB.AppendText(vm.GetMultipartContents());
		CheckBox_Click(null, null);
		foreach (string item in new List<string> { "application/x-www-form-urlencoded", "application/json", "text/plain" })
		{
			contentTypeCombobox.Items.Add(item);
		}
		names = Enum.GetNames(typeof(SecurityProtocol));
		foreach (string newItem4 in names)
		{
			securityProtocolCombobox.Items.Add(newItem4);
		}
		names = vm.ProtocolVersions;
		foreach (string newItem5 in names)
		{
			protocolVersionComboBox.Items.Add(newItem5);
		}
		protocolVersionComboBox.Text = vm.ProtocolVersion.ToString();
		securityProtocolCombobox.SelectedIndex = (int)vm.SecurityProtocol;

		// Defer UI addition until Loaded so the visual/logical tree is fully set up
		this.Loaded += OnPageLoaded;
	}

	private void OnPageLoaded(object sender, RoutedEventArgs e)
	{
		this.Loaded -= OnPageLoaded;
		AppendAdvancedOptions();
	}

	private void methodCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.Method = (HttpMethod)methodCombobox.SelectedIndex;
	}

	private void requestTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.RequestType = (RequestType)requestTypeCombobox.SelectedIndex;
		RequestType requestType = vm.RequestType;
		switch ((int)requestType)
		{
		default:
			requestTypeTabControl.SelectedIndex = 1;
			break;
		case 0:
			requestTypeTabControl.SelectedIndex = 2;
			break;
		case 2:
			requestTypeTabControl.SelectedIndex = 3;
			break;
		case 3:
			requestTypeTabControl.SelectedIndex = 4;
			break;
		}
	}

	private void responseTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.ResponseType = (ResponseType)responseTypeCombobox.SelectedIndex;
		ResponseType responseType = vm.ResponseType;
		if ((int)responseType != 1)
		{
			if ((int)responseType != 2)
			{
				responseTypeTabControl.SelectedIndex = 0;
			}
			else
			{
				responseTypeTabControl.SelectedIndex = 2;
			}
		}
		else
		{
			responseTypeTabControl.SelectedIndex = 1;
		}
	}

	private void customCookiesRTB_LostFocus(object sender, RoutedEventArgs e)
	{
		vm.SetCustomCookies(customCookiesRTB.Lines());
	}

	private void customHeadersRTB_LostFocus(object sender, RoutedEventArgs e)
	{
		vm.SetCustomHeaders(customHeadersRTB.Lines());
	}

	private void multipartContentsRTB_LostFocus(object sender, RoutedEventArgs e)
	{
		vm.SetMultipartContents(multipartContentsRTB.Lines());
	}

	private void securityProtocolCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.SecurityProtocol = (SecurityProtocol)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void AnalyzeLoginPage_Click(object sender, RoutedEventArgs e)
	{
		Transform analyzeRenderTransform = analyzeIcon.RenderTransform;
		Storyboard waitForAnalyze = (Storyboard)FindResource("WaitForAnalyze");
		try
		{
			waitForAnalyze.Begin();
			Tuple<string, string, string> tuple = null;
			try
			{
				analyzeTask?.Dispose();
			}
			catch
			{
			}
			analyzeTask = Task.Run(() => tuple = vm.Analyze()).ContinueWith(delegate
			{
				try
				{
					if (string.IsNullOrWhiteSpace(tuple.Item1) || string.IsNullOrWhiteSpace(tuple.Item2))
					{
						base.Dispatcher.Invoke(delegate
						{
							waitForAnalyze.Stop();
							analyzeIcon.RenderTransform = analyzeRenderTransform;
						});
						SB.Logger.Log("URL or POSTDATA not found!", (LogLevel)2, prompt: true);
					}
					else
					{
						vm.Url = tuple.Item1;
						vm.PostData = tuple.Item2;
						base.Dispatcher.Invoke(delegate
						{
							waitForAnalyze.Stop();
							analyzeIcon.RenderTransform = analyzeRenderTransform;
						});
					}
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					Exception ex3 = ex2;
					waitForAnalyze.Stop();
					base.Dispatcher.Invoke(() => analyzeIcon.RenderTransform = analyzeRenderTransform);
					base.Dispatcher.Invoke(delegate
					{
						SB.Logger.Log(ex3.Message, (LogLevel)2, prompt: true);
					});
				}
			});
		}
		catch (Exception ex4)
		{
			waitForAnalyze.Stop();
			analyzeIcon.RenderTransform = analyzeRenderTransform;
			SB.Logger.Log(ex4.Message, (LogLevel)2, prompt: true);
		}
	}

	private void ProtocolVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		string text = protocolVersionComboBox.SelectedItem.ToString();
		int major = 1;
		int minor = 0;
		try
		{
			major = int.Parse(text.Split('.')[0]);
		}
		catch
		{
		}
		try
		{
			minor = int.Parse(text.Split('.')[1]);
		}
		catch
		{
		}
		vm.ProtocolVersion = new Version(major, minor);
	}

	private void ProtocolVersionComboBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		string text = protocolVersionComboBox.Text;
		int major = 1;
		int minor = 0;
		try
		{
			major = int.Parse(text.Split('.')[0]);
		}
		catch
		{
		}
		try
		{
			minor = int.Parse(text.Split('.')[1]);
		}
		catch
		{
		}
		vm.ProtocolVersion = new Version(major, minor);
	}

	private void CheckBox_Click(object sender, RoutedEventArgs e)
	{
		expander.IsHitTestVisible = vm.UseAkamai;
		if (!vm.UseAkamai)
		{
			expander.IsExpanded = false;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblockrequest.xaml", UriKind.Relative);
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
			((Button)target).Click += AnalyzeLoginPage_Click;
			break;
		case 2:
			analyzeIcon = (Path)target;
			break;
		case 3:
			methodCombobox = (ComboBox)target;
			methodCombobox.SelectionChanged += methodCombobox_SelectionChanged;
			break;
		case 4:
			protocolVersionComboBox = (ComboBox)target;
			protocolVersionComboBox.SelectionChanged += ProtocolVersionComboBox_SelectionChanged;
			protocolVersionComboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(ProtocolVersionComboBox_TextChanged));
			break;
		case 5:
			securityProtocolCombobox = (ComboBox)target;
			securityProtocolCombobox.SelectionChanged += securityProtocolCombobox_SelectionChanged;
			break;
		case 6:
			requestTypeCombobox = (ComboBox)target;
			requestTypeCombobox.SelectionChanged += requestTypeCombobox_SelectionChanged;
			break;
		case 7:
			requestTypeTabControl = (TabControl)target;
			break;
		case 8:
			emptyTab = (TabItem)target;
			break;
		case 9:
			basicAuthTab = (TabItem)target;
			break;
		case 10:
			standardTab = (TabItem)target;
			break;
		case 11:
			contentTypeCombobox = (ComboBox)target;
			break;
		case 12:
			multipartTab = (TabItem)target;
			break;
		case 13:
			multipartContentsRTB = (RichTextBox)target;
			multipartContentsRTB.LostFocus += multipartContentsRTB_LostFocus;
			break;
		case 14:
			rawTab = (TabItem)target;
			break;
		case 15:
			customCookiesRTB = (RichTextBox)target;
			customCookiesRTB.LostFocus += customCookiesRTB_LostFocus;
			break;
		case 16:
			customHeadersRTB = (RichTextBox)target;
			customHeadersRTB.LostFocus += customHeadersRTB_LostFocus;
			break;
		case 17:
			((CheckBox)target).Click += CheckBox_Click;
			break;
		case 18:
			expander = (Expander)target;
			break;
		case 19:
			responseTypeCombobox = (ComboBox)target;
			responseTypeCombobox.SelectionChanged += responseTypeCombobox_SelectionChanged;
			break;
		case 20:
			responseTypeTabControl = (TabControl)target;
			break;
		case 21:
			emptyTab2 = (TabItem)target;
			break;
		case 22:
			fileTab = (TabItem)target;
			break;
		case 23:
			base64Tab = (TabItem)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	// ── Advanced options: built programmatically since source XAML is unavailable ──

	private void AppendAdvancedOptions()
	{
		try
		{
			Panel mainPanel = FindAncestorPanel(responseTypeTabControl);

			if (mainPanel == null)
			{
				var existing = this.Content as UIElement;
				var wrapper = new StackPanel();
				this.Content = null;
				this.Content = wrapper;
				if (existing != null) wrapper.Children.Add(existing);
				mainPanel = wrapper;
			}

			mainPanel.Children.Add(BuildAdvancedSection());
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[PageBlockRequest] AppendAdvancedOptions failed: {ex}");
		}
	}

	private static Panel FindAncestorPanel(DependencyObject child)
	{
		DependencyObject current = LogicalTreeHelper.GetParent(child);
		while (current != null)
		{
			if (current is StackPanel sp) return sp;
			if (current is WrapPanel  wp) return wp;
			if (current is Page)          return null;
			current = LogicalTreeHelper.GetParent(current);
		}
		return null;
	}

	private UIElement BuildAdvancedSection()
	{
		var dark    = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
		var fg      = new SolidColorBrush(Colors.WhiteSmoke);
		var fgDim   = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
		var accent  = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00));
		var inputBg = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D));
		var border  = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
		var sepLine = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));

		// Helper: label TextBlock
		TextBlock Lbl(string text) => new TextBlock
		{
			Text = text, Foreground = fg,
			Margin = new Thickness(0, 0, 8, 0),
			VerticalAlignment = VerticalAlignment.Center
		};

		// Helper: two-column label + control row
		Grid Row(string labelText, UIElement ctrl, int labelW = 130)
		{
			var g = new Grid { Margin = new Thickness(0, 3, 0, 3) };
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelW) });
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			var lbl = Lbl(labelText);
			Grid.SetColumn(lbl, 0);
			Grid.SetColumn(ctrl, 1);
			g.Children.Add(lbl);
			g.Children.Add(ctrl);
			return g;
		}

		// Helper: section separator with title
		UIElement Sep(string title)
		{
			var g = new Grid { Margin = new Thickness(0, 10, 0, 4) };
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			var lbl = new TextBlock
			{
				Text = title, Foreground = fgDim, FontSize = 10,
				Margin = new Thickness(0, 0, 6, 0),
				VerticalAlignment = VerticalAlignment.Center
			};
			var line = new Rectangle
			{
				Height = 1, Fill = sepLine,
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(lbl, 0);
			Grid.SetColumn(line, 1);
			g.Children.Add(lbl);
			g.Children.Add(line);
			return g;
		}

		// Helper: numeric-only TextBox
		TextBox NumBox(int initial, int width = 75, string tooltip = null)
		{
			var tb = new TextBox
			{
				Background = inputBg, Foreground = fg, BorderBrush = border,
				Text = initial.ToString(), Width = width,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(0, 0, 6, 0),
				ToolTip = tooltip
			};
			tb.PreviewTextInput += (s, e) =>
			{
				foreach (char c in e.Text)
					if (!char.IsDigit(c)) { e.Handled = true; return; }
			};
			return tb;
		}

		// ── Http Library ──────────────────────────────────────────────────────
		httpLibraryCombobox = new ComboBox
		{
			Background = inputBg, Foreground = fg, BorderBrush = border,
			Width = 160, HorizontalAlignment = HorizontalAlignment.Left
		};
		foreach (string name in Enum.GetNames(typeof(HttpLibrary)))
			httpLibraryCombobox.Items.Add(name);
		httpLibraryCombobox.SelectedIndex = (int)vm.HttpLibrary;
		httpLibraryCombobox.SelectionChanged += (s, e) =>
		{
			vm.HttpLibrary = (HttpLibrary)httpLibraryCombobox.SelectedIndex;
			if (curlProfileRow != null)
				curlProfileRow.Visibility = vm.HttpLibrary == HttpLibrary.CurlImpersonate
					? Visibility.Visible : Visibility.Collapsed;
		};

		// ── Curl Impersonate Browser Profile ─────────────────────────────────
		curlProfileComboBox = new ComboBox
		{
			Background = inputBg, Foreground = fg, BorderBrush = border,
			Width = 200, HorizontalAlignment = HorizontalAlignment.Left,
			ToolTip = "Browser TLS fingerprint to impersonate (cipher suite order)"
		};
		foreach (string name in Enum.GetNames(typeof(CurlImpersonateBrowserProfile)))
			curlProfileComboBox.Items.Add(name);
		curlProfileComboBox.SelectedIndex = (int)vm.CurlImpersonateProfile;
		curlProfileComboBox.SelectionChanged += (s, e) =>
			vm.CurlImpersonateProfile = (CurlImpersonateBrowserProfile)curlProfileComboBox.SelectedIndex;

		// ── Ignore Certificate Validation ─────────────────────────────────────
		ignoreCertCheckBox = new CheckBox
		{
			Content = "Ignore Certificate Validation",
			Foreground = fg, Margin = new Thickness(0, 3, 0, 3),
			IsChecked = vm.IgnoreCertificateValidation
		};
		ignoreCertCheckBox.Checked   += (s, e) => vm.IgnoreCertificateValidation = true;
		ignoreCertCheckBox.Unchecked += (s, e) => vm.IgnoreCertificateValidation = false;

		// ── Always Send Content ───────────────────────────────────────────────
		alwaysSendCheckBox = new CheckBox
		{
			Content = "Always Send Content (even on GET/DELETE)",
			Foreground = fg, Margin = new Thickness(0, 3, 0, 3),
			IsChecked = vm.AlwaysSendContent
		};
		alwaysSendCheckBox.Checked   += (s, e) => vm.AlwaysSendContent = true;
		alwaysSendCheckBox.Unchecked += (s, e) => vm.AlwaysSendContent = false;

		// ── Response Encoding ─────────────────────────────────────────────────
		codePagesTextBox = new TextBox
		{
			Background = inputBg, Foreground = fg, BorderBrush = border,
			Text = vm.CodePagesEncoding, Width = 160,
			HorizontalAlignment = HorizontalAlignment.Left,
			ToolTip = "Leave empty for UTF-8. Examples: windows-1252, iso-8859-1, gb2312"
		};
		codePagesTextBox.LostFocus += (s, e) => vm.CodePagesEncoding = codePagesTextBox.Text.Trim();

		// ── Timing ────────────────────────────────────────────────────────────
		timeoutTextBox = NumBox(vm.RequestTimeoutMs, 85, "0 = use global setting from config");
		timeoutTextBox.LostFocus += (s, e) =>
		{
			if (int.TryParse(timeoutTextBox.Text, out int v))
				vm.RequestTimeoutMs = Math.Max(0, v);
		};

		retryCountTextBox = NumBox(vm.RetryCount, 50);
		retryCountTextBox.LostFocus += (s, e) =>
		{
			if (int.TryParse(retryCountTextBox.Text, out int v))
				vm.RetryCount = Math.Max(0, Math.Min(10, v));
		};

		retryDelayTextBox = NumBox(vm.RetryDelayMs, 75);
		retryDelayTextBox.LostFocus += (s, e) =>
		{
			if (int.TryParse(retryDelayTextBox.Text, out int v))
				vm.RetryDelayMs = Math.Max(0, v);
		};

		// Retry inline row: [count] retries, delay (ms) [delay]
		var retryPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center
		};
		retryPanel.Children.Add(retryCountTextBox);
		retryPanel.Children.Add(new TextBlock
		{
			Text = "retries,  delay (ms)",
			Foreground = fgDim,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0, 0, 6, 0)
		});
		retryPanel.Children.Add(retryDelayTextBox);

		// ── Cookie Handling ───────────────────────────────────────────────────
		saveCookiesCheckBox = new CheckBox
		{
			Content = "Save Response Cookies to Jar",
			Foreground = fg, Margin = new Thickness(0, 3, 0, 3),
			IsChecked = vm.SaveResponseCookies,
			ToolTip = "Uncheck to discard Set-Cookie headers from response (jar stays unchanged)"
		};
		saveCookiesCheckBox.Checked   += (s, e) => vm.SaveResponseCookies = true;
		saveCookiesCheckBox.Unchecked += (s, e) => vm.SaveResponseCookies = false;

		loadCookiesCheckBox = new CheckBox
		{
			Content = "Send Jar Cookies with Request",
			Foreground = fg, Margin = new Thickness(0, 3, 0, 3),
			IsChecked = vm.LoadRequestCookies,
			ToolTip = "Uncheck to only send cookies set in 'Custom Cookies' for this block"
		};
		loadCookiesCheckBox.Checked   += (s, e) => vm.LoadRequestCookies = true;
		loadCookiesCheckBox.Unchecked += (s, e) => vm.LoadRequestCookies = false;

		// ── Custom Cipher Suites ──────────────────────────────────────────────
		customCipherCheckBox = new CheckBox
		{
			Content = "Use Custom Cipher Suites",
			Foreground = fg, Margin = new Thickness(0, 3, 0, 3),
			IsChecked = vm.UseCustomCipherSuites,
			ToolTip = "Only effective on Linux/OpenSSL. Windows Schannel ignores this setting."
		};
		cipherSuitesTextBox = new TextBox
		{
			Background = inputBg, BorderBrush = border,
			Text = vm.CustomCipherSuites,
			AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap,
			Height = 90, Margin = new Thickness(0, 2, 0, 2),
			FontFamily = new FontFamily("Consolas"), FontSize = 10,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			IsEnabled = vm.UseCustomCipherSuites,
			Foreground = vm.UseCustomCipherSuites ? fg : fgDim
		};
		customCipherCheckBox.Checked += (s, e) =>
		{
			vm.UseCustomCipherSuites = true;
			cipherSuitesTextBox.IsEnabled = true;
			cipherSuitesTextBox.Foreground = fg;
		};
		customCipherCheckBox.Unchecked += (s, e) =>
		{
			vm.UseCustomCipherSuites = false;
			cipherSuitesTextBox.IsEnabled = false;
			cipherSuitesTextBox.Foreground = fgDim;
		};
		cipherSuitesTextBox.LostFocus += (s, e) => vm.CustomCipherSuites = cipherSuitesTextBox.Text;

		// ── Assemble ──────────────────────────────────────────────────────────
		curlProfileRow = Row("Browser Profile", curlProfileComboBox);
		curlProfileRow.Visibility = vm.HttpLibrary == HttpLibrary.CurlImpersonate
			? Visibility.Visible : Visibility.Collapsed;

		var inner = new StackPanel { Margin = new Thickness(2, 0, 2, 0) };

		inner.Children.Add(Row("Http Library", httpLibraryCombobox));
		inner.Children.Add(curlProfileRow);
		inner.Children.Add(ignoreCertCheckBox);
		inner.Children.Add(alwaysSendCheckBox);
		inner.Children.Add(Row("Response Encoding", codePagesTextBox));

		inner.Children.Add(Sep("Timing"));
		inner.Children.Add(Row("Timeout (ms)", timeoutTextBox));
		inner.Children.Add(Row("Retry", retryPanel));

		inner.Children.Add(Sep("Cookie Handling"));
		inner.Children.Add(saveCookiesCheckBox);
		inner.Children.Add(loadCookiesCheckBox);

		inner.Children.Add(Sep("Cipher Suites"));
		inner.Children.Add(customCipherCheckBox);
		inner.Children.Add(cipherSuitesTextBox);

		var section = new GroupBox
		{
			Header = new TextBlock
			{
				Text = "Advanced Request Options",
				Foreground = accent,
				FontWeight = FontWeights.Bold
			},
			BorderBrush = border,
			Background = dark,
			Margin = new Thickness(5, 8, 5, 5),
			Padding = new Thickness(8, 4, 8, 8)
		};
		section.Content = inner;
		return section;
	}
}
