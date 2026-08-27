using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RuriLib.LS;
using RuriLib.Utils.Parsing;

namespace RuriLib;

public class BlockParse : BlockBase
{
	private string parseTarget = "<SOURCE>";

	private string variableName = "";

	private bool isCapture;

	private string prefix = "";

	private string suffix = "";

	private bool recursive;

	private bool dotMatches;

	private bool caseSensitive = true;

	private bool encodeOutput;

	private bool createEmpty = true;

	private ParseType type;

	private string leftString = "";

	private string rightString = "";

	private bool useRegexLR;

	private string cssSelector = "";

	private string attributeName = "";

	private int cssElementIndex;

	private string jsonField = "";

	private bool jTokenParsing;

	private string regexString = "";

	private string regexOutput = "";

	private int indexOutputList = -1;

	public string ParseTarget
	{
		get
		{
			return parseTarget;
		}
		set
		{
			parseTarget = value;
			OnPropertyChanged("ParseTarget");
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

	public string Prefix
	{
		get
		{
			return prefix;
		}
		set
		{
			prefix = value;
			OnPropertyChanged("Prefix");
		}
	}

	public string Suffix
	{
		get
		{
			return suffix;
		}
		set
		{
			suffix = value;
			OnPropertyChanged("Suffix");
		}
	}

	public bool Recursive
	{
		get
		{
			return recursive;
		}
		set
		{
			recursive = value;
			OnPropertyChanged("Recursive");
		}
	}

	public bool DotMatches
	{
		get
		{
			return dotMatches;
		}
		set
		{
			dotMatches = value;
			OnPropertyChanged("DotMatches");
		}
	}

	public bool CaseSensitive
	{
		get
		{
			return caseSensitive;
		}
		set
		{
			caseSensitive = value;
			OnPropertyChanged("CaseSensitive");
		}
	}

	public bool EncodeOutput
	{
		get
		{
			return encodeOutput;
		}
		set
		{
			encodeOutput = value;
			OnPropertyChanged("EncodeOutput");
		}
	}

	public bool CreateEmpty
	{
		get
		{
			return createEmpty;
		}
		set
		{
			createEmpty = value;
			OnPropertyChanged("CreateEmpty");
		}
	}

	public ParseType Type
	{
		get
		{
			return type;
		}
		set
		{
			type = value;
			OnPropertyChanged("Type");
		}
	}

	public string LeftString
	{
		get
		{
			return leftString;
		}
		set
		{
			leftString = value;
			OnPropertyChanged("LeftString");
		}
	}

	public string RightString
	{
		get
		{
			return rightString;
		}
		set
		{
			rightString = value;
			OnPropertyChanged("RightString");
		}
	}

	public bool UseRegexLR
	{
		get
		{
			return useRegexLR;
		}
		set
		{
			useRegexLR = value;
			OnPropertyChanged("UseRegexLR");
		}
	}

	public string CssSelector
	{
		get
		{
			return cssSelector;
		}
		set
		{
			cssSelector = value;
			OnPropertyChanged("CssSelector");
		}
	}

	public string AttributeName
	{
		get
		{
			return attributeName;
		}
		set
		{
			attributeName = value;
			OnPropertyChanged("AttributeName");
		}
	}

	public int CssElementIndex
	{
		get
		{
			return cssElementIndex;
		}
		set
		{
			cssElementIndex = value;
			OnPropertyChanged("CssElementIndex");
		}
	}

	public string JsonField
	{
		get
		{
			return jsonField;
		}
		set
		{
			jsonField = value;
			OnPropertyChanged("JsonField");
		}
	}

	public bool JTokenParsing
	{
		get
		{
			return jTokenParsing;
		}
		set
		{
			jTokenParsing = value;
			OnPropertyChanged("JTokenParsing");
		}
	}

	public string RegexString
	{
		get
		{
			return regexString;
		}
		set
		{
			regexString = value;
			OnPropertyChanged("RegexString");
		}
	}

	public string RegexOutput
	{
		get
		{
			return regexOutput;
		}
		set
		{
			regexOutput = value;
			OnPropertyChanged("RegexOutput");
		}
	}

	public int IndexOutputList
	{
		get
		{
			return indexOutputList;
		}
		set
		{
			indexOutputList = value;
			OnPropertyChanged("IndexOutputList");
		}
	}

	public BlockParse()
	{
		base.Label = "PARSE";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		ParseTarget = LineParser.ParseLiteral(ref input, "TARGET");
		Type = (ParseType)LineParser.ParseEnum(ref input, "TYPE", typeof(ParseType));
		switch (Type)
		{
		case ParseType.LR:
			LeftString = LineParser.ParseLiteral(ref input, "LEFT STRING");
			RightString = LineParser.ParseLiteral(ref input, "RIGHT STRING");
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case ParseType.CSS:
			CssSelector = LineParser.ParseLiteral(ref input, "SELECTOR");
			AttributeName = LineParser.ParseLiteral(ref input, "ATTRIBUTE");
			if (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			else if (LineParser.Lookahead(ref input) == TokenType.Integer)
			{
				CssElementIndex = LineParser.ParseInt(ref input, "INDEX");
			}
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case ParseType.JSON:
			JsonField = LineParser.ParseLiteral(ref input, "FIELD");
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case ParseType.REGEX:
		{
			RegexString = LineParser.ParseLiteral(ref input, "PATTERN");
			RegexOutput = LineParser.ParseLiteral(ref input, "OUTPUT");
			string text = input;
			try
			{
				IndexOutputList = LineParser.ParseInt(ref input, "INDEX");
			}
			catch
			{
				input = text;
			}
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		}
		}
		LineParser.ParseToken(ref input, TokenType.Arrow, essential: true);
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
			VariableName = LineParser.ParseLiteral(ref input, "NAME");
		}
		catch
		{
			throw new ArgumentException("Variable name not specified");
		}
		try
		{
			Prefix = LineParser.ParseLiteral(ref input, "PREFIX");
			Suffix = LineParser.ParseLiteral(ref input, "SUFFIX");
		}
		catch
		{
		}
		return this;
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("PARSE").Literal(ParseTarget)
			.Token(Type);
		switch (Type)
		{
		case ParseType.LR:
			blockWriter.Literal(LeftString).Literal(RightString).Boolean(Recursive, "Recursive")
				.Boolean(EncodeOutput, "EncodeOutput")
				.Boolean(CreateEmpty, "CreateEmpty")
				.Boolean(UseRegexLR, "UseRegexLR");
			break;
		case ParseType.CSS:
			blockWriter.Literal(CssSelector).Literal(AttributeName);
			if (Recursive)
			{
				blockWriter.Boolean(Recursive, "Recursive");
			}
			else
			{
				blockWriter.Integer(CssElementIndex, "CssElementIndex");
			}
			blockWriter.Boolean(EncodeOutput, "EncodeOutput").Boolean(CreateEmpty, "CreateEmpty");
			break;
		case ParseType.JSON:
			blockWriter.Literal(JsonField).Boolean(JTokenParsing, "JTokenParsing").Boolean(Recursive, "Recursive")
				.Boolean(EncodeOutput, "EncodeOutput")
				.Boolean(CreateEmpty, "CreateEmpty");
			break;
		case ParseType.REGEX:
			if (IndexOutputList > -1)
			{
				blockWriter.Literal(RegexString).Literal(RegexOutput).Integer(indexOutputList)
					.Boolean(Recursive, "Recursive")
					.Boolean(EncodeOutput, "EncodeOutput")
					.Boolean(CreateEmpty, "CreateEmpty")
					.Boolean(DotMatches, "DotMatches")
					.Boolean(CaseSensitive, "CaseSensitive");
			}
			else
			{
				blockWriter.Literal(RegexString).Literal(RegexOutput).Boolean(Recursive, "Recursive")
					.Boolean(EncodeOutput, "EncodeOutput")
					.Boolean(CreateEmpty, "CreateEmpty")
					.Boolean(DotMatches, "DotMatches")
					.Boolean(CaseSensitive, "CaseSensitive");
			}
			break;
		}
		blockWriter.Arrow().Token(IsCapture ? "CAP" : "VAR").Literal(VariableName);
		if (!blockWriter.CheckDefault(Prefix, "Prefix") || !blockWriter.CheckDefault(Suffix, "Suffix"))
		{
			blockWriter.Literal(Prefix).Literal(Suffix);
		}
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		string input = BlockBase.ReplaceValues(parseTarget, data);
		List<string> list = new List<string>();
		switch (Type)
		{
		case ParseType.LR:
			list = Parse.LR(input, BlockBase.ReplaceValues(leftString, data), BlockBase.ReplaceValues(rightString, data), recursive, useRegexLR).ToList();
			break;
		case ParseType.CSS:
			list = Parse.CSS(input, BlockBase.ReplaceValues(cssSelector, data), BlockBase.ReplaceValues(attributeName, data), cssElementIndex, recursive).ToList();
			break;
		case ParseType.JSON:
		{
			string resolvedField = BlockBase.ReplaceValues(jsonField, data);
			// Auto-enable JToken/SelectToken mode when the field is a dot-path or contains
			// array indexers (e.g. "data.subscriptions.Items[0].name") — matches OB2 behavior.
			bool useJToken = jTokenParsing || resolvedField.Contains('.') || resolvedField.Contains('[');
			list = Parse.JSON(input, resolvedField, recursive, useJToken).ToList();
			break;
		}
		case ParseType.XPATH:
			throw new NotImplementedException("XPATH parsing is not implemented yet");
		case ParseType.REGEX:
		{
			RegexOptions regexOptions = RegexOptions.None;
			if (dotMatches)
			{
				regexOptions |= RegexOptions.Singleline;
			}
			if (!caseSensitive)
			{
				regexOptions |= RegexOptions.IgnoreCase;
			}
			list = Parse.REGEX(input, BlockBase.ReplaceValues(regexString, data), BlockBase.ReplaceValues(regexOutput, data), recursive, regexOptions).ToList();
			if (IndexOutputList > -1 && Recursive)
			{
				try
				{
					string item = list[IndexOutputList];
					list = new List<string> { item };
				}
				catch
				{
				}
			}
			break;
		}
		}
		BlockBase.InsertVariable(data, isCapture, recursive, list, variableName, prefix, suffix, encodeOutput, createEmpty);
	}
}
