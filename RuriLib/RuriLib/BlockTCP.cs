using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using RuriLib.LS;
using RuriLib.Models;

namespace RuriLib;

public class BlockTCP : BlockBase
{
	private TCPCommand tcpCommand;

	private string host = "";

	private string port = "";

	private bool useSSL = true;

	private bool webSocket;

	private bool waitForHello = true;

	private string message = "";

	private string variableName = "";

	private bool isCapture;

	public TCPCommand TCPCommand
	{
		get
		{
			return tcpCommand;
		}
		set
		{
			tcpCommand = value;
			OnPropertyChanged("TCPCommand");
		}
	}

	public string Host
	{
		get
		{
			return host;
		}
		set
		{
			host = value;
			OnPropertyChanged("Host");
		}
	}

	public string Port
	{
		get
		{
			return port;
		}
		set
		{
			port = value;
			OnPropertyChanged("Port");
		}
	}

	public bool UseSSL
	{
		get
		{
			return useSSL;
		}
		set
		{
			useSSL = value;
			OnPropertyChanged("UseSSL");
		}
	}

	public bool WebSocket
	{
		get
		{
			return webSocket;
		}
		set
		{
			webSocket = value;
			OnPropertyChanged("WebSocket");
		}
	}

	public bool WaitForHello
	{
		get
		{
			return waitForHello;
		}
		set
		{
			waitForHello = value;
			OnPropertyChanged("WaitForHello");
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

	public BlockTCP()
	{
		base.Label = "TCP";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		TCPCommand = (TCPCommand)LineParser.ParseEnum(ref input, "Command", typeof(TCPCommand));
		switch (TCPCommand)
		{
		case TCPCommand.Connect:
			Host = LineParser.ParseLiteral(ref input, "Host");
			Port = LineParser.ParseLiteral(ref input, "Port");
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case TCPCommand.Send:
			Message = LineParser.ParseLiteral(ref input, "Message");
			if (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		}
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
			VariableName = LineParser.ParseToken(ref input, TokenType.Literal, essential: true);
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
		blockWriter.Label(base.Label).Token("TCP").Token(TCPCommand);
		switch (TCPCommand)
		{
		case TCPCommand.Connect:
			blockWriter.Literal(Host).Literal(Port).Boolean(UseSSL, "UseSSL")
				.Boolean(WaitForHello, "WaitForHello");
			break;
		case TCPCommand.Send:
			blockWriter.Literal(Message).Boolean(WebSocket, "WebSocket");
			break;
		}
		if (!blockWriter.CheckDefault(VariableName, "VariableName"))
		{
			blockWriter.Arrow().Token(IsCapture ? "CAP" : "VAR").Literal(VariableName);
		}
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		TcpClient tcpClient = data.GetCustomObject("TCPClient") as TcpClient;
		NetworkStream networkStream = data.GetCustomObject("NETStream") as NetworkStream;
		SslStream sslStream = data.GetCustomObject("SSLStream") as SslStream;
		byte[] array = new byte[4096];
		string text = "";
		switch (TCPCommand)
		{
		case TCPCommand.Connect:
		{
			string text3 = BlockBase.ReplaceValues(host, data);
			int num3 = int.Parse(BlockBase.ReplaceValues(port, data));
			tcpClient = new TcpClient();
			tcpClient.Connect(text3, num3);
			if (tcpClient.Connected)
			{
				networkStream = tcpClient.GetStream();
				if (UseSSL)
				{
					sslStream = new SslStream(networkStream);
					sslStream.AuthenticateAsClient(text3);
				}
				if (WaitForHello)
				{
					text = ReadFully(UseSSL ? (Stream)sslStream : networkStream, networkStream, array);
				}
				data.CustomObjects["TCPClient"] = tcpClient;
				data.CustomObjects["NETStream"] = networkStream;
				data.CustomObjects["SSLStream"] = sslStream;
				data.CustomObjects["TCPSSL"] = UseSSL;
				data.Log(new LogEntry($"Succesfully connected to host {text3} on port {num3}. The server says:", Colors.Green));
				data.Log(new LogEntry(text, Colors.GreenYellow));
			}
			if (VariableName != string.Empty)
			{
				data.Variables.Set(new CVar(VariableName, text, IsCapture));
				data.Log(new LogEntry("Saved Response in variable " + VariableName, Colors.White));
			}
			break;
		}
		case TCPCommand.Disconnect:
			if (tcpClient == null)
			{
				throw new Exception("Make a connection first!");
			}
			tcpClient.Close();
			tcpClient = null;
			networkStream?.Close();
			sslStream?.Close();
			data.Log(new LogEntry("Succesfully closed the stream", Colors.GreenYellow));
			break;
		case TCPCommand.Send:
		{
			if (tcpClient == null)
			{
				throw new Exception("Make a connection first!");
			}
			string text2 = BlockBase.ReplaceValues(Message, data);
			byte[] array2 = new byte[0];
			byte[] bytes = Encoding.ASCII.GetBytes(text2.Unescape());
			if (WebSocket)
			{
				List<byte> list = new List<byte>();
				list.Add(129);
				ulong num2 = (ulong)bytes.Length;
				if (num2 <= 125)
				{
					list.Add((byte)(num2 + 128));
				}
				else if (num2 <= 65535)
				{
					list.Add(254);
					list.Add((byte)(num2 >> 8));
					list.Add((byte)(num2 & 0xFF));
				}
				else
				{
					list.Add(byte.MaxValue);
					list.Add((byte)(num2 >> 56));
					list.Add((byte)(num2 >> 48));
					list.Add((byte)(num2 >> 40));
					list.Add((byte)(num2 >> 32));
					list.Add((byte)(num2 >> 24));
					list.Add((byte)((num2 >> 16) & 0xFF));
					list.Add((byte)((num2 >> 8) & 0xFF));
					list.Add((byte)(num2 & 0xFF));
				}
				byte[] array3 = RandomNumberGenerator.GetBytes(4);
				list.AddRange(array3);
				for (int i = 0; i < bytes.Length; i++)
				{
					list.Add((byte)(bytes[i] ^ array3[i % 4]));
				}
				array2 = list.ToArray();
			}
			else
			{
				array2 = bytes;
			}
			data.Log(new LogEntry("> " + text2, Colors.White));
			bool? flag = data.GetCustomObject("TCPSSL") as bool?;
			if (flag.HasValue && flag.Value)
			{
				sslStream.Write(array2);
				text = ReadFully(sslStream, networkStream, array);
			}
			else
			{
				networkStream.Write(array2, 0, array2.Length);
				text = ReadFully(networkStream, networkStream, array);
			}
			data.Log(new LogEntry("> " + text, Colors.GreenYellow));
			if (VariableName != string.Empty)
			{
				data.Variables.Set(new CVar(VariableName, text, IsCapture));
				data.Log(new LogEntry("Saved Response in variable " + VariableName + ".", Colors.White));
			}
			break;
		}
		}
	}

	private static string ReadFully(Stream stream, NetworkStream ns, byte[] buffer)
	{
		using var ms = new MemoryStream();
		do
		{
			int read = stream.Read(buffer, 0, buffer.Length);
			if (read <= 0) break;
			ms.Write(buffer, 0, read);
		}
		while (ns.DataAvailable);
		return Encoding.ASCII.GetString(ms.ToArray());
	}
}
