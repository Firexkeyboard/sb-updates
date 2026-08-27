using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RuriLib.LS;

public static class LineParser
{
	public static string ParseToken(ref string input, TokenType type, bool essential, bool proceed = true)
	{
		string pattern = GetPattern(type);
		string text = "";
		Match match = new Regex(pattern).Match(input);
		if (match.Success)
		{
			text = match.Value;
			if (proceed)
			{
				input = input.Substring(text.Length).Trim();
			}
			if (type == TokenType.Literal)
			{
				text = text.Substring(1, text.Length - 2).Replace("\\\\", "\\").Replace("\\\"", "\"");
			}
		}
		else if (essential)
		{
			throw new ArgumentException("Cannot parse token");
		}
		return text;
	}

	public static void SetBool(ref string input, object instance)
	{
		string[] array = ParseToken(ref input, TokenType.Parameter, essential: true).Split('=');
		PropertyInfo propertyInfo = null;
		try
		{
			propertyInfo = instance.GetType().GetProperty(array[0]);
		}
		catch
		{
			return;
		}
		if (propertyInfo.GetValue(instance).GetType() != typeof(bool))
		{
			throw new InvalidCastException("The property " + array[0] + " is not a boolean");
		}
		string text = array[1].ToUpper();
		if (!(text == "TRUE"))
		{
			if (!(text == "FALSE"))
			{
				throw new ArgumentException("Expected bool value for '" + propertyInfo.Name + "'");
			}
			propertyInfo.SetValue(instance, false);
		}
		else
		{
			propertyInfo.SetValue(instance, true);
		}
	}

	public static dynamic ParseEnum(ref string input, string label, Type enumType)
	{
		string text = "";
		try
		{
			text = ParseToken(ref input, TokenType.Parameter, essential: true);
		}
		catch
		{
			throw new ArgumentException("Missing '" + label + "'");
		}
		try
		{
			return Enum.Parse(enumType, text, ignoreCase: true);
		}
		catch
		{
			throw new ArgumentException("Invalid '" + label + "'");
		}
	}

	public static string ParseLiteral(ref string input, string label, bool replace = false, BotData data = null)
	{
		try
		{
			if (replace)
			{
				return BlockBase.ReplaceValues(ParseToken(ref input, TokenType.Literal, essential: true), data);
			}
			return ParseToken(ref input, TokenType.Literal, essential: true);
		}
		catch
		{
			throw new ArgumentException("Expected Literal value for '" + label + "'");
		}
	}

	public static int ParseInt(ref string input, string label)
	{
		try
		{
			return int.Parse(ParseToken(ref input, TokenType.Parameter, essential: true));
		}
		catch
		{
			throw new ArgumentException("Expected Integer value for '" + label + "'");
		}
	}

	public static string ParseLabel(ref string input)
	{
		return ParseToken(ref input, TokenType.Label, essential: false).Substring(1);
	}

	public static void EnsureIdentifier(ref string input, string id)
	{
		if (ParseToken(ref input, TokenType.Parameter, essential: true).ToUpper() != id.ToUpper())
		{
			throw new ArgumentException("Expected identifier '" + id + "'");
		}
	}

	public static TokenType Lookahead(ref string input)
	{
		int result = 0;
		string text = ParseToken(ref input, TokenType.Parameter, essential: true, proceed: false);
		if (text.Contains("\""))
		{
			return TokenType.Literal;
		}
		if (text == "->")
		{
			return TokenType.Arrow;
		}
		if (text.StartsWith("#"))
		{
			return TokenType.Label;
		}
		if (text.ToUpper().Contains("=TRUE") || text.ToUpper().Contains("=FALSE"))
		{
			return TokenType.Boolean;
		}
		if (int.TryParse(text, out result))
		{
			return TokenType.Integer;
		}
		return TokenType.Parameter;
	}

	public static bool CheckIdentifier(ref string input, string id)
	{
		try
		{
			return ParseToken(ref input, TokenType.Parameter, essential: true, proceed: false).ToUpper() == id.ToUpper();
		}
		catch
		{
			return false;
		}
	}

	private static string GetPattern(TokenType type)
	{
		return type switch
		{
			TokenType.Label => "^#[^ ]*", 
			TokenType.Parameter => "^[^ ]*", 
			TokenType.Literal => "\"(\\\\.|[^\\\"])*\"", 
			TokenType.Arrow => "^->", 
			_ => "", 
		};
	}
}
