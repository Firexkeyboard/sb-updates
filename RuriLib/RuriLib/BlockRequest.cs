using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ExHttpMethod = RuriLib.Functions.Requests.HttpMethod;
using RuriLib.Functions.Files;
using RuriLib.Functions.Requests;
using RuriLib.LS;

namespace RuriLib;

public class BlockRequest : BlockBase
{
	private string url = "https://google.com";

	private RequestType requestType;

	private string authUser = "";

	private string authPass = "";

	private string postData = "";

	private string rawData = "";

	public byte[] RawBytes { get; set; }

	private ExHttpMethod method;

	private SecurityProtocol securityProtocol;

	private string contentType = "application/x-www-form-urlencoded";

	private bool autoRedirect = true;

	private bool readResponseSource = true;

	private bool encodeContent;

	private bool acceptEncoding = true;

	private bool allowEmptyHeaderValues;

	private string multipartBoundary = "";

	private ResponseType responseType;

	private string downloadPath = "";

	private string outputVariable = "";

	private bool saveAsScreenshot;

	private Version protocolVersion = new Version(1, 1);

	private bool useAkamai;

	private string urlSensor = "";

	private string n4sAuth = "Basic ";

	private string dataSession = "";

	private string sensorDataOut = "SENSORDATA";

	// ── New request options ──────────────────────────────────────────────────
	private HttpLibrary httpLibrary = HttpLibrary.SystemNet;
	private bool ignoreCertificateValidation = true;
	private bool alwaysSendContent = false;
	private string codePagesEncoding = "";
	private bool useCustomCipherSuites = false;
	private string customCipherSuites =
		"TLS_AES_128_GCM_SHA256\n" +
		"TLS_CHACHA20_POLY1305_SHA256\n" +
		"TLS_AES_256_GCM_SHA384\n" +
		"TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256\n" +
		"TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256\n" +
		"TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256\n" +
		"TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256";

	// Timing & retry
	private int requestTimeoutMs = 0;
	private int retryCount = 0;
	private int retryDelayMs = 1000;
	// Cookie jar control
	private bool saveResponseCookies = true;
	private bool loadRequestCookies = true;
	// CurlImpersonate
	private CurlImpersonateBrowserProfile curlImpersonateProfile = CurlImpersonateBrowserProfile.Chrome142;

	public HttpLibrary HttpLibrary
	{
		get => httpLibrary;
		set { httpLibrary = value; OnPropertyChanged("HttpLibrary"); }
	}

	public bool IgnoreCertificateValidation
	{
		get => ignoreCertificateValidation;
		set { ignoreCertificateValidation = value; OnPropertyChanged("IgnoreCertificateValidation"); }
	}

	public bool AlwaysSendContent
	{
		get => alwaysSendContent;
		set { alwaysSendContent = value; OnPropertyChanged("AlwaysSendContent"); }
	}

	public string CodePagesEncoding
	{
		get => codePagesEncoding;
		set { codePagesEncoding = value; OnPropertyChanged("CodePagesEncoding"); }
	}

	public bool UseCustomCipherSuites
	{
		get => useCustomCipherSuites;
		set { useCustomCipherSuites = value; OnPropertyChanged("UseCustomCipherSuites"); }
	}

	public string CustomCipherSuites
	{
		get => customCipherSuites;
		set { customCipherSuites = value; OnPropertyChanged("CustomCipherSuites"); }
	}

	public int RequestTimeoutMs
	{
		get => requestTimeoutMs;
		set { requestTimeoutMs = value; OnPropertyChanged("RequestTimeoutMs"); }
	}
	public int RetryCount
	{
		get => retryCount;
		set { retryCount = value; OnPropertyChanged("RetryCount"); }
	}
	public int RetryDelayMs
	{
		get => retryDelayMs;
		set { retryDelayMs = value; OnPropertyChanged("RetryDelayMs"); }
	}
	public bool SaveResponseCookies
	{
		get => saveResponseCookies;
		set { saveResponseCookies = value; OnPropertyChanged("SaveResponseCookies"); }
	}
	public bool LoadRequestCookies
	{
		get => loadRequestCookies;
		set { loadRequestCookies = value; OnPropertyChanged("LoadRequestCookies"); }
	}

	public CurlImpersonateBrowserProfile CurlImpersonateProfile
	{
		get => curlImpersonateProfile;
		set { curlImpersonateProfile = value; OnPropertyChanged("CurlImpersonateProfile"); }
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

	public RequestType RequestType
	{
		get
		{
			return requestType;
		}
		set
		{
			requestType = value;
			OnPropertyChanged("RequestType");
		}
	}

	public string AuthUser
	{
		get
		{
			return authUser;
		}
		set
		{
			authUser = value;
			OnPropertyChanged("AuthUser");
		}
	}

	public string AuthPass
	{
		get
		{
			return authPass;
		}
		set
		{
			authPass = value;
			OnPropertyChanged("AuthPass");
		}
	}

	public string PostData
	{
		get
		{
			return postData;
		}
		set
		{
			postData = value;
			OnPropertyChanged("PostData");
		}
	}

	public string RawData
	{
		get
		{
			return rawData;
		}
		set
		{
			rawData = value;
			OnPropertyChanged("RawData");
		}
	}

	public ExHttpMethod Method
	{
		get
		{
			return method;
		}
		set
		{
			method = value;
			OnPropertyChanged("Method");
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

	public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>
	{
		{ "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36" },
		{ "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7" },
		{ "Accept-Language", "en-US,en;q=0.9" },
		{ "Sec-Ch-Ua", "\"Chromium\";v=\"142\", \"Not?A_Brand\";v=\"99\", \"Google Chrome\";v=\"142\"" },
		{ "Sec-Ch-Ua-Mobile", "?0" },
		{ "Sec-Ch-Ua-Platform", "\"Windows\"" }
	};

	public Dictionary<string, string> CustomCookies { get; set; } = new Dictionary<string, string>();

	public string ContentType
	{
		get
		{
			return contentType;
		}
		set
		{
			contentType = value;
			OnPropertyChanged("ContentType");
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

	public bool ReadResponseSource
	{
		get
		{
			return readResponseSource;
		}
		set
		{
			readResponseSource = value;
			OnPropertyChanged("ReadResponseSource");
		}
	}

	public bool EncodeContent
	{
		get
		{
			return encodeContent;
		}
		set
		{
			encodeContent = value;
			OnPropertyChanged("EncodeContent");
		}
	}

	public bool AcceptEncoding
	{
		get
		{
			return acceptEncoding;
		}
		set
		{
			acceptEncoding = value;
			OnPropertyChanged("AcceptEncoding");
		}
	}

	public bool AllowEmptyHeaderValues
	{
		get
		{
			return allowEmptyHeaderValues;
		}
		set
		{
			allowEmptyHeaderValues = value;
			OnPropertyChanged("AllowEmptyHeaderValues");
		}
	}

	public string MultipartBoundary
	{
		get
		{
			return multipartBoundary;
		}
		set
		{
			multipartBoundary = value;
			OnPropertyChanged("MultipartBoundary");
		}
	}

	public List<RuriLib.Functions.Requests.MultipartContent> MultipartContents { get; set; } = new List<RuriLib.Functions.Requests.MultipartContent>();

	public ResponseType ResponseType
	{
		get
		{
			return responseType;
		}
		set
		{
			responseType = value;
			OnPropertyChanged("ResponseType");
		}
	}

	public string DownloadPath
	{
		get
		{
			return downloadPath;
		}
		set
		{
			downloadPath = value;
			OnPropertyChanged("DownloadPath");
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

	public bool SaveAsScreenshot
	{
		get
		{
			return saveAsScreenshot;
		}
		set
		{
			saveAsScreenshot = value;
			OnPropertyChanged("SaveAsScreenshot");
		}
	}

	public Version ProtocolVersion
	{
		get
		{
			return protocolVersion;
		}
		set
		{
			protocolVersion = value;
			OnPropertyChanged("ProtocolVersion");
		}
	}

	public string[] ProtocolVersions => new string[3] { "1.1", "2.0", "2.1" };

	public bool UseAkamai
	{
		get
		{
			return useAkamai;
		}
		set
		{
			useAkamai = value;
			OnPropertyChanged("UseAkamai");
		}
	}

	public string URLSensor
	{
		get
		{
			return urlSensor;
		}
		set
		{
			urlSensor = value;
			OnPropertyChanged("URLSensor");
		}
	}

	public string N4SAuth
	{
		get
		{
			return n4sAuth;
		}
		set
		{
			n4sAuth = value;
			OnPropertyChanged("N4SAuth");
		}
	}

	public string DataSession
	{
		get
		{
			return dataSession;
		}
		set
		{
			dataSession = value;
			OnPropertyChanged("DataSession");
		}
	}

	public string SensorDataOut
	{
		get
		{
			return sensorDataOut;
		}
		set
		{
			sensorDataOut = value;
			OnPropertyChanged("SensorDataOut");
		}
	}

	public bool UseTLS => false;

	public BlockRequest()
	{
		base.Label = "REQUEST";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		Method = (ExHttpMethod)LineParser.ParseEnum(ref input, "METHOD", typeof(ExHttpMethod));
		Url = LineParser.ParseLiteral(ref input, "URL");
		while (LineParser.Lookahead(ref input) == TokenType.Boolean)
		{
			LineParser.SetBool(ref input, this);
		}
		CustomHeaders.Clear();
		while (input != string.Empty && !input.StartsWith("->"))
		{
			switch (LineParser.ParseToken(ref input, TokenType.Parameter, essential: true).ToUpper())
			{
			case "MULTIPART":
				RequestType = RequestType.Multipart;
				break;
			case "BASICAUTH":
				RequestType = RequestType.BasicAuth;
				break;
			case "STANDARD":
				RequestType = RequestType.Standard;
				break;
			case "RAW":
				RequestType = RequestType.Raw;
				break;
			case "CONTENT":
				PostData = LineParser.ParseLiteral(ref input, "POST DATA");
				break;
			case "RAWDATA":
				RawData = LineParser.ParseLiteral(ref input, "RAW DATA");
				break;
			case "STRINGCONTENT":
			{
				string[] array4 = ParseString(LineParser.ParseLiteral(ref input, "STRING CONTENT"), ':', 2);
				MultipartContents.Add(new RuriLib.Functions.Requests.MultipartContent
				{
					Type = MultipartContentType.String,
					Name = array4[0],
					Value = array4[1]
				});
				break;
			}
			case "FILECONTENT":
			{
				string[] array3 = ParseString(LineParser.ParseLiteral(ref input, "FILE CONTENT"), ':', 3);
				MultipartContents.Add(new RuriLib.Functions.Requests.MultipartContent
				{
					Type = MultipartContentType.File,
					Name = array3[0],
					Value = array3[1],
					ContentType = array3[2]
				});
				break;
			}
			case "COOKIE":
			{
				string[] array2 = ParseString(LineParser.ParseLiteral(ref input, "COOKIE VALUE"), ':', 2);
				CustomCookies[array2[0]] = array2[1];
				break;
			}
			case "HEADER":
			{
				string[] array = ParseString(LineParser.ParseLiteral(ref input, "HEADER VALUE"), ':', 2);
				CustomHeaders[array[0]] = array[1];
				break;
			}
			case "CONTENTTYPE":
				ContentType = LineParser.ParseLiteral(ref input, "CONTENT TYPE");
				break;
			case "USERNAME":
				AuthUser = LineParser.ParseLiteral(ref input, "USERNAME");
				break;
			case "PASSWORD":
				AuthPass = LineParser.ParseLiteral(ref input, "PASSWORD");
				break;
			case "BOUNDARY":
				MultipartBoundary = LineParser.ParseLiteral(ref input, "BOUNDARY");
				break;
			case "SECPROTO":
				SecurityProtocol = LineParser.ParseEnum(ref input, "Security Protocol", typeof(SecurityProtocol));
				break;
			case "HTTPLIBRARY":
				HttpLibrary = LineParser.ParseEnum(ref input, "Http Library", typeof(HttpLibrary));
				break;
			case "IGNORECERT":
				IgnoreCertificateValidation = false;
				break;
			case "ALWAYSSEND":
				AlwaysSendContent = true;
				break;
			case "CPENCODING":
				CodePagesEncoding = LineParser.ParseLiteral(ref input, "Code Pages Encoding");
				break;
			case "TIMEOUT":
				if (int.TryParse(LineParser.ParseLiteral(ref input, "Timeout"), out int _tout))
					RequestTimeoutMs = _tout;
				break;
			case "NOSAVECOOKIES":
				SaveResponseCookies = false;
				break;
			case "NOLOADCOOKIES":
				LoadRequestCookies = false;
				break;
			case "RETRYCOUNT":
				if (int.TryParse(LineParser.ParseLiteral(ref input, "Retry Count"), out int _rc))
					RetryCount = _rc;
				break;
			case "RETRYDELAY":
				if (int.TryParse(LineParser.ParseLiteral(ref input, "Retry Delay"), out int _rd))
					RetryDelayMs = _rd;
				break;
			case "CURLPROFILE":
				CurlImpersonateProfile = LineParser.ParseEnum(ref input, "Curl Profile", typeof(CurlImpersonateBrowserProfile));
				break;
			case "AKAMAI":
				UseAkamai = true;
				break;
			case "URLSENSOR":
				URLSensor = LineParser.ParseLiteral(ref input, "URL Sensor");
				break;
			case "N4SAUTH":
				N4SAuth = LineParser.ParseLiteral(ref input, "N4S Authorization");
				break;
			case "DATASESSION":
				DataSession = LineParser.ParseLiteral(ref input, "Data Session");
				break;
			case "SENSORDATAOUT":
				SensorDataOut = LineParser.ParseLiteral(ref input, "Sensor Data Out");
				break;
			case "PROTOVER":
			{
				string text = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false);
				int major = 1;
				int minor = 1;
				try
				{
					major = int.Parse(text.Split('.')[0]);
				}
				catch
				{
				}
				try
				{
					minor = int.Parse(text.Split('.')[1]);
				}
				catch
				{
				}
				ProtocolVersion = new Version(major, minor);
				break;
			}
			}
		}
		if (input.StartsWith("->"))
		{
			LineParser.EnsureIdentifier(ref input, "->");
			string text2 = LineParser.ParseToken(ref input, TokenType.Parameter, essential: true);
			if (text2.ToUpper() == "STRING")
			{
				ResponseType = ResponseType.String;
			}
			else if (text2.ToUpper() == "FILE")
			{
				ResponseType = ResponseType.File;
				DownloadPath = LineParser.ParseLiteral(ref input, "DOWNLOAD PATH");
				while (LineParser.Lookahead(ref input) == TokenType.Boolean)
				{
					LineParser.SetBool(ref input, this);
				}
			}
			else if (text2.ToUpper() == "BASE64")
			{
				ResponseType = ResponseType.Base64String;
				OutputVariable = LineParser.ParseLiteral(ref input, "OUTPUT VARIABLE");
			}
		}
		return this;
	}

	public static string[] ParseString(string input, char separator, int count)
	{
		return (from s in input.Split(new char[1] { separator }, count)
			select s.Trim()).ToArray();
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("REQUEST").Token(Method)
			.Literal(Url)
			.Boolean(AcceptEncoding, "AcceptEncoding")
			.Boolean(AutoRedirect, "AutoRedirect")
			.Boolean(ReadResponseSource, "ReadResponseSource")
			.Boolean(EncodeContent, "EncodeContent")
			.Boolean(AllowEmptyHeaderValues, "AllowEmptyHeaderValues")
			.Token(RequestType, "RequestType")
			.Indent();
		switch (RequestType)
		{
		case RequestType.BasicAuth:
			blockWriter.Token("USERNAME").Literal(AuthUser).Token("PASSWORD")
				.Literal(AuthPass)
				.Indent();
			break;
		case RequestType.Standard:
			if (Request.CanContainRequestBody(method))
			{
				blockWriter.Token("CONTENT").Literal(PostData).Indent()
					.Token("CONTENTTYPE")
					.Literal(ContentType);
			}
			break;
		case RequestType.Multipart:
			foreach (RuriLib.Functions.Requests.MultipartContent multipartContent in MultipartContents)
			{
				BlockWriter blockWriter2 = blockWriter.Indent();
				MultipartContentType type = multipartContent.Type;
				blockWriter2.Token(type.ToString().ToUpper() + "CONTENT");
				if (multipartContent.Type == MultipartContentType.String)
				{
					blockWriter.Literal(multipartContent.Name + ": " + multipartContent.Value);
				}
				else if (multipartContent.Type == MultipartContentType.File)
				{
					blockWriter.Literal(multipartContent.Name + ": " + multipartContent.Value + ": " + multipartContent.ContentType);
				}
			}
			if (!blockWriter.CheckDefault(MultipartBoundary, "MultipartBoundary"))
			{
				blockWriter.Indent().Token("BOUNDARY").Literal(MultipartBoundary);
			}
			break;
		case RequestType.Raw:
			if (Request.CanContainRequestBody(method))
			{
				blockWriter.Token("RAWDATA").Literal(RawData).Indent()
					.Token("CONTENTTYPE")
					.Literal(ContentType);
			}
			break;
		}
		if (ProtocolVersion.ToString() != "1.1")
		{
			blockWriter.Indent().Token("PROTOVER").Token(ProtocolVersion.Major + "." + ProtocolVersion.Minor, "ProtocolVersion");
		}
		if (SecurityProtocol != 0)
		{
			blockWriter.Indent().Token("SECPROTO").Token(SecurityProtocol, "SecurityProtocol");
		}
		if (HttpLibrary != HttpLibrary.SystemNet)
		{
			blockWriter.Indent().Token("HTTPLIBRARY").Token(HttpLibrary);
			if (HttpLibrary == HttpLibrary.CurlImpersonate && CurlImpersonateProfile != CurlImpersonateBrowserProfile.Chrome142)
				blockWriter.Indent().Token("CURLPROFILE").Token(CurlImpersonateProfile);
		}
		if (!IgnoreCertificateValidation)
		{
			blockWriter.Indent().Token("IGNORECERT");
		}
		if (AlwaysSendContent)
		{
			blockWriter.Indent().Token("ALWAYSSEND");
		}
		if (!string.IsNullOrEmpty(CodePagesEncoding))
		{
			blockWriter.Indent().Token("CPENCODING").Literal(CodePagesEncoding);
		}
		if (RequestTimeoutMs > 0)
		{
			blockWriter.Indent().Token("TIMEOUT").Literal(RequestTimeoutMs.ToString());
		}
		if (!SaveResponseCookies)
		{
			blockWriter.Indent().Token("NOSAVECOOKIES");
		}
		if (!LoadRequestCookies)
		{
			blockWriter.Indent().Token("NOLOADCOOKIES");
		}
		if (RetryCount > 0)
		{
			blockWriter.Indent().Token("RETRYCOUNT").Literal(RetryCount.ToString());
			if (RetryDelayMs != 1000)
				blockWriter.Indent().Token("RETRYDELAY").Literal(RetryDelayMs.ToString());
		}
		if (UseAkamai)
		{
			blockWriter.Indent().Token("AKAMAI").Indent()
				.Token("URLSENSOR")
				.Literal(URLSensor)
				.Indent()
				.Token("N4SAUTH")
				.Literal(N4SAuth);
			if (!string.IsNullOrEmpty(DataSession))
			{
				blockWriter.Indent().Token("DATASESSION").Literal(DataSession);
			}
			blockWriter.Indent().Token("SENSORDATAOUT").Literal(SensorDataOut);
		}
		foreach (KeyValuePair<string, string> customCookie in CustomCookies)
		{
			blockWriter.Indent().Token("COOKIE").Literal(customCookie.Key + ": " + customCookie.Value);
		}
		foreach (KeyValuePair<string, string> customHeader in CustomHeaders)
		{
			blockWriter.Indent().Token("HEADER").Literal(customHeader.Key + ": " + customHeader.Value);
		}
		if (ResponseType == ResponseType.File)
		{
			blockWriter.Indent().Arrow().Token("FILE")
				.Literal(DownloadPath)
				.Boolean(SaveAsScreenshot, "SaveAsScreenshot");
		}
		else if (ResponseType == ResponseType.Base64String)
		{
			blockWriter.Indent().Arrow().Token("BASE64")
				.Literal(OutputVariable);
		}
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		using var request = new Request();
		request.Setup(data.GlobalSettings, securityProtocol, AutoRedirect, data.ConfigSettings.MaxRedirects,
			AcceptEncoding, ProtocolVersion, AllowEmptyHeaderValues,
			HttpLibrary, IgnoreCertificateValidation, AlwaysSendContent,
			CodePagesEncoding, RequestTimeoutMs, CurlImpersonateProfile);
		string text = BlockBase.ReplaceValues(Url, data);
		data.Log(new LogEntry("Calling URL: " + text, Colors.MediumTurquoise));
		switch (RequestType)
		{
		case RequestType.Standard:
			request.SetStandardContent(BlockBase.ReplaceValues(PostData, data), BlockBase.ReplaceValues(ContentType, data), Method, EncodeContent, GetLogBuffer(data), AlwaysSendContent);
			break;
		case RequestType.BasicAuth:
			request.SetBasicAuth(BlockBase.ReplaceValues(AuthUser, data), BlockBase.ReplaceValues(AuthPass, data));
			break;
		case RequestType.Multipart:
		{
			IEnumerable<RuriLib.Functions.Requests.MultipartContent> contents = MultipartContents.Select(delegate(RuriLib.Functions.Requests.MultipartContent m)
			{
				RuriLib.Functions.Requests.MultipartContent result = default(RuriLib.Functions.Requests.MultipartContent);
				result.Name = BlockBase.ReplaceValues(m.Name ?? string.Empty, data);
				result.Value = BlockBase.ReplaceValues(m.Value ?? string.Empty, data);
				result.ContentType = BlockBase.ReplaceValues(m.ContentType ?? string.Empty, data);
				result.Type = m.Type;
				return result;
			});
			request.SetMultipartContent(contents, BlockBase.ReplaceValues(MultipartBoundary, data), GetLogBuffer(data));
			break;
		}
		case RequestType.Raw:
			if (RawBytes != null)
				request.SetRawContent(RawBytes, BlockBase.ReplaceValues(ContentType, data), Method, GetLogBuffer(data), AlwaysSendContent);
			else if (!string.IsNullOrEmpty(RawData) && RawData.StartsWith("@"))
			{
				// @varName — look up a byte[] stored in bot Variables (e.g. from LoliScript inline C#)
				string _rawVarName = RawData.Substring(1);
				byte[] _rawBytesVar = data.Variables.Get(_rawVarName)?.Value as byte[];
				if (_rawBytesVar != null)
					request.SetRawContent(_rawBytesVar, BlockBase.ReplaceValues(ContentType, data), Method, GetLogBuffer(data), AlwaysSendContent);
				else
					request.SetRawContent(BlockBase.ReplaceValues(RawData, data), BlockBase.ReplaceValues(ContentType, data), Method, GetLogBuffer(data), AlwaysSendContent);
			}
			else
				request.SetRawContent(BlockBase.ReplaceValues(RawData, data), BlockBase.ReplaceValues(ContentType, data), Method, GetLogBuffer(data), AlwaysSendContent);
			break;
		}
		if (data.UseProxies)
		{
			request.SetProxy(data.Proxy);
		}
		data.Log(new LogEntry("Sent Headers:", Colors.DarkTurquoise));
		Dictionary<string, string> headers = CustomHeaders.Select((KeyValuePair<string, string> h) => new KeyValuePair<string, string>(BlockBase.ReplaceValues(h.Key, data), BlockBase.ReplaceValues(h.Value, data))).ToDictionary((KeyValuePair<string, string> h) => h.Key, (KeyValuePair<string, string> h) => h.Value);
		request.SetHeaders(headers, AcceptEncoding, GetLogBuffer(data));
		data.Log(new LogEntry("Sent Cookies:", Colors.MediumTurquoise));
		foreach (var _cc in CustomCookies)
			data.Cookies[BlockBase.ReplaceValues(_cc.Key, data)] = BlockBase.ReplaceValues(_cc.Value, data);
		if (LoadRequestCookies)
			request.SetCookies(data.Cookies, GetLogBuffer(data));
		else
			request.SetCookies(CustomCookies.ToDictionary(
				_cc => BlockBase.ReplaceValues(_cc.Key, data),
				_cc => BlockBase.ReplaceValues(_cc.Value, data)), GetLogBuffer(data));
		data.LogNewLine();
		int _maxAttempts = Math.Max(0, RetryCount) + 1;
		Exception _lastEx = null;
		for (int _attempt = 0; _attempt < _maxAttempts; _attempt++)
		{
			if (_attempt > 0)
			{
				data.Log(new LogEntry($"Retrying request (attempt {_attempt + 1}/{_maxAttempts})...", Colors.Orange));
				System.Threading.Thread.Sleep(Math.Max(0, RetryDelayMs));
			}
			try
			{
				var (_addr, _code, _respHeaders, _respCookies) = request.Perform(text, Method, GetLogBuffer(data));
				data.Address = _addr;
				data.ResponseCode = _code;
				data.ResponseHeaders = _respHeaders;
				if (SaveResponseCookies)
					data.Cookies = _respCookies;
				_lastEx = null;
				break;
			}
			catch (Exception ex)
			{
				_lastEx = ex;
				if (_attempt < _maxAttempts - 1)
					data.Log(new LogEntry($"Request failed ({ex.Message}), retrying...", Colors.Orange));
			}
		}
		if (_lastEx != null)
		{
			if (data.ConfigSettings.IgnoreResponseErrors)
			{
				data.Log(new LogEntry(_lastEx.Message, Colors.Tomato));
				data.ResponseSource = _lastEx.Message;
				return;
			}
			throw _lastEx;
		}
		switch (ResponseType)
		{
		case ResponseType.String:
			data.ResponseSource = request.SaveString(ReadResponseSource, data.ResponseHeaders, GetLogBuffer(data));
			data.RawSourceBytes = request.GetRawBytes();
			break;
		case ResponseType.File:
			if (SaveAsScreenshot)
			{
				using var ssStream = request.GetResponseStream();
				Files.SaveScreenshot(ssStream, data);
				data.Log(new LogEntry("File saved as screenshot", Colors.Green));
			}
			else
			{
				request.SaveFile(BlockBase.ReplaceValues(DownloadPath, data), GetLogBuffer(data));
			}
			break;
		case ResponseType.Base64String:
		{
			using var b64Stream = request.GetResponseStream();
			string value = Convert.ToBase64String(b64Stream.ToArray());
			BlockBase.InsertVariable(data, isCapture: false, value, OutputVariable);
			break;
		}
		}
		if (UseAkamai)
		{
			data.Log(new LogEntry("", Colors.Transparent));
			Akamai(data);
		}
	}

	public string GetCustomCookies()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, string> customCookie in CustomCookies)
		{
			stringBuilder.Append(customCookie.Key + ": " + customCookie.Value);
			if (!customCookie.Equals(CustomCookies.Last()))
			{
				stringBuilder.Append(Environment.NewLine);
			}
		}
		return stringBuilder.ToString();
	}

	public void SetCustomCookies(string[] lines)
	{
		CustomCookies.Clear();
		foreach (string text in lines)
		{
			if (text.Contains(':'))
			{
				string[] array = text.Split(new char[1] { ':' }, 2);
				CustomCookies[array[0].Trim()] = array[1].Trim();
			}
		}
	}

	public string GetCustomHeaders()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, string> customHeader in CustomHeaders)
		{
			stringBuilder.Append(customHeader.Key + ": " + customHeader.Value);
			if (!customHeader.Equals(CustomHeaders.Last()))
			{
				stringBuilder.Append(Environment.NewLine);
			}
		}
		return stringBuilder.ToString();
	}

	public void SetCustomHeaders(string[] lines)
	{
		CustomHeaders.Clear();
		foreach (string text in lines)
		{
			if (text.Contains(':'))
			{
				string[] array = text.Split(new char[1] { ':' }, 2);
				CustomHeaders[array[0].Trim()] = array[1].Trim();
			}
		}
	}

	public string GetMultipartContents()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < MultipartContents.Count; i++)
		{
			var multipartContent = MultipartContents[i];
			string[] array = new string[5];
			MultipartContentType type = multipartContent.Type;
			array[0] = type.ToString().ToUpper();
			array[1] = ": ";
			array[2] = multipartContent.Name;
			array[3] = ": ";
			array[4] = multipartContent.Value;
			stringBuilder.Append(string.Concat(array));
			if (i < MultipartContents.Count - 1)
				stringBuilder.Append(Environment.NewLine);
		}
		return stringBuilder.ToString();
	}

	public void SetMultipartContents(string[] lines)
	{
		MultipartContents.Clear();
		foreach (string text in lines)
		{
			try
			{
				string[] array = text.Split(new char[1] { ':' }, 3);
				MultipartContents.Add(new RuriLib.Functions.Requests.MultipartContent
				{
					Type = (MultipartContentType)Enum.Parse(typeof(MultipartContentType), array[0].Trim(), ignoreCase: true),
					Name = array[1].Trim(),
					Value = array[2].Trim()
				});
			}
			catch
			{
			}
		}
	}

	private List<LogEntry> GetLogBuffer(BotData data)
	{
		if (!data.GlobalSettings.General.EnableBotLog && !data.IsDebug)
		{
			return null;
		}
		return data.LogBuffer;
	}

	public Tuple<string, string, string> Analyze()
	{
		string empty = string.Empty;
		string text = string.Empty;
		string empty2 = string.Empty;
		string empty3 = string.Empty;
		string text2 = string.Empty;
		string empty4 = string.Empty;

		using var client = Request.BuildRawClient(ignoreSsl: true);
		HttpResponseMessage response = null;

		var req = Request.BuildRawGetRequest(Url, CustomHeaders);
		response = client.SendAsync(req).GetAwaiter().GetResult();
		text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
		empty = ((int)response.StatusCode).ToString();
		empty2 = response.Headers.TryGetValues("Set-Cookie", out var sc)
			? string.Join("; ", sc.Select(s => s.Split(';')[0]))
			: string.Empty;

		if (text.Contains("<form"))
		{
			int num = 0;
			int num2 = text.IndexOf("<form");
			while (num2 != -1)
			{
				num2 = text.IndexOf("<form", num2 + 1);
				num++;
			}
			string text3 = "<form";
			string text4 = "</form>";
			string text5 = string.Empty;
			for (int i = 1; i <= num; i++)
			{
				string text6 = text.Split(new string[1] { text3 }, StringSplitOptions.None)[i].Split(new string[1] { text4 }, StringSplitOptions.None)[0];
				if (((!text6.ToLower().Contains("register") && !text6.ToLower().Contains("search")) && text6.ToLower().Contains("post")) || text6.ToLower().Contains("login"))
				{
					text5 = text6;
					break;
				}
			}
			if (string.IsNullOrEmpty(text5))
				throw new Exception($"Analyze: no login/post form found among {num} form(s) at {Url}");
			string empty5 = string.Empty;
			empty5 = ((!text5.Contains("action='")) ? "action=\"\"" : "action=''");
			empty4 = WebUtility.HtmlDecode(Regex.Match(text5, empty5.Substring(0, empty5.Length - 1) + "(.*?)" + empty5.Last()).Groups[1].Value);
			if (!empty4.Contains("://"))
			{
				// Resolve against the actual response URL so both "" and relative paths (login.php, /path) work
				var _baseUri = new Uri(response.RequestMessage?.RequestUri?.ToString() ?? Url);
				empty4 = string.IsNullOrWhiteSpace(empty4)
					? _baseUri.ToString()
					: new Uri(_baseUri, empty4).ToString();
			}
			text5 = text5.Replace("<label>", "<input");
			text5 = text5.Replace("<button", "<input");
			MatchCollection matchCollection = Regex.Matches(text5, @"<input\b([^>]*)>", RegexOptions.IgnoreCase);
			for (int j = 0; j < matchCollection.Count; j++)
			{
				empty3 = matchCollection[j].Groups[1].Value;
				empty3 = empty3.Replace("name='", "name=\"");
				empty3 = empty3.Replace("name = '", "name=\"");
				empty3 = empty3.Replace("value='", "value=\"");
				empty3 = empty3.Replace("'", "\"");
				string empty6 = string.Empty;
				_ = string.Empty;
				empty6 = (empty3.Contains("name=\"") ? Regex.Match(empty3, "name=\"(.*?)\"").Groups[1].Value : Regex.Match(empty3, "name=(.*?) ").Groups[1].Value);
				if (!empty3.Contains("type=\""))
				{
					_ = Regex.Match(empty3, "type=(.*?) ").Groups[1].Value;
				}
				else
				{
					_ = Regex.Match(empty3, "type=\"(.*?)\"").Groups[1].Value;
				}
				string value = Regex.Match(empty3, "value=\"(.*?)\"").Groups[1].Value;
				if (!string.IsNullOrEmpty(empty6))
				{
					text2 = ((!string.IsNullOrEmpty(text2)) ? (text2 + "&" + WebUtility.UrlEncode(empty6) + "=" + WebUtility.UrlEncode(value)) : (WebUtility.UrlEncode(empty6) + "=" + WebUtility.UrlEncode(value)));
				}
			}
			return new Tuple<string, string, string>(empty4, text2, empty2);
		}
		throw new WebException(empty + $" ({response.StatusCode})");
	}

	private void Akamai(BotData data)
	{
		using var request = new Request();
		request.Setup(data.GlobalSettings, securityProtocol, autoRedirect: true, 8, acceptEncoding: false, HttpVersion.Version11, allowEmptyHeaderValues: true);
		string text = BlockBase.ReplaceValues(URLSensor, data);
		data.Log(new LogEntry("Calling URL: " + text, Colors.MediumTurquoise));
		string text2 = "";
		if (string.IsNullOrEmpty(DataSession))
		{
			if (!data.Cookies.TryGetValue("_abck", out string text3) ||
			    !data.Cookies.TryGetValue("bm_sz",  out string text4))
			{
				data.Log(new LogEntry("Akamai: cookies _abck / bm_sz not found in jar — skipping sensor call", Colors.Tomato));
				return;
			}
			text2 = Newtonsoft.Json.JsonConvert.SerializeObject(new { abck = text3, bm_sz = text4 });
		}
		else
		{
			text2 = BlockBase.ReplaceValues(DataSession, data);
		}
		request.SetStandardContent(text2, "application/json", ExHttpMethod.POST, encodeContent: false, GetLogBuffer(data));
		if (data.UseProxies)
		{
			request.SetProxy(data.Proxy);
		}
		request.SetHeaders(new Dictionary<string, string> { 
		{
			"Authorization",
			BlockBase.ReplaceValues(N4SAuth, data)
		} }, acceptEncoding: true, GetLogBuffer(data));
		data.LogNewLine();
		Dictionary<string, string> headers = null;
		Dictionary<string, string> cookies = null;
		try
		{
			(string, string, Dictionary<string, string>, Dictionary<string, string>) tuple = request.Perform(text, ExHttpMethod.POST, GetLogBuffer(data));
			headers = tuple.Item3;
			cookies = tuple.Item4;
			SetHeadersAndCookies();
		}
		catch (Exception ex)
		{
			SetHeadersAndCookies();
			if (data.ConfigSettings.IgnoreResponseErrors)
			{
				data.Log(new LogEntry(ex.Message, Colors.Tomato));
				data.ResponseSource = ex.Message;
				return;
			}
			throw;
		}
		data.Log(new LogEntry("", Colors.Transparent));
		string value = request.SaveString(readResponseSource: true, data.ResponseHeaders, GetLogBuffer(data));
		if (!string.IsNullOrEmpty(SensorDataOut))
		{
			BlockBase.InsertVariable(data, isCapture: false, value, SensorDataOut);
		}
		void SetHeadersAndCookies()
		{
			if (headers != null)
			{
				data.ResponseHeaders = data.ResponseHeaders.Concat(headers.Where((KeyValuePair<string, string> h) => !data.ResponseHeaders.ContainsKey(h.Key))).ToDictionary((KeyValuePair<string, string> x) => x.Key, (KeyValuePair<string, string> x) => x.Value);
			}
			if (cookies != null)
			{
				// Merge: start from the existing jar, let response cookies override/add
				var merged = new Dictionary<string, string>(data.Cookies, StringComparer.OrdinalIgnoreCase);
				foreach (var kv in cookies)
					merged[kv.Key] = kv.Value;
				data.Cookies = merged;
			}
		}
	}
}


