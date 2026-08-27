using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Windows.Media;
using AngleSharp.Text;
using RuriLib.LS;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace RuriLib;

public class BlockWebSocket : BlockBase
{
	private string variableName = "";

	private bool isCapture;

	private string url = string.Empty;

	private CompressionMethod compression;

	private string origin;

	private TimeSpan waitTime = TimeSpan.FromSeconds(5.0);

	private bool emitOnPing;

	private bool redirection;

	private WSCommand command;

	private string message;

	private SslProtocols sslProtocols = SslProtocols.None;

	private bool credentials;

	private string username;

	private string password;

	private bool preAuth;

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
			url = value;
			OnPropertyChanged("Url");
		}
	}

	public CompressionMethod Compression
	{
		get
		{
			return compression;
		}
		set
		{
			compression = value;
			OnPropertyChanged("Compression");
		}
	}

	public string Origin
	{
		get
		{
			return origin;
		}
		set
		{
			origin = value;
			OnPropertyChanged("Origin");
		}
	}

	public TimeSpan WaitTime
	{
		get
		{
			return waitTime;
		}
		set
		{
			waitTime = value;
			OnPropertyChanged("WaitTime");
		}
	}

	public bool EmitOnPing
	{
		get
		{
			return emitOnPing;
		}
		set
		{
			emitOnPing = value;
			OnPropertyChanged("EmitOnPing");
		}
	}

	public bool Redirection
	{
		get
		{
			return redirection;
		}
		set
		{
			redirection = value;
			OnPropertyChanged("Redirection");
		}
	}

	public WSCommand Command
	{
		get
		{
			return command;
		}
		set
		{
			command = value;
			OnPropertyChanged("Command");
		}
	}

	public string Message
	{
		get
		{
			return message;
		}
		set
		{
			message = value;
			OnPropertyChanged("Message");
		}
	}

	public SslProtocols SslProtocols
	{
		get
		{
			return sslProtocols;
		}
		set
		{
			sslProtocols = value;
			OnPropertyChanged("SslProtocols");
		}
	}

	public Dictionary<string, string> CustomCookies { get; set; } = new Dictionary<string, string>();

	public bool Credentials
	{
		get
		{
			return credentials;
		}
		set
		{
			credentials = value;
			OnPropertyChanged("Credentials");
		}
	}

	public string Username
	{
		get
		{
			return username;
		}
		set
		{
			username = value;
			OnPropertyChanged("Username");
		}
	}

	public string Password
	{
		get
		{
			return password;
		}
		set
		{
			password = value;
			OnPropertyChanged("Password");
		}
	}

	public bool PreAuth
	{
		get
		{
			return preAuth;
		}
		set
		{
			preAuth = value;
			OnPropertyChanged("PreAuth");
		}
	}

	public BlockWebSocket()
	{
		base.Label = "WS";
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		WebSocket webSocket = data.GetCustomObject("WebSocket") as WebSocket;
		List<string> wsMessages = data.GetCustomObject("WebSocketMessages") as List<string>;
		switch (Command)
		{
		case WSCommand.Connect:
		{
			string text = BlockBase.ReplaceValues(Url, data);
			try
			{
				webSocket?.Close(CloseStatusCode.NoStatus);
			}
			catch
			{
			}
			webSocket = new WebSocket(text)
			{
				Compression = Compression,
				Origin = Origin,
				WaitTime = WaitTime,
				EmitOnPing = EmitOnPing,
				EnableRedirection = Redirection
			};
			webSocket.SslConfiguration.EnabledSslProtocols = SslProtocols;
			if (data.UseProxies)
			{
				if (data.Proxy.Type != 0)
				{
					throw new NotSupportedException("Only HTTP proxy is supported");
				}
				webSocket.SetProxy("http://" + data.Proxy.Host + ":" + data.Proxy.Port, data.Proxy.Username, data.Proxy.Password);
			}
			if (Credentials)
			{
				webSocket.SetCredentials(BlockBase.ReplaceValues(Username, data), BlockBase.ReplaceValues(Password, data), PreAuth);
			}
			data.Log(new LogEntry("Sent Cookies:", Colors.MediumTurquoise));
			foreach (KeyValuePair<string, string> customCookie in CustomCookies)
			{
				webSocket.SetCookie(new Cookie(BlockBase.ReplaceValues(customCookie.Key, data), BlockBase.ReplaceValues(customCookie.Value, data), "/"));
				data.LogBuffer.Add(new LogEntry(customCookie.Key + ": " + customCookie.Value, Colors.MediumTurquoise));
			}
			foreach (KeyValuePair<string, string> cookie in data.Cookies)
			{
				webSocket.SetCookie(new Cookie(BlockBase.ReplaceValues(cookie.Key, data), BlockBase.ReplaceValues(cookie.Value, data), "/"));
				data.LogBuffer.Add(new LogEntry(cookie.Key + ": " + cookie.Value, Colors.MediumTurquoise));
			}
			if (wsMessages == null)
			{
				wsMessages = new List<string>();
			}
			data.CustomObjects["WebSocketMessages"] = wsMessages;
			webSocket.OnMessage += delegate(object sender, MessageEventArgs e)
			{
				lock (wsMessages)
				{
					wsMessages.Add(e.Data);
				}
			};
			webSocket.Connect();
			if (!WaitForConnect(webSocket))
			{
				throw new Exception($"Connection Status: {webSocket.ReadyState}");
			}
			data.CustomObjects["WebSocket"] = webSocket;
			data.Log(new LogEntry("Succesfully connected to url: " + text + ".", Colors.LimeGreen));
			data.Log(new LogEntry($"Connection Status: {webSocket.ReadyState}", Colors.LimeGreen));
			break;
		}
		case WSCommand.Disconnect:
			if (webSocket == null)
			{
				throw new NullReferenceException("Make a connection first!");
			}
			webSocket.Close();
			webSocket = null;
			data.Log(new LogEntry("Succesfully closed", Colors.GreenYellow));
			data.CustomObjects["WebSocket"] = null;
			break;
		case WSCommand.Send:
		{
			if (webSocket == null)
			{
				throw new NullReferenceException("Make a connection first!");
			}
			string text2 = BlockBase.ReplaceValues(Message, data);
			webSocket.Send(text2);
			data.Log(new LogEntry("Sent " + text2 + " to the server", Colors.LimeGreen));
			break;
		}
		case WSCommand.Read:
			if (webSocket == null)
			{
				throw new NullReferenceException("Make a connection first!");
			}
			wsMessages = data.CustomObjects["WebSocketMessages"] as List<string>;
			data.Log(new LogEntry("Unread messages from server", Colors.LimeGreen));
			if (!string.IsNullOrEmpty(VariableName))
			{
				BlockBase.InsertVariable(data, IsCapture, wsMessages, VariableName);
				wsMessages?.Clear();
			}
			break;
		}
	}

	private bool WaitForConnect(WebSocket ws)
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		while (ws.ReadyState == WebSocketState.Connecting)
		{
			if (stopwatch.Elapsed.TotalSeconds >= WaitTime.TotalSeconds)
			{
				return OpenState(ws);
			}
			System.Threading.Thread.Sleep(1);
		}
		stopwatch.Stop();
		return OpenState(ws);
	}

	private bool OpenState(WebSocket ws)
	{
		return ws.ReadyState == WebSocketState.Open;
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		Command = (WSCommand)LineParser.ParseEnum(ref input, "Command", typeof(WSCommand));
		switch (Command)
		{
		case WSCommand.Connect:
			Url = LineParser.ParseLiteral(ref input, "Url");
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			ParseVarOrCap(ref input);
			while (input != "" && LineParser.Lookahead(ref input) == TokenType.Parameter)
			{
				switch (LineParser.ParseToken(ref input, TokenType.Parameter, essential: true).ToUpper())
				{
				case "USERNAME":
					Username = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false);
					break;
				case "PASSWORD":
					Password = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false);
					break;
				case "PREAUTH":
					PreAuth = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false).ToBoolean();
					break;
				case "SSLPROTO":
					SslProtocols = LineParser.ParseEnum(ref input, "SSL Protocols", typeof(SslProtocols));
					break;
				case "COMPRESSION":
					Compression = LineParser.ParseEnum(ref input, "Compression Method", typeof(CompressionMethod));
					break;
				case "COOKIE":
				{
					string[] array = ParseString(LineParser.ParseLiteral(ref input, "COOKIE VALUE"), ':', 2);
					CustomCookies[array[0]] = array[1];
					break;
				}
				}
			}
			break;
		case WSCommand.Send:
			Message = LineParser.ParseLiteral(ref input, "Message");
			if (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			ParseVarOrCap(ref input);
			break;
		default:
			ParseVarOrCap(ref input);
			break;
		}
		return this;
	}

	private void ParseVarOrCap(ref string input)
	{
		if (LineParser.ParseToken(ref input, TokenType.Arrow, essential: false) == string.Empty)
		{
			return;
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
			VariableName = LineParser.ParseToken(ref input, TokenType.Literal, essential: true);
		}
		catch
		{
			throw new ArgumentException("Variable name not specified");
		}
	}

	private void WriteVarOrCap(BlockWriter writer)
	{
		if (!writer.CheckDefault(VariableName, "VariableName"))
		{
			writer.Arrow().Token(IsCapture ? "CAP" : "VAR").Literal(VariableName);
		}
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("WS").Token(Command);
		switch (Command)
		{
		case WSCommand.Connect:
			blockWriter.Literal(Url);
			if (Redirection)
			{
				blockWriter.Boolean(Redirection, "Redirection");
			}
			if (EmitOnPing)
			{
				blockWriter.Boolean(EmitOnPing, "EmitOnPing");
			}
			if (Credentials)
			{
				blockWriter.Boolean(Credentials, "Credentials");
			}
			WriteVarOrCap(blockWriter);
			if (Credentials)
			{
				blockWriter.Indent().Token("USERNAME").Token(Username, "Username");
				blockWriter.Indent().Token("PASSWORD").Token(Password, "Password");
				if (PreAuth)
				{
					blockWriter.Indent().Token("PREAUTH").Token(PreAuth, "PreAuth");
				}
			}
			if (SslProtocols != SslProtocols.None)
			{
				blockWriter.Indent().Token("SSLPROTO").Token(SslProtocols, "SslProtocols");
			}
			if (Compression != 0)
			{
				blockWriter.Indent().Token("COMPRESSION").Token(Compression, "Compression");
			}
			foreach (KeyValuePair<string, string> customCookie in CustomCookies)
			{
				blockWriter.Indent().Token("COOKIE").Literal(customCookie.Key + ": " + customCookie.Value);
			}
			break;
		case WSCommand.Send:
			blockWriter.Literal(Message);
			WriteVarOrCap(blockWriter);
			break;
		default:
			WriteVarOrCap(blockWriter);
			break;
		}
		return blockWriter.ToString();
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

	public static string[] ParseString(string input, char separator, int count)
	{
		return (from s in input.Split(new char[1] { separator }, count)
			select s.Trim()).ToArray();
	}
}
