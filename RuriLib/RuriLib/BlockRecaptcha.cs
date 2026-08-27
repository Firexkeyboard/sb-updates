using System;
using System.Windows.Media;
using RuriLib.Functions.Captchas;
using RuriLib.LS;
using RuriLib.Models;

namespace RuriLib;

[Obsolete]
public class BlockRecaptcha : BlockCaptcha
{
	private string variableName = "";

	private string url = "https://google.com";

	private string siteKey = "";

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

	public string Url
	{
		get
		{
			return url;
		}
		set
		{
			url = value;
			OnPropertyChanged("Url");
		}
	}

	public string SiteKey
	{
		get
		{
			return siteKey;
		}
		set
		{
			siteKey = value;
			OnPropertyChanged("SiteKey");
		}
	}

	public BlockRecaptcha()
	{
		base.Label = "RECAPTCHA";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		Url = LineParser.ParseLiteral(ref input, "URL");
		SiteKey = LineParser.ParseLiteral(ref input, "SITEKEY");
		if (LineParser.ParseToken(ref input, TokenType.Arrow, essential: false) == string.Empty)
		{
			return this;
		}
		LineParser.EnsureIdentifier(ref input, "VAR");
		VariableName = LineParser.ParseLiteral(ref input, "VARIABLE NAME");
		return this;
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("RECAPTCHA").Literal(Url)
			.Literal(SiteKey)
			.Arrow()
			.Token("VAR")
			.Literal(VariableName);
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		if (!data.GlobalSettings.Captchas.BypassBalanceCheck)
		{
			base.Process(data);
		}
		data.Log(new LogEntry("WARNING! This block is obsolete and WILL BE REMOVED IN THE FUTURE! Use the SOLVECAPTCHA block!", Colors.Tomato));
		data.Log(new LogEntry("Solving reCaptcha...", Colors.White));
		string text = "";
		try
		{
			text = Captchas.GetService(data.GlobalSettings.Captchas).SolveRecaptchaV2Async(BlockBase.ReplaceValues(siteKey, data), BlockBase.ReplaceValues(url, data), string.Empty).Result.Response;
		}
		catch
		{
			data.Log((text == string.Empty) ? new LogEntry("Couldn't get a reCaptcha response from the service", Colors.Tomato) : new LogEntry("Succesfully got the response: " + text, Colors.GreenYellow));
		}
		if (VariableName != string.Empty)
		{
			data.Log(new LogEntry("Response stored in variable: " + variableName, Colors.White));
			data.Variables.Set(new CVar(variableName, text));
		}
	}
}
