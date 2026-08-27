using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows.Media;
using CloudflareSolverRe;
using CloudflareSolverRe.Types;
using RuriLib.Functions.Captchas;
using RuriLib.Functions.Requests;
using RuriLib.LS;

namespace RuriLib;

public class BlockBypassCF : BlockBase
{
	private string url = "";

	private string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36";

	private bool printResponseInfo = true;

	private bool autoRedirect;

	private SecurityProtocol securityProtocol;

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

	public bool PrintResponseInfo
	{
		get
		{
			return printResponseInfo;
		}
		set
		{
			printResponseInfo = value;
			OnPropertyChanged("PrintResponseInfo");
		}
	}

	public bool AutoRedirect
	{
		get
		{
			return autoRedirect;
		}
		set
		{
			autoRedirect = value;
			OnPropertyChanged("AutoRedirect");
		}
	}

	public SecurityProtocol SecurityProtocol
	{
		get
		{
			return securityProtocol;
		}
		set
		{
			securityProtocol = value;
			OnPropertyChanged("SecurityProtocol");
		}
	}

	public BlockBypassCF()
	{
		base.Label = "BYPASS CF";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		Url = LineParser.ParseLiteral(ref input, "URL");
		if (input != "" && LineParser.Lookahead(ref input) == TokenType.Literal)
		{
			UserAgent = LineParser.ParseLiteral(ref input, "UA");
		}
		if (input != "" && LineParser.ParseToken(ref input, TokenType.Parameter, essential: false, proceed: false) == "SECPROTO")
		{
			LineParser.ParseToken(ref input, TokenType.Parameter, essential: true);
			SecurityProtocol = LineParser.ParseEnum(ref input, "Security Protocol", typeof(SecurityProtocol));
		}
		while (input != "")
		{
			LineParser.SetBool(ref input, this);
		}
		return this;
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("BYPASSCF").Literal(Url)
			.Literal(UserAgent, "UserAgent");
		if (SecurityProtocol != 0)
		{
			blockWriter.Token("SECPROTO").Token(SecurityProtocol);
		}
		blockWriter.Boolean(PrintResponseInfo, "PrintResponseInfo").Boolean(AutoRedirect, "AutoRedirect");
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		if (data.UseProxies && data.Proxy.Clearance != "" && !data.GlobalSettings.Proxies.AlwaysGetClearance)
		{
			data.Log(new LogEntry("Skipping CF Bypass because there is already a valid cookie", Colors.White));
			data.Cookies["cf_clearance"] = data.Proxy.Clearance;
			data.Cookies["__cfduid"] = data.Proxy.Cfduid;
			return;
		}
		Uri uri = new Uri(BlockBase.ReplaceValues(url, data));
		CloudflareSolver cloudflareSolver = new CloudflareSolver(new CaptchaSharpProvider(Captchas.GetService(data.GlobalSettings.Captchas)), BlockBase.ReplaceValues(UserAgent, data))
		{
			ClearanceDelay = 3000,
			MaxCaptchaTries = 1,
			MaxTries = 3
		};
		CookieContainer cookieContainer = new CookieContainer();
		foreach (KeyValuePair<string, string> cookie3 in data.Cookies)
		{
			cookieContainer.Add(new Cookie(cookie3.Key, cookie3.Value, "/", uri.Host));
		}
		HttpClientHandler val = new HttpClientHandler
		{
			AllowAutoRedirect = AutoRedirect,
			CookieContainer = cookieContainer,
			AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate),
			SslProtocols = SecurityProtocol.ToSslProtocols()
		};
		if (data.UseProxies)
		{
			if (data.Proxy.Type != 0)
			{
				throw new Exception($"The proxy type {data.Proxy.Type} is not supported by this block yet");
			}
			val.Proxy = new WebProxy(data.Proxy.Proxy, BypassOnLocal: false);
			val.UseProxy = true;
			if (!string.IsNullOrEmpty(data.Proxy.Username))
			{
				val.Proxy.Credentials = new NetworkCredential(data.Proxy.Username, data.Proxy.Password);
			}
		}
		HttpClient val2 = new HttpClient((HttpMessageHandler)(object)val);
		val2.Timeout = TimeSpan.FromSeconds(data.GlobalSettings.General.RequestTimeout);
		((HttpHeaders)val2.DefaultRequestHeaders).Add("User-Agent", BlockBase.ReplaceValues(UserAgent, data));
		SolveResult solveResult = default(SolveResult);
		try
		{
			solveResult = cloudflareSolver.Solve(val2, val, uri, BlockBase.ReplaceValues(UserAgent, data)).Result;
		}
		catch (AggregateException ex)
		{
			string text = string.Join(Environment.NewLine, ex.InnerExceptions.Select((Exception e) => e.Message));
			if (data.ConfigSettings.IgnoreResponseErrors)
			{
				data.Log(new LogEntry(text, Colors.Tomato));
				data.ResponseCode = text;
				return;
			}
			throw new Exception(text);
		}
		catch (Exception ex2)
		{
			if (data.ConfigSettings.IgnoreResponseErrors)
			{
				data.Log(new LogEntry(ex2.Message, Colors.Tomato));
				data.ResponseSource = ex2.Message;
				return;
			}
			throw;
		}
		if (solveResult.Success)
		{
			data.Log(new LogEntry($"[Success] Protection bypassed: {solveResult.DetectResult.Protection}", Colors.GreenYellow));
		}
		else
		{
			if (solveResult.DetectResult.Protection != CloudflareProtection.Unknown)
			{
				string text2 = "CF Bypass Failed: " + solveResult.FailReason;
				if (data.ConfigSettings.IgnoreResponseErrors)
				{
					data.Log(new LogEntry(text2, Colors.Tomato));
					data.ResponseSource = text2;
					return;
				}
				throw new Exception(text2);
			}
			data.Log(new LogEntry("Unknown protection, skipping the bypass!", Colors.Tomato));
		}
		HttpResponseMessage val3 = null;
		try
		{
			val3 = val2.GetAsync(uri).Result;
		}
		catch (Exception ex3)
		{
			if (data.ConfigSettings.IgnoreResponseErrors)
			{
				data.ResponseSource = ex3.Message;
				return;
			}
			throw new Exception(ex3.Message);
		}
		finally
		{
			((HttpMessageHandler)val).Dispose();
			((HttpMessageInvoker)val2).Dispose();
		}
		string result = val3.Content.ReadAsStringAsync().Result;
		string text3 = "";
		string text4 = "";
		foreach (Cookie cookie4 in cookieContainer.GetCookies(uri))
		{
			string name = cookie4.Name;
			if (!(name == "cf_clearance"))
			{
				if (name == "__cfduid")
				{
					text4 = cookie4.Value;
				}
			}
			else
			{
				text3 = cookie4.Value;
			}
		}
		if (data.UseProxies)
		{
			data.Proxy.Clearance = text3;
			data.Proxy.Cfduid = text4;
		}
		if (text3 != "")
		{
			data.Log(new LogEntry("Got Cloudflare clearance!", Colors.GreenYellow));
			data.Log(new LogEntry(text3 + Environment.NewLine + text4 + Environment.NewLine, Colors.White));
		}
		data.Address = val3.RequestMessage.RequestUri.AbsoluteUri;
		if (PrintResponseInfo)
		{
			data.Log(new LogEntry("Address: " + data.Address, Colors.Cyan));
		}
		data.ResponseCode = ((int)val3.StatusCode).ToString();
		if (PrintResponseInfo)
		{
			data.Log(new LogEntry("Response code: " + data.ResponseCode, Colors.Cyan));
		}
		if (PrintResponseInfo)
		{
			data.Log(new LogEntry("Received headers:", Colors.DeepPink));
		}
		data.ResponseHeaders.Clear();
		foreach (KeyValuePair<string, IEnumerable<string>> item in (HttpHeaders)val3.Headers)
		{
			KeyValuePair<string, string> keyValuePair = new KeyValuePair<string, string>(item.Key, item.Value.First());
			data.ResponseHeaders.Add(keyValuePair.Key, keyValuePair.Value);
			if (PrintResponseInfo)
			{
				data.Log(new LogEntry(keyValuePair.Key + ": " + keyValuePair.Value, Colors.LightPink));
			}
		}
		if (!data.ResponseHeaders.ContainsKey("Content-Length"))
		{
			if (data.ResponseHeaders.ContainsKey("Content-Encoding") && data.ResponseHeaders["Content-Encoding"].Contains("gzip"))
			{
				data.ResponseHeaders["Content-Length"] = GZip.Zip(result).Length.ToString();
			}
			else
			{
				data.ResponseHeaders["Content-Length"] = result.Length.ToString();
			}
			if (PrintResponseInfo)
			{
				data.Log(new LogEntry("Content-Length: " + data.ResponseHeaders["Content-Length"], Colors.LightPink));
			}
		}
		if (PrintResponseInfo)
		{
			data.Log(new LogEntry("Received cookies:", Colors.Goldenrod));
		}
		foreach (Cookie cookie5 in cookieContainer.GetCookies(uri))
		{
			data.Cookies[cookie5.Name] = cookie5.Value;
			if (PrintResponseInfo)
			{
				data.Log(new LogEntry(cookie5.Name + ": " + cookie5.Value, Colors.LightGoldenrodYellow));
			}
		}
		data.ResponseSource = result;
		if (PrintResponseInfo)
		{
			data.Log(new LogEntry("Response Source:", Colors.Green));
			data.Log(new LogEntry(data.ResponseSource, Colors.GreenYellow));
		}
	}
}
