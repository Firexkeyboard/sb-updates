using System;
using System.Collections.Generic;
using System.Windows.Media;
using AngleSharp.Text;
using RuriLib.Models;

namespace RuriLib.LS;

internal class SetParser
{
	public static Action Parse(string line, BotData data)
	{
		string input = line.Trim();
		string field = LineParser.ParseToken(ref input, TokenType.Parameter, essential: true).ToUpper();
		return delegate
		{
			switch (field)
			{
			case "SOURCE":
				data.ResponseSource = LineParser.ParseLiteral(ref input, "SOURCE", replace: true, data);
				break;
			case "STATUS":
				data.Status = (BotStatus)LineParser.ParseEnum(ref input, "STATUS", typeof(BotStatus));
				if (data.Status == BotStatus.CUSTOM)
				{
					data.CustomStatus = LineParser.ParseLiteral(ref input, "CUSTOM STATUS");
				}
				break;
			case "RESPONSECODE":
				data.ResponseCode = LineParser.ParseInt(ref input, "RESPONSECODE").ToString();
				break;
			case "COOKIE":
			{
				string key = LineParser.ParseLiteral(ref input, "NAME", replace: true, data);
				data.Cookies.Add(key, LineParser.ParseLiteral(ref input, "VALUE", replace: true, data));
				break;
			}
			case "ADDRESS":
				data.Address = LineParser.ParseLiteral(ref input, "ADDRESS", replace: true, data);
				break;
			case "USEPROXY":
			{
				string text = LineParser.ParseToken(ref input, TokenType.Parameter, essential: true).ToUpper();
				if (text == "TRUE")
				{
					data.UseProxies = true;
				}
				else if (text == "FALSE")
				{
					data.UseProxies = false;
				}
				break;
			}
			case "PROXY":
			{
				string input2 = LineParser.ParseLiteral(ref input, "PROXY", replace: true, data);
				data.Proxy = new CProxy(BlockBase.ReplaceValues(input2, data), (data.Proxy != null) ? data.Proxy.Type : ProxyType.Http);
				break;
			}
			case "PROXYTYPE":
				data.Proxy.Type = (ProxyType)LineParser.ParseEnum(ref input, "PROXYTYPE", typeof(ProxyType));
				break;
			case "DATA":
				data.Data = new CData(LineParser.ParseLiteral(ref input, "DATA", replace: true, data), new WordlistType());
				break;
			case "VAR":
			{
				CVar cVar4 = new CVar(LineParser.ParseLiteral(ref input, "NAME", replace: true, data), LineParser.ParseLiteral(ref input, "VALUE", replace: true, data));
				try
				{
					cVar4.Hidden = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false).ToBoolean();
				}
				catch
				{
				}
				data.Variables.Set(cVar4);
				break;
			}
			case "CAP":
			{
				CVar cVar3 = new CVar(LineParser.ParseLiteral(ref input, "NAME", replace: true, data), LineParser.ParseLiteral(ref input, "VALUE", replace: true, data), isCapture: true);
				try
				{
					cVar3.Hidden = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false).ToBoolean();
				}
				catch
				{
				}
				data.Variables.Set(cVar3);
				break;
			}
			case "GVAR":
				try
				{
					CVar cVar2 = new CVar(LineParser.ParseLiteral(ref input, "NAME", replace: true, data), LineParser.ParseLiteral(ref input, "VALUE", replace: true, data));
					try
					{
						cVar2.Hidden = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false).ToBoolean();
					}
					catch
					{
					}
					data.GlobalVariables.Set(cVar2);
				}
				catch
				{
				}
				break;
			case "NEWGVAR":
				try
				{
					CVar cVar = new CVar(LineParser.ParseLiteral(ref input, "NAME", replace: true, data), LineParser.ParseLiteral(ref input, "VALUE", replace: true, data));
					try
					{
						cVar.Hidden = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false).ToBoolean();
					}
					catch
					{
					}
					data.GlobalVariables.SetNew(cVar);
				}
				catch
				{
				}
				break;
			case "GCOOKIES":
				data.GlobalCookies.Clear();
				foreach (KeyValuePair<string, string> cookie in data.Cookies)
				{
					data.GlobalCookies.Add(cookie.Key, cookie.Value);
				}
				break;
			default:
				throw new ArgumentException("Invalid identifier " + field);
			}
			data.Log(new LogEntry("SET command executed on field " + field, Colors.White));
		};
	}
}
