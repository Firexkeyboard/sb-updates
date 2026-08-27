using System;
using System.Windows.Media;
using OpenQA.Selenium;
using RuriLib.LS;

namespace RuriLib;

public class SBlockNavigate : BlockBase
{
	private string url = "https://example.com";

	private int timeout = 60;

	private bool banOnTimeout = true;

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

	public int Timeout
	{
		get
		{
			return timeout;
		}
		set
		{
			timeout = value;
			OnPropertyChanged("Timeout");
		}
	}

	public bool BanOnTimeout
	{
		get
		{
			return banOnTimeout;
		}
		set
		{
			banOnTimeout = value;
			OnPropertyChanged("BanOnTimeout");
		}
	}

	public SBlockNavigate()
	{
		base.Label = "NAVIGATE";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		Url = LineParser.ParseLiteral(ref input, "URL");
		if (LineParser.Lookahead(ref input) == TokenType.Integer)
		{
			Timeout = LineParser.ParseInt(ref input, "TIMEOUT");
		}
		if (LineParser.Lookahead(ref input) == TokenType.Boolean)
		{
			LineParser.SetBool(ref input, this);
		}
		return this;
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("NAVIGATE").Literal(Url)
			.Integer(Timeout, "Timeout")
			.Boolean(BanOnTimeout, "BanOnTimeout");
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
		data.Log(new LogEntry("Navigating to " + BlockBase.ReplaceValues(url, data), Colors.White));
		data.Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(timeout);
		try
		{
			data.Driver.Navigate().GoToUrl(BlockBase.ReplaceValues(url, data));
			data.Log(new LogEntry("Navigated!", Colors.White));
		}
		catch (WebDriverTimeoutException)
		{
			data.Log(new LogEntry("Timeout on Page Load", Colors.Tomato));
			if (BanOnTimeout)
			{
				data.Status = BotStatus.BAN;
			}
		}
		data.Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(data.GlobalSettings.Selenium.PageLoadTimeout);
		BlockBase.UpdateSeleniumData(data);
	}
}
