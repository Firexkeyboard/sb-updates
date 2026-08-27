using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Windows.Media;
using RuriLib.Functions.Conditions;
using RuriLib.Functions.Conversions;
using RuriLib.Functions.Files;
using RuriLib.LS;
using RuriLib.Models;

namespace RuriLib;

public class BlockUtility : BlockBase
{
	private UtilityGroup group;

	private string variableName = "";

	private bool isCapture;

	private string inputString = "";

	private ListAction listAction = ListAction.Join;

	private string listName = "";

	private string separator = ",";

	private bool ascending = true;

	private bool numeric;

	private string secondListName = "";

	private string listItem = "";

	private string listIndex = "-1";

	private Comparer listElementComparer = Comparer.EqualTo;

	private string listComparisonTerm = "";

	private VarAction varAction;

	private string varName = "";

	private string splitSeparator = "";

	private RuriLib.Functions.Conversions.Encoding conversionFrom;

	private RuriLib.Functions.Conversions.Encoding conversionTo = RuriLib.Functions.Conversions.Encoding.BASE64;

	private string filePath = "test.txt";

	private FileAction fileAction = FileAction.Read;

	private string folderPath = "TestFolder";

	private FolderAction folderAction = FolderAction.Create;

	// Misc group fields
	private MiscAction miscAction;

	// Conversion extended fields
	private ConversionAction conversionAction = ConversionAction.Encoding;
	private bool readableSizeOutputBits;
	private bool readableSizeBinaryUnit;
	private string readableSizeDecimalPlaces = "2";
	private string byteStringEncoding = "UTF8"; // for BytesToString / StringToBytes

	// Images group fields
	private ImageAction imageAction = ImageAction.SvgToPng;
	private string imageSvgWidth = "300";
	private string imageSvgHeight = "150";

	public UtilityGroup Group
	{
		get
		{
			return group;
		}
		set
		{
			group = value;
			OnPropertyChanged("Group");
		}
	}

	public string VariableName
	{
		get
		{
			return variableName;
		}
		set
		{
			variableName = value;
			OnPropertyChanged("VariableName");
		}
	}

	public bool IsCapture
	{
		get
		{
			return isCapture;
		}
		set
		{
			isCapture = value;
			OnPropertyChanged("IsCapture");
		}
	}

	public string InputString
	{
		get
		{
			return inputString;
		}
		set
		{
			inputString = value;
			OnPropertyChanged("InputString");
		}
	}

	public ListAction ListAction
	{
		get
		{
			return listAction;
		}
		set
		{
			listAction = value;
			OnPropertyChanged("ListAction");
		}
	}

	public string ListName
	{
		get
		{
			return listName;
		}
		set
		{
			listName = value;
			OnPropertyChanged("ListName");
		}
	}

	public string Separator
	{
		get
		{
			return separator;
		}
		set
		{
			separator = value;
			OnPropertyChanged("Separator");
		}
	}

	public bool Ascending
	{
		get
		{
			return ascending;
		}
		set
		{
			ascending = value;
			OnPropertyChanged("Ascending");
		}
	}

	public bool Numeric
	{
		get
		{
			return numeric;
		}
		set
		{
			numeric = value;
			OnPropertyChanged("Numeric");
		}
	}

	public string SecondListName
	{
		get
		{
			return secondListName;
		}
		set
		{
			secondListName = value;
			OnPropertyChanged("SecondListName");
		}
	}

	public string ListItem
	{
		get
		{
			return listItem;
		}
		set
		{
			listItem = value;
			OnPropertyChanged("ListItem");
		}
	}

	public string ListIndex
	{
		get
		{
			return listIndex;
		}
		set
		{
			listIndex = value;
			OnPropertyChanged("ListIndex");
		}
	}

	public Comparer ListElementComparer
	{
		get
		{
			return listElementComparer;
		}
		set
		{
			listElementComparer = value;
			OnPropertyChanged("ListElementComparer");
		}
	}

	public string ListComparisonTerm
	{
		get
		{
			return listComparisonTerm;
		}
		set
		{
			listComparisonTerm = value;
			OnPropertyChanged("ListComparisonTerm");
		}
	}

	public VarAction VarAction
	{
		get
		{
			return varAction;
		}
		set
		{
			varAction = value;
			OnPropertyChanged("VarAction");
		}
	}

	public string VarName
	{
		get
		{
			return varName;
		}
		set
		{
			varName = value;
			OnPropertyChanged("VarName");
		}
	}

	public string SplitSeparator
	{
		get
		{
			return splitSeparator;
		}
		set
		{
			splitSeparator = value;
			OnPropertyChanged("SplitSeparator");
		}
	}

	public RuriLib.Functions.Conversions.Encoding ConversionFrom
	{
		get
		{
			return conversionFrom;
		}
		set
		{
			conversionFrom = value;
			OnPropertyChanged("ConversionFrom");
		}
	}

	public RuriLib.Functions.Conversions.Encoding ConversionTo
	{
		get
		{
			return conversionTo;
		}
		set
		{
			conversionTo = value;
			OnPropertyChanged("ConversionTo");
		}
	}

	public string FilePath
	{
		get
		{
			return filePath;
		}
		set
		{
			filePath = value;
			OnPropertyChanged("FilePath");
		}
	}

	public FileAction FileAction
	{
		get
		{
			return fileAction;
		}
		set
		{
			fileAction = value;
			OnPropertyChanged("FileAction");
		}
	}

	public string FolderPath
	{
		get
		{
			return folderPath;
		}
		set
		{
			folderPath = value;
			OnPropertyChanged("FolderPath");
		}
	}

	public FolderAction FolderAction
	{
		get
		{
			return folderAction;
		}
		set
		{
			folderAction = value;
			OnPropertyChanged("FolderAction");
		}
	}

	public BlockUtility()
	{
		base.Label = "UTILITY";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		Group = (UtilityGroup)LineParser.ParseEnum(ref input, "Group", typeof(UtilityGroup));
		switch (Group)
		{
		case UtilityGroup.List:
			ListName = LineParser.ParseLiteral(ref input, "List Name");
			ListAction = (ListAction)LineParser.ParseEnum(ref input, "List Action", typeof(ListAction));
			switch (ListAction)
			{
			case ListAction.Join:
				Separator = LineParser.ParseLiteral(ref input, "Separator");
				break;
			case ListAction.Sort:
				while (LineParser.Lookahead(ref input) == TokenType.Boolean)
				{
					LineParser.SetBool(ref input, this);
				}
				break;
			case ListAction.Concat:
			case ListAction.Zip:
			case ListAction.Map:
				SecondListName = LineParser.ParseLiteral(ref input, "Second List Name");
				break;
			case ListAction.Add:
				ListItem = LineParser.ParseLiteral(ref input, "Item");
				ListIndex = LineParser.ParseLiteral(ref input, "Index");
				break;
			case ListAction.Remove:
				ListIndex = LineParser.ParseLiteral(ref input, "Index");
				break;
			case ListAction.RemoveValues:
				ListElementComparer = LineParser.ParseEnum(ref input, "Comparer", typeof(Comparer));
				ListComparisonTerm = LineParser.ParseLiteral(ref input, "Comparison Term");
				break;
			}
			break;
		case UtilityGroup.Variable:
			VarName = LineParser.ParseLiteral(ref input, "Var Name");
			VarAction = (VarAction)LineParser.ParseEnum(ref input, "Var Action", typeof(VarAction));
			if (VarAction == VarAction.Split)
			{
				SplitSeparator = LineParser.ParseLiteral(ref input, "SplitSeparator");
			}
			break;
		case UtilityGroup.Conversion:
		{
			string savedInput = input;
			bool parsedExtended = false;
			try
			{
				var ca = (ConversionAction)LineParser.ParseEnum(ref input, "Conversion Action", typeof(ConversionAction));
				if (ca != ConversionAction.Encoding)
				{
					ConversionAct = ca;
					InputString = LineParser.ParseLiteral(ref input, "Input");
					if (ca == ConversionAction.ReadableSize)
					{
						while (LineParser.Lookahead(ref input) == TokenType.Boolean)
							LineParser.SetBool(ref input, this);
						if (LineParser.Lookahead(ref input) == TokenType.Literal)
							ReadableSizeDecimalPlaces = LineParser.ParseLiteral(ref input, "Decimal Places");
					}
					else if (ca == ConversionAction.BytesToString || ca == ConversionAction.StringToBytes)
					{
						if (LineParser.Lookahead(ref input) == TokenType.Literal)
							ByteStringEncoding = LineParser.ParseLiteral(ref input, "Encoding");
					}
					parsedExtended = true;
				}
			}
			catch { }
			if (!parsedExtended)
			{
				input = savedInput;
				ConversionAct = ConversionAction.Encoding;
				ConversionFrom = (RuriLib.Functions.Conversions.Encoding)LineParser.ParseEnum(ref input, "Conversion From", typeof(RuriLib.Functions.Conversions.Encoding));
				ConversionTo = (RuriLib.Functions.Conversions.Encoding)LineParser.ParseEnum(ref input, "Conversion To", typeof(RuriLib.Functions.Conversions.Encoding));
				InputString = LineParser.ParseLiteral(ref input, "Input");
			}
			break;
		}
		case UtilityGroup.File:
		{
			FilePath = LineParser.ParseLiteral(ref input, "File Name");
			FileAction = (FileAction)LineParser.ParseEnum(ref input, "File Action", typeof(FileAction));
			FileAction fileAction = FileAction;
			if ((uint)(fileAction - 3) <= 5u)
			{
				InputString = LineParser.ParseLiteral(ref input, "Input String");
			}
			break;
		}
		case UtilityGroup.Folder:
			FolderPath = LineParser.ParseLiteral(ref input, "Folder Name");
			FolderAction = (FolderAction)LineParser.ParseEnum(ref input, "Folder Action", typeof(FolderAction));
			break;
		case UtilityGroup.Misc:
			MiscAction = (MiscAction)LineParser.ParseEnum(ref input, "Misc Action", typeof(MiscAction));
			break;
		case UtilityGroup.Images:
			ImageAct = (ImageAction)LineParser.ParseEnum(ref input, "Image Action", typeof(ImageAction));
			InputString = LineParser.ParseLiteral(ref input, "Input");
			if (LineParser.Lookahead(ref input) == TokenType.Literal)
				ImageSvgWidth = LineParser.ParseLiteral(ref input, "Width");
			if (LineParser.Lookahead(ref input) == TokenType.Literal)
				ImageSvgHeight = LineParser.ParseLiteral(ref input, "Height");
			break;
		}
		if (LineParser.ParseToken(ref input, TokenType.Arrow, essential: false) == string.Empty)
		{
			return this;
		}
		try
		{
			string text2 = LineParser.ParseToken(ref input, TokenType.Parameter, essential: true);
			if (text2.ToUpper() == "VAR" || text2.ToUpper() == "CAP")
			{
				IsCapture = text2.ToUpper() == "CAP";
			}
		}
		catch
		{
			throw new ArgumentException("Invalid or missing variable type");
		}
		try
		{
			VariableName = LineParser.ParseToken(ref input, TokenType.Literal, essential: true);
			return this;
		}
		catch
		{
			throw new ArgumentException("Variable name not specified");
		}
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("UTILITY").Token(Group);
		switch (Group)
		{
		case UtilityGroup.List:
			blockWriter.Literal(ListName).Token(ListAction);
			switch (ListAction)
			{
			case ListAction.Join:
				blockWriter.Literal(Separator);
				break;
			case ListAction.Sort:
				blockWriter.Boolean(Ascending, "Ascending").Boolean(Numeric, "Numeric");
				break;
			case ListAction.Concat:
			case ListAction.Zip:
			case ListAction.Map:
				blockWriter.Literal(SecondListName);
				break;
			case ListAction.Add:
				blockWriter.Literal(ListItem).Literal(ListIndex);
				break;
			case ListAction.Remove:
				blockWriter.Literal(ListIndex);
				break;
			case ListAction.RemoveValues:
				blockWriter.Token(ListElementComparer).Literal(ListComparisonTerm);
				break;
			}
			break;
		case UtilityGroup.Variable:
			blockWriter.Literal(VarName).Token(VarAction);
			if (VarAction == VarAction.Split)
			{
				blockWriter.Literal(SplitSeparator);
			}
			break;
		case UtilityGroup.Conversion:
			if (conversionAction == ConversionAction.Encoding)
			{
				blockWriter.Token(ConversionFrom).Token(ConversionTo).Literal(InputString);
			}
			else
			{
				blockWriter.Token(ConversionAct).Literal(InputString);
				if (conversionAction == ConversionAction.ReadableSize)
				{
					blockWriter.Boolean(ReadableSizeOutputBits, "ReadableSizeOutputBits")
					           .Boolean(ReadableSizeBinaryUnit, "ReadableSizeBinaryUnit")
					           .Literal(ReadableSizeDecimalPlaces);
				}
				else if (conversionAction == ConversionAction.BytesToString ||
				         conversionAction == ConversionAction.StringToBytes)
				{
					blockWriter.Literal(ByteStringEncoding);
				}
			}
			break;
		case UtilityGroup.File:
		{
			blockWriter.Literal(FilePath).Token(FileAction);
			FileAction fileAction = FileAction;
			if ((uint)(fileAction - 3) <= 5u)
			{
				blockWriter.Literal(InputString);
			}
			break;
		}
		case UtilityGroup.Folder:
			blockWriter.Literal(FolderPath).Token(FolderAction);
			break;
		case UtilityGroup.Misc:
			blockWriter.Token(MiscAction);
			break;
		case UtilityGroup.Images:
			blockWriter.Token(ImageAct).Literal(InputString).Literal(ImageSvgWidth).Literal(ImageSvgHeight);
			break;
		}
		if (!blockWriter.CheckDefault(VariableName, "VariableName"))
		{
			blockWriter.Arrow().Token(IsCapture ? "CAP" : "VAR").Literal(VariableName);
		}
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		try
		{
			string text = BlockBase.ReplaceValues(inputString, data);
			switch (group)
			{
			case UtilityGroup.List:
			{
				List<string> list = data.Variables.GetList(listName);
				List<string> list2 = data.Variables.GetList(secondListName);
				string text7 = BlockBase.ReplaceValues(listItem, data);
				int num = int.Parse(BlockBase.ReplaceValues(listIndex, data));
				CVar cVar = null;
				switch (listAction)
				{
				case ListAction.Create:
					data.Variables.Set(new CVar(variableName, new List<string>(), isCapture));
					break;
				case ListAction.Length:
					data.Variables.Set(new CVar(variableName, list.Count().ToString(), isCapture));
					break;
				case ListAction.Join:
					data.Variables.Set(new CVar(variableName, string.Join(separator, list), isCapture));
					break;
				case ListAction.Sort:
				{
					List<string> list4 = list.Select((string e) => e).ToList();
					if (Numeric)
					{
						List<double> list5 = list4.Select((string e) => double.Parse(e, CultureInfo.InvariantCulture)).ToList();
						list5.Sort();
						list4 = list5.Select((double e) => e.ToString()).ToList();
					}
					else
					{
						list4.Sort();
					}
					if (!Ascending)
					{
						list4.Reverse();
					}
					data.Variables.Set(new CVar(variableName, list4, isCapture));
					break;
				}
				case ListAction.Concat:
					data.Variables.Set(new CVar(variableName, list.Concat(list2).ToList(), isCapture));
					break;
				case ListAction.Zip:
					data.Variables.Set(new CVar(variableName, list.Zip(list2, (string a, string b) => a + b).ToList(), isCapture));
					break;
				case ListAction.Map:
					data.Variables.Set(new CVar(variableName, list.Zip(list2, (string k, string v) => new { k, v }).ToDictionary(x => x.k, x => x.v), isCapture));
					break;
				case ListAction.Add:
					cVar = data.Variables.Get(listName, CVar.VarType.List);
					if (cVar == null)
					{
						cVar = data.GlobalVariables.Get(listName, CVar.VarType.List);
					}
					if (cVar != null)
					{
						if (cVar.Value.Count == 0)
						{
							num = 0;
						}
						else if (num < 0)
						{
							num += cVar.Value.Count;
						}
						cVar.Value.Insert(num, text7);
					}
					break;
				case ListAction.Remove:
					cVar = data.Variables.Get(listName, CVar.VarType.List);
					if (cVar == null)
					{
						cVar = data.GlobalVariables.Get(listName, CVar.VarType.List);
					}
					if (cVar != null)
					{
						if (cVar.Value.Count == 0)
						{
							num = 0;
						}
						else if (num < 0)
						{
							num += cVar.Value.Count;
						}
						cVar.Value.RemoveAt(num);
					}
					break;
				case ListAction.RemoveValues:
					data.Variables.Set(new CVar(variableName, list.Where(delegate(string l)
					{
						KeycheckCondition kcCond = default(KeycheckCondition);
						kcCond.Left = BlockBase.ReplaceValues(l, data);
						kcCond.Comparer = ListElementComparer;
						kcCond.Right = ListComparisonTerm;
						return !Condition.Verify(kcCond);
					}).ToList(), isCapture));
					break;
				case ListAction.RemoveDuplicates:
					data.Variables.Set(new CVar(variableName, list.Distinct().ToList(), isCapture));
					break;
				case ListAction.Random:
					data.Variables.Set(new CVar(variableName, list[data.random.Next(list.Count)], isCapture));
					break;
				case ListAction.Shuffle:
				{
					List<string> list3 = list.ToArray().ToList();
					list3.Shuffle(data.random);
					data.Variables.Set(new CVar(variableName, list3, isCapture));
					break;
				}
				}
				data.Log(new LogEntry($"Executed action {listAction} on list {listName}", isCapture ? Colors.Tomato : Colors.Yellow));
				break;
			}
			case UtilityGroup.Variable:
				if (varAction == VarAction.Split)
				{
					string single = data.Variables.GetSingle(varName);
					data.Variables.Set(new CVar(variableName, single.Split(new string[1] { BlockBase.ReplaceValues(splitSeparator, data) }, StringSplitOptions.None).ToList(), isCapture));
				}
				data.Log(new LogEntry($"Executed action {varAction} on variable {varName}", isCapture ? Colors.Tomato : Colors.Yellow));
				break;
			case UtilityGroup.Conversion:
			{
				string convResult;
				if (conversionAction == ConversionAction.Encoding)
				{
					convResult = text.ConvertFrom(conversionFrom).ConvertTo(conversionTo);
					data.Log(new LogEntry($"Executed conversion {conversionFrom} to {conversionTo} on input {text} with outcome {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.BigIntegerToByteArray)
				{
					var biVal = BigInteger.Parse(text);
					byte[] biBytes = biVal.IsZero ? new byte[] { 0 } : biVal.ToByteArray(isUnsigned: true, isBigEndian: true);
					convResult = Convert.ToHexString(biBytes).ToLower();
					data.Log(new LogEntry($"BigIntegerToByteArray: {text} → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.ByteArrayToBigInteger)
				{
					byte[] baBytes;
					try { baBytes = Convert.FromHexString(text.Trim()); }
					catch { baBytes = Convert.FromBase64String(text.Trim()); }
					convResult = new BigInteger(baBytes, isUnsigned: true, isBigEndian: true).ToString();
					data.Log(new LogEntry($"ByteArrayToBigInteger: {text} → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.Base64ToBytes)
				{
					byte[] b64Bytes = Convert.FromBase64String(text.Trim());
					convResult = Convert.ToHexString(b64Bytes).ToLower();
					data.Log(new LogEntry($"Base64ToBytes: → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.Base64ToUtf8)
				{
					byte[] b64utf8 = Convert.FromBase64String(text.Trim());
					convResult = System.Text.Encoding.UTF8.GetString(b64utf8);
					data.Log(new LogEntry($"Base64ToUtf8: → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.BinaryStringToBytes)
				{
					string binStr = text.Trim().Replace(" ", "").Replace("\t", "");
					var binBytes = new System.Collections.Generic.List<byte>();
					for (int i = 0; i + 8 <= binStr.Length; i += 8)
						binBytes.Add(Convert.ToByte(binStr.Substring(i, 8), 2));
					convResult = Convert.ToHexString(binBytes.ToArray()).ToLower();
					data.Log(new LogEntry($"BinaryStringToBytes: → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.BytesToBase64)
				{
					byte[] b2b64 = ParseHexOrBase64(text.Trim());
					convResult = Convert.ToBase64String(b2b64);
					data.Log(new LogEntry($"BytesToBase64: → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.BytesToBinaryString)
				{
					byte[] b2bin = ParseHexOrBase64(text.Trim());
					convResult = string.Concat(b2bin.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
					data.Log(new LogEntry($"BytesToBinaryString: → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.BytesToHex)
				{
					byte[] b2hex = ParseHexOrBase64(text.Trim());
					convResult = Convert.ToHexString(b2hex).ToLower();
					data.Log(new LogEntry($"BytesToHex: → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.BytesToString)
				{
					byte[] b2str = ParseHexOrBase64(text.Trim());
					convResult = GetTextEncoding(BlockBase.ReplaceValues(byteStringEncoding, data)).GetString(b2str);
					data.Log(new LogEntry($"BytesToString ({byteStringEncoding}): → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.HexToBytes)
				{
					string hexIn = text.Trim().Replace(" ", "").Replace("0x", "").Replace("0X", "");
					if (hexIn.Length % 2 != 0) hexIn = "0" + hexIn;
					byte[] hexBytes = Convert.FromHexString(hexIn);
					convResult = Convert.ToHexString(hexBytes).ToLower();
					data.Log(new LogEntry($"HexToBytes: → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.StringToBytes)
				{
					byte[] strBytes = GetTextEncoding(BlockBase.ReplaceValues(byteStringEncoding, data)).GetBytes(text);
					convResult = Convert.ToHexString(strBytes).ToLower();
					data.Log(new LogEntry($"StringToBytes ({byteStringEncoding}): → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else if (conversionAction == ConversionAction.Utf8ToBase64)
				{
					byte[] u2b64 = System.Text.Encoding.UTF8.GetBytes(text);
					convResult = Convert.ToBase64String(u2b64);
					data.Log(new LogEntry($"Utf8ToBase64: → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				else
				{
					long sizeBytes = long.Parse(text);
					int dp = int.TryParse(BlockBase.ReplaceValues(readableSizeDecimalPlaces, data), out int dpParsed) ? dpParsed : 2;
					convResult = ComputeReadableSize(sizeBytes, readableSizeOutputBits, readableSizeBinaryUnit, dp);
					data.Log(new LogEntry($"ReadableSize: {text} → {convResult}", isCapture ? Colors.Tomato : Colors.Yellow));
				}
				data.Variables.Set(new CVar(variableName, convResult, isCapture));
				break;
			}
			case UtilityGroup.File:
			{
				string text4 = BlockBase.ReplaceValues(filePath, data);
				Files.ThrowIfNotInCWD(text4);
				switch (fileAction)
				{
				case FileAction.Exists:
					data.Variables.Set(new CVar(variableName, System.IO.File.Exists(text4).ToString(), isCapture));
					break;
				case FileAction.Read:
					lock (FileLocker.GetLock(text4))
					{
						data.Variables.Set(new CVar(variableName, System.IO.File.ReadAllText(text4), isCapture));
					}
					break;
				case FileAction.ReadLines:
					lock (FileLocker.GetLock(text4))
					{
						data.Variables.Set(new CVar(variableName, System.IO.File.ReadAllLines(text4).ToList(), isCapture));
					}
					break;
				case FileAction.Write:
					Files.CreatePath(text4);
					lock (FileLocker.GetLock(text4))
					{
						System.IO.File.WriteAllText(text4, text.Unescape());
					}
					break;
				case FileAction.WriteLines:
					Files.CreatePath(text4);
					lock (FileLocker.GetLock(text4))
					{
						System.IO.File.WriteAllLines(text4, from i in BlockBase.ReplaceValuesRecursive(inputString, data)
							select i.Unescape());
					}
					break;
				case FileAction.Append:
					Files.CreatePath(text4);
					lock (FileLocker.GetLock(text4))
					{
						System.IO.File.AppendAllText(text4, text.Unescape());
					}
					break;
				case FileAction.AppendLines:
					Files.CreatePath(text4);
					lock (FileLocker.GetLock(text4))
					{
						System.IO.File.AppendAllLines(text4, from i in BlockBase.ReplaceValuesRecursive(inputString, data)
							select i.Unescape());
					}
					break;
				case FileAction.Copy:
				{
					string text6 = BlockBase.ReplaceValues(inputString, data);
					Files.ThrowIfNotInCWD(text6);
					Files.CreatePath(text6);
					lock (FileLocker.GetLock(text4))
					{
						lock (FileLocker.GetLock(text6))
						{
							System.IO.File.Copy(text4, text6);
						}
					}
					break;
				}
				case FileAction.Move:
				{
					string text5 = BlockBase.ReplaceValues(inputString, data);
					Files.ThrowIfNotInCWD(text5);
					Files.CreatePath(text5);
					lock (FileLocker.GetLock(text4))
					{
						lock (FileLocker.GetLock(text5))
						{
							System.IO.File.Move(text4, text5);
						}
					}
					break;
				}
				case FileAction.Delete:
					lock (FileLocker.GetLock(text4))
					{
						System.IO.File.Delete(text4);
					}
					break;
				}
				data.Log(new LogEntry($"Executed action {fileAction} on file {text4}", isCapture ? Colors.Tomato : Colors.Yellow));
				break;
			}
			case UtilityGroup.Folder:
			{
				string text3 = BlockBase.ReplaceValues(folderPath, data);
				Files.ThrowIfNotInCWD(text3);
				switch (folderAction)
				{
				case FolderAction.Exists:
					data.Variables.Set(new CVar(variableName, Directory.Exists(text3).ToString(), isCapture));
					break;
				case FolderAction.Create:
					data.Variables.Set(new CVar(variableName, Directory.CreateDirectory(text3).ToString(), isCapture));
					break;
				case FolderAction.Delete:
					Directory.Delete(text3, recursive: true);
					break;
				}
				data.Log(new LogEntry($"Executed action {folderAction} on folder {text3}", isCapture ? Colors.Tomato : Colors.Yellow));
				break;
			}
			case UtilityGroup.Misc:
			{
				string miscResult = "";
				switch (miscAction)
				{
				case MiscAction.ClearCookies:
					data.Cookies.Clear();
					break;
				case MiscAction.GetHWID:
					try
					{
						miscResult = Microsoft.Win32.Registry.LocalMachine
							.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography")?
							.GetValue("MachineGuid")?.ToString() ?? System.Environment.MachineName;
					}
					catch { miscResult = System.Environment.MachineName; }
					data.Variables.Set(new CVar(variableName, miscResult, isCapture));
					break;
				}
				data.Log(new LogEntry($"Executed misc action {miscAction}", isCapture ? Colors.Tomato : Colors.Yellow));
				break;
			}
			case UtilityGroup.Images:
			{
				string imgInput = BlockBase.ReplaceValues(inputString, data);
				string imgResult = "";
				if (imageAction == ImageAction.SvgToPng)
				{
					int svgW = int.TryParse(BlockBase.ReplaceValues(imageSvgWidth, data), out int w) ? w : 300;
					int svgH = int.TryParse(BlockBase.ReplaceValues(imageSvgHeight, data), out int h) ? h : 150;
					var svgDoc = Svg.SvgDocument.FromSvg<Svg.SvgDocument>(imgInput);
					using var pngMs = new MemoryStream();
					using (var bmp = svgDoc.Draw(svgW, svgH))
						bmp.Save(pngMs, System.Drawing.Imaging.ImageFormat.Png);
					imgResult = Convert.ToBase64String(pngMs.ToArray());
				}
				data.Variables.Set(new CVar(variableName, imgResult, isCapture));
				data.Log(new LogEntry($"Executed image action {imageAction}", isCapture ? Colors.Tomato : Colors.Yellow));
				break;
			}
			}
		}
		catch (Exception ex)
		{
			data.Log(new LogEntry(ex.Message, Colors.Tomato));
			if (ex.InnerException != null)
			{
				data.Log(new LogEntry(ex.InnerException.Message, Colors.Tomato));
			}
		}
	}

	private static byte[] ParseHexOrBase64(string s)
	{
		try { return Convert.FromHexString(s); } catch { }
		return Convert.FromBase64String(s);
	}

	private static System.Text.Encoding GetTextEncoding(string name) =>
		(name ?? "UTF8").ToUpperInvariant() switch
		{
			"ASCII"                  => System.Text.Encoding.ASCII,
			"UNICODE" or "UTF16" or "UTF-16" => System.Text.Encoding.Unicode,
			"UTF32"  or "UTF-32"    => System.Text.Encoding.UTF32,
			"LATIN1" or "ISO-8859-1" => System.Text.Encoding.Latin1,
			_                        => System.Text.Encoding.UTF8,
		};

	private static string ComputeReadableSize(long byteCount, bool outputBits, bool binaryUnit, int decimals)
	{
		double value = outputBits ? byteCount * 8.0 : byteCount;
		double divisor = binaryUnit ? 1024.0 : 1000.0;
		string[] units = binaryUnit
			? (outputBits ? new[] { "bit", "Kibit", "Mibit", "Gibit", "Tibit", "Pibit" }
			              : new[] { "B", "KiB", "MiB", "GiB", "TiB", "PiB" })
			: (outputBits ? new[] { "bit", "Kbit", "Mbit", "Gbit", "Tbit", "Pbit" }
			              : new[] { "B", "KB", "MB", "GB", "TB", "PB" });
		int order = 0;
		while (value >= divisor && order < units.Length - 1) { order++; value /= divisor; }
		return value.ToString("F" + decimals, CultureInfo.InvariantCulture) + " " + units[order];
	}


	// Misc group public properties
	public MiscAction MiscAction
	{
		get => miscAction;
		set { miscAction = value; OnPropertyChanged("MiscAction"); }
	}

	// Conversion extended public properties
	public ConversionAction ConversionAct
	{
		get => conversionAction;
		set { conversionAction = value; OnPropertyChanged("ConversionAct"); }
	}

	public bool ReadableSizeOutputBits
	{
		get => readableSizeOutputBits;
		set { readableSizeOutputBits = value; OnPropertyChanged("ReadableSizeOutputBits"); }
	}

	public bool ReadableSizeBinaryUnit
	{
		get => readableSizeBinaryUnit;
		set { readableSizeBinaryUnit = value; OnPropertyChanged("ReadableSizeBinaryUnit"); }
	}

	public string ReadableSizeDecimalPlaces
	{
		get => readableSizeDecimalPlaces;
		set { readableSizeDecimalPlaces = value; OnPropertyChanged("ReadableSizeDecimalPlaces"); }
	}

	public string ByteStringEncoding
	{
		get => byteStringEncoding;
		set { byteStringEncoding = value; OnPropertyChanged("ByteStringEncoding"); }
	}

	// Images group public properties
	public ImageAction ImageAct
	{
		get => imageAction;
		set { imageAction = value; OnPropertyChanged("ImageAct"); }
	}

	public string ImageSvgWidth
	{
		get => imageSvgWidth;
		set { imageSvgWidth = value; OnPropertyChanged("ImageSvgWidth"); }
	}

	public string ImageSvgHeight
	{
		get => imageSvgHeight;
		set { imageSvgHeight = value; OnPropertyChanged("ImageSvgHeight"); }
	}

}
