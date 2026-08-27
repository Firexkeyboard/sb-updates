using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media;
using RuriLib.Functions.Captchas;
using RuriLib.Functions.Download;
using RuriLib.LS;
using RuriLib.Models;

namespace RuriLib;

[Obsolete]
public class BlockImageCaptcha : BlockCaptcha
{
	private string url = "";

	private string variableName = "";

	private bool base64;

	private bool sendScreenshot;

	private string userAgent = "";

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

	public bool Base64
	{
		get
		{
			return base64;
		}
		set
		{
			base64 = value;
			OnPropertyChanged("Base64");
		}
	}

	public bool SendScreenshot
	{
		get
		{
			return sendScreenshot;
		}
		set
		{
			sendScreenshot = value;
			OnPropertyChanged("SendScreenshot");
		}
	}

	public string UserAgent
	{
		get
		{
			return userAgent;
		}
		set
		{
			userAgent = value;
			OnPropertyChanged("UserAgent");
		}
	}

	public BlockImageCaptcha()
	{
		base.Label = "CAPTCHA";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		Url = LineParser.ParseLiteral(ref input, "URL");
		if (LineParser.Lookahead(ref input) == TokenType.Literal)
		{
			UserAgent = LineParser.ParseLiteral(ref input, "UserAgent");
		}
		while (LineParser.Lookahead(ref input) == TokenType.Boolean)
		{
			LineParser.SetBool(ref input, this);
		}
		LineParser.EnsureIdentifier(ref input, "->");
		LineParser.EnsureIdentifier(ref input, "VAR");
		VariableName = LineParser.ParseLiteral(ref input, "VARIABLE NAME");
		return this;
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("CAPTCHA").Literal(Url);
		if (UserAgent != string.Empty)
		{
			blockWriter.Literal(UserAgent);
		}
		blockWriter.Boolean(Base64, "Base64").Boolean(SendScreenshot, "SendScreenshot").Arrow()
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
		string s = BlockBase.ReplaceValues(url, data);
		data.Log(new LogEntry("WARNING! This block is obsolete and WILL BE REMOVED IN THE FUTURE! Use the SOLVECAPTCHA block!", Colors.Tomato));
		data.Log(new LogEntry("Downloading image...", Colors.White));
		string text = $"UserData/Captchas/captcha{data.BotNumber}.jpg";
		if (base64)
		{
			byte[] array = Convert.FromBase64String(s);
			using FileStream fileStream = new FileStream(text, FileMode.Create);
			fileStream.Write(array, 0, array.Length);
			fileStream.Flush();
		}
		else if (sendScreenshot && data.Screenshots.Count > 0)
		{
			new Bitmap(data.Screenshots.Last()).Save(text);
		}
		else
		{
			try
			{
				Download.RemoteFile(text, s, data.UseProxies, data.Proxy, data.Cookies, out var newCookies, data.GlobalSettings.General.RequestTimeout * 1000, BlockBase.ReplaceValues(UserAgent, data));
				data.Cookies = newCookies;
			}
			catch (Exception ex)
			{
				data.Log(new LogEntry(ex.Message, Colors.Tomato));
				throw;
			}
		}
		string text2 = "";
		Bitmap bitmap = new Bitmap(text);
		try
		{
			byte[] inArray = (byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[]));
			text2 = Captchas.GetService(data.GlobalSettings.Captchas).SolveImageCaptchaAsync(Convert.ToBase64String(inArray)).Result.Response;
		}
		catch (Exception ex2)
		{
			data.Log(new LogEntry(ex2.Message, Colors.Tomato));
			throw;
		}
		finally
		{
			bitmap.Dispose();
		}
		data.Log((text2 == string.Empty) ? new LogEntry("Couldn't get a response from the service", Colors.Tomato) : new LogEntry("Succesfully got the response: " + text2, Colors.GreenYellow));
		if (VariableName != string.Empty)
		{
			data.Log(new LogEntry("Response stored in variable: " + variableName, Colors.White));
			data.Variables.Set(new CVar(variableName, text2));
		}
	}
}
