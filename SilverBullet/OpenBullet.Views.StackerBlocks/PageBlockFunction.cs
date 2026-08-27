using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using RuriLib;
using RuriLib.Functions.Crypto;
using RuriLib.Functions.UserAgent;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockFunction : Page, IComponentConnector
{
	private BlockFunction vm;

	public Dictionary<string, string> infoDic = new Dictionary<string, string>
	{
		{ "Constant", "This will just return anything written in the input string and store it in a variable, after possibly replacing all the input variables.\nUse this to chain constants and variables together." },
		{ "Base64Encode", "Encodes a string to Base64" },
		{ "Base64Decode", "Decodes a Base64 string to text" },
		{ "Hash", "The input string will be hashed with the selected Function. Remember you can chain variables if you need a salt." },
		{ "HMAC", "The input string will be hashed with the selected Hashtype with the key you input." },
		{ "Translate", "Format like headers (this: that), one per line." },
		{ "DateToUnixTime", "This turns date to Unixtime. ex(input = 2020-01-01:01-01-01 to output = 1577840461" },
		{ "Length", "This will count the Length of the input Ex(Input = apple Output = 5)" },
		{ "ToLowercase", "Turns input to Lowercase Ex(A to a)" },
		{ "ToUppercase", "Turns input to Uppercase Ex(a to A)" },
		{ "Replace", "Replaces the Input from (Replace:) with the Input from (with:) from the Input string" },
		{ "RegexMatch", "Matches the input with the given Regex (Match:)" },
		{ "URLEncode", "URLEncodes the input. Ex(https://example.com to https%3A%2F%2Fexample.com)" },
		{ "URLDecode", "URLDecodes the input. Ex(https%3A%2F%2Fexample.com to https://example.com)" },
		{ "Unescape", "Unescapes the input." },
		{ "HTMLEntityEncode", "HTMLEntity Encodes the input. Ex(© to &#169;)" },
		{ "HTMLEntityDecode", "HTMLEntity Decodes the input. Ex(&#169; to ©)" },
		{ "UnixTimeToDate", "This turns Unixtime to date. ex(input = 1577840461 to output = 2020-01-01:01-01-01" },
		{ "CurrentUnixTime", "Returns current Unix time. Input empty = seconds (10 digits, ex: 1577840461). Input 'ms' or '1000' = milliseconds (13 digits, ex: 1577840461000). Input any number N = seconds * N." },
		{ "UnixTimeToISO8601", "This Returns theh Current Unixtime. ex(1577840461 to 2020-01-01T01:01:01.000Z)" },
		{ "RandomNum", "Generates a random number based on ranges (min max) given." },
		{ "RandomString", "?l = Lowercase, ?u = Uppercase, ?d = Digit, ?f = Uppercase + Lowercase, ?s = Symbol, ?h = Hex (Lowercase), ?m = Upper + Digits,?n = Lower + Digits ?i = Lower + Upper + Digits, ?a = Any" },
		{ "EvaluateMathString", "Evaluating string '3 * 5 + Pow(2,3)' yield int 23" },
		{ "Ceil", "This Function rounds up to the input to the next full integer. Ex(input = 2.9 Output = 3" },
		{ "Floor", "This Function gets rid of the numbers after the decimal. Ex(input = 2.9 Output = 2" },
		{ "Round", "This Function round input to the nearest integer. Ex(input = 2.5 Output = 3" },
		{ "Compute", "Calculates the value of a math expression, for example (6+3)*5 will return 45." },
		{ "ClearCookies", "Removes/Clears all the Cookies." },
		{ "CountOccurrences", "Counts the number of times that (Find:) input occurs in the input." },
		{ "RSAEncrypt", "Encrypts data with RSA. All parameters must be provided as base64 strings" },
		{ "RSAPKCS1PAD", "Encrypts data with RSA Pkcs1Pad2. Modulus and exponent must be provided as HEX strings." },
		{ "Delay", "Write the amount of MILLISECONDS you want to wait in the input field" },
		{ "CharAt", "Returns the character at the specified index of the string in the input field" },
		{ "Substring", "Returns a new string that is a substring of Input String. The substring begins at the specified Start Index and Length." },
		{ "Reversestring", "Reverses the the characters if the Input String. Ex(input = abc output = cba" },
		{ "Trim", "Removes whitespace from both ends of a Input string." },
		{ "GetRandomUA", "Generates a random User Agent." },
		{ "AESEncrypt", "Encrypts data with AES. All parameters must be provided as base64 strings. Uses SHA-256 to get a 256 bit key" },
		{ "AESDecrypt", "Decrypts data with AES. All parameters must be provided as base64 strings. Uses SHA-256 to get a 256 bit key" },
		{ "PBKDF2PKCS5", "Generates a key based on a password. The salt, if provided, must be a base64 string" },
		{ "Ntlm", "Generates NTLM hash" },
		{ "Scrypt", "Scrypt encoder" },
		{ "GenerateGUID", "Generates a random GUID. Optionally in uppercase (e.g. 550E8400-E29B-41D4-A716-446655440000)." },
		{ "MergeByteArrays", "Merges two Base64-encoded byte arrays into one. First = Input string (Base64), Second = Second Input (Base64). Output is Base64." },
		{ "RegexReplace", "Replaces all regex matches in the input. Pattern = regex pattern, Replacement = replacement string." },
		{ "XOR", "XOR byte arrays. Input = hex/base64 bytes, Key = hex/base64 key. Output = lowercase hex." },
		{ "XORStrings", "XOR strings. Input = plain string, Key = plain string key. Output = XOR'd string." },
		{ "RSADecrypt", "Decrypts RSA-encrypted data. Modulus and D (private exponent) as HEX. Input as Base64." },
		{ "JWTEncode", "Encodes a JWT token. Input = JSON payload, Secret = signing secret, Algorithm = HS256/HS384/HS512." },
		{ "MaxFloat", "Returns the larger of two decimal numbers." },
		{ "MinFloat", "Returns the smaller of two decimal numbers." },
		{ "RandomFloat", "Generates a random decimal number between Min and Max." },
		{ "MaxInt", "Returns the larger of two integers." },
		{ "MinInt", "Returns the smaller of two integers." },
		{ "AddKeyValuePair", "Adds or updates a key-value pair in an existing dictionary variable. Input = dict name, Key, Value." },
		{ "GetKey", "Gets a value from a dictionary variable by key. Input = dict name, Key." },
		{ "RemoveByKey", "Removes a key from a dictionary variable. Input = dict name, Key." },
		{ "CreateListOfNumbers", "Creates a list of numbers. Input = start, Count = how many, Step = increment (default 1)." },
		{ "IndexOf", "Returns the index of an item in a list variable. Input = list name, Item = element to find." },
		{ "ListToDict", "Converts a list to a dictionary using alternating elements as key-value pairs. Input = list name." },
		{ "BCryptVerify", "Verifies a plaintext password against a BCrypt hash. Input = plaintext, Hash = the bcrypt hash. Returns True/False." },
		{ "ScryptDeriveKey", "Derives a key using Scrypt KDF. Input = password, Salt = salt string, and cost parameters." },
		{ "AWS4Signature", "Computes an AWS Signature V4. Input = stringToSign, SecretKey, Date (YYYYMMDD), Region, Service. Returns hex signature." },
	};

	internal ComboBox functionTypeCombobox;

	internal TabControl functionTabControl;

	internal TabItem emptyTab;

	internal TabItem hashTab;

	internal ComboBox hashTypeCombobox;

	internal TabItem hmacTab;

	internal ComboBox hmacHashTypeCombobox;

	internal TabItem translateTab;

	internal RichTextBox dictionaryRTB;

	internal TabItem dateToUnixTimeTab;

	internal ComboBox dateToUnixTimeCombobox;

	internal TabItem stringReplaceTab;

	internal TabItem regexMatchTab;

	internal TabItem randomNumberTab;

	internal TabItem countOccurrencesTab;

	internal TabItem rsaEncTab;

	internal TabItem rsaDecTab;

	internal TabItem rsaPkcs1Pad2Tab;

	internal TabItem charAtTab;

	internal TabItem substringTab;

	internal TabItem randomUATab;

	internal ComboBox randomUABrowserCombobox;

	internal TabItem aesTab;

	internal ComboBox aesModeCombobox;

	internal ComboBox aesPaddingCombobox;

	internal TabItem kdfTab;

	internal ComboBox kdfAlgorithmCombobox;

	internal ComboBox encCombobox;

	internal ComboBox encFuncCombobox;

	internal TabItem Scrypt;

	internal ComboBox scryptMethods;

	internal StackPanel stackPanelEncode;

	internal DockPanel dockPanelCompInput;

	internal TabItem Bcrypt;

	internal ComboBox bcryptMethods;

	internal DockPanel dockPanelBcryptSalt;

	internal DockPanel dockPanelWorkFactorInput;

	internal DockPanel dockPanelVerifyInput;

	internal TextBlock functionInfoTextblock;

	internal CheckBox guidUppercaseCheckbox;
	private ComboBox guidVersionComboBox;
	private ComboBox guidFormatComboBox;
	private StackPanel _guidRow;
	private CheckBox _guidToUpperCb;
	private bool _guidControlsInjected;

	private FrameworkElement _mergeSecondInputRow;
	private bool _mergeControlsInjected;

	private bool _contentLoaded;

	private List<string> _allFunctionNames;
	private TextBox _functionSearchBox;
	private FrameworkElement _secondInputRow;
	private TextBlock _secondInputLabel;
	private FrameworkElement _thirdInputRow;
	private TextBlock _thirdInputLabel;
	private FrameworkElement _jwtAlgoRow;
	private bool _extraInputsInjected;

	// Functions that require a second string input (label → field name)
	private static readonly Dictionary<BlockFunction.Function, string> _secondInputLabels = new()
	{
		{ BlockFunction.Function.XOR,            "Key (hex/base64)" },
		{ BlockFunction.Function.XORStrings,     "Key (string)" },
		{ BlockFunction.Function.MaxFloat,        "Second value" },
		{ BlockFunction.Function.MinFloat,        "Second value" },
		{ BlockFunction.Function.MaxInt,          "Second value" },
		{ BlockFunction.Function.MinInt,          "Second value" },
		{ BlockFunction.Function.GetKey,          "Key" },
		{ BlockFunction.Function.RemoveByKey,     "Key" },
		{ BlockFunction.Function.IndexOf,         "Item" },
		{ BlockFunction.Function.AddKeyValuePair, "Key" },
		{ BlockFunction.Function.CreateListOfNumbers, "Count" },
		{ BlockFunction.Function.JWTEncode,       "Secret" },
		{ BlockFunction.Function.BCryptVerify,    "BCrypt Hash" },
		{ BlockFunction.Function.ScryptDeriveKey, "Salt" },
		{ BlockFunction.Function.AWS4Signature,   "Secret Key" },
	};

	// Functions that also require a third string input
	private static readonly Dictionary<BlockFunction.Function, string> _thirdInputLabels = new()
	{
		{ BlockFunction.Function.AddKeyValuePair,    "Value" },
		{ BlockFunction.Function.CreateListOfNumbers, "Step (default 1)" },
		{ BlockFunction.Function.AWS4Signature,       "Date (YYYYMMDD)" },
	};

	public PageBlockFunction(BlockFunction block)
	{
		InitializeComponent();
		vm = block;
		base.DataContext = vm;
		string[] names = Enum.GetNames(typeof(BlockFunction.Function));
		_allFunctionNames = names.ToList();
		foreach (string newItem in names)
		{
			functionTypeCombobox.Items.Add(newItem);
		}
		functionTypeCombobox.SelectedItem = vm.FunctionType.ToString();
		names = Enum.GetNames(typeof(Hash));
		foreach (string newItem2 in names)
		{
			hashTypeCombobox.Items.Add(newItem2);
			hmacHashTypeCombobox.Items.Add(newItem2);
			kdfAlgorithmCombobox.Items.Add(newItem2);
		}
		hashTypeCombobox.SelectedIndex = (int)vm.HashType;
		hmacHashTypeCombobox.SelectedIndex = (int)vm.HashType;
		kdfAlgorithmCombobox.SelectedIndex = (int)vm.KdfAlgorithm;
		names = Enum.GetNames(typeof(UserAgent.Browser));
		foreach (string newItem3 in names)
		{
			randomUABrowserCombobox.Items.Add(newItem3);
		}
		randomUABrowserCombobox.SelectedIndex = (int)vm.UserAgentBrowser;
		names = Enum.GetNames(typeof(CipherMode));
		foreach (string newItem4 in names)
		{
			aesModeCombobox.Items.Add(newItem4);
		}
		aesModeCombobox.SelectedIndex = (int)(vm.AesMode - 1);
		names = Enum.GetNames(typeof(PaddingMode));
		foreach (string newItem5 in names)
		{
			aesPaddingCombobox.Items.Add(newItem5);
		}
		names = Enum.GetNames(typeof(BlockFunction.DateToUnixTimeType));
		foreach (string newItem6 in names)
		{
			dateToUnixTimeCombobox.Items.Add(newItem6);
		}
		encCombobox.Items.Add("utf-8");
		encCombobox.Items.Add("windows-1251");
		encCombobox.Items.Add(1251);
		names = Enum.GetNames(typeof(BlockFunction.EncodingMethods));
		foreach (string newItem7 in names)
		{
			encFuncCombobox.Items.Add(newItem7);
		}
		names = Enum.GetNames(typeof(BlockFunction.ScryptMethods));
		foreach (string text in names)
		{
			if (text == "Encode")
			{
				scryptMethods.Items.Add(text);
				break;
			}
		}
		names = Enum.GetNames(typeof(BlockFunction.BCryptMethods));
		foreach (string newItem8 in names)
		{
			bcryptMethods.Items.Add(newItem8);
		}
		bcryptMethods.SelectedIndex = (int)vm.BCryptMeth;
		aesPaddingCombobox.SelectedIndex = (int)(vm.AesPadding - 1);
		dictionaryRTB.AppendText(vm.GetDictionary());

		// Hidden legacy checkbox — backing vm.GuidUppercase; replaced in UI by _guidToUpperCb
		guidUppercaseCheckbox = new CheckBox { Visibility = Visibility.Collapsed, IsChecked = vm.GuidUppercase };
		guidUppercaseCheckbox.Checked   += (s, e) => vm.GuidUppercase = true;
		guidUppercaseCheckbox.Unchecked += (s, e) => vm.GuidUppercase = false;

		// Version combobox — Visibility managed by _guidRow, not individually
		guidVersionComboBox = new ComboBox { Width = 60, VerticalAlignment = VerticalAlignment.Center };
		foreach (string v in Enum.GetNames(typeof(RuriLib.GuidVersion)))
			guidVersionComboBox.Items.Add(v);
		guidVersionComboBox.SelectedIndex = (int)vm.GuidVer;
		guidVersionComboBox.SelectionChanged += (s, e) =>
		{
			if (guidVersionComboBox.SelectedIndex >= 0)
				vm.GuidVer = (RuriLib.GuidVersion)guidVersionComboBox.SelectedIndex;
		};

		// Format combobox — Visibility managed by _guidRow, not individually
		guidFormatComboBox = new ComboBox { Width = 55, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
		foreach (string f in Enum.GetNames(typeof(RuriLib.GuidFormat)))
			guidFormatComboBox.Items.Add(f);
		guidFormatComboBox.SelectedIndex = (int)vm.GuidFmt;
		guidFormatComboBox.SelectionChanged += (s, e) =>
		{
			if (guidFormatComboBox.SelectedIndex >= 0)
				vm.GuidFmt = (RuriLib.GuidFormat)guidFormatComboBox.SelectedIndex;
		};
	}

	private void functionTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (functionTypeCombobox.SelectedItem is string fname &&
		    Enum.TryParse<BlockFunction.Function>(fname, out var parsed))
			vm.FunctionType = parsed;
		bool isGuidNow  = vm.FunctionType == BlockFunction.Function.GenerateGUID;
		bool isMergeNow = vm.FunctionType == BlockFunction.Function.MergeByteArrays;
		UpdateExtraInputVisibility();
		if (_guidRow != null)
			_guidRow.Visibility = isGuidNow ? Visibility.Visible : Visibility.Collapsed;
		if (_guidToUpperCb != null)
			_guidToUpperCb.Visibility = isGuidNow ? Visibility.Visible : Visibility.Collapsed;
		if (_mergeSecondInputRow != null)
			_mergeSecondInputRow.Visibility = isMergeNow ? Visibility.Visible : Visibility.Collapsed;
		BlockFunction.Function functionType;
		try
		{
			TextBlock textBlock = functionInfoTextblock;
			Dictionary<string, string> dictionary = infoDic;
			functionType = vm.FunctionType;
			textBlock.Text = dictionary[functionType.ToString()];
		}
		catch
		{
			functionInfoTextblock.Text = "No additional information available for this BlockFunction.Function";
		}
		if (isGuidNow) UpdateGuidInfoText();
		functionType = vm.FunctionType;
		if ((int)functionType <= 24)
		{
			switch ((int)functionType - 3)
			{
			case 0:
				functionTabControl.SelectedIndex = 1;
				return;
			case 1:
				functionTabControl.SelectedIndex = 2;
				return;
			case 2:
				functionTabControl.SelectedIndex = 3;
				return;
			}
			if ((int)functionType == 12)
			{
				goto IL_0161;
			}
			if ((int)functionType == 24)
			{
				functionTabControl.SelectedIndex = 5;
				return;
			}
		}
		else if ((int)functionType <= 57)
		{
			if ((int)functionType == 25)
			{
				functionTabControl.SelectedIndex = 6;
				return;
			}
			switch ((int)functionType - 31)
			{
			case 1:
				goto IL_0161;
			case 4:
				functionTabControl.SelectedIndex = 7;
				return;
			case 12:
				functionTabControl.SelectedIndex = 8;
				return;
			case 14:
				functionTabControl.SelectedIndex = 9;
				return;
			case 15:
				functionTabControl.SelectedIndex = 11;
				return;
			case 17:
				functionTabControl.SelectedIndex = 12;
				return;
			case 20:
				functionTabControl.SelectedIndex = 13;
				return;
			case 23:
				functionTabControl.SelectedIndex = 14;
				return;
			case 24:
			case 25:
				functionTabControl.SelectedIndex = 15;
				return;
			case 26:
				functionTabControl.SelectedIndex = 16;
				return;
			case 19:
				functionTabControl.SelectedIndex = 17;
				return;
			case 18:
				functionTabControl.SelectedIndex = 18;
				return;
			case 0:
				functionTabControl.SelectedIndex = 19;
				return;
			}
		}
		else
		{
			if ((int)functionType == 63)
			{
				functionTabControl.SelectedIndex = 20;
				return;
			}
			if ((int)functionType == 64)
			{
				functionTabControl.SelectedIndex = 21;
				return;
			}
		}
		functionTabControl.SelectedIndex = 0;
		return;
		IL_0161:
		functionTabControl.SelectedIndex = 4;
	}

	private void UpdateGuidInfoText()
	{
		if (functionInfoTextblock == null || vm.FunctionType != BlockFunction.Function.GenerateGUID) return;

		int v = guidVersionComboBox?.SelectedIndex ?? 0;
		int f = guidFormatComboBox?.SelectedIndex ?? 0;

		string vDesc = v == 1
			? "V7 — Time-sortable (Unix ms timestamp prefix)"
			: "V4 — Standard random GUID";

		string fDesc = f switch
		{
			1 => "N — No hyphens",
			2 => "B — Braces  { }",
			3 => "P — Parentheses  ( )",
			_ => "D — With hyphens"
		};

		functionInfoTextblock.Text = "Generates a random GUID.\n" + vDesc + "\n" + fDesc;
	}

	private void dictionaryRTB_TextChanged(object sender, TextChangedEventArgs e)
	{
		vm.SetDictionary(dictionaryRTB.Lines());
	}

	private void hashTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.HashType = (Hash)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void hmacHashTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.HashType = (Hash)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void aesModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.AesMode = (CipherMode)(((ComboBox)e.OriginalSource).SelectedIndex + 1);
	}

	private void aesPaddingCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.AesPadding = (PaddingMode)(((ComboBox)e.OriginalSource).SelectedIndex + 1);
	}

	private void kdfAlgorithmCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.KdfAlgorithm = (Hash)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void randomUABrowserCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		vm.UserAgentBrowser = (UserAgent.Browser)((ComboBox)e.OriginalSource).SelectedIndex;
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		try { SetItemToComboBox(); } catch { }
		try { InjectGuidControlsNextToIsCapture(); } catch { }
		try { UpdateGuidInfoText(); } catch { }
		try { InjectMergeByteArrayControls(); } catch { }
		try { InjectFunctionSearchBox(); } catch { }
		try { InjectExtraInputControls(); } catch { }
	}

	private void InjectFunctionSearchBox()
	{
		if (_functionSearchBox != null) return;
		var funcContainer = functionTypeCombobox.Parent as FrameworkElement;
		var funcParent    = funcContainer?.Parent as Panel;
		if (funcParent == null) return;

		_functionSearchBox = new TextBox
		{
			Text              = "Search functions...",
			Foreground        = new SolidColorBrush(Colors.Gray),
			Margin            = new Thickness(funcContainer?.Margin.Left ?? 0, 0, 0, 4),
			VerticalAlignment = VerticalAlignment.Center,
		};

		_functionSearchBox.GotFocus += (s, e) =>
		{
			if (_functionSearchBox.Text == "Search functions...")
			{
				_functionSearchBox.Text       = "";
				_functionSearchBox.Foreground = new SolidColorBrush(Colors.White);
			}
		};
		_functionSearchBox.LostFocus += (s, e) =>
		{
			if (string.IsNullOrWhiteSpace(_functionSearchBox.Text))
			{
				_functionSearchBox.Text       = "Search functions...";
				_functionSearchBox.Foreground = new SolidColorBrush(Colors.Gray);
			}
		};
		_functionSearchBox.TextChanged += (s, e) =>
		{
			string q = _functionSearchBox.Text;
			FilterFunctions(q == "Search functions..." ? "" : q);
		};

		int idx = funcParent.Children.IndexOf(funcContainer ?? functionTypeCombobox);
		if (funcParent is DockPanel dp)
			DockPanel.SetDock(_functionSearchBox, DockPanel.GetDock(funcContainer ?? functionTypeCombobox));
		funcParent.Children.Insert(Math.Max(0, idx), _functionSearchBox);
	}

	private void FilterFunctions(string query)
	{
		if (_allFunctionNames == null) return;
		string current = vm.FunctionType.ToString();
		functionTypeCombobox.Items.Clear();
		var filtered = string.IsNullOrWhiteSpace(query)
			? _allFunctionNames
			: _allFunctionNames.Where(n => n.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		foreach (string n in filtered) functionTypeCombobox.Items.Add(n);
		if (functionTypeCombobox.Items.Contains(current))
			functionTypeCombobox.SelectedItem = current;
		else if (functionTypeCombobox.Items.Count > 0)
			functionTypeCombobox.SelectedIndex = 0;
	}

	private void InjectExtraInputControls()
	{
		if (_extraInputsInjected) return;
		var funcContainer = functionTypeCombobox.Parent as FrameworkElement;
		var funcParent    = funcContainer?.Parent as Panel;
		if (funcParent == null) return;
		_extraInputsInjected = true;

		var white = new SolidColorBrush(Colors.White);
		FrameworkElement MakeInputRow(string defaultLabel, out TextBlock labelOut, out TextBox boxOut)
		{
			labelOut = new TextBlock { Text = defaultLabel, Foreground = white, MinWidth = 80,
			                           VerticalAlignment = VerticalAlignment.Center };
			boxOut = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch,
			                       VerticalAlignment   = VerticalAlignment.Center,
			                       Padding = new Thickness(4, 2, 4, 2) };
			var row = new DockPanel { Margin = new Thickness(funcContainer?.Margin.Left ?? 0, 2, 0, 2) };
			DockPanel.SetDock(labelOut, Dock.Left);
			row.Children.Add(labelOut);
			row.Children.Add(boxOut);
			return row;
		}

		TextBox secondBox, thirdBox, jwtAlgoBox;
		_secondInputRow = MakeInputRow("Second:", out _secondInputLabel, out secondBox);
		secondBox.Text  = vm.SecondInput ?? "";
		secondBox.TextChanged += (s, e) => vm.SecondInput = secondBox.Text;

		_thirdInputRow = MakeInputRow("Third:", out _thirdInputLabel, out thirdBox);
		thirdBox.Text  = vm.ThirdInput ?? "";
		thirdBox.TextChanged += (s, e) => vm.ThirdInput = thirdBox.Text;

		// JWT algorithm row (special combo)
		var jwtAlgoLabel = new TextBlock { Text = "Algorithm:", Foreground = white, MinWidth = 80,
		                                    VerticalAlignment = VerticalAlignment.Center };
		var jwtAlgoCb = new ComboBox { MinWidth = 80, VerticalAlignment = VerticalAlignment.Center,
		                                Margin = new Thickness(0) };
		foreach (string alg in new[] { "HS256", "HS384", "HS512" }) jwtAlgoCb.Items.Add(alg);
		jwtAlgoCb.SelectedItem = string.IsNullOrEmpty(vm.JwtAlgorithm) ? "HS256" : vm.JwtAlgorithm;
		jwtAlgoCb.SelectionChanged += (s, e) => { if (jwtAlgoCb.SelectedItem is string a) vm.JwtAlgorithm = a; };
		var jwtRow = new DockPanel { Margin = new Thickness(funcContainer?.Margin.Left ?? 0, 2, 0, 2) };
		DockPanel.SetDock(jwtAlgoLabel, Dock.Left);
		jwtRow.Children.Add(jwtAlgoLabel);
		jwtRow.Children.Add(jwtAlgoCb);
		_jwtAlgoRow = jwtRow;

		// RSADecrypt rows (N and D)
		var rsaDecNLabel = new TextBlock { Text = "Modulus (N):", Foreground = white, MinWidth = 80, VerticalAlignment = VerticalAlignment.Center };
		var rsaDecNBox   = new TextBox   { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(4,2,4,2) };
		rsaDecNBox.Text  = vm.RsaN ?? "";
		rsaDecNBox.TextChanged += (s, e) => vm.RsaN = rsaDecNBox.Text;
		var rsaDecNRow = new DockPanel { Margin = new Thickness(funcContainer?.Margin.Left ?? 0, 2, 0, 2) };
		DockPanel.SetDock(rsaDecNLabel, Dock.Left);
		rsaDecNRow.Children.Add(rsaDecNLabel); rsaDecNRow.Children.Add(rsaDecNBox);

		var rsaDecDLabel = new TextBlock { Text = "Private D:", Foreground = white, MinWidth = 80, VerticalAlignment = VerticalAlignment.Center };
		var rsaDecDBox   = new TextBox   { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(4,2,4,2) };
		rsaDecDBox.Text  = vm.RsaD ?? "";
		rsaDecDBox.TextChanged += (s, e) => vm.RsaD = rsaDecDBox.Text;
		var rsaDecDRow = new DockPanel { Margin = new Thickness(funcContainer?.Margin.Left ?? 0, 2, 0, 2) };
		DockPanel.SetDock(rsaDecDLabel, Dock.Left);
		rsaDecDRow.Children.Add(rsaDecDLabel); rsaDecDRow.Children.Add(rsaDecDBox);

		// RegexReplace rows (pattern + replacement)
		var regexPatLabel = new TextBlock { Text = "Pattern:", Foreground = white, MinWidth = 80, VerticalAlignment = VerticalAlignment.Center };
		var regexPatBox   = new TextBox   { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(4,2,4,2) };
		regexPatBox.Text  = vm.RegexMatch ?? "";
		regexPatBox.TextChanged += (s, e) => vm.RegexMatch = regexPatBox.Text;
		var regexPatRow = new DockPanel { Margin = new Thickness(funcContainer?.Margin.Left ?? 0, 2, 0, 2) };
		DockPanel.SetDock(regexPatLabel, Dock.Left);
		regexPatRow.Children.Add(regexPatLabel); regexPatRow.Children.Add(regexPatBox);

		var regexReplLabel = new TextBlock { Text = "Replacement:", Foreground = white, MinWidth = 80, VerticalAlignment = VerticalAlignment.Center };
		var regexReplBox   = new TextBox   { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(4,2,4,2) };
		regexReplBox.Text  = vm.ReplaceWith ?? "";
		regexReplBox.TextChanged += (s, e) => vm.ReplaceWith = regexReplBox.Text;
		var regexReplRow = new DockPanel { Margin = new Thickness(funcContainer?.Margin.Left ?? 0, 2, 0, 2) };
		DockPanel.SetDock(regexReplLabel, Dock.Left);
		regexReplRow.Children.Add(regexReplLabel); regexReplRow.Children.Add(regexReplBox);

		// AWS4Signature rows (region + service)
		var awsRegLabel = new TextBlock { Text = "Region:", Foreground = white, MinWidth = 80, VerticalAlignment = VerticalAlignment.Center };
		var awsRegBox   = new TextBox   { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(4,2,4,2) };
		awsRegBox.Text  = vm.AwsRegion ?? "";
		awsRegBox.TextChanged += (s, e) => vm.AwsRegion = awsRegBox.Text;
		var awsRegRow = new DockPanel { Margin = new Thickness(funcContainer?.Margin.Left ?? 0, 2, 0, 2) };
		DockPanel.SetDock(awsRegLabel, Dock.Left);
		awsRegRow.Children.Add(awsRegLabel); awsRegRow.Children.Add(awsRegBox);

		var awsSvcLabel = new TextBlock { Text = "Service:", Foreground = white, MinWidth = 80, VerticalAlignment = VerticalAlignment.Center };
		var awsSvcBox   = new TextBox   { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(4,2,4,2) };
		awsSvcBox.Text  = vm.AwsService ?? "";
		awsSvcBox.TextChanged += (s, e) => vm.AwsService = awsSvcBox.Text;
		var awsSvcRow = new DockPanel { Margin = new Thickness(funcContainer?.Margin.Left ?? 0, 2, 0, 2) };
		DockPanel.SetDock(awsSvcLabel, Dock.Left);
		awsSvcRow.Children.Add(awsSvcLabel); awsSvcRow.Children.Add(awsSvcBox);

		// Wrap specialized rows into a container panel to toggle together
		var specialPanel = new StackPanel { Orientation = Orientation.Vertical };
		specialPanel.Children.Add(rsaDecNRow);
		specialPanel.Children.Add(rsaDecDRow);
		specialPanel.Children.Add(regexPatRow);
		specialPanel.Children.Add(regexReplRow);
		specialPanel.Children.Add(awsRegRow);
		specialPanel.Children.Add(awsSvcRow);

		// Insert all extra rows after the GUID/Merge anchor (or after funcContainer)
		var anchor = _mergeSecondInputRow ?? _guidRow ?? (FrameworkElement)funcContainer;
		int anchorIdx = anchor != null ? funcParent.Children.IndexOf(anchor) : -1;

		void InsertAfterAnchor(FrameworkElement el)
		{
			if (funcParent is DockPanel dpp) DockPanel.SetDock(el, DockPanel.GetDock(anchor ?? funcContainer));
			if (anchorIdx >= 0 && anchorIdx < funcParent.Children.Count)
				funcParent.Children.Insert(++anchorIdx, el);
			else
				funcParent.Children.Add(el);
		}

		InsertAfterAnchor(_secondInputRow);
		InsertAfterAnchor(_thirdInputRow);
		InsertAfterAnchor(_jwtAlgoRow);
		InsertAfterAnchor(specialPanel);

		// Store references to the specialized panel items for UpdateExtraInputVisibility
		_rsaDecPanel     = new FrameworkElement[] { rsaDecNRow, rsaDecDRow };
		_regexReplPanel  = new FrameworkElement[] { regexPatRow, regexReplRow };
		_awsPanel        = new FrameworkElement[] { awsRegRow, awsSvcRow };

		UpdateExtraInputVisibility();
	}

	private FrameworkElement[] _rsaDecPanel;
	private FrameworkElement[] _regexReplPanel;
	private FrameworkElement[] _awsPanel;

	private void UpdateExtraInputVisibility()
	{
		if (!_extraInputsInjected) return;
		var ft = vm.FunctionType;

		bool needsSecond = _secondInputLabels.ContainsKey(ft);
		bool needsThird  = _thirdInputLabels.ContainsKey(ft);
		bool needsJwt    = ft == BlockFunction.Function.JWTEncode;
		bool needsRsa    = ft == BlockFunction.Function.RSADecrypt;
		bool needsRegex  = ft == BlockFunction.Function.RegexReplace;
		bool needsAws    = ft == BlockFunction.Function.AWS4Signature;

		if (_secondInputRow != null)
		{
			_secondInputRow.Visibility = (needsSecond && !needsJwt) ? Visibility.Visible : Visibility.Collapsed;
			if (needsSecond && _secondInputLabels.TryGetValue(ft, out string lbl))
				_secondInputLabel.Text = lbl + ":";
		}
		if (_thirdInputRow != null)
			_thirdInputRow.Visibility = needsThird ? Visibility.Visible : Visibility.Collapsed;
		if (_thirdInputRow != null && needsThird && _thirdInputLabels.TryGetValue(ft, out string lbl3))
			_thirdInputLabel.Text = lbl3 + ":";
		if (_jwtAlgoRow != null)
			_jwtAlgoRow.Visibility = needsJwt ? Visibility.Visible : Visibility.Collapsed;
		if (_rsaDecPanel != null)
			foreach (var el in _rsaDecPanel)
				el.Visibility = needsRsa ? Visibility.Visible : Visibility.Collapsed;
		if (_regexReplPanel != null)
			foreach (var el in _regexReplPanel)
				el.Visibility = needsRegex ? Visibility.Visible : Visibility.Collapsed;
		if (_awsPanel != null)
			foreach (var el in _awsPanel)
				el.Visibility = needsAws ? Visibility.Visible : Visibility.Collapsed;
	}

	private void InjectGuidControlsNextToIsCapture()
	{
		if (_guidControlsInjected) return;

		CheckBox isCaptureCb = FindVisualChild<CheckBox>(
			this, c => c.Content is string s && s == "Is Capture");
		if (isCaptureCb == null) return;
		if (!(isCaptureCb.Parent is Panel parent)) return;

		_guidControlsInjected = true;
		bool isGuid = vm.FunctionType == BlockFunction.Function.GenerateGUID;

		// ToUpperCase checkbox next to Is Capture
		_guidToUpperCb = new CheckBox
		{
			Content                  = "ToUpperCase",
			Margin                   = new Thickness(20, 0, 0, 0),
			VerticalAlignment        = VerticalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center,
			Visibility               = isGuid ? Visibility.Visible : Visibility.Collapsed,
			IsChecked                = vm.GuidUppercase
		};
		_guidToUpperCb.Checked   += (s, e) => vm.GuidUppercase = true;
		_guidToUpperCb.Unchecked += (s, e) => vm.GuidUppercase = false;

		// Version/Format block — vertical, two rows, white text like other labels, below Function row
		var white = new SolidColorBrush(Colors.White);
		_guidRow = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Visibility  = isGuid ? Visibility.Visible : Visibility.Collapsed
		};

		TextBlock MakeLabel(string text) => new TextBlock
		{
			Text              = text,
			Width             = 60,
			Foreground        = white,
			VerticalAlignment = VerticalAlignment.Center
		};
		StackPanel MakeRow(TextBlock lbl, ComboBox cb)
		{
			cb.Margin = new Thickness(0);
			var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2), VerticalAlignment = VerticalAlignment.Center };
			row.Children.Add(lbl);
			row.Children.Add(cb);
			return row;
		}
		_guidRow.Children.Add(MakeRow(MakeLabel("Version"), guidVersionComboBox));
		_guidRow.Children.Add(MakeRow(MakeLabel("Format"),  guidFormatComboBox));

		// Hook combos to refresh the info textblock at the bottom
		guidVersionComboBox.SelectionChanged += (s, e) => UpdateGuidInfoText();
		guidFormatComboBox.SelectionChanged  += (s, e) => UpdateGuidInfoText();

		// --- Step 1: add ToUpperCase next to Is Capture ---
		if (parent is StackPanel spH && spH.Orientation == Orientation.Horizontal)
		{
			if (!spH.Children.Contains(_guidToUpperCb)) spH.Children.Add(_guidToUpperCb);
		}
		else
		{
			int isCaptureIdx = parent.Children.IndexOf(isCaptureCb);
			if (isCaptureIdx >= 0)
			{
				var origMargin = isCaptureCb.Margin;
				var origDock   = parent is DockPanel ? DockPanel.GetDock(isCaptureCb) : System.Windows.Controls.Dock.Top;
				parent.Children.Remove(isCaptureCb);
				isCaptureCb.Margin               = new Thickness(0);
				isCaptureCb.VerticalAlignment     = VerticalAlignment.Center;
				isCaptureCb.VerticalContentAlignment = VerticalAlignment.Center;

				var isCaptureRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = origMargin, VerticalAlignment = VerticalAlignment.Center };
				isCaptureRow.Children.Add(isCaptureCb);
				isCaptureRow.Children.Add(_guidToUpperCb);
				if (parent is DockPanel) DockPanel.SetDock(isCaptureRow, origDock);
				parent.Children.Insert(isCaptureIdx, isCaptureRow);
			}
		}

		// --- Step 2: insert _guidRow AFTER the Function combobox row ---
		var funcContainer = functionTypeCombobox.Parent as FrameworkElement;
		Panel funcParent  = funcContainer?.Parent as Panel ?? parent;
		FrameworkElement anchor = (funcParent.Children.Contains(funcContainer) ? funcContainer : functionTypeCombobox) as FrameworkElement;
		int funcIdx = funcParent.Children.IndexOf(anchor);
		if (funcIdx >= 0)
		{
			_guidRow.Margin = new Thickness(anchor.Margin.Left, 2, 0, 2);
			if (funcParent is DockPanel) DockPanel.SetDock(_guidRow, DockPanel.GetDock(anchor));
			funcParent.Children.Insert(funcIdx + 1, _guidRow);
		}
		else
		{
			parent.Children.Add(_guidRow);
		}
	}

	private static T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate)
		where T : DependencyObject
	{
		int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < count; i++)
		{
			var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
			if (child is T typed && predicate(typed)) return typed;
			var found = FindVisualChild(child, predicate);
			if (found != null) return found;
		}
		return null;
	}

	private void InjectMergeByteArrayControls()
	{
		if (_mergeControlsInjected) return;

		// Anchor injection next to the function combo (same parent panel as GUID row)
		var funcContainer = functionTypeCombobox.Parent as FrameworkElement;
		Panel funcParent  = funcContainer?.Parent as Panel;
		if (funcParent == null) return;

		_mergeControlsInjected = true;
		bool isMerge = vm.FunctionType == BlockFunction.Function.MergeByteArrays;

		var white = new SolidColorBrush(Colors.White);

		var label = new TextBlock
		{
			Text              = "Second Input (Base64)",
			Foreground        = white,
			VerticalAlignment = VerticalAlignment.Center,
			MinWidth          = 120,
		};

		var textBox = new TextBox
		{
			Text                = vm.SecondInput,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment   = VerticalAlignment.Center,
			Padding             = new Thickness(4, 2, 4, 2),
		};
		textBox.TextChanged += (s, e) => vm.SecondInput = textBox.Text;

		var row = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
		DockPanel.SetDock(label, Dock.Left);
		row.Children.Add(label);
		row.Children.Add(textBox);

		_mergeSecondInputRow = row;
		_mergeSecondInputRow.Visibility = isMerge ? Visibility.Visible : Visibility.Collapsed;

		// Insert after the GUID row (or after the function combo container)
		var anchor = _guidRow ?? (FrameworkElement)funcContainer;
		int idx    = anchor != null ? funcParent.Children.IndexOf(anchor) : -1;
		if (idx >= 0)
			funcParent.Children.Insert(idx + 1, _mergeSecondInputRow);
		else
			funcParent.Children.Add(_mergeSecondInputRow);
	}

	private void SetItemToComboBox()
	{
		dateToUnixTimeCombobox.SelectedIndex = (int)vm.UnixTimeType;
	}

	private void scryptMethods_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			switch (scryptMethods.SelectedIndex)
			{
			case 0:
				dockPanelCompInput.Visibility = Visibility.Collapsed;
				stackPanelEncode.Visibility = Visibility.Visible;
				break;
			case 1:
				stackPanelEncode.Visibility = Visibility.Collapsed;
				dockPanelCompInput.Visibility = Visibility.Visible;
				break;
			default:
				stackPanelEncode.Visibility = Visibility.Collapsed;
				dockPanelCompInput.Visibility = Visibility.Collapsed;
				break;
			}
		}
		catch
		{
		}
	}

	private void bcryptMethods_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			switch (bcryptMethods.SelectedIndex)
			{
			case 0:
				dockPanelVerifyInput.Visibility = Visibility.Collapsed;
				dockPanelBcryptSalt.Visibility = Visibility.Visible;
				break;
			case 2:
				dockPanelBcryptSalt.Visibility = Visibility.Collapsed;
				dockPanelVerifyInput.Visibility = Visibility.Visible;
				break;
			default:
				dockPanelBcryptSalt.Visibility = Visibility.Collapsed;
				dockPanelVerifyInput.Visibility = Visibility.Collapsed;
				break;
			}
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
			Uri resourceLocator = new Uri("/SilverBullet;component/views/stackerblocks/pageblockfunction.xaml", UriKind.Relative);
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
			((PageBlockFunction)target).Loaded += Page_Loaded;
			break;
		case 2:
			functionTypeCombobox = (ComboBox)target;
			functionTypeCombobox.SelectionChanged += functionTypeCombobox_SelectionChanged;
			break;
		case 3:
			functionTabControl = (TabControl)target;
			break;
		case 4:
			emptyTab = (TabItem)target;
			break;
		case 5:
			hashTab = (TabItem)target;
			break;
		case 6:
			hashTypeCombobox = (ComboBox)target;
			hashTypeCombobox.SelectionChanged += hashTypeCombobox_SelectionChanged;
			break;
		case 7:
			hmacTab = (TabItem)target;
			break;
		case 8:
			hmacHashTypeCombobox = (ComboBox)target;
			hmacHashTypeCombobox.SelectionChanged += hmacHashTypeCombobox_SelectionChanged;
			break;
		case 9:
			translateTab = (TabItem)target;
			break;
		case 10:
			dictionaryRTB = (RichTextBox)target;
			dictionaryRTB.TextChanged += dictionaryRTB_TextChanged;
			break;
		case 11:
			dateToUnixTimeTab = (TabItem)target;
			break;
		case 12:
			dateToUnixTimeCombobox = (ComboBox)target;
			break;
		case 13:
			stringReplaceTab = (TabItem)target;
			break;
		case 14:
			regexMatchTab = (TabItem)target;
			break;
		case 15:
			randomNumberTab = (TabItem)target;
			break;
		case 16:
			countOccurrencesTab = (TabItem)target;
			break;
		case 17:
			rsaEncTab = (TabItem)target;
			break;
		case 18:
			rsaDecTab = (TabItem)target;
			break;
		case 19:
			rsaPkcs1Pad2Tab = (TabItem)target;
			break;
		case 20:
			charAtTab = (TabItem)target;
			break;
		case 21:
			substringTab = (TabItem)target;
			break;
		case 22:
			randomUATab = (TabItem)target;
			break;
		case 23:
			randomUABrowserCombobox = (ComboBox)target;
			randomUABrowserCombobox.SelectionChanged += randomUABrowserCombobox_SelectionChanged;
			break;
		case 24:
			aesTab = (TabItem)target;
			break;
		case 25:
			aesModeCombobox = (ComboBox)target;
			aesModeCombobox.SelectionChanged += aesModeCombobox_SelectionChanged;
			break;
		case 26:
			aesPaddingCombobox = (ComboBox)target;
			aesPaddingCombobox.SelectionChanged += aesPaddingCombobox_SelectionChanged;
			break;
		case 27:
			kdfTab = (TabItem)target;
			break;
		case 28:
			kdfAlgorithmCombobox = (ComboBox)target;
			kdfAlgorithmCombobox.SelectionChanged += kdfAlgorithmCombobox_SelectionChanged;
			break;
		case 29:
			encCombobox = (ComboBox)target;
			break;
		case 30:
			encFuncCombobox = (ComboBox)target;
			break;
		case 31:
			Scrypt = (TabItem)target;
			break;
		case 32:
			scryptMethods = (ComboBox)target;
			scryptMethods.SelectionChanged += scryptMethods_SelectionChanged;
			break;
		case 33:
			stackPanelEncode = (StackPanel)target;
			break;
		case 34:
			dockPanelCompInput = (DockPanel)target;
			break;
		case 35:
			Bcrypt = (TabItem)target;
			break;
		case 36:
			bcryptMethods = (ComboBox)target;
			bcryptMethods.SelectionChanged += bcryptMethods_SelectionChanged;
			break;
		case 37:
			dockPanelBcryptSalt = (DockPanel)target;
			break;
		case 38:
			dockPanelWorkFactorInput = (DockPanel)target;
			break;
		case 39:
			dockPanelVerifyInput = (DockPanel)target;
			break;
		case 40:
			functionInfoTextblock = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
