using System;
using System.Collections.Generic;
using System.Windows.Media;
using RuriLib.LS;

namespace RuriLib;

public class SBlockExecuteJS : BlockBase
{
	private string javascriptCode = "alert('henlo');";

	private string outputVariable = "";

	private bool isCapture;

	public string JavascriptCode
	{
		get
		{
			return javascriptCode;
		}
		set
		{
			javascriptCode = value;
			OnPropertyChanged("JavascriptCode");
		}
	}

	public string OutputVariable
	{
		get
		{
			return outputVariable;
		}
		set
		{
			outputVariable = value;
			OnPropertyChanged("OutputVariable");
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

	public SBlockExecuteJS()
	{
		base.Label = "EXECUTE JS";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		JavascriptCode = LineParser.ParseLiteral(ref input, "SCRIPT");
		if (LineParser.ParseToken(ref input, TokenType.Arrow, essential: false) == string.Empty)
		{
			return this;
		}
		try
		{
			string text = LineParser.ParseToken(ref input, TokenType.Parameter, essential: true);
			if (text.ToUpper() == "VAR" || text.ToUpper() == "CAP")
			{
				IsCapture = text.ToUpper() == "CAP";
			}
		}
		catch
		{
			throw new ArgumentException("Invalid or missing variable type");
		}
		try
		{
			OutputVariable = LineParser.ParseToken(ref input, TokenType.Literal, essential: true);
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
		blockWriter.Label(base.Label).Token("EXECUTEJS").Literal(JavascriptCode.Replace("\r\n", " ").Replace("\n", " "));
		if (!blockWriter.CheckDefault(OutputVariable, "OutputVariable"))
		{
			blockWriter.Arrow().Token(IsCapture ? "CAP" : "VAR").Literal(OutputVariable);
		}
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		if (data.Driver == null)
		{
			data.Log(new LogEntry("Open a browser first!", Colors.White));
			throw new Exception("Browser not open");
		}
		data.Log(new LogEntry("Executing JS code!", Colors.White));
		object obj = data.Driver.ExecuteScript(BlockBase.ReplaceValues(javascriptCode, data));
		if (obj != null)
		{
			try
			{
				BlockBase.InsertVariable(data, isCapture, recursive: false, new List<string> { obj.ToString() }, outputVariable);
			}
			catch
			{
				throw new Exception("Failed to convert the returned value to a string");
			}
		}
		data.Log(new LogEntry("... executed!", Colors.White));
		BlockBase.UpdateSeleniumData(data);
	}
}
