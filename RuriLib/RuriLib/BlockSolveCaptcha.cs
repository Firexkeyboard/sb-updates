using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CaptchaSharp;
using CaptchaSharp.Enums;
using CaptchaSharp.Exceptions;
using CaptchaSharp.Models;
using RuriLib.Functions.Captchas;
using RuriLib.LS;

namespace RuriLib;

public class BlockSolveCaptcha : BlockBase
{
	private CaptchaType type = CaptchaType.ReCaptchaV2;

	private bool useProxy;

	private string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36";

	private string question = "";

	private CaptchaLanguageGroup languageGroup;

	private CaptchaLanguage language;

	private string base64 = "";

	private bool isPhrase;

	private bool caseSensitive;

	private CharacterSet charSet;

	private bool requiresCalculation;

	private int minLength;

	private int maxLength;

	private string textInstructions = "";

	private string siteKey = "";

	private string siteUrl = "";

	private bool isInvisible;

	private string action = "";

	private string minScore = "0.3";

	private string publicKey = "";

	private string serviceUrl = "";

	private bool noJS;

	private string userId = "";

	private string sessionId = "";

	private string webServerSign1 = "";

	private string webServerSign2 = "";

	private string gt = "";

	private string challenge = "";

	private string apiServer = "";

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

	public bool UseProxy
	{
		get
		{
			return useProxy;
		}
		set
		{
			useProxy = value;
			OnPropertyChanged("UseProxy");
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

	public string Question
	{
		get
		{
			return question;
		}
		set
		{
			question = value;
			OnPropertyChanged("Question");
		}
	}

	public CaptchaLanguageGroup LanguageGroup
	{
		get
		{
			return languageGroup;
		}
		set
		{
			languageGroup = value;
			OnPropertyChanged("LanguageGroup");
		}
	}

	public CaptchaLanguage Language
	{
		get
		{
			return language;
		}
		set
		{
			language = value;
			OnPropertyChanged("Language");
		}
	}

	public string Base64
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

	public bool IsPhrase
	{
		get
		{
			return isPhrase;
		}
		set
		{
			isPhrase = value;
			OnPropertyChanged("IsPhrase");
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

	public CharacterSet CharSet
	{
		get
		{
			return charSet;
		}
		set
		{
			charSet = value;
			OnPropertyChanged("CharSet");
		}
	}

	public bool RequiresCalculation
	{
		get
		{
			return requiresCalculation;
		}
		set
		{
			requiresCalculation = value;
			OnPropertyChanged("RequiresCalculation");
		}
	}

	public int MinLength
	{
		get
		{
			return minLength;
		}
		set
		{
			minLength = value;
			OnPropertyChanged("MinLength");
		}
	}

	public int MaxLength
	{
		get
		{
			return maxLength;
		}
		set
		{
			maxLength = value;
			OnPropertyChanged("MaxLength");
		}
	}

	public string TextInstructions
	{
		get
		{
			return textInstructions;
		}
		set
		{
			textInstructions = value;
			OnPropertyChanged("TextInstructions");
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

	public string SiteUrl
	{
		get
		{
			return siteUrl;
		}
		set
		{
			siteUrl = value;
			OnPropertyChanged("SiteUrl");
		}
	}

	public bool IsInvisible
	{
		get
		{
			return isInvisible;
		}
		set
		{
			isInvisible = value;
			OnPropertyChanged("IsInvisible");
		}
	}

	public string Action
	{
		get
		{
			return action;
		}
		set
		{
			action = value;
			OnPropertyChanged("Action");
		}
	}

	public string MinScore
	{
		get
		{
			return minScore;
		}
		set
		{
			minScore = value;
			OnPropertyChanged("MinScore");
		}
	}

	public string PublicKey
	{
		get
		{
			return publicKey;
		}
		set
		{
			publicKey = value;
			OnPropertyChanged("PublicKey");
		}
	}

	public string ServiceUrl
	{
		get
		{
			return serviceUrl;
		}
		set
		{
			serviceUrl = value;
			OnPropertyChanged("ServiceUrl");
		}
	}

	public bool NoJS
	{
		get
		{
			return noJS;
		}
		set
		{
			noJS = value;
			OnPropertyChanged("NoJS");
		}
	}

	public string UserId
	{
		get
		{
			return userId;
		}
		set
		{
			userId = value;
			OnPropertyChanged("UserId");
		}
	}

	public string SessionId
	{
		get
		{
			return sessionId;
		}
		set
		{
			sessionId = value;
			OnPropertyChanged("SessionId");
		}
	}

	public string WebServerSign1
	{
		get
		{
			return webServerSign1;
		}
		set
		{
			webServerSign1 = value;
			OnPropertyChanged("WebServerSign1");
		}
	}

	public string WebServerSign2
	{
		get
		{
			return webServerSign2;
		}
		set
		{
			webServerSign2 = value;
			OnPropertyChanged("WebServerSign2");
		}
	}

	public string GT
	{
		get
		{
			return gt;
		}
		set
		{
			gt = value;
			OnPropertyChanged("GT");
		}
	}

	public string Challenge
	{
		get
		{
			return challenge;
		}
		set
		{
			challenge = value;
			OnPropertyChanged("Challenge");
		}
	}

	public string ApiServer
	{
		get
		{
			return apiServer;
		}
		set
		{
			apiServer = value;
			OnPropertyChanged("ApiServer");
		}
	}

	public BlockSolveCaptcha()
	{
		base.Label = "SOLVE CAPTCHA";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		Type = (CaptchaType)LineParser.ParseEnum(ref input, "TYPE", typeof(CaptchaType));
		switch (Type)
		{
		case CaptchaType.TextCaptcha:
			Question = LineParser.ParseLiteral(ref input, "QUESTION");
			LanguageGroup = (CaptchaLanguageGroup)LineParser.ParseEnum(ref input, "LANG GROUP", typeof(CaptchaLanguageGroup));
			Language = (CaptchaLanguage)LineParser.ParseEnum(ref input, "LANG", typeof(CaptchaLanguage));
			break;
		case CaptchaType.ImageCaptcha:
			Base64 = LineParser.ParseLiteral(ref input, "BASE64");
			LanguageGroup = (CaptchaLanguageGroup)LineParser.ParseEnum(ref input, "LANG GROUP", typeof(CaptchaLanguageGroup));
			Language = (CaptchaLanguage)LineParser.ParseEnum(ref input, "LANG", typeof(CaptchaLanguage));
			MinLength = LineParser.ParseInt(ref input, "MIN LEN");
			MaxLength = LineParser.ParseInt(ref input, "MAX LEN");
			CharSet = (CharacterSet)LineParser.ParseEnum(ref input, "CHARSET", typeof(CharacterSet));
			TextInstructions = LineParser.ParseLiteral(ref input, "INSTRUCTIONS");
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case CaptchaType.ReCaptchaV2:
			SiteKey = LineParser.ParseLiteral(ref input, "SITE KEY");
			SiteUrl = LineParser.ParseLiteral(ref input, "SITE URL");
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case CaptchaType.ReCaptchaV3:
			SiteKey = LineParser.ParseLiteral(ref input, "SITE KEY");
			SiteUrl = LineParser.ParseLiteral(ref input, "SITE URL");
			Action = LineParser.ParseLiteral(ref input, "ACTION");
			MinScore = LineParser.ParseLiteral(ref input, "MIN SCORE");
			break;
		case CaptchaType.FunCaptcha:
			SiteUrl = LineParser.ParseLiteral(ref input, "SITE URL");
			PublicKey = LineParser.ParseLiteral(ref input, "PUBLIC KEY");
			ServiceUrl = LineParser.ParseLiteral(ref input, "SERVICE URL");
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case CaptchaType.KeyCaptcha:
			SiteUrl = LineParser.ParseLiteral(ref input, "SITE URL");
			UserId = LineParser.ParseLiteral(ref input, "USER ID");
			SessionId = LineParser.ParseLiteral(ref input, "SESSION ID");
			WebServerSign1 = LineParser.ParseLiteral(ref input, "WEBSERVER SIGN 1");
			WebServerSign2 = LineParser.ParseLiteral(ref input, "WEBSERVER SIGN 2");
			break;
		case CaptchaType.HCaptcha:
			SiteKey = LineParser.ParseLiteral(ref input, "SITE KEY");
			SiteUrl = LineParser.ParseLiteral(ref input, "SITE URL");
			break;
		case CaptchaType.GeeTest:
			SiteUrl = LineParser.ParseLiteral(ref input, "SITE URL");
			GT = LineParser.ParseLiteral(ref input, "GT");
			Challenge = LineParser.ParseLiteral(ref input, "CHALLENGE");
			ApiServer = LineParser.ParseLiteral(ref input, "API SERVER");
			break;
		case CaptchaType.Capy:
			SiteKey = LineParser.ParseLiteral(ref input, "SITE KEY");
			SiteUrl = LineParser.ParseLiteral(ref input, "SITE URL");
			break;
		}
		while (LineParser.Lookahead(ref input) == TokenType.Boolean)
		{
			LineParser.SetBool(ref input, this);
		}
		if (LineParser.Lookahead(ref input) == TokenType.Literal)
		{
			UserAgent = LineParser.ParseLiteral(ref input, "USER AGENT");
		}
		return this;
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("SOLVECAPTCHA").Token(Type);
		switch (Type)
		{
		case CaptchaType.TextCaptcha:
			blockWriter.Literal(Question).Token(LanguageGroup).Token(Language);
			break;
		case CaptchaType.ImageCaptcha:
			blockWriter.Literal(Base64).Token(LanguageGroup).Token(Language)
				.Integer(MinLength)
				.Integer(MaxLength)
				.Token(CharSet)
				.Literal(TextInstructions)
				.Boolean(IsPhrase, "IsPhrase")
				.Boolean(CaseSensitive, "CaseSensitive")
				.Boolean(RequiresCalculation, "RequiresCalculation");
			break;
		case CaptchaType.ReCaptchaV2:
			blockWriter.Literal(SiteKey).Literal(SiteUrl).Boolean(IsInvisible, "IsInvisible");
			break;
		case CaptchaType.ReCaptchaV3:
			blockWriter.Literal(SiteKey).Literal(SiteUrl).Literal(Action)
				.Literal(MinScore);
			break;
		case CaptchaType.FunCaptcha:
			blockWriter.Literal(SiteUrl).Literal(PublicKey).Literal(ServiceUrl)
				.Boolean(NoJS, "NoJS");
			break;
		case CaptchaType.KeyCaptcha:
			blockWriter.Literal(SiteUrl).Literal(UserId).Literal(SessionId)
				.Literal(WebServerSign1)
				.Literal(WebServerSign2);
			break;
		case CaptchaType.HCaptcha:
		case CaptchaType.Capy:
			blockWriter.Literal(SiteKey).Literal(SiteUrl);
			break;
		case CaptchaType.GeeTest:
			blockWriter.Literal(SiteUrl).Literal(GT).Literal(Challenge)
				.Literal(ApiServer);
			break;
		}
		blockWriter.Boolean(UseProxy, "UseProxy").Literal(UserAgent, "UserAgent");
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		CaptchaService service = Captchas.GetService(data.GlobalSettings.Captchas);
		Proxy proxy = ((!data.UseProxies || !UseProxy) ? null : (proxy = new Proxy
		{
			Host = data.Proxy.Host,
			Port = int.Parse(data.Proxy.Port),
			Type = (ProxyType)Enum.Parse(typeof(ProxyType), data.Proxy.Type.ToString()),
			Username = data.Proxy.Username,
			Password = data.Proxy.Password,
			UserAgent = UserAgent,
			Cookies = (from p in data.Cookies.ToList().Concat(data.GlobalCookies.ToList())
				select (Key: p.Key, Value: p.Value)).ToArray()
		}));
		if (!data.GlobalSettings.Captchas.BypassBalanceCheck)
		{
			try
			{
				try
				{
					data.Balance = service.GetBalanceAsync().Result;
					data.Log($"[{data.GlobalSettings.Captchas.CurrentService}] Balance: ${data.Balance}");
					if (data.Balance < 0.002m)
					{
						throw new Exception("The remaining balance is too low!");
					}
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
			catch (BadAuthenticationException ex2)
			{
				data.Log(new LogEntry("Bad credentials! " + ex2.Message, Colors.Tomato));
				return;
			}
			catch (Exception ex3)
			{
				data.Log(new LogEntry("An error occurred! " + ex3.Message, Colors.Tomato));
			}
		}
		string logString;
		try
		{
			try
			{
				CaptchaResponse response = GetResponse(service, data, proxy);
				BlockBase.InsertVariable(data, isCapture: false, response.Id.ToString(), "CAPTCHAID");
				if (!(response is StringResponse stringResponse))
				{
					if (response is GeeTestResponse geeTestResponse)
					{
						BlockBase.InsertVariable(data, isCapture: false, geeTestResponse.Challenge, "GT_CHALLENGE");
						BlockBase.InsertVariable(data, isCapture: false, geeTestResponse.Validate, "GT_VALIDATE");
						BlockBase.InsertVariable(data, isCapture: false, geeTestResponse.SecCode, "GT_SECCODE");
						data.Log(new LogEntry($"Captcha solved successfully! Id: {geeTestResponse.Id} Challenge: {geeTestResponse.Challenge}\r\nValidate: {geeTestResponse.Validate}\r\nSecCode: {geeTestResponse.SecCode}", Colors.GreenYellow));
					}
				}
				else
				{
					BlockBase.InsertVariable(data, isCapture: false, stringResponse.Response, "SOLUTION");
					data.Log(new LogEntry($"Captcha solved successfully! Id: {stringResponse.Id} Solution: {stringResponse.Response}", Colors.GreenYellow));
				}
				return;
			}
			catch (Exception ex4)
			{
				if (ex4 is AggregateException)
				{
					throw ex4.InnerException;
				}
				throw;
			}
		}
		catch (NotSupportedException ex5)
		{
			logString = $"The currently selected service ({data.GlobalSettings.Captchas.CurrentService}) does not support this task! {ex5.Message}";
		}
		catch (TaskCreationException ex6)
		{
			logString = "Could not create the captcha task! " + ex6.Message;
		}
		catch (TaskSolutionException ex7)
		{
			logString = "Could not solve the captcha! " + ex7.Message;
		}
		catch (Exception ex8)
		{
			logString = "An error occurred! " + ex8.Message;
		}
		data.Log(new LogEntry(logString, Colors.Tomato));
	}

	private CaptchaResponse GetResponse(CaptchaService service, BotData data, Proxy proxy)
	{
		return Type switch
		{
			CaptchaType.TextCaptcha => service.SolveTextCaptchaAsync(BlockBase.ReplaceValues(Question, data), new TextCaptchaOptions
			{
				CaptchaLanguage = Language,
				CaptchaLanguageGroup = LanguageGroup
			}).Result, 
			CaptchaType.ImageCaptcha => service.SolveImageCaptchaAsync(BlockBase.ReplaceValues(Base64, data), new ImageCaptchaOptions
			{
				CaptchaLanguage = Language,
				CaptchaLanguageGroup = LanguageGroup,
				IsPhrase = IsPhrase,
				CaseSensitive = CaseSensitive,
				RequiresCalculation = RequiresCalculation,
				CharacterSet = CharSet,
				MinLength = MinLength,
				MaxLength = MaxLength,
				TextInstructions = BlockBase.ReplaceValues(TextInstructions, data)
			}).Result, 
			CaptchaType.ReCaptchaV2 => service.SolveRecaptchaV2Async(BlockBase.ReplaceValues(SiteKey, data), BlockBase.ReplaceValues(SiteUrl, data), IsInvisible, proxy).Result, 
			CaptchaType.ReCaptchaV3 => service.SolveRecaptchaV3Async(BlockBase.ReplaceValues(SiteKey, data), BlockBase.ReplaceValues(SiteUrl, data), BlockBase.ReplaceValues(Action, data), float.Parse(BlockBase.ReplaceValues(MinScore, data)), proxy).Result, 
			CaptchaType.FunCaptcha => service.SolveFuncaptchaAsync(BlockBase.ReplaceValues(PublicKey, data), BlockBase.ReplaceValues(ServiceUrl, data), BlockBase.ReplaceValues(SiteUrl, data), NoJS, proxy).Result, 
			CaptchaType.HCaptcha => service.SolveHCaptchaAsync(BlockBase.ReplaceValues(SiteKey, data), BlockBase.ReplaceValues(SiteUrl, data), proxy).Result, 
			CaptchaType.Capy => service.SolveCapyAsync(BlockBase.ReplaceValues(SiteKey, data), BlockBase.ReplaceValues(SiteUrl, data), proxy).Result, 
			CaptchaType.KeyCaptcha => service.SolveKeyCaptchaAsync(BlockBase.ReplaceValues(UserId, data), BlockBase.ReplaceValues(SessionId, data), BlockBase.ReplaceValues(WebServerSign1, data), BlockBase.ReplaceValues(WebServerSign2, data), BlockBase.ReplaceValues(SiteUrl, data), proxy).Result, 
			CaptchaType.GeeTest => service.SolveGeeTestAsync(BlockBase.ReplaceValues(GT, data), BlockBase.ReplaceValues(Challenge, data), BlockBase.ReplaceValues(ApiServer, data), BlockBase.ReplaceValues(SiteUrl, data), proxy).Result, 
			_ => throw new NotSupportedException(), 
		};
	}
}
