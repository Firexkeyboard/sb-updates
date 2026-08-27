using RuriLib.LS;

namespace RuriLib;

public class BlockSpeechToText : BlockBase
{
	private string variableName = "";

	private bool isCapture;

	private string url = "";

	private string lang = "en-US";

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

	public string Url
	{
		get
		{
			return url;
		}
		set
		{
			url = value.Replace("\n", "");
			OnPropertyChanged("Url");
		}
	}

	public string Lang
	{
		get
		{
			return lang;
		}
		set
		{
			lang = value;
			OnPropertyChanged("Lang");
		}
	}

	public BlockSpeechToText()
	{
		base.Label = "SPEECHTOTEXT";
	}

	public override void Process(BotData data)
	{
		base.Process(data);
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("SPEECHTOTEXT").Token(Lang)
			.Literal(Url);
		if (!blockWriter.CheckDefault(VariableName, "VariableName"))
		{
			blockWriter.Arrow().Token(IsCapture ? "CAP" : "VAR").Literal(VariableName)
				.Indent();
		}
		return blockWriter.ToString();
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		Lang = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false);
		Url = LineParser.ParseLiteral(ref input, "Url");
		return this;
	}
}
