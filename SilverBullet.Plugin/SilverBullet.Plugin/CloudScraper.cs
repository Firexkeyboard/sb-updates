using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using RuriLib.Models;
using PluginFramework;
using PluginFramework.Attributes;
using RuriLib;
using RuriLib.LS;
using RuriLib.ViewModels;

namespace SilverBullet.Plugin;

public class CloudScraper : BlockBase, IBlockPlugin
{
	private string variableName;

	private string url;

	public string Name => "CloudScraper";

	public LinearGradientBrush LinearGradientBrush => new LinearGradientBrush(new GradientStopCollection
	{
		new GradientStop(LinearGradientBrushExtensions.ColorConverter("#F38020"), 1.0)
	});

	public bool LightForeground => false;

	[Text("VariableName:", "return user agent")]
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

	[Text("Url:", "")]
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

	public CloudScraper()
	{
		((BlockBase)this).Label = "CloudScraper";
	}

	public override void Process(BotData data)
	{
		((BlockBase)this).Process(data);
		string text = Url;
		if (data.UseProxies)
		{
			string[] obj = new string[5] { text, " ", null, null, null };
			ProxyType type = data.Proxy.Type;
			obj[2] = type.ToString().ToLower();
			obj[3] = "://";
			obj[4] = data.Proxy.Proxy;
			text = string.Concat(obj);
		}
		Process process = new Process
		{
			StartInfo = new ProcessStartInfo("bin\\CloudScraper.exe", text)
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardInput = true,
				CreateNoWindow = true
			}
		};
		process.Start();
		string text2 = process.StandardOutput.ReadToEnd();
		try
		{
			if (!process.HasExited)
			{
				process.Kill();
			}
		}
		catch
		{
		}
		string[] source;
		if ((source = text2.Split('\n')).Any((string o) => o == "bypassed = true\r"))
		{
			foreach (string item in source.FirstOrDefault((string o) => o.StartsWith("cookie = \"")).Split(new char[1] { '"' }, 2)[1].Split(';').AsParallel())
			{
				if (item == "\r" || item == "\n")
				{
					continue;
				}
				try
				{
					string key = item.Split(new char[1] { '=' }, 2)[0];
					if (data.Cookies.ContainsKey(key))
					{
						data.Cookies[key] = item.Split(new char[1] { '=' }, 2)[1];
					}
					else
					{
						data.Cookies.Add(key, item.Split(new char[1] { '=' }, 2)[1]);
					}
				}
				catch (Exception ex)
				{
					data.Log(new LogEntry(ex.Message, Colors.PaleVioletRed));
				}
			}
			BlockBase.InsertVariable(data, false, source.FirstOrDefault((string o) => o.StartsWith("useragent =")).Split(new char[1] { '=' }, 2)[1].Replace("\r", string.Empty), VariableName, "", "", false, true);
		}
		else if (text2.Contains("Cannot connect to proxy."))
		{
			BlockBase.InsertVariable(data, false, "Cannot connect to proxy.", VariableName, "", "", false, true);
		}
		else
		{
			if (!text2.Contains("Missing dependencies for SOCKS support."))
			{
				BlockBase.InsertVariable(data, false, "an error occurred", VariableName, "", "", false, true);
				throw new Exception("an error occurred\nbypassed = false");
			}
			BlockBase.InsertVariable(data, false, "Missing dependencies for SOCKS support.", VariableName, "", "", false, true);
		}
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter val = new BlockWriter(((object)this).GetType(), indent, ((BlockBase)this).Disabled);
		val.Label(((BlockBase)this).Label).Token((object)"CloudScraper", "").Literal(Url, "");
		if (!val.CheckDefault((object)VariableName, "VariableName"))
		{
			val.Arrow().Token((object)"VAR", "").Literal(VariableName, "")
				.Indent(1);
		}
		return ((object)val).ToString();
	}

	public override BlockBase FromLS(string line)
	{
		string text = line.Trim();
		if (text.StartsWith("#"))
		{
			((BlockBase)this).Label = LineParser.ParseLabel(ref text);
		}
		Url = LineParser.ParseLiteral(ref text, "Url", false, (BotData)null);
		try
		{
			VariableName = LineParser.ParseToken(ref text, (TokenType)2, true, true);
			return (BlockBase)(object)this;
		}
		catch
		{
			throw new ArgumentException("Variable name not specified");
		}
	}
}
