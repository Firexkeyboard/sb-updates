using System;
using System.Linq;
using System.Windows.Media;
using RuriLib.Functions.Conditions;

namespace RuriLib.LS;

internal class DeleteParser
{
	public static Action Parse(string line, BotData data)
	{
		string input = line.Trim();
		string field = LineParser.ParseToken(ref input, TokenType.Parameter, essential: true).ToUpper();
		return delegate
		{
			string text = "";
			Comparer comparer = Comparer.EqualTo;
			switch (field)
			{
			case "COOKIE":
			{
				if (LineParser.Lookahead(ref input) == TokenType.Parameter)
				{
					comparer = (Comparer)LineParser.ParseEnum(ref input, "TYPE", typeof(Comparer));
				}
				text = LineParser.ParseLiteral(ref input, "NAME");
				for (int i = 0; i < data.Cookies.Count; i++)
				{
					string key = data.Cookies.ToList()[i].Key;
					if (Condition.ReplaceAndVerify(key, comparer, text, data))
					{
						data.Cookies.Remove(key);
					}
				}
				break;
			}
			case "VAR":
				if (LineParser.Lookahead(ref input) == TokenType.Parameter)
				{
					comparer = (Comparer)LineParser.ParseEnum(ref input, "TYPE", typeof(Comparer));
				}
				text = LineParser.ParseLiteral(ref input, "NAME");
				data.Variables.Remove(comparer, text, data);
				break;
			case "GVAR":
				if (LineParser.Lookahead(ref input) == TokenType.Parameter)
				{
					comparer = (Comparer)LineParser.ParseEnum(ref input, "TYPE", typeof(Comparer));
				}
				text = LineParser.ParseLiteral(ref input, "NAME");
				try
				{
					data.GlobalVariables.Remove(comparer, text, data);
				}
				catch
				{
				}
				break;
			default:
				throw new ArgumentException("Invalid identifier " + field);
			}
			data.Log(new LogEntry("DELETE command executed on field " + field, Colors.White));
		};
	}
}
