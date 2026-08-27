using System;
using System.Windows.Media;
using CaptchaSharp;
using CaptchaSharp.Enums;
using CaptchaSharp.Exceptions;
using RuriLib.Functions.Captchas;
using RuriLib.LS;

namespace RuriLib;

public class BlockReportCaptcha : BlockBase
{
	private CaptchaType type = CaptchaType.ImageCaptcha;

	private string captchaId = "<CAPTCHAID>";

	public CaptchaType Type
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

	public string CaptchaId
	{
		get
		{
			return captchaId;
		}
		set
		{
			captchaId = value;
			OnPropertyChanged("CaptchaId");
		}
	}

	public BlockReportCaptcha()
	{
		base.Label = "REPORT CAPTCHA";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		Type = (CaptchaType)LineParser.ParseEnum(ref input, "TYPE", typeof(CaptchaType));
		CaptchaId = LineParser.ParseLiteral(ref input, "CAPTCHA ID");
		return this;
	}

	public override string ToLS(bool indent = true)
	{
		return new BlockWriter(GetType(), indent, base.Disabled).Label(base.Label).Token("REPORTCAPTCHA").Token(Type)
			.Literal(CaptchaId)
			.ToString();
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		CaptchaService service = Captchas.GetService(data.GlobalSettings.Captchas);
		string logString;
		try
		{
			try
			{
				string s = BlockBase.ReplaceValues(CaptchaId, data);
				service.ReportSolution(long.Parse(s), Type).Wait();
				data.Log(new LogEntry("Captcha reported successfully!", Colors.GreenYellow));
				return;
			}
			catch (Exception ex)
			{
				if (ex is AggregateException)
				{
					throw ex.InnerException;
				}
				throw;
			}
		}
		catch (TaskReportException ex2)
		{
			logString = "The captcha report was not accepted! " + ex2.Message;
		}
		catch (NotSupportedException ex3)
		{
			logString = $"The currently selected service ({data.GlobalSettings.Captchas.CurrentService}) does not support reports! {ex3.Message}";
		}
		catch (Exception ex4)
		{
			logString = "An error occurred! " + ex4.Message;
		}
		data.Log(new LogEntry(logString, Colors.Tomato));
	}
}
